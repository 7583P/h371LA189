"""Ten-second observed item outcomes; never equate observational gain with causal effect."""
import argparse
import bisect
import json
import statistics
import csv
from collections import defaultdict
from pathlib import Path

ITEMS=['None','TurboPulse','KineticMissile','ElectroSpike','MagneticShield','PulseMine','HyperCharge','ShockwavePulse','TripleTurbo','TwinRock','TrackingDart','HomingRocket','AerialRecovery','Coin10','MiniTurbo','Lancer','GoldenRock']


def analyze(root):
    outcomes=[]
    traces={}
    def crossing(trace, distance):
        ds=trace['distances']; ts=trace['times']; at=bisect.bisect_left(ds,distance)
        if at==0:return ts[0]
        if at>=len(ds):return None
        span=ds[at]-ds[at-1]
        return ts[at-1]+(ts[at]-ts[at-1])*(distance-ds[at-1])/span if span>0 else ts[at]
    def at_time(trace, now):
        ts=trace['times'];ds=trace['distances'];at=bisect.bisect_left(ts,now)
        if at==0:return ds[0]
        if at>=len(ts):return None
        return ds[at-1]+(ds[at]-ds[at-1])*(now-ts[at-1])/(ts[at]-ts[at-1])
    def time_gap(case, index, now):
        distance=at_time(case[index],now)
        if distance is None:return None
        crossings=[t for trace in case if (t:=crossing(trace,distance)) is not None]
        return now-min(crossings) if crossings else None
    telemetry_paths = sorted(set(root.glob('telemetry/*.json')) | set(root.glob('mmr-*/telemetry/*.json')))
    for path in telemetry_paths:
        d=json.loads(path.read_text(encoding='utf-8-sig'));m=d['metadata'];samples=d['samples']
        if len(samples)<2:continue
        ts=[s['timestamp'] for s in samples]
        key=(m['playerMmr'],m['trackSeed']);trace_case=None;index=None;offset=0
        folder=root/f'mmr-{key[0]}'/f'seed-{key[1]}'
        if (folder/'elite-samples.csv').exists():
            if key not in traces:
                summary=json.loads((folder/'elite-forensics.json').read_text(encoding='utf-8-sig'))
                case=[dict(times=[],distances=[],finish=r['finishTime']) for r in summary['racers']]
                for row in csv.DictReader((folder/'elite-samples.csv').open(encoding='utf-8-sig')):
                    trace=case[int(row['racer'])]; distance=float(row['distance'])
                    if trace['distances'] and distance<trace['distances'][-1]:continue
                    trace['times'].append(float(row['time']));trace['distances'].append(distance)
                traces[key]=case
            trace_case=traces[key]
            index=min(range(len(trace_case)),key=lambda i:abs(trace_case[i]['finish']-m['totalRaceTime']))
            if abs(trace_case[index]['finish']-m['totalRaceTime'])>.01:trace_case=None
            else:
                length=json.loads((folder/'elite-forensics.json').read_text(encoding='utf-8-sig'))['trackLength']
                own=dict(times=ts,distances=[(s['lap']-1)*length+s['trackDistance'] for s in samples])
                offsets=[a-b for distance in [100,500,1000,2000] if (a:=crossing(own,distance)) is not None and (b:=crossing(trace_case[index],distance)) is not None]
                offset=statistics.median(offsets) if offsets else 0
        for e in d['events']:
            if e['eventType']!=10:continue
            at=bisect.bisect_left(ts,e['timestamp']);end=bisect.bisect_left(ts,e['timestamp']+10)
            if at>=len(samples) or end>=len(samples):continue
            a,b=samples[at],samples[end]
            # Player telemetry does not contain exact leader crossing clocks.
            # Keep meter gaps; do not invent a time-gap reduction from speed.
            item=int(e.get('param1',0));item=ITEMS[item] if 0<=item<len(ITEMS) else str(item)
            window=[x for x in d['events'] if e['timestamp']<=x['timestamp']<=e['timestamp']+10]
            before_gap=time_gap(trace_case,index,e['timestamp']-offset) if trace_case is not None else None
            after_gap=time_gap(trace_case,index,e['timestamp']+10-offset) if trace_case is not None else None
            outcomes.append(dict(seed=m['trackSeed'],mmr=m['playerMmr'],session=m['sessionId'],item=item,at=e['timestamp'],
                position_before=a['racePosition'],position_after=b['racePosition'],position_gain=a['racePosition']-b['racePosition'],
                leader_gap_before_m=a['distanceToLeadRacer'],leader_gap_after_m=b['distanceToLeadRacer'],leader_gap_reduction_m=a['distanceToLeadRacer']-b['distanceToLeadRacer'],
                gap_ahead_before_m=a['gapAhead'],gap_ahead_after_m=b['gapAhead'],rejoined=b['gapAhead']<=25,
                time_gap_reduction_seconds=before_gap-after_gap if before_gap is not None and after_gap is not None else None,
                recovery_in_window=any(x['eventType']==9 for x in window),hit_in_window=any(x['eventType']==11 for x in window),
                observation_note='Ten-second observed outcome; multiple items/traffic/route events can overlap; not a randomized item effect.'))
    groups=defaultdict(list)
    for o in outcomes:groups[(o['item'],'rear' if o['position_before']>=9 else 'middle' if o['position_before']>=4 else 'front')].append(o)
    summary=[]
    for (item,bracket),rr in sorted(groups.items()):
        summary.append(dict(item=item,bracket=bracket,n=len(rr),position_gain_mean=statistics.mean(r['position_gain'] for r in rr),
            gap_reduction_m_mean=statistics.mean(r['leader_gap_reduction_m'] for r in rr),
            time_gap_reduction_seconds_mean=statistics.mean(gaps) if (gaps:=[r['time_gap_reduction_seconds'] for r in rr if r['time_gap_reduction_seconds'] is not None]) else None,
            rejoin_fraction=statistics.mean(r['rejoined'] for r in rr),recoveries=sum(r['recovery_in_window'] for r in rr)))
    (root/'item-outcomes.json').write_text(json.dumps(dict(summary=summary,outcomes=outcomes),indent=2),encoding='utf-8')
    print(json.dumps(summary,indent=2))

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('root',type=Path);analyze(p.parse_args().root)
