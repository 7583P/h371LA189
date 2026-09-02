using System;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    /// <summary>Applies an animated crystalline light tide to every distance-unlocked Glitch cosmetic.</summary>
    public static class GlitchCrystalTideEffect
    {
        public const string SkinPrefix = "glitch-phase-";
        public const string WheelPrefix = "glitch-wheel-";
        private static readonly int TideSpeedId = Shader.PropertyToID("_TideSpeed");
        private static readonly int TideScaleId = Shader.PropertyToID("_TideScale");
        private static readonly int CrystalDensityId = Shader.PropertyToID("_CrystalDensity");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
        private static readonly int PhaseOffsetId = Shader.PropertyToID("_PhaseOffset");
        private static readonly int RadialFlowId = Shader.PropertyToID("_RadialFlow");
        private static Material sharedMaterial;

        public static bool IsAnimated(VehicleDefinition.CustomizationOption skin,
            VehicleDefinition.CustomizationOption wheel) => IsGlitchSkin(skin) || IsGlitchWheel(wheel);

        public static bool IsGlitchSkin(VehicleDefinition.CustomizationOption option) =>
            HasPrefix(option, SkinPrefix);

        public static bool IsGlitchWheel(VehicleDefinition.CustomizationOption option) =>
            HasPrefix(option, WheelPrefix);

        public static bool ApplySkin(SpriteRenderer renderer,
            VehicleDefinition.CustomizationOption option)
        {
            if (!IsGlitchSkin(option)) return false;
            return Apply(renderer, option, false);
        }

        public static bool ApplyWheel(SpriteRenderer renderer,
            VehicleDefinition.CustomizationOption option)
        {
            if (!IsGlitchWheel(option)) return false;
            return Apply(renderer, option, true);
        }

        private static bool HasPrefix(VehicleDefinition.CustomizationOption option, string prefix) =>
            option != null && !string.IsNullOrEmpty(option.optionId) &&
            option.optionId.StartsWith(prefix, StringComparison.Ordinal);

        private static bool Apply(SpriteRenderer renderer,
            VehicleDefinition.CustomizationOption option, bool radial)
        {
            if (renderer == null) return false;
            Material material = GetMaterial();
            if (material == null) return false;

            int phase = ResolvePhase(option.optionId);
            renderer.sharedMaterial = material;
            renderer.SetPropertyBlock(null);
            var properties = new MaterialPropertyBlock();
            properties.SetFloat(TideSpeedId, (radial ? .33f : .27f) + phase * .022f);
            properties.SetFloat(TideScaleId, (radial ? 2.65f : 2.08f) + phase * .11f);
            // Irregular facets stay broad enough to read at garage-card scale without aliasing
            // into a rectangular grid. Later phases still gain a clearly stronger light tide.
            properties.SetFloat(CrystalDensityId, (radial ? 9.2f : 6.4f) + phase * .38f);
            properties.SetFloat(GlowIntensityId, .88f + phase * .06f);
            properties.SetFloat(PhaseOffsetId, phase * .137f + (radial ? .21f : 0f));
            // Bodies combine crossed tides with an expanding radial wave; wheels remain primarily
            // radial. Neither effect has a single dominant travel direction.
            properties.SetFloat(RadialFlowId, radial ? 1f : .48f);
            renderer.SetPropertyBlock(properties);
            return true;
        }

        private static int ResolvePhase(string optionId)
        {
            if (string.IsNullOrEmpty(optionId)) return 0;
            int separator = optionId.LastIndexOf('-');
            if (separator < 0 || separator >= optionId.Length - 1 ||
                !int.TryParse(optionId.Substring(separator + 1), out int threshold) || threshold <= 0)
                return 0;
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Log(threshold, 2f)) - 15, 0, 5);
        }

        private static Material GetMaterial()
        {
            if (sharedMaterial != null) return sharedMaterial;
            Shader shader = Shader.Find("AnimeHillRacing/GlitchCrystalTide");
            if (shader == null) shader = Resources.Load<Shader>("GlitchCrystalTide");
            if (shader == null || !shader.isSupported) return null;
            sharedMaterial = new Material(shader)
            {
                name = "Glitch Crystal Tide (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            return sharedMaterial;
        }
    }
}
