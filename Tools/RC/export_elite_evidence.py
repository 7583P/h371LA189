"""Per-racer observations and additive distance-bin associations, not causal claims."""
import argparse
import bisect
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def crossing(trace, distance):
    at = bisect.bisect_left(trace['distances'], distance)
    if at == 0:
        return trace['times'][0]
    if at == len(trace['distances']):
        return trace['times'][-1]
    span = trace['distances'][at] - trace['distances'][at-1]
    return trace['times'][at-1] + (trace['times'][at]-trace['times'][at-1]) * (distance-trace['distances'][at-1])/span


def make_trace(samples, finish, final_distance):
    times, distances = [0.0], [0.0]
    for sample in samples:
        if sample['distance'] > distances[-1] and sample['time'] > times[-1] and sample['time'] < finish:
            distances.append(sample['distance'])
            times.append(sample['time'])
    if final_distance > distances[-1]:
        distances.append(final_distance)
        times.append(finish)
    return dict(times=times, distances=distances)


def pass_counts(samples, count):
    completed = Counter()
    frames = defaultdict(dict)
    for sample in samples:
        frames[round(sample['time'], 4)][int(sample['racer'])] = sample
    pending = {}
    for now, frame in sorted(frames.items()):
        for i, own in frame.items():
            if int(own['flags']) & 1024:
                for j, rival in frame.items():
                    gap = rival['distance'] - own['distance']
                    key = (i, j)
                    if i != j and 2 < gap < 35 and key not in pending:
                        pending[key] = dict(start=now, clear=None, own=own['recoveries'], rival=rival['recoveries'])
        for key, attempt in list(pending.items()):
            i, j = key
            if i not in frame or j not in frame or now-attempt['start'] > 10:
                del pending[key]
                continue
            own, rival = frame[i], frame[j]
            if own['recoveries'] != attempt['own'] or rival['recoveries'] != attempt['rival']:
                del pending[key]
                continue
            ahead = own['distance'] - rival['distance']
            if ahead >= 6 and attempt['clear'] is None:
                attempt['clear'] = now
            if ahead < 3:
                attempt['clear'] = None
            if attempt['clear'] is not None and now-attempt['clear'] >= 1:
                completed[i] += 1
                del pending[key]
    return completed


