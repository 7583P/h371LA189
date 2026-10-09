using AnimeHillRacing.Ads;
using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class VipSubscriptionShopUI : MonoBehaviour
    {
        IStorePurchaseProvider provider,lastProvider;
        Button buy,restore;
        Text price,status;
        bool built,busy,subscribed,awaitingApproval;
        float nextRefresh;
        string feedback="";

        public static VipSubscriptionShopUI Attach(GameObject storePanel)
        {
            if(storePanel==null || !Application.isPlaying)return null;
            Transform content=null;
            foreach(var child in storePanel.GetComponentsInChildren<Transform>(true))
                if(child.name=="Shop Content"){content=child;break;}
            if(content==null)return null;
            var existing=content.Find("VIP Monthly Runtime");
            var root=existing!=null?existing.gameObject:new GameObject("VIP Monthly Runtime",typeof(RectTransform),typeof(Image),typeof(LayoutElement));
            if(existing==null){root.transform.SetParent(content,false);root.transform.SetSiblingIndex(0);}
            var view=root.GetComponent<VipSubscriptionShopUI>()??root.AddComponent<VipSubscriptionShopUI>();
            view.Build();view.Refresh();return view;
        }

        void OnEnable(){Build();Refresh();}
        void OnDisable(){Unsubscribe();}
        void OnDestroy(){Unsubscribe();}
        void Update()
        {
            if(Time.unscaledTime<nextRefresh)return;
            nextRefresh=Time.unscaledTime+30;Refresh();
        }
        void Subscribe()
        {
            if(!isActiveAndEnabled)return;
            var next=ExternalServiceProviders.Store;
            if(!ReferenceEquals(provider,next)){
                if(provider!=null)provider.StateChanged-=Refresh;
                // Hiding and reopening the card must retain a deferred checkout.
                // Reset it only when the underlying store provider actually changes.
                if(!ReferenceEquals(lastProvider,next)){feedback="";awaitingApproval=false;}
                lastProvider=next;
                provider=next;if(provider!=null)provider.StateChanged+=Refresh;
            }
            if(!subscribed){AdEntitlementService.Changed+=Refresh;subscribed=true;}
        }
        void Unsubscribe()
        {
            if(provider!=null)provider.StateChanged-=Refresh;provider=null;
            if(subscribed)AdEntitlementService.Changed-=Refresh;subscribed=false;
        }
        public void Refresh()
        {
            if(!built)return;Subscribe();
            var store=ExternalServiceProviders.Store;
            string product=AdEntitlementService.VipProductId;
            bool active=AdEntitlementService.IsVipActive;
            if(active){awaitingApproval=false;feedback="";}
            bool available=store!=null && store.IsReady && store.IsProductAvailable(product);
            price.text=store!=null && store.TryGetLocalizedPrice(product,out string localized) && !string.IsNullOrWhiteSpace(localized)?localized:
                store!=null && (store.State==ExternalStoreState.Initializing || store.State==ExternalStoreState.NotInitialized)?"LOADING...":"UNAVAILABLE";
            buy.interactable=!busy && !awaitingApproval && !active && available;
            restore.interactable=!busy && store!=null && store.IsReady;
            if(busy)status.text="CONTACTING GOOGLE PLAY...";
            else if(!string.IsNullOrEmpty(feedback))status.text=feedback;
            else if(active)status.text="VIP ACTIVE / ADS DISABLED";
            else if(AdEntitlementService.IsAdFree)status.text="PURCHASE AD-FREE PERIOD ACTIVE";
            else status.text=available?"SUBSCRIBE OR RESTORE YOUR MEMBERSHIP":"VIP UNAVAILABLE IN THIS STORE";
        }
        void Buy()
        {
            var store=ExternalServiceProviders.Store;string product=AdEntitlementService.VipProductId;
            if(busy || awaitingApproval || AdEntitlementService.IsVipActive || store==null || !store.IsReady || !store.IsProductAvailable(product))return;
            busy=true;feedback="";Refresh();
            store.Purchase(product,result=>{
                if(this==null)return;busy=false;
                awaitingApproval=result.Status==StorePurchaseStatus.Pending;
                feedback=result.Succeeded?"PURCHASE CONFIRMED / ENTITLEMENTS UPDATED":
                    result.Status==StorePurchaseStatus.Cancelled?"PURCHASE CANCELLED":
                    result.Status==StorePurchaseStatus.Pending?"PURCHASE PENDING APPROVAL":"PURCHASE NOT COMPLETED";
                Refresh();
            });
        }
        void Restore()
        {
            var store=ExternalServiceProviders.Store;if(busy || store==null || !store.IsReady)return;
            busy=true;feedback="";Refresh();
            store.RestorePurchases(result=>{
                if(this==null)return;busy=false;feedback=result.Succeeded?"RESTORE COMPLETE":"RESTORE NOT COMPLETED";Refresh();
            });
        }
        void Build()
        {
            if(built)return;built=true;
            GetComponent<Image>().color=new Color(.025f,.075f,.14f,.98f);
            var layout=GetComponent<LayoutElement>();layout.preferredHeight=270;layout.flexibleHeight=0;
            var title=Label("VIP Title","VIP MONTHLY",30,.04f,.77f,.60f,.18f);title.color=new Color(.25f,.9f,1);title.fontStyle=FontStyle.Bold;
            Label("VIP Terms","Monthly subscription, automatically renews.\nNo ads while VIP is active. Each confirmed purchase adds 30 ad-free days.",17,.04f,.39f,.92f,.34f);
            price=Label("VIP Localized Price","LOADING...",23,.64f,.77f,.32f,.18f);price.color=new Color(1,.86f,.28f);price.alignment=TextAnchor.MiddleRight;
            buy=ActionButton("Subscribe VIP","SUBSCRIBE",.04f,.17f,.45f,.18f);buy.onClick.AddListener(Buy);
            restore=ActionButton("Restore VIP","RESTORE PURCHASES",.51f,.17f,.45f,.18f);restore.onClick.AddListener(Restore);
            status=Label("VIP Status","",14,.04f,.02f,.92f,.12f);status.color=new Color(.65f,.82f,.94f);
        }
        RectTransform Area(string name,Transform parent,float x,float y,float w,float h)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=new Vector2(x,y);rect.anchorMax=new Vector2(x+w,y+h);rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        Text Label(string name,string value,int size,float x,float y,float w,float h)
        {
            var text=Area(name,transform,x,y,w,h).gameObject.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text=value;text.fontSize=size;text.color=Color.white;text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;return text;
        }
        Button ActionButton(string name,string caption,float x,float y,float w,float h)
        {
            var rect=Area(name,transform,x,y,w,h);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.045f,.16f,.27f,1);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var text=Area("Label",rect,0,0,1,1).gameObject.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text=caption;text.fontSize=18;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return button;
        }
    }
}
