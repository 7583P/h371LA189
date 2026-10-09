using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using AnimeHillRacing.UI;

namespace AnimeHillRacing.Racing3D
{
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
}
