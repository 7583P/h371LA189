"""Freeze RC evidence against the initial dirty-tree snapshot; never edit game data."""
import argparse
import hashlib
import json
import subprocess
from datetime import datetime, timezone
from pathlib import Path


PURPOSES = {
    'MainMenu.unity': 'Repair 2,974 invalid script references and disable six duplicate EventSystems; preserve artwork and other serialized fields.',
    'AdventurePhysics2DPlayModeRunner.cs': 'Count completed versus expected cases honestly; isolate results owner only in the explicit mechanical QA fixture.',
    'BuggyMountainsInstaller.cs': 'Preserve existing additional skins, wheels, progression and Glitch transforms when refreshing canonical Buggy cosmetics; retain existing authored tuning.',
    'RacingClosureValidation.cs': 'Run real physics/contract suites and build the isolated Windows Development player with frame timing enabled.',
    'RacingClosureTests.cs': 'Integrate release/causality checks, native parity and existing CLI tests into the Unity validation entry point.',
    'RacingPerformanceBenchmarkRunner.cs': 'Replace synthetic CPU estimates with an asynchronous real Unity race benchmark.',
    'RacingSystemAuditor.cs': 'Validate continuous skill and progression contracts against the final implementation.',
    'RacingImprovementContracts.cs': 'Update the native UI fixture to the eight requested MMR presets and retain the existing runtime/UI contracts.',
    'RacingReleaseContracts.cs': 'Meaningful legality, data provenance, ML compatibility, temporal causality and parity contracts.',
    'RacingReleaseSceneAudit.cs': 'Read-only audit of all nine build scenes, missing scripts/materials, error shaders and deferred runtime setup.',
    'RacingReleaseAssetGuard.cs': 'Prevent the existing automatic Editor installer from regenerating protected Adventure assets only during explicit QA invocations; preserve normal and manual installation.',
    'RacingTrackGeometry.cs': 'Clear shortcut shoulder portals and support deck joins while preserving shared road geometry ownership.',
    'RacingCityBackdrop.cs': 'Balance ambient/night lighting and skyline presentation; extract runtime components into correct MonoScript files.',
    'NightCityCircuit.cs': 'Name the three sectors, keep exactly two wide primary routes and correct shared route approach/exit setup.',
    'CanyonRushBuilder.cs': 'Improve asphalt readability, shared launch direction and native environment component binding.',
    'RacingVehiclePhysics.cs': 'Expose command provenance, actual jump time and observable events; preserve all 20 force/torque calls and five force/stat methods.',
    'PlayerRacingInput.cs': 'Preserve keyboard commands and support bounded joystick pitch/air-roll input and release.',
    'RacingVfxPool.cs': 'Keep pooled mechanic-specific effects and move vehicle VFX to a correctly named runtime script.',
    'RacingVehicleVisuals.cs': 'Tune DAPEX material response, bind visual components correctly and add per-car brake lamp feedback without changing shared materials or physics.',
    'RacingMetaVisuals.cs': 'Reduce Whity overexposure; expose occluded DAPEX fascia/rear light ribbon and scale the centered rear badge.',
    'RacingVehicleAudio.cs': 'Move the existing vehicle audio component to its correct MonoScript file; remove an unused field.',
    'RacingRaceMetrics.cs': 'Measure real frame distribution, 1% low, profiler AI/physics/render/GC/draw counters and causal ML tick costs.',
    'RacingPositionAuditTests.cs': 'Exercise sequential checkpoint, finish, recovery and position invariants.',
    'RacingClosureProbe.cs': 'Execute real native callbacks/races, equal-build MMR matrix, protected provenance and strict finish/recovery checks.',
    'RacingClosureContracts.cs': 'Exercise item/drift runtime behavior and release checks in real Unity Play Mode.',
    'NightCityEvidenceProbe.cs': 'Capture native DAPEX/Whity/game/menu evidence and actual Adventure callbacks, physics and normal defeat/results flow.',
    'RacingRenderedPlayerProbe.cs': 'Explicit QA-only full camera/HUD GPU rendering to a 1600x900 render texture, with native render proof.',
    'RacingTelemetrySchema.cs': 'Version telemetry schema, 33 normalized context features, eight command outputs, source and measured lap fields.',
    'RacingTelemetryFeatures.cs': 'Shared causal context feature construction with Unity/Python numerical parity.',
    'RacingTelemetryRecorder.cs': 'Record actual command intent, repeated flight evidence, measured laps, source provenance and explicit QA destinations.',
    'RacingMlInferenceAdapter.cs': 'Maintain ML01 compatibility, strict ML02 loading, staggered Shadow inference, causal comparisons and full tick timing.',
    'MovementTechDetector.cs': 'Detect repeated legal movement-tech chains and stable FNV IDs compatible with the offline extractor.',
    'IRacingTelemetrySink.cs': 'Preserve local telemetry foundation and allow explicit isolated QA recording destinations.',
    'DeterministicLegalityFilter.cs': 'Validate finite bounded states, temporal continuity, impulse/recovery events and sequential progress.',
    'CandidateMetaRoute.cs': 'Keep candidate route metadata, legality, provenance and evidence gates explicit.',
    'RacingShowroom.cs': 'Preserve native garage previews and correctly bind preview/input helper components.',
    'RacingModeSelectUI.cs': 'Use development-only 1k, 10k, 25k, 50k, 75k, 90k, 97k and 100k test presets backed by actual races.',
    'RacingMobileControls.cs': 'Keep compact left joystick/right actions and release ownership; extract touch components into correct script files.',
    'RacingMainMenuUI.cs': 'Refresh real coins, gems, MMR and tier while preserving the original Adventure/Ranked cover art.',
    'RacingLoadingScreen.cs': 'Keep native loading visuals and correct the file binding of its car graphic.',
    'RacingHUD.cs': 'Keep readable HUD/minimap and correct its runtime graphic binding.',
    'RacingDesign.cs': 'Guarantee one active EventSystem and correct reusable UI/graphic MonoScript bindings.',
    'RacingDebugOverlay.cs': 'Development-only actual skill/style/route/action queue, ML mode/version/proposals and uncalibrated probabilities.',
    'RaceRules.cs': 'Explicit equal-build benchmark configuration with the requested rating and unchanged shared physical limits.',
    'RaceManager.cs': 'Keep normal 1 human + 11 AI spawning, equivalent build stats and honest finish/DNF result behavior.',
    'ItemInventory.cs': 'Preserve shared items and expose actual item-hit/collision observations for telemetry.',
    'TrackProgressTracker.cs': 'Require sequential checkpoint visits; preserve progress/rank across legal shortcut and recovery paths.',
    'RacingCamera3D.cs': 'Bound lookahead and viewport placement so the complete player vehicle remains readable; preserve collision avoidance.',
    'RacingFeedback.cs': 'Preserve feedback behavior while extracting runtime audio into its correctly named file.',
    'ShortcutKnowledgeBase.cs': 'Separate route knowledge from execution accuracy and risk, keeping exactly two authored routes.',
    'AiVehicleController.cs': 'Refresh local steering every physics step, correct braking/aerial signs and timing, retain safe committed rejoin and real bounded action queues.',
    'AiSkillModel.cs': 'Continuous 1k-100k decision skill, imperfect ceiling and independent knowledge/execution/tech capability.',
    'AiRacerProfile.cs': 'Continuous perception/planning and consistent seeded tactical style parameters.',
    'AiItemAgent.cs': 'Predict relative target motion, curve/wall firing windows and tactical risk without hidden item forces.',
    'RacingTrafficFlow.cs': 'Correct MonoScript binding and wrap decorative traffic vehicles individually.',
    'RacingEnvironmentMotion.cs': 'Move existing city activity to its correct MonoScript file.',
    'RacingBillboardPulse.cs': 'Move existing emissive signage animation to its correct MonoScript file.',
    'RacingGeneratedMaterial.cs': 'Correct runtime generated-material ownership and MonoScript binding.',
    'RacingGeneratedMesh.cs': 'Correct runtime generated-mesh ownership and MonoScript binding.',
    'RacingAnimatedPart.cs': 'Correct animated-part marker MonoScript binding without changing behavior.',
    'RacingVehicleVfx.cs': 'Correct vehicle effect component MonoScript binding; retain pooled mechanic-specific bursts.',
    'RacingButtonMotion.cs': 'Correct reusable button animation MonoScript binding.',
    'RacingCircuitTrimGraphic.cs': 'Correct native vector circuit graphic MonoScript binding.',
    'RacingCoverInteractionMotion.cs': 'Correct cover interaction animation MonoScript binding.',
    'RacingEmblem.cs': 'Correct vector emblem MonoScript binding.',
    'RacingHoldControl.cs': 'Correct held-touch control binding and pointer release ownership.',
    'RacingLoadingCarGraphic.cs': 'Correct native loading car graphic MonoScript binding.',
    'RacingMobileTouchButton.cs': 'Correct touch action binding and ownership on release/disable.',
    'RacingPanelGraphic.cs': 'Correct native panel graphic MonoScript binding.',
    'RacingRaceMap.cs': 'Correct native HUD minimap binding and keep the road/shortcut mesh static instead of rebuilding it at 30 Hz.',
    'RacingRaceMapMarkers.cs': 'Update only live racer markers at 30 Hz without allocating per-marker arrays; preserve marker shapes and shared projection.',
    'RacingSafeArea.cs': 'Correct safe-area MonoScript binding.',
    'RacingScreenMotion.cs': 'Correct screen animation MonoScript binding.',
    'RacingVirtualJoystick.cs': 'Correct compact steering/pitch/roll joystick binding and pointer release.',
    'racing_dataset.py': 'Strict raw telemetry validation, trajectory deduplication, causal next-command windows and quality weighting independent of MMR.',
    'meta_tech.py': 'Stable repeated-chain clusters, matched legal baselines and bootstrap/sampling-resolution promotion gates.',
    'release_pipeline.py': 'Local offline MLP/GRU/attention training, disjoint seed splits, held-out persistence reference, ONNX/native exports and diverse parity cases.',
    'test_release_pipeline.py': '32 meaningful Python tests for integrity, causal labels/splits, chains, meta gates and representative parity selection.',
    'release_audit_report.py': 'Aggregate verified real race laps/sectors/tech, human provenance gate, curve CSV/plots and baseline source preservation.',
    'release_performance_report.py': 'Aggregate and plot actual full-camera/HUD Windows frame samples, CPU/ML/GC/draw counters and strict race failures across baseline, Off and Shadow.',
    'release_evidence_manifest.py': 'Freeze final source/build/model hashes and provide exact baseline-relative file purposes without resetting the dirty tree.',
    'release_evidence_verify.py': 'Verify final source/build/save/test/evidence integrity and document links, then freeze raw artifact hashes without asserting release acceptance.',
    'train_behavior.py': 'Preserve existing MLP foundation and reject unverified/incompatible telemetry instead of trusting smoke data.',
    'train_temporal.py': 'Preserve temporal GRU foundation and use validated compatible command demonstrations.',
    'evaluate_model.py': 'Independently evaluate candidate checkpoints against their original held-out split and fingerprints.',
    'export_onnx.py': 'Preserve export/parity tooling and causal/versioned candidate compatibility.',
    'export_unity_weights.py': 'Preserve legacy ML01 export compatibility.',
    'inspect_dataset.py': 'Make dataset quality/provenance inspection explicit.',
    'validate_telemetry.py': 'Validate raw integrity/provenance and export per-trajectory rejection evidence.',
    'RELEASE_PIPELINE.md': 'Document local causal training, Shadow-only candidates, meta evidence gates and reproducible commands.',
}


