using UnityEngine;

namespace AnimeHillRacing.Core
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Starter Pack Configuration",
        fileName = "StarterPackConfiguration")]
    public sealed class StarterPackConfiguration : ScriptableObject
    {
        [Header("Project Catalog")]
        [Tooltip("Existing Phase 1 catalog used to discover maps, vehicles and explicitly configured IAP skins.")]
        public GameCatalog gameCatalog;

        [Header("Google Play Games")]
        [Tooltip("Attempts the non-blocking automatic Play Games v2 sign-in once at startup.")]
        public bool autoAuthenticate = true;
        [Tooltip("Keep disabled until Saved Games is enabled and tested in Play Console.")]
        public bool enableCloudSave;
        [Tooltip("Stable snapshot filename. This is not a Google credential or resource ID.")]
        public string cloudSaveFilename = "hill-adventurer-save-v1";

        [Header("Starter Pack IAP")]
        [Tooltip("Real Google Play product ID. Intentionally empty until Play Console is configured.")]
        public string googlePlayProductId;
        [Tooltip("Local vehicle ID shown in Starter_Pack artwork. This is not a Google Product ID.")]
        public string exclusiveVehicleId;
        [Tooltip("Local skin ID shown in Starter_Pack artwork. This is not a Google Product ID.")]
        public string exclusiveSkinId;
        [Tooltip("Local wheel ID included with the Starter Pack vehicle.")]
        public string exclusiveWheelId;
        [Tooltip("Global character accessory ID included with the Starter Pack.")]
        public string exclusiveAccessoryId;
        [Tooltip("Legacy Phase 1 PremiumBundleCatalog ID. Used only when the direct local IDs above are empty.")]
        public string exclusiveBundleId;
        [Min(0)] public int coinReward = 500000;
        [Min(0)] public int gemReward = 1000;
        [Tooltip("Design reference only. Production UI waits for the localized Google Play price.")]
        public string conceptualPrice = "$2.99";

        private static StarterPackConfiguration cached;

        public static StarterPackConfiguration Instance
        {
            get
            {
                if (cached == null)
                    cached = Resources.Load<StarterPackConfiguration>("StarterPackConfiguration");
                return cached;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => cached = null;
    }
}
