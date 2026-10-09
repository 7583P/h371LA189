using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeHillRacing.Racing3D
{
    // Original compact performance utility. Geometry is shared and contains no colliders.
    public static class RacingMetaVisuals
    {
        public const string BodyName = "DAPEX";
        public const string WheelName = "WHITY";
        static Mesh whityFace;
        static Mesh whityTyre;
        static Mesh arch;
        static Material badgeMaterial;
        static Font badgeFont;
        // Whity wheel face material with vivid, elegant neon luminescence
        public static Material White => RacingArt.IdentityMaterial("Whity / solid titanium ceramic", new Color(.78f,.86f,.93f), .65f);

        public static void Body(Transform parent, Material paint, Material graphite, Material glass)
        {
            // Crisp, fine white neon accent material
            var white = RacingArt.IdentityMaterial("DAPEX / white signature", RacingVehicleFactory.MetaWhite, .48f);
            // Vivid crimson red rear continuous brake ribbon
            var brake = RacingArt.IdentityMaterial("DAPEX / rear brake ribbon", new Color(.98f, .06f, .12f), .95f);

            // 1. Aerodynamic crossover body shell with flared wheel arches and athletic waistline
            RacingArt.Part(parent, "DAPEX sculpted lower body", RacingArt.AeroShell("dapex_body_crossover",
                new Vector4(.70f, -.10f, .36f, -1.60f), // Tapered rear bumper
                new Vector4(.82f, -.12f, .42f, -1.35f), // Rear overhang transition
                new Vector4(.89f, -.13f, .48f, -1.02f), // Muscular rear haunches (wide hips over rear wheels)
                new Vector4(.84f, -.13f, .46f, 0.05f),  // Tucked athletic waistline / sculpted doors
                new Vector4(.87f, -.13f, .47f, 1.02f),  // Powerful front fender arches
                new Vector4(.78f, -.10f, .42f, 1.48f),  // Front fascia transition
                new Vector4(.70f, -.06f, .35f, 1.62f)), // Front nose & chin
                paint, Vector3.zero, Quaternion.identity);

            // 2. Panoramic crossover coupe greenhouse with tinted glass
            RacingArt.Part(parent, "DAPEX panoramic cabin", RacingArt.Hull("dapex_cabin_crossover",
                new Vector4(.71f, .40f, .65f, -1.22f), // Base of fastback rear window / tailgate shelf
                new Vector4(.67f, .42f, .94f, -.82f),  // Sloping rear coupe glass
                new Vector4(.66f, .43f, 1.05f, -.32f), // Mid roof apex
                new Vector4(.65f, .44f, 1.04f, .14f),  // Windshield top / A-pillar crest
                new Vector4(.72f, .42f, .54f, .76f)),  // Base of windshield / hood cowl
                glass, Vector3.zero, Quaternion.identity);

            // 3. Floating dark blue roof panel
            RacingArt.Part(parent, "Floating blue roof", RacingArt.AeroShell("dapex_roof_crossover",
                new Vector4(.58f, .93f, 1.01f, -.84f), // Rear roof edge
                new Vector4(.60f, 1.00f, 1.07f, -.35f), // Center crown
                new Vector4(.56f, .98f, 1.06f, .15f)),  // Front windshield top
                paint, Vector3.zero, Quaternion.identity);

            for (int side = -1; side <= 1; side += 2)
            {
                // A. Floating roofline white neon / chrome arch (traces the entire upper window profile from A-pillar to D-pillar)
                Trace(parent, "Floating roof edge", white, .012f,
                    new Vector3(side * .72f, .48f, .72f),
                    new Vector3(side * .66f, .86f, .22f),
                    new Vector3(side * .60f, 1.04f, -.12f),
                    new Vector3(side * .62f, 1.04f, -.48f),
                    new Vector3(side * .70f, .74f, -.92f),
                    new Vector3(side * .73f, .48f, -1.22f));

                // B. Front Fascia - Upper Blade Headlight & Eyebrow
                RacingArt.Box(parent, "White blade headlight", new Vector3(side * .49f, .335f, 1.655f), new Vector3(.38f, .020f, .032f), white, .006f);
                RacingArt.Box(parent, "Headlight return", new Vector3(side * .69f, .315f, 1.645f), new Vector3(.024f, .060f, .025f), white, .006f);

                // C. Front Fascia - Iconic L-Shaped / Bracket Air-Intake Corner LEDs ([  ] framing the air curtains)
                RacingArt.Box(parent, "Intake corner bracket vertical", new Vector3(side * .685f, .215f, 1.65f), new Vector3(.020f, .210f, .020f), white, .005f);
                RacingArt.Box(parent, "Intake corner bracket horizontal", new Vector3(side * .595f, .105f, 1.655f), new Vector3(.180f, .018f, .020f), white, .005f);

                // D. Front Fascia - Angled lower intake chrome trim kick-up
                var intakeKick = RacingArt.Box(parent, "Intake lower corner trim", new Vector3(side * .44f, .092f, 1.665f), new Vector3(.016f, .095f, .020f), white, .004f);
                intakeKick.transform.localRotation = Quaternion.Euler(0, 0, side * 32f);

                // E. Hood character lines (fine, elegant dual creases running forward)
                Trace(parent, "Hood character crease", white, .008f,
                    new Vector3(side * .24f, .437f, 1.52f), new Vector3(side * .35f, .482f, .85f));

                // F. Side Profile - Pillars & Glass Architecture
                Trace(parent, "Sculpted windshield pillar", paint, .045f,
                    new Vector3(side * .72f, .48f, .74f), new Vector3(side * .65f, .96f, .12f));
                Trace(parent, "Sculpted rear pillar", paint, .045f,
                    new Vector3(side * .72f, .56f, -1.18f), new Vector3(side * .67f, .92f, -.76f));
                RacingArt.Box(parent, "Cabin B pillar", new Vector3(side * .685f, .73f, -.33f), new Vector3(.035f, .51f, .055f), paint, .01f);

                // G. Side Profile - Lower Rocker Blade with White Neon Accent Line
                RacingArt.Box(parent, "Graphite rocker blade", new Vector3(side * .84f, -.025f, 0), new Vector3(.05f, .085f, 1.35f), graphite, .02f);
                RacingArt.Box(parent, "White rocker neon blade", new Vector3(side * .865f, -.010f, 0), new Vector3(.014f, .016f, 1.30f), white, .004f);

                // H. Side Profile - Aerodynamic Side Mirrors
                RacingArt.Box(parent, "Mirror stem", new Vector3(side * .72f, .51f, .33f), new Vector3(.075f, .035f, .035f), graphite, .008f);
                RacingArt.Box(parent, "Aero side mirror", new Vector3(side * .79f, .55f, .33f), new Vector3(.16f, .070f, .100f), paint, .018f);
                RacingArt.Box(parent, "Mirror white indicator", new Vector3(side * .87f, .55f, .335f), new Vector3(.010f, .016f, .085f), white, .003f);

                // I. Side Profile - Flush Luxury Door Handles
                RacingArt.Box(parent, "Flush front door handle", new Vector3(side * .848f, .45f, .15f), new Vector3(.014f, .022f, .11f), graphite, .005f);
                RacingArt.Box(parent, "Flush rear door handle", new Vector3(side * .855f, .46f, -.35f), new Vector3(.014f, .022f, .11f), graphite, .005f);

                // J. Sculpted Wheel Arches (Fender Flares)
                for (int axle = -1; axle <= 1; axle += 2)
                    RacingArt.Part(parent, "Sculpted wheel arch", Arch(), graphite,
                        new Vector3(side * .88f, -.19f, axle * 1.02f), Quaternion.identity);

                // K. Rear Lamp Corner Wraps & Lower Reflectors
                RacingArt.Box(parent, "Rear lamp corner wrap", new Vector3(side * .66f, .315f, -1.62f), new Vector3(.12f, .024f, .080f), brake, .006f);
                RacingArt.Box(parent, "Rear bumper reflector", new Vector3(side * .52f, .095f, -1.635f), new Vector3(.24f, .018f, .022f), brake, .005f);
            }

            // Front Horizon Lightbar Bridge
            // The fascia shell ends at z=1.62. Recessing the lamps and grille
            // inside it hid their centers in the actual garage and race renders.
            RacingArt.Box(parent, "Front signature bridge", new Vector3(0, .335f, 1.655f), new Vector3(.44f, .014f, .024f), white, .004f);

            // Front Lower Trapezoidal Intake Grille & Louvers
            RacingArt.Box(parent, "Front graphite intake", new Vector3(0, .14f, 1.645f), new Vector3(1.08f, .14f, .036f), graphite, .025f);
            for (int slat = 0; slat < 3; slat++)
            {
                float ySlat = .085f + slat * .045f;
                RacingArt.Box(parent, "Front intake slat " + slat, new Vector3(0, ySlat, 1.67f), new Vector3(.96f - slat * .08f, .012f, .020f), graphite, .003f);
            }
            // Front Lower Intake Neon Framing Line
            RacingArt.Box(parent, "Front lower intake neon trim", new Vector3(0, .060f, 1.67f), new Vector3(.76f, .015f, .020f), white, .004f);

            // Integrated Front Chin Splitter
            RacingArt.Box(parent, "Integrated chin splitter", new Vector3(0, -.060f, 1.52f), new Vector3(1.48f, .048f, .24f), graphite, .04f);

            // Rear Aerodynamic Roof Spoiler
            RacingArt.Box(parent, "Roof spoiler", new Vector3(0, .975f, -.88f), new Vector3(1.26f, .050f, .26f), paint, .035f);
            // High-Mount 3rd Brake Light Strip under spoiler
            RacingArt.Box(parent, "High-mount 3rd brake lamp", new Vector3(0, .965f, -.99f), new Vector3(.44f, .016f, .022f), brake, .004f);

            // Full-Width Rear Continuous Red LED Lamp
            // Keep the entire light ribbon outside the rear cap at z=-1.60.
            RacingArt.Box(parent, "Rear continuous lamp", new Vector3(0, .315f, -1.638f), new Vector3(1.22f, .024f, .028f), brake, .007f);

            // Tailgate Recessed Shelf with "DAPEX" Badge
            var badge = new GameObject("DAPEX rear badge");
            badge.transform.SetParent(parent, false);
            badge.transform.localPosition = new Vector3(0, .205f, -1.645f);
            var lettering = badge.AddComponent<TextMesh>();
            lettering.text = BodyName;
            lettering.font = RacingDesign.Font;
            lettering.fontSize = 72;
            lettering.characterSize = .014f;
            lettering.fontStyle = FontStyle.Bold;
            lettering.anchor = TextAnchor.MiddleCenter;
            lettering.alignment = TextAlignment.Center;
            lettering.color = RacingVehicleFactory.MetaWhite;
            lettering.font.RequestCharactersInTexture(BodyName, lettering.fontSize, lettering.fontStyle);
            var badgeRenderer = badge.GetComponent<MeshRenderer>();
            badgeRenderer.sharedMaterial = BadgeMaterial(lettering.font);
            badgeRenderer.shadowCastingMode = ShadowCastingMode.Off;
            badgeRenderer.receiveShadows = false;

            // Rear Diffuser & Lower Styling
            RacingArt.Box(parent, "Rear diffuser", new Vector3(0, -.065f, -1.49f), new Vector3(1.48f, .12f, .24f), graphite, .035f);
            RacingArt.Box(parent, "Lower diffuser neon accent", new Vector3(0, .015f, -1.635f), new Vector3(.82f, .016f, .020f), white, .004f);
            for (int vane = -2; vane <= 2; vane++)
                RacingArt.Box(parent, "Diffuser channel", new Vector3(vane * .23f, -.10f, -1.53f), new Vector3(.022f, .10f, .24f), graphite, .005f);
        }

        static Material BadgeMaterial(Font font)
        {
            if(badgeMaterial!=null)return badgeMaterial;
            badgeFont=font;
            badgeMaterial=new Material(Resources.Load<Shader>("RacingVehicleBadge"))
                {name="DAPEX / rear lettering with depth",mainTexture=font.material.mainTexture};
            Font.textureRebuilt-=RefreshBadgeAtlas;Font.textureRebuilt+=RefreshBadgeAtlas;
            return badgeMaterial;
        }
        static void RefreshBadgeAtlas(Font font)
        {if(font==badgeFont&&badgeMaterial!=null)badgeMaterial.mainTexture=font.material.mainTexture;}

        public static void Wheel(Transform pivot, float width, Material tyre, Material alloy)
        {
            RacingArt.Part(pivot, "Whity rounded performance tyre", Tyre(), tyre, Vector3.zero, Quaternion.identity);
            RacingArt.Part(pivot, "Whity thin graphite edge", RacingArt.Ring(.322f,.308f,width+.006f,48), alloy, Vector3.zero, Quaternion.identity);
            RacingArt.Part(pivot, "Whity uninterrupted white disc", Face(), White, Vector3.zero, Quaternion.identity);
        }

        static Mesh Face()
        {
            if (whityFace!=null) return whityFace;
            var vertices=new List<Vector3>(); var triangles=new List<int>();
            const int segments=64;
            for(int face=-1;face<=1;face+=2)
            {
                int center=vertices.Count;vertices.Add(new Vector3(face*.130f,0,0));
                float[] radii={.255f,.300f,.314f};float[] depths={.130f,.129f,.123f};
                for(int ring=0;ring<radii.Length;ring++)for(int i=0;i<segments;i++)
                {
                    float a=i*Mathf.PI*2/segments;
                    vertices.Add(new Vector3(face*depths[ring],Mathf.Cos(a)*radii[ring],Mathf.Sin(a)*radii[ring]));
                }
                for(int i=0;i<segments;i++)
                {
                    int next=(i+1)%segments;
                    Tri(center,center+1+i,center+1+next);
                    for(int ring=0;ring<radii.Length-1;ring++)
                    {
                        int inner=center+1+ring*segments,outer=inner+segments;
                        Tri(inner+i,outer+i,outer+next);Tri(inner+i,outer+next,inner+next);
                    }
                }
                void Tri(int a,int b,int c)
                {
                    triangles.Add(a);triangles.Add(face>0?b:c);triangles.Add(face>0?c:b);
                }
            }
            whityFace=new Mesh { name="Whity / continuous titanium white disc" };
            whityFace.SetVertices(vertices); whityFace.SetTriangles(triangles,0);
            whityFace.RecalculateNormals(); whityFace.RecalculateBounds(); return whityFace;
        }

        static Mesh Tyre()
        {
            if(whityTyre!=null)return whityTyre;
            const int segments=48;
            Vector2[] profile={new Vector2(-.115f,.309f),new Vector2(-.119f,.329f),new Vector2(-.095f,.368f),
                new Vector2(-.055f,.38f),new Vector2(.055f,.38f),new Vector2(.095f,.368f),new Vector2(.119f,.329f),new Vector2(.115f,.309f)};
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int ring=0;ring<profile.Length;ring++)for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments;
                vertices.Add(new Vector3(profile[ring].x,Mathf.Cos(a)*profile[ring].y,Mathf.Sin(a)*profile[ring].y));
            }
            for(int ring=0;ring<profile.Length;ring++)for(int i=0;i<segments;i++)
            {
                int a=ring*segments+i,b=ring*segments+(i+1)%segments;
                int c=((ring+1)%profile.Length)*segments+(i+1)%segments,d=((ring+1)%profile.Length)*segments+i;
                triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(a);triangles.Add(c);triangles.Add(d);
            }
            whityTyre=new Mesh{name="Whity / rounded black tyre"};whityTyre.SetVertices(vertices);whityTyre.SetTriangles(triangles,0);
            whityTyre.RecalculateNormals();whityTyre.RecalculateBounds();return whityTyre;
        }

        static Mesh Arch()
        {
            if(arch!=null) return arch;
            var vertices=new List<Vector3>(); var triangles=new List<int>();
            for(int s=0;s<=20;s++)
            {
                float a=Mathf.Lerp(-18,198,s/20f)*Mathf.Deg2Rad;
                vertices.Add(new Vector3(-.028f,Mathf.Sin(a)*.475f,Mathf.Cos(a)*.475f));
                vertices.Add(new Vector3(.028f,Mathf.Sin(a)*.475f,Mathf.Cos(a)*.475f));
                if(s==20)continue;int v=s*2;
                triangles.Add(v);triangles.Add(v+2);triangles.Add(v+1);
                triangles.Add(v+1);triangles.Add(v+2);triangles.Add(v+3);
            }
            arch=new Mesh { name="DAPEX / curved performance arch" };
            arch.SetVertices(vertices);arch.SetTriangles(triangles,0);arch.RecalculateNormals();arch.RecalculateBounds();return arch;
        }

        static void Trace(Transform parent,string name,Material material,float thickness,params Vector3[] points)
        {
            for(int i=1;i<points.Length;i++)
            {
                Vector3 delta=points[i]-points[i-1];
                var line=RacingArt.Box(parent,name,(points[i]+points[i-1])*.5f,new Vector3(thickness,thickness,delta.magnitude+.012f),material,thickness*.25f);
                line.transform.localRotation=Quaternion.LookRotation(delta,Vector3.up);
            }
        }
    }
}
