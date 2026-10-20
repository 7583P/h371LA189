"""Observed elite metrics and matched distance timing; labels are associations."""
import argparse
import csv
import json
import math
import statistics
from collections import Counter, defaultdict
from pathlib import Path


def read(p):
    return json.loads(p.read_text(encoding='utf-8-sig'))


def rows(p):
    with p.open(encoding='utf-8-sig', newline='') as stream:
        return [{k:float(v) for k,v in r.items()} for r in csv.DictReader(stream)]


def percentile(v, q):
    return sorted(v)[min(len(v)-1, math.floor(len(v)*q))]


def analyze(root, pattern=None, output='analysis.json'):
    cases=[]
    paths=list(root.glob('mmr-*/seed-*/metrics.json'))+list(root.glob('elite-*-final/seed-*/metrics.json'))
    if pattern:
        paths=list(root.glob(pattern))
    for path in sorted(paths):
        folder=path.parent
        metrics=read(path); summary=read(folder/'elite-forensics.json')
        racers=summary['racers']; all_samples=rows(folder/'elite-samples.csv'); contacts=rows(folder/'elite-contacts.csv')
        per=defaultdict(list)
        for sample in all_samples: per[int(sample['racer'])].append(sample)
        category=Counter(); lap_contacts=Counter(); locations=Counter(); severe=[]
        for c in contacts:
            kind='wall' if c['other']<0 else 'nose_tail' if abs(c['normal_forward'])>.7 else 'side' if abs(c['normal_side'])>.7 else 'oblique'
            category[kind]+=1;lap_contacts[int(c['lap'])]+=1;locations[int(c['node'])]+=1
            samples=per[int(c['racer'])]
            after=[s['speed'] for s in samples if c['time'] < s['time'] <= c['time']+.6]
            minimum=min(after) if after else c['speed_at_contact']
            loss=max(0,c['speed_before']-minimum)
            if loss>20:severe.append(dict(c,kind=kind,observed_speed_drop_kmh=loss))
        times=[r['finishTime'] for r in metrics['racers'] if r['finished']]
        sorted_racers=sorted(metrics['racers'],key=lambda r:r['finishTime'] if r['finished'] else math.inf)
        first,last=sorted_racers[0],sorted_racers[-1]
        sectors=[b-a for a,b in zip(first['sectorTimes'],last['sectorTimes'])]
        sector_groups=[sum(sectors[i::3]) for i in range(3)]
        # Attributed deficits against each driver's *actual final safe command*.
        # These cannot establish causal contact costs or fully explain route value.
        deficits=[]
        for index,racer in enumerate(racers):
            budget=Counter()
            for a,b in zip(per[index],per[index][1:]):
                dt=b['time']-a['time'];speed=a['speed'];safe=a['safe'];flags=int(a['flags'])
                if dt<=0 or dt>.3 or safe<=40:continue
                deficit=max(0,1-speed/safe)*dt
                kind='recovery' if flags&1 else 'item_spin' if flags&2 else 'wall_recent' if flags&256 else 'racer_contact_recent' if flags&128 else 'traffic_braking' if flags&64 else 'edge_command' if a['edge']>.1 else 'shortcut' if a['route'] else 'airborne' if flags&8 else 'other_execution'
                budget[kind]+=deficit
            deficits.append(dict(name=racer['name'],finishTime=racer['finishTime'],safe_command_deficit_seconds=dict(budget),
                                 total_deficit_seconds=sum(budget.values()),measurement='Integral max(0,1-speed/final_safe_command)*dt; overlapping causes assigned by stated precedence; not causal seconds added to finish time.'))
        route_times=read(folder/'shortcut-route-times.json')['spans'] if (folder/'shortcut-route-times.json').exists() else []
        routes=[]
        for route in sorted({s['route'] for s in route_times}):
            for use in [False,True]:
                rr=[s for s in route_times if s['route']==route and s['shortcut']==use and s['recoveries']==0]
                if rr:routes.append(dict(route=route,shortcut_zone=use,count=len(rr),mean_seconds=statistics.mean(s['seconds'] for s in rr),median_seconds=statistics.median(s['seconds'] for s in rr),note='Zone overlap can misclassify main-road traversal; confirm AI entry counts in samples.'))
        case=dict(mmr=int(folder.parent.name.split('-')[1]),seed=metrics['seed'],finishers=len(times),dnf=len(metrics['racers'])-len(times),
                  ml_modes=sorted({r['mlMode'] for r in metrics['racers']}),
                  ml_ticks=sum(r['mlTicks'] for r in metrics['racers']),
                  recoveries=sum(r['recoveries'] for r in metrics['racers']),spread=max(times)-min(times) if len(times)==12 else None,
                  first=first['name'],last=last['name'],sector_difference_seconds=sectors,sector_group_difference_seconds=sector_groups,
                  start_finish_residual_seconds=(last['finishTime']-first['finishTime'])-sum(sectors),
                  racer_contact_counter=sum(r['racerCollisions'] for r in metrics['racers']),contact_callback_count=len(contacts),
                  contact_types=dict(category),contact_by_lap=dict(lap_contacts),top_contact_nodes=locations.most_common(12),
                  momentum_loss_contacts=len(severe),contact_relative_kmh_median=statistics.median(c['relative_kmh'] for c in contacts) if contacts else 0,
                  route_entries=sum(r['shortcutVisits'] for r in metrics['racers']),route_completions=sum(r['shortcutCompletions'] for r in metrics['racers']),
                  route_portals=routes,racers=racers,safe_speed_deficits=deficits,
                  severe_contacts=sorted(severe,key=lambda c:c['observed_speed_drop_kmh'],reverse=True)[:30],
                  samples_dropped=summary['droppedSamples'],contacts_dropped=summary['droppedContacts'])
        cases.append(case)
    (root/output).write_text(json.dumps(cases,indent=2),encoding='utf-8')
    print(json.dumps([{k:v for k,v in c.items() if k not in ('racers','safe_speed_deficits','severe_contacts','route_portals')} for c in cases],indent=2))


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('root',type=Path);p.add_argument('--pattern');p.add_argument('--output',default='analysis.json');args=p.parse_args();analyze(args.root,args.pattern,args.output)