def export(root):
    all_racers, all_bins, cases = [], [], []
    for path in sorted(root.glob('mmr-*/seed-*/metrics.json')):
        folder = path.parent
        metrics, summary = read(path), read(folder/'elite-forensics.json')
        with (folder/'elite-samples.csv').open(encoding='utf-8-sig', newline='') as stream:
            samples = [{k:float(v) for k,v in row.items()} for row in csv.DictReader(stream)]
        per = defaultdict(list)
        for sample in samples:
            per[int(sample['racer'])].append(sample)
        completed = pass_counts(samples, len(summary['racers']))
        metrics_by_name = {r['name']:r for r in metrics['racers']}
        mmr = int(folder.parent.name.split('-')[1])
        for index, observed in enumerate(summary['racers']):
            r = metrics_by_name[observed['name']]
            row = dict(mmr=mmr, seed=metrics['seed'], name=r['name'], finished=r['finished'], finish_time=r['finishTime'],
                       recoveries=r['recoveries'], racer_contacts=r['racerCollisions'], wall_contacts=r['wallCollisions'],
                       item_hits=r['itemHits'], item_uses=r['items'], boost_seconds=observed['boostSeconds'],
                       lancer_uses=r['lancerUses'], golden_rock_uses=r['itemUseCounts'][16],
                       shortcut_entries=observed['routeEntries'], shortcut_completions=observed['routeCompletions'],
                       abort_or_deferral_reason_changes=observed['routeAborts'],
                       near_safe_seconds=observed['nearSafeSeconds'], brake_below_safe_seconds=observed['brakeBelowSafeSeconds'],
                       traffic_braking_seconds=observed['blockedSeconds'], passing_decisions=observed['passAttempts'],
                       observed_completed_tactical_progress_passes=completed[index], position_gain_events=observed['positionChanges'],
                       off_optimal_lane_seconds=observed['offLineSeconds'], below_80_safe_seconds=observed['below80SafeSeconds'],
                       lancer_full_windows=r['lancerCompletedWindows'], lancer_interrupted_windows=r['lancerInterruptedWindows'],
                       peak_lancer_kmh=r['peakLancerSpeedKmh'])
            for i,t in enumerate(r['lapTimes']): row[f'lap_{i+1}_seconds'] = t
            for i,t in enumerate(r['sectorTimes']): row[f'sector_{i+1}_seconds'] = t
            all_racers.append(row)
        finished = sorted((r for r in metrics['racers'] if r['finished']), key=lambda r:r['finishTime'])
        case = dict(mmr=mmr, seed=metrics['seed'], finishers=len(finished), dnf=12-len(finished),
                    recoveries=sum(r['recoveries'] for r in metrics['racers']))
        if len(finished) == 12:
            first, last = finished[0], finished[-1]
            indices = {r['name']:i for i,r in enumerate(summary['racers'])}
            ia, ib = indices[first['name']], indices[last['name']]
            length = summary['trackLength'] * 3
            a = make_trace(per[ia], first['finishTime'], length)
            b = make_trace(per[ib], last['finishTime'], length)
            bounds = list(range(0, int(length), 50)) + [length]
            budget, positive, negative = Counter(), Counter(), Counter()
            for start,end in zip(bounds,bounds[1:]):
                a0,a1,b0,b1 = crossing(a,start),crossing(a,end),crossing(b,start),crossing(b,end)
                flags = 0; edge = False; route = False
                for sample in per[ib]:
                    if b0-.5 <= sample['time'] <= b1:
                        flags |= int(sample['flags'])
                        edge |= sample['edge'] > .1
                        route |= bool(sample['route'])
                kind = ('recovery' if flags&1 else 'item_spin' if flags&2 else 'wall_recent' if flags&256
                        else 'racer_contact_recent' if flags&128 else 'traffic_braking' if flags&64
                        else 'edge_command' if edge else 'route_traversal' if route
                        else 'airborne' if flags&8 else 'ordinary_execution')
                delta = (b1-b0)-(a1-a0)
                budget[kind] += delta
                if delta > 0: positive[kind] += delta
                else: negative[kind] += delta
                all_bins.append(dict(mmr=mmr,seed=metrics['seed'],first=first['name'],last=last['name'],
                                     start_m=start,end_m=end,first_seconds=a1-a0,last_seconds=b1-b0,
                                     difference_seconds=delta,last_racer_association=kind))
            spread = last['finishTime']-first['finishTime']
            case.update(spread_seconds=spread,first=first['name'],last=last['name'],
                        distance_bin_association_seconds=dict(budget),
                        positive_association_seconds=dict(positive),negative_association_seconds=dict(negative),
                        decomposition_residual_seconds=spread-sum(budget.values()),
                        sector_difference_seconds=[y-x for x,y in zip(first['sectorTimes'],last['sectorTimes'])])
        cases.append(case)
    def write_csv(path, rows):
        fields = list(dict.fromkeys(k for row in rows for k in row))
        with path.open('w',encoding='utf-8',newline='') as stream:
            writer=csv.DictWriter(stream,fieldnames=fields);writer.writeheader();writer.writerows(rows)
    write_csv(root/'all-racers.csv',all_racers)
    write_csv(root/'spread-distance-bins.csv',all_bins)
    (root/'spread-decomposition.json').write_text(json.dumps(dict(cases=cases,
        definitions='50 m crossing-time differences add to observed P1/P12 spread. Last-racer flags assign overlapping associations by explicit precedence; these are not causal seconds per contact/item. No route randomization. Near-safe means 90-110% final safe command. Brake-below-safe is a diagnostic candidate, not proven unnecessary. Abort field counts reason changes including crowd deferrals, not every repeated abort. Completed tactical progress pass means a rival 2-35m ahead during pass intent was cleared by 6m and remained at least 3m behind for 1s within 10s, with no recovery; not a physics overlap census.'),indent=2),encoding='utf-8')
    print(json.dumps(cases,indent=2))


if __name__ == '__main__':
    parser=argparse.ArgumentParser();parser.add_argument('root',type=Path)
    export(parser.parse_args().root)
