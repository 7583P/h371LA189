using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public struct ItemRollContext
    {
        public int position, lap, totalLaps;
        public float gapLeaderSeconds;
        public RaceItemType held, previous, previousPrevious;
    }
    public static class ItemDistributionTable
    {
        [Serializable] public struct ItemWeight { public RaceItemType Type; public int Weight; }
        static RacingItemBalanceConfiguration config;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { config = null; }
        static RacingItemBalanceConfiguration Config {
            get { if (config == null) config = Resources.Load<RacingItemBalanceConfiguration>("RacingItemBalanceConfiguration"); return config; }
        }
        public static RaceItemType RollItem(int racePosition, DeterministicRng rng = null)
            => RollItem(new ItemRollContext { position = racePosition, lap = 1, totalLaps = 3 }, rng);
        public static ItemWeight[] WeightsForPosition(int position)
        {
            var c = Config;
            if(position<=1)return c!=null&&c.leader!=null&&c.leader.Length>0?c.leader:RacingItemBalanceConfiguration.LeaderDefaults;
            var entries = position <= 3 ? c != null ? c.front : null : position <= 6 ? c != null ? c.middle : null : c != null ? c.rear : null;
            return entries != null && entries.Length > 0 ? entries : position <= 3 ? RacingItemBalanceConfiguration.FrontDefaults :
                position <= 6 ? RacingItemBalanceConfiguration.MiddleDefaults : RacingItemBalanceConfiguration.RearDefaults;
        }
        public static bool IsAllowedAtPosition(RaceItemType type, int position)
        {
            if (type <= RaceItemType.None || (int)type > RaceItemUtility.Count) return false;
            if (position <= 1) return type == RaceItemType.Coin10 || type == RaceItemType.ElectroSpike;
            if (position <= 3) return type == RaceItemType.MagneticShield || type == RaceItemType.Coin10 || type == RaceItemType.GoldenRock;
            if (position <= 6) return type == RaceItemType.MagneticShield || type == RaceItemType.Coin10 || type == RaceItemType.MiniTurbo || type == RaceItemType.GoldenRock;
            return true;
        }
        public static RaceItemType RollItem(ItemRollContext context, DeterministicRng rng = null)
        {
            if (context.held != RaceItemType.None) return RaceItemType.None;
            context.position = Mathf.Clamp(context.position, 1, 12);
            var bracket = WeightsForPosition(context.position);
            float total = 0;
            for (int i = 0; i < bracket.Length; i++) total += Weight(bracket[i], context);
            if (total <= 0) return RaceItemType.None;
            float roll = (rng != null ? rng.NextFloat() : UnityEngine.Random.value) * total;
            RaceItemType last = RaceItemType.None;
            for (int i = 0; i < bracket.Length; i++) {
                float weight = Weight(bracket[i], context); if (weight <= 0) continue;
                last = bracket[i].Type; roll -= weight; if (roll < 0) return last;
            }
            return last;
        }
        static float Weight(ItemWeight entry, ItemRollContext context)
        {
            if (!IsAllowedAtPosition(entry.Type, context.position)) return 0;
            float result = Mathf.Max(0, entry.Weight);
            // Rear-only catch-up transformation. Last place is eight times as
            // likely as seventh to roll it before normalizing the other weights.
            if (entry.Type == RaceItemType.Lancer)
                result *= Mathf.Lerp(.25f, 2f, Mathf.InverseLerp(7, 12, context.position));
            bool mobility = RaceItemUtility.IsBoost(entry.Type) || entry.Type == RaceItemType.AerialRecovery;
            float gap = Mathf.Clamp01(context.gapLeaderSeconds / 18f);
            // Strong mobility stays in its placement pool. No assignment of a winner.
            result *= mobility ? 1 + gap * .55f : 1 - gap * .15f;
            if (context.totalLaps > 0 && context.lap >= context.totalLaps)
                result *= mobility ? 1.10f : context.position > 3 ? 1.18f : 1f;
            if (entry.Type == context.previous) result *= .30f;
            if (entry.Type == context.previousPrevious) result *= .65f;
            return result;
        }
    }
    public static class RaceItemUtility
    {
        public const int Count = 16;
        public const int CoinReward = 10;
        public const float LancerDurationSeconds = 6f;
        public const float LancerSpeedKmh = 300f;
        public const float GoldenRockSpeedMps = 85f;
        public static bool IsBoost(RaceItemType type) => type == RaceItemType.TurboPulse || type == RaceItemType.TripleTurbo || type == RaceItemType.HyperCharge || type == RaceItemType.MiniTurbo || type == RaceItemType.Lancer;
        // A short mid-pack item cannot be mistaken for launch fuel on a long jump route.
        public static bool IsShortcutBoost(RaceItemType type) => IsBoost(type) && type != RaceItemType.MiniTurbo && type != RaceItemType.Lancer;
        // Preserve all six installed-model encodings. New items use their mechanical
        // family in the existing scalar input; candidate policies remain Shadow.
        public static int LegacyFeatureFamily(int type) {
            switch ((RaceItemType)type) {
                case RaceItemType.PulseMine: case RaceItemType.TwinRock: return 3;
                case RaceItemType.TripleTurbo: case RaceItemType.MiniTurbo: case RaceItemType.Lancer: return 1;
                case RaceItemType.Coin10: return 0;
                case RaceItemType.TrackingDart: case RaceItemType.HomingRocket: case RaceItemType.GoldenRock: return 2;
                case RaceItemType.AerialRecovery: return 6;
                default: return type >= 0 && type <= 6 ? type : 0;
            }
        }
        public static string DisplayName(RaceItemType type) {
            switch (type) {
                case RaceItemType.TurboPulse: return "TURBO 2S";
                case RaceItemType.ElectroSpike: return "ROCK";
                case RaceItemType.KineticMissile: return "DART";
                case RaceItemType.PulseMine: return "PULSE MINE";
                case RaceItemType.TripleTurbo: return "TRIPLE TURBO";
                case RaceItemType.TwinRock: return "TWIN ROCK";
                case RaceItemType.TrackingDart: return "TRACKING DART";
                case RaceItemType.MagneticShield: return "NEON SHIELD";
                case RaceItemType.HyperCharge: return "SUPER TURBO";
                case RaceItemType.ShockwavePulse: return "SHOCKWAVE";
                case RaceItemType.HomingRocket: return "HOMING ROCKET";
                case RaceItemType.AerialRecovery: return "AERIAL RECOVERY";
                case RaceItemType.Coin10: return "COIN +10";
                case RaceItemType.MiniTurbo: return "MINI TURBO";
                case RaceItemType.Lancer: return "LANCER";
                case RaceItemType.GoldenRock: return "GOLDEN ROCK";
                default: return "CHARGE POD";
            }
        }
        public static Color ColorFor(RaceItemType type) {
            if (type == RaceItemType.Coin10) return new Color(1f,.80f,.06f);
            if (type == RaceItemType.Lancer) return new Color(1f,.32f,.055f);
            if (type == RaceItemType.GoldenRock) return new Color(1f,.74f,.04f);
            if (type == RaceItemType.MiniTurbo) return RacingDriftFeedback.Green;
            if (type == RaceItemType.ElectroSpike || type == RaceItemType.TwinRock) return new Color(1,.65f,.22f);
            if (type == RaceItemType.PulseMine || type == RaceItemType.HomingRocket) return new Color(1,.18f,.36f);
            if (type == RaceItemType.HyperCharge || type == RaceItemType.ShockwavePulse || type == RaceItemType.TrackingDart) return new Color(.85f,.28f,1);
            return RacingDriftFeedback.Cyan;
        }
    }
}
