using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private GaragePreviewRenderer preview;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text gemsText;
        [SerializeField] private Text vehicleNameText;

        private int vehicleIndex;
        private int displayedCoins = int.MinValue;
        private int displayedGems = int.MinValue;

        private VehicleDefinition CurrentVehicle => catalog != null && catalog.vehicles != null &&
            catalog.vehicles.Length > 0
                ? catalog.vehicles[Mathf.Clamp(vehicleIndex, 0, catalog.vehicles.Length - 1)]
                : null;

        private void OnEnable()
        {
            FindSavedVehicle();
            Refresh();
        }

        private void Update()
        {
            if (displayedCoins != SaveManager.TotalCoins || displayedGems != SaveManager.TotalGems)
                RefreshCoins();
        }

        public void PreviousVehicle() => CycleVehicle(-1);
        public void NextVehicle() => CycleVehicle(1);

        public void Race()
        {
            GameSession.OpenTuningOnPreStart = false;
            GameSession.SelectedTransmission = TransmissionMode.Auto;
            SceneManager.LoadScene("PreStart");
        }

        public void Play() => Race();

        public void ThreeD()
        {
            GameSession.SelectedGameMode = GameModeType.Adventure3D;
            Race();
        }

        public void Vehicles() => OpenGarage(GarageSection.Vehicle);
        public void Vehicle() => OpenGarage(GarageSection.Vehicle);
        public void Garage() => OpenGarage(GarageSection.Vehicle);
        public void Tuning() => OpenGarage(GarageSection.Tuning);
        public void Upgrade() => OpenGarage(GarageSection.Upgrade);
        public void Skins() => OpenGarage(GarageSection.Skins);
        public void Store() => OpenGarage(GarageSection.Store);
        public void Settings() => OpenGarage(GarageSection.Settings);

        public void TimeTrial()
        {
            GameSession.SelectedGameMode = GameModeType.TimeTrial3D;
            GameSession.SelectedCircuitId = "procedural-test-circuit";
            SceneManager.LoadScene("TimeTrial3D");
        }

        private void OpenGarage(GarageSection section)
        {
            GameSession.RequestedGarageSection = section;
            SceneManager.LoadScene("Garage");
        }

        private void CycleVehicle(int direction)
        {
            int count = catalog != null && catalog.vehicles != null ? catalog.vehicles.Length : 0;
            if (count <= 0) return;
            vehicleIndex = (vehicleIndex + direction + count) % count;
            if (CurrentVehicle != null) SaveManager.SelectedVehicleId = CurrentVehicle.vehicleId;
            Refresh();
        }

        private void Refresh()
        {
            RefreshCoins();
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicleNameText != null)
                vehicleNameText.text = vehicle != null ? vehicle.displayName.ToUpperInvariant() : "NO VEHICLE";
            if (vehicle == null || preview == null) return;
            VehicleDefinition.CustomizationOption skin = vehicle.FindSkin(SaveManager.GetSelectedSkinId(vehicle));
            VehicleDefinition.CustomizationOption wheel = vehicle.FindWheel(SaveManager.GetSelectedWheelId(vehicle));
            string fallbackDriver = catalog?.drivers != null && catalog.drivers.Length > 0
                ? catalog.drivers[0].driverId : string.Empty;
            DriverDefinition driver = catalog?.FindDriver(SaveManager.GetSelectedDriverId(fallbackDriver));
            preview.Apply(vehicle, skin, wheel, driver);
        }

        private void RefreshCoins()
        {
            displayedCoins = SaveManager.TotalCoins;
            displayedGems = SaveManager.TotalGems;
            if (coinsText != null) coinsText.text = $"COINS  {displayedCoins:N0}";
            if (gemsText != null) gemsText.text = $"GEMS  {displayedGems:N0}";
        }

        private void FindSavedVehicle()
        {
            vehicleIndex = 0;
            if (catalog?.vehicles == null) return;
            for (int i = 0; i < catalog.vehicles.Length; i++)
                if (catalog.vehicles[i] != null && catalog.vehicles[i].vehicleId == SaveManager.SelectedVehicleId)
                    vehicleIndex = i;
        }

        public void Assign(GameCatalog gameCatalog, GaragePreviewRenderer previewRenderer, Text totalCoins,
            Text vehicleLabel, Text totalGems = null)
        {
            catalog = gameCatalog;
            preview = previewRenderer;
            coinsText = totalCoins;
            gemsText = totalGems;
            vehicleNameText = vehicleLabel;
            FindSavedVehicle();
            Refresh();
        }
    }
}
