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
        public static readonly Color Background=new Color(.017f,.030f,.060f), Surface=new Color(.035f,.060f,.105f,.96f), Highlight=new Color(.10f,.18f,.23f);
        public static readonly Color Cyan=new Color(.38f,.82f,1f), Secondary=new Color(.96f,.46f,.29f), Accent=new Color(.67f,.42f,1f);
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
            // Authored menus can contain several legacy EventSystems. Retain one
            // active input owner without deleting or modifying the saved scene.
            var systems=UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.InstanceID);
            EventSystem selected=EventSystem.current;
            if(selected==null||!selected.isActiveAndEnabled)
                foreach(var system in systems)if(system.isActiveAndEnabled){selected=system;break;}
            if(selected==null)
            {
                var system=new GameObject("Racing input",typeof(EventSystem),typeof(InputSystemUIInputModule));
                try { system.GetComponent<InputSystemUIInputModule>().AssignDefaultActions(); } catch {}
                selected=system.GetComponent<EventSystem>();
            }
            foreach(var system in systems)if(system!=selected)system.enabled=false;
            EventSystem.current=selected;
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
        public static RacingPanelGraphic CompetitivePanel(Transform parent,string name,float x,float y,float w,float h,Color accent)
        {
            var panel=Panel(parent,name,x,y,w,h,Surface,14);
            var detail=Area(panel.transform,"Circuit trim",0,0,1,1).gameObject.AddComponent<RacingCircuitTrimGraphic>();
            detail.color=accent;detail.raycastTarget=false;return panel;
        }
        public static Text Label(Transform parent,string value,RacingType role,float x,float y,float w,float h,Color? color=null,TextAnchor anchor=TextAnchor.MiddleLeft,bool display=false)
        {
            var rt=Area(parent,"Text "+value,x,y,w,h);var text=rt.gameObject.AddComponent<Text>();
            var markerType = Type.GetType("AnimeHillRacing.UI.StylizedBitmapFallbackMarker, Assembly-CSharp");
            if (markerType != null) rt.gameObject.AddComponent(markerType);
            text.font=Font;text.fontSize=Size(role);text.fontStyle=role==RacingType.Body||role==RacingType.Caption?FontStyle.Normal:FontStyle.Bold;
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
            if(fill.HasValue && fill.Value.r*.2126f+fill.Value.g*.7152f+fill.Value.b*.0722f>.55f)title.color=Background;
            if(label.Length>3 && label!="MEDIUM")
                Label(graphic.transform,">",RacingType.Section,.85f,.12f,.1f,.76f,Cyan,TextAnchor.MiddleCenter);
            graphic.gameObject.AddComponent<RacingButtonMotion>();
            if(action!=null)button.onClick.AddListener(()=>{RacingFeedback.Play(RacingCue.UiConfirm);action();});
            return button;
        }
        public static void Focus(Button button) { if(button!=null && EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(button.gameObject); }
        public static Color TierColor(int mmr) => RacingRankPalette.Primary((int)MmrTierUtility.GetTier(mmr));
        public static RacingRankBadge RankBadge(Transform parent,int mmr,float x,float y,float w,float h) {
            var badge=Area(parent,"Rank artwork "+MmrTierUtility.GetDisplayName(mmr),x,y,w,h).gameObject.AddComponent<RacingRankBadge>();badge.Configure(mmr);return badge;
        }
        public static Text Title(Transform parent,string id,int achievedMmr,float x,float y,float w,float h) {
            var text=Label(parent,"",RacingType.Caption,x,y,w,h);
            text.gameObject.AddComponent<RacingTitleView>().Configure(text,id,RacingProgressionService.State.currentSeasonNumber,achievedMmr);return text;
        }
        public static string VehicleName(string id)
        {
            switch(id) { case "formula-01":return RacingMetaVisuals.BodyName;case "rally-01":return "RALLY TURBO";case "buggy-01":return "DESERT BUGGY";case "sport-bike-01":return "SPORT BIKE";case "hill-adventurer-01":case "hill-adventurer":return "HILL ADVENTURER";case "aerial-satelite-01":return "ORBIT RUNNER";default:return "TRAIL JEEP"; }
        }
        public static string ItemName(RaceItemType item)
        {
            return RaceItemUtility.DisplayName(item);
        }
    }




}
