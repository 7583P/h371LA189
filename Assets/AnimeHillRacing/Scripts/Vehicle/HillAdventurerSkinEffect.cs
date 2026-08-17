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
            Shader shader = Resources.Load<Shader>("VehicleAnimatedLightSweep");
            if (shader == null) shader = Shader.Find("AnimeHillRacing/VehicleAnimatedLightSweep");
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
            Shader shader = Resources.Load<Shader>("VehicleBeltlineCut");
            if (shader == null) shader = Resources.Load<Shader>("VehicleBodyBeltline");
            if (shader == null) shader = Shader.Find("AnimeHillRacing/VehicleBodyBeltline");
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

        public static void Apply(SpriteRenderer body, VehicleDefinition definition, VehicleDefinition.CustomizationOption skin)
        {
            if (body == null) return;

            if (IsHillAdventurer(definition, skin))
            {
                Material sweepMat = GetSweepMaterial();
                if (sweepMat != null)
                {
                    body.sharedMaterial = sweepMat;
                }
            }
            else if (definition != null && definition.archetype == VehicleArchetype.Rally)
            {
                // Rally skins (Default, Neo Rally, etc.) use Beltline Cut material for roof damage
                Material beltlineMat = GetBeltlineMaterial();
                if (beltlineMat != null)
                {
                    body.sharedMaterial = beltlineMat;
                }
            }
            else
            {
                // Non-Rally vehicles (such as Hill Adventurer 4x4, Formula, Jeep, SportBike)
                // If renderer previously had a Rally sweep or beltline material (e.g. from Garage Preview reuse),
                // reset to null so SpriteRenderer cleanly uses Sprites-Default without pink/magenta errors.
                if (body.sharedMaterial != null && body.sharedMaterial.shader != null &&
                    (body.sharedMaterial.shader.name == "AnimeHillRacing/VehicleAnimatedLightSweep" ||
                     body.sharedMaterial.shader.name == "AnimeHillRacing/VehicleBodyBeltline"))
                {
                    body.sharedMaterial = null;
                }
            }
        }
    }
}
