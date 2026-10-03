using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using AnimeHillRacing.UI;

namespace AnimeHillRacing.Racing3D
{
    public enum RacingType { Display, Title, Section, Body, Caption, Stat, Timer, Position, Mmr }
    public static class RacingDesign
    {
        public static readonly Color Background=new Color(.027f,.055f,.095f), Surface=new Color(.052f,.09f,.14f,.96f), Highlight=new Color(.10f,.18f,.23f);
        public static readonly Color Cyan=new Color(.18f,.91f,.91f), Secondary=new Color(.96f,.46f,.29f), Accent=new Color(.67f,.42f,1f);
        public static readonly Color Success=new Color(.43f,.93f,.67f), Danger=new Color(1f,.30f,.35f), Warning=new Color(1f,.71f,.29f);
        public static readonly Color Gold=new Color(1f,.80f,.37f), Platinum=new Color(.73f,.87f,.96f), Apex=new Color(.82f,.55f,1f), Ink=new Color(.96f,.95f,.89f), Muted=new Color(.58f,.70f,.76f);
        static Font font;
        public static Font Font => font != null ? font : font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static int Size(RacingType role) => role==RacingType.Display?68:role==RacingType.Title?42:role==RacingType.Section?26:role==RacingType.Body?20:role==RacingType.Caption?15:role==RacingType.Position?88:role==RacingType.Mmr?54:role==RacingType.Stat?30:24;
        public static RectTransform Canvas(string name,int order=20)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=order;
            var scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1600,900); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight=1f;
            if(EventSystem.current==null)
            {
                var system=new GameObject("Racing input",typeof(EventSystem),typeof(InputSystemUIInputModule));
                system.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var safe=Rect(go.transform,"Safe area",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            safe.gameObject.AddComponent<RacingSafeArea>();
            return safe;
        }
        public static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max,Vector2 insetMin,Vector2 insetMax)
        {
            var go=new GameObject(name,typeof(RectTransform)); var rt=(RectTransform)go.transform;
            rt.SetParent(parent,false); rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=insetMin;rt.offsetMax=insetMax;return rt;
        }
        public static RectTransform Area(Transform parent,string name,float x,float y,float w,float h,float margin=0)
            => Rect(parent,name,new Vector2(x,y),new Vector2(x+w,y+h),Vector2.one*margin,-Vector2.one*margin);
        public static RacingPanelGraphic Panel(Transform parent,string name,float x,float y,float w,float h,Color color,float cut=18)
        {
            var rt=Area(parent,name,x,y,w,h);var graphic=rt.gameObject.AddComponent<RacingPanelGraphic>();graphic.color=color;graphic.Cut=cut;graphic.raycastTarget=false;return graphic;
        }
        public static Text Label(Transform parent,string value,RacingType role,float x,float y,float w,float h,Color? color=null,TextAnchor anchor=TextAnchor.MiddleLeft,bool display=false)
        {
            var rt=Area(parent,"Text "+value,x,y,w,h);var text=rt.gameObject.AddComponent<Text>();
            rt.gameObject.AddComponent<StylizedBitmapFallbackMarker>();text.font=Font;text.fontSize=Size(role);text.fontStyle=role==RacingType.Body||role==RacingType.Caption?FontStyle.Normal:FontStyle.Bold;
            text.color=color??Ink;text.text=value;text.alignment=anchor;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            text.resizeTextForBestFit=true;text.resizeTextMinSize=Mathf.RoundToInt(text.fontSize*.72f);text.resizeTextMaxSize=text.fontSize;
            if(display)
            {
                var lettering=Rect(rt,"Adventure lettering",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
                lettering.gameObject.AddComponent<CanvasRenderer>();
                var overlay=lettering.gameObject.AddComponent<RacingDisplayGraphic>();
                overlay.raycastTarget=false;overlay.Configure(text,text.color);text.color=new Color(1,1,1,0);
            }
            return text;
        }
        public static Button Button(Transform parent,string label,float x,float y,float w,float h,Action action,Color? fill=null)
        {
            var graphic=Panel(parent,label,x,y,w,h,fill??Highlight);graphic.raycastTarget=true;
            var button=graphic.gameObject.AddComponent<Button>();button.targetGraphic=graphic;button.transition=Selectable.Transition.None;
            var navigation=button.navigation;navigation.mode=Navigation.Mode.Automatic;button.navigation=navigation;
            var title=Label(graphic.transform,label,RacingType.Section,.08f,.13f,.78f,.74f,Ink);
            if(label.Length>3 && label!="MEDIUM")
                Label(graphic.transform,">",RacingType.Section,.85f,.12f,.1f,.76f,Cyan,TextAnchor.MiddleCenter);
            graphic.gameObject.AddComponent<RacingButtonMotion>();
            if(action!=null)button.onClick.AddListener(()=>{RacingFeedback.Play(RacingCue.UiConfirm);action();});
            return button;
        }
        public static void Focus(Button button) { if(button!=null && EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(button.gameObject); }
        public static Color TierColor(int mmr) => mmr>=10000?Apex:mmr>=7000?Platinum:mmr>=4000?Gold:Cyan;
        public static string VehicleName(string id)
        {
            switch(id) { case "formula-01":return "FORMULA APEX";case "rally-01":return "RALLY TURBO";case "buggy-01":return "DESERT BUGGY";case "sport-bike-01":return "SPORT BIKE";case "hill-adventurer-01":case "hill-adventurer":return "HILL ADVENTURER";case "aerial-satelite-01":return "ORBIT RUNNER";default:return "TRAIL JEEP"; }
        }
        public static string ItemName(RaceItemType item)
        {
            switch(item){case RaceItemType.TurboPulse:return "TURBO PULSE";case RaceItemType.KineticMissile:return "KINETIC DART";case RaceItemType.ElectroSpike:return "ELECTRO SPIKE";case RaceItemType.MagneticShield:return "MAGNETIC SHIELD";case RaceItemType.ShockwavePulse:return "SHOCKWAVE";case RaceItemType.HyperCharge:return "HYPER CHARGE";default:return "CHARGE POD";}
        }
    }
    public sealed class RacingSafeArea:MonoBehaviour
    {
        Rect previous;int width,height;
        void LateUpdate()
        {
            Rect safe=Screen.safeArea;if(safe==previous&&width==Screen.width&&height==Screen.height)return;
            previous=safe;width=Screen.width;height=Screen.height;
            var rt=(RectTransform)transform;rt.anchorMin=new Vector2(safe.x/Mathf.Max(1,width),safe.y/Mathf.Max(1,height));rt.anchorMax=new Vector2(safe.xMax/Mathf.Max(1,width),safe.yMax/Mathf.Max(1,height));rt.offsetMin=rt.offsetMax=Vector2.zero;
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RacingPanelGraphic:MaskableGraphic
    {
        public float Cut=18;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect r=rectTransform.rect;float c=Mathf.Min(Cut,Mathf.Min(r.width,r.height)*.2f);
            Vector2[] p={new Vector2(r.xMin+c,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax-c,r.yMax),new Vector2(r.xMin,r.yMax),new Vector2(r.xMin,r.yMin+c)};
            vh.AddVert(r.center,color,Vector2.zero);
            for(int i=0;i<p.Length;i++){Color tint=Color.Lerp(color,Color.white,.055f*Mathf.InverseLerp(r.yMin,r.yMax,p[i].y));tint.a=color.a;vh.AddVert(p[i],tint,Vector2.zero);}
            for(int i=0;i<p.Length;i++)vh.AddTriangle(0,i+1,(i+1)%p.Length+1);
        }
    }
    public sealed class RacingButtonMotion:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler
    {
        bool hover,focus,pressed;float value;Graphic graphic;Color baseColor;Button button;
        void Awake(){graphic=GetComponent<Graphic>();baseColor=graphic.color;button=GetComponent<Button>();}
        void Update()
        {
            float target=button.interactable&&(hover||focus)?1:0;value=Mathf.Lerp(value,target,1-Mathf.Exp(-Time.unscaledDeltaTime*18));
            transform.localScale=Vector3.one*(pressed?.965f:1f+value*.018f);graphic.color=Color.Lerp(baseColor,RacingDesign.Highlight+new Color(.04f,.08f,.08f,0),value);
        }
        public void OnPointerEnter(PointerEventData e){hover=true;RacingFeedback.Play(RacingCue.UiFocus);}
        public void OnPointerExit(PointerEventData e){hover=false;pressed=false;}
        public void OnPointerDown(PointerEventData e){pressed=true;}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        public void OnSelect(BaseEventData e){focus=true;}
        public void OnDeselect(BaseEventData e){focus=false;pressed=false;}
    }
    [DisallowMultipleComponent]
    public sealed class RacingScreenMotion:MonoBehaviour
    {
        CanvasGroup group;float age;
        void Awake(){group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();Restart();}
        public void Restart(){age=0;if(group!=null)group.alpha=0;enabled=true;}
        void Update(){age+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(age/.24f);group.alpha=1-Mathf.Pow(1-t,3);transform.localScale=Vector3.one*Mathf.Lerp(.985f,1,group.alpha);if(t>=1)enabled=false;}
    }
}
