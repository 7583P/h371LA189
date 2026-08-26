using System;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.Vehicle;
using AnimeHillRacing.Chests;
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
        public ChestCatalog chestCatalog;

        public void SortPurchasableContent()
        {
            if (maps != null)
                Array.Sort(maps, CompareMaps);
            if (vehicles != null)
                Array.Sort(vehicles, CompareVehicles);
        }

        private void OnEnable()
        {
            // Player builds must use the same price order serialized by the editor. This also
            // protects runtime-created catalogs and older assets that have not been re-saved yet.
            SortPurchasableContent();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SortPurchasableContent();
        }
#endif

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
            if (maps != null)
                for (int i = 0; i < maps.Length; i++)
                    if (maps[i] != null && maps[i].supportedGameMode == fallbackMode && maps[i].unlockedByDefault)
                        return maps[i];
            return GetCompatibleMap(fallbackMode, 0);
        }

        public VehicleDefinition FindVehicle(string vehicleId)
        {
            if (vehicles != null)
                for (int i = 0; i < vehicles.Length; i++)
                    if (vehicles[i] != null && vehicles[i].vehicleId == vehicleId)
                        return vehicles[i];
            if (vehicles != null)
                for (int i = 0; i < vehicles.Length; i++)
                    if (vehicles[i] != null && vehicles[i].unlockedByDefault)
                        return vehicles[i];
            return vehicles != null && vehicles.Length > 0 ? vehicles[0] : null;
        }

        private static int CompareMaps(MapDefinition left, MapDefinition right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            // Track grids are filled from top to bottom, so the cheapest tracks come first.
            int price = left.coinPrice.CompareTo(right.coinPrice);
            if (price != 0) return price;
            int name = string.Compare(left.displayName, right.displayName, StringComparison.OrdinalIgnoreCase);
            return name != 0 ? name : string.Compare(left.mapId, right.mapId, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareVehicles(VehicleDefinition left, VehicleDefinition right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            // Vehicle arrows move forward through this array: Jeep/free vehicles stay on the
            // left and progressively more expensive vehicles appear to the right.
            int price = left.coinPrice.CompareTo(right.coinPrice);
            if (price != 0) return price;
            int defaultUnlock = right.unlockedByDefault.CompareTo(left.unlockedByDefault);
            if (defaultUnlock != 0) return defaultUnlock;
            int name = string.Compare(left.displayName, right.displayName, StringComparison.OrdinalIgnoreCase);
            return name != 0
                ? name
                : string.Compare(left.vehicleId, right.vehicleId, StringComparison.OrdinalIgnoreCase);
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
