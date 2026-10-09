"""Aggregate measured races and preserve the distinction between AI and human evidence."""
import argparse
import csv
import hashlib
import json
import statistics
from collections import defaultdict
from pathlib import Path
from racing_dataset import validate_session, trajectory_hash

RATINGS=(1000,10000,25000,50000,75000,90000,97000,100000)

def read(path):return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def mean(values):return statistics.fmean(values) if values else None

def verified_telemetry_index(benchmark, validation):
    """Join immutable raw sessions to final race positions, never infer provenance."""
    audit=read(validation) if validation else None
    audited={str(Path(r['file']).resolve()):r for r in audit['rows']} if audit else {}
    index={}; rejected=[]; tech=defaultdict(list)
    from meta_tech import extract_occurrences
    for path in sorted((Path(benchmark)/'telemetry').glob('*.json')):
        session=read(path); meta=session['metadata']
        row=audited.get(str(path.resolve()))
        errors=row['errors'] if row is not None else validate_session(session)
        if errors:rejected.append(dict(file=str(path),errors=errors));continue
        key=(meta['trackSeed'],meta['playerMmr'],meta['finalPosition'])
        if key in index:raise ValueError('Ambiguous telemetry race/position identity.')
        index[key]=dict(file=str(path),source=meta['source'],best_lap=meta['bestLapTime'],
            vehicle=meta['vehicleName'],wheels=meta['wheelSetup'],
            sha256=hashlib.sha256(path.read_bytes()).hexdigest())
        tech[meta['playerMmr']].extend(extract_occurrences(session))
    tech_rows={}
    for rating, occurrences in tech.items():
        flights=[o for o in occurrences if o['flights']==1 and 'NaturalAirborne' not in o['actions']]
        repeated=[o for o in occurrences if o['flights']>1]
        tech_rows[rating]=dict(commanded_or_launch_pad_flights=len(flights),successful_landing_boost_flights=sum(o['success'] for o in flights),
            landing_boost_success=sum(o['success'] for o in flights)/len(flights) if flights else None,
            repeated_chains=len(repeated),successful_repeated_chains=sum(o['success'] for o in repeated),
            repeated_chain_success=sum(o['success'] for o in repeated)/len(repeated) if repeated else None,
            flights_by_depth={str(n):sum(o['flights']==n for o in repeated) for n in (2,3,4)},
            success_definition='Commanded jump or shared launch pad, observed landing boost and no collision/recovery; excludes natural road bounces.')
    return index,rejected,tech_rows
def human_gate(data):
    inspected=[];trusted=[];seen=set()
    for path in sorted(Path(data).rglob('*.json')):
        session=read(path)
        if 'samples' not in session:continue
        meta=session.get('metadata',{});errors=validate_session(session)
        human=meta.get('source')=='HUMAN_INPUT' and meta.get('isHumanPlayer') is True
        eligible=human and not errors and meta.get('trackId')=='NightCityGrandCircuit' and meta.get('trackRevision')=='night-city-192-v3' and meta.get('vehicleName')=='formula-01' and meta.get('wheelSetup')=='wheel-whity'
        fingerprint=trajectory_hash(session)
        inspected.append(dict(file=str(path),claimed_human=bool(meta.get('isHumanPlayer')),verified_human=human,
            integrity_errors=errors,eligible_current_build=eligible,duplicate=fingerprint in seen))
        seen.add(fingerprint)
        if eligible:trusted.append(dict(file=str(path),best_lap=meta['bestLapTime']))
    fastest=min(trusted,key=lambda r:r['best_lap']) if trusted else None
    return dict(status='PROVISIONAL_HUMAN_TARGET' if fastest else 'HUMAN GATE PENDING',
        label='Best verified human telemetry currently available — provisional expert target.' if fastest else None,
        fastest=fastest,expert_gap=None,inspected=inspected,
        explanation='AI and scripted telemetry are excluded from expert-human ground truth.')

