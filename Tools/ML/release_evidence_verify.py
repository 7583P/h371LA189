"""Check final local RC evidence and freeze hashes after all Unity runs finish."""
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import unquote


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Logs/ReleaseCandidate/RC20261006'
CLOSURE = ROOT / 'Logs/RacingPhase2/closure'
AUDIT = ROOT / 'tmp/release-candidate-20261005'
HEADINGS = ('EXECUTIVE VERDICT', 'BASELINE VS FINAL', 'GRAPHICS', 'NIGHT CITY', 'DAPEX + WHITY',
    'UI / UX', 'AI ROOT CAUSES FOUND', 'NEW MMR DIFFICULTY CURVE', 'MACHINE LEARNING', 'MOVEMENT TECH',
    '100K ELITE VALIDATION', 'FAIRNESS AUDIT', 'PERFORMANCE', 'TESTS', 'HUMAN GATE', 'FILES MODIFIED',
    'REMAINING RISKS', 'NEXT RECOMMENDED PHASE')


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def read_log(path):
    data = path.read_bytes()
    return data.decode('utf-16' if data.startswith((b'\xff\xfe', b'\xfe\xff')) else 'utf-8-sig')


def digest(path):
    value = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            value.update(chunk)
    return value.hexdigest()


def run():
    checks = []
    def check(condition, description):
        checks.append(dict(check=description, passed=bool(condition)))
    source = read(OUTPUT / 'source-build-manifest.json')
    check(source['initial_head'] == source['final_head'], 'HEAD preserved; no commit created')
    check(source['protected_runtime_unchanged'] and len(source['protected_runtime']) == 129,
          'All 129 protected Adventure runtime sources unchanged')
    check(not source['missing_original_sources'] and source['installed_models_unchanged'],
          'No missing initial source or replaced installed model')
    check(all(digest(ROOT / row['file']) == row['after_sha256'] for row in source['files']),
          'Final modified source files match the frozen manifest')
    check(all(digest(ROOT / row['file']) == row['sha256'] for row in source['build']),
          'Final EXE, runtime assembly and game configuration hashes match')
    restoration = read(AUDIT / 'final/qa-importer-restoration.json')
    check(all(digest(ROOT / row['file']) == row['initial_sha256'] for row in restoration['files']),
          'Buggy definition and original dirty prefab remain byte-identical to initial state')
    contracts = CLOSURE / 'rc-contracts-contentprotected-verified-20261006'
    names = ('foundation-tests', 'phase2-edit-contracts', 'improvement-edit-contracts',
             'release-edit-contracts', 'existing-cli-tests', 'item-drift-contracts', 'improvement-ui-contracts')
    reports = [read(contracts / (name + '.json')) for name in names]
    check(sum(row['passed'] for row in reports) == 374 and all(row['failed'] == 0 for row in reports),
          '374 Unity checks passed without relaxing failed race/Adventure gates')
    python_log = read_log(AUDIT / 'python-tests-final.txt')
    check('Ran 32 tests' in python_log and re.search(r'\nOK\s*$', python_log),
          'Final Python log records 32 tests and OK')
    benchmark = read(AUDIT / 'final/benchmark-summary.json')
    check(sum(row['races'] for row in benchmark['rows']) == 24 and
          benchmark['verified_telemetry_sessions'] == 282 and len(benchmark['rejected_telemetry']) == 6,
          '24-race benchmark includes 282 verified and six explicitly excluded trajectories')
    check(benchmark['human_gate']['status'] == 'HUMAN GATE PENDING' and
          benchmark['human_gate']['fastest'] is None and benchmark['human_gate']['expert_gap'] is None,
          'No invented expert-human pace or ghost')
    performance = read(OUTPUT / 'performance-summary.json')
    check(len(performance['phases']) == 3 and all(len(p['rows']) == 3 for p in performance['phases']),
          'Baseline, Off and Shadow each have three complete native rendered races')
    for label in ('rc-rendered-off-optimized-20261006', 'rc-rendered-shadow-optimized-20261006',
                  'rc-human-gate-build-releasefinal-20261006', 'rc-contracts-contentprotected-verified-20261006',
                  'rc-hero-player-motion-20261006', 'rc-adventure-player-protected-20261006'):
        save = read(CLOSURE / label / 'save-restored.json')
        check(save['Restored'] and save['Verified'] and save['Values'] == 442, f'442 saved values restored: {label}')
    text = (OUTPUT / 'AUDIT.md').read_text(encoding='utf-8')
    check(tuple(re.findall(r'^# (.+)$', text, re.MULTILINE)) == HEADINGS and
          'PENDING_FINAL_PERFORMANCE_TABLE' not in text, 'Audit contains the 18 required headings and final performance values')
    missing = []
    for path in list(OUTPUT.glob('*.md')) + list(OUTPUT.glob('*.html')):
        contents = path.read_text(encoding='utf-8-sig')
        refs = re.findall(r'\]\(([^)]+)\)', contents) if path.suffix == '.md' else re.findall(r'(?:href|src)="([^"]+)"', contents)
        for ref in refs:
            if re.match(r'^[a-zA-Z]+:', ref) or ref.startswith('#'):
                continue
            target = path.parent / unquote(ref.split('#')[0].strip('<>'))
            generated_here = target.parent.resolve() == OUTPUT.resolve() and target.name in ('artifact-manifest.json', 'evidence-checks.json')
            if not target.exists() and not generated_here:
                missing.append(dict(document=path.relative_to(ROOT).as_posix(), target=ref))
    check(not missing, 'All local document and native image links resolve')
    selected = list(OUTPUT.glob('*')) + list((AUDIT / 'final').glob('*.*'))
    selected += [AUDIT / name for name in ('final-telemetry-validation.json', 'fairness-source-audit.json',
                                          'provisional-human-search.json', 'candidate-training.log', 'python-tests-final.txt')]
    selected += list((CLOSURE / 'rc-mmr-matrix-complete-20261006/telemetry').glob('*.json'))
    selected += list((ROOT / 'Tools/ML/Models/Candidates/RC20261006').rglob('*.*'))
    for label in ('rc-hero-player-motion-20261006', 'rc-adventure-player-protected-20261006',
                  'rc-rendered-off-optimized-20261006', 'rc-rendered-shadow-optimized-20261006'):
        selected += [p for p in (CLOSURE / label).rglob('*') if p.suffix in ('.json', '.png', '.csv', '.txt')]
    artifacts = []
    for path in sorted(set(selected)):
        if path.is_file() and path.name not in ('artifact-manifest.json', 'evidence-checks.json'):
            artifacts.append(dict(file=path.relative_to(ROOT).as_posix(), bytes=path.stat().st_size, sha256=digest(path)))
    report = dict(recorded_utc=datetime.now(timezone.utc).isoformat(), checks=checks,
                  failed=sum(not row['passed'] for row in checks), missing_links=missing,
                  scope='Evidence integrity verification; not a PASS for release acceptance, elite behavior, Adventure matrix or human gates.')
    (OUTPUT / 'artifact-manifest.json').write_text(json.dumps(dict(recorded_utc=report['recorded_utc'],
        artifacts=artifacts), indent=2), encoding='utf-8')
    (OUTPUT / 'evidence-checks.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(dict(checks=len(checks), failed=report['failed'], artifacts=len(artifacts), missing_links=missing)))
    if report['failed']:
        raise SystemExit(1)


if __name__ == '__main__':
    run()
