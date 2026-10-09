"""Capture the existing validated dirty tree before the authorized product continuation."""
import hashlib
import json
import subprocess
import zipfile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Logs/ProductIntegration/RC20261006'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    baseline = OUT / 'continuation-baseline.json'
    if baseline.exists():
        print('Existing continuation baseline preserved.')
        return
    files = sorted(p for p in (ROOT / 'Assets').rglob('*') if p.is_file() and
                   p.suffix in ('.cs', '.meta', '.asset', '.prefab', '.shader', '.unity', '.png', '.bytes'))
    head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    status = subprocess.check_output(['git', 'status', '--short', '--untracked-files=normal'], cwd=ROOT, text=True)
    record = dict(recorded_utc=datetime.now(timezone.utc).isoformat(), head=head, initial_dirty_status=status,
                  prior_evidence='Logs/ReleaseCandidate/RC20261006/evidence-checks.json',
                  files=[dict(file=p.relative_to(ROOT).as_posix(), bytes=p.stat().st_size, sha256=digest(p)) for p in files])
    with zipfile.ZipFile(OUT / 'continuation-sources.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
        for path in files:
            if path.suffix in ('.cs', '.shader'):
                archive.write(path, path.relative_to(ROOT).as_posix())
    baseline.write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(json.dumps(dict(files=len(files), head=head, baseline=str(baseline))))


if __name__ == '__main__':
    main()
