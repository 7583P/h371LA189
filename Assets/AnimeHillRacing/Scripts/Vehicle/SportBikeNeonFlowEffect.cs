using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    /// <summary>Animates the authored neon colors on Sport Bike's Neon Flow body and wheel.</summary>
    public static class SportBikeNeonFlowEffect
    {
        public const string SkinId = "sport-bike-skin-neon-flow";
        public const string WheelId = "sport-bike-wheel-neon-flow";
        private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
        private static readonly int FlowScaleId = Shader.PropertyToID("_FlowScale");
        private static readonly int RadialFlowId = Shader.PropertyToID("_RadialFlow");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
        private static Material sharedMaterial;

        public static bool IsAnimated(VehicleDefinition definition,
            VehicleDefinition.CustomizationOption skin, VehicleDefinition.CustomizationOption wheel) =>
            IsNeonSkin(definition, skin) || IsNeonWheel(definition, wheel);

        public static bool ApplySkin(SpriteRenderer renderer, VehicleDefinition definition,
            VehicleDefinition.CustomizationOption skin)
        {
            bool active = IsNeonSkin(definition, skin);
            if (active) Apply(renderer, false);
            else if (IsSportBike(definition)) Reset(renderer);
            return active;
        }

        public static bool ApplyWheel(SpriteRenderer renderer, VehicleDefinition definition,
            VehicleDefinition.CustomizationOption wheel)
        {
            bool active = IsNeonWheel(definition, wheel);
            if (active) Apply(renderer, true);
            else Reset(renderer);
            return active;
        }

        public static bool IsNeonSkin(VehicleDefinition definition,
            VehicleDefinition.CustomizationOption skin) =>
            IsSportBike(definition) && skin != null && skin.optionId == SkinId;

        public static bool IsNeonWheel(VehicleDefinition definition,
            VehicleDefinition.CustomizationOption wheel) =>
            IsSportBike(definition) && wheel != null && wheel.optionId == WheelId;

        private static bool IsSportBike(VehicleDefinition definition) =>
            definition != null && definition.archetype == VehicleArchetype.SportBike;

        private static void Apply(SpriteRenderer renderer, bool radial)
        {
            if (renderer == null) return;
            Material material = GetMaterial();
            if (material == null)
            {
                Reset(renderer);
                return;
            }

            renderer.sharedMaterial = material;
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetFloat(FlowSpeedId, radial ? .34f : .27f);
            properties.SetFloat(FlowScaleId, radial ? 1.35f : 1.8f);
            properties.SetFloat(RadialFlowId, radial ? 1f : 0f);
            properties.SetFloat(GlowIntensityId, radial ? .72f : .62f);
            renderer.SetPropertyBlock(properties);
        }

        private static void Reset(SpriteRenderer renderer)
        {
            if (renderer == null) return;
            renderer.sharedMaterial = HillAdventurerSkinEffect.GetDefaultSpriteMaterial();
            renderer.SetPropertyBlock(null);
        }

        private static Material GetMaterial()
        {
            if (sharedMaterial != null) return sharedMaterial;
            Shader shader = Shader.Find("AnimeHillRacing/SportBikeNeonFlow");
            if (shader == null) shader = Resources.Load<Shader>("SportBikeNeonFlow");
            if (shader == null) return null;
            sharedMaterial = new Material(shader)
            {
                name = "Sport Bike Neon Flow (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            return sharedMaterial;
        }
    }
}
