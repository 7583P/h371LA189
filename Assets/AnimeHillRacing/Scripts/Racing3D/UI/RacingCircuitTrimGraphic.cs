using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using AnimeHillRacing.UI;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RacingCircuitTrimGraphic:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect r=rectTransform.rect;
            RacingEmblem.Line(vh,new Vector2(r.xMin+18,r.yMax-2),new Vector2(r.xMax-28,r.yMax-2),2,color);
            RacingEmblem.Line(vh,new Vector2(r.xMax-28,r.yMax-2),new Vector2(r.xMax-2,r.yMax-28),2,color);
            for(int i=0;i<3;i++)RacingEmblem.Line(vh,new Vector2(r.xMin+10+i*10,r.yMin+10),new Vector2(r.xMin+16+i*10,r.yMin+16),2,color);
        }
    }
}
