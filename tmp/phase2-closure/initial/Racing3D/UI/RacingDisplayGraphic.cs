using AnimeHillRacing.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RacingDisplayGraphic : MaskableGraphic
    {
        private Text source;
        private Color ink;
        public override Texture mainTexture => StylizedBitmapFont.Material.mainTexture;

        public void Configure(Text label, Color visibleColor)
        {
            source=label;ink=visibleColor;material=StylizedBitmapFont.Material;
            raycastTarget=false;SetAllDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(source==null)return;
            string[] lines=source.text.Split('\n');Rect r=rectTransform.rect;
            float glyphHeight=Mathf.Min(source.fontSize*.84f,r.height/(lines.Length*1.26f));
            for(int line=0;line<lines.Length;line++)
            {
                float units=0;
                foreach(char c in lines[line])units+=char.IsWhiteSpace(c)?.36f:StylizedBitmapFont.TryGetUv(c,out Rect uv)?StylizedBitmapFont.GetGlyphAspect(uv)+.09f:.5f;
                float h=Mathf.Min(glyphHeight,r.width/Mathf.Max(1,units));float width=units*h;
                bool centered=source.alignment==TextAnchor.MiddleCenter||source.alignment==TextAnchor.UpperCenter||source.alignment==TextAnchor.LowerCenter;
                bool right=source.alignment==TextAnchor.MiddleRight||source.alignment==TextAnchor.UpperRight||source.alignment==TextAnchor.LowerRight;
                float x=centered?r.center.x-width*.5f:right?r.xMax-width:r.xMin;
                float y=r.center.y+(lines.Length-1)*glyphHeight*.63f-line*glyphHeight*1.26f-h*.5f;
                foreach(char c in lines[line])
                {
                    if(char.IsWhiteSpace(c)){x+=h*.36f;continue;}
                    if(!StylizedBitmapFont.TryGetUv(c,out Rect uv)){x+=h*.5f;continue;}
                    float w=StylizedBitmapFont.GetGlyphAspect(uv)*h;int i=vh.currentVertCount;
                    vh.AddVert(new Vector3(x,y),ink,new Vector2(uv.xMin,uv.yMin));
                    vh.AddVert(new Vector3(x,y+h),ink,new Vector2(uv.xMin,uv.yMax));
                    vh.AddVert(new Vector3(x+w,y+h),ink,new Vector2(uv.xMax,uv.yMax));
                    vh.AddVert(new Vector3(x+w,y),ink,new Vector2(uv.xMax,uv.yMin));
                    vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);x+=w+h*.09f;
                }
            }
        }
    }
}
