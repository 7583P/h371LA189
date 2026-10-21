"""Summarize observed pre-recovery motion; never assign causal lost seconds."""
import argparse
from collections import Counter
import json
from pathlib import Path


def summarize(root):
    cases = []
    for path in sorted(root.glob("seed-*/recovery-*.json")):
        trace = json.loads(path.read_text(encoding="utf-8-sig"))
        samples = trace["before"]
        if not samples:
            continue
        end = samples[-1]["time"]
        last = [s for s in samples if s["time"] >= end - 2]
        outside = [s for s in last if abs(s["lateralOffset"]) > s["trackWidth"] * .5 + 2]
        cases.append({
            "source": path.as_posix(), "seed": trace["seed"], "name": trace["name"], "reason": trace["reason"],
            "time": end, "lap": last[-1]["lap"], "nodes": sorted({s["currentNode"] for s in last}),
            "minimumSpeedKmh": min(s["speed"] for s in last), "maximumSpeedKmh": max(s["speed"] for s in last),
            "maximumAbsLateralM": max(abs(s["lateralOffset"]) for s in last),
            "outsideSamples": len(outside), "samples": len(last),
            "airborneSamples": sum(not s["grounded"] for s in last), "miniHopSamples": sum(s["miniHop"] for s in last),
            "boostSamples": sum(s["boosting"] for s in last), "shortcutSamples": sum(s["shortcut"] for s in last),
            "contactCounterIncrements": last[-1]["racerContacts"] - last[0]["racerContacts"],
            "wallCounterIncrements": last[-1]["wallContacts"] - last[0]["wallContacts"],
            "itemHitCounterIncrements": last[-1]["itemHits"] - last[0]["itemHits"],
            "intents": dict(Counter(s["intent"] for s in last)),
            "finalSample": last[-1],
            "interpretation": "Observed 2-second pre-recovery window; contact/item/hop associations do not establish a counterfactual or causal finish-time cost.",
        })
    return cases


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("root", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    cases = summarize(args.root)
    with args.output.open("x", encoding="utf-8") as stream:
        json.dump({"classification": "OBSERVATIONAL_TRACE_ANALYSIS_NOT_CAUSAL_AB", "recoveries": cases}, stream, indent=2)
    for case in cases:
        print(json.dumps({k: v for k, v in case.items() if k not in {"finalSample", "interpretation", "source"}}))


if __name__ == "__main__":
    main()
