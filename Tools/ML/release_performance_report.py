"""Aggregate actual standalone frame samples; never turn CPU estimates into FPS."""
import argparse
import csv
import hashlib
import json
import math
import statistics
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def distribution(samples):
    ordered = sorted(samples)
    count = len(ordered)
    worst = max(1, math.ceil(count * .01))
    return dict(frames=count, measured_seconds=sum(samples) / 1000,
                average_fps=1000 / statistics.fmean(samples),
                one_percent_low_fps=1000 / statistics.fmean(ordered[-worst:]),
                p95_frame_ms=ordered[min(count - 1, math.floor(count * .95))],
                p99_frame_ms=ordered[min(count - 1, math.floor(count * .99))],
                frames_over_60fps_budget=sum(v > 1000 / 60 for v in samples),
                fraction_over_60fps_budget=sum(v > 1000 / 60 for v in samples) / count)


def phase(root, label, mode, raw_required):
    suite = read(root / 'suite.json')
    rows, all_samples = [], []
    for path in sorted(root.glob('seed-*/metrics.json')):
        metric = read(path)
        race = read(path.with_name('race.json'))
        proof_path = path.with_name('render-proof.json')
        proof = read(proof_path)
        assert metric['classification'] == 'WINDOWS_STANDALONE_FULL_OFFSCREEN_RENDER'
        assert metric['offscreenRendering'] and not metric['batchMode']
        assert metric['timeScale'] == 1 and metric['equalBuildBenchmark']
        assert metric['width'] == 1600 and metric['height'] == 900
        assert race['samePhysics'] and race['validFinishes']
        assert proof['targetCreated'] and proof['renderedFrames'] >= metric['frames']
        assert proof['canvases'] > 0
        assert len(metric['racers']) == 12
        assert all(r['vehicle'] == 'formula-01' and r['wheels'] == 'wheel-whity'
                   for r in metric['racers'])
        adapters = [r for r in metric['racers'] if r['mlMode']]
        if mode == 'Off':
            assert all(r['mlTicks'] == 0 for r in metric['racers'])
        elif mode == 'Shadow':
            assert len(adapters) == 11
            assert all(r['mlMode'] == 'Shadow' and r['modelVersion'].startswith('ML02')
                       and r['mlTicks'] > 0 for r in adapters)
        ticks = sum(r['mlTicks'] for r in adapters)
        network_us = sum(r['mlInferenceUs'] * r['mlTicks'] for r in adapters) / ticks if ticks else 0
        full_tick_us = sum(r['mlFullTickUs'] * r['mlTicks'] for r in adapters) / ticks if ticks else 0
        samples_path = path.with_name('frame-times.csv')
        sample_stats = None
        if samples_path.exists():
            with samples_path.open(encoding='utf-8-sig', newline='') as stream:
                samples = [float(r['milliseconds']) for r in csv.DictReader(stream)]
            assert len(samples) == metric['frames'] and all(math.isfinite(v) and v > 0 for v in samples)
            assert abs(statistics.fmean(samples) - metric['meanFrameMs']) < .0001
            sample_stats = distribution(samples)
            all_samples.extend(samples)
        elif raw_required:
            raise ValueError(f'Missing actual frame samples: {samples_path}')
        rows.append(dict(seed=metric['seed'], frames=metric['frames'],
            measured_seconds=metric['measuredSeconds'], average_fps=metric['averageFps'],
            one_percent_low_fps=metric['onePercentLowFps'], p95_frame_ms=metric['p95FrameMs'],
            p99_frame_ms=metric['p99FrameMs'], ai_cpu_ms=metric['meanAiMs'],
            physics_cpu_ms=metric['meanPhysicsMs'], render_cpu_ms=metric['meanRenderMs'],
            gc_bytes_per_frame=metric['meanGcBytes'], gc_max_bytes=metric['maxGcBytes'],
            gc_collections=metric['gcCollectionFrames'], gc_collect_max_ms=metric['maxGcCollectMs'],
            gc_worst_collection_frame_ms=metric['worstGcCollectionFrameMs'],
            draw_calls=metric['meanDrawCalls'], batches=metric['meanBatches'],
            gpu_timing_available=metric['gpuTimingAvailable'],
            ml_ticks=ticks, ml_network_us=network_us, ml_full_tick_us=full_tick_us,
            model_versions=sorted({r['modelVersion'] for r in adapters}),
            finished=sum(r['finished'] for r in metric['racers']),
            recoveries=sum(r['recoveries'] for r in metric['racers']),
            max_recoveries=max(r['recoveries'] for r in metric['racers']),
            runtime_errors=suite['errors'], runtime_warnings=suite['warnings'],
            strict_race_passed=race['passed'],
            item_entities_created_during_race=metric['itemEntitiesCreatedDuringRace'],
            instantiate_calls=metric['instantiateCalls'], destroy_calls=metric['destroyCalls'],
            peak_particle_systems=metric['peakActiveParticleSystems'], peak_particles=metric['peakParticles'],
            diagnostics=metric.get('diagnosticFrameCounters', []), raw_distribution=sample_stats,
            evidence=str(path), metrics_sha256=sha(path), render_proof_sha256=sha(proof_path),
            frame_times=str(samples_path) if samples_path.exists() else None,
            frame_times_sha256=sha(samples_path) if samples_path.exists() else None))
    assert [r['seed'] for r in rows] == [4207, 4211, 4219], 'Require three complete measured seeds.'
    weights = sum(r['frames'] for r in rows)
    weighted = {key:sum(r[key] * r['frames'] for r in rows) / weights
                for key in ('ai_cpu_ms', 'physics_cpu_ms', 'render_cpu_ms', 'gc_bytes_per_frame', 'draw_calls', 'batches')}
    weighted['average_fps'] = weights / sum(r['measured_seconds'] for r in rows)
    return dict(label=label, mode=mode, device=metric['device'], processor=metric['processor'],
        graphics=metric['graphics'], quality=metric['quality'], width=1600, height=900,
        onscreen_validation=False, classification=metric['classification'],
        suite_passed=suite['passed'], weighted=weighted, rows=rows,
        pooled_actual_frames=distribution(all_samples) if raw_required else None,
        timing_note='GPU timing is unavailable when its availability flag is false; zero is not a measured GPU cost. Inclusive profiler counters overlap and may include multiple threads.')


