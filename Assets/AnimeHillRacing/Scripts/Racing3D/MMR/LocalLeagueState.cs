using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D
{
    [Serializable]
    public sealed class SoftResetPoint
    {
        public int endingMmr;
        public int startingMmr;
        public SoftResetPoint(int ending, int starting) { endingMmr = ending; startingMmr = starting; }
    }

    [Serializable]
    public sealed class LocalLeagueConfiguration
    {
        public int historyLimit = 20;
        public int eventsPerHour = 120;
        public int maximumCatchupEvents = 8192;
        public float menuBudgetMilliseconds = 1.5f;
        public int population = 10000;
        public float ratingScale = 1100f;
        public float establishedK = 50f;
        public float uncertaintyK = 120f;
        public SoftResetPoint[] resetCurve = {
            new SoftResetPoint(0, 0), new SoftResetPoint(1000, 980),
            new SoftResetPoint(2500, 2350), new SoftResetPoint(5000, 4500),
            new SoftResetPoint(7500, 6500), new SoftResetPoint(9000, 8200),
            new SoftResetPoint(9500, 9200), new SoftResetPoint(10000, 10000)
        };

        public int ResetMmr(int mmr)
        {
            int normalResult = CalculateNormalSeasonReset(mmr);
            // Beyond is a ceiling after the shared curve, never a starting-rating floor.
            return mmr > MmrTierUtility.MaxTargetMmr
                ? Math.Min(normalResult, MmrTierUtility.MaxTargetMmr) : normalResult;
        }

        public int CalculateNormalSeasonReset(int mmr)
        {
            mmr = Math.Max(0, mmr);
            if (resetCurve == null || resetCurve.Length < 2) throw new InvalidOperationException("Missing season reset curve.");
            for (int i = 1; i < resetCurve.Length; i++)
            {
                var a = resetCurve[i - 1]; var b = resetCurve[i];
                if (b.endingMmr <= a.endingMmr || b.startingMmr < a.startingMmr)
                    throw new InvalidOperationException("Season reset curve must preserve ordering.");
                if (mmr <= b.endingMmr)
                {
                    double width = b.endingMmr - a.endingMmr;
                    double t = (mmr - a.endingMmr) / width, t2 = t * t, t3 = t2 * t;
                    double result = (2*t3-3*t2+1)*a.startingMmr + (t3-2*t2+t)*width*ResetSlope(i-1) +
                        (-2*t3+3*t2)*b.startingMmr + (t3-t2)*width*ResetSlope(i);
                    return Math.Max(0, Math.Min(mmr, (int)Math.Round(result, MidpointRounding.AwayFromZero)));
                }
            }
            return Math.Min(mmr, resetCurve[resetCurve.Length - 1].startingMmr);
        }
        double ResetSlope(int knot)
        {
            int left = Math.Max(0, knot-1), right = Math.Min(resetCurve.Length-2, knot);
            double h0 = resetCurve[left+1].endingMmr-resetCurve[left].endingMmr;
            double h1 = resetCurve[right+1].endingMmr-resetCurve[right].endingMmr;
            double d0 = (resetCurve[left+1].startingMmr-resetCurve[left].startingMmr)/h0;
            double d1 = (resetCurve[right+1].startingMmr-resetCurve[right].startingMmr)/h1;
            if (d0 <= 0 || d1 <= 0) return 0;
            double w0 = 2*h1+h0, w1 = h1+2*h0;
            return (w0+w1)/(w0/d0+w1/d1);
        }
    }

    [Serializable]
    public sealed class LeagueSkillProfile
    {
        public float latentSkill, consistency, aggression, riskTolerance, driftSkill, aerialSkill;
        public float shortcutSkill, itemSkill, racecraft, recoverySkill, learningPotential;
        public AiRacingStyle preferredStyle;
        internal LeagueSkillProfile CopyForSave() => (LeagueSkillProfile)MemberwiseClone();
    }

    [Serializable]
    public sealed class RacingSeasonHistory
    {
        public int seasonNumber, startingMmr, endingMmr, peakMmr, beyondPeakCounter;
        public string startingRank, endingRank, peakRank;
        public string selectedTitleId="";
        public int races, wins, podiums, nextStartingMmr;
        public float bestLap;
        public long seasonStartTimestamp, seasonEndTimestamp;
        internal RacingSeasonHistory CopyForSave() => (RacingSeasonHistory)MemberwiseClone();
    }

    [Serializable]
    public sealed class LeagueCompetitor
    {
        public string id, displayName, vehiclePreference = "car-01", wheelPreference = "wheel-default";
        public string characterPreference="cobalt-driver",accessoryPreference="none";
        public int personalitySeed, currentMmr, peakMmr, seasonStartingMmr, seasonPeakMmr;
        public int races, wins, podiums, seasonRaces, seasonWins, seasonPodiums, shortcutSuccesses, aerialSuccesses, landingSuccesses;
        public float uncertainty = 1f, recentForm, bestLap, seasonBestLap;
        public LeagueSkillProfile profile;
        public List<RacingSeasonHistory> history = new List<RacingSeasonHistory>();
        public List<string> unlockedTitleIds = new List<string>();
        public string selectedTitleId = "";
        public int lastLeaderboardPosition;

        internal LeagueCompetitor CopyForSave()
        {
            var copy=(LeagueCompetitor)MemberwiseClone();
            copy.profile=profile?.CopyForSave();
            copy.history=new List<RacingSeasonHistory>(history.Count);
            foreach(var entry in history)copy.history.Add(entry.CopyForSave());
            copy.unlockedTitleIds=new List<string>(unlockedTitleIds);
            return copy;
        }
    }

    [Serializable]
    public sealed class LocalLeagueState
    {
        public const int CurrentVersion = 3;
        public int version = CurrentVersion, populationSeed, currentSeasonNumber = 1, lastProcessedSeasonNumber;
        public long installationTimestamp, seasonStartTimestamp, seasonEndTimestamp, lastKnownUtcTimestamp;
        public long lastLeagueSimulationUtc, simulationEventNumber, skippedCatchupEvents;
        public int matchmakingSequence;
        public LeagueCompetitor human;
        [NonSerialized] public CompactLeagueBot[] bots;
        public int population=10000, ratingScaleMaximum=10000;
        public List<LegacyBotAlias> botAliases=new List<LegacyBotAlias>();
        public List<LeagueSeasonArchive> archives=new List<LeagueSeasonArchive>();
        public LeagueSeasonArchive pendingSeasonArchive;
        public int seasonTransitionCursor=-1;
        public long simulationRatingCreatedAtFloor;
        public List<string> recentOpponentIds = new List<string>();

        // Only the main thread creates this snapshot. The save worker owns every
        // mutable object it serializes, so live matches and season changes cannot
        // produce a partially updated journal generation.
        public LocalLeagueState CopyForSave()
        {
            var copy=(LocalLeagueState)MemberwiseClone();
            copy.human=human.CopyForSave();copy.bots=(CompactLeagueBot[])bots.Clone();
            copy.recentOpponentIds=new List<string>(recentOpponentIds);
            copy.botAliases=new List<LegacyBotAlias>(botAliases.Count);
            foreach(var alias in botAliases)copy.botAliases.Add(new LegacyBotAlias{index=alias.index,displayName=alias.displayName});
            copy.archives=new List<LeagueSeasonArchive>(archives.Count);
            foreach(var archive in archives)copy.archives.Add(archive.CopyForSave());
            copy.pendingSeasonArchive=pendingSeasonArchive?.CopyForSave();
            return copy;
        }
    }
}
