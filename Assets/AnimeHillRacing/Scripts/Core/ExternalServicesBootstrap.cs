using UnityEngine;

namespace AnimeHillRacing.Core
{
    /// <summary>Single runtime composition root for the Phase 1 external-service adapters.</summary>
    public sealed class ExternalServicesBootstrap : MonoBehaviour
    {
        private static ExternalServicesBootstrap instance;
        private GooglePlayCloudSaveService cloudSave;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (instance != null) return;
            StarterPackConfiguration configuration = StarterPackConfiguration.Instance;
            GameCatalog catalog = configuration != null ? configuration.gameCatalog : null;

            GooglePlayGamesProvider googlePlay = new GooglePlayGamesProvider();
            UnityIapPurchaseProvider store = new UnityIapPurchaseProvider(catalog, configuration);
            ExternalServiceProviders.Authentication = googlePlay;
            ExternalServiceProviders.Leaderboards = googlePlay;
            ExternalServiceProviders.Store = store;
            LeaderboardSyncService.Configure(catalog, googlePlay, googlePlay);

            GameObject root = new GameObject("Hill Adventurer External Services");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<ExternalServicesBootstrap>();
            instance.cloudSave = new GooglePlayCloudSaveService(catalog, configuration, googlePlay);

            if (configuration == null || configuration.autoAuthenticate)
                googlePlay.Authenticate(false);
            store.Initialize();
        }

        private void Update()
        {
            LeaderboardSyncService.Tick(Time.unscaledTime);
            cloudSave?.Tick(Time.unscaledTime);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => instance = null;
    }
}
