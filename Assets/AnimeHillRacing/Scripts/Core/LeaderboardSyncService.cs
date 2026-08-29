using System;
using System.Collections.Generic;
using System.Globalization;
using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.Core
{
    /// <summary>
    /// Keeps one submitted-best marker per map/leaderboard. Local SaveManager data remains the
    /// immediate source of truth; a larger local value is the entire pending queue.
    /// </summary>
    public static class LeaderboardSyncService
    {
        private sealed class Entry
        {
            public GameModeType Mode;
            public string MapId;
            public string LeaderboardId;
        }

        private const float RetryIntervalSeconds = 60f;
        private const string SubmittedKeyPrefix = "AHR.Leaderboard.SubmittedBestMeters.V1.";
        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
        private static readonly HashSet<string> InFlight = new HashSet<string>();
        private static IAuthenticationProvider authentication;
        private static ILeaderboardProvider leaderboards;
        private static float nextRetryTime;

        public static void Configure(GameCatalog catalog, IAuthenticationProvider authProvider,
            ILeaderboardProvider leaderboardProvider)
        {
            if (authentication != null) authentication.StateChanged -= HandleAuthenticationChanged;
            authentication = authProvider;
            leaderboards = leaderboardProvider;
            if (authentication != null) authentication.StateChanged += HandleAuthenticationChanged;

            Entries.Clear();
            InFlight.Clear();
            if (catalog?.maps != null)
                for (int i = 0; i < catalog.maps.Length; i++) RegisterMap(catalog.maps[i]);
            nextRetryTime = 0f;
        }

        public static void RegisterMap(MapDefinition map)
        {
            if (map == null || string.IsNullOrWhiteSpace(map.mapId)) return;
            string key = EntryKey(map.supportedGameMode, map.mapId);
            Entries[key] = new Entry
            {
                Mode = map.supportedGameMode,
                MapId = map.mapId,
                LeaderboardId = map.leaderboardId ?? string.Empty
            };
            TrySubmit(Entries[key]);
        }

        public static void RecordLocalBest(GameModeType mode, string mapId, float bestDistanceMeters)
        {
            if (string.IsNullOrWhiteSpace(mapId) || bestDistanceMeters < 0f) return;
            if (Entries.TryGetValue(EntryKey(mode, mapId), out Entry entry)) TrySubmit(entry);
        }

        public static void Tick(float unscaledTime)
        {
            if (unscaledTime < nextRetryTime) return;
            nextRetryTime = unscaledTime + RetryIntervalSeconds;
            if (authentication != null && authentication.IsAuthenticated) RetryPendingScores();
        }

        public static void RetryPendingScores()
        {
            if (leaderboards == null || authentication == null || !authentication.IsAuthenticated) return;
            Entry[] snapshot = new Entry[Entries.Count];
            Entries.Values.CopyTo(snapshot, 0);
            for (int i = 0; i < snapshot.Length; i++) TrySubmit(snapshot[i]);
        }

        public static long GetSubmittedBestMeters(GameModeType mode, string mapId, string leaderboardId)
        {
            string stored = PlayerPrefs.GetString(SubmittedKey(mode, mapId, leaderboardId), "0");
            return long.TryParse(stored, NumberStyles.None, CultureInfo.InvariantCulture, out long value)
                ? Math.Max(0L, value) : 0L;
        }

        private static void TrySubmit(Entry entry)
        {
            if (entry == null || leaderboards == null || authentication == null ||
                !authentication.IsAuthenticated || string.IsNullOrWhiteSpace(entry.LeaderboardId)) return;

            string entryKey = EntryKey(entry.Mode, entry.MapId);
            if (InFlight.Contains(entryKey)) return;
            long localBest = DistanceToScore(SaveManager.GetBestDistance(entry.Mode, entry.MapId));
            long submittedBest = GetSubmittedBestMeters(entry.Mode, entry.MapId, entry.LeaderboardId);
            if (localBest <= submittedBest) return;

            InFlight.Add(entryKey);
            leaderboards.SubmitScore(entry.MapId, entry.LeaderboardId, localBest, succeeded =>
            {
                InFlight.Remove(entryKey);
                if (!succeeded) return;
                string submittedKey = SubmittedKey(entry.Mode, entry.MapId, entry.LeaderboardId);
                long previous = GetSubmittedBestMeters(entry.Mode, entry.MapId, entry.LeaderboardId);
                if (localBest <= previous) return;
                PlayerPrefs.SetString(submittedKey, localBest.ToString(CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
            });
        }

        private static void HandleAuthenticationChanged()
        {
            if (authentication != null && authentication.IsAuthenticated) RetryPendingScores();
        }

        private static long DistanceToScore(float distanceMeters)
        {
            double safeDistance = Math.Max(0d, distanceMeters);
            if (safeDistance >= long.MaxValue) return long.MaxValue;
            return (long)Math.Round(safeDistance, MidpointRounding.AwayFromZero);
        }

        private static string EntryKey(GameModeType mode, string mapId) =>
            ((int)mode).ToString(CultureInfo.InvariantCulture) + "|" + (mapId ?? string.Empty);

        private static string SubmittedKey(GameModeType mode, string mapId, string leaderboardId) =>
            SubmittedKeyPrefix + StableHash(EntryKey(mode, mapId) + "|" + (leaderboardId ?? string.Empty));

        private static string StableHash(string value)
        {
            unchecked
            {
                const ulong offset = 14695981039346656037UL;
                const ulong prime = 1099511628211UL;
                ulong hash = offset;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= prime;
                }
                return hash.ToString("X16", CultureInfo.InvariantCulture);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            if (authentication != null) authentication.StateChanged -= HandleAuthenticationChanged;
            authentication = null;
            leaderboards = null;
            Entries.Clear();
            InFlight.Clear();
            nextRetryTime = 0f;
        }
    }
}
