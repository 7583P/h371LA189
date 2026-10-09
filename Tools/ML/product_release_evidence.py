"""Summarize native product continuation evidence without inventing expert pace."""
import argparse
import collections
import csv
import json
from pathlib import Path
from statistics import mean
from racing_dataset import validate_session


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def summarize(paths, output):
    races, sessions, rejected, fastest, fastest_clean = [], [], [], None, None
    items, held_seconds, provenance = collections.Counter(), collections.Counter(), collections.Counter()
    for directory in paths:
        metric_paths = set(directory.glob('mmr-*/seed-*/metrics.json')) | set(directory.glob('seed-*/metrics.json'))
        for path in sorted(metric_paths):
            d = read(path)
            cars = d['racers']
            times = [c['finishTime'] for c in cars if c['finished']]
            routes = collections.defaultdict(lambda: dict(entries=0, completions=0))
            for car in cars:
                for route in car['routes']:
                    for key in ('entries', 'completions'):
                        routes[route['id']][key] += route[key]
            races.append(dict(evidence=path.as_posix(), mmr=cars[0]['mmr'], seed=d['seed'],
                racers=len(cars), finished=len(times), dnfs=len(cars)-len(times),
                p1_finish=min(times) if times else None,
                p12_finish=max(times) if len(times)==12 else None,
                finished_only_spread=max(times)-min(times) if times else None,
                complete_p1_p12_spread=max(times)-min(times) if len(times)==12 else None,
                recoveries=sum(c['recoveries'] for c in cars),
                collisions=sum(c['collisions'] for c in cars),
                walls=sum(c['wallCollisions'] for c in cars),
                racer_collisions=sum(c['racerCollisions'] for c in cars),
                item_hits=sum(c['itemHits'] for c in cars), items=sum(c['items'] for c in cars),
                jumps=sum(c['jumps'] for c in cars), tricks=sum(c['tricks'] for c in cars),
                landing_boosts=sum(c['landingBoosts'] for c in cars),
                chains=sum(c['jumpTrickLandingChains'] for c in cars),
                full_throttle_fraction=mean(c['fullThrottleSeconds']/max(.001,c['drivingSeconds']) for c in cars),
                routes=dict(routes), builds=sorted({(c['vehicle'],c['wheels']) for c in cars}),
                styles=sorted({c['style'] for c in cars}), models=sorted({c['modelVersion'] for c in cars}),
                pool_capacity=d['poolCapacity'], pool_created=d['poolCreated'],
                race_entities_created=d['itemEntitiesCreatedDuringRace'], pool_dropped=d['poolDroppedRequests'],
                time_scale=d['timeScale'], runtime_scope=d['classification'],
                clean_elite_gate=cars[0]['mmr']==10000 and len(times)==12 and
                    sum(c['recoveries'] for c in cars)==0 and 3<=max(times)-min(times)<=8))
        for path in sorted((directory/'telemetry').glob('*.json')):
            s=read(path);m=s['metadata'];errors=validate_session(s)
            provenance[m.get('source','UNVERIFIED')]+=1
            entry=dict(file=path.as_posix(),mmr=m['playerMmr'],seed=m['trackSeed'],
                legal=not errors, failures=errors[:5], best_lap=m.get('bestLapTime',0),
                laps=m.get('lapTimes',[]),source=m.get('source'),human=m.get('isHumanPlayer',False),
                recovery_events=sum(e.get('eventType')==9 for e in s.get('events',[])),
                track_revision=m.get('trackRevision'),rating_schema=m.get('schemaVersion'),
                item_catalog=m.get('itemCatalogVersion'),item_encoding=m.get('itemFeatureEncoding'))
            sessions.append(entry)
            if errors:rejected.append(entry)
            elif (entry['source']=='SCRIPTED_UNITY_PHYSICS' and not entry['human'] and
                  entry['best_lap']>0):
                if fastest is None or entry['best_lap']<fastest['best_lap']:fastest=entry
                if entry['recovery_events']==0 and (fastest_clean is None or entry['best_lap']<fastest_clean['best_lap']):fastest_clean=entry
            for e in s.get('events',[]):
                if e.get('eventType')==10:items[e.get('param1',0)]+=1
            samples=s['samples']
            for a,b in zip(samples,samples[1:]):
                if 0<b['timestamp']-a['timestamp']<.5:
                    held_seconds[a.get('activeItemType',0)]+=b['timestamp']-a['timestamp']
    matrix=[]
    for mmr in sorted({r['mmr'] for r in races}):
        rs=[r for r in races if r['mmr']==mmr]
        ss=[s for s in sessions if s['mmr']==mmr and s['legal']]
        laps=[lap for s in ss for lap in s['laps'] if lap>0]
        matrix.append(dict(mmr=mmr,seeds=[r['seed'] for r in rs],racers=sum(r['racers'] for r in rs),
            finished=sum(r['finished'] for r in rs),dnfs=sum(r['dnfs'] for r in rs),
            recoveries=sum(r['recoveries'] for r in rs),mean_legal_lap=mean(laps) if laps else None,
            fastest_legal_lap=min(laps) if laps else None,
            collisions=sum(r['collisions'] for r in rs), walls=sum(r['walls'] for r in rs),
            racer_collisions=sum(r['racer_collisions'] for r in rs),
            jumps=sum(r['jumps'] for r in rs), tricks=sum(r['tricks'] for r in rs),
            landing_boosts=sum(r['landing_boosts'] for r in rs), chains=sum(r['chains'] for r in rs),
            item_hits=sum(r['item_hits'] for r in rs), items=sum(r['items'] for r in rs),
            complete_spreads=[r['complete_p1_p12_spread'] for r in rs],routes={key:dict(
                entries=sum(r['routes'].get(key,{}).get('entries',0) for r in rs),
                completions=sum(r['routes'].get(key,{}).get('completions',0) for r in rs))
                for key in sorted({k for r in rs for k in r['routes']})}))
    human=[]
    for path in Path('Tools/ML/Data').rglob('*.json'):
        try:s=read(path)
        except (ValueError,OSError):continue
        m=s.get('metadata',{})
        if 'samples' not in s:continue
        clear=m.get('source')=='HUMAN_INPUT' and m.get('isHumanPlayer') is True
        errors=validate_session(s)
        current=(m.get('trackId')=='NightCityGrandCircuit' and m.get('vehicleName')=='formula-01' and m.get('wheelSetup')=='wheel-whity' and m.get('trackRevision')=='night-city-192-v3')
        human.append(dict(file=path.as_posix(),clearly_human=clear,current_build=current,
            passes_validation=not errors,errors=errors[:6],best_lap=m.get('bestLapTime',0)))
    verified=[h for h in human if h['clearly_human'] and h['current_build'] and h['passes_validation'] and h['best_lap']>0]
    target=min(verified,key=lambda h:h['best_lap']) if verified else None
    result=dict(scope='Real native 12-racer physics. Local league simulations and QA result fixtures are excluded.',
        matrix=matrix,races=races,telemetry_sessions=len(sessions),legal_sessions=len(sessions)-len(rejected),
        rejected_sessions=rejected,provenance=dict(provenance),fastest_verified_legal_ai=fastest,
        fastest_verified_zero_recovery_ai=fastest_clean,
        ai_pace_label='Verified legal AI telemetry — never an expert-human ground truth.',
        item_uses=dict(items),item_held_seconds=dict(held_seconds),
        human_scan=human,provisional_human_target=target,
        expert_gap_status='Best verified human telemetry currently available — provisional expert target.' if target else 'HUMAN GATE PENDING',
        normal_rating_scale_maximum=10000,
        all_elite_clean_pack_gates=bool([r for r in races if r['mmr']==10000]) and
            all(r['clean_elite_gate'] for r in races if r['mmr']==10000))
    output.parent.mkdir(parents=True,exist_ok=True)
    if output.exists():raise FileExistsError('Existing evidence is not overwritten: '+str(output))
    output.write_text(json.dumps(result,indent=2),encoding='utf-8')
    with output.with_suffix('.csv').open('w',newline='',encoding='utf-8') as f:
        keys=['mmr','seed','racers','finished','dnfs','p1_finish','p12_finish','complete_p1_p12_spread','finished_only_spread','recoveries','collisions','walls','items','item_hits','jumps','tricks','landing_boosts','chains','full_throttle_fraction','clean_elite_gate']
        writer=csv.DictWriter(f,fieldnames=keys,extrasaction='ignore');writer.writeheader();writer.writerows(races)
    print(json.dumps(dict(races=len(races),sessions=len(sessions),legal=len(sessions)-len(rejected),
        elite_pass=result['all_elite_clean_pack_gates'],fastest_ai=fastest and fastest['best_lap'],human_gate=result['expert_gap_status'],output=str(output))))


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--matrix',type=Path,nargs='+',required=True);parser.add_argument('--out',type=Path,required=True)
    args=parser.parse_args();summarize(args.matrix,args.out)
