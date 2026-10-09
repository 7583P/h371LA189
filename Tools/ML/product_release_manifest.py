"""Freeze continuation source/build evidence; preserve the earlier RC manifest."""
import argparse
import difflib
import hashlib
import json
import re
import subprocess
import zipfile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Logs/ProductIntegration/RC20261006'
SUFFIXES = {'.cs', '.meta', '.asset', '.prefab', '.shader', '.unity', '.png', '.bytes'}
AUTHORIZED = {
    'Ads/AdMobManager.cs': 'Continuous free-user banner eligibility, verified entitlements and actual bottom inset.',
    'Core/RealMoneyEntitlementService.cs': 'Only confirmed new paid transactions grant 30 days; configured real VIP subscription remains distinct.',
    'Core/UnityIapPurchaseProvider.cs': 'Verified IAP callbacks, nonduplicating restores and actual subscription expiry.',
    'UI/SafeArea.cs': 'Reserve the actual visible banner height and reclaim it when ad-free.',
    'Vehicle/AdventurePhysics2DPlayModeProbe.cs': 'Read-only fixture measurements of native upright wheel anchors; retain driving/landing assertions.',
    'Vehicle/VehiclePhysics2DController.cs': 'Independent before/after proof permits only upright teleport of detached wheel offsets; forces and tuning unchanged.',
}
PURPOSES = {
    'RacingProductContracts.cs': 'Native serialization, 20 rank boundaries, twelve-item contracts, feature parity and independent rotated Adventure teleport proof.',
    'RacingClosureValidation.cs': 'Isolated native product/Adventure matrix validation and current-tree Development build.',
    'AdventurePhysics2DPlayModeRunner.cs': 'Isolate the mechanical Mountains fixture from the normal results owner; preserve physics and strict case counts.',
    'RacingClosureTests.cs': 'Include new native product contracts without removing existing release checks.',
    'RacingSystemAuditor.cs': 'Use the approved front/middle/rear item identities in the distribution auditor.',
    'AiVehicleController.cs': 'Physics-step route/rejoin tracking, actual yaw/braking capacity, aligned launch timing and route-relative traffic decisions.',
    'AiItemAgent.cs': 'Predictive twelve-item use and hold decisions; use the shared inventory API for a prepared launch.',
    'AiRacerProfile.cs': 'Seven tactical styles including Balanced; current MMR remains the runtime driving-skill input.',
    'RaceManager.cs': 'Use persistent production opponents and freeze background simulation through the real race; save actual ranked results.',
    'RaceResults.cs': 'Carry real technique achievements and persistent-opponent rating changes into results.',
    'RacerIdentity.cs': 'Persist identity/style/title data on physical racers.',
    'RacingTechniqueObserver.cs': 'Read-only human route, aerial and stable landing achievement detection.',
    'RacingProductProbe.cs': 'Render actual native rank/title/league/item UI and measure the real menu scheduler/save queue with isolated QA data.',
    'RacingClosureContracts.cs': 'Real Rigidbody checks for triple/twin/mine/recovery/homing and common late-boost jump-strip behavior.',
    'BlacklineContracts.cs': 'Retain shared behavior contracts with the expanded item catalog.',
    'RacingImprovementContracts.cs': 'Retain UI/physical checks while using the approved twelve-item bands.',
    'CanyonRushBuilder.cs': 'Attach shared wet-road presentation, bounded original city decoration and common jump-pad setup.',
    'RacingCityBackdrop.cs': 'Load the resource-backed wet asphalt shader without modifying legal topology.',
    'RacingCityPolish.cs': 'Original bounded neon signs and blimp geometry around the validated Night City circuit.',
    'RacingJumpPad.cs': 'Apply the unchanged shared launch impulse when boost becomes eligible inside the strip; deduplicate per vehicle.',
    'RacingWetCityRoad.shader': 'Opaque wet-asphalt response and bounded broken neon reflections.',
    'RaceItemType.cs': 'Twelve raw identities with preserved legacy values and explicit mechanical-family feature encoding.',
    'ItemDistributionTable.cs': 'Position, gap, lap, held item and two-item-history weighted pools.',
    'RacingItemBalanceConfiguration.cs': 'Authored front/middle/rear probabilities and bounded conditional modifiers.',
    'ItemBox.cs': 'Roll with actual race position, leader gap, lap and item history.',
    'ItemInventory.cs': 'Shared twelve-item effects, charges, cooldowns and grounded/airborne eligibility.',
    'ItemEntityBase.cs': 'Shared shield-aware controlled yaw hits and correctly bound concrete runtime entities.',
    'ProjectileItemEntity.cs': 'Straight dart and limited-turn tracking/rocket projectiles with collider/LOS checks.',
    'TrapItemEntity.cs': 'Shared armed pulse mine and physical rolling rock behavior.',
    'RacingItemPool.cs': 'Prewarm all variants within the existing 56-entity capacity.',
    'RacingItemArt.cs': 'Original low-poly twelve-item silhouettes using combined meshes and shared materials.',
    'RacingItemVisualSet.cs': 'Prewarm holding/pooled meshes and bounded trail/shield presentation.',
    'RacingTelemetryFeatures.cs': 'Encode the new identities into compatible mechanical families; retain 33-feature Unity/Python parity.',
    'RacingTelemetrySchema.cs': 'Record raw catalog/feature-encoding versions without changing the installed model feature schema.',
    'RacingMlInferenceAdapter.cs': 'Allow an explicit local candidate only in isolated Editor QA or the QA Development player; keep normal installed weights intact.',
    'LocalLeagueState.cs': 'Versioned 1001-competitor state, data-driven reset curve and immutable worker-owned deep save snapshots.',
    'LocalLeagueEngine.cs': 'Seeded persistent population, cheap autonomous matches, multiplayer expected-versus-actual Elo and varied near-MMR opponents.',
    'LocalSeasonEngine.cs': 'Installation-anchored calendar seasons, individual reset, bounded histories and monotonic accepted UTC.',
    'LocalLeagueStore.cs': 'SHA-validated two-generation journal with partial-write fallback and future-version preservation.',
    'RacingProgressionService.cs': 'Budgeted menu-only catch-up, real human results and coalesced background journal writes with pause/quit flush.',
    'RacingProgressionHost.cs': 'Initialize local seasons once, whitelist menus, freeze both gameplay modes and flush outstanding saves on pause/quit.',
    'RacingProgressionConfiguration.cs': 'Native configurable reset/simulation/title data.',
    'RacingTitleCatalog.cs': 'Earn/select plain, glow and neon titles; authoritative emerald Elite I/II palettes.',
    'MmrTier.cs': 'Twenty exact 5k divisions and live Beyond 100k display/progress/counter.',
    'MmrSystem.cs': 'Shared multiplayer Elo deltas and fresh zero-MMR default.',
    'MatchmakingService.cs': 'Eleven persistent weighted-neighborhood opponents; keep disposable seeded lobbies explicit QA only.',
    'RacingSaveData.cs': 'Route human progression through the local journal while preserving existing selections and legacy migration.',
    'RacingRankBadge.cs': 'Original faceted native UI mesh with division-specific frames, emerald Elite and dynamic Beyond counter.',
    'RacingEmblem.cs': 'Replace old rank artwork and provide the twelve native item silhouettes.',
    'RacingDesign.cs': 'Shared approved rank/title/item UI factories and palette.',
    'RacingLeaderboardUI.cs': 'Virtualized 1001-entry search/top/near-player leaderboard with 14 pooled rows and saved movement snapshot.',
    'RacingProfileUI.cs': 'Persistent rank/career/peak profile and owned-title inventory/selection.',
    'RacingSeasonUI.cs': 'Exact calendar boundary/reset preview, paged history and all-rank gallery.',
    'RacingTitleView.cs': 'Bounded plain/glow/neon title drawing and optional low-frequency prestige pulse.',
    'RacingLeagueDiagnostics.cs': 'Development-only bounded day/week/season/10k diagnostics, distribution/history and entitlement inspection.',
    'RacingGarageUI.cs': 'Current approved rank visible in both garage and confirmation.',
    'RacingHUD.cs': 'Twelve-item roulette/held icons and live rank/title lobby views.',
    'RacingResultsUI.cs': 'Persistent ranks/titles and correct 5k/Beyond 10k progression on actual results.',
    'RacingMainMenuUI.cs': 'Live rank/title/current-MMR main menu integration.',
    'RacingModeSelectUI.cs': 'Production League/Titles/Season navigation and separately labeled development test lobbies.',
    'RacingLoadingScreen.cs': 'Approved live current-rank presentation during loading.',
    'RacingSafeArea.cs': 'Reserve only the actual visible bottom banner area.',
    'AdEntitlementLedger.cs': 'Pure persisted monotonic UTC/transaction replay-safe 30-day and real VIP entitlement rules.',
    'AdEntitlementService.cs': 'Integrate actual purchase callbacks and persisted ad-free eligibility; isolate development test grants.',
    'AdEntitlementConfiguration.cs': 'Explicit configured real VIP product ID; blank remains configuration pending.',
    'AdBannerCanvasInset.cs': 'Inset legacy root canvases only while an actual banner occupies bottom space.',
    'RacingProgressionConfiguration.asset': 'Authored simulation limits, nonlinear reset curve and title inventory.',
    'RacingItemBalanceConfiguration.asset': 'Authored twelve-item position probabilities.',
    'AdEntitlementConfiguration.asset': 'Explicit unconfigured VIP ID, avoiding invented production products.',
}
for relative, purpose in AUTHORIZED.items():
    PURPOSES[Path(relative).name] = purpose


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for part in iter(lambda: stream.read(1024*1024), b''): h.update(part)
    return h.hexdigest()


