using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed class MatchmakingService
    {
        public const int RequiredLobbySize = 12;
        public const int AiOpponentsCount = 11;

        private static readonly string[] BotNamePool = new[]
        {
            "Apex_Kite", "VoltRacer", "NovaShift", "TurboZen", "ShadowDrift",
            "IronClad", "SpeedDemon", "BlazeRider", "AeroAce", "PixelRunner",
            "PhantomK", "EchoStrike", "ViperGT", "CyberTrack", "DriftKing99",
            "PulseDrive", "StormSurge", "Hyperion", "RedlineFox", "ZeroG_Rider",
            "VelocityX", "NightRaven", "TorqueMaster", "CircuitGhost"
        };

        private static readonly string[] StarterVehicles = new[] { "car-01", "hill-adventurer" };
        private static readonly string[] MidVehicles = new[] { "car-01", "rally-01", "buggy-01", "hill-adventurer" };
        private static readonly string[] AdvancedVehicles = new[] { "rally-01", "buggy-01", "sport-bike-01", "formula-01" };
        private static readonly string[] ApexVehicles = new[] { "formula-01", "sport-bike-01", "rally-01", "aerial-satelite-01" };

        private static readonly string[] StandardWheels = new[] { "wheel-default", "wheel-sport", "wheel-offroad" };
        private static readonly string[] ApexWheels = new[] { "wheel-sport", "wheel-racing", "wheel-carbon", "wheel-plasma" };

        /// <summary>
        /// Generates a competitive 12-racer lobby around the player's MMR.
        /// Guaranteed: exactly 1 human, exactly 11 AI bots.
        /// </summary>
        public static RacerIdentity[] GenerateSeededTestLobby(
            int playerMmr,
            string playerVehicleId,
            string playerWheelId,
            string playerDisplayName = "PLAYER",
            int seed = 0)
        {
            var rng = new DeterministicRng(seed == 0 ? (int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF) : seed);

            var lobby = new RacerIdentity[RequiredLobbySize];

            // 1. Setup Human Racer
            lobby[0] = new RacerIdentity(
                id: "player_human",
                name: string.IsNullOrWhiteSpace(playerDisplayName) ? "PLAYER" : playerDisplayName,
                vehicle: string.IsNullOrWhiteSpace(playerVehicleId) ? "car-01" : playerVehicleId,
                wheel: string.IsNullOrWhiteSpace(playerWheelId) ? "wheel-default" : playerWheelId,
                rating: Mathf.Max(0, playerMmr),
                human: true,
                seed: rng.Next(1, 999999),
                grid: 1 // Temporarily 1; will be assigned grid position below
            );

            // 2. Compute spread parameters based on MMR tier
            // Lower MMR: narrow cluster (~120-200 stdDev). Higher MMR: wider natural spread (~300-500).
            float baseSpread;
            bool eliteRanked = playerMmr >= 9700 && playerMmr <= 10000;
            if (eliteRanked) baseSpread = 90f;
            else if (playerMmr < 2000) baseSpread = 140f;
            else if (playerMmr < 500) baseSpread = 220f;
            else if (playerMmr < 1500) baseSpread = 380f;
            else if (playerMmr < 4000) baseSpread = 750f;
            else if (playerMmr < 7000) baseSpread = 1400f;
            else if (playerMmr < 10000) baseSpread = 2200f;
            else baseSpread = 3000f; // Apex uncapped

            // Pick 11 unique names from the bot pool
            List<string> availableNames = new List<string>(BotNamePool);
            ShuffleList(availableNames, rng);

            // 3. Generate 11 AI Rivals with continuous MMR probability and diversity guard
            int[] rivalMmrs = new int[AiOpponentsCount];
            MetaBuildCategory[] categories = new MetaBuildCategory[AiOpponentsCount];
            float[] probs = new float[AiOpponentsCount];
            int metaCount = 0;
            float[] targetDeltas = new float[]
            {
                -2.2f, -1.6f, -1.1f, -0.6f, -0.2f,
                0.2f,  0.7f,  1.2f,  1.7f,  2.1f,
                0.0f
            };

            for (int i = 0; i < AiOpponentsCount; i++)
            {
                float tierOffset = targetDeltas[i] * (baseSpread * 0.45f);
                float randomJitter = rng.NextGaussian(0f, baseSpread * 0.35f);
                int rivalMmr = Mathf.Max(0, Mathf.RoundToInt(playerMmr + tierOffset + randomJitter));
                if (eliteRanked) rivalMmr = Mathf.Clamp(rivalMmr, 9700, 10000);
                rivalMmrs[i] = rivalMmr;

                categories[i] = MetaProgressionConfig.RollBuildCategory(rivalMmr, rng, out float p);
                probs[i] = p;
                if (categories[i] == MetaBuildCategory.Meta) metaCount++;
            }

            // High-MMR Convergence Calibration:
            // At high MMR (70k+), lobbies strongly converge toward META and Near-META setups.
            // Category rolls remain probabilistic; upgrade only remaining beginner builds.
            if (playerMmr >= 7000)
            {
                // Convert any remaining Standard builds to NearMeta or Specialist
                for (int i = 0; i < AiOpponentsCount; i++)
                {
                    if (categories[i] == MetaBuildCategory.Standard)
                    {
                        categories[i] = rng.NextFloat() < .85f ? MetaBuildCategory.NearMeta : MetaBuildCategory.Specialist;
                    }
                }
            }

            // A seeded benchmark adoption roll gives 7.5k+ grids ten or eleven exact
            // META opponents. Lower grids retain their individual build probabilities.
            float eliteConvergence = Mathf.InverseLerp(5000, 7500, playerMmr);
            if (rng.NextFloat() < eliteConvergence)
            {
                float allMetaChance = Mathf.Lerp(.78f, .94f, Mathf.InverseLerp(7500, 12500, playerMmr));
                int specialist = rng.NextFloat() < allMetaChance ? -1 : rng.Next(AiOpponentsCount);
                for (int i = 0; i < AiOpponentsCount; i++)
                    categories[i] = i == specialist ? MetaBuildCategory.Specialist : MetaBuildCategory.Meta;
            }
            else if (playerMmr >= 5000 && metaCount == 0)
            {
                // Extreme zero guard at mid-to-high MMR
                int highestIdx = 0;
                for (int i = 1; i < AiOpponentsCount; i++)
                {
                    if (rivalMmrs[i] > rivalMmrs[highestIdx]) highestIdx = i;
                }
                categories[highestIdx] = MetaBuildCategory.Meta;
            }

            for (int i = 0; i < AiOpponentsCount; i++)
            {
                int rivalMmr = rivalMmrs[i];
                string botVehicle;
                string botWheel;
                Color botPaint;
                string buildTitle;

                switch (categories[i])
                {
                    case MetaBuildCategory.Meta:
                        botVehicle = MetaProgressionConfig.MetaVehicleId;
                        botWheel = MetaProgressionConfig.MetaWheelId;
                        botPaint = RacingVehicleFactory.MetaNavy;
                        buildTitle = "DAPEX / WHITY";
                        break;
                    case MetaBuildCategory.NearMeta:
                        var nearPool = playerMmr >= 9000 ? MetaProgressionConfig.CompetitiveAlternativePool : MetaProgressionConfig.NearMetaBuildPool;
                        var nearDef = nearPool[rng.Next(nearPool.Length)];
                        botVehicle = nearDef.vehicleId;
                        botWheel = nearDef.wheelId;
                        botPaint = nearDef.paintColor;
                        buildTitle = nearDef.buildName;
                        break;
                    case MetaBuildCategory.Specialist:
                        var specDef = MetaProgressionConfig.SpecialistBuildPool[rng.Next(MetaProgressionConfig.SpecialistBuildPool.Length)];
                        botVehicle = specDef.vehicleId;
                        botWheel = specDef.wheelId;
                        botPaint = specDef.paintColor;
                        buildTitle = specDef.buildName;
                        break;
                    default:
                        var stdDef = MetaProgressionConfig.StandardBuildPool[rng.Next(MetaProgressionConfig.StandardBuildPool.Length)];
                        botVehicle = stdDef.vehicleId;
                        botWheel = stdDef.wheelId;
                        botPaint = stdDef.paintColor;
                        buildTitle = stdDef.buildName;
                        break;
                }

                string botName = (i < availableNames.Count) ? availableNames[i] : $"Rival_{i + 1}";
                int botPersonalitySeed = rng.Next(1, 9999999);

                lobby[i + 1] = new RacerIdentity(
                    id: $"ai_racer_{i + 1}",
                    name: botName,
                    vehicle: botVehicle,
                    wheel: botWheel,
                    rating: rivalMmr,
                    human: false,
                    seed: botPersonalitySeed,
                    grid: i + 2
                )
                {
                    buildCategory = categories[i],
                    metaProbabilityUsed = probs[i],
                    paintColor = botPaint,
                    buildName = buildTitle
                };
            }

            // Assign human setup: Formula Apex is current competitive META
            bool humanIsFormula = RacingStatsCalculator.NormalizeVehicleId(lobby[0].vehicleId) == MetaProgressionConfig.MetaVehicleId;
            bool humanHasMetaWheels = lobby[0].wheelId == MetaProgressionConfig.MetaWheelId || lobby[0].wheelId == MetaProgressionConfig.MetaWheelVisualId;
            bool humanIsMeta = humanIsFormula && humanHasMetaWheels;
            lobby[0].buildCategory = humanIsMeta ? MetaBuildCategory.Meta : (humanIsFormula ? MetaBuildCategory.NearMeta : MetaBuildCategory.Specialist);
            lobby[0].paintColor = humanIsFormula ? RacingVehicleFactory.MetaNavy : RacingDesign.Cyan;
            lobby[0].buildName = humanIsMeta ? "DAPEX / WHITY" : (humanIsFormula ? "DAPEX" : "Custom Setup");
            lobby[0].metaProbabilityUsed = MetaProgressionConfig.EvaluateMetaProbability(playerMmr);

            // 4. Assign Starting Grid Positions (1 to 12)
            AssignGridPositions(lobby, rng);

            return lobby;
        }

        // Production opponents are selected from saved careers, never invented for a race.
        public static RacerIdentity[] GenerateRankedLobby(int playerMmr, string playerVehicleId, string playerWheelId, string playerDisplayName = "PLAYER", int seed = 0)
        {
            if (seed == 0) seed = unchecked((int)(DateTime.UtcNow.Ticks & int.MaxValue));
            var rng = new DeterministicRng(seed);
            var selected = RacingProgressionService.Engine.SelectOpponents(playerMmr, seed, true);
            var lobby = new RacerIdentity[12];
            var human = RacingProgressionService.State.human;
            lobby[0] = new RacerIdentity(human.id, playerDisplayName, playerVehicleId, playerWheelId, playerMmr, true, rng.Next(1, 999999), 1) { titleId = human.selectedTitleId, characterId=human.characterPreference, accessoryId=human.accessoryPreference };
            bool humanMeta = RacingStatsCalculator.NormalizeVehicleId(playerVehicleId) == MetaProgressionConfig.MetaVehicleId && RacingStatsCalculator.NormalizeWheelId(playerWheelId) == MetaProgressionConfig.MetaWheelId;
            lobby[0].buildCategory = humanMeta ? MetaBuildCategory.Meta : MetaBuildCategory.Specialist;
            lobby[0].buildName = humanMeta ? "DAPEX / WHITY" : "Custom Setup";
            lobby[0].paintColor = humanMeta ? RacingVehicleFactory.MetaNavy : RacingDesign.Cyan;
            for (int i = 0; i < selected.Length; i++) {
                var bot = selected[i];
                string vehicle = bot.vehiclePreference;
                string wheel = bot.wheelPreference;
                bool meta = vehicle == MetaProgressionConfig.MetaVehicleId && wheel == MetaProgressionConfig.MetaWheelId;
                lobby[i + 1] = new RacerIdentity(bot.id, bot.displayName, vehicle, wheel, bot.currentMmr, false, bot.personalitySeed, i + 2) {
                    titleId = bot.selectedTitleId, preferredStyle = bot.profile.preferredStyle, hasPersistentStyle = true, characterId=bot.characterPreference, accessoryId=bot.accessoryPreference,
                    buildCategory = meta ? MetaBuildCategory.Meta : MetaBuildCategory.Standard,
                    metaProbabilityUsed = MetaProgressionConfig.EvaluateMetaProbability(bot.currentMmr),
                    paintColor = meta ? RacingVehicleFactory.MetaNavy : Color.HSVToRGB((bot.personalitySeed % 1000) / 1000f, .65f, .65f),
                    buildName = meta ? "DAPEX / WHITY" : RacingDesign.VehicleName(vehicle)
                };
            }
            AssignGridPositions(lobby, rng);
            RacingProgressionService.MarkChanged(); RacingProgressionService.Save();
            return lobby;
        }

        private static string PickVehicleForMmr(int mmr, DeterministicRng rng)
        {
            string[] pool;
            if (mmr < 2000) pool = StarterVehicles;
            else if (mmr < 5000) pool = MidVehicles;
            else if (mmr < 8000) pool = AdvancedVehicles;
            else pool = ApexVehicles;

            return pool[rng.Next(pool.Length)];
        }

        private static string PickWheelForMmr(int mmr, DeterministicRng rng)
        {
            string[] pool = (mmr >= 7000) ? ApexWheels : StandardWheels;
            return pool[rng.Next(pool.Length)];
        }

        private static void AssignGridPositions(RacerIdentity[] lobby, DeterministicRng rng)
        {
            // Create list of grid slots 1..12 and shuffle
            int[] slots = new int[RequiredLobbySize];
            for (int i = 0; i < RequiredLobbySize; i++) slots[i] = i + 1;

            // Fisher-Yates shuffle
            for (int i = slots.Length - 1; i > 0; i--)
            {
                int swapIdx = rng.Next(i + 1);
                int temp = slots[i];
                slots[i] = slots[swapIdx];
                slots[swapIdx] = temp;
            }

            for (int i = 0; i < lobby.Length; i++)
            {
                lobby[i].gridPosition = slots[i];
            }
        }

        private static void ShuffleList<T>(List<T> list, DeterministicRng rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIdx = rng.Next(i + 1);
                T temp = list[i];
                list[i] = list[swapIdx];
                list[swapIdx] = temp;
            }
        }
    }
}
