"""Validate a retained native-render Off/ML02 Shadow pair before comparing it."""
import argparse
import json
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def compare(root, run_label='policy', off_label=None, shadow_label=None):
    samples = {}
    manifests = {}
    sources = {}
    reasons = []
    for mode in ('off', 'shadow'):
        source_label = (off_label if mode == 'off' else shadow_label) or run_label
        directory = root / f'performance-{mode}-{source_label}'
        sources[mode] = str(directory)
        sample = samples[mode] = read(directory / 'seed-4207/performance-analysis.json')
        manifest = manifests[mode] = read(directory / 'measurement-run.json')
        render = sample['render_proof']
        expected = 'Off' if mode == 'off' else 'Shadow'
        checks = {
            'all measured frames focused': sample['unfocused_frames'] == 0,
            'complete undropped frame samples': sample['frames'] == sample['forensic_frames'] and sample['dropped_forensic_frames'] == 0,
            'native window with camera and HUD': render['renderWorkVerified'] and render['renderMode'] == 'native' and sample['onscreen_validation'] and not sample['batch_mode'],
            'reference resolution and quality': (render['width'], render['height'], sample['quality']) == (1600, 900, 'High'),
            'reference seed and MMR': (sample['seed'], sample['mmr']) == (4207, 10000),
            'at least 59 seconds': sample['seconds'] >= 59,
            'stable AC and frame settings': sample['power_status_available'] and sample['ac_states'] == [1] and len(sample['power_configurations']) == 1 and sample['power_configurations'][0]['energy_saver'] == 0,
            'requested ML mode actually effective': set(sample['ml_modes']) - {''} == {expected},
            'unchanged executable and managed assembly': manifest['artifactsBefore'] == manifest['artifactsAfter'],
            'unchanged active power plan': manifest['powerPlanBefore'] == manifest['powerPlanAfter'],
        }
        checks['correct inference activity'] = sample['ml_ticks'] == 0 if mode == 'off' else sample['ml_ticks'] > 0 and sample['ml_comparison_ticks'] > 0
        checks['candidate ML02 loaded'] = set(sample['ml_models']) - {''} == {'ML02/' + manifest['modelSha256'][:12].upper()}
        reasons.extend(f'{mode}: {name}' for name, passed in checks.items() if not passed)
        sample['comparison_checks'] = checks

    for field in ('seed', 'mmr', 'quality', 'device', 'graphics', 'power_configurations'):
        if samples['off'][field] != samples['shadow'][field]:
            reasons.append(f'pair: different {field}')
    for field in ('width', 'height', 'antiAliasing'):
        if samples['off']['render_proof'][field] != samples['shadow']['render_proof'][field]:
            reasons.append(f'pair: different {field}')
    for field in ('artifactsBefore', 'powerPlanBefore', 'modelSha256'):
        if manifests['off'][field] != manifests['shadow'][field]:
            reasons.append(f'pair: different {field}')
    fields = ('average_fps', 'median_fps', 'one_percent_low_fps', 'point_one_percent_low_fps',
              'p95_ms', 'p99_ms', 'max_frame_ms', 'main_thread_ms', 'render_thread_ms',
              'physics_ms', 'ai_ms', 'gpu_ms', 'gc_mean_bytes', 'gc_max_bytes',
              'peak_working_mib', 'peak_private_mib', 'ml_ticks', 'ml_inference_mean_us', 'ml_full_tick_mean_us')
    result = dict(classification='LOCAL_WINDOWS_MEASUREMENT_NOT_ANDROID_OR_AUTHORITY_APPROVAL',
                  accepted=not reasons, rejection_reasons=reasons,
                  source_paths=sources,
                  cases={mode: {field: sample[field] for field in fields} | {'checks': sample['comparison_checks']}
                         for mode, sample in samples.items()},
                  shadow_minus_off={field: samples['shadow'][field] - samples['off'][field]
                                    for field in fields if samples['shadow'][field] is not None and samples['off'][field] is not None} if not reasons else None,
                  interpretation='An accepted single pair is an observed local comparison, not a statistical claim about ML cost. Partial 60-second race suite exits remain unchanged and do not replace the full-race gate. Unfocused/power-changing runs are retained but rejected. Overlapping scopes are not summed; stalls alone do not establish an ML cause.')
    destination = root / ('performance-comparison.json' if run_label == 'policy' else f'performance-comparison-{run_label}.json')
    destination.write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('root', type=Path)
    parser.add_argument('--run-label', default='policy')
    parser.add_argument('--off-label')
    parser.add_argument('--shadow-label')
    args = parser.parse_args()
    compare(args.root, args.run_label, args.off_label, args.shadow_label)