def run(args):
    output = Path(args.output)
    output.mkdir(parents=True, exist_ok=True)
    phases = [phase(Path(args.baseline), 'Before minimap/telemetry/VFX allocation fixes', 'Off', False),
              phase(Path(args.off), 'Final optimized build', 'Off', True),
              phase(Path(args.shadow), 'Final optimized build + local candidate', 'Shadow', True)]
    report = dict(scope='Three full rendered real-time Windows races per phase; DAPEX/Whity, 12 scripted drivers, 100k, High 1600x900, uncapped.',
        phases=phases,
        comparison_note='Same hardware/settings/three seeds. Separate runs include frame pacing, thermal and OS variability; Off/Shadow differences alone do not prove causal ML FPS regression. The baseline has per-race metrics but no raw frame CSV; no pooled baseline 1% low is invented.',
        acceptance_note='Average FPS alone cannot establish stable 60 FPS. Onscreen human and actual mobile performance remain pending.')
    (output / 'performance-summary.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    columns = ('phase', 'mode', 'seed', 'average_fps', 'one_percent_low_fps', 'p99_frame_ms',
               'ai_cpu_ms', 'physics_cpu_ms', 'render_cpu_ms', 'ml_network_us', 'ml_full_tick_us',
               'gc_bytes_per_frame', 'gc_collections', 'draw_calls', 'batches', 'finished', 'recoveries')
    with (output / 'performance.csv').open('w', encoding='utf-8', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=columns)
        writer.writeheader()
        for group in phases:
            for row in group['rows']:
                writer.writerow({key:group['label'] if key == 'phase' else group['mode'] if key == 'mode'
                                 else row[key] for key in columns})
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    figure, axes = plt.subplots(2, 1, figsize=(11, 7), layout='constrained')
    for group in phases:
        label = group['label'] + ' / ' + group['mode']
        axes[0].plot([r['seed'] for r in group['rows']], [r['average_fps'] for r in group['rows']], 'o-', label=label)
        axes[1].plot([r['seed'] for r in group['rows']], [r['one_percent_low_fps'] for r in group['rows']], 'o-', label=label)
    for axis in axes:
        axis.axhline(60, color='red', linestyle='--', linewidth=1)
        axis.set_xticks([4207, 4211, 4219])
        axis.grid(alpha=.2)
    axes[0].set_ylabel('Average FPS'); axes[0].legend(fontsize=8)
    axes[1].set_ylabel('1% low FPS'); axes[1].set_xlabel('Independent race seed')
    figure.suptitle('Actual GPU camera + HUD render; High 1600x900; RTX 3070 Laptop')
    figure.savefig(output / 'performance.png', dpi=180)
    figure.savefig(output / 'performance.svg')
    plt.close(figure)
    print(json.dumps([dict(label=p['label'], mode=p['mode'], weighted=p['weighted'],
        pooled=p['pooled_actual_frames']) for p in phases]))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--baseline', required=True)
    parser.add_argument('--off', required=True)
    parser.add_argument('--shadow', required=True)
    parser.add_argument('--output', default='Logs/ReleaseCandidate/RC20261006')
    run(parser.parse_args())
