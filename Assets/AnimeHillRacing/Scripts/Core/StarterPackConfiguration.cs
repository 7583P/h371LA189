using UnityEngine;

namespace AnimeHillRacing.Core
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Starter Pack Configuration",
        fileName = "StarterPackConfiguration")]
    public sealed class StarterPackConfiguration : ScriptableObject
    {
        [Tooltip("Real Google Play product ID. Intentionally empty until Play Console is configured.")]
        public string googlePlayProductId;
        [Tooltip("PremiumBundleCatalog bundle ID for the exclusive vehicle/skin shown by Starter_Pack.")]
        public string exclusiveBundleId;
        [Min(0)] public int coinReward = 100000;
        [Min(0)] public int gemReward = 1000;
        [Tooltip("Concept price shown only while no localized store price is available.")]
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
