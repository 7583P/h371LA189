using System;

namespace AnimeHillRacing.Racing3D
{
    public enum RaceOrigin : byte { Physical = 1, Simulated = 2 }
    public enum ParticipantOutcome : byte { Finished = 0, Dnf = 1, Abandoned = 2 }

    public struct RatingParticipant
    {
        public int rating;              // visible MMR when ranked, provisional estimate during Placement
        public bool ranked;             // Placement 3/3 complete
        public int placementGames;      // completed Placement races before this race (0..3)
        public float uncertainty;       // 0..1
        public int position;            // 1..n for finishers; ignored for DNF/abandon
        public ParticipantOutcome outcome;
    }

    public struct RatingOutcome
    {
        public int previousRating, delta, newRating;
        public int newPlacementGames;
        public bool rankedBefore, rankedAfter, placementCompleted;
        public int floorInjection, ceilingRemoval, placementIssuance;
        public double rawDelta;
    }

    // Formula HA-PW2 (pairwise expected result, 12-racer ordinal):
    //
    //  order key   k_i = position (finished) | 1000+distance rank (DNF) | 2000 (abandoned)
    //  pair score  S_ij = 1 if k_i < k_j, 0.5 if equal, 0 otherwise
    //  pair scale  D_ij = D * (1 + 0.18 * (u_i + u_j) / 2)
    //  expectation E_ij = 1 / (1 + 10^((r_j - r_i) / D_ij)),  exponent clamped to +-20
    //  pair K      K_ij = (K_i + K_j) / 2,   K_i = Kest + Kunc * u_i
    //  ranked      d_i  = sum_{j ranked, j != i} K_ij (S_ij - E_ij) / (n - 1)
    //
    // E_ij + E_ji = 1 and K_ij = K_ji, so sum_i d_i = 0 exactly in reals over
    // ranked participants. Integer deltas use floor + largest-remainder
    // reconciliation, giving an exact integer zero-sum per race. Explicit,
    // separately counted exceptions: the 0 floor (injection), the safety
    // ceiling (removal) and Placement completion (issuance). Pairs with a
    // Placement participant never move an established rating.
    //
    // Placement (exactly three races): provisional mu starts at the prior and
    //  mu' = clamp(mu + Kp[g] * sum_{j != i} (S_ij - E(mu, r_j)) / (n - 1), 0, provisionalMaximum)
    // where r_j is the opponent's visible MMR (ranked) or provisional mu.
    // After the third race visible MMR = clamp(round(mu), 0, placementCeiling).
    public static class CompetitiveRating
    {
        public const string FormulaVersion = "HA-PW2";
        public const int PlacementRaces = 3;
        public const int MaximumMmr = 999999;

        // DNFs rank below every finisher, ordered by distance covered when the
        // position is known (equal positions tie); abandonments rank below DNFs.
        public static int OrderKey(in RatingParticipant p) =>
            p.outcome == ParticipantOutcome.Finished ? Math.Max(1, p.position) : p.outcome == ParticipantOutcome.Dnf ? 1000 + Math.Max(0, p.position) : 2000;

        public static double Expected(double ri, double rj, double scale)
        {
            double exponent = Math.Max(-20, Math.Min(20, (rj - ri) / scale));
            return 1.0 / (1.0 + Math.Pow(10, exponent));
        }

        public static double PlacementK(LocalLeagueConfiguration c, int game)
        {
            var k = c.placementK;
            if (k == null || k.Length == 0) return 1000;
            return k[Math.Max(0, Math.Min(k.Length - 1, game))];
        }

