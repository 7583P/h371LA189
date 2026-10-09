using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Racing 3D/Item Balance")]
    public sealed class RacingItemBalanceConfiguration : ScriptableObject
    {
        public static readonly ItemDistributionTable.ItemWeight[] LeaderDefaults = { W(RaceItemType.Coin10,65), W(RaceItemType.ElectroSpike,35) };
        public static readonly ItemDistributionTable.ItemWeight[] FrontDefaults = { W(RaceItemType.MagneticShield,40), W(RaceItemType.Coin10,40), W(RaceItemType.GoldenRock,20) };
        public static readonly ItemDistributionTable.ItemWeight[] MiddleDefaults = { W(RaceItemType.MagneticShield,30), W(RaceItemType.Coin10,30), W(RaceItemType.MiniTurbo,20), W(RaceItemType.GoldenRock,20) };
        public static readonly ItemDistributionTable.ItemWeight[] RearDefaults = {
            W(RaceItemType.TurboPulse,20), W(RaceItemType.ElectroSpike,6), W(RaceItemType.KineticMissile,18),
            W(RaceItemType.MagneticShield,8), W(RaceItemType.AerialRecovery,8), W(RaceItemType.TripleTurbo,8),
            W(RaceItemType.TwinRock,3), W(RaceItemType.TrackingDart,4), W(RaceItemType.HyperCharge,2),
            W(RaceItemType.ShockwavePulse,1), W(RaceItemType.HomingRocket,2), W(RaceItemType.Lancer,8), W(RaceItemType.GoldenRock,12) };
        [Tooltip("Position 1. Coin +10 or ordinary unguided Rock only.")]
        public ItemDistributionTable.ItemWeight[] leader = (ItemDistributionTable.ItemWeight[])LeaderDefaults.Clone();
        [Tooltip("Positions 2-3. Shield, Coin +10 or Golden Rock.")]
        public ItemDistributionTable.ItemWeight[] front = (ItemDistributionTable.ItemWeight[])FrontDefaults.Clone();
        [Tooltip("Positions 4-6. Shield, Coin +10, short Mini Turbo or Golden Rock.")]
        public ItemDistributionTable.ItemWeight[] middle = (ItemDistributionTable.ItemWeight[])MiddleDefaults.Clone();
        [Tooltip("Positions 7–12. Catch-up items; ordinary shared vehicle effects.")]
        public ItemDistributionTable.ItemWeight[] rear = (ItemDistributionTable.ItemWeight[])RearDefaults.Clone();
        static ItemDistributionTable.ItemWeight W(RaceItemType type,int weight) => new ItemDistributionTable.ItemWeight { Type=type, Weight=weight };
    }
}
