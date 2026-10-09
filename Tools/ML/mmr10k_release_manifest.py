"""Freeze and verify the current continuation, without rewriting prior RC evidence."""
import argparse
import hashlib
import json
import subprocess
import zipfile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / 'Logs/MMR10KMigration/RC20261006/continuation-baseline.json'
OUT = ROOT / 'Logs/MMR10KMigration/RC20261006'
SHARED = [
    'Assets/AnimeHillRacing/Scripts/Vehicle/VehiclePhysics2DController.cs',
    'Assets/AnimeHillRacing/Scripts/Racing3D/Vehicles/RacingVehiclePhysics.cs',
    'Assets/AnimeHillRacing/Scripts/Racing3D/Vehicles/RacingVehicleStats.cs',
    'Assets/AnimeHillRacing/Scripts/Racing3D/Vehicles/RacingStatsCalculator.cs',
    'Assets/AnimeHillRacing/Scripts/Racing3D/Environment/RacingJumpPad.cs',
]
WEIGHTS = {
    'Tools/ML/Models/behavior_baseline.pt': '7ba02c5eb2955a1b63f41dd07b486b6e85ef2b0c2bd63d57d588759053a31c92',
    'Tools/ML/Models/temporal_model.pt': 'd26cd907dac2a7cadaff17a745c3476504a3f7cb6bdc5a35624d0b0af88bc64d',
    'Assets/AnimeHillRacing/Resources/RacingMlWeights.bytes': '0eb9b23327d6de4dfd05a5c104d459d2771d73e3df9d31a6c290ea537ff9ab33',
}


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def file_row(path):
    return dict(file=path.relative_to(ROOT).as_posix(), bytes=path.stat().st_size,
                sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT, text=True).strip()


def allowed(path):
    prefix = 'Assets/AnimeHillRacing/'
    if not path.startswith(prefix):
        return False
    relative = path[len(prefix):]
    return (relative.startswith('Scripts/Racing3D/') or
            relative.startswith('Resources/RacingProgressionConfiguration.asset') or
            relative.startswith('Scripts/Editor/Racing')) and path not in SHARED


def freeze(build_report):
    target = OUT / 'final-source-build-manifest.json'
    if target.exists():
        raise FileExistsError('Final evidence is immutable: ' + str(target))
    old = {r['file']: r for r in read(BASE)['files']}
    current = {r['file']: r for p in (ROOT / 'Assets').rglob('*') if p.is_file()
               for r in [file_row(p)]}
    changed = [p for p in old if p in current and old[p]['sha256'] != current[p]['sha256']]
    missing = sorted(set(old) - set(current))
    added = sorted(set(current) - set(old))
    outside = [p for p in changed + added if not allowed(p)]
    prebuild = read(OUT / 'prebuild-selected-source.json')
    prebuild_source = {r['file']: r['sha256'] for r in prebuild['source']}
    source_matches_prebuild = prebuild_source == {p: r['sha256'] for p, r in current.items()}
    weight_checks = []
    for relative, expected in WEIGHTS.items():
        row = file_row(ROOT / relative)
        weight_checks.append(dict(**row, expected_sha256=expected, preserved=row['sha256'] == expected))
    preserved = [dict(file=p, preserved=p in current and current[p]['sha256'] == old[p]['sha256'],
                      sha256=current.get(p, {}).get('sha256')) for p in SHARED]
    build = ROOT / 'Builds/HumanGateLatest'
    binaries = [file_row(p) for p in build.rglob('*') if p.is_file()]
    summary = (ROOT / build_report / 'build.txt').read_text(encoding='utf-8-sig')
    if 'result=Succeeded' not in summary or 'errors=0' not in summary:
        raise ValueError('A fresh successful build report is mandatory.')
    result = dict(recorded_utc=datetime.now(timezone.utc).isoformat(), head=git('rev-parse', 'HEAD'),
        branch=git('branch', '--show-current'), baseline=str(BASE.relative_to(ROOT)),
        baseline_files=len(old), current_assets=len(current), changed=sorted(changed), added=added,
        missing=missing, changes_outside_migration_scope=outside,
        shared_physics=preserved, installed_models=weight_checks,
        source=[current[p] for p in sorted(current)], build=binaries, build_report=str(build_report),
        prebuild_source_evidence='Logs/MMR10KMigration/RC20261006/prebuild-selected-source.json',
        source_matches_prebuild=source_matches_prebuild,
        build_summary=summary, candidate=file_row(ROOT / 'Tools/ML/Models/Candidates/RC20261006/behavior_mlp.bytes'),
        no_commit=git('rev-parse', 'HEAD') == read(BASE)['head'])
    if missing or outside or not source_matches_prebuild or not all(r['preserved'] for r in preserved + weight_checks) or not result['no_commit']:
        raise ValueError('Continuation protection failed: ' + json.dumps(
            dict(missing=missing, outside=outside, source_matches_prebuild=source_matches_prebuild, shared=preserved, weights=weight_checks)))
    target.write_text(json.dumps(result, indent=2), encoding='utf-8')
    with zipfile.ZipFile(OUT / 'final-source-snapshot.zip', 'x', compression=zipfile.ZIP_DEFLATED) as archive:
        for p in sorted(current):
            if Path(p).suffix in {'.cs', '.shader', '.asset', '.mat', '.prefab', '.meta'}:
                archive.write(ROOT / p, p)
    print(json.dumps(dict(manifest=str(target), changed=len(changed), added=len(added),
                          preserved_shared=len(preserved), models_preserved=len(weight_checks))))


