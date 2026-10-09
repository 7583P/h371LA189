"""One-time continuation fixture migration; no original evidence is rewritten."""
import json
import re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]


def edit(relative, change):
    path=ROOT/relative
    before=path.read_text(encoding='utf-8-sig')
    after=change(before)
    if before==after:
        return
    path.write_text(after,encoding='utf-8')


def compress_rating_literals(text):
    # Explicit MMR-only validation sections, never seeds, coin contracts or physics.
    return re.sub(r'(?<![\w.])(\d{5,6})(?![\w.])',lambda m:str(int(m[1])//10),text)


def auditor(text):
    for start,end in [('        public static void TestVoidRespawnAndCollision','        public static void TestUprightSpinAudioAndFormulaVisuals'),('        public static void TestAiSkill','        public static')]:
        first=text.find(start)
        if first<0: continue
        last=text.find(end,first+len(start))
        if last<0: last=len(text)
        body=compress_rating_literals(text[first:last])
        text=text[:first]+body+text[last:]
    # Other identity/AI fixtures use explicit old elite rating literals.
    text=text.replace(', 100000, true',', 10000, true').replace(', 100000, false',', 10000, false').replace('CreateProfile(100000,','CreateProfile(10000,')
    return text


if __name__=='__main__':
    if (ROOT/'tmp/mmr10k/feature-parity.json').exists():
        raise SystemExit('Completed one-time fixture migration; current MMR values must not be divided again.')
    edit('Assets/AnimeHillRacing/Scripts/Editor/RacingSystemAuditor.cs',auditor)
    def improvement(t):
        t=compress_rating_literals(t)
        t=t.replace('int[] ratings={0,1000,','int[] ratings={0,100,')
        t=t.replace('100k','10k').replace('20k','2k')
        t=t.replace('"1K","10K","25K","50K","75K","90K","97K","100K"','"100","1,000","2,500","5,000","7,500","9,000","9,700","10,000"')
        return t
    edit('Assets/AnimeHillRacing/Scripts/Racing3D/Diagnostics/RacingImprovementContracts.cs',improvement)
    edit('Assets/AnimeHillRacing/Scripts/Racing3D/Diagnostics/RacingClosureContracts.cs',lambda t:t.replace('100000','10000'))
    edit('Assets/AnimeHillRacing/Scripts/Editor/RacingReleaseContracts.cs',lambda t:t.replace('CreateProfile(100000,','CreateProfile(10000,'))
    edit('Assets/AnimeHillRacing/Scripts/Editor/RacingPerformanceBenchmarkRunner.cs',lambda t:t.replace('Mathf.Clamp(parsed,1000,100000):85000','Mathf.Clamp(parsed,0,10000):8500').replace('playerMmr=85000','playerMmr=8500'))
    def product(t):
        return t.replace('1000 persistent bots','10000 compact persistent bots').replace('state.bots.Length==1000','state.bots.Length==10000').replace('1000 zero-MMR bots','10000 zero-MMR bots').replace('league.EntityCount==1001','league.EntityCount==10001').replace('1001 identities','10001 identities').replace('SetHumanMmr(120000)','SetHumanMmr(12000)').replace('human.currentMmr==100000','human.currentMmr==10000').replace('human.peakMmr==120000','human.peakMmr==12000').replace('>100k','>10k').replace('all 1000 ratings','all 10000 compact ratings').replace('Background native JSON commits','Background native binary journal commits')
    edit('Assets/AnimeHillRacing/Scripts/Racing3D/Diagnostics/RacingProductProbe.cs',product)
    edit('Assets/AnimeHillRacing/Scripts/Editor/RacingProductContracts.cs',lambda t:t.replace('badge.Configure(120000)','badge.Configure(12000)').replace('.bots.Length==1000','.bots.Length==10000').replace('Unity JsonUtility journals and reloads every persistent bot profile','Unity metadata and binary journals reload every compact persistent bot'))
    def assets(t):
        t=t.replace("'LocalLeagueState',", "'CompactLeagueBot', 'LeagueRankIndex', 'LegacyLeagueMigration', 'LocalLeagueState',")
        t=t.replace("'RacingRankBadge',", "'RacingLoadoutIcon', 'RacingRankBadge',")
        t=t.replace("'RacingItemVisualSet'", "'RacingLoadoutVisuals', 'RacingItemVisualSet'")
        t=t.replace('ratingScale: 12000','population: 10000\\n    ratingScale: 1100').replace('establishedK: 1800','establishedK: 50').replace('uncertaintyK: 9000','uncertaintyK: 120')
        t=t.replace('[(0, 0), (10000, 9500), (30000, 26500), (50000, 41000), (75000, 58000), (90000, 69000), (100000, 76000)]','[(0, 0), (1000, 980), (2500, 2350), (5000, 4500), (7500, 6500), (9000, 8200), (9500, 9200), (10000, 9800)]')
        return t.replace("'beyond-apex','Beyond Apex',2,5,100000", "'beyond-apex','Beyond Apex',2,5,10000")
    edit('Tools/ML/prepare_product_assets.py',assets)
    fixture=ROOT/'tmp/product-integration/item-feature-parity.json'
    value=json.loads(fixture.read_text(encoding='utf-8-sig'))
    from racing_dataset import feature_vector
    for case in value['cases']:
        case['mmr']=case['mmr']//10
        case['normalized']=feature_vector(case['sample'],case['mmr']).tolist()
    fixture.write_text(json.dumps(value,indent=2,allow_nan=False),encoding='utf-8')
    print('Current fixtures moved to 10k units; archived v2 evidence preserved.')
