"""Create deterministic metadata and the authored local progression configuration."""
from pathlib import Path
import uuid

ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / 'Assets/AnimeHillRacing/Scripts'
FILES = {
    'Racing3D/MMR': ['CompactLeagueBot', 'LeagueRankIndex', 'LegacyLeagueMigration', 'LocalLeagueState', 'LocalSeasonEngine', 'LocalLeagueEngine', 'LocalLeagueStore', 'RacingTitleCatalog', 'RacingProgressionConfiguration', 'RacingProgressionService', 'RacingProgressionHost'],
    'Racing3D/UI': ['RacingLoadoutIcon', 'RacingRankBadge', 'RacingTitleView', 'RacingLeaderboardUI', 'RacingProfileUI', 'RacingSeasonUI', 'RacingLeagueDiagnostics'],
    'Editor': ['RacingProductContracts'],
    'Racing3D/Environment': ['RacingCityPolish'],
    'Racing3D/Diagnostics': ['RacingProductProbe'],
    'Racing3D/Core': ['RacingTechniqueObserver'],
    'Racing3D/Items': ['RacingItemBalanceConfiguration', 'ProjectileItemEntity', 'TrapItemEntity'],
    'Racing3D/Presentation': ['RacingLoadoutVisuals', 'RacingItemVisualSet'],
    'Ads': ['AdEntitlementLedger', 'AdEntitlementConfiguration', 'AdEntitlementService', 'AdBannerCanvasInset']
}


def guid(relative):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'hill-adventurer/product-integration/' + relative).hex


def main():
    for directory, names in FILES.items():
        for name in names:
            path = SCRIPTS / directory / (name + '.cs')
            if not path.exists():
                continue
            meta = path.with_suffix('.cs.meta')
            if not meta.exists():
                meta.write_text('fileFormatVersion: 2\nguid: ' + guid(path.relative_to(ROOT).as_posix()) + '\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')
    resources = ROOT / 'Assets/AnimeHillRacing/Resources'
    def asset(name, script, fields):
        path = resources / (name + '.asset')
        if path.exists():
            return
        script_guid = guid((SCRIPTS / script).relative_to(ROOT).as_posix())
        value = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: ' + script_guid + ', type: 3}\n  m_Name: ' + name + '\n  m_EditorClassIdentifier: \n' + fields
        path.write_text(value, encoding='utf-8')
        path.with_suffix('.asset.meta').write_text('fileFormatVersion: 2\nguid: ' + guid(path.relative_to(ROOT).as_posix()) + '\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')
    fields = '  league:\n    historyLimit: 20\n    eventsPerHour: 120\n    maximumCatchupEvents: 8192\n    menuBudgetMilliseconds: 1.5\n    population: 10000\n    ratingScale: 1100\n    establishedK: 50\n    uncertaintyK: 120\n    resetCurve:\n'
    for ending, starting in [(0, 0), (1000, 980), (2500, 2350), (5000, 4500), (7500, 6500), (9000, 8200), (9500, 9200), (10000, 10000)]:
        fields += f'    - endingMmr: {ending}\n      startingMmr: {starting}\n'
    fields += '  titles:\n'
    titles = [('current-season-rank','CURRENT SEASON RANK',1,0,0,(.7,.8,.9)), ('night-city-veteran','Night City Veteran',0,1,50,(.75,.83,.88)), ('shortcut-hunter','Shortcut Hunter',2,2,20,(1,.73,.24)), ('aerial-specialist','Aerial Specialist',2,3,30,(.76,.39,1)), ('landing-master','Landing Master',1,4,50,(.2,.83,1)), ('beyond-apex','Beyond Apex',2,5,10000,(1,.86,.56))]
    for ident, display, style, requirement, threshold, color in titles:
        fields += f'  - id: {ident}\n    displayName: {display}\n    style: {style}\n    primaryColor: {{r: {color[0]}, g: {color[1]}, b: {color[2]}, a: 1}}\n    secondaryColor: {{r: 0.55, g: 0.32, b: 1, a: 1}}\n    emissionIntensity: {0.65 if style == 2 else 0.15}\n    animationType: {1 if ident == "beyond-apex" else 0}\n    unlockRequirement: {requirement}\n    threshold: {threshold}\n'
    asset('RacingProgressionConfiguration', 'Racing3D/MMR/RacingProgressionConfiguration.cs', fields)
    asset('AdEntitlementConfiguration', 'Ads/AdEntitlementConfiguration.cs', '  vipSubscriptionProductId: \n')
    fields = ''
    for name, entries in [('front',[(1,32),(3,27),(2,23),(7,18)]),('middle',[(8,32),(9,22),(10,25),(4,21)]),('rear',[(6,38),(5,20),(11,25),(12,17)])]:
        fields += f'  {name}:\n'
        for item, weight in entries:
            fields += f'  - Type: {item}\n    Weight: {weight}\n'
    asset('RacingItemBalanceConfiguration','Racing3D/Items/RacingItemBalanceConfiguration.cs',fields)
    print('Authored progression assets and new script metadata prepared; existing assets preserved.')


if __name__ == '__main__':
    main()
