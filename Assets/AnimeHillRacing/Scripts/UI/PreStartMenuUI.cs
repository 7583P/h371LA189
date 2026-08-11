using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class PreStartMenuUI : MonoBehaviour
    {
        private enum Step { Mode, Vehicle }

        [SerializeField] private GameCatalog catalog;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text progressText;
        [SerializeField] private GameObject[] stepPanels;
        [SerializeField] private Image[] modeCards;
        [SerializeField] private RectTransform[] modeCardRects;
        [SerializeField] private Text[] modeCardStatusTexts;
        [SerializeField] private Text modeText;
        [SerializeField] private Text mapText;
        [SerializeField] private Text vehicleText;
        [SerializeField] private Image vehiclePreviewImage;
        [SerializeField] private GaragePreviewRenderer vehiclePreviewRenderer;
        [SerializeField] private GameObject vehiclePreviewFallback;
        [SerializeField] private Text vehicleStatsText;
        [SerializeField] private Text tuningSummaryText;
        [SerializeField] private Text transmissionText;
        [SerializeField] private GameObject tuningPanel;
        [SerializeField] private Text[] tuningNameTexts;
        [SerializeField] private Text[] tuningLevelTexts;
        [SerializeField] private Text[] tuningValueTexts;
        [SerializeField] private Text[] tuningCostTexts;
        [SerializeField] private Button[] tuningButtons;
        [SerializeField] private Text tuningCoinsText;

        private Step step;
        private GameModeType mode;
        private int mapIndex;
        private int vehicleIndex;

        private VehicleDefinition CurrentVehicle => catalog != null && catalog.vehicles != null && catalog.vehicles.Length > 0
            ? catalog.vehicles[Mathf.Clamp(vehicleIndex, 0, catalog.vehicles.Length - 1)] : null;
        private MapDefinition CurrentMap => catalog?.GetCompatibleMap(mode, mapIndex);

        private void Start()
        {
            mode = GameSession.SelectedGameMode;
            FindSavedVehicle();
            FindSavedMap();
            GameSession.OpenTuningOnPreStart = false;
            GameSession.SelectedTransmission = TransmissionMode.Auto;
            ShowStep(Step.Mode);
        }

        public void SelectAdventure2_5D() => SelectMode(GameModeType.Adventure2_5D);
        public void SelectAdventure3D() => SelectMode(GameModeType.Adventure3D);
        public void SelectTimeTrial3D() => SelectMode(GameModeType.TimeTrial3D);
        public void SelectClimb() => SelectAdventure2_5D();
        public void SelectRoad() => SelectAdventure3D();

        private void SelectMode(GameModeType selected)
        {
            mode = selected;
            GameSession.SelectedGameMode = mode;
            FindSavedMap();
            if (CurrentMap == null) mapIndex = 0;
            Refresh();
        }

        public void ContinueStep()
        {
            if (step == Step.Vehicle) { StartGame(); return; }
            ShowStep(Step.Vehicle);
        }

        public void BackStep()
        {
            if (step == Step.Mode) { SceneManager.LoadScene("MainMenu"); return; }
            ShowStep(Step.Mode);
        }

        public void Back() => BackStep();
        public void PreviousMap() { CycleMap(-1); }
        public void NextMap() { CycleMap(1); }
        public void PreviousVehicle() { CycleVehicle(-1); }
        public void NextVehicle() { CycleVehicle(1); }

        public void ToggleTransmission()
        {
            GameSession.SelectedTransmission = TransmissionMode.Auto;
            Refresh();
        }

        public void OpenTuning()
        {
            GameSession.RequestedGarageSection = GarageSection.Tuning;
            SceneManager.LoadScene("Garage");
        }

        public void CloseTuning()
        {
            if (tuningPanel != null) tuningPanel.SetActive(false);
            ShowStep(Step.Vehicle);
        }

        public void UpgradeEngine() => Purchase(VehicleUpgradeType.Engine);
        public void UpgradeAcceleration() => Purchase(VehicleUpgradeType.Acceleration);
        public void UpgradeSuspension() => Purchase(VehicleUpgradeType.Suspension);
        public void UpgradeGrip() => Purchase(VehicleUpgradeType.Grip);
        public void UpgradeThird() { if (CurrentVehicle != null) Purchase(CurrentVehicle.GetTuneTypeForSlot(2)); }
        public void UpgradeFourth() { if (CurrentVehicle != null) Purchase(CurrentVehicle.GetTuneTypeForSlot(3)); }
        public void UpgradeFuel() => Purchase(VehicleUpgradeType.Fuel);

        public void StartGame()
        {
            MapDefinition map = CurrentMap;
            VehicleDefinition vehicle = CurrentVehicle;
            if (map == null || vehicle == null) return;
            GameSession.SelectedGameMode = mode;
            GameSession.SelectedMapId = map.mapId;
            if (mode == GameModeType.TimeTrial3D) GameSession.SelectedCircuitId = map.mapId;
            GameSession.SelectedVehicleId = vehicle.vehicleId;
            SceneManager.LoadScene(mode == GameModeType.Adventure2_5D ? "Adventure2_5D" :
                mode == GameModeType.Adventure3D ? "Adventure3D" : "TimeTrial3D");
        }

        private void ShowStep(Step target)
        {
            step = target;
            // UnityEngine.Object uses a custom null state; null-conditional access can still throw an
            // UnassignedReferenceException for an intentionally empty serialized field.
            if (tuningPanel != null) tuningPanel.SetActive(false);
            SetStepPanelsVisible(true);
            Refresh();
        }

        private void SetStepPanelsVisible(bool showCurrent)
        {
            if (stepPanels == null) return;
            for (int i = 0; i < stepPanels.Length; i++)
                if (stepPanels[i] != null) stepPanels[i].SetActive(showCurrent && i == (int)step);
        }

        private void CycleMap(int direction)
        {
            int count = catalog != null ? catalog.CountMaps(mode) : 0;
            if (count > 0) mapIndex = PositiveModulo(mapIndex + direction, count);
            Refresh();
        }

        private void CycleVehicle(int direction)
        {
            int count = catalog != null && catalog.vehicles != null ? catalog.vehicles.Length : 0;
            if (count > 0) vehicleIndex = PositiveModulo(vehicleIndex + direction, count);
            if (CurrentVehicle != null) GameSession.SelectedVehicleId = CurrentVehicle.vehicleId;
            Refresh();
        }

        private void Purchase(VehicleUpgradeType type)
        {
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null || catalog?.upgradeEconomy == null) return;
            SaveManager.TryPurchaseTune(vehicle, type, catalog.upgradeEconomy, out _);
            Refresh();
        }

        private void Refresh()
        {
            MapDefinition map = CurrentMap;
            VehicleDefinition vehicle = CurrentVehicle;
            if (coinsText != null) coinsText.text = $"COINS  {SaveManager.TotalCoins:N0}";
            if (progressText != null) progressText.text = ProgressLabel();
            if (modeText != null) modeText.text = GameModeNames.DisplayName(mode);
            if (mapText != null)
            {
                string best = mode == GameModeType.TimeTrial3D
                    ? $"BEST  {FormatTime(SaveManager.GetBestTime(map != null ? map.mapId : string.Empty))}"
                    : $"BEST  {SaveManager.GetBestDistance(mode, map != null ? map.mapId : string.Empty):N0} m";
                mapText.text = map != null ? $"{map.displayName.ToUpperInvariant()}\nDIFFICULTY  NORMAL\n{best}" : "NO COMPATIBLE MAP";
            }
            if (vehicleText != null) vehicleText.text = vehicle != null ? vehicle.displayName.ToUpperInvariant() : "NO VEHICLE";
            if (vehiclePreviewImage != null)
            {
                vehiclePreviewImage.sprite = vehicle != null ? vehicle.previewSprite : null;
                vehiclePreviewImage.enabled = vehiclePreviewRenderer == null && vehiclePreviewImage.sprite != null;
                vehiclePreviewImage.preserveAspect = true;
            }
            if (vehiclePreviewFallback != null)
                vehiclePreviewFallback.SetActive(vehiclePreviewRenderer == null && vehicle != null &&
                                                 vehicle.previewSprite == null);
            if (vehiclePreviewRenderer != null && vehicle != null)
            {
                VehicleDefinition.CustomizationOption skin = vehicle.FindSkin(SaveManager.GetSelectedSkinId(vehicle));
                VehicleDefinition.CustomizationOption wheel = vehicle.FindWheel(SaveManager.GetSelectedWheelId(vehicle));
                string fallbackDriver = catalog?.drivers != null && catalog.drivers.Length > 0
                    ? catalog.drivers[0].driverId : string.Empty;
                DriverDefinition driver = catalog?.FindDriver(SaveManager.GetSelectedDriverId(fallbackDriver));
                vehiclePreviewRenderer.Apply(vehicle, skin, wheel, driver);
            }
            if (transmissionText != null) transmissionText.text = "AUTOMATIC";
            RefreshModeCards();
            if (vehicle != null)
            {
                VehicleUpgradeState state = SaveManager.LoadUpgrades(vehicle.vehicleId, vehicle);
                ResolvedVehicleStats stats = vehicle.Resolve(state);
                string tuneLines = $"SPEED   {stats.topSpeedMps * 3.6f:0} KM/H\n" +
                                   $"ACCELERATION   {stats.zeroToHundredTargetSeconds:0.0} SEC\n" +
                                   $"SUSPENSION   {stats.suspensionTravel:0.00} M\n" +
                                   $"GRIP   {stats.gripRating} / {VehicleDefinition.GlobalGripMaximum}\n" +
                                   $"FUEL   {stats.totalRunFuelDuration:0} SEC";
                if (vehicleStatsText != null)
                    vehicleStatsText.text = tuneLines;
                if (tuningSummaryText != null)
                    tuningSummaryText.text = $"MODE   {GameModeNames.DisplayName(mode)}\n" +
                                             $"MAP   {(map != null ? map.displayName.ToUpperInvariant() : "-")}\n" +
                                             $"VEHICLE   {vehicle.displayName.ToUpperInvariant()}\n\n" +
                                             tuneLines;
            }
        }

        private void RefreshModeCards()
        {
            int count = modeCards != null ? modeCards.Length : 0;
            for (int i = 0; i < count; i++)
            {
                bool selected = i == (int)mode;
                if (modeCards != null && i < modeCards.Length && modeCards[i] != null)
                {
                    modeCards[i].color = selected ? new Color(0.18f, 0.48f, 0.76f, 1f) : new Color(0.055f, 0.10f, 0.18f, 0.98f);
                    Outline outline = modeCards[i].GetComponent<Outline>();
                    if (outline != null)
                    {
                        outline.effectColor = selected ? new Color(0.38f, 0.88f, 1f, 1f) : new Color(0.18f, 0.30f, 0.42f, 0.75f);
                        outline.effectDistance = selected ? new Vector2(4f, -4f) : new Vector2(1.5f, -1.5f);
                    }
                }
                if (modeCardRects != null && i < modeCardRects.Length && modeCardRects[i] != null)
                    modeCardRects[i].localScale = selected ? Vector3.one * 1.025f : Vector3.one;
                if (modeCardStatusTexts != null && i < modeCardStatusTexts.Length && modeCardStatusTexts[i] != null)
                    modeCardStatusTexts[i].text = selected ? "✓  SELECTED" : "SELECT";
            }
        }

        private void RefreshTuning()
        {
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null) return;
            VehicleUpgradeState state = SaveManager.LoadUpgrades(vehicle.vehicleId, vehicle);
            int visibleSlots = vehicle.GetTuneSlotCount();
            for (int i = 0; i < 5; i++)
            {
                bool visible = i < visibleSlots;
                if (tuningNameTexts != null && i < tuningNameTexts.Length && tuningNameTexts[i] != null)
                    tuningNameTexts[i].transform.parent.gameObject.SetActive(visible);
                if (!visible) continue;
                VehicleUpgradeType type = vehicle.GetTuneTypeForSlot(i);
                int current = state.GetTune(type);
                int maximum = vehicle.GetMaxTunes(type);
                bool atMax = current >= maximum;
                if (tuningNameTexts != null && i < tuningNameTexts.Length && tuningNameTexts[i] != null)
                    tuningNameTexts[i].text = vehicle.GetTuneDisplayName(type);
                if (tuningLevelTexts != null && i < tuningLevelTexts.Length && tuningLevelTexts[i] != null)
                    tuningLevelTexts[i].text = $"LEVEL {current} / {maximum}";
                if (tuningValueTexts != null && i < tuningValueTexts.Length && tuningValueTexts[i] != null)
                    tuningValueTexts[i].text = TuneComparison(vehicle, state, type, atMax);
                int cost = atMax || catalog?.upgradeEconomy == null ? -1 : catalog.upgradeEconomy.GetCostForNextTune(current);
                if (tuningCostTexts != null && i < tuningCostTexts.Length && tuningCostTexts[i] != null)
                    tuningCostTexts[i].text = atMax ? "MAX" : $"TUNE\n{cost:N0} COINS";
                if (tuningButtons != null && i < tuningButtons.Length && tuningButtons[i] != null)
                    tuningButtons[i].interactable = !atMax;
            }
            if (tuningCoinsText != null) tuningCoinsText.text = $"COINS  {SaveManager.TotalCoins:N0}";
        }

        private static string TuneComparison(VehicleDefinition vehicle, VehicleUpgradeState current,
            VehicleUpgradeType type, bool atMax)
        {
            ResolvedVehicleStats currentStats = vehicle.Resolve(current);
            var next = new VehicleUpgradeState();
            next.CopyFrom(current);
            next.SetTune(type, current.GetTune(type) + (atMax ? 0 : 1));
            next.ClampTo(vehicle);
            ResolvedVehicleStats nextStats = vehicle.Resolve(next);
            string arrow = atMax ? "" : "  →  ";
            switch (type)
            {
                case VehicleUpgradeType.Engine:
                    return atMax ? $"{currentStats.topSpeedMps * 3.6f:0} KM/H   MAX"
                        : $"MAX SPEED  {currentStats.topSpeedMps * 3.6f:0}{arrow}{nextStats.topSpeedMps * 3.6f:0} KM/H\n" +
                          $"POWER  {currentStats.peakPowerKw:0.0}{arrow}{nextStats.peakPowerKw:0.0} KW";
                case VehicleUpgradeType.Acceleration:
                    return atMax ? $"0-100  {currentStats.zeroToHundredTargetSeconds:0.0} SEC   MAX"
                        : $"0-100  {currentStats.zeroToHundredTargetSeconds:0.0}{arrow}{nextStats.zeroToHundredTargetSeconds:0.0} SEC";
                case VehicleUpgradeType.Suspension:
                    return atMax ? $"TRAVEL  {currentStats.suspensionTravel:0.000} M   MAX"
                        : $"TRAVEL  {currentStats.suspensionTravel:0.000}{arrow}{nextStats.suspensionTravel:0.000} M\n" +
                          $"SPRING  {currentStats.spring:0}{arrow}{nextStats.spring:0}   DAMPER  {currentStats.damping:0}{arrow}{nextStats.damping:0}";
                case VehicleUpgradeType.Grip:
                    return atMax ? $"GRIP  {currentStats.gripRating}/{VehicleDefinition.GlobalGripMaximum}   MAX"
                        : $"GRIP  {currentStats.gripRating}/{VehicleDefinition.GlobalGripMaximum}{arrow}{nextStats.gripRating}/{VehicleDefinition.GlobalGripMaximum}";
                case VehicleUpgradeType.AirControl:
                    return atMax ? $"AIR TORQUE  {currentStats.airControlTorque:0} NM   MAX"
                        : $"AIR TORQUE  {currentStats.airControlTorque:0}{arrow}{nextStats.airControlTorque:0} NM";
                case VehicleUpgradeType.Downforce:
                    return atMax ? $"AIR DOWNFORCE  {currentStats.airborneDownforceAcceleration:0.0} M/S²   MAX"
                        : $"AIR DOWNFORCE  {currentStats.airborneDownforceAcceleration:0.0}{arrow}{nextStats.airborneDownforceAcceleration:0.0} M/S²";
                case VehicleUpgradeType.Turbo:
                    return atMax ? $"0-100  {currentStats.zeroToHundredTargetSeconds:0.0} SEC   MAX"
                        : $"0-100  {currentStats.zeroToHundredTargetSeconds:0.0}{arrow}{nextStats.zeroToHundredTargetSeconds:0.0} SEC";
                default:
                    return atMax ? $"TOTAL RUN  {currentStats.totalRunFuelDuration:0} SEC   MAX"
                        : $"TOTAL RUN  {currentStats.totalRunFuelDuration:0}{arrow}{nextStats.totalRunFuelDuration:0} SEC";
            }
        }

        private string ProgressLabel()
        {
            string[] labels = { "1 MODE", "2 VEHICLE" };
            for (int i = 0; i < labels.Length; i++)
                if (i == (int)step) labels[i] = $"<color=#64D8FF>[{labels[i]}]</color>";
            return string.Join("   >   ", labels);
        }

        private void FindSavedVehicle()
        {
            vehicleIndex = 0;
            if (catalog == null || catalog.vehicles == null) return;
            for (int i = 0; i < catalog.vehicles.Length; i++)
                if (catalog.vehicles[i] != null && catalog.vehicles[i].vehicleId == GameSession.SelectedVehicleId)
                    vehicleIndex = i;
        }

        private void FindSavedMap()
        {
            mapIndex = 0;
            int count = catalog != null ? catalog.CountMaps(mode) : 0;
            for (int i = 0; i < count; i++)
                if (catalog.GetCompatibleMap(mode, i)?.mapId == GameSession.SelectedMapId) mapIndex = i;
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static string FormatTime(float seconds) => seconds <= 0f ? "--:--.---" :
            $"{Mathf.FloorToInt(seconds / 60f):00}:{seconds % 60f:00.000}";

        public void Assign(GameCatalog gameCatalog, Text coins, Text progress, GameObject[] panels,
            Image[] cards, RectTransform[] cardRects, Text[] cardStatuses, Text modeLabel, Text mapLabel,
            Text vehicleLabel, Image vehiclePreview, GameObject previewFallback, Text stats, Text summary,
            Text transmission, GameObject tuningRoot,
            Text[] tuneNames, Text[] tuneLabels, Text[] tuneValues, Text[] tuneCosts, Button[] tuneButtons, Text tuningCoins,
            GaragePreviewRenderer composedPreview = null)
        {
            catalog = gameCatalog; coinsText = coins; progressText = progress; stepPanels = panels;
            modeCards = cards; modeCardRects = cardRects; modeCardStatusTexts = cardStatuses;
            modeText = modeLabel; mapText = mapLabel; vehicleText = vehicleLabel; vehiclePreviewImage = vehiclePreview;
            vehiclePreviewFallback = previewFallback;
            vehicleStatsText = stats;
            tuningSummaryText = summary; transmissionText = transmission; tuningPanel = tuningRoot;
            tuningNameTexts = tuneNames; tuningLevelTexts = tuneLabels; tuningValueTexts = tuneValues; tuningCostTexts = tuneCosts;
            tuningButtons = tuneButtons; tuningCoinsText = tuningCoins;
            vehiclePreviewRenderer = composedPreview;
        }
    }
}
