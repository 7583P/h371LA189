"""Strict telemetry validation; failures return a nonzero exit code."""
import argparse
import json
from pathlib import Path
from racing_dataset import validate_session, trajectory_hash


def validate_telemetry_file(filepath):
    session = json.loads(Path(filepath).read_text(encoding='utf-8-sig'))
    errors = validate_session(session)
    print(f"{'FAIL' if errors else 'PASS'} {Path(filepath).name}: {'; '.join(errors) or 'integrity valid'}")
    return not errors


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--data-dir', default='Tools/ML/Data')
    parser.add_argument('--output')
    args = parser.parse_args()
    rows, fingerprints = [], set()
    for f in sorted(Path(args.data_dir).rglob('*.json')):
        try:
            session = json.loads(f.read_text(encoding='utf-8-sig'))
            if 'samples' not in session:
                continue
            errors = validate_session(session)
            fingerprint = trajectory_hash(session)
            duplicate = fingerprint in fingerprints
            fingerprints.add(fingerprint)
            rows.append(dict(file=str(f), errors=errors, duplicate=duplicate))
        except (ValueError, TypeError, KeyError) as ex:
            rows.append(dict(file=str(f), errors=[str(ex)], duplicate=False))
    report = dict(passed=bool(rows) and all(not r['errors'] for r in rows), sessions=len(rows), unique_trajectories=len(fingerprints), rows=rows)
    if args.output:
        Path(args.output).parent.mkdir(parents=True, exist_ok=True)
        Path(args.output).write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report, indent=2))
    return 0 if report['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
