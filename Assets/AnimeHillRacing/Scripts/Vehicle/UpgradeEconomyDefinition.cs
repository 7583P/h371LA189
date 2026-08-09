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

    [CreateAssetMenu(menuName = "Anime Hill Racing/Upgrade Economy", fileName = "UpgradeEconomy")]
    public sealed class UpgradeEconomyDefinition : ScriptableObject
    {
        [Tooltip("Index N is the cost from Tune N to Tune N+1.")]
        public int[] costs = { 100, 200, 350, 550, 800, 1150, 1550, 2050, 2700,
            3500, 4500, 5700, 7200, 9000, 11200 };

        public int GetCostForNextTune(int currentTune)
        {
            if (costs == null || currentTune < 0 || currentTune >= costs.Length)
                return -1;
            return Mathf.Max(0, costs[currentTune]);
        }
    }
}
