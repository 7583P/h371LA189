using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public enum RaceItemType
    {
        None = 0,
        TurboPulse = 1,      // Boost surge
        KineticMissile = 2,  // Forward targeted projectile
        ElectroSpike = 3,    // Legacy serialized ID: Rock
        MagneticShield = 4,  // Defensive shield & hazard absorption
        ShockwavePulse = 5,  // Radial area-of-effect pushback
        HyperCharge = 6,     // Legacy serialized ID: Super Turbo (ordinary shared boost)
        PulseMine = 7,
        TripleTurbo = 8,
        TwinRock = 9,
        TrackingDart = 10,
        HomingRocket = 11,
        AerialRecovery = 12,
        Coin10 = 13,         // Shared Adventure/Racing wallet; no vehicle effect
        MiniTurbo = 14,      // Short, ordinary item boost available to every racer
        Lancer = 15,         // Rear-pack rocket/meteor transformation; shared item physics
        GoldenRock = 16      // Guided rock; adjacent race-position target, shield absorbs it
    }

    [CreateAssetMenu(menuName = "Anime Hill Racing/Racing 3D/Item Definition", fileName = "NewRaceItem")]
    public sealed class RaceItemDefinition : ScriptableObject
    {
        public RaceItemType itemType;
        public string displayName = "TURBO 2S";
        public Sprite icon;
        public GameObject entityPrefab;
        public float duration = 2.5f;
        public float speedMultiplier = 1.35f;
        public float effectRadius = 6.0f;
    }
}
