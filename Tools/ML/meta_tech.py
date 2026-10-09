"""Offline measured tech clustering and conservative local meta drift evidence."""
import hashlib
import json
from collections import defaultdict
from pathlib import Path
import numpy as np


def stable_id(metadata, progress, actions):
    context=f"{metadata['trackId']}:{int(progress*16)}:{'>'.join(actions)}"
    hash_value=2166136261
    for character in context:hash_value=((hash_value^ord(character))*16777619)&0xffffffff
    return f'TECH_CHAIN_{hash_value:08X}'


def extract_occurrences(session):
    rows=session['samples']; m=session['metadata']; events=session.get('events',[])
    start=None; output=[]
    for i in range(1,len(rows)):
        s=rows[i]
        if start is None and s.get('isAirborne') and not rows[i-1].get('isAirborne'): start=i-1
        if start is None or s.get('isAirborne'): continue
        span=rows[start:i+1]; entry=span[0]
        inside=[e for e in events if entry['timestamp']<=e['timestamp']<=s['timestamp']+.2]
        commanded=any(x.get('jumpInput') for x in span) or any(
            e.get('eventType')==1 and e.get('tag')=='CommandJump' for e in inside)
        pad=any(e.get('eventType')==1 and e.get('tag')=='AutomaticLaunch' for e in inside)
        actions=['Jump' if commanded else 'LaunchPad' if pad else 'NaturalAirborne']
        if entry.get('isDrifting'): actions.insert(0,'Drift')
        if any(abs(x.get('pitchInput',0))>.15 for x in span): actions.append('PitchCorrection')
        if any(x.get('isAerialBoosting') for x in span): actions.append('AerialBoost')
        if any(abs(x.get('airRollInput',0))>.15 for x in span): actions.append('AirRollCorrection')
        landed=any(e.get('eventType')==3 for e in inside)
        failed=any(e.get('eventType') in (9,12) for e in inside)
        if any(e.get('eventType') in (6,7) for e in inside): actions.append('Shortcut')
        actions.append('LandingBoost' if landed else 'Landing')
        output.append(dict(tech_id=stable_id(m,entry['trackProgress'],actions),
            actions=actions,session=m['sessionId'],seed=m['trackSeed'],source=m.get('source'),
            start_time=entry['timestamp'],end_time=s['timestamp'],flights=1,
            context=(m['trackId'],m.get('trackRevision',''),m.get('vehicleName',''),m.get('wheelSetup','')),
            entry_progress=entry['trackProgress'],exit_progress=s['trackProgress'],
            entry_speed=entry['speedMps'],exit_speed=s['speedMps'],duration=s['timestamp']-entry['timestamp'],
            pitch=abs(s['pitch']),roll=abs(s['roll']),success=landed and not failed,
            failed=failed,time_saved=None,matched_baseline=False))
        start=None
    # Retain individual flights and separately measure repeated landing chains.
    # A road bounce must not become an invented commanded jump chain.
    flights=list(output)
    for a in range(len(flights)):
        for count in (2,3,4):
            chain=flights[a:a+count]
            if len(chain)!=count:continue
            if any('NaturalAirborne' in x['actions'] for x in chain):continue
            if any(x['end_time']+1.8<y['start_time'] or x['exit_progress']>y['entry_progress']+.002
                   for x,y in zip(chain,chain[1:])):continue
            first,last=chain[0],chain[-1]
            if last['end_time']-first['start_time']>10:continue
            between=[e for e in events if first['start_time']<=e['timestamp']<=last['end_time']]
            actions=[action for flight in chain for action in flight['actions']]
            failed=any(x['failed'] for x in chain) or any(e.get('eventType') in (9,12) for e in between)
            combined=dict(first,tech_id=stable_id(m,first['entry_progress'],actions),actions=actions,
                end_time=last['end_time'],flights=count,exit_progress=last['exit_progress'],
                exit_speed=last['exit_speed'],duration=last['end_time']-first['start_time'],
                pitch=last['pitch'],roll=last['roll'],failed=failed,
                success=all(x['success'] for x in chain) and not failed)
            output.append(combined)
    return output


