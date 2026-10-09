using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    // Game-native faceted artwork: metallic wings, crystal, crown, live division/diamond labels.
    // It uses one UI mesh, no bitmap board, per-badge material, or animated rebuild during racing.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RacingRankBadge : MaskableGraphic
    {
        int rating = -1;
        Text diamond, division;
        public int Rating => rating;
        public void Configure(int mmr)
        {
            mmr = Mathf.Max(0, mmr); if (rating == mmr) return;
            rating = mmr; raycastTarget = false;
            if (diamond == null) {
                diamond = MakeText("Beyond diamond counter", .30f, .37f, .40f, .28f, 96);
                division = MakeText("Live rank division", .27f, .06f, .46f, .16f, 46);
            }
            bool beyond = mmr > MmrTierUtility.MaxTargetMmr;
            diamond.text = beyond ? MmrTierUtility.GetBeyondCounter(mmr).ToString() : "";
            diamond.color = new Color(.98f, 1f, 1f);
            division.text = beyond ? "APEX" : (int)MmrTierUtility.GetTier(mmr) % 2 == 0 ? "I" : "II";
            division.color = RacingDesign.Ink;
            SetVerticesDirty();
        }
        Text MakeText(string name, float x, float y, float w, float h, int size)
        {
            var text = RacingDesign.Area(transform, name, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = RacingDesign.Font; text.fontStyle = FontStyle.Bold; text.fontSize = size;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 10; text.resizeTextMaxSize = size;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.gameObject.AddComponent<Shadow>().effectColor = new Color(.01f,.03f,.09f,.95f);
            return text;
        }
        protected override void OnPopulateMesh(VertexHelper vh) { Populate(vh, rectTransform.rect, rating < 0 ? 0 : rating); }

        public static void Populate(VertexHelper vh, Rect rect, int mmr)
        {
            vh.Clear(); int band = (int)MmrTierUtility.GetTier(mmr), family = Mathf.Min(9, band / 2);
            bool beyond = band == 20; int evolution = band % 2;
            Color gem = RacingRankPalette.Primary(band), metal = RacingRankPalette.Trim(band);
            Color dark = new Color(.025f,.045f,.085f,1), silver = new Color(.78f,.87f,.95f,1);
            Vector2 center = rect.center + Vector2.up * rect.height * .02f;
            float scale = Mathf.Min(rect.width, rect.height) * .48f;
            Vector2 P(float x,float y) => center + new Vector2(x,y)*scale;
            void Polygon(Color c, params Vector2[] points) {
                int first = vh.currentVertCount; foreach (var point in points) vh.AddVert(P(point.x,point.y), c, Vector2.zero);
                for (int i=1;i<points.Length-1;i++) vh.AddTriangle(first,first+i,first+i+1);
            }
            void Stroke(Vector2 a,Vector2 b,float thickness,Color c) => RacingEmblem.Line(vh,P(a.x,a.y),P(b.x,b.y),thickness*scale,c);
            void Crystal(float cx,float cy,float w,float h,Color c) {
                Vector2 top=new Vector2(cx,cy+h),left=new Vector2(cx-w,cy),bottom=new Vector2(cx,cy-h),right=new Vector2(cx+w,cy),core=new Vector2(cx,cy+.06f*h);
                Polygon(Color.Lerp(c,Color.white,.58f),top,left,core);
                Polygon(Color.Lerp(c,dark,.28f),left,bottom,core);
                Polygon(c,bottom,right,core);
                Polygon(Color.Lerp(c,Color.white,.22f),right,top,core);
                Stroke(top,left,.014f,silver); Stroke(top,right,.012f,silver); Stroke(left,bottom,.012f,c); Stroke(right,bottom,.012f,c);
            }
            void Ring(float radius,int count,Color c,float thickness) {
                for(int i=0;i<count;i++) {
                    float a=(i+.5f)*Mathf.PI*2/count,b=(i+1.5f)*Mathf.PI*2/count;
                    Stroke(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,thickness,c);
                }
            }

            // Architecture accumulates across bands; Elite retains Expert's wing complexity.
            int wings = band < 2 ? 0 : 1 + band / 4;
            if (band >= 2) { Ring(.66f,12,Color.Lerp(metal,dark,.4f),.055f); Ring(.60f,12,gem,.021f); }
            for(int side=-1;side<=1;side+=2) for(int i=0;i<wings;i++) {
                float y=-.48f+i*.24f, reach=.57f+i*.055f;
                Vector2 a=new Vector2(side*.22f,y),b=new Vector2(side*reach,y+.24f),c=new Vector2(side*(reach+.15f),Mathf.Min(.86f,y+.63f)),d=new Vector2(side*.36f,y+.16f);
                Polygon(dark,a,b,c,d);
                Polygon(metal,a,b,c);
                Polygon(Color.Lerp(silver,metal,.25f),a,c,d);
                Stroke(b,c,.023f,gem); Stroke(a,c,.013f,new Color(1,1,1,.9f));
            }
            for(int i=0;i<band;i++) {
                float angle=(i*137.5f+20)*Mathf.Deg2Rad;
                float r=.70f+(i%3)*.038f;
                Vector2 a=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*r;
                Stroke(a,a*.94f,.018f,Color.Lerp(gem,metal,(i%2)*.5f));
            }
            if(band>=2) {
                // Each division earns another small gem in its upper collar. Higher
                // families retain them, keeping visible detail across band changes.
                for(int i=0;i<band-1;i++) {
                    float a=(195+i*150f/Mathf.Max(1,band-2))*Mathf.Deg2Rad;
                    Crystal(Mathf.Cos(a)*.88f,Mathf.Sin(a)*.88f,.028f,.045f,gem);
                }
                if(evolution==1)Ring(.83f,16,Color.Lerp(metal,gem,.35f),.018f);
            }
            if(family==0) {
                for(int i=0;i<1+evolution;i++) {
                    float y=.32f-i*.24f;
                    Polygon(dark,new Vector2(-.58f,y-.38f),new Vector2(0,y+.24f),new Vector2(.58f,y-.38f),new Vector2(.43f,y-.58f),new Vector2(0,y-.10f),new Vector2(-.43f,y-.58f));
                    Stroke(new Vector2(-.48f,y-.38f),new Vector2(0,y+.13f),.10f,silver); Stroke(new Vector2(0,y+.13f),new Vector2(.48f,y-.38f),.10f,silver);
                }
            } else {
                if(family==1) for(int i=0;i<4+evolution*4;i++) {
                    float a=i*Mathf.PI*2/(4+evolution*4);
                    Vector2 tip=new Vector2(Mathf.Sin(a),Mathf.Cos(a))*.88f;
                    Polygon(metal,tip,new Vector2(tip.x*.71f-.07f,tip.y*.71f),new Vector2(tip.x*.71f+.07f,tip.y*.71f));
                }
                if(family>=3) { Ring(.72f,16,metal,.022f); Crystal(0,.77f,.12f,.20f,metal); }
                if(family>=6) { Ring(.80f,24,new Color(gem.r,gem.g,gem.b,.65f),.011f); }
                if(family>=8) for(int i=0;i<3+evolution;i++) {
                    float x=(i-(2+evolution)*.5f)*.23f;
                    Polygon(metal,new Vector2(x-.08f,.71f),new Vector2(x,.98f-(Mathf.Abs(x)*.3f)),new Vector2(x+.08f,.71f));
                    Crystal(x,.77f,.04f,.075f,gem);
                }
                Crystal(0,.03f,family==5?.34f:.38f,family==5?.65f:.52f,gem);
                if(family==2||family==6||family>=8) {
                    Stroke(new Vector2(-.32f,-.05f),new Vector2(0,.44f),.024f,metal);
                    Stroke(new Vector2(0,.44f),new Vector2(.32f,-.05f),.024f,metal);
                }
                if(family==5) {
                    // Mountain facet of the two standalone emerald Elite references.
                    Stroke(new Vector2(-.23f,-.11f),new Vector2(-.02f,.18f),.03f,silver);
                    Stroke(new Vector2(-.02f,.18f),new Vector2(.15f,-.11f),.03f,silver);
                    Stroke(new Vector2(.02f,.02f),new Vector2(.14f,.12f),.02f,silver);
                    Stroke(new Vector2(.14f,.12f),new Vector2(.27f,-.11f),.02f,silver);
                }
                if(beyond) { Crystal(0,.03f,.47f,.45f,new Color(.15f,.78f,1)); Stroke(new Vector2(-.44f,0),new Vector2(0,-.40f),.04f,new Color(.9f,.25f,1)); }
            }
            Polygon(metal,new Vector2(-.38f,-.58f),new Vector2(.38f,-.58f),new Vector2(.30f,-.83f),new Vector2(0,-.96f),new Vector2(-.30f,-.83f));
            Polygon(dark,new Vector2(-.31f,-.62f),new Vector2(.31f,-.62f),new Vector2(.25f,-.80f),new Vector2(0,-.90f),new Vector2(-.25f,-.80f));
            Stroke(new Vector2(-.22f,-.91f),new Vector2(0,-1f),.02f,gem); Stroke(new Vector2(0,-1f),new Vector2(.22f,-.91f),.02f,gem);
        }
    }
}
