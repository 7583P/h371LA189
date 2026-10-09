using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RacingLoadingCarGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect r=rectTransform.rect;
            // The Iconic Hill Adventurer Jeep: red chassis, chunky wheels, roll cage, windshield, spare tire
            Color jeepRed = new Color(0.92f, 0.22f, 0.16f);
            Color darkMetal = new Color(0.18f, 0.22f, 0.28f);
            Color tireCharcoal = new Color(0.08f, 0.10f, 0.14f);
            Color rimSilver = new Color(0.82f, 0.86f, 0.92f);
            Color glass = new Color(0.35f, 0.80f, 0.98f, 0.85f);
            Color light = new Color(1.0f, 0.88f, 0.22f);
            Color pilotHelmet = new Color(0.08f, 0.48f, 1.0f);

            // 1. Main Jeep Tub & Hood (Front to Back)
            Quad(vh,r,.08f,.28f,.88f,.58f,jeepRed);
            // Hood step & front grill
            Quad(vh,r,.04f,.30f,.38f,.55f,jeepRed);
            Quad(vh,r,.01f,.26f,.08f,.42f,darkMetal); // Front bumper
            Quad(vh,r,.01f,.44f,.04f,.54f,light);     // Headlight

            // 2. Roll Cage & Cab Framing
            Quad(vh,r,.36f,.58f,.42f,.92f,darkMetal); // A-pillar
            Quad(vh,r,.40f,.88f,.82f,.94f,darkMetal); // Roof bar
            Quad(vh,r,.78f,.58f,.84f,.92f,darkMetal); // B/C pillar roll bar
            Quad(vh,r,.38f,.60f,.46f,.88f,glass);     // Windshield glass

            // 3. Driver Silhouette inside roll cage
            Quad(vh,r,.48f,.58f,.64f,.84f,pilotHelmet);

            // 4. Rear Spare Tire mounted on tailgate
            Quad(vh,r,.86f,.38f,.98f,.72f,tireCharcoal);
            Quad(vh,r,.89f,.46f,.95f,.64f,rimSilver);

            // 5. Rocker panel / undercarriage line
            Quad(vh,r,.30f,.26f,.66f,.32f,darkMetal);

            // 6. Chunky Off-Road Wheels & Rims (sitting on the loading bar)
            // Front Wheel
            Quad(vh,r,.10f,.02f,.32f,.38f,tireCharcoal);
            Quad(vh,r,.15f,.10f,.27f,.30f,rimSilver);
            // Rear Wheel
            Quad(vh,r,.64f,.02f,.86f,.38f,tireCharcoal);
            Quad(vh,r,.69f,.10f,.81f,.30f,rimSilver);
        }
        static void Quad(VertexHelper vh,Rect r,float x,float y,float x2,float y2,Color c)
        {
            int i=vh.currentVertCount;vh.AddVert(new Vector2(r.xMin+x*r.width,r.yMin+y*r.height),c,Vector2.zero);
            vh.AddVert(new Vector2(r.xMin+x*r.width,r.yMin+y2*r.height),c,Vector2.zero);
            vh.AddVert(new Vector2(r.xMin+x2*r.width,r.yMin+y2*r.height),c,Vector2.zero);
            vh.AddVert(new Vector2(r.xMin+x2*r.width,r.yMin+y*r.height),c,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
