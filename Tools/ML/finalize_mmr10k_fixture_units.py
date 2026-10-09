"""Migrate remaining explicit historical QA presets; original results stay archived."""
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[2]
if __name__=='__main__':
    if (ROOT/'tmp/mmr10k/feature-parity.json').exists():
        raise SystemExit('Completed one-time fixture migration; preserving the validated current names and units.')
    files=('BlacklineShortcutProbe','BlacklinePresentationProbe','BlacklineValidationProbe','BlacklineContracts','NightCityEvidenceProbe')
    for name in files:
        path=ROOT/f'Assets/AnimeHillRacing/Scripts/Racing3D/Diagnostics/{name}.cs'
        text=path.read_text(encoding='utf-8-sig')
        if '// current-10k QA presets' in text: continue
        text=re.sub(r'(?<![\w.])(100000|125000|97000|95000|90000|85000|75000|50000|25000|10000)(?![\w.])',lambda m:str(int(m[1])//10),text)
        text=text.replace('{1000,1000,2500','{100,1000,2500').replace('rating==1000','rating==100').replace('mmr+=1000','mmr+=100')
        text='// current-10k QA presets\n'+text
        path.write_text(text,encoding='utf-8')
    path=ROOT/'Assets/AnimeHillRacing/Scripts/Editor/RacingSystemAuditor.cs'
    text=path.read_text(encoding='utf-8-sig')
    # Names/text only: remove obsolete unit labels from current tests.
    for old,new in [('100k','10k'),('95k','9p5k'),('90k','9k'),('75k','7p5k'),('55k','5p5k'),('50k','5k'),('25k','2p5k'),('5kRare','500Rare'),('5k MMR','500 MMR'),('100,000','10,000'),('55,000','5,500')]: text=text.replace(old,new)
    path.write_text(text,encoding='utf-8')
    print('Remaining current QA presets updated; no driving statistics or archived evidence rewritten.')
