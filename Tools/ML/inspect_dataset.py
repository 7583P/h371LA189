#!/usr/bin/env python3
"""
Inspect and summarize Racing 3D telemetry datasets.
Scans Tools/ML/Data/ for recorded sessions and computes quality, MMR and tech distributions.
"""

import os
import sys
import glob
import json
from pathlib import Path

def inspect_datasets(data_dir: str = "Tools/ML/Data"):
    path = Path(data_dir)
    if not path.exists():
        print(f"[inspect_dataset] Data directory does not exist: {path.resolve()}")
        return

    json_files = list(path.glob("*.json"))
    if not json_files:
        print(f"[inspect_dataset] No telemetry sessions found in {path.resolve()}")
        return

    print("=" * 80)
    print(f"               RACING 3D TELEMETRY DATASET AUDIT ({len(json_files)} sessions)")
    print("=" * 80)

    total_samples = 0
    total_duration = 0.0
    mmr_list = []
    quality_scores = []
    speeds = []
    tracks = set()
    event_counts = {}

    for f in sorted(json_files):
        try:
            with open(f, "r", encoding="utf-8") as fp:
                session = json.load(fp)
            
            meta = session.get("metadata", {})
            samples = session.get("samples", [])
            events = session.get("events", [])

            s_count = len(samples)
            total_samples += s_count
            mmr = meta.get("playerMmr", 50000)
            quality = meta.get("trainingQualityScore", 0.0)
            track = meta.get("trackId", "Unknown")
            duration = meta.get("totalRaceTime", 0.0)

            mmr_list.append(mmr)
            quality_scores.append(quality)
            tracks.add(track)
            total_duration += duration

            for s in samples:
                speeds.append(s.get("speedKmh", 0.0))

            for ev in events:
                ev_type = ev.get("tag", "Unknown")
                event_counts[ev_type] = event_counts.get(ev_type, 0) + 1

            best_lap = meta.get("bestLapTime", 0.0)
            print(f"Session {meta.get('sessionId', f.stem):10s} | Track: {track:22s} | MMR: {mmr:6d} | Q-Score: {quality:.2f} | Samples: {s_count:4d} | Best Lap: {best_lap:5.2f}s")
        except Exception as e:
            print(f"Failed parsing {f.name}: {e}")

    print("-" * 80)
    avg_speed = sum(speeds) / max(1, len(speeds))
    max_speed = max(speeds) if speeds else 0.0
    avg_quality = sum(quality_scores) / max(1, len(quality_scores))
    min_mmr = min(mmr_list) if mmr_list else 0
    max_mmr = max(mmr_list) if mmr_list else 0

    print(f"Total Telemetry Samples: {total_samples:,}")
    print(f"Total Race Time:         {total_duration / 60.0:.1f} minutes")
    print(f"MMR Range:               {min_mmr:,} -> {max_mmr:,}")
    print(f"Mean Training Quality:   {avg_quality:.3f}")
    print(f"Fleet Speed:             Avg={avg_speed:.1f} km/h | Peak={max_speed:.1f} km/h")
    print(f"Tracks Covered:          {', '.join(sorted(tracks))}")
    print(f"Recorded Tech Events:    {dict(sorted(event_counts.items()))}")
    print("=" * 80)

if __name__ == "__main__":
    target_dir = sys.argv[1] if len(sys.argv) > 1 else "Tools/ML/Data"
    inspect_datasets(target_dir)
