using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AnimeHillRacing.Ads
{
    [Serializable]
    public sealed class AdEntitlementData
    {
        public int version = 1;
        public long adFreeUntilUtc, vipUntilUtc, lastKnownUtcTimestamp;
        // One digest per paid transaction is retained to reject replayed callbacks after restart.
        public List<string> handledTransactions = new List<string>();
    }

    public sealed class AdEntitlementLedger
    {
        public AdEntitlementData Data { get; }
        public AdEntitlementLedger(AdEntitlementData data = null) { Data = data ?? new AdEntitlementData(); if (Data.handledTransactions == null) Data.handledTransactions = new List<string>(); }
        public long AcceptUtc(long now) { Data.lastKnownUtcTimestamp = Math.Max(now, Data.lastKnownUtcTimestamp); return Data.lastKnownUtcTimestamp; }
        public bool IsVipActive(long now) => AcceptUtc(now) < Data.vipUntilUtc;
        public bool IsAdFree(long now) { now = AcceptUtc(now); return now < Data.vipUntilUtc || now < Data.adFreeUntilUtc; }

        public bool CompleteQualifyingPurchase(string transactionId, long now)
        {
            if (string.IsNullOrWhiteSpace(transactionId)) return false;
            string digest;
            using (var hash = SHA256.Create()) digest = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(transactionId))).Replace("-", "");
            if (Data.handledTransactions.Contains(digest)) return false;
            now = AcceptUtc(now);
            long activation = Math.Max(now, Data.adFreeUntilUtc);
            long duration = TimeSpan.FromDays(30).Ticks;
            Data.adFreeUntilUtc = activation > DateTime.MaxValue.Ticks - duration ? DateTime.MaxValue.Ticks : activation + duration;
            Data.handledTransactions.Add(digest); return true;
        }

        // Only the store adapter supplies verified subscription expiry. No season code calls this.
        public void UpdateVipExpiry(long expiry) { Data.vipUntilUtc = Math.Max(0, Math.Min(DateTime.MaxValue.Ticks, expiry)); }
    }
}
