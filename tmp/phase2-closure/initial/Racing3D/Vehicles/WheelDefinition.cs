using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Racing 3D/Wheel Definition", fileName = "NewWheel")]
    public sealed class WheelDefinition : ScriptableObject
    {
        public string wheelId = "wheel-default";
        public string displayName = "STANDARD WHEELS";
        public Sprite previewSprite;
        public bool unlockedByDefault = true;
        public int coinPrice;
        public int gemPrice;

        public WheelRacingModifiers modifiers = WheelRacingModifiers.Zero;
    }
}
