"""Summarize observed sectors and contact/route symptoms without causal attribution."""
import argparse
from collections import defaultdict
import csv
import json
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def write_csv(path, rows):
    if not rows:
        return
    with path.open('x', newline='', encoding='utf-8') as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('matrix', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    args.output.mkdir(exist_ok=False)
    all_sectors, all_racers, cases = [], [], []
    bits = {'recovering': 1, 'spunOut': 2, 'drifting': 4, 'airborne': 8, 'boosting': 16,
            'lancer': 32, 'trafficBraking': 64, 'contactWithin1s': 128, 'wallWithin1s': 256,
            'itemHitWithin3s': 512, 'committedPass': 1024}
    for path in sorted(args.matrix.glob('mmr-*/seed-*/elite-forensics.json')):
        case = path.parent.relative_to(args.matrix).as_posix()
        report = read(path)
        metrics = read(path.parent / 'metrics.json')
        length = report['trackLength']
        previous = {}
        sectors = {}
        with (path.parent / 'elite-samples.csv').open(encoding='utf-8-sig', newline='') as stream:
            for raw in csv.DictReader(stream):
                racer = int(raw['racer'])
                now = float(raw['time'])
                prior = previous.get(racer)
                previous[racer] = now
                if prior is None:
                    continue
                dt = now - prior
                if dt <= 0:
                    raise ValueError('Nonmonotonic sample times')
                sector = min(7, int((float(raw['distance']) % length) / length * 8))
                key = (racer, int(raw['lap']), sector)
                if key not in sectors:
                    sectors[key] = defaultdict(float)
                row = sectors[key]
                row['observedSeconds'] += dt
                row['speedIntegral'] += float(raw['speed']) * dt
                if int(raw['route']) > 0:
                    row['committedRouteSeconds'] += dt
                else:
                    row['mainRoadLaneSeconds'] += dt
                    row['mainRoadLaneErrorIntegral'] += abs(float(raw['lane']) - float(raw['optimal_lane'])) * dt
                if float(raw['safe']) > 1 and float(raw['speed']) < .8 * float(raw['safe']):
                    row['below80SafeSeconds'] += dt
                flags = int(raw['flags'])
                for name, bit in bits.items():
                    if flags & bit:
                        row[name + 'Seconds'] += dt
        for (racer, lap, sector), value in sorted(sectors.items()):
            seconds = value['observedSeconds']
            row = {'case': case, 'racerIndex': racer, 'name': report['racers'][racer]['name'], 'lap': lap,
                   'arcBin0To7': sector, 'observedSeconds': seconds,
                   'meanSpeedKmh': value['speedIntegral'] / seconds,
                   'committedRouteSeconds': value['committedRouteSeconds'],
                   'mainRoadLaneObservedSeconds': value['mainRoadLaneSeconds'],
                   'meanAbsMainRoadLaneErrorM': value['mainRoadLaneErrorIntegral'] / value['mainRoadLaneSeconds'] if value['mainRoadLaneSeconds'] else None,
                   'below80SafeSeconds': value['below80SafeSeconds']}
            row.update({name + 'Seconds': value[name + 'Seconds'] for name in bits})
            all_sectors.append(row)
        for index, racer in enumerate(report['racers']):
            row = {'case': case, 'racerIndex': index}
            row.update(racer)
            all_racers.append(row)
        ranked = sorted((r for r in metrics['racers'] if r['finished']), key=lambda r: r['finishTime'])
        laps = []
        gap_residual = None
        if len(ranked) == 12:
            winner, last = ranked[0], ranked[-1]
            for lap, (first_time, last_time) in enumerate(zip(winner['lapTimes'], last['lapTimes']), 1):
                laps.append({'lap': lap, 'winnerLapSeconds': first_time, 'lastFinisherLapSeconds': last_time,
                             'differenceSeconds': last_time - first_time})
            gap_residual = last['finishTime'] - winner['finishTime'] - sum(lap['differenceSeconds'] for lap in laps)
        routes_path = path.parent / 'shortcut-route-times.json'
        routes = read(routes_path)['spans'] if routes_path.exists() else []
        cases.append({'case': case, 'sampleCount': report['sampleCount'], 'contactCount': report['contactCount'],
                      'droppedSamples': report['droppedSamples'], 'droppedContacts': report['droppedContacts'],
                      'finishers': len(ranked), 'winnerVsLastLapDifferences': laps,
                      'finishGapMinusLapDifferenceSumSeconds': gap_residual,
                      'routeSpans': routes,
                      'allRouteChoicesObservedNotRandomized': True})
    write_csv(args.output / 'observed-sectors.csv', all_sectors)
    write_csv(args.output / 'observed-racers.csv', all_racers)
    result = {'classification': 'OBSERVATIONAL_REAL_PHYSICS_NO_CAUSAL_SECONDS_OR_FAKE_RESULTS',
              'limits': 'Eight diagnostic equal arc-length bins, not authored checkpoint sectors. Approximate durations use consecutive 10 Hz samples assigned to the ending bin. Main-road lane error excludes committed-route samples because projecting a shortcut onto the main road does not measure its navigation error. Flags overlap and cannot be summed into lost time. Lap differences need not sum to the finish gap; the residual is retained. Contacts and route selection are not randomized interventions. No driver commands, clocks, positions or saves are changed.',
              'cases': cases, 'racerRows': len(all_racers), 'sectorRows': len(all_sectors)}
    with (args.output / 'forensic-summary.json').open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2)
    print(json.dumps({'cases': len(cases), 'racers': len(all_racers), 'sectorRows': len(all_sectors),
                      'droppedSamples': sum(c['droppedSamples'] for c in cases),
                      'droppedContacts': sum(c['droppedContacts'] for c in cases)}))


if __name__ == '__main__':
    main()
