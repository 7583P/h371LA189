"""Export owned QA logs into a fresh versionable directory, omitting credentials."""
import argparse
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
SENSITIVE = re.compile(r'(?i)(authorization\s*:\s*bearer\s+\S+|["\x27]?(?:access_token|refresh_token|password)["\x27]?\s*[:=]\s*["\x27]?\S+|token\s*:\s*\S+)')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('evidence', type=Path)
    args = parser.parse_args()
    evidence = args.evidence.resolve()
    if not evidence.is_relative_to(ROOT / 'Docs' / 'ReleaseGates'):
        raise ValueError('Only workspace evidence may be exported')
    output = evidence / 'logs'
    output.mkdir(exist_ok=False)
    private = ROOT / 'tmp' / 'ReleaseGates' / evidence.name
    paths = sorted(set(private.glob('*.log')) | set(private.glob('*.txt')) | set(evidence.rglob('*.log')))
    rows = []
    for path in paths:
        relative = path.relative_to(private if path.is_relative_to(private) else evidence)
        name = ('editor-' if path.is_relative_to(private) else 'player-') + str(relative).replace('\\', '__').replace('/', '__') + '.txt'
        raw = path.read_bytes()
        # Windows PowerShell redirects native stderr as UTF-16. Preserve readable
        # CLI failures and test logs while exporting only owned top-level logs;
        # private snapshots, registry exports and keys remain excluded.
        encoding = 'utf-16' if raw.startswith((b'\xff\xfe', b'\xfe\xff')) else 'utf-8-sig'
        lines = raw.decode(encoding, errors='replace').splitlines(keepends=True)
        count = 0
        for i, line in enumerate(lines):
            if SENSITIVE.search(line):
                lines[i] = '[REDACTED credential-like log line; original retained in local evidence]\n'
                count += 1
        result = ''.join(lines).encode('utf-8')
        (output / name).write_bytes(result)
        rows.append({'source': str(path.relative_to(ROOT)), 'sourceSha256': hashlib.sha256(raw).hexdigest(),
                     'export': name, 'exportSha256': hashlib.sha256(result).hexdigest(), 'redactedLines': count})
    (output / 'manifest.json').write_text(json.dumps({'logs': rows, 'registryAndKeysExported': False}, indent=2), encoding='utf-8')
    print(json.dumps({'logs': len(rows), 'redactedLines': sum(row['redactedLines'] for row in rows)}))


if __name__ == '__main__':
    main()
