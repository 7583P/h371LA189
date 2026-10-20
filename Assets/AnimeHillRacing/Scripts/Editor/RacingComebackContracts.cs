#if UNITY_EDITOR
using System;
using System.IO;
using AnimeHillRacing.Racing3D;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    public static class RacingComebackContracts
    {
        const int Rolls = 24000;
        [Serializable] sealed class Evidence
        {
            public int rollsPerContext = Rolls;
            public float packedMobility, detachedMobility, groupedMobility, earlyMobility, repeatedLancerChance;
            public float packedLancerChance, moderateLancerChance, detachedLancerChance;
            public int[] packed, moderate, detached, grouped, early, repeated;
        }
        static int[] Sample(ItemRollContext context)
        {
            var counts = new int[RaceItemUtility.Count + 1];var rng = new DeterministicRng(73917);
            for (int i = 0; i < Rolls; i++) counts[(int)ItemDistributionTable.RollItem(context, rng)]++;
            return counts;
        }
        static float Mobility(int[] counts)
        {
            int count = 0;
            for (int i = 1; i < counts.Length; i++)
                if (RaceItemUtility.IsBoost((RaceItemType)i) || (RaceItemType)i == RaceItemType.AerialRecovery) count += counts[i];
            return count / (float)Rolls;
        }
        public static void Run()
        {
            string output = RacingFrameForensics.Argument("--closure-output");Directory.CreateDirectory(output);
            var report = new ClosureContractReport();var evidence = new Evidence();
            var packed = new ItemRollContext { position = 12, lap = 3, totalLaps = 3, lapProgress = .9f };
            var detached = packed;detached.gapLeaderMeters = 480;detached.gapPackMeters = 150;detached.packSpreadMeters = 480;
            var moderate = packed;moderate.gapLeaderMeters = 100;moderate.gapPackMeters = 25;moderate.packSpreadMeters = 180;
            var grouped = detached;grouped.gapPackMeters = 3;
            var early = detached;early.lap = 1;early.lapProgress = 0;
            var repeated = detached;repeated.previous = repeated.previousPrevious = RaceItemType.Lancer;
            evidence.packed = Sample(packed);evidence.moderate = Sample(moderate);evidence.detached = Sample(detached);evidence.grouped = Sample(grouped);
            evidence.early = Sample(early);evidence.repeated = Sample(repeated);
            evidence.packedMobility = Mobility(evidence.packed);evidence.detachedMobility = Mobility(evidence.detached);
            evidence.groupedMobility = Mobility(evidence.grouped);evidence.earlyMobility = Mobility(evidence.early);
            evidence.repeatedLancerChance = evidence.repeated[(int)RaceItemType.Lancer] / (float)Rolls;
            evidence.packedLancerChance = evidence.packed[(int)RaceItemType.Lancer] / (float)Rolls;
            evidence.moderateLancerChance = evidence.moderate[(int)RaceItemType.Lancer] / (float)Rolls;
            evidence.detachedLancerChance = evidence.detached[(int)RaceItemType.Lancer] / (float)Rolls;
            report.Check(evidence.detachedMobility > evidence.packedMobility + .10f, "Real distance/spread increase rear mobility at the same position and lap");
            report.Check(evidence.detachedMobility > evidence.groupedMobility + .02f, "Separation from the nearby pack changes catch-up odds independently of leader distance");
            report.Check(evidence.detachedMobility > evidence.earlyMobility, "Late race progress strengthens bounded catch-up assistance");
            report.Check(evidence.detachedLancerChance > evidence.moderateLancerChance + .05f, "A severely detached tail racer has more burst catch-up opportunities than a nearby rear racer");
            report.Check(evidence.detachedLancerChance < .40f && evidence.moderateLancerChance < .25f, "Neither gradual nor burst catch-up guarantees Lancer or a race win");
            report.Check(evidence.repeated[(int)RaceItemType.Lancer] < evidence.detached[(int)RaceItemType.Lancer] / 2, "Recent item history suppresses repeated Lancer even far behind");
            report.Check(evidence.detachedMobility < .90f && evidence.detached[(int)RaceItemType.Coin10] == 0, "Rear pool remains mixed rather than guaranteeing a winning item");
            detached.position = 1;var leader = Sample(detached);
            report.Check(leader[(int)RaceItemType.Coin10] + leader[(int)RaceItemType.ElectroSpike] == Rolls, "Leader remains Coin +10 or ordinary Rock at every gap and race phase");
            detached.position = 2;var front = Sample(detached);
            report.Check(front[(int)RaceItemType.Lancer] == 0 && front[(int)RaceItemType.HyperCharge] == 0, "Front three never acquire rear-only catch-up powers");
            detached.position = 12;detached.held = RaceItemType.GoldenRock;
            report.Check(ItemDistributionTable.RollItem(detached, new DeterministicRng(1)) == RaceItemType.None, "Catch-up logic never replaces an occupied inventory slot");
            detached.held = RaceItemType.None;var rng = new DeterministicRng(4207);ItemDistributionTable.RollItem(detached, rng);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) ItemDistributionTable.RollItem(detached, rng);
            report.Check(GC.GetAllocatedBytesForCurrentThread() == before, "10000 warmed gap-aware rolls allocate zero managed bytes");
            File.WriteAllText(Path.Combine(output, "comeback-probability.json"), JsonUtility.ToJson(evidence, true));
            File.WriteAllText(Path.Combine(output, "comeback-contracts.json"), JsonUtility.ToJson(report, true));
            if (report.failed != 0) throw new InvalidOperationException("Comeback contracts failed: " + report.failed);
            // Existing focused suites cover the unchanged item tables and the
            // visual environment/shortcut/control boundary in the same Editor run.
            RacingLancerContracts.Run();
            RacingSkyHopContracts.Run();
        }
    }
}
#endif
