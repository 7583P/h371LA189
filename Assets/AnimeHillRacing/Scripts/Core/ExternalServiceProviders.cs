using System;

namespace AnimeHillRacing.Core
{
    public interface ILeaderboardProvider
    {
        bool IsConfigured { get; }
        bool TryShowLeaderboard(string mapId, string leaderboardId, out string unavailableReason);
    }

    public interface IStorePurchaseProvider
    {
        bool IsConfigured { get; }
        bool TryGetLocalizedPrice(string productId, out string localizedPrice);
        void Purchase(string productId, Action<StorePurchaseResult> completed);
    }

    public readonly struct StorePurchaseResult
    {
        public readonly bool Succeeded;
        public readonly string Error;

        public StorePurchaseResult(bool succeeded, string error)
        {
            Succeeded = succeeded;
            Error = error ?? string.Empty;
        }
    }

    public static class ExternalServiceProviders
    {
        private sealed class UnconfiguredLeaderboards : ILeaderboardProvider
        {
            public bool IsConfigured => false;

            public bool TryShowLeaderboard(string mapId, string leaderboardId,
                out string unavailableReason)
            {
                unavailableReason = string.IsNullOrWhiteSpace(leaderboardId)
                    ? "LEADERBOARD ID NOT CONFIGURED"
                    : "GOOGLE PLAY GAMES NOT CONFIGURED";
                return false;
            }
        }

        private sealed class UnconfiguredPurchases : IStorePurchaseProvider
        {
            public bool IsConfigured => false;

            public bool TryGetLocalizedPrice(string productId, out string localizedPrice)
            {
                localizedPrice = string.Empty;
                return false;
            }

            public void Purchase(string productId, Action<StorePurchaseResult> completed) =>
                completed?.Invoke(new StorePurchaseResult(false,
                    string.IsNullOrWhiteSpace(productId)
                        ? "PRODUCT ID NOT CONFIGURED"
                        : "GOOGLE PLAY BILLING NOT CONFIGURED"));
        }

        private static readonly ILeaderboardProvider UnconfiguredLeaderboardProvider =
            new UnconfiguredLeaderboards();
        private static readonly IStorePurchaseProvider UnconfiguredStoreProvider =
            new UnconfiguredPurchases();
        private static ILeaderboardProvider leaderboards = UnconfiguredLeaderboardProvider;
        private static IStorePurchaseProvider store = UnconfiguredStoreProvider;

        public static ILeaderboardProvider Leaderboards
        {
            get => leaderboards;
            set => leaderboards = value ?? UnconfiguredLeaderboardProvider;
        }

        public static IStorePurchaseProvider Store
        {
            get => store;
            set => store = value ?? UnconfiguredStoreProvider;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetProviders()
        {
            leaderboards = UnconfiguredLeaderboardProvider;
            store = UnconfiguredStoreProvider;
        }
    }
}
