using UnityEngine;

namespace AnimeHillRacing.Ads
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Ads/Entitlement Configuration")]
    public sealed class AdEntitlementConfiguration : ScriptableObject
    {
        [Tooltip("Existing real store subscription product ID. Leave empty until configured in Google Play; never use a made-up production ID.")]
        public string vipSubscriptionProductId = "";
    }
}
