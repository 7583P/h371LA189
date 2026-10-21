"""Summarize real rendered QA frames without mixing bytes and timing counters."""
import argparse
import csv
import json
import math
import statistics
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def percentile(values, fraction):
    values = sorted(values)
    return values[min(len(values) - 1, math.floor(len(values) * fraction))]


def low_fps(values, fraction):
    slowest = sorted(values, reverse=True)[:max(1, math.ceil(len(values) * fraction))]
    return 1000 / statistics.mean(slowest)


def analyze(folder, output=None):
    destination = output if output is not None else folder / 'performance-analysis.json'
    if destination.exists():
        raise FileExistsError('Preserve prior performance evidence; choose a new --output')
    metrics = read(folder / 'metrics.json')
    proof = read(folder / 'frame-forensics.json')
    render = read(folder / 'render-proof.json')
    with (folder / 'frame-times.csv').open(encoding='utf-8-sig', newline='') as stream:
        frames = [float(r['milliseconds']) for r in csv.DictReader(stream)]
    with (folder / 'frame-forensics.csv').open(encoding='utf-8-sig', newline='') as stream:
        rows = [{k: float(v) for k, v in r.items()} for r in csv.DictReader(stream)]
    units = {('main:' if r['mainThreadOnly'] else 'all:') + r['name']: r
             for r in proof['counters']}
    def mean_counter(key):
        entry = units.get(key)
        if not entry or not entry['available']:
            return None
        return statistics.mean(r[key] for r in rows)
    # Never sum overlapping profiler scopes to manufacture a frame budget.
    duration_keys = [k for k, info in units.items()
                     if info['available'] and info['unit'] == 'TimeNanoseconds']
    worst = sorted(range(len(rows)), key=lambda i: rows[i]['wall_ms'], reverse=True)[:10]
    stalls = []
    for index in worst:
        row = rows[index]
        scopes = sorted(((k, row[k]) for k in duration_keys), key=lambda pair: pair[1], reverse=True)
        stalls.append(dict(frame=index, wall_ms=row['wall_ms'], unity_ms=row['unity_ms'],
                           previous_wall_ms=rows[index-1]['wall_ms'] if index else None,
                           next_wall_ms=rows[index+1]['wall_ms'] if index+1 < len(rows) else None,
                           main_cpu_ms=row['main_cpu_ms'], process_cpu_ms=row['process_cpu_ms'],
                           fixed_steps=int(row['fixed_steps']), focused=bool(row['focused']),
                           gc_count_changed=index > 0 and row['gc_collections'] != rows[index-1]['gc_collections'],
                           largest_scopes=scopes[:16]))
    racers = metrics['racers']
    ticks = sum(r['mlTicks'] for r in racers)
    comparisons = sum(r['mlComparisonTicks'] for r in racers)
    result = dict(path=str(folder), classification='REAL_RENDERED_QA_MEASURED_FRAMES_NOT_ANDROID_DEVICE_PROFILE',
                  seed=metrics['seed'], mmr=proof['mmr'], quality=metrics['quality'],
                  device=metrics['device'], graphics=metrics['graphics'],
                  batch_mode=metrics['batchMode'], onscreen_validation=metrics['onscreenValidation'],
                  frames=len(frames), forensic_frames=len(rows), dropped_forensic_frames=proof['droppedFrames'],
                  seconds=sum(frames)/1000, average_fps=1000/statistics.mean(frames),
                  median_fps=1000/statistics.median(frames), one_percent_low_fps=low_fps(frames, .01),
                  point_one_percent_low_fps=low_fps(frames, .001),
                  p95_ms=percentile(frames, .95), p99_ms=percentile(frames, .99), max_frame_ms=max(frames),
                  max_wall_ms=max(r['wall_ms'] for r in rows), frames_over_100ms=sum(t > 100 for t in frames),
                  frames_over_50ms=sum(t > 50 for t in frames),
                  main_thread_ms=metrics['meanMainThreadMs'],
                  render_thread_ms=mean_counter('all:CPU Render Thread Frame Time'),
                  physics_ms=mean_counter('main:Physics.Simulate'), ai_ms=mean_counter('main:Racing.AI.Decide'),
                  gpu_ms=metrics['meanGpuMs'] if metrics.get('gpuTimingAvailable') and metrics['meanGpuMs'] > 0 else None,
                  gc_mean_bytes=metrics['meanGcBytes'], gc_max_bytes=metrics['maxGcBytes'],
                  gc_collection_frames=metrics['gcCollectionFrames'], max_gc_collect_ms=metrics['maxGcCollectMs'],
                  observed_gc_collection_count=int(max(r['gc_collections'] for r in rows)-min(r['gc_collections'] for r in rows)),
                  peak_working_mib=proof['peakWorkingSetBytes']/1048576,
                  peak_private_mib=proof['privateBytes']/1048576,
                  focus_frames=sum(int(r['focused']) for r in rows),
                  unfocused_frames=sum(not r['focused'] for r in rows),
                  ac_states=sorted({r['acLineStatus'] for r in proof['power']}),
                  power_status_available=proof['powerStatusAvailable'],
                  power_configurations=[dict(zip(('ac', 'energy_saver', 'frame_rate_limit', 'v_sync', 'render_interval'), values))
                                        for values in sorted({(r['acLineStatus'], r['energySaver'], r['frameRateLimit'],
                                                               r['vSyncCount'], r['renderInterval']) for r in proof['power']})],
                  maximum_fixed_steps=int(max(r['fixed_steps'] for r in rows)),
                  fixed_steps_at_least_10=sum(r['fixed_steps'] >= 10 for r in rows),
                  synchronous_trace_writes=proof['synchronousDiagnosticTraceWrites'],
                  ml_ticks=ticks, ml_comparison_ticks=comparisons,
                  ml_inference_mean_us=sum(r['mlInferenceUs']*r['mlTicks'] for r in racers)/ticks if ticks else 0,
                  ml_full_tick_mean_us=sum(r['mlFullTickUs']*r['mlTicks'] for r in racers)/ticks if ticks else 0,
                  ml_scope_ms_per_frame=mean_counter('all:Racing3D.ML.Tick'),
                  ml_modes=sorted({r['mlMode'] for r in racers}),
                  ml_models=sorted({r.get('modelVersion', r.get('mlModelVersion', '')) for r in racers}),
                  render_proof=render, sampling_cpu_ms=proof['samplingCpuMs'], worst_wall_frames=stalls,
                  definitions='FPS tails use inverse mean of slowest ceil(N*p) actual frame-times.csv durations. p95/p99 are duration quantiles. Scope times overlap; all-thread waits are not a summed frame budget. Process memory peaks include QA diagnostics.')
    with destination.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2)
    print(json.dumps({k:v for k,v in result.items() if k not in ('worst_wall_frames','render_proof','definitions')}, indent=2))
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('folder', type=Path)
    parser.add_argument('--output', type=Path, help='Write reanalysis to a fresh path without changing the original evidence')
    args = parser.parse_args()
    analyze(args.folder, args.output)
