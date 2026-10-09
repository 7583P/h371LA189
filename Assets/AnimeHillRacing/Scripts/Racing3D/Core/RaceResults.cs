using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [Serializable]
    public sealed class RacerResultEntry
    {
        public int finishPosition; // 1 to 12
        public string racerId;
        public string displayName;
        public bool isHuman;
        public string vehicleId;
        public string wheelId;
        public float totalTime;
        public bool finished;
        public float bestLapTime;
        public int oldMmr;
        public int mmrDelta;
        public int newMmr;
        public string titleId;
        public int shortcutSuccesses, aerialSuccesses, landingSuccesses;

        public override string ToString() =>
            $"P{finishPosition:00} | {displayName,-14} | Time: {FormatTime(totalTime)} | Best: {FormatTime(bestLapTime)} | MMR: {oldMmr} -> {newMmr} ({(mmrDelta >= 0 ? "+" : "")}{mmrDelta})";

        public static string FormatTime(float seconds)
        {
            if (seconds <= 0f || float.IsInfinity(seconds) || float.IsNaN(seconds)) return "--:--.---";
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remainder = seconds - (minutes * 60f);
            return $"{minutes:00}:{remainder:00.000}";
        }
    }

    [Serializable]
    public sealed class RaceResults
    {
        public RacerResultEntry[] entries = Array.Empty<RacerResultEntry>();
        public float totalRaceTime;
        public string trackId = "track_01";
        public string trackDisplayName = "GRAND CIRCUIT";

        public RacerResultEntry GetPlayerEntry()
        {
            if (entries == null) return null;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].isHuman) return entries[i];
            }
            return null;
        }

        public RacerResultEntry GetEntryAtPosition(int position)
        {
            if (entries == null) return null;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].finishPosition == position) return entries[i];
            }
            return null;
        }
    }
}
