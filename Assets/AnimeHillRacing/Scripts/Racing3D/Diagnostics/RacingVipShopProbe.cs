#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using AnimeHillRacing.Ads;
using AnimeHillRacing.Core;
using AnimeHillRacing.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    public static class RacingVipShopProbe
    {
        // Explicit UI transport fixture. It never fulfills, grants, or bills.
        sealed class FixtureStore : IStorePurchaseProvider
        {
            public bool ready;
            public int purchases,restores;
            public string requested;
            public StorePurchaseStatus outcome=StorePurchaseStatus.Cancelled;
            Action changed;
            public int Subscribers=>changed?.GetInvocationList().Length??0;
            public bool IsConfigured=>true;
            public bool IsReady=>ready;
            public ExternalStoreState State=>ready?ExternalStoreState.Ready:ExternalStoreState.Failed;
            public string LastError=>"";
            public event Action StateChanged {add=>changed+=value;remove=>changed-=value;}
            public bool TryGetLocalizedPrice(string id,out string value){value=ready?"TEST PRICE":"";return ready;}
            public bool IsProductAvailable(string id)=>ready && id=="vip_monthly";
            public void Purchase(string id,Action<StorePurchaseResult> done){purchases++;requested=id;done(new StorePurchaseResult(outcome,""));}
            public void RestorePurchases(Action<StoreRestoreResult> done){restores++;done(new StoreRestoreResult(true,0,""));}
            public void Notify()=>changed?.Invoke();
        }
        public static IEnumerator Verify(ClosureContractReport report,string output)
        {
            var original=ExternalServiceProviders.Store;
            var section=GameSession.RequestedGarageSection;
            var ledger=AdEntitlementService.Ledger.Data;
            long vip=ledger.vipUntilUtc,adFree=ledger.adFreeUntilUtc;
            int handled=ledger.handledTransactions.Count;
            FixtureStore store=null;
            try
            {
                GameSession.RequestedGarageSection=GarageSection.Store;
                yield return SceneManager.LoadSceneAsync("Garage");
                yield return new WaitForSecondsRealtime(1);
                var view=UnityEngine.Object.FindFirstObjectByType<VipSubscriptionShopUI>(FindObjectsInactive.Include);
                report.Check(view!=null,"VIP subscription card is connected to the actual shared Garage shop");
                if(view==null)yield break;
                store=new FixtureStore();ExternalServiceProviders.Store=store;
                ledger.vipUntilUtc=0;view.Refresh();
                var buttons=view.GetComponentsInChildren<Button>(true);
                var buy=buttons.Single(b=>b.name=="Subscribe VIP");var restore=buttons.Single(b=>b.name=="Restore VIP");
                report.Check(!buy.interactable && !restore.interactable,"Disconnected billing cannot start VIP purchase or restoration");
                store.ready=true;store.Notify();view.Refresh();
                report.Check(buy.interactable && view.GetComponentsInChildren<Text>().Any(t=>t.name=="VIP Localized Price" && t.text=="TEST PRICE"),"Ready VIP card receives localized pricing from the store provider");
                buy.onClick.Invoke();
                report.Check(store.purchases==1 && store.requested=="vip_monthly" && buy.interactable,"Cancelled checkout uses vip_monthly and releases the purchase button");
                store.outcome=StorePurchaseStatus.Pending;buy.onClick.Invoke();buy.onClick.Invoke();
                report.Check(store.purchases==2 && !buy.interactable,"Pending store approval blocks repeated VIP checkout even when a callback is replayed");
                restore.onClick.Invoke();
                report.Check(store.restores==1 && ledger.vipUntilUtc==0 && ledger.adFreeUntilUtc==adFree && ledger.handledTransactions.Count==handled,"Cancelled, pending and restore UI callbacks cannot invent VIP expiry or thirty ad-free days");
                view.gameObject.SetActive(false);bool released=store.Subscribers==0;
                view.gameObject.SetActive(true);view.Refresh();
                report.Check(released && store.Subscribers==1,"VIP card releases store events while hidden and rebinds once when shown");
                buy.onClick.Invoke();
                report.Check(!buy.interactable && store.purchases==2,"A deferred VIP checkout remains blocked after hiding and reopening the card");
                // Native screenshot uses the actual platform provider, not TEST PRICE.
                ExternalServiceProviders.Store=original;ledger.vipUntilUtc=vip;view.Refresh();
                yield return null;
                string image=Path.Combine(output,"09-native-vip-shop.png");RacingEvidenceCapture.Capture(image);
                report.Check(File.Exists(image),"Native VIP shop evidence uses actual platform billing availability");
            }
            finally
            {
                ExternalServiceProviders.Store=original;GameSession.RequestedGarageSection=section;ledger.vipUntilUtc=vip;
            }
        }
    }
}
#endif