        // onlyAbandonerPairs: crash/forced-close settlement where the other
        // racers' physical order is unknown. Only pairs that include an
        // abandoned participant are evaluated; everyone else keeps their rating.
        public static void Settle(RatingParticipant[] p, LocalLeagueConfiguration c, RatingOutcome[] output, double[] scratch, bool onlyAbandonerPairs = false)
        {
            int n = p.Length;
            if (n < 2 || output.Length < n || scratch.Length < n) throw new ArgumentException("Invalid rating lobby.");
            for (int i = 0; i < n; i++)
                if (p[i].rating < 0 || float.IsNaN(p[i].uncertainty) || float.IsInfinity(p[i].uncertainty)) throw new ArgumentException("Invalid rating participant.");
            double baseScale = Math.Max(100, c.ratingScale);
            for (int i = 0; i < n; i++)
            {
                double sum = 0; bool placement = !p[i].ranked;
                double ui = Clamp01(p[i].uncertainty), ki = c.establishedK + c.uncertaintyK * ui;
                int keyI = OrderKey(p[i]);
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    if (onlyAbandonerPairs && p[i].outcome != ParticipantOutcome.Abandoned && p[j].outcome != ParticipantOutcome.Abandoned) continue;
                    if (!placement && !p[j].ranked) continue;
                    int keyJ = OrderKey(p[j]);
                    double s = keyI < keyJ ? 1 : keyI == keyJ ? .5 : 0;
                    double uj = Clamp01(p[j].uncertainty);
                    double scale = baseScale * (1 + .18 * (ui + uj) * .5);
                    double e = Expected(p[i].rating, p[j].rating, scale);
                    sum += placement ? (s - e) : (ki + c.establishedK + c.uncertaintyK * uj) * .5 * (s - e);
                }
                scratch[i] = sum / (n - 1);
            }
            // Ranked: floor + largest-remainder reconciliation to exact zero-sum.
            long floorSum = 0; double realSum = 0;
            for (int i = 0; i < n; i++)
            {
                output[i] = new RatingOutcome { previousRating = p[i].rating, rankedBefore = p[i].ranked, newPlacementGames = p[i].placementGames };
                if (!p[i].ranked) continue;
                double d = scratch[i]; if (double.IsNaN(d) || double.IsInfinity(d)) throw new InvalidOperationException("Non-finite rating delta.");
                output[i].rawDelta = d; output[i].delta = (int)Math.Floor(d); floorSum += output[i].delta; realSum += d;
            }
            long remaining = (long)Math.Round(realSum, MidpointRounding.AwayFromZero) - floorSum;
            while (remaining > 0)
            {
                int best = -1; double bestFraction = -1;
                for (int i = 0; i < n; i++)
                {
                    if (!p[i].ranked) continue;
                    double fraction = output[i].rawDelta - output[i].delta;
                    if (fraction > bestFraction + 1e-12) { best = i; bestFraction = fraction; }
                }
                if (best < 0) break;
                output[best].delta++; remaining--;
                // Each participant receives at most one reconciliation unit.
                output[best].rawDelta = output[best].delta;
            }
            for (int i = 0; i < n; i++)
            {
                ref var o = ref output[i];
                if (p[i].ranked)
                {
                    long next = (long)p[i].rating + o.delta;
                    if (next < 0) { o.floorInjection = (int)(-next); next = 0; }
                    if (next > MaximumMmr) { o.ceilingRemoval = (int)(next - MaximumMmr); next = MaximumMmr; }
                    o.newRating = (int)next; o.delta = o.newRating - p[i].rating; o.rankedAfter = true;
                    continue;
                }
                // Placement participant: provisional estimate update. A partial
                // (abandonment) settlement never consumes another racer's Placement race.
                if (onlyAbandonerPairs && p[i].outcome != ParticipantOutcome.Abandoned) { o.newRating = p[i].rating; o.delta = 0; continue; }
                double mu = p[i].rating + PlacementK(c, p[i].placementGames) * scratch[i];
                mu = Math.Max(0, Math.Min(c.provisionalMaximum, mu));
                o.newPlacementGames = Math.Min(PlacementRaces, p[i].placementGames + 1);
                if (o.newPlacementGames >= PlacementRaces)
                {
                    int visible = (int)Math.Max(0, Math.Min(c.placementCeiling, Math.Round(mu, MidpointRounding.AwayFromZero)));
                    o.newRating = visible; o.placementCompleted = true; o.rankedAfter = true; o.placementIssuance = visible; o.delta = visible;
                }
                else { o.newRating = (int)Math.Round(mu, MidpointRounding.AwayFromZero); o.delta = 0; }
            }
        }

        static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