def verify():
    manifest = read(OUT / 'final-source-build-manifest.json')
    checks = []
    def check(name, value): checks.append(dict(check=name, passed=bool(value)))
    check('HEAD unchanged; no commit created', git('rev-parse', 'HEAD') == manifest['head'] == read(BASE)['head'])
    check('All frozen source assets retain exact bytes', all(
        (ROOT / r['file']).exists() and file_row(ROOT / r['file'])['sha256'] == r['sha256']
        for r in manifest['source']))
    check('Frozen source inventory has no missing or additional assets',
          {p.relative_to(ROOT).as_posix() for p in (ROOT / 'Assets').rglob('*') if p.is_file()} ==
          {r['file'] for r in manifest['source']})
    check('Final build source matches the pre-build selected source exactly',
          manifest['source_matches_prebuild'] and
          {r['file']: r['sha256'] for r in manifest['source']} ==
          {r['file']: r['sha256'] for r in read(ROOT / manifest['prebuild_source_evidence'])['source']})
    check('Full final build retains exact bytes', all(
        (ROOT / r['file']).exists() and file_row(ROOT / r['file'])['sha256'] == r['sha256']
        for r in manifest['build']))
    check('Installed models preserved', all(file_row(ROOT / p)['sha256'] == expected
                                            for p, expected in WEIGHTS.items()))
    check('Shared 2D/3D physical forces and stats preserved', all(r['preserved'] for r in manifest['shared_physics']))
    check('ML02 local Shadow candidate unchanged', file_row(ROOT / manifest['candidate']['file'])['sha256'] == manifest['candidate']['sha256'])
    result = dict(recorded_utc=datetime.now(timezone.utc).isoformat(), checks=checks,
                  passed=sum(r['passed'] for r in checks), failed=sum(not r['passed'] for r in checks))
    path = OUT / 'final-integrity-checks.json'
    if path.exists(): raise FileExistsError(str(path))
    path.write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result))
    if result['failed']: raise ValueError('Final integrity verification failed.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--build-report', type=Path)
    parser.add_argument('--verify', action='store_true')
    args = parser.parse_args()
    if args.verify: verify()
    elif args.build_report: freeze(args.build_report)
    else: parser.error('--build-report or --verify is required')
