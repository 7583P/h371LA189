"""Aggregate measured Unity races without mixing headless and rendered FPS."""
import argparse
import csv
import json
import statistics
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def mean(values):
    return round(statistics.fmean(values), 4) if values else None


def load_runs(root):
    if not root:
        return []
    runs = []
    for path in sorted(Path(root).rglob("race.json")):
        metric = path.with_name("metrics.json")
        if metric.exists():
            runs.append({"path": str(path.parent), "race": read(path), "metrics": read(metric)})
    return runs


def aggregate(runs):
    bots = [r for run in runs for r in run["metrics"]["racers"] if r["name"] != "PLAYER"]
    laps = [lap for bot in bots for lap in bot.get("lapTimes", []) if lap > 5]
    flying = [lap for bot in bots for lap in bot.get("lapTimes", [])[1:] if lap > 5]
    seconds = sum(bot.get("drivingSeconds", 0) for bot in bots)
    contacts_available = bool(bots) and all("wallCollisions" in bot and "racerCollisions" in bot for bot in bots)
    physical = sum(bot["wallCollisions"] + bot["racerCollisions"] for bot in bots) if contacts_available else None
    row = {
        "races": len(runs), "bots": len(bots), "completedLaps": len(laps),
        "finishedBots": sum(bot.get("finished", False) for bot in bots),
        "meanRaceSeconds": mean([run["race"]["raceSeconds"] for run in runs]),
        "meanBestLap": mean([bot["bestLap"] for bot in bots if bot.get("bestLap", 0) > 5]),
        "meanLap": mean(laps), "meanFlyingLap": mean(flying),
        "meanSpeedKmh": mean([bot.get("averageSpeedKmh", 0) for bot in bots]),
        "physicalContacts": physical,
        "contactsPerRacerMinute": round(physical * 60 / seconds, 4) if seconds and contacts_available else None,
        "samePhysics": all(run["race"]["samePhysics"] for run in runs),
        "validFinishes": all(run["race"]["validFinishes"] for run in runs),
        "runtimeErrors": sum(run["race"]["runtimeErrors"] for run in runs),
        "stalledRacers": sum(run["race"]["stalledRacers"] for run in runs),
        "underTrackRacers": sum(run["race"]["underTrackRacers"] for run in runs),
        "strictRacePasses": sum(run["race"]["passed"] for run in runs),
    }
    fields = ["recoveries", "wallCollisions", "racerCollisions", "itemHits", "items",
              "shortcutVisits", "shortcutCompletions", "driftSeconds", "miniTurbos",
              "jumps", "tricks", "landingBoosts", "offroadSeconds", "passingDecisions",
              "defensiveDecisions", "trafficBrakingDecisions", "driftJumpChains",
              "jumpTrickLandingChains", "boostsForShortcuts"]
    for field in fields:
        values = [bot[field] for bot in bots if field in bot]
        row[field + "Total"] = round(sum(values), 4) if values else None
        row[field + "PerBot"] = mean(values)
        row[field + "Bots"] = sum(value > 0 for value in values) if values else None
    return row


def profile_summary(runs):
    if not runs:
        return {}
    metrics = [run["metrics"] for run in runs]
    frames = sum(metric["frames"] for metric in metrics)
    weighted_fields = ["meanFrameMs", "meanMainThreadMs", "meanGpuMs", "meanPhysicsMs",
                       "meanAiMs", "meanRenderMs", "meanGcBytes", "meanDrawCalls", "meanBatches"]
    result = {
        "runs": len(runs), "frames": frames,
        "seconds": round(sum(metric["measuredSeconds"] for metric in metrics), 3),
        "timeScales": sorted(set(metric["timeScale"] for metric in metrics)),
        "classifications": sorted(set(metric["classification"] for metric in metrics)),
        "hardware": {key: metrics[0][key] for key in ["processor", "graphics", "width", "height", "quality"]},
    }
    for field in weighted_fields:
        result[field] = round(sum(metric[field] * metric["frames"] for metric in metrics) / frames, 4)
    # Unsupported timing is absent evidence, never a zero-cost GPU/render result.
    if not any(metric["gpuFrames"] > 0 for metric in metrics):
        result["meanGpuMs"] = None
    if not any(metric.get("renderAvailable", False) and metric["meanRenderMs"] > 0 for metric in metrics):
        result["meanRenderMs"] = None
    result["averageFps"] = round(1000 / result["meanFrameMs"], 3)
    for field in ["p95FrameMs", "p99FrameMs", "onePercentLowFps", "maxGcBytes", "gcCollectionFrames",
                  "poolDroppedRequests", "itemEntitiesCreatedDuringRace", "instantiateCalls", "destroyCalls"]:
        result[field + "BySeed"] = {str(run["race"]["seed"]): run["metrics"][field] for run in runs}
    result["gpuFrames"] = sum(metric["gpuFrames"] for metric in metrics)
    result["renderCounterBySeed"] = {str(run["race"]["seed"]): run["metrics"].get("renderCounter") for run in runs}
    result["minimumFpsBySeed"] = {str(run["race"]["seed"]): run["metrics"].get("minimumFps") for run in runs}
    return result


def main():
    parser = argparse.ArgumentParser()
    for name in ["benchmark", "ranked", "baseline", "profile"]:
        parser.add_argument("--" + name)
    parser.add_argument("--out", required=True)
    args = parser.parse_args()
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    summary = {}
    for name in ["ranked", "baseline"]:
        root = getattr(args, name)
        if root:
            summary[name] = aggregate(load_runs(root))
    if args.benchmark:
        grouped = {}
        for run in load_runs(args.benchmark):
            grouped.setdefault(run["race"]["playerMmr"], []).append(run)
        rows = [{"mmr": mmr, **aggregate(runs)} for mmr, runs in sorted(grouped.items())]
        summary["benchmark"] = rows
        if rows:
            with (out / "benchmark.csv").open("w", newline="", encoding="utf-8") as handle:
                writer = csv.DictWriter(handle, fieldnames=rows[0].keys())
                writer.writeheader()
                writer.writerows(rows)
        for row in rows:
            print(f"{row['mmr']:>6}: bots={row['bots']} best={row['meanBestLap']} lap={row['meanLap']} "
                  f"recovery={row['recoveriesPerBot']} shortcut={row['shortcutCompletionsPerBot']} "
                  f"mini={row['miniTurbosPerBot']} chains={row['jumpTrickLandingChainsPerBot']} "
                  f"speed={row['meanSpeedKmh']} pass={row['strictRacePasses']}/{row['races']}")
    if args.profile:
        summary["profile"] = profile_summary(load_runs(args.profile))
    (out / "measured-summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps({key: value for key, value in summary.items() if key != "benchmark"}, indent=2))


if __name__ == "__main__":
    main()
