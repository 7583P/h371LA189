"""Write a fresh manifest of release evidence and the exact source change list."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]


def digest(path):
    value = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            value.update(block)
    return value.hexdigest()


def git(*arguments):
    return subprocess.check_output(['git', *arguments], cwd=ROOT, text=True).splitlines()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('evidence', type=Path)
    args = parser.parse_args()
    destination = args.evidence.resolve()
    if not destination.is_relative_to(ROOT / 'Docs' / 'ReleaseGates'):
        raise ValueError('Only workspace release evidence is allowed')
    if git('branch', '--show-current') != ['main']:
        raise ValueError('Only main is authorized')
    outputs = [destination / name for name in ('changed-files.json', 'SHA256SUMS.json')]
    if any(path.exists() for path in outputs):
        raise FileExistsError('Never overwrite an earlier evidence manifest')
    baseline = json.loads((destination / 'baseline-inventory.json').read_text(encoding='utf-8-sig'))
    originals = {row['path']: row for row in baseline['sources']}
    paths = set(git('diff', '--name-only')) | set(git('ls-files', '--others', '--exclude-standard', 'Assets', 'Tools'))
    changed = []
    for name in sorted(paths):
        path = ROOT / name
        if path.is_file():
            before = originals.get(name)
            changed.append({'path': name, 'kind': 'modified' if before else 'new_or_not_in_source_snapshot',
                            'sha256': digest(path), 'bytes': path.stat().st_size,
                            'beforeSha256': before['sha256'] if before else None,
                            'snapshot': str(Path(baseline['snapshotRoot']) / name) if before else None})
    outputs[0].write_text(json.dumps({'head': git('rev-parse', 'HEAD')[0], 'branch': 'main',
                                    'committed': False, 'sourceFiles': changed}, indent=2), encoding='utf-8')
    rows = []
    for path in sorted(destination.rglob('*')):
        if path.is_file() and path != outputs[1]:
            rows.append({'path': path.relative_to(destination).as_posix(), 'bytes': path.stat().st_size,
                         'sha256': digest(path)})
    outputs[1].write_text(json.dumps({'algorithm': 'SHA-256', 'manifestExcludesItself': True,
                                    'files': rows, 'totalBytes': sum(row['bytes'] for row in rows)}, indent=2), encoding='utf-8')
    print(json.dumps({'sourceFiles': len(changed), 'evidenceFiles': len(rows),
                      'evidenceBytes': sum(row['bytes'] for row in rows)}))


if __name__ == '__main__':
    main()
