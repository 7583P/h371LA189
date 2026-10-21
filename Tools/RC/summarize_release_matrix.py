"""Independently enforce the requested six-race gate, not a harness PASS label."""
import argparse
import csv
import hashlib
import json
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("root", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    if args.output.exists():
        raise SystemExit("Refusing to overwrite matrix analysis")
    args.output.mkdir(parents=True)
    rows = []
    for path in sorted(args.root.glob("mmr-*/seed-*/metrics.json")):
        m = read(path)
        race = read(path.parent / "race.json")
        racers = m["racers"]
        times = [r["finishTime"] for r in racers if r["finished"]]
        rows.append({"mmr": int(path.parent.parent.name.removeprefix("mmr-")), "seed": m["seed"],
                     "finishers": len(times), "dnf": len(racers) - len(times), "racers": len(racers),
                     "recoveries": sum(r["recoveries"] for r in racers),
                     "gapSeconds": max(times) - min(times) if len(times) == 12 else None,
                     "mlTicks": sum(r["mlTicks"] for r in racers),
                     "mlModes": ",".join(sorted({r["mlMode"] for r in racers if r["mlMode"]})),
                     "quality": m["quality"], "timeScale": race["timeScale"],
                     "runtimeErrors": race["runtimeErrors"], "samePhysics": race["samePhysics"],
                     "seedPath": path.parent.as_posix(), "metricsSha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    expected = {(mmr, seed) for mmr in (9700, 10000) for seed in (4207, 4208, 4209)}
    identities = {(r["mmr"], r["seed"]) for r in rows}
    gates = {"sixDistinctRequiredCases": len(rows) == 6 and identities == expected,
             "all72Finishers": sum(r["finishers"] for r in rows) == 72 and all(r["racers"] == 12 for r in rows),
             "zeroDnf": bool(rows) and all(r["dnf"] == 0 for r in rows),
             "atMostTwoRecoveriesTotal": sum(r["recoveries"] for r in rows) <= 2,
             "allGaps3To8Seconds": bool(rows) and all(r["gapSeconds"] is not None and 3 <= r["gapSeconds"] <= 8 for r in rows),
             "effectiveMlOff": bool(rows) and all(r["mlTicks"] == 0 and r["mlModes"] == "Off" for r in rows),
             "controlledConfiguration": bool(rows) and all(r["timeScale"] == 1 and r["quality"] == "High" and r["runtimeErrors"] == 0 and r["samePhysics"] for r in rows)}
    report = {"classification": "PHYSICAL_RACE_MATRIX_NOT_PERFORMANCE_OR_ANDROID_APPROVAL", "status": "PASS" if all(gates.values()) else "FAIL",
              "gates": gates, "totalRecoveries": sum(r["recoveries"] for r in rows), "cases": rows,
              "limits": "Scripted human; wall-time/physics scheduling is not proven bit deterministic. Other Unity project remains open. No focused FPS comparison or human visual approval is inferred."}
    (args.output / "matrix.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    if rows:
        with (args.output / "matrix.csv").open("x", newline="", encoding="utf-8") as stream:
            writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
            writer.writeheader()
            writer.writerows(rows)
    print(json.dumps({k: v for k, v in report.items() if k != "cases"}, indent=2))


if __name__ == "__main__":
    main()
