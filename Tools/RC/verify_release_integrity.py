"""Recheck preserved source, scenes, models, artifacts and production save files."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[2]


def sha(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("evidence", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    evidence = args.evidence.resolve()
    destination = args.output.resolve()
    if not evidence.is_relative_to(ROOT / "Docs" / "ReleaseGates") or not destination.is_relative_to(evidence):
        raise ValueError("Use a fresh manifest inside the selected release evidence directory")
    if destination.exists():
        raise FileExistsError("Preserve previous integrity evidence")
    original = read(args.evidence / "baseline-inventory.json")
    environment = read(args.evidence / "environment.json")
    snapshot = ROOT / original["snapshotRoot"]
    changed = []
    for row in original["sources"]:
        path = ROOT / row["path"]
        actual = sha(path) if path.exists() else None
        if actual != row["sha256"]:
            before = snapshot / row["path"]
            normalized_equal = path.exists() and before.exists() and path.read_bytes().replace(b"\r\n", b"\n") == before.read_bytes().replace(b"\r\n", b"\n")
            changed.append({"path": row["path"], "beforeSha256": row["sha256"], "currentSha256": actual, "lineEndingsOnly": normalized_equal})
    scenes = [{"path": row["path"], "unchanged": (ROOT / row["path"]).exists() and sha(ROOT / row["path"]) == row["sha256"]} for row in original["scenesAndPrefabs"]]
    artifacts = [{"path": row["path"], "unchanged": (ROOT / row["path"]).exists() and sha(ROOT / row["path"]) == row["sha256"]} for row in original["previousArtifacts"]]
    saves = []
    for row in environment["productionSaveFiles"]:
        path = Path(environment["productionSaveRoot"]) / row["path"]
        saves.append({"path": row["path"], "unchanged": path.exists() and sha(path) == row["sha256"].lower()})
    allowed = {
        "Assets/AnimeHillRacing/Scripts/Racing3D/Environment/CanyonRushBuilder.cs",
        "Assets/AnimeHillRacing/Scripts/Racing3D/Presentation/RacingVehicleVisuals.cs",
        "Assets/AnimeHillRacing/Scripts/Racing3D/Diagnostics/RacingClosureProbe.cs",
        "Assets/AnimeHillRacing/Scripts/Racing3D/UI/RacingLeaderboardUI.cs",
        "Assets/AnimeHillRacing/Scripts/Racing3D/UI/RacingResultsUI.cs",
        "Assets/AnimeHillRacing/Scripts/Racing3D/MMR/RacingProgressionService.cs",
        "Assets/AnimeHillRacing/Scripts/Racing3D/Core/RaceManager.cs",
        "Assets/AnimeHillRacing/Scripts/Racing3D/Diagnostics/RacingRaceMetrics.cs",
        "Assets/AnimeHillRacing/Scripts/Editor/RacingRcEvidenceBuilder.cs",
        "Tools/CompetitiveQA/Program.cs", "Tools/RC/Open-ReleaseHumanGate.ps1",
        "Tools/CompetitiveQA/Json.cs",
        "Assets/AnimeHillRacing/Scripts/Editor/RacingCompetitiveDisplayContracts.cs",
        "Tools/RC/Run-ReleaseNativeReadback.ps1",
        "Tools/RC/analyze_performance.py",
        "Tools/RC/compare_performance.py",
        "Tools/RC/export_release_logs.py",
        "Tools/RC/verify_release_integrity.py",
    }
    protected_changes = [row for row in changed if not row["lineEndingsOnly"] and row["path"] not in allowed]
    keystores = [{"path": row["path"], "unchanged": Path(row["path"]).is_file() and sha(Path(row["path"])) == row["sha256"].lower()}
                for row in environment.get("keystores", [])]
    current_saves = {path.relative_to(environment["productionSaveRoot"]).as_posix()
                     for path in Path(environment["productionSaveRoot"]).rglob("*") if path.is_file()}
    initial_saves = {row["path"].replace("\\", "/") for row in environment["productionSaveFiles"]}
    registry = {"baselineAvailable": False, "unchanged": None}
    baseline_registry = environment.get("productionRegistry", {})
    if baseline_registry.get("available"):
        before = (ROOT / baseline_registry["snapshot"]).resolve()
        if not before.is_relative_to(ROOT / "tmp" / "ReleaseGates"):
            raise ValueError("Keep private registry snapshots outside versionable evidence")
        after = before.parent / ("production-playerprefs-after-" + uuid.uuid4().hex + ".reg")
        export = subprocess.run(["reg.exe", "export", r"HKCU\Software\DefaultCompany\Hill Adventurer", str(after)], capture_output=True)
        registry = {"baselineAvailable": True, "beforeSha256": baseline_registry["sha256"],
                    "afterSha256": sha(after) if export.returncode == 0 and after.is_file() else None,
                    "baselineSnapshotIntact": before.is_file() and sha(before) == baseline_registry["sha256"],
                    "afterPrivateSnapshot": after.relative_to(ROOT).as_posix()}
        registry["unchanged"] = registry["baselineSnapshotIntact"] and registry["afterSha256"] == registry["beforeSha256"]
    branch = subprocess.check_output(["git", "branch", "--show-current"], cwd=ROOT, text=True).strip()
    report = {"classification": "RECORDED_SOURCE_ARTIFACT_FILE_AND_REGISTRY_INTEGRITY_NOT_RELEASE_APPROVAL", "branch": branch,
              "head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
              "changedOriginalSources": changed, "unplannedSemanticChanges": protected_changes,
              "scenesAndPrefabs": scenes, "priorArtifactsAndModels": artifacts, "productionSaveFiles": saves,
              "allScenesUnchanged": all(x["unchanged"] for x in scenes), "allPriorArtifactsAndModelsUnchanged": all(x["unchanged"] for x in artifacts),
              "allInventoriedProductionSavesUnchanged": all(x["unchanged"] for x in saves),
              "productionSaveFileSetUnchanged": current_saves == initial_saves,
              "newProductionSaveFiles": sorted(current_saves - initial_saves),
              "keystores": keystores, "allInventoriedKeystoresUnchanged": all(x["unchanged"] for x in keystores),
              "nativeRegistryBaselineAvailable": registry["baselineAvailable"], "nativeProductionRegistry": registry,
              "limits": "Integrity establishes the recorded file and native registry snapshots only; it does not approve gameplay or services. Missing baseline evidence is reported, never inferred. Private registry/key contents are not exported."}
    report["preservationPassed"] = (branch == "main" and report["head"] == original["head"] and not protected_changes and
                                    report["allScenesUnchanged"] and report["allPriorArtifactsAndModelsUnchanged"] and
                                    report["allInventoriedProductionSavesUnchanged"] and report["productionSaveFileSetUnchanged"] and
                                    report["allInventoriedKeystoresUnchanged"] and registry["unchanged"] is not False)
    with args.output.open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
    print(json.dumps({k: v for k, v in report.items() if not isinstance(v, list)}, indent=2))
    print(json.dumps({"changedFiles": len(changed), "unexpectedSemanticChanges": [r["path"] for r in protected_changes], "productionSaveFiles": len(saves)}))
    return 0 if report["preservationPassed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
