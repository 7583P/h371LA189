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
        [Min(0)] public int jeepFinalUpgradeCost = 5000;
        [Min(0)] public int formulaFinalUpgradeCost = 15000;
        [Min(0)] public int rallyFinalUpgradeCost = 10000;
        [Min(0)] public int sportBikeFinalUpgradeCost = 20000;
        [Min(0)] public int jeepFirstUpgradeCost = 100;
        [Min(0)] public int formulaFirstUpgradeCost = 250;
        [Min(0)] public int rallyFirstUpgradeCost = 180;
        [Min(0)] public int sportBikeFirstUpgradeCost = 300;
        [Range(1f, 3f)] public float costExponent = 1.45f;
        [Min(1)] public int roundingStep = 25;

        public int GetCostForNextTune(VehicleDefinition vehicle, VehicleUpgradeType type, int currentLevel)
        {
            if (vehicle == null) return -1;
            int maximum = vehicle.GetMaxTunes(type);
            if (maximum <= 1 || currentLevel < 1 || currentLevel >= maximum) return -1;
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
