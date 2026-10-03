using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public enum MetaBuildCategory
    {
        Standard,
        Specialist,
        NearMeta,
        Meta
    }

    [Serializable]
    public struct VehicleBuildDefinition
    {
        public string vehicleId;
        public string wheelId;
        public string buildName;
        public Color paintColor;

        public VehicleBuildDefinition(string vehicle, string wheel, string name, Color color)
        {
            vehicleId = vehicle;
            wheelId = wheel;
            buildName = name;
            paintColor = color;
        }
    }

    /// <summary>
    /// Authoritative configuration and continuous probability evaluation for the Ranked META system.
    /// Drives visual identity, probability adoption curve, and lobby diversity guarantees.
    /// </summary>
    public static class MetaProgressionConfig
    {
        // Authoritative Meta Specification: Formula Apex
        public const string MetaVehicleId = "formula-01";
        public const string MetaWheelId = "wheel-plasma";
        public const string MetaWheelVisualId = "wheel-luxury-black";

        // Elite lobbies may be completely META; the human choice is always preserved.
        public const int MinimumNonMetaRacers = 0;
        public const int MaximumExactMetaRacers = 11;

        // Alternative High-Performance Near-META Builds
        public static readonly VehicleBuildDefinition[] NearMetaBuildPool = new[]
        {
            new VehicleBuildDefinition("rally-01", "wheel-plasma", "Rally Apex", new Color(0.20f, 0.88f, 0.55f)),
            new VehicleBuildDefinition("formula-01", "wheel-racing", "Formula Pro", new Color(0.12f, 0.85f, 0.95f)),
            new VehicleBuildDefinition("sport-bike-01", "wheel-racing", "Apex Carver Pro", new Color(0.95f, 0.38f, 0.15f)),
            new VehicleBuildDefinition("aerial-satelite-01", "wheel-plasma", "Orbit Turbo", new Color(0.25f, 0.65f, 0.92f)),
            new VehicleBuildDefinition("formula-01", "wheel-carbon", "Monocoque GT", new Color(0.55f, 0.22f, 0.88f))
        };

        public static readonly VehicleBuildDefinition[] CompetitiveAlternativePool = new[]
        {
            NearMetaBuildPool[0], NearMetaBuildPool[2], NearMetaBuildPool[3]
        };

        // Competitive Specialist Builds (Offroad, Extreme Drift, Stability)
        public static readonly VehicleBuildDefinition[] SpecialistBuildPool = new[]
        {
            new VehicleBuildDefinition("buggy-01", "wheel-offroad", "Offroad Brawler", new Color(0.88f, 0.62f, 0.12f)),
            new VehicleBuildDefinition("sport-bike-01", "wheel-sport", "Apex Carver", new Color(0.95f, 0.38f, 0.15f)),
            new VehicleBuildDefinition("aerial-satelite-01", "wheel-racing", "Orbit Specialist", new Color(0.25f, 0.65f, 0.92f))
        };

        // Standard Builds for lower MMR entry lobbies
        public static readonly VehicleBuildDefinition[] StandardBuildPool = new[]
        {
            new VehicleBuildDefinition("car-01", "wheel-default", "Standard Tourer", new Color(0.22f, 0.58f, 0.82f)),
            new VehicleBuildDefinition("rally-01", "wheel-default", "Rally Cross", new Color(0.85f, 0.45f, 0.18f)),
            new VehicleBuildDefinition("buggy-01", "wheel-default", "Dune Explorer", new Color(0.68f, 0.72f, 0.25f))
        };

        // Continuous piecewise knots (MMR, Probability)
        // Individual adoption rises from 6% at 10k to 82% at 50k and 99.8% at
        // 100k. Matchmaking adds seeded elite convergence to 10 or 11 exact builds.
        private static readonly float[] MmrKnots = new float[]
        {
            0f, 10000f, 20000f, 30000f, 40000f, 50000f, 60000f, 70000f, 80000f, 90000f, 100000f, 125000f
        };

        private static readonly float[] ProbKnots = new float[]
        {
            0.00f, 0.06f, 0.16f, 0.30f, 0.55f, 0.82f, 0.92f, 0.97f, 0.985f, 0.995f, 0.998f, 0.999f
        };

        /// <summary>
        /// Evaluates the continuous probability of an AI racer adopting the Exact META build based on MMR.
        /// Guaranteed continuous interpolation with zero discrete jumps.
        /// </summary>
        public static float EvaluateMetaProbability(float mmr, string trackCharacteristics = "")
        {
            if (mmr <= MmrKnots[0]) return ProbKnots[0];
            int n = MmrKnots.Length;
            if (mmr >= MmrKnots[n - 1]) return ProbKnots[n - 1];

            for (int i = 0; i < n - 1; i++)
            {
                if (mmr >= MmrKnots[i] && mmr <= MmrKnots[i + 1])
                {
                    float t = (mmr - MmrKnots[i]) / (MmrKnots[i + 1] - MmrKnots[i]);
                    float prob = Mathf.Lerp(ProbKnots[i], ProbKnots[i + 1], t);

                    // Track-aware modification hook
                    if (!string.IsNullOrEmpty(trackCharacteristics))
                    {
                        if (trackCharacteristics.Contains("technical")) prob *= 0.96f;
                        else if (trackCharacteristics.Contains("offroad")) prob *= 0.94f;
                    }

                    return Mathf.Clamp01(prob);
                }
            }

            return ProbKnots[n - 1];
        }

        /// <summary>
        /// Rolls the initial build category for an individual racer based on MMR.
        /// </summary>
        public static MetaBuildCategory RollBuildCategory(int mmr, DeterministicRng rng, out float metaProb)
        {
            metaProb = EvaluateMetaProbability(mmr);
            float roll = rng.NextFloat();

            if (roll < metaProb)
            {
                return MetaBuildCategory.Meta;
            }

            // Distribute remaining non-meta probability dynamically
            float nearMetaShare = Mathf.Lerp(0.20f, 0.70f, Mathf.Clamp01(mmr / 70000f));
            float specialistShare = Mathf.Lerp(0.15f, 0.25f, Mathf.Clamp01(mmr / 50000f));

            float subRoll = rng.NextFloat();
            if (subRoll < nearMetaShare)
            {
                return MetaBuildCategory.NearMeta;
            }
            if (subRoll < nearMetaShare + specialistShare)
            {
                return MetaBuildCategory.Specialist;
            }

            return MetaBuildCategory.Standard;
        }
    }
}