def match_baselines(occurrences,sessions):
    """Compare exact progress bounds at similar entry speed, across race seeds.

    Only grounded legal traversals without hits, recoveries or shortcuts qualify.
    These may be scripted references; they are never expert-human ground truth.
    """
    references=defaultdict(list)
    for session in sessions:
        m=session['metadata'];key=(m['trackId'],m.get('trackRevision',''),m.get('vehicleName',''),m.get('wheelSetup',''))
        laps=defaultdict(list)
        for s in session['samples']:laps[s.get('lap',1)].append(s)
        for rows in laps.values():
            p=np.array([s['trackProgress'] for s in rows]);t=np.array([s['timestamp'] for s in rows])
            if len(p)<5 or np.any(np.diff(p)<-.002):continue
            references[key].append(dict(p=p,t=t,speed=np.array([s['speedMps'] for s in rows]),
                air=np.array([s.get('isAirborne',False) for s in rows]),events=session.get('events',[]),seed=m['trackSeed']))
    for occurrence in occurrences:
        entry,exit=occurrence['entry_progress'],occurrence['exit_progress']
        if exit<=entry or occurrence['failed']:continue
        matches=[]
        for ref in references[occurrence['context']]:
            if ref['seed']==occurrence['seed'] or entry<ref['p'][0] or exit>ref['p'][-1]:continue
            a=int(np.searchsorted(ref['p'],entry));b=int(np.searchsorted(ref['p'],exit))
            if a==0 or b>=len(ref['p']) or b<=a or np.any(ref['air'][a-1:b+1]):continue
            if abs(ref['speed'][a]-occurrence['entry_speed'])>max(2,occurrence['entry_speed']*.15):continue
            start=float(np.interp(entry,ref['p'],ref['t']));end=float(np.interp(exit,ref['p'],ref['t']))
            if end<=start or any(start<=e['timestamp']<=end and e.get('eventType') in (6,7,9,11,12) for e in ref['events']):continue
            matches.append(end-start)
        if len(matches)>=5:
            occurrence['time_saved']=float(np.median(matches))-occurrence['duration']
            occurrence['baseline_duration']=float(np.median(matches));occurrence['matched_baseline']=True


def report(sessions, output, previous=None):
    # Interpolated progress crossings do not eliminate command/state sampling
    # uncertainty. Require the saving's lower confidence bound to exceed one
    # observed sampling interval before scheduling local retraining.
    resolutions=[float(np.median(np.diff([s['timestamp'] for s in session['samples']])))
                 for session in sessions if len(session['samples'])>1]
    minimum_saving=max(.1,max(resolutions,default=.1))
    occurrences=[o for s in sessions for o in extract_occurrences(s)]
    match_baselines(occurrences,sessions)
    groups=defaultdict(list)
    for o in occurrences: groups[o['tech_id']].append(o)
    clusters=[]
    for key,rows in sorted(groups.items()):
        successes=sum(x['success'] for x in rows)
        matched=[x for x in rows if x['matched_baseline']]
        savings=[x['time_saved'] for x in matched]
        seeds={x['seed'] for x in matched}
        bounds=None
        if len(seeds)>=3:
            seed_means=np.array([np.mean([x['time_saved'] for x in matched if x['seed']==seed]) for seed in sorted(seeds)])
            rng=np.random.default_rng(42);boot=np.mean(rng.choice(seed_means,(1000,len(seed_means)),replace=True),axis=1)
            bounds=[float(x) for x in np.percentile(boot,[2.5,97.5])]
        eligible=len(matched)>=5 and len(seeds)>=3 and successes/len(rows)>=.85 and bounds is not None and bounds[0]>minimum_saving
        clusters.append(dict(tech_id=key,actions=rows[0]['actions'],flights=rows[0]['flights'],occurrences=len(rows),successes=successes,
            success_rate=successes/len(rows),risk=1-successes/len(rows),
            independent_seeds=len({x['seed'] for x in rows}),
            entry_speed_mps=float(np.mean([x['entry_speed'] for x in rows])),
            exit_speed_mps=float(np.mean([x['exit_speed'] for x in rows])),
            median_duration=float(np.median([x['duration'] for x in rows])),
            landing_pitch=float(np.mean([x['pitch'] for x in rows])),
            landing_roll=float(np.mean([x['roll'] for x in rows])),
            average_time_saved=float(np.mean(savings)) if savings else None,
            best_time_saved=float(max(savings)) if savings else None,
            matched_occurrences=len(matched),time_saving_95pct_seed_bootstrap=bounds,
            baseline_status='LEGAL_SHARED_PHYSICS_REFERENCE' if savings else 'MATCHED_LEGAL_BASELINE_PENDING',
            minimum_verified_saving_seconds=minimum_saving,
            promotion_eligible=eligible))
    # Repeated success alone cannot establish a faster route. A prior matched
    # traversal with confidence bounds is required before scheduling retraining.
    drift=[]
    if previous:
        old={c['tech_id']:c for c in json.loads(Path(previous).read_text())['clusters']}
        for c in clusters:
            prior=old.get(c['tech_id'])
            if c['promotion_eligible'] and (prior is None or not prior.get('promotion_eligible')):
                drift.append(dict(tech_id=c['tech_id'],status='REPEATED_LEGAL_TIME_SAVING',
                    retrain=True,reason='Local candidate retraining only; control authority stays Shadow.'))
    result=dict(clusters=clusters,observed_air_sequences=sum(o['flights']==1 for o in occurrences),
        observed_repeated_landing_chains=sum(o['flights']>1 for o in occurrences),meta_drift=drift,
        retrain_scheduled=any(x['retrain'] for x in drift),authority='SHADOW',minimum_evidence=dict(independent_seeds=3,attempts=5,
            success_rate=.85,minimum_saving_seconds=minimum_saving,
            required='Matched legal route, lower time-saving confidence bound above one sampling interval, unchanged build/track.'))
    Path(output).write_text(json.dumps(result,indent=2,allow_nan=False),encoding='utf-8')
    return result
