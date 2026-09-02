using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class VehicleDefinition : ScriptableObject
    {
        public sealed class CustomizationOption
        {
            public string optionId;
            public string displayName;
            public Sprite sprite;
            public bool overrideGlitchTransform;
            public Vector2 glitchManualSize;
            public Vector2 glitchManualOffset;
            public bool overrideGlitchWheelTransform;
            public float glitchRearWheelScale = 1f;
            public float glitchFrontWheelScale = 1f;
            public Vector2 glitchRearWheelOffset;
            public Vector2 glitchFrontWheelOffset;
        }

        public CustomizationOption[] skins;
        public CustomizationOption[] wheels;
        public float visualWidth;
        public Vector2 bodyVisualOffset;
        public Vector2 rearWheelVisualPosition;
        public Vector2 frontWheelVisualPosition;
        public bool flipBodyX;
        public string displayName;
        public float RearWheelDiameter => 1f;
        public float FrontWheelDiameter => 1f;

        public static float SpriteWidthScale(Sprite sprite, float canonicalWidth) => 1f;
        public static float WheelVisualScale(CustomizationOption option, float diameter) => 1f;
        public Vector3 BodyCustomizationScale(CustomizationOption option) => Vector3.one;
        public Vector3 BodyCustomizationOffset(CustomizationOption option) => Vector3.zero;
        public float WheelCustomizationScale(CustomizationOption option, float diameter,
            bool front = false) => 1f;
        public Vector2 WheelCustomizationOffset(CustomizationOption option, bool front) => Vector2.zero;
    }

    public static class GlitchCrystalTideEffect
    {
        public const string SkinPrefix = "glitch-phase-";
        public static bool IsGlitchSkin(VehicleDefinition.CustomizationOption option) => true;
        public static bool IsGlitchWheel(VehicleDefinition.CustomizationOption option) => true;
    }

    public sealed class VehicleVisual2D : MonoBehaviour
    {
        public void ApplySavedCustomization() { }
    }
}

namespace AnimeHillRacing.Core
{
    public sealed class GameCatalog : ScriptableObject
    {
        public AnimeHillRacing.Vehicle.VehicleDefinition[] vehicles;
    }
}

namespace AnimeHillRacing.Road
{
    public sealed class VehicleVisual3D : MonoBehaviour
    {
        public void ApplySavedCustomization() { }
    }
}

namespace AnimeHillRacing.UI
{
    public sealed class GarageUI : MonoBehaviour
    {
        public void RefreshVehicleDefinitionPreview() { }
    }
}
