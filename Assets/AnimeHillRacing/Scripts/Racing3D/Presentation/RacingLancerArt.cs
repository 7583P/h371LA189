using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    // Shared low-poly geometry for the pickup, held item and full transformation.
    // No live lights, decorative colliders or new particle system per racer.
    public static class RacingLancerArt
    {
        public static Transform Build(Transform parent)
        {
            var shell = RacingArt.Material(new Color(.16f, .20f, .27f), .82f);
            var metal = RacingArt.Material(new Color(.72f, .79f, .87f), .84f);
            var fire = RacingArt.Material(RaceItemUtility.ColorFor(RaceItemType.Lancer), .35f, true);
            var core = RacingArt.Material(new Color(1f, .88f, .38f), .4f, true);
            RacingArt.Part(parent, "Lancer meteor shell", RacingArt.Hull("lancer_shell",
                new Vector4(.26f, -.24f, .24f, -.82f), new Vector4(.44f, -.34f, .34f, -.50f),
                new Vector4(.37f, -.29f, .29f, .38f), new Vector4(.16f, -.13f, .13f, .82f)),
                shell, Vector3.zero, Quaternion.identity);
            RacingArt.Part(parent, "Molten meteor nose", RacingArt.Hull("lancer_nose",
                new Vector4(.16f, -.13f, .13f, .82f), new Vector4(.01f, -.01f, .01f, 1.16f)),
                fire, Vector3.zero, Quaternion.identity);
            for (int side = -1; side <= 1; side += 2) {
                var fin = RacingArt.Box(parent, "Swept silver stabilizer", new Vector3(side * .47f, 0, -.55f),
                    new Vector3(.65f, .055f, .55f), metal, .08f);
                fin.transform.localRotation = Quaternion.Euler(0, side * -24, side * 12);
                RacingArt.Box(parent, "Molten flank seam", new Vector3(side * .381f, 0, -.05f),
                    new Vector3(.018f, .09f, .55f), fire, .006f);
            }
            RacingArt.Part(parent, "Rocket exhaust ring", RacingArt.Ring(.28f, .205f, .10f, 12),
                metal, new Vector3(0, 0, -.82f), Quaternion.Euler(0, 90, 0));
            var flame = new GameObject("Lancer fire plume").transform;
            flame.SetParent(parent, false);
            flame.localPosition = new Vector3(0, 0, -.86f);
            RacingArt.Part(flame, "Amber meteor tail", RacingArt.Hull("lancer_flame",
                new Vector4(.012f, -.01f, .01f, -1.85f), new Vector4(.18f, -.14f, .14f, -.56f),
                new Vector4(.22f, -.18f, .18f, 0)), fire, Vector3.zero, Quaternion.identity);
            RacingArt.Part(flame, "White hot exhaust", RacingArt.Hull("lancer_flame_core",
                new Vector4(.006f, -.006f, .006f, -1.13f), new Vector4(.105f, -.085f, .085f, 0)),
                core, new Vector3(0, .08f, .005f), Quaternion.identity);
            foreach (var renderer in parent.GetComponentsInChildren<MeshRenderer>()) {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return flame;
        }
    }
}
