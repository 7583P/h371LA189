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
            public bool allowsBlueprintProgression = true;
        }

        public string driverId = "stickman-black";
        public string displayName = "STICKMAN BLACK";
        public bool unlockedByDefault = true;
        [Tooltip("Canonical character whose body-part dimensions and pivots this character follows. " +
                 "The first driver in GameCatalog is assigned automatically to future characters.")]
        public DriverDefinition poseReference;
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
