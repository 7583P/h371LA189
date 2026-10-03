"""Collect final, reproducible audit evidence without altering game saves."""
import hashlib
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
AUDIT = ROOT / "tmp/racing-audit-20261003"
CLOSURE = ROOT / "Logs/RacingPhase2/closure"
LABELS = {
    "contracts": "audit-contracts-v13",
    "qa": "audit-final-qa-v13",
    "benchmark": "audit-mmr-benchmark-v13",
    "ranked": "audit-ranked-100k-v13",
    "build": "audit-build-v13",
    "profile": "audit-rendered-high-v13",
    "adventure": "audit-adventure-matrix-v15",
}


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def hash_file(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def relative(path):
    return path.relative_to(ROOT).as_posix()


def main():
    final = {"labels": LABELS, "assertions": {}, "runs": {}, "saves": {}}
    contracts = CLOSURE / LABELS["contracts"]
    files = ["foundation-tests.json", "existing-cli-tests.json", "phase2-edit-contracts.json",
             "improvement-edit-contracts.json", "item-drift-contracts.json", "improvement-ui-contracts.json"]
    for name in files:
        final["assertions"][name] = {k: read(contracts / name)[k] for k in ["passed", "failed"]}
    qa = CLOSURE / LABELS["qa"] / "seed-4207/final-qa-contracts.json"
    final["assertions"]["final-qa-contracts.json"] = {k: read(qa)[k] for k in ["passed", "failed"]}
    final["passedAssertions"] = sum(checks["passed"] for checks in final["assertions"].values())
    final["failedAssertions"] = sum(checks["failed"] for checks in final["assertions"].values())
    camera = qa.with_name("camera-observations.txt").read_text(encoding="utf-8-sig")
    final["camera"] = dict(line.split("=", 1) for line in camera.splitlines() if "=" in line)
    for key, label in LABELS.items():
        path = CLOSURE / label
        save = path / "save-restored.json"
        final["saves"][key] = read(save) if save.exists() else {"pending": True}
        suite = path / "suite.json"
        if suite.exists():
            data = read(suite)
            final["runs"][key] = {k: data[k] for k in ["passed", "errors", "warnings"]}
            final["runs"][key]["races"] = len(data["races"])
            final["runs"][key]["racePasses"] = sum(race["passed"] for race in data["races"])
    build = CLOSURE / LABELS["build"] / "build.txt"
    final["build"] = dict(line.split("=", 1) for line in build.read_text().splitlines() if "=" in line) if build.exists() else {"pending": True}
    adventure_log = CLOSURE / LABELS["adventure"] / "unity.log"
    if adventure_log.exists():
        log = adventure_log.read_text(encoding="utf-8-sig", errors="replace")
        results = {}
        for line in log.splitlines():
            if not line.startswith("ADVENTURE_BOX2D_PLAY_RESULT|"):
                continue
            data = dict(part.split("=", 1) for part in line.split("|")[1:] if "=" in part)
            results[(data.get("vehicle"), data.get("map"))] = data
        endings = re.findall(r"ADVENTURE_BOX2D_PLAY_END\|([^\r\n]+)", log)
        final["adventure"] = {
            "completed": len(results), "passed": sum(row.get("passed") == "True" for row in results.values()),
            "ending": endings[-1] if endings else None,
            "results": list(results.values()),
        }
    baseline = read(AUDIT / "baseline/adventure-hashes.json")
    unchanged, changed, missing = [], [], []
    for row in baseline:
        path = ROOT / row["Path"]
        if not path.exists():
            missing.append(row["Path"])
        elif hash_file(path) == row["Hash"]:
            unchanged.append(row["Path"])
        else:
            changed.append(relative(path))
    final["adventurePreservation"] = {"checked": len(baseline), "unchanged": len(unchanged), "changed": changed, "missing": missing}
    source_changes = []
    for group in ["Racing3D", "Editor"]:
        frozen = AUDIT / "baseline" / group
        current = ROOT / "Assets/AnimeHillRacing/Scripts" / group
        for path in sorted(frozen.rglob("*")):
            if not path.is_file():
                continue
            target = current / path.relative_to(frozen)
            if not target.exists() or hash_file(path) != hash_file(target):
                source_changes.append(relative(target))
    final["changedFromOriginalWorkingTree"] = source_changes
    final["finalRaceCount"] = sum(final["runs"].get(key, {}).get("races", 0) for key in ["benchmark", "ranked", "profile", "qa"])
    final["allEvidenceComplete"] = (
        final["passedAssertions"] == 307 and final["failedAssertions"] == 0
        and all(final["runs"].get(key, {}).get("passed", False) for key in ["contracts", "benchmark", "ranked", "profile", "qa"])
        and final["finalRaceCount"] == 32
        and final["build"].get("result") == "Succeeded" and final["build"].get("errors") == "0"
        and final.get("adventure", {}).get("passed") == 25
        and "passed=True|count=25|simulation=FixedUpdate|manualSimulate=False" in (final.get("adventure", {}).get("ending") or "")
        and all(save.get("Verified", False) and save.get("Values") == 388 and save.get("UnityExit") == 0 for save in final["saves"].values())
        and sorted(changed) == ["Assets/AnimeHillRacing/Scripts/Terrain/TerrainManager.cs", "Assets/AnimeHillRacing/Scripts/UI/CoinPackShopUI.cs", "Assets/AnimeHillRacing/Scripts/UI/GemPackShopUI.cs"]
        and not missing
    )
    (AUDIT / "final-evidence.json").write_text(json.dumps(final, indent=2), encoding="utf-8")
    print(json.dumps({key: value for key, value in final.items() if key not in ["changedFromOriginalWorkingTree", "adventure"]}, indent=2))
    print("Adventure:", final.get("adventure", {}).get("passed", 0), "/ 25")


if __name__ == "__main__":
    main()
