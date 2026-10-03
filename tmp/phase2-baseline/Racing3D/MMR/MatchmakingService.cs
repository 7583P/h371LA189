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
        public static RacerIdentity[] GenerateRankedLobby(
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
            if (playerMmr < 2000) baseSpread = 140f;
            else if (playerMmr < 5000) baseSpread = 220f;
            else if (playerMmr < 8000) baseSpread = 320f;
            else if (playerMmr < 10000) baseSpread = 420f;
            else baseSpread = 520f; // Apex uncapped

            // Pick 11 unique names from the bot pool
            List<string> availableNames = new List<string>(BotNamePool);
            ShuffleList(availableNames, rng);

            // 3. Generate 11 AI Rivals
            // 5 bots slightly below/at player, 5 bots slightly above/at player, 1 close rival
            // Staggered offsets with Gaussian jitter to create a lifelike distribution
            float[] targetDeltas = new float[]
            {
                -2.2f, -1.6f, -1.1f, -0.6f, -0.2f, // Below player
                0.2f,  0.7f,  1.2f,  1.7f,  2.1f,  // Above player
                0.0f                                // Peer rival
            };

            for (int i = 0; i < AiOpponentsCount; i++)
            {
                float tierOffset = targetDeltas[i] * (baseSpread * 0.45f);
                float randomJitter = rng.NextGaussian(0f, baseSpread * 0.35f);
                int rivalMmr = Mathf.Max(0, Mathf.RoundToInt(playerMmr + tierOffset + randomJitter));

                // Vehicle & Wheel selection progression by bot MMR
                string botVehicle = PickVehicleForMmr(rivalMmr, rng);
                string botWheel = PickWheelForMmr(rivalMmr, rng);
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
                );
            }

            // 4. Assign Starting Grid Positions (1 to 12)
            // Deterministic grid assignment: can be based on qualifying MMR or randomized
            // By default in Ranked, racers are ordered randomly or by MMR with human placed fairly
            AssignGridPositions(lobby, rng);

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
