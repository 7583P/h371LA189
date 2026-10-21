"""Read-only source inventory plus new, non-overwriting forensic snapshots."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[2]


def sha(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True, encoding="utf-8").strip()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    output = args.output.resolve()
    output.relative_to(ROOT / "Docs" / "ReleaseGates")
    if git("branch", "--show-current") != "main":
        raise SystemExit("Only main is authorized")
    if output.exists():
        raise SystemExit("Preserve prior evidence: output already exists")
    backup = ROOT / "tmp" / "ReleaseGates" / output.name / "before"
    if backup.exists():
        raise SystemExit("Preserve prior snapshots: backup already exists")
    output.mkdir(parents=True)
    backup.mkdir(parents=True)
    sources = []
    for folder in ("Assets/AnimeHillRacing/Scripts", "Assets/AnimeHillRacing/Resources", "ProjectSettings", "Packages", "Tools/CompetitiveQA", "Tools/RC"):
        for path in (ROOT / folder).rglob("*"):
            if not path.is_file() or set(path.parts) & {"obj", "bin", "__pycache__"}:
                continue
            if path.suffix not in {".cs", ".shader", ".asset", ".json", ".ps1", ".py", ".meta", ".csproj", ".txt"}:
                continue
            relative = path.relative_to(ROOT).as_posix()
            sources.append({"path": relative, "bytes": path.stat().st_size, "sha256": sha(path)})
            # Keep potentially private configuration snapshots outside versionable evidence.
            target = backup / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, target)
    scenes = []
    for pattern in ("*.unity", "*.prefab"):
        for path in (ROOT / "Assets/AnimeHillRacing").rglob(pattern):
            scenes.append({"path": path.relative_to(ROOT).as_posix(), "bytes": path.stat().st_size, "sha256": sha(path)})
    old_manifest = ROOT / "Logs/SeasonExpansion/20261018/provenance/final-artifact-hashes.json"
    artifacts = []
    for old in json.loads(old_manifest.read_text(encoding="utf-8-sig"))["artifacts"]:
        path = ROOT / old["path"]
        actual = sha(path) if path.exists() else None
        artifacts.append({**old, "currentSha256": actual, "matchesPriorEvidence": actual == old["sha256"]})
    source_prompt = Path(r"C:\Users\diego\Downloads\Hill_Adventurer_Prompt_Maestro_GPT_Sol_6_1_MAX_20261019.txt")
    if source_prompt.exists():
        shutil.copy2(source_prompt, output / "authorized-request.txt")
    shutil.copy2(ROOT / "Docs/SeasonExpansion-MMR-20261018.md", output / "prior-report.md")
    manifest = {
        "auditedUtc": datetime.now(timezone.utc).isoformat(), "head": git("rev-parse", "HEAD"),
        "branch": git("branch", "--show-current"), "initialStatus": git("status", "--porcelain=v1", "--untracked-files=all"),
        "initialDiff": git("diff", "--stat"), "initialStagedDiff": git("diff", "--cached", "--stat"),
        "snapshotRoot": backup.relative_to(ROOT).as_posix(), "sources": sorted(sources, key=lambda x: x["path"]),
        "scenesAndPrefabs": scenes, "previousArtifacts": artifacts,
        "scope": "SOURCE_AND_ARTIFACT_FORENSICS_NOT_RUNTIME_APPROVAL",
    }
    (output / "baseline-inventory.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(json.dumps({"output": str(output), "head": manifest["head"], "sources": len(sources), "scenesAndPrefabs": len(scenes), "priorArtifactMatches": [a["matchesPriorEvidence"] for a in artifacts]}))


if __name__ == "__main__":
    main()
