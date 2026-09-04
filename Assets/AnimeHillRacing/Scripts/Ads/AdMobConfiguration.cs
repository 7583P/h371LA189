using UnityEngine;

namespace AnimeHillRacing.Ads
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/AdMob Configuration",
        fileName = "AdMobConfiguration")]
    public sealed class AdMobConfiguration : ScriptableObject
    {
        // Google-provided sample IDs. They are not associated with a publisher account and are
        // safe for development. Production IDs remain serialized only in this asset.
        public const string AndroidTestAppId = "ca-app-pub-3940256099942544~3347511713";
        public const string AndroidTestRewardedId = "ca-app-pub-3940256099942544/5224354917";
        public const string AndroidTestInterstitialId = "ca-app-pub-3940256099942544/1033173712";
        public const string AndroidTestAdaptiveBannerId = "ca-app-pub-3940256099942544/9214589741";
        public const string IosTestAppId = "ca-app-pub-3940256099942544~1458002511";
        public const string IosTestRewardedId = "ca-app-pub-3940256099942544/1712485313";
        public const string IosTestInterstitialId = "ca-app-pub-3940256099942544/4411468910";
        public const string IosTestAdaptiveBannerId = "ca-app-pub-3940256099942544/2435281174";

        [Header("Safety")]
        [Tooltip("Keep enabled for development. Disable only after every production ID below is configured.")]
        public bool useTestAds = true;

        [Header("Android Production IDs")]
        [Tooltip("AdMob app ID (contains a ~), not an ad-unit ID.")]
        public string androidAppId;
        public string androidRewardedAdUnitId;
        public string androidInterstitialAdUnitId;
        public string androidBannerAdUnitId;

        [Header("iOS Production IDs")]
        [Tooltip("AdMob app ID (contains a ~), not an ad-unit ID.")]
        public string iosAppId;
        public string iosRewardedAdUnitId;
        public string iosInterstitialAdUnitId;
        public string iosBannerAdUnitId;

        public string AppId => useTestAds
            ? (UseIosIds ? IosTestAppId : AndroidTestAppId)
            : (UseIosIds ? iosAppId : androidAppId);

        public string RewardedAdUnitId => useTestAds
            ? (UseIosIds ? IosTestRewardedId : AndroidTestRewardedId)
            : (UseIosIds ? iosRewardedAdUnitId : androidRewardedAdUnitId);

        public string InterstitialAdUnitId => useTestAds
            ? (UseIosIds ? IosTestInterstitialId : AndroidTestInterstitialId)
            : (UseIosIds ? iosInterstitialAdUnitId : androidInterstitialAdUnitId);

        public string BannerAdUnitId => useTestAds
            ? (UseIosIds ? IosTestAdaptiveBannerId : AndroidTestAdaptiveBannerId)
            : (UseIosIds ? iosBannerAdUnitId : androidBannerAdUnitId);

        public string AndroidAppId => useTestAds ? AndroidTestAppId : androidAppId;
        public string IosAppId => useTestAds ? IosTestAppId : iosAppId;

        public bool ValidateCurrentPlatform(out string error)
        {
            if (string.IsNullOrWhiteSpace(AppId))
            {
                error = $"{PlatformName} AdMob app ID is not configured";
                return false;
            }
            if (string.IsNullOrWhiteSpace(RewardedAdUnitId) ||
                string.IsNullOrWhiteSpace(InterstitialAdUnitId) ||
                string.IsNullOrWhiteSpace(BannerAdUnitId))
            {
                error = $"One or more {PlatformName} AdMob ad-unit IDs are not configured";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static string PlatformName => UseIosIds ? "iOS" : "Android";

        private static bool UseIosIds
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
    }
}
