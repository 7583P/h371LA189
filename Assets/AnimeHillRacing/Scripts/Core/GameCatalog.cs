using AnimeHillRacing.Terrain;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Core
{
    [CreateAssetMenu(menuName = "Anime Hill Racing/Game Catalog", fileName = "GameCatalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        public MapDefinition[] maps;
        public VehicleDefinition[] vehicles;
        public DriverDefinition[] drivers;
        public UpgradeEconomyDefinition upgradeEconomy;

        public int CountMaps(GameModeType mode)
        {
            int count = 0;
            if (maps == null)
                return count;
            for (int i = 0; i < maps.Length; i++)
                if (maps[i] != null && maps[i].supportedGameMode == mode)
                    count++;
            return count;
        }

        public MapDefinition GetCompatibleMap(GameModeType mode, int compatibleIndex)
        {
            if (maps == null)
                return null;
            int current = 0;
            for (int i = 0; i < maps.Length; i++)
            {
                if (maps[i] == null || maps[i].supportedGameMode != mode)
                    continue;
                if (current == compatibleIndex)
                    return maps[i];
                current++;
            }
            return null;
        }

        public MapDefinition FindMap(string mapId, GameModeType fallbackMode)
        {
            if (maps != null)
                for (int i = 0; i < maps.Length; i++)
                    if (maps[i] != null && maps[i].mapId == mapId && maps[i].supportedGameMode == fallbackMode)
                        return maps[i];
            return GetCompatibleMap(fallbackMode, 0);
        }

        public VehicleDefinition FindVehicle(string vehicleId)
        {
            if (vehicles != null)
                for (int i = 0; i < vehicles.Length; i++)
                    if (vehicles[i] != null && vehicles[i].vehicleId == vehicleId)
                        return vehicles[i];
            return vehicles != null && vehicles.Length > 0 ? vehicles[0] : null;
        }

        public DriverDefinition FindDriver(string driverId)
        {
            if (drivers != null)
                for (int i = 0; i < drivers.Length; i++)
                    if (drivers[i] != null && drivers[i].driverId == driverId)
                        return drivers[i];
            return drivers != null && drivers.Length > 0 ? drivers[0] : null;
        }
    }
}
