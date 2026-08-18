using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public static class HillAdventurerSkinEffect
    {
        public const string HillAdventurerSkinId = "rally-skin-hill-adventurer";
        private static Material s_sharedSweepMaterial;
        private static Material s_sharedBeltlineMaterial;

        public static bool IsHillAdventurer(VehicleDefinition definition, VehicleDefinition.CustomizationOption skin) =>
            definition != null && definition.archetype == VehicleArchetype.Rally &&
            skin != null && (skin.optionId == HillAdventurerSkinId || skin.displayName.IndexOf("Hill Adventurer", System.StringComparison.OrdinalIgnoreCase) >= 0);

        public static Material GetSweepMaterial()
        {
            if (s_sharedSweepMaterial != null) return s_sharedSweepMaterial;
            Shader shader = Shader.Find("AnimeHillRacing/VehicleAnimatedLightSweep");
            if (shader == null) shader = Resources.Load<Shader>("VehicleAnimatedLightSweep");
            if (shader != null)
            {
                s_sharedSweepMaterial = new Material(shader)
                {
                    name = "Runtime_HillAdventurerMaterial",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            return s_sharedSweepMaterial;
        }

        public static Material GetBeltlineMaterial()
        {
            if (s_sharedBeltlineMaterial != null) return s_sharedBeltlineMaterial;
            Shader shader = Shader.Find("AnimeHillRacing/VehicleBodyBeltline");
            if (shader == null) shader = Resources.Load<Shader>("VehicleBeltlineCut");
            if (shader != null)
            {
                s_sharedBeltlineMaterial = new Material(shader)
                {
                    name = "Runtime_RallyBeltlineMaterial",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            return s_sharedBeltlineMaterial;
        }

        private static Material s_defaultSpriteMaterial;

        public static Material GetDefaultSpriteMaterial()
        {
            if (s_defaultSpriteMaterial != null) return s_defaultSpriteMaterial;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                s_defaultSpriteMaterial = new Material(shader)
                {
                    name = "Runtime_SpritesDefault",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            return s_defaultSpriteMaterial;
        }

        public static void Apply(SpriteRenderer body, VehicleDefinition definition, VehicleDefinition.CustomizationOption skin)
        {
            if (body == null) return;

            if (IsHillAdventurer(definition, skin))
            {
                Material sweepMat = GetSweepMaterial();
                body.sharedMaterial = sweepMat != null ? sweepMat : GetDefaultSpriteMaterial();
            }
            else
            {
                // Always assign the valid default sprite material so SpriteRenderer never has a null material (which renders as magenta/pink).
                body.sharedMaterial = GetDefaultSpriteMaterial();
            }
        }
    }
}
