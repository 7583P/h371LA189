using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D
{
    // Explicit settlement lifecycle. A RaceId advances monotonically and can
    // never return to an earlier state; Settled+ is terminal for MMR.
    public enum RaceSettlementState : byte
    {
        Reserved = 0,          // opponents atomically reserved, nothing rated
        Started = 1,           // GO happened: leaving now is an abandonment
        RaceCompleted = 2,     // physical order known (results built)
        RaceSettled = 3,       // MMR/Placement applied to every participant
        RewardsCommitted = 4,  // wallet/coin rewards flushed exactly once
        ProfileSaved = 5,      // durable journal generation contains the settlement
        Cancelled = 6          // released before GO; no rating effect
    }

    [Serializable]
    public sealed class RaceSettlementRecord
    {
        public string raceId;
        public int seasonId;
        public byte origin;          // RaceOrigin
        public byte state;           // RaceSettlementState
        public string formulaVersion = CompetitiveRating.FormulaVersion;
        public long createdUtc, settledUtc;
        public int seed;
        public bool abandonment, partial;
        public string[] participantIds = Array.Empty<string>();
        public int[] positions = Array.Empty<int>();
        public byte[] outcomes = Array.Empty<byte>();
        public int[] previousMmr = Array.Empty<int>();
        public int[] deltaMmr = Array.Empty<int>();
        public int[] finalMmr = Array.Empty<int>();
        public int[] placementBefore = Array.Empty<int>();
        public int[] placementAfter = Array.Empty<int>();
        public int[] provisionalBefore = Array.Empty<int>();
        public int[] provisionalAfter = Array.Empty<int>();
        public int[] shortcuts = Array.Empty<int>(), aerials = Array.Empty<int>(), landings = Array.Empty<int>();
        public float[] bestLaps = Array.Empty<float>();
        public int netDelta, floorInjection, ceilingRemoval, placementIssuance;
        public RaceSettlementRecord Copy()
        {
            var c = (RaceSettlementRecord)MemberwiseClone();
            c.participantIds = (string[])participantIds.Clone(); c.positions = (int[])positions.Clone(); c.outcomes = (byte[])outcomes.Clone();
            c.previousMmr = (int[])previousMmr.Clone(); c.deltaMmr = (int[])deltaMmr.Clone(); c.finalMmr = (int[])finalMmr.Clone();
            c.placementBefore = (int[])placementBefore.Clone(); c.placementAfter = (int[])placementAfter.Clone();
            c.provisionalBefore = (int[])provisionalBefore.Clone(); c.provisionalAfter = (int[])provisionalAfter.Clone();
            c.shortcuts=(int[])(shortcuts??Array.Empty<int>()).Clone();c.aerials=(int[])(aerials??Array.Empty<int>()).Clone();c.landings=(int[])(landings??Array.Empty<int>()).Clone();c.bestLaps=(float[])(bestLaps??Array.Empty<float>()).Clone();
            return c;
        }
    }

    // Physical ranked race reserved for the human. Persisted with the journal
    // and in the tiny session journal so a crash/forced close is recoverable.
    [Serializable]
    public sealed class PendingRankedRace
    {
        public string raceId;
        public int seasonId, seed;
        public byte state;
        public long reservedUtc, startedUtc;
        public int[] botIndices = Array.Empty<int>();
        public int humanRatingAtReservation;
        public bool humanRankedAtReservation;
        public PendingRankedRace Copy() { var c = (PendingRankedRace)MemberwiseClone(); c.botIndices = (int[])botIndices.Clone(); return c; }
    }

    [Serializable]
    public sealed class SeasonTransitionReceipt
    {
        public string transitionId;
        public int sourceSeason, destinationSeason, contractVersion;
        public long boundaryUtc, committedUtc;
        public long ratingBefore, ratingAfter; // population sum, bots + human
        public int rankedHumansAndBots;
    }

    [Serializable]
    public sealed class ClockAnomaly
    {
        public string kind;
        public long observedUtc, acceptedUtc, monotonicMilliseconds;
        public long magnitudeSeconds;
    }

    [Serializable]
    public sealed class RatingBudget
    {
        // Every way rating can enter or leave the population is a named counter.
        public long pairwiseNet;          // must remain exactly 0
        public long floorInjection, ceilingRemoval, placementIssuance, softResetRemoval, migrationRemoval;
        public long physicalRaces, simulatedRaces, settledParticipants;
        public long Expected(long initial) => initial + pairwiseNet + floorInjection - ceilingRemoval + placementIssuance - softResetRemoval - migrationRemoval;
    }

    [Serializable]
    public sealed class CompetitiveLedger
    {
        public const int RetainedSettlements = 96, RetainedRaceIds = 4096, RetainedAnomalies = 48;
        public List<string> appliedMigrations = new List<string>();
        public List<SeasonTransitionReceipt> seasonReceipts = new List<SeasonTransitionReceipt>();
        public List<RaceSettlementRecord> recentSettlements = new List<RaceSettlementRecord>();
        public List<string> settledRaceIds = new List<string>();
        public List<string> importedExportIds = new List<string>();
        public List<ClockAnomaly> anomalies = new List<ClockAnomaly>();
        public RatingBudget budget = new RatingBudget();
        public long totalHumanSettlements, duplicateSettlementRejections, totalAnomalies;

        [NonSerialized] HashSet<string> settledLookup;
        public bool IsSettled(string raceId)
        {
            if (string.IsNullOrEmpty(raceId)) return false;
            if (settledLookup == null || settledLookup.Count != settledRaceIds.Count) settledLookup = new HashSet<string>(settledRaceIds, StringComparer.Ordinal);
            return settledLookup.Contains(raceId);
        }
        public bool HasSeasonTransition(string id) { foreach (var r in seasonReceipts) if (r.transitionId == id) return true; return false; }
        public bool HasMigration(string id) => appliedMigrations.Contains(id);

        public void MarkSettled(RaceSettlementRecord record)
        {
            settledRaceIds.Add(record.raceId); settledLookup?.Add(record.raceId);
            while (settledRaceIds.Count > RetainedRaceIds) { settledLookup?.Remove(settledRaceIds[0]); settledRaceIds.RemoveAt(0); }
            recentSettlements.Add(record); while (recentSettlements.Count > RetainedSettlements) recentSettlements.RemoveAt(0);
        }

        public void Record(ClockAnomaly anomaly)
        {
            totalAnomalies++; anomalies.Add(anomaly); while (anomalies.Count > RetainedAnomalies) anomalies.RemoveAt(0);
        }

        public CompetitiveLedger Copy()
        {
            var c = (CompetitiveLedger)MemberwiseClone();
            c.settledLookup = null;
            c.appliedMigrations = new List<string>(appliedMigrations); c.settledRaceIds = new List<string>(settledRaceIds);
            c.importedExportIds = new List<string>(importedExportIds);
            c.seasonReceipts = new List<SeasonTransitionReceipt>(seasonReceipts.Count); foreach (var r in seasonReceipts) c.seasonReceipts.Add((SeasonTransitionReceipt)r.MemberwiseCloneCopy());
            c.recentSettlements = new List<RaceSettlementRecord>(recentSettlements.Count); foreach (var r in recentSettlements) c.recentSettlements.Add(r.Copy());
            c.anomalies = new List<ClockAnomaly>(anomalies.Count); foreach (var a in anomalies) c.anomalies.Add(new ClockAnomaly { kind = a.kind, observedUtc = a.observedUtc, acceptedUtc = a.acceptedUtc, monotonicMilliseconds = a.monotonicMilliseconds, magnitudeSeconds = a.magnitudeSeconds });
            c.budget = (RatingBudget)budget.MemberwiseCloneCopy();
            return c;
        }
    }

    static class LedgerCopyExtensions
    {
        public static object MemberwiseCloneCopy(this SeasonTransitionReceipt r) => new SeasonTransitionReceipt { transitionId = r.transitionId, sourceSeason = r.sourceSeason, destinationSeason = r.destinationSeason, contractVersion = r.contractVersion, boundaryUtc = r.boundaryUtc, committedUtc = r.committedUtc, ratingBefore = r.ratingBefore, ratingAfter = r.ratingAfter, rankedHumansAndBots = r.rankedHumansAndBots };
        public static object MemberwiseCloneCopy(this RatingBudget b) => new RatingBudget { pairwiseNet = b.pairwiseNet, floorInjection = b.floorInjection, ceilingRemoval = b.ceilingRemoval, placementIssuance = b.placementIssuance, softResetRemoval = b.softResetRemoval, migrationRemoval = b.migrationRemoval, physicalRaces = b.physicalRaces, simulatedRaces = b.simulatedRaces, settledParticipants = b.settledParticipants };
    }
}
