using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public static class HillAdventurerSkinEffect
    {
        public const string HillAdventurerSkinId = "rally-skin-hill-adventurer";
        private static Material s_sharedMaterial;

        public static bool IsHillAdventurer(VehicleDefinition definition, VehicleDefinition.CustomizationOption skin) =>
            definition != null && definition.archetype == VehicleArchetype.Rally &&
            skin != null && (skin.optionId == HillAdventurerSkinId || skin.displayName.IndexOf("Hill Adventurer", System.StringComparison.OrdinalIgnoreCase) >= 0);

        public static void Apply(SpriteRenderer body, VehicleDefinition definition, VehicleDefinition.CustomizationOption skin)
        {
            if (body == null) return;
            if (IsHillAdventurer(definition, skin))
            {
                if (s_sharedMaterial == null)
                {
                    Shader shader = Shader.Find("AnimeHillRacing/VehicleAnimatedLightSweep");
                    if (shader != null)
                        s_sharedMaterial = new Material(shader) { name = "Runtime_HillAdventurerMaterial" };
                }
                if (s_sharedMaterial != null)
                    body.sharedMaterial = s_sharedMaterial;
            }
            else
            {
                if (body.sharedMaterial != null && body.sharedMaterial.shader != null &&
                    body.sharedMaterial.shader.name == "AnimeHillRacing/VehicleAnimatedLightSweep")
                {
                    Shader defaultShader = Shader.Find("AnimeHillRacing/VehicleBodyBeltline");
                    if (defaultShader != null)
                        body.sharedMaterial = new Material(defaultShader) { name = "Runtime_RallyBeltlineMaterial" };
                }
            }
        }
    }
}
