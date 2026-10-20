using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Racing 3D/Item Balance")]
    public sealed class RacingItemBalanceConfiguration : ScriptableObject
    {
        public static readonly ItemDistributionTable.ItemWeight[] LeaderDefaults = { W(RaceItemType.Coin10,65), W(RaceItemType.ElectroSpike,35) };
        // Front: defensive/neutral rewards. A small Golden Rock lets P2-P3 contest
        // a runaway leader; no Lancer, strong turbo or long-range tracking.
        public static readonly ItemDistributionTable.ItemWeight[] FrontDefaults = { W(RaceItemType.MagneticShield,40), W(RaceItemType.Coin10,36), W(RaceItemType.MiniTurbo,8), W(RaceItemType.ElectroSpike,8), W(RaceItemType.GoldenRock,8) };
        // Midfield: tactical variety that produces position fights.
        public static readonly ItemDistributionTable.ItemWeight[] MiddleDefaults = {
            W(RaceItemType.MagneticShield,18), W(RaceItemType.Coin10,12), W(RaceItemType.ElectroSpike,18), W(RaceItemType.KineticMissile,16),
            W(RaceItemType.MiniTurbo,14), W(RaceItemType.TurboPulse,8), W(RaceItemType.TwinRock,4), W(RaceItemType.GoldenRock,10) };
        // Rear: comeback mobility, guided offense and recovery tools. Gap-aware
        // weighting in ItemDistributionTable scales these by real separation.
        public static readonly ItemDistributionTable.ItemWeight[] RearDefaults = {
            W(RaceItemType.TurboPulse,18), W(RaceItemType.TripleTurbo,12), W(RaceItemType.HyperCharge,6), W(RaceItemType.Lancer,8),
            W(RaceItemType.GoldenRock,16), W(RaceItemType.TrackingDart,8), W(RaceItemType.HomingRocket,6), W(RaceItemType.KineticMissile,8),
            W(RaceItemType.AerialRecovery,6), W(RaceItemType.MagneticShield,6), W(RaceItemType.ShockwavePulse,2), W(RaceItemType.ElectroSpike,4) };
        [Tooltip("Position 1. Coin +10 or ordinary unguided Rock only.")]
        public ItemDistributionTable.ItemWeight[] leader = (ItemDistributionTable.ItemWeight[])LeaderDefaults.Clone();
        [Tooltip("Positions 2-3. Shield, Coin +10, mild utility, small Golden Rock.")]
        public ItemDistributionTable.ItemWeight[] front = (ItemDistributionTable.ItemWeight[])FrontDefaults.Clone();
        [Tooltip("Positions 4-8. Balanced tactical mix.")]
        public ItemDistributionTable.ItemWeight[] middle = (ItemDistributionTable.ItemWeight[])MiddleDefaults.Clone();
        [Tooltip("Positions 9–12. Catch-up items; ordinary shared vehicle effects.")]
        public ItemDistributionTable.ItemWeight[] rear = (ItemDistributionTable.ItemWeight[])RearDefaults.Clone();
        static ItemDistributionTable.ItemWeight W(RaceItemType type,int weight) => new ItemDistributionTable.ItemWeight { Type=type, Weight=weight };
    }
}
