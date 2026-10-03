using AnimeHillRacing.Vehicle;
using AnimeHillRacing.Terrain;

namespace AnimeHillRacing.Core
{
    public enum GarageSection
    {
        Vehicle,
        Tuning,
        Upgrade,
        Skins,
        Store,
        Settings
    }

    public static class GameSession
    {
        private static bool adventureGarageContext;
        private static string pendingAdventureMapId = string.Empty;

        public static bool OpenTuningOnPreStart { get; set; }
        public static bool IsAdventureSelectionRequested { get; private set; }
        public static GarageSection RequestedGarageSection { get; set; }

        /// <summary>
        /// True only when Garage was opened by selecting an Adventure track during this app session.
        /// The persisted selected map is intentionally not enough to enable START: direct Garage
        /// navigation must never inherit an old run by accident.
        /// </summary>
        public static bool HasPendingAdventureTrack =>
            adventureGarageContext && !string.IsNullOrEmpty(pendingAdventureMapId);

        public static string PendingAdventureMapId =>
            HasPendingAdventureTrack ? pendingAdventureMapId : string.Empty;
        public static GameModeType SelectedGameMode
        {
            get => SaveManager.SelectedGameMode;
            set => SaveManager.SelectedGameMode = value;
        }

        public static string SelectedMapId
        {
            get => SaveManager.SelectedMapId;
            set => SaveManager.SelectedMapId = value;
        }

        public static string SelectedVehicleId
        {
            get => SaveManager.SelectedVehicleId;
            set => SaveManager.SelectedVehicleId = value;
        }

        public static string SelectedCircuitId
        {
            get => SaveManager.SelectedCircuitId;
            set => SaveManager.SelectedCircuitId = value;
        }

        public static TransmissionMode SelectedTransmission
        {
            get => SaveManager.SelectedTransmission;
            set => SaveManager.SelectedTransmission = value;
        }

        public static void BeginAdventureTrackSelection()
        {
            ClearGarageContext();
            IsAdventureSelectionRequested = true;
            SelectedGameMode = GameModeType.Adventure2_5D;
            SelectedTransmission = TransmissionMode.Auto;
            RequestedGarageSection = GarageSection.Vehicle;
        }

        public static bool PrepareAdventureGarage(MapDefinition map)
        {
            if (map == null || string.IsNullOrWhiteSpace(map.mapId) ||
                map.supportedGameMode != GameModeType.Adventure2_5D ||
                !SaveManager.IsMapUnlocked(map.mapId, map.unlockedByDefault))
            {
                ClearGarageContext();
                return false;
            }

            SelectedGameMode = GameModeType.Adventure2_5D;
            SelectedMapId = map.mapId;
            SelectedTransmission = TransmissionMode.Auto;
            // Adventure preparation opens directly on the useful upgrade cards. The regular
            // Garage still opens on Vehicle through RequestDirectGarage.
            RequestedGarageSection = GarageSection.Tuning;
            pendingAdventureMapId = map.mapId;
            adventureGarageContext = true;
            return true;
        }

        public static void RequestDirectGarage(GarageSection section)
        {
            ClearGarageContext();
            RequestedGarageSection = section;
        }

        public static void ClearGarageContext()
        {
            IsAdventureSelectionRequested = false;
            adventureGarageContext = false;
            pendingAdventureMapId = string.Empty;
        }

        /// <summary>
        /// Resolves the pending track by exact id and mode. GameCatalog.FindMap deliberately has a
        /// fallback, which is useful for gameplay recovery but unsafe for a START confirmation.
        /// </summary>
        public static bool TryResolvePendingAdventureTrack(GameCatalog catalog, out MapDefinition map)
        {
            map = null;
            if (!HasPendingAdventureTrack || catalog?.maps == null) return false;

            for (int i = 0; i < catalog.maps.Length; i++)
            {
                MapDefinition candidate = catalog.maps[i];
                if (candidate == null || candidate.supportedGameMode != GameModeType.Adventure2_5D ||
                    candidate.mapId != pendingAdventureMapId) continue;
                if (!SaveManager.IsMapUnlocked(candidate.mapId, candidate.unlockedByDefault))
                {
                    ClearGarageContext();
                    return false;
                }
                map = candidate;
                return true;
            }

            ClearGarageContext();
            return false;
        }

        public static bool TryCommitPendingAdventureRun(GameCatalog catalog, out string gameplayScene)
        {
            gameplayScene = string.Empty;
            if (!TryResolvePendingAdventureTrack(catalog, out MapDefinition map)) return false;
            SelectedGameMode = GameModeType.Adventure2_5D;
            SelectedMapId = map.mapId;
            SelectedTransmission = TransmissionMode.Auto;
            gameplayScene = "Adventure2_5D";
            // The exact selection is now committed to SaveManager. Do not leak a stale
            // Garage START context into later menu visits after gameplay begins.
            ClearGarageContext();
            return true;
        }
    }
}
