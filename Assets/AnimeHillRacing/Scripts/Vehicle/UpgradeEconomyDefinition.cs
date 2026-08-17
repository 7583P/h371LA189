using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public enum VehicleUpgradeType
    {
        Engine = 0,
        Acceleration = 1,
        Suspension = 2,
        Grip = 3,
        Fuel = 4,
        AirControl = 5,
        Downforce = 6,
        Turbo = 7
    }

    public enum TunePurchaseResult
    {
        Success,
        InvalidConfiguration,
        MaximumLevel,
        NotEnoughCoins
    }

    [CreateAssetMenu(menuName = "Anime Hill Racing/Upgrade Economy", fileName = "UpgradeEconomy")]
    public sealed class UpgradeEconomyDefinition : ScriptableObject
    {
        [Min(0)] public int jeepFinalUpgradeCost = 50000;
        [Min(0)] public int formulaFinalUpgradeCost = 150000;
        [Min(0)] public int rallyFinalUpgradeCost = 100000;
        [Min(0)] public int sportBikeFinalUpgradeCost = 200000;
        [Min(0)] public int jeepFirstUpgradeCost = 1000;
        [Min(0)] public int formulaFirstUpgradeCost = 2500;
        [Min(0)] public int rallyFirstUpgradeCost = 1800;
        [Min(0)] public int sportBikeFirstUpgradeCost = 3000;
        [Range(1f, 3f)] public float costExponent = 1.45f;
        [Min(1)] public int roundingStep = 250;

        private static readonly int[] s_hillAdventurerSpeedCosts = { 300, 500, 550, 650, 800, 950, 1100, 1250, 1450, 1650, 1850, 2100, 2350, 2600, 2850, 3150, 3400, 3700, 3800 };
        private static readonly int[] s_hillAdventurerFuelCosts = { 300, 500, 550, 650, 800, 950, 1100, 1250, 1450, 1650, 1850, 2050, 2300, 2550, 2800, 3050, 3200 };
        private static readonly int[] s_hillAdventurerAccelCosts = { 350, 450, 600, 750, 950, 1150, 1350, 1700, 1950, 2250, 2550, 2850, 3100 };
        private static readonly int[] s_hillAdventurerSuspCosts = { 350, 450, 600, 750, 900, 1100, 1400, 1650, 1900, 2150, 2450, 2800, 3000 };
        private static readonly int[] s_hillAdventurerGripCosts = { 350, 450, 550, 750, 900, 1100, 1300, 1550, 1800, 2050, 2300, 2600, 2800 };

        public int GetCostForNextTune(VehicleDefinition vehicle, VehicleUpgradeType type, int currentLevel)
        {
            if (vehicle == null) return -1;
            int maximum = vehicle.GetMaxTunes(type);
            if (maximum <= 1 || currentLevel < 1 || currentLevel >= maximum) return -1;
            if (vehicle.archetype == VehicleArchetype.HillAdventurer)
            {
                int index = currentLevel - 1;
                switch (type)
                {
                    case VehicleUpgradeType.Engine:
                        return index >= 0 && index < s_hillAdventurerSpeedCosts.Length ? s_hillAdventurerSpeedCosts[index] : -1;
                    case VehicleUpgradeType.Fuel:
                        return index >= 0 && index < s_hillAdventurerFuelCosts.Length ? s_hillAdventurerFuelCosts[index] : -1;
                    case VehicleUpgradeType.Acceleration:
                    case VehicleUpgradeType.Turbo:
                        return index >= 0 && index < s_hillAdventurerAccelCosts.Length ? s_hillAdventurerAccelCosts[index] : -1;
                    case VehicleUpgradeType.Suspension:
                        return index >= 0 && index < s_hillAdventurerSuspCosts.Length ? s_hillAdventurerSuspCosts[index] : -1;
                    case VehicleUpgradeType.Grip:
                        return index >= 0 && index < s_hillAdventurerGripCosts.Length ? s_hillAdventurerGripCosts[index] : -1;
                    default:
                        return -1;
                }
            }
            int first = FirstCost(vehicle.archetype);
            int final = FinalCost(vehicle.archetype);
            float t = maximum <= 2 ? 1f : Mathf.Clamp01((currentLevel - 1f) / (maximum - 2f));
            float curved = Mathf.Pow(t, Mathf.Max(1f, costExponent));
            if (currentLevel == maximum - 1) return final;
            int raw = Mathf.RoundToInt(Mathf.Lerp(first, final, curved));
            int step = Mathf.Max(1, roundingStep);
            return Mathf.Max(first, Mathf.RoundToInt(raw / (float)step) * step);
        }

        public int FinalCost(VehicleArchetype archetype) => archetype == VehicleArchetype.Formula
            ? formulaFinalUpgradeCost : archetype == VehicleArchetype.Rally ? rallyFinalUpgradeCost :
            archetype == VehicleArchetype.SportBike ? sportBikeFinalUpgradeCost : jeepFinalUpgradeCost;

        private int FirstCost(VehicleArchetype archetype) => archetype == VehicleArchetype.Formula
            ? formulaFirstUpgradeCost : archetype == VehicleArchetype.Rally ? rallyFirstUpgradeCost :
            archetype == VehicleArchetype.SportBike ? sportBikeFirstUpgradeCost : jeepFirstUpgradeCost;
    }
}