def digest(path):
    result=hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda:stream.read(1024*1024),b''):result.update(chunk)
    return result.hexdigest()


def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))


def run(root,output):
    root=Path(root).resolve();output=root/output;output.mkdir(parents=True,exist_ok=True)
    baseline=root/'tmp/release-candidate-20261005/baseline'
    original={Path(row['Path']).as_posix().replace('\\','/'):row['Hash'].lower() for row in read(baseline/'source-hashes.json')}
    changed=[];missing=[];protected=[]
    def append(path,before,status):
        name=path.relative_to(root).as_posix()
        purpose=PURPOSES.get(path.name)
        if not purpose:raise ValueError(f'Exact file purpose missing: {name}')
        changed.append(dict(file=name,status=status,before_sha256=before,after_sha256=digest(path),purpose=purpose))
    for name,before in original.items():
        path=root/name
        if not path.exists():missing.append(name);continue
        after=digest(path)
        is_protected=name.startswith('Assets/AnimeHillRacing/Scripts/') and '/Racing3D/' not in name and '/Editor/' not in name
        if is_protected:protected.append(dict(file=name,unchanged=before==after))
        if before!=after:append(path,before,'CHANGED_FROM_INITIAL_DIRTY_TREE')
    script_root=root/'Assets/AnimeHillRacing/Scripts'
    for path in sorted(script_root.rglob('*.cs')):
        if path.relative_to(root).as_posix() not in original:
            append(path,None,'ADDED_TO_SOURCE_SNAPSHOT')
            meta=path.with_suffix(path.suffix+'.meta')
            if meta.exists():
                changed.append(dict(file=meta.relative_to(root).as_posix(),status='NEW_SCRIPT_MONOSCRIPT_GUID',before_sha256=None,
                    after_sha256=digest(meta),purpose='Unity MonoScript GUID/importer for '+path.name+'.'))
    ml_root=root/'Tools/ML'
    for path in sorted(list(ml_root.glob('*.py'))+list(ml_root.glob('*.md'))):
        previous=baseline/path.name
        before=digest(previous) if previous.exists() else None
        if before is None or before!=digest(path):append(path,before,'TOOL_CHANGED' if before else 'AUDIT_PIPELINE_TOOL')
    installed=[]
    for name in ('behavior_baseline.pt','temporal_model.pt'):
        path=ml_root/'Models'/name;before=digest(baseline/'models'/name)
        installed.append(dict(file=path.relative_to(root).as_posix(),sha256=digest(path),unchanged=digest(path)==before))
    native=root/'Assets/AnimeHillRacing/Resources/RacingMlWeights.bytes'
    native_before=read(root/'tmp/release-candidate-20261005/native-parity.json')['weights_sha256']
    installed.append(dict(file=native.relative_to(root).as_posix(),sha256=digest(native),unchanged=digest(native)==native_before))
    build_root=root/'Builds/HumanGateLatest'
    build=[]
    for name in ('HillAdventurer-HumanGate.exe','HillAdventurer-HumanGate_Data/Managed/Assembly-CSharp.dll','HillAdventurer-HumanGate_Data/globalgamemanagers'):
        path=build_root/name
        build.append(dict(file=path.relative_to(root).as_posix(),sha256=digest(path),bytes=path.stat().st_size,
            modified_utc=datetime.fromtimestamp(path.stat().st_mtime,timezone.utc).isoformat()))
    generated=[]
    for name in (
        'tmp/editor-check/obj/Debug/EditorCheck.AssemblyInfo.cs',
        'tmp/editor-check/obj/Debug/EditorCheck.AssemblyInfoInputs.cache',
        'tmp/racing-test/obj/Debug/net10.0/RacingTest.AssemblyInfo.cs',
        'tmp/racing-test/obj/Debug/net10.0/RacingTest.AssemblyInfoInputs.cache',
        'tmp/racing-test/obj/Debug/net10.0/RacingTest.csproj.AssemblyReference.cache',
    ):
        path=root/name
        if path.exists():generated.append(dict(file=name,sha256=digest(path),
            purpose='Incidental generated assembly metadata/cache from existing compilation checks; not game source.'))
    support=[]
    for path in sorted((root/'tmp/release-candidate-20261005').glob('*')):
        if path.is_file() and path.suffix in ('.py','.ps1'):
            purpose='Retained local audit, diagnosis or repair helper; not loaded by the game or installed training runtime.'
            if path.name=='Run-ReleaseValidation.ps1':purpose='Run sequential isolated Unity validation, preserve all 442 original PlayerPrefs bytes and retain every run under a unique label.'
            if path.name=='restore_qa_importer_assets.py':purpose='Restore only two unintended automatic-importer changes after proving their bytes match the initial dirty-tree hashes.'
            support.append(dict(file=path.relative_to(root).as_posix(),sha256=digest(path),purpose=purpose))
    manifest=dict(recorded_utc=datetime.now(timezone.utc).isoformat(),baseline_scope='Initial authoritative dirty working tree, not git HEAD.',
        initial_head=(baseline/'head.txt').read_text(encoding='utf-8-sig').strip(),
        final_head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),
        files=sorted(changed,key=lambda row:row['file']),missing_original_sources=missing,
        protected_runtime=protected,protected_runtime_unchanged=all(row['unchanged'] for row in protected),
        installed_models=installed,installed_models_unchanged=all(row['unchanged'] for row in installed),
        build=build,build_fidelity_note='Unity may reuse the unchanged native EXE bootstrap. The regenerated Assembly-CSharp.dll and globalgamemanagers identify this compiled game code; EXE timestamp alone is insufficient.',
        generated_check_outputs=generated,audit_support_files=support,
        evidence_scope='Candidate models, benchmark telemetry, logs and native images are additional artifacts linked in AUDIT.md and the evidence index; they are not installed production models.')
    (output/'source-build-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    lines=['# Files modified — RC20261006','',
        'Comparison uses the initial dirty working tree. Existing unrelated modifications remain intact. Full hashes: [source-build-manifest.json](source-build-manifest.json).','',
        '| Exact file | Status | Purpose |','|---|---|---|']
    for row in manifest['files']:
        path=Path('../../../')/row['file']
        lines.append(f"| [{row['file']}]({path.as_posix()}) | {row['status']} | {row['purpose']} |")
    lines+=['','Additional generated check outputs and retained audit helpers:','',
        '| Exact file | Purpose |','|---|---|']
    for row in generated+support:
        path=Path('../../../')/row['file']
        lines.append(f"| [{row['file']}]({path.as_posix()}) | {row['purpose']} |")
    lines+=['','The Buggy definition and prefab were restored to their exact initial dirty-tree bytes; see the QA importer restoration proof.',
        'No protected Adventure runtime source changed. No missing original source. Existing installed Torch checkpoints and ML01 native weights were compared with the baseline hashes.']
    (output/'FILES-MODIFIED.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    print(json.dumps(dict(files=len(changed),protected=len(protected),protected_unchanged=manifest['protected_runtime_unchanged'],
        models_unchanged=manifest['installed_models_unchanged'],missing=missing)))
    if missing or not manifest['protected_runtime_unchanged'] or not manifest['installed_models_unchanged']:raise SystemExit(1)


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--root',default='.');parser.add_argument('--output',default='Logs/ReleaseCandidate/RC20261006')
    args=parser.parse_args();run(Path(args.root),Path(args.output))
