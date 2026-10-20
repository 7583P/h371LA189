#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using AnimeHillRacing.Racing3D.Experiments;

namespace AnimeHillRacing.EditorTools
{
    // This mesh and its text live only in an editor preview scene. A level of
    // 970 uses the same five vertices as level 0; no rank textures are generated.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AfterPreviewBadge : MaskableGraphic
    {
        Text counter;
        public string Counter => counter==null?"":counter.text;
        public bool ShowsLevel {get;private set;}
        public void Configure(int mmr,bool ranked,bool qualified,AfterScale scale)
        {
            if(counter==null){
                var node=new GameObject("Dynamic experimental level",typeof(RectTransform),typeof(Text));node.transform.SetParent(transform,false);
                counter=node.GetComponent<Text>();counter.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");counter.fontSize=58;
                counter.alignment=TextAnchor.MiddleCenter;counter.resizeTextForBestFit=true;counter.resizeTextMinSize=16;counter.resizeTextMaxSize=58;counter.raycastTarget=false;
                var rect=counter.rectTransform;rect.anchorMin=new Vector2(.15f,.25f);rect.anchorMax=new Vector2(.85f,.75f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            }
            int level=-1;ShowsLevel=ranked&&qualified&&scale.TryLevel(mmr,out level);
            counter.text=ShowsLevel?level.ToString():"";counter.color=Color.white;raycastTarget=false;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vertices)=>Populate(vertices,rectTransform.rect,ShowsLevel);
        public static void Populate(VertexHelper vertices,Rect rect,bool visible)
        {
            vertices.Clear();if(!visible)return;
            var center=rect.center;float w=rect.width*.44f,h=rect.height*.46f;
            vertices.AddVert(center,new Color(.10f,.28f,.50f),Vector2.zero);
            vertices.AddVert(center+Vector2.up*h,new Color(.65f,.97f,1),Vector2.zero);
            vertices.AddVert(center+Vector2.right*w,new Color(.10f,.70f,1),Vector2.zero);
            vertices.AddVert(center-Vector2.up*h,new Color(.12f,.22f,.65f),Vector2.zero);
            vertices.AddVert(center-Vector2.right*w,new Color(.35f,.86f,.98f),Vector2.zero);
            for(int i=1;i<=4;i++)vertices.AddTriangle(0,i,i==4?1:i+1);
        }
    }
}
#endif