def aggregate(benchmark,validation=None):
    verified,rejected,tech=verified_telemetry_index(benchmark,validation)
    groups=defaultdict(list);invalid=[]
    for path in sorted(Path(benchmark).glob('mmr-*/seed-*/metrics.json')):
        if not path.with_name('race.json').exists():continue
        race=read(path.with_name('race.json'));metrics=read(path)
        if not race.get('validFinishes') or not race.get('samePhysics'):invalid.append(str(path));continue
        rating=race['playerMmr']
        if not metrics.get('equalBuildBenchmark'):invalid.append(str(path));continue
        groups[rating].append((race,metrics,str(path)))
    rows=[];fastest=None
    for rating in RATINGS:
        races=groups[rating];racers=[r for _,m,_ in races for r in m['racers']]
        finished=[r for r in racers if r.get('finished')]
        def trusted(seed,racer):
            entry=verified.get((seed,rating,racer['position']))
            return entry if entry and entry['vehicle']==racer['vehicle'] and entry['wheels']==racer['wheels'] else None
        valid_racers=[r for race,m,_ in races for r in m['racers'] if trusted(race['seed'],r)]
        laps=[v for r in valid_racers for v in r['lapTimes'] if v>0]
        for _,metrics,path in races:
            for racer in metrics['racers']:
                evidence=trusted(metrics['seed'],racer)
                if evidence and racer.get('finished') and racer.get('lapTimes'):
                    candidate=dict(seconds=min(racer['lapTimes']),mmr=rating,name=racer['name'],seed=metrics['seed'],evidence=path,
                        verified_telemetry=evidence)
                    if fastest is None or candidate['seconds']<fastest['seconds']:fastest=candidate
        sector=[[],[],[]]
        for racer in valid_racers:
            for i,duration in enumerate(racer.get('sectorTimes',[])):sector[i%3].append(duration)
        totals={key:sum(r.get(key,0) for r in racers) for key in ('collisions','wallCollisions','racerCollisions','recoveries',
            'shortcutVisits','shortcutCompletions','failedShortcuts','miniTurbos','jumps','tricks','landingBoosts','aerialBoosts',
            'driftJumpChains','jumpTrickLandingChains','drifts','items','passingDecisions','defensiveDecisions','trafficBrakingDecisions')}
        routes=defaultdict(lambda:dict(entries=0,completions=0))
        for racer in racers:
            for route in racer.get('routes',[]):
                routes[route['id']]['entries']+=route['entries'];routes[route['id']]['completions']+=route['completions']
        seed_means={str(r['seed']):mean([v for racer in m['racers'] if trusted(r['seed'],racer) for v in racer['lapTimes']]) for r,m,_ in races}
        seed_bounds=None
        if len(seed_means)>=3:
            import numpy as np
            generator=np.random.default_rng(42);means=np.array(list(seed_means.values()))
            seed_bounds=[float(v) for v in np.percentile(generator.choice(means,(1000,len(means)),replace=True).mean(axis=1),(2.5,97.5))]
        rows.append(dict(mmr=rating,races=len(races),independent_seeds=len({r['seed'] for r,_,_ in races}),racers=len(racers),
            finished=len(finished),verified_telemetry_racers=len(valid_racers),laps=len(laps),mean_lap=mean(laps),best_lap=min(laps) if laps else None,
            median_lap=statistics.median(laps) if laps else None,lap_variance=statistics.pvariance(laps) if laps else None,
            mean_sectors=[mean(s) for s in sector],mean_recoveries_per_racer=totals['recoveries']/max(1,len(racers)),
            mean_collisions_per_racer=totals['collisions']/max(1,len(racers)),
            shortcut_success=totals['shortcutCompletions']/max(1,totals['shortcutVisits']),
            landing_boost_per_jump=totals['landingBoosts']/max(1,totals['jumps']),
            jump_trick_landing_per_trick=totals['jumpTrickLandingChains']/max(1,totals['tricks']),
            race_checks_passed=sum(r['passed'] for r,_,_ in races),totals=totals,routes=dict(routes),verified_tech=tech.get(rating,{}),
            seed_mean_laps=seed_means,seed_mean_lap_95pct_bootstrap=seed_bounds,
            rate_note='Mechanic count ratios are not opportunity-based success rates; verified flight and repeated-chain success is reported separately from telemetry.'))
    populated=[r for r in rows if r['races']]
    monotonic=len(populated)==len(RATINGS) and all(a['mean_lap']>b['mean_lap'] for a,b in zip(populated,populated[1:]))
    return dict(classification='CONTROLLED_DAPEX_WHITY_HEURISTIC_REAL_UNITY_PHYSICS',
        human_input=False,rows=rows,fastest_legal_ai=fastest,invalid_metrics=invalid,rejected_telemetry=rejected,
        verified_telemetry_sessions=len(verified),mean_lap_curve_monotonic=monotonic,
        sampling_note='Lap and sector statistics require checkpoint and trajectory validation, including partial laps before DNF. Pack mechanics, finish and recovery counts include all racers, with invalid telemetry explicitly listed. Fastest legal AI must finish and have verified telemetry. Three independent race seeds per rating; racers/laps in a seed share pack interactions. Headless accelerated races do not measure rendered FPS.')

