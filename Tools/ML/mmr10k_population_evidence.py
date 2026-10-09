"""Evaluate staged population gates from real core, Unity and frame evidence."""
import argparse
import json
from pathlib import Path

from release_performance_report import phase, read


def evaluate(args):
    core = read(args.core / 'benchmark.json')
    baseline = read(args.baseline)
    off = phase(args.off, '10k compact league / current native build', 'Off', True)
    shadow = (phase(args.shadow, '10k compact league / ML02 Shadow', 'Shadow', True)
              if args.shadow else None)
    groups = [off] + ([shadow] if shadow else [])
    comparisons, league = [], []
    for group in groups:
        inherited = next(p for p in baseline['phases']
                         if p['mode'] == group['mode'] and p['pooled_actual_frames'])
        current, previous = group['pooled_actual_frames'], inherited['pooled_actual_frames']
        comparisons.append(dict(mode=group['mode'], current=current, inherited=previous,
            average_fps_change_percent=100 * (current['average_fps'] / previous['average_fps'] - 1),
            one_percent_low_change_percent=100 * (
                current['one_percent_low_fps'] / previous['one_percent_low_fps'] - 1),
            evidence_comparison='Same hardware, track, builds, styles, quality and seed set; separate runs retain OS/thermal variability. The previous normal elite 100k maps to current 10k.'))
        for path in sorted((args.off if group['mode'] == 'Off' else args.shadow).glob('seed-*/metrics.json')):
            m = read(path)
            league.append(dict(seed=m['seed'], mode=group['mode'],
                pump_calls=m['leagueRacePumpCalls'], pump_allocated_bytes=m['leagueRacePumpAllocatedBytes'],
                simulation_events=m['leagueSimulatedEventsDuringRace'],
                pump_counter_available=m['leaguePumpCounterAvailable'], pump_ms=m['meanLeaguePumpMs']))
    frozen = all(x['pump_calls'] > 0 and x['pump_allocated_bytes'] == 0 and
                 x['simulation_events'] == 0 and x['pump_counter_available'] for x in league)
    stable_60 = all(p['pooled_actual_frames']['one_percent_low_fps'] >= 60 for p in groups)
    no_regression = all(c['current']['average_fps'] >= c['inherited']['average_fps'] and
                        c['current']['one_percent_low_fps'] >= c['inherited']['one_percent_low_fps']
                        for c in comparisons)
    native = read(args.product / 'native-product-tests.json') if (
        args.product / 'native-product-tests.json').exists() else None
    native_pass = native is not None and native['failed'] == 0
    passes = core['desktopCoreQueryGates'] and frozen and stable_60 and no_regression and native_pass
    estimates = [dict(population=n, raw_records=n * core['bytesPerBot'],
        rank_index=(n + 1) * 20, one_season=n * core['seasonBytesPerBot'],
        twenty_retained_seasons=n * core['seasonBytesPerBot'] * 20,
        two_journals_minimum=n * core['bytesPerBot'] * 2,
        classification='Arithmetic storage estimate only; never a benchmark or mobile approval')
        for n in (10000, 100000, 1000000)]
    result = dict(scope='Staged 10k population audit; only measured populations can pass.',
        population=core['population'], core=core, native_product=native,
        retained_ram=read(args.ram / 'ram.json') if args.ram else None,
        rendered=groups, comparisons=comparisons, league_freeze=league,
        gates=dict(core_queries=core['desktopCoreQueryGates'], native_product=native_pass,
                   racing_league_zero_gc_and_events=frozen, stable_60fps=stable_60,
                   no_measured_fps_regression=no_regression,
                   phase_a_desktop_pass=passes, next_stage_authorized=passes,
                   android_device_validated=False),
        storage_extrapolation=estimates,
        next_stage=('Phase B desktop experiment permitted; Android approval remains separate.' if passes else
                    'STOP SCALING. Phase B and C were not run because Phase A has unresolved gates.'),
        production_recommendation='Retain 10,000 data bots. No larger population is approved by these measurements.',
        caveat='OS/thermal noise is not removed from frame samples, and no causal FPS claim is made solely from separate runs.')
    if args.out.exists():
        raise FileExistsError('Existing evidence is not overwritten: ' + str(args.out))
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, indent=2, allow_nan=False), encoding='utf-8')
    print(json.dumps(dict(output=str(args.out), gates=result['gates'], next_stage=result['next_stage'])))


if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('--core', type=Path, required=True)
    p.add_argument('--ram', type=Path)
    p.add_argument('--product', type=Path, required=True)
    p.add_argument('--off', type=Path, required=True)
    p.add_argument('--shadow', type=Path)
    p.add_argument('--baseline', type=Path,
                   default=Path('Logs/ReleaseCandidate/RC20261006/performance-summary.json'))
    p.add_argument('--out', type=Path, required=True)
    evaluate(p.parse_args())
