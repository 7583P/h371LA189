using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    public static class RacingVehicleFactory
    {
        static PhysicsMaterial contact;
        public static readonly Color MetaNavy = new Color(.018f, .060f, .20f);
        public static readonly Color MetaWhite = new Color(.92f, .97f, 1f);
        public static readonly Color MetaCyan = new Color(0.08f, 0.92f, 1.0f);
        public static readonly Color MetaBlue = new Color(0.06f, 0.42f, 0.98f);
        public static readonly Color MetaBlack = new Color(.010f, .012f, .016f); // Deep jet black body
        public static readonly Color MetaBlueHelmet = new Color(0.08f, 0.48f, 1.0f);
        public static readonly Color MetaRed = new Color(.98f, .05f, .10f);
        public static readonly Color MetaGreen = new Color(0.08f, 0.92f, 1.0f); // Green removed: redirected to Cyan

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
                paint = MetaNavy;
            }
            else
            {
                paint = colors[Mathf.Abs(identity.personalitySeed % colors.Length)];
            }

            string wheel = (identity.wheelId == MetaProgressionConfig.MetaWheelId || identity.wheelId == "wheel-meta-cyan") ? MetaProgressionConfig.MetaWheelVisualId : identity.wheelId;

            var visual=RacingVehicleVisuals.Build(root, identity.vehicleId, wheel, paint, isHuman: identity.isHuman);
            RacingLoadoutVisuals.Apply(visual,identity.characterId,identity.accessoryId);
            return root;
        }
    }
}
