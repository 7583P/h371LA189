using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public enum RaceItemType
    {
        None = 0,
        TurboPulse = 1,      // Boost surge
        KineticMissile = 2,  // Forward targeted projectile
        ElectroSpike = 3,    // Deployable rear hazard / mine
        MagneticShield = 4,  // Defensive shield & hazard absorption
        ShockwavePulse = 5,  // Radial area-of-effect pushback
        HyperCharge = 6      // Powerful comeback warp / rocket boost
    }

    [CreateAssetMenu(menuName = "Anime Hill Racing/Racing 3D/Item Definition", fileName = "NewRaceItem")]
    public sealed class RaceItemDefinition : ScriptableObject
    {
        public RaceItemType itemType;
        public string displayName = "TURBO PULSE";
        public Sprite icon;
        public GameObject entityPrefab;
        public float duration = 2.5f;
        public float speedMultiplier = 1.35f;
        public float effectRadius = 6.0f;
    }
}
