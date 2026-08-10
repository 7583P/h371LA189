using AnimeHillRacing.Vehicle;

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
        public static bool OpenTuningOnPreStart { get; set; }
        public static GarageSection RequestedGarageSection { get; set; }
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
    }
}
