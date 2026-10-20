"""Read preserved RC evidence without converting missing measurements into passes."""
import argparse
import csv
import datetime
import hashlib
import json
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Logs/RCPass/20261012'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()


def write(name, data):
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / name).write_text(json.dumps(data, indent=2), encoding='utf-8')


def provenance():
    target = OUT / 'provenance'
    if target.exists():
        raise RuntimeError('Initial provenance already exists; refusing to overwrite it.')
    target.mkdir(parents=True)
    def git(*args):
        return subprocess.check_output(['git', *args], cwd=ROOT)
    (target / 'status.txt').write_bytes(git('status', '--short'))
    (target / 'diff.patch').write_bytes(git('diff', '--binary'))
    files = set(git('diff', '--name-only', '-z').decode().split('\0'))
    files.update(git('ls-files', '--others', '--exclude-standard', '-z').decode().split('\0'))
    sources = []
    for name in sorted(files):
        if not name or name.startswith(('Logs/', 'Builds/', 'Tools/RC/')):
            continue
        source = ROOT / name
        if not source.is_file():
            sources.append(dict(path=name, absent=True))
            continue
        destination = target / 'working-source' / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, destination)
        sources.append(dict(path=name, sha256=sha(source), bytes=source.stat().st_size))
    builds = []
    for folder in sorted((ROOT / 'Builds').iterdir()):
        if not folder.is_dir():
            continue
        evidence = []
        for pattern in ('*.exe', '**/Assembly-CSharp.dll', '**/globalgamemanagers'):
            for p in folder.glob(pattern):
                evidence.append(dict(path=str(p.relative_to(ROOT)), bytes=p.stat().st_size, sha256=sha(p)))
        builds.append(dict(path=str(folder.relative_to(ROOT)), evidence=evidence,
                           executable_present=any(folder.glob('*.exe'))))
    selected = ROOT / 'Logs/RCPass/20261009/AiVehicleController.cs.selected'
    ai = ROOT / 'Assets/AnimeHillRacing/Scripts/Racing3D/AI/AiVehicleController.cs'
    data = dict(recorded_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                branch=git('branch', '--show-current').decode().strip(),
                head=git('rev-parse', 'HEAD').decode().strip(), sources=sources, builds=builds,
                ai_sha256=sha(ai), selected_ai_sha256=sha(selected),
                selected_ai_matches=sha(ai) == sha(selected),
                d2_ai_sha256=sha(ROOT / 'Logs/RCPass/20261009/AiVehicleController.cs.d2'))
    (target / 'manifest.json').write_text(json.dumps(data, indent=2), encoding='utf-8')
    print(json.dumps({k:v for k,v in data.items() if k != 'sources'}, indent=2))


def preserved():
    rows, matrices, perf = [], [], []
    for label in ('rc-d1-elite-01', 'rc-d2-elite-01', 'rc-final-elite-01', 'rc-final2-elite-01'):
        root = ROOT / 'Logs/RacingPhase2/closure' / label
        cases = []
        for p in sorted(root.glob('mmr-*/seed-*/metrics.json')):
            d = read(p)
            racers = d['racers']
            finished = [r['finishTime'] for r in racers if r['finished']]
            case = dict(label=label, mmr=int(p.parent.parent.name.split('-')[1]), seed=d['seed'],
                        finishers=len(finished), dnf=len(racers)-len(finished),
                        recoveries=sum(r['recoveries'] for r in racers),
                        spread=max(finished)-min(finished) if len(finished)==12 else None,
                        racer_contacts=sum(r['racerCollisions'] for r in racers),
                        wall_contacts=sum(r['wallCollisions'] for r in racers),
                        item_hits=sum(r['itemHits'] for r in racers),
                        route_entries=sum(r['shortcutVisits'] for r in racers),
                        route_completions=sum(r['shortcutCompletions'] for r in racers),
                        evidence=str(p.relative_to(ROOT)))
            cases.append(case)
            for racer in racers:
                rows.append(dict(racer, label=label, case_mmr=case['mmr'], seed=d['seed']))
        matrices.append(dict(label=label, cases=cases))
    for label in ('rc-final-perf-off-01', 'rc-final-perf-shadow-01'):
        d=read(ROOT / 'Logs/RCPass/20261009' / (label+'-performance.json'))
        perf.append({k:v for k,v in d.items() if k not in ('power','peaks')})
    write('preserved-matrices.json', matrices)
    write('preserved-racers.json', rows)
    write('preserved-performance.json', perf)
    keys=list(dict.fromkeys(k for r in rows for k in r))
    with (OUT/'preserved-racers.csv').open('w', newline='', encoding='utf-8') as stream:
        writer=csv.DictWriter(stream, fieldnames=keys);writer.writeheader()
        for row in rows:
            writer.writerow({k:json.dumps(v) if isinstance(v,(list,dict)) else v for k,v in row.items()})
    print(json.dumps(matrices, indent=2))


def archives():
    data=[]
    paths=list(ROOT.glob('*.aab'))+list(ROOT.glob('*.apk'))
    paths+=list((ROOT/'Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build/outputs').glob('**/*.aab'))
    for p in paths:
        with zipfile.ZipFile(p) as z:
            entries=[dict(path=i.filename, raw_bytes=i.file_size, zip_bytes=i.compress_size) for i in z.infolist()]
        groups={}
        for e in entries:
            name=e['path']
            group='unity_data' if '/assets/bin/Data/' in '/'+name else 'native' if '/lib/' in '/'+name else 'dex' if name.endswith('.dex') else 'other'
            bucket=groups.setdefault(group,dict(raw_bytes=0,zip_bytes=0,entries=0))
            for k in ('raw_bytes','zip_bytes'):bucket[k]+=e[k]
            bucket['entries']+=1
        item=dict(path=str(p.relative_to(ROOT)), bytes=p.stat().st_size,sha256=sha(p),
                  groups=groups,top50=sorted(entries,key=lambda e:e['zip_bytes'],reverse=True)[:50],
                  abis=sorted({e['path'].split('/lib/')[1].split('/')[0] for e in entries if '/lib/' in e['path']}))
        data.append(item)
    write('existing-android-archives.json',data)
    print(json.dumps([{k:v for k,v in d.items() if k!='top50'} for d in data],indent=2))


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('mode',choices=('provenance','preserved','archives'))
    {'provenance':provenance,'preserved':preserved,'archives':archives}[parser.parse_args().mode]()
