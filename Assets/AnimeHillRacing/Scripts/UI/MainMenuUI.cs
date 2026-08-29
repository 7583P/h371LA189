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
            vehicleIndex >= 0 && vehicleIndex < catalog.vehicles.Length
                ? catalog.vehicles[vehicleIndex]
                : null;

        private void OnEnable()
        {
            SaveManager.CurrencyChanged -= RefreshCoins;
            SaveManager.CurrencyChanged += RefreshCoins;
            GameSession.ClearGarageContext();
            FindSavedVehicle();
            Refresh();
            EconomyCounterIconBinder.RefreshActiveScene();
        }

        private void OnDisable() => SaveManager.CurrencyChanged -= RefreshCoins;

        private void Update()
        {
            if (displayedCoins != SaveManager.TotalCoins || displayedGems != SaveManager.TotalGems)
                RefreshCoins();
        }

        public void PreviousVehicle() => CycleVehicle(-1);
        public void NextVehicle() => CycleVehicle(1);

        public void Race()
        {
            GameSession.BeginAdventureTrackSelection();
            GameSession.OpenTuningOnPreStart = false;
            SceneManager.LoadScene("PreStart");
        }

        public void Play() => Race();
        public void Adventure() => Race();

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
        public void Shop() => OpenGarage(GarageSection.Store);
        public void Settings() => OpenGarage(GarageSection.Settings);

        public void TimeTrial()
        {
            GameSession.ClearGarageContext();
            GameSession.SelectedGameMode = GameModeType.TimeTrial3D;
            GameSession.SelectedCircuitId = "procedural-test-circuit";
            SceneManager.LoadScene("TimeTrial3D");
        }

        private void OpenGarage(GarageSection section)
        {
            GameSession.RequestDirectGarage(section);
            SceneManager.LoadScene("Garage");
        }

        private void CycleVehicle(int direction)
        {
            int count = catalog != null && catalog.vehicles != null ? catalog.vehicles.Length : 0;
            if (count <= 0) return;

            int index = vehicleIndex >= 0 ? vehicleIndex : (direction >= 0 ? count - 1 : 0);
            for (int inspected = 0; inspected < count; inspected++)
            {
                index = (index + direction + count) % count;
                VehicleDefinition candidate = catalog.vehicles[index];
                if (candidate == null || !SaveManager.IsVehicleUnlocked(candidate.vehicleId,
                        candidate.unlockedByDefault)) continue;

                vehicleIndex = index;
                SaveManager.SelectedVehicleId = candidate.vehicleId;
                Refresh();
                return;
            }

            vehicleIndex = -1;
            Refresh();
        }

        private void Refresh()
        {
            RefreshCoins();
            VehicleDefinition vehicle = CurrentVehicle;
            bool unlocked = vehicle != null && SaveManager.IsVehicleUnlocked(vehicle.vehicleId,
                vehicle.unlockedByDefault);
            if (vehicleNameText != null)
            {
                vehicleNameText.text = vehicle != null ? vehicle.displayName.ToUpperInvariant() +
                    (unlocked ? string.Empty : "  —  LOCKED") : "NO VEHICLE";
                vehicleNameText.color = unlocked ? Color.white : new Color(1f, .28f, .22f, 1f);
            }
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
            vehicleIndex = -1;
            if (catalog?.vehicles == null) return;

            for (int i = 0; i < catalog.vehicles.Length; i++)
            {
                VehicleDefinition candidate = catalog.vehicles[i];
                if (candidate != null && candidate.vehicleId == SaveManager.SelectedVehicleId &&
                    SaveManager.IsVehicleUnlocked(candidate.vehicleId, candidate.unlockedByDefault))
                {
                    vehicleIndex = i;
                    return;
                }
            }

            for (int i = 0; i < catalog.vehicles.Length; i++)
            {
                VehicleDefinition candidate = catalog.vehicles[i];
                if (candidate == null || !SaveManager.IsVehicleUnlocked(candidate.vehicleId,
                        candidate.unlockedByDefault)) continue;
                vehicleIndex = i;
                SaveManager.SelectedVehicleId = candidate.vehicleId;
                return;
            }
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