def render_chart(report,output):
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    populated=[r for r in report['rows'] if r['races']]
    figure,axes=plt.subplots(2,1,figsize=(10,8),sharex=True,layout='constrained')
    x=[r['mmr']/1000 for r in populated]
    axes[0].plot(x,[r['mean_lap'] for r in populated],'o-',label='Mean valid lap')
    axes[0].plot(x,[r['best_lap'] for r in populated],'s--',label='Fastest valid lap')
    for r in populated:axes[0].scatter([r['mmr']/1000]*len(r['seed_mean_laps']),list(r['seed_mean_laps'].values()),s=20,alpha=.4,color='gray')
    axes[0].set_ylabel('Measured lap time (seconds)');axes[0].legend();axes[0].grid(alpha=.2)
    axes[0].set_title('Night City · DAPEX + Whity · shared physics · 12 scripted racers')
    axes[1].plot(x,[r['shortcut_success']*100 for r in populated],'o-',label='Shortcut completions / entries (%)')
    axes[1].plot(x,[(r['verified_tech'].get('landing_boost_success') or 0)*100 for r in populated],'s-',label='Verified launch flights with safe landing boost (%)')
    axes[1].set_xlabel('MMR (thousands)');axes[1].set_ylabel('Observed rate (%)');axes[1].set_xticks(x);axes[1].grid(alpha=.2);axes[1].legend()
    figure.savefig(output/'mmr-curve.png',dpi=180);figure.savefig(output/'mmr-curve.svg');plt.close(figure)

def source_manifest(baseline,output):
    rows=read(baseline);changes=[];missing=[];protected=[]
    for row in rows:
        path=Path(row['Path']);before=row['Hash'].lower()
        if not path.exists():missing.append(str(path));continue
        after=hashlib.sha256(path.read_bytes()).hexdigest()
        is_protected=str(path).startswith('Assets\\AnimeHillRacing\\Scripts\\') and '\\Racing3D\\' not in str(path) and '\\Editor\\' not in str(path)
        if is_protected:protected.append(dict(file=str(path),unchanged=before==after))
        if before!=after:changes.append(dict(file=str(path),before=before,after=after,protected=is_protected))
    report=dict(changed_from_session_baseline=changes,missing=missing,protected_runtime=protected,
        protected_runtime_unchanged=all(r['unchanged'] for r in protected))
    (output/'source-preservation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    return report

def run(benchmark,output,human_data,baseline,validation=None):
    output=Path(output);output.mkdir(parents=True,exist_ok=True)
    report=aggregate(benchmark,validation);report['human_gate']=human_gate(human_data)
    (output/'benchmark-summary.json').write_text(json.dumps(report,indent=2,allow_nan=False),encoding='utf-8')
    columns=('mmr','races','racers','finished','laps','mean_lap','best_lap','median_lap','lap_variance',
        'mean_recoveries_per_racer','mean_collisions_per_racer','shortcut_success','landing_boost_success',
        'repeated_chain_success','commanded_or_launch_pad_flights','repeated_chains',
        'landing_boost_events_per_jump_unbounded','jump_trick_landing_events_per_trick','race_checks_passed')
    with (output/'mmr-curve.csv').open('w',newline='',encoding='utf-8') as file:
        writer=csv.DictWriter(file,fieldnames=columns);writer.writeheader()
        for row in report['rows']:
            exported=dict(row)
            exported.update(row['verified_tech'])
            exported['landing_boost_events_per_jump_unbounded']=row['landing_boost_per_jump']
            exported['jump_trick_landing_events_per_trick']=row['jump_trick_landing_per_trick']
            writer.writerow({key:exported.get(key) for key in columns})
    source_manifest(baseline,output)
    render_chart(report,output)
    print(json.dumps(dict(races=sum(r['races'] for r in report['rows']),monotonic=report['mean_lap_curve_monotonic'],
        fastest=report['fastest_legal_ai'],human_gate=report['human_gate']['status'])))
    return report

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--benchmark',required=True);parser.add_argument('--output',required=True)
    parser.add_argument('--human-data',default='Tools/ML/Data');parser.add_argument('--baseline',default='tmp/release-candidate-20261005/baseline/source-hashes.json')
    parser.add_argument('--telemetry-validation')
    args=parser.parse_args();run(args.benchmark,args.output,args.human_data,args.baseline,args.telemetry_validation)
