using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Driver Definition", fileName = "Driver")]
    public sealed class DriverDefinition : ScriptableObject
    {
        [System.Serializable]
        public sealed class CharacterAccessoryOption
        {
            public string accessoryId = "default";
            public string displayName = "DEFAULT / NO ACCESSORY";
            public Sprite sprite;
            public bool unlockedByDefault = true;
            [Min(0)] public int gemPrice;
            [Min(.05f)] public float visibleWidth = .72f;
            [Tooltip("Shared local correction applied from every vehicle Accessory Anchor.")]
            public Vector2 anchorOffset;
            [Tooltip("Default horizontal orientation of the sprite.")]
            public bool flipX;
            [Tooltip("Lets the equipped accessory toggle between its normal and mirrored orientations.")]
            public bool supportsFlipVariant;
            [Tooltip("Animates fast multicolor neon lightning across the accessory from left to right.")]
            public bool animateRainbow;
            [Min(.01f)] public float rainbowSpeed = .35f;
            [Min(.1f)] public float rainbowFrequency = 1.15f;
            [Range(0f, 2f)] public float rainbowNeonGlow = 1.25f;
            public bool allowsBlueprintProgression = true;
            [Tooltip("When disabled, the first/default accessory resolves to Common and every other accessory resolves to Mythic.")]
            public bool hasExplicitRarity;
            public AnimeHillRacing.Core.ItemRarity rarity = AnimeHillRacing.Core.ItemRarity.Mythic;

            public AnimeHillRacing.Core.ItemRarity ResolveRarity(bool isDefaultItem) =>
                AnimeHillRacing.Core.ItemRarityUtility.Resolve(hasExplicitRarity, rarity, isDefaultItem);
        }

        public string driverId = "stickman-black";
        public string displayName = "STICKMAN BLACK";
        public bool unlockedByDefault = true;
        [Tooltip("Canonical character whose body-part dimensions and pivots this character follows. " +
                 "The first driver in GameCatalog is assigned automatically to future characters.")]
        public DriverDefinition poseReference;
        [Tooltip("Optional material applied to every articulated body part and to the Garage preview. " +
                 "Leave empty to render the authored sprites normally.")]
        public Material bodyMaterial;
        public Sprite previewSprite;
        public Sprite head;
        public Sprite torso;
        public Sprite upperArm;
        public Sprite forearm;
        public Sprite thigh;
        public Sprite lowerLeg;
        public CharacterAccessoryOption[] accessories;

        public DriverDefinition PoseReference => poseReference != null && poseReference != this
            ? poseReference : this;

        public CharacterAccessoryOption FindAccessory(string accessoryId)
        {
            if (accessories != null)
                for (int i = 0; i < accessories.Length; i++)
                    if (accessories[i] != null && accessories[i].accessoryId == accessoryId)
                        return accessories[i];
            return accessories != null && accessories.Length > 0 ? accessories[0] : null;
        }

        public string DefaultAccessoryId => accessories != null && accessories.Length > 0 &&
            accessories[0] != null ? accessories[0].accessoryId : "default";
    }
}
