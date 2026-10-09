using System;
using UnityEngine;

namespace AnimeHillRacing.Ads
{
    public static class AdEntitlementService
    {
        const string Key = "AHR.AdEntitlements.V1";
        static AdEntitlementLedger ledger;
        static string vipProductId;
        public static event Action Changed;
        public static AdEntitlementLedger Ledger { get { EnsureLoaded(); return ledger; } }
        public static string VipProductId { get { EnsureLoaded(); return vipProductId; } }
        public static bool IsAdFree => Ledger.IsAdFree(DateTime.UtcNow.Ticks);
        public static bool IsVipActive => Ledger.IsVipActive(DateTime.UtcNow.Ticks);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { ledger = null; vipProductId = null; Changed = null; }

        static void EnsureLoaded()
        {
            if (ledger != null) return;
            string json = PlayerPrefs.GetString(Key, "");
            AdEntitlementData data = null;
            if (!string.IsNullOrEmpty(json)) {
                try { data = JsonUtility.FromJson<AdEntitlementData>(json); }
                catch (ArgumentException exception) { Debug.LogError("[Ads] Ad entitlement save could not be read: " + exception.Message); }
            }
            ledger = new AdEntitlementLedger(data);
            var config = Resources.Load<AdEntitlementConfiguration>("AdEntitlementConfiguration");
            vipProductId = config != null ? config.vipSubscriptionProductId : "";
        }

        internal static void CompleteConfirmedPurchase(string transactionId)
        {
            if (!Ledger.CompleteQualifyingPurchase(transactionId, DateTime.UtcNow.Ticks)) return;
            Persist(); Changed?.Invoke();
        }

        internal static void UpdateStoreVipExpiry(long expiry)
        {
            Ledger.UpdateVipExpiry(expiry); Persist(); Changed?.Invoke();
        }

        public static void Persist()
        {
            if (ledger == null) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(ledger.Data)); PlayerPrefs.Save();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static void DiagnosticVip(TimeSpan duration) { Ledger.UpdateVipExpiry(DateTime.UtcNow.Add(duration).Ticks); Persist(); Changed?.Invoke(); }
        public static void DiagnosticPurchase() { CompleteConfirmedPurchase("DEVELOPMENT-ONLY-" + Guid.NewGuid().ToString("N")); }
        public static void DiagnosticExpire() { Ledger.Data.vipUntilUtc = Ledger.Data.adFreeUntilUtc = 0; Persist(); Changed?.Invoke(); }
#endif
    }
}