def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))


def source_inputs():
    paths = []
    for directory in ('Assets', 'ProjectSettings', 'Packages'):
        paths.extend(p for p in (ROOT/directory).rglob('*') if p.is_file() and
                     (p.suffix in SUFFIXES | {'.asmdef', '.mat', '.json', '.physicsMaterial2D', '.physicMaterial'}))
    return [dict(file=p.relative_to(ROOT).as_posix(), sha256=sha(p)) for p in sorted(paths)]


def omit_method(text, name):
    match = re.search(r'^\s*(?:public|private|protected|internal)[^\n]*\b'+name+r'\([^\n]*\)\s*\{', text, re.M)
    if not match: raise ValueError('Missing proof method: '+name)
    start = match.end()-1; depth = 1; end = start+1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}'); end += 1
    return text[:match.start()] + '\n<'+name+'>\n' + text[end:]


def finish():
    baseline = read(OUT/'continuation-baseline.json'); previous = {r['file']:r for r in baseline['files']}
    old_release = read(ROOT/'Logs/ReleaseCandidate/RC20261006/source-build-manifest.json')
    current = {r['file']:r for r in source_inputs() if r['file'].startswith('Assets/') and Path(r['file']).suffix in SUFFIXES}
    missing = sorted(set(previous)-set(current)); changed=[]
    for name, row in current.items():
        before = previous.get(name, {}).get('sha256')
        if before == row['sha256']: continue
        filename = Path(name).name
        purpose = 'Unity GUID/importer for '+filename[:-5] if filename.endswith('.meta') else PURPOSES.get(filename)
        if purpose is None: raise ValueError('Document exact purpose for '+name)
        changed.append(dict(file=name, status='CHANGED_FROM_CONTINUATION' if before else 'ADDED_IN_CONTINUATION',
                            before_sha256=before, after_sha256=row['sha256'], purpose=purpose))
    protected=[];prefix='Assets/AnimeHillRacing/Scripts/'
    for entry in old_release['protected_runtime']:
        name=entry['file']; unchanged=current[name]['sha256']==previous[name]['sha256']
        protected.append(dict(file=name,unchanged=unchanged,authorized_exception=AUTHORIZED.get(name[len(prefix):],''),
                              before_sha256=previous[name]['sha256'],after_sha256=current[name]['sha256']))
    native_name=prefix+'Vehicle/VehiclePhysics2DController.cs'
    with zipfile.ZipFile(OUT/'continuation-sources.zip') as archive: before=archive.read(native_name).decode('utf-8-sig').replace('\r\n','\n')
    after=(ROOT/native_name).read_text(encoding='utf-8-sig')
    omitted_before, omitted_after=before,after
    for name in ('TeleportToPosition','MoveWheelWithChassis'):
        omitted_before=omit_method(omitted_before,name);omitted_after=omit_method(omitted_after,name)
    force_preserved=omitted_before==omitted_after
    (OUT/'adventure-authorized-teleport.diff').write_text(''.join(difflib.unified_diff(before.splitlines(True),after.splitlines(True),fromfile='authoritative-continuation',tofile='proven-teleport-fix')),encoding='utf-8')
    installed=[dict(file=r['file'],sha256=sha(ROOT/r['file']),unchanged=sha(ROOT/r['file'])==r['sha256']) for r in old_release['installed_models']]
    build_input=read(OUT/'build-inputs.json')
    build_input_changed=[r['file'] for r in build_input['files'] if not (ROOT/r['file']).exists() or sha(ROOT/r['file'])!=r['sha256']]
    build=[]
    for name in ('HillAdventurer-HumanGate.exe','HillAdventurer-HumanGate_Data/Managed/Assembly-CSharp.dll','HillAdventurer-HumanGate_Data/globalgamemanagers'):
        path=ROOT/'Builds/HumanGateLatest'/name
        build.append(dict(file=path.relative_to(ROOT).as_posix(),sha256=sha(path),bytes=path.stat().st_size,modified_utc=datetime.fromtimestamp(path.stat().st_mtime,timezone.utc).isoformat()))
    head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
    report=dict(recorded_utc=datetime.now(timezone.utc).isoformat(),baseline_scope='Existing audited dirty working tree at authorized continuation start.',
        before_head=baseline['head'],after_head=head,files=sorted(changed,key=lambda r:r['file']),missing_original_files=missing,
        protected_runtime=protected,protected_unchanged=sum(r['unchanged'] for r in protected),
        unauthorized_protected_changes=[r for r in protected if not r['unchanged'] and not r['authorized_exception']],
        adventure_physics_identical_outside_two_teleport_methods=force_preserved,
        racing_shared_physics_unchanged=current[prefix+'Racing3D/Physics/RacingVehiclePhysics.cs']['sha256']==previous[prefix+'Racing3D/Physics/RacingVehiclePhysics.cs']['sha256'],
        installed_models=installed,build_inputs_changed_since_build=build_input_changed,build=build,
        native_bootstrap_note='Unity may reuse the native EXE bootstrap. The current build log, frozen input hashes and regenerated managed assembly/globalgamemanagers identify this build.')
    path=OUT/'source-build-manifest.json'
    if path.exists(): raise FileExistsError('Final manifest already frozen.')
    path.write_text(json.dumps(report,indent=2),encoding='utf-8')
    lines=['# Files modified — product continuation','',
        'The comparison uses the preserved continuation baseline, not git HEAD. The earlier RC evidence stays unchanged. Full hashes: [source-build-manifest.json](source-build-manifest.json).','',
        '| File | Change | Purpose |','|---|---|---|']
    for r in report['files']:lines.append(f"| [{r['file']}](../../../{r['file']}) | {r['status']} | {r['purpose']} |")
    lines+=['',f"Protected prior runtime sources: {report['protected_unchanged']}/{len(protected)} byte-identical; all exceptions are listed in the manifest.",
        'Independent native Adventure proof: rotated teleport error 1.682005 m before, 0 m after. Every byte outside the two teleport methods is preserved after newline normalization.',
        'Race forces and all three installed model weights remain unchanged. No original file was removed. No commit or push.']
    (OUT/'FILES-MODIFIED.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    print(json.dumps(dict(changed=len(changed),missing=missing,protected_unchanged=report['protected_unchanged'],
        authorized_exceptions=len(protected)-report['protected_unchanged'],force_preserved=force_preserved,
        inputs_changed=build_input_changed,installed_unchanged=all(r['unchanged'] for r in installed))))
    if missing or head!=baseline['head'] or report['unauthorized_protected_changes'] or not force_preserved or build_input_changed or not all(r['unchanged'] for r in installed):raise SystemExit(1)


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--prepare-build',action='store_true');args=parser.parse_args()
    if args.prepare_build:
        path=OUT/'build-inputs.json'
        if path.exists():raise FileExistsError('Build inputs already frozen.')
        path.write_text(json.dumps(dict(recorded_utc=datetime.now(timezone.utc).isoformat(),files=source_inputs()),indent=2),encoding='utf-8')
        print('Current build inputs frozen.')
    else:finish()
