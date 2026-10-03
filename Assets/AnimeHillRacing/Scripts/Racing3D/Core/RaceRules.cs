using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public enum RaceState
    {
        Loading,
        Preparing,
        Countdown,
        Racing,
        PlayerFinished,
        RaceFinishing,
        Results,
        Exiting
    }

    public enum RacingGameMode
    {
        RankedOffline,
        GrandPrix,
        TimeTrial,
        Practice
    }

    [Serializable]
    public sealed class RaceRules
    {
        public RacingGameMode Mode = RacingGameMode.RankedOffline;
        public int TotalRacers = 12;
        public int TotalLaps = 3;
        public float CountdownSeconds = 3.0f;
        public float PostPlayerFinishTimeoutSeconds = 5.0f;
        public bool EnableLaunchTiming = false;
        public int RaceSeed = 0; // 0 = random dynamic seed
        public bool AllowItems = true;
        public bool DeterministicDebug = false;

        public static RaceRules CreateRanked(int seed = 0) => new RaceRules
        {
            Mode = RacingGameMode.RankedOffline,
            TotalRacers = 12,
            TotalLaps = 3,
            CountdownSeconds = 3.0f,
            PostPlayerFinishTimeoutSeconds = 5.0f,
            RaceSeed = seed,
            AllowItems = true,
            DeterministicDebug = seed != 0
        };
    }
}
