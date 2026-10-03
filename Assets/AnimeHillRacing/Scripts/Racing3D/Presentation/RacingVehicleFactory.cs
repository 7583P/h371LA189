using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    public static class RacingVehicleFactory
    {
        static PhysicsMaterial contact;
        public static readonly Color MetaCyan = new Color(0.08f, 0.94f, 0.98f);
        public static readonly Color MetaBlack = new Color(.018f, .025f, .037f);
        public static readonly Color MetaBlueHelmet = new Color(.025f, .24f, .95f);
        public static readonly Color MetaRed = new Color(.98f, .045f, .08f);
        public static readonly Color MetaGreen = new Color(.08f, .94f, .32f);

        public static GameObject Create(RacerIdentity identity)
        {
            var root = new GameObject(identity.displayName);
            if (contact == null)
                contact = new PhysicsMaterial("Racing glancing contact")
                {
                    dynamicFriction = 0.02f,
                    staticFriction = 0.02f,
                    bounciness = 0.05f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };

            // 1. Central chassis collider (chamfered envelope)
            var bodyBox = root.AddComponent<BoxCollider>();
            bodyBox.center = new Vector3(0, 0.08f, 0);
            bodyBox.size = new Vector3(1.42f, 0.48f, 2.75f);
            bodyBox.sharedMaterial = contact;

            // 2. Rounded front bumper deflector (prevents 90-degree corner snagging on wall seams)
            var noseSphere = root.AddComponent<SphereCollider>();
            noseSphere.center = new Vector3(0, 0.05f, 1.45f);
            noseSphere.radius = 0.34f;
            noseSphere.sharedMaterial = contact;

            // 3. Dedicated wheel contact spheres (prevents visual wheel penetration and entrapment)
            Vector3[] wheelSpots = {
                new Vector3(-0.84f, -0.05f, 1.02f),
                new Vector3(0.84f, -0.05f, 1.02f),
                new Vector3(-0.84f, -0.05f, -1.02f),
                new Vector3(0.84f, -0.05f, -1.02f)
            };
            for (int i = 0; i < 4; i++)
            {
                var wheelCol = root.AddComponent<SphereCollider>();
                wheelCol.center = wheelSpots[i];
                wheelCol.radius = 0.36f;
                wheelCol.sharedMaterial = contact;
            }

            root.AddComponent<RacingVehiclePhysics>();

            Color[] colors = { RacingDesign.Cyan, RacingDesign.Secondary, RacingDesign.Gold, new Color(.32f, .65f, .94f), new Color(.52f, .84f, .64f), RacingDesign.Apex };
            Color paint;
            if (identity.paintColor != Color.clear && identity.paintColor.a > 0.1f)
            {
                paint = identity.paintColor;
            }
            else if (identity.buildCategory == MetaBuildCategory.Meta || identity.vehicleId == "formula-01" || identity.vehicleId == MetaProgressionConfig.MetaVehicleId)
            {
                paint = MetaBlack;
            }
            else
            {
                paint = colors[Mathf.Abs(identity.personalitySeed % colors.Length)];
            }

            string wheel = (identity.wheelId == MetaProgressionConfig.MetaWheelId || identity.wheelId == "wheel-meta-cyan") ? MetaProgressionConfig.MetaWheelVisualId : identity.wheelId;

            RacingVehicleVisuals.Build(root, identity.vehicleId, wheel, paint, isHuman: identity.isHuman);
            return root;
        }
    }
}
