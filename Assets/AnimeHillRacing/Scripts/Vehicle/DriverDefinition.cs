using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Driver Definition", fileName = "Driver")]
    public sealed class DriverDefinition : ScriptableObject
    {
        public string driverId = "stickman-black";
        public string displayName = "STICKMAN BLACK";
        public bool unlockedByDefault = true;
        public Sprite previewSprite;
        public Sprite head;
        public Sprite torso;
        public Sprite upperArm;
        public Sprite forearm;
        public Sprite thigh;
        public Sprite lowerLeg;
    }
}
