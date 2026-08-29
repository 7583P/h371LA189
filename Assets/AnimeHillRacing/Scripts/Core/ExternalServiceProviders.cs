using System;

namespace AnimeHillRacing.Core
{
    public enum ExternalAuthenticationState
    {
        NotInitialized,
        Authenticating,
        Authenticated,
        Failed
    }

    public enum ExternalStoreState
    {
        NotInitialized,
        Initializing,
        Ready,
        Failed
    }

    public enum StorePurchaseStatus
    {
        Succeeded,
        Cancelled,
        Pending,
        Failed
    }

    public interface IAuthenticationProvider
    {
        bool IsAvailable { get; }
        bool IsConfigured { get; }
        bool IsAuthenticated { get; }
        ExternalAuthenticationState State { get; }
        string LastError { get; }
        event Action StateChanged;
        void Authenticate(bool interactive);
    }

    public interface ILeaderboardProvider
    {
        bool IsConfigured { get; }
        bool IsAuthenticated { get; }
        bool TryShowLeaderboard(string mapId, string leaderboardId, out string unavailableReason);
        void SubmitScore(string mapId, string leaderboardId, long scoreMeters, Action<bool> completed);
    }

    public interface IStorePurchaseProvider
    {
        bool IsConfigured { get; }
        bool IsReady { get; }
        ExternalStoreState State { get; }
        string LastError { get; }
        event Action StateChanged;
        bool TryGetLocalizedPrice(string productId, out string localizedPrice);
        bool IsProductAvailable(string productId);
        void Purchase(string productId, Action<StorePurchaseResult> completed);
        void RestorePurchases(Action<StoreRestoreResult> completed);
    }

    public readonly struct StorePurchaseResult
    {
        public readonly StorePurchaseStatus Status;
        public readonly string Error;
        public bool Succeeded => Status == StorePurchaseStatus.Succeeded;

        public StorePurchaseResult(StorePurchaseStatus status, string error)
        {
            Status = status;
            Error = error ?? string.Empty;
        }

        public StorePurchaseResult(bool succeeded, string error) : this(
            succeeded ? StorePurchaseStatus.Succeeded : StorePurchaseStatus.Failed, error)
        {
        }
    }

    public readonly struct StoreRestoreResult
    {
        public readonly bool Succeeded;
        public readonly int EntitlementsRestored;
        public readonly string Error;

        public StoreRestoreResult(bool succeeded, int entitlementsRestored, string error)
        {
            Succeeded = succeeded;
            EntitlementsRestored = Math.Max(0, entitlementsRestored);
            Error = error ?? string.Empty;
        }
    }

    public static class ExternalServiceProviders
    {
        private sealed class UnconfiguredGooglePlay : IAuthenticationProvider, ILeaderboardProvider
        {
            public bool IsAvailable => false;
            public bool IsConfigured => false;
            public bool IsAuthenticated => false;
            public ExternalAuthenticationState State => ExternalAuthenticationState.NotInitialized;
            public string LastError => "GOOGLE PLAY GAMES NOT CONFIGURED";
            public event Action StateChanged { add { } remove { } }

            public void Authenticate(bool interactive)
            {
            }

            public bool TryShowLeaderboard(string mapId, string leaderboardId,
                out string unavailableReason)
            {
                unavailableReason = string.IsNullOrWhiteSpace(leaderboardId)
                    ? "LEADERBOARD ID NOT CONFIGURED"
                    : "GOOGLE PLAY GAMES NOT CONFIGURED";
                return false;
            }

            public void SubmitScore(string mapId, string leaderboardId, long scoreMeters,
                Action<bool> completed) => completed?.Invoke(false);
        }

        private sealed class UnconfiguredPurchases : IStorePurchaseProvider
        {
            public bool IsConfigured => false;
            public bool IsReady => false;
            public ExternalStoreState State => ExternalStoreState.NotInitialized;
            public string LastError => "GOOGLE PLAY BILLING NOT CONFIGURED";
            public event Action StateChanged { add { } remove { } }

            public bool TryGetLocalizedPrice(string productId, out string localizedPrice)
            {
                localizedPrice = string.Empty;
                return false;
            }

            public bool IsProductAvailable(string productId) => false;

            public void Purchase(string productId, Action<StorePurchaseResult> completed) =>
                completed?.Invoke(new StorePurchaseResult(StorePurchaseStatus.Failed,
                    string.IsNullOrWhiteSpace(productId)
                        ? "PRODUCT ID NOT CONFIGURED"
                        : "GOOGLE PLAY BILLING NOT CONFIGURED"));

            public void RestorePurchases(Action<StoreRestoreResult> completed) =>
                completed?.Invoke(new StoreRestoreResult(false, 0,
                    "GOOGLE PLAY BILLING NOT CONFIGURED"));
        }

        private static readonly UnconfiguredGooglePlay UnconfiguredGooglePlayProvider =
            new UnconfiguredGooglePlay();
        private static readonly IStorePurchaseProvider UnconfiguredStoreProvider =
            new UnconfiguredPurchases();
        private static IAuthenticationProvider authentication = UnconfiguredGooglePlayProvider;
        private static ILeaderboardProvider leaderboards = UnconfiguredGooglePlayProvider;
        private static IStorePurchaseProvider store = UnconfiguredStoreProvider;

        public static IAuthenticationProvider Authentication
        {
            get => authentication;
            set => authentication = value ?? UnconfiguredGooglePlayProvider;
        }

        public static ILeaderboardProvider Leaderboards
        {
            get => leaderboards;
            set => leaderboards = value ?? UnconfiguredGooglePlayProvider;
        }

        public static IStorePurchaseProvider Store
        {
            get => store;
            set => store = value ?? UnconfiguredStoreProvider;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetProviders()
        {
            authentication = UnconfiguredGooglePlayProvider;
            leaderboards = UnconfiguredGooglePlayProvider;
            store = UnconfiguredStoreProvider;
        }
    }
}
