using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    [ExecuteAlways]
    public sealed class GarageUI : MonoBehaviour
    {
        private enum Category { Skin, Wheels, Driver, Tuning }

        [SerializeField] private GameCatalog catalog;
        [SerializeField] private GaragePreviewRenderer preview;
        [SerializeField] private Text vehicleName;
        [SerializeField] private Text categoryName;
        [SerializeField] private Text skinSummary;
        [SerializeField] private Text wheelSummary;
        [SerializeField] private Text driverSummary;
        [SerializeField] private Button[] categoryButtons;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private Image[] optionImages;
        [SerializeField] private Text[] optionLabels;
        [SerializeField] private GameObject tuningPanel;
        [SerializeField] private Text[] tuningNames;
        [SerializeField] private Text[] tuningLevels;
        [SerializeField] private Text[] tuningValues;
        [SerializeField] private Text[] tuningCosts;
        [SerializeField] private Button[] tuningButtons;
        [SerializeField] private Text tuningCoins;
        [SerializeField] private Text sectionTitle;
        [SerializeField] private Text upgradeSummary;
        [SerializeField] private GameObject vehiclePanel;
        [SerializeField] private GameObject customizationPanel;
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private GameObject storePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject confirmationModal;
        [SerializeField] private Text confirmationTitle;
        [SerializeField] private Text confirmationValues;
        [SerializeField] private Text confirmationCost;
        [SerializeField] private Text confirmationStatus;
        [SerializeField] private Button confirmationYes;

        private int vehicleIndex;
        private Category category;
        private GarageSection section;
        private VehicleUpgradeType pendingType;
        private bool purchaseInFlight;

        private VehicleDefinition CurrentVehicle => catalog != null && catalog.vehicles != null && catalog.vehicles.Length > 0
            ? catalog.vehicles[Mathf.Clamp(vehicleIndex, 0, catalog.vehicles.Length - 1)] : null;

        private void OnEnable()
        {
            if (catalog == null) return;
            FindSavedVehicle();
            section = GameSession.RequestedGarageSection;
            if (confirmationModal != null) confirmationModal.SetActive(false);
            Refresh();
        }

        public void Back() => SceneManager.LoadScene("MainMenu");
        public void PreviousVehicle() => CycleVehicle(-1);
        public void NextVehicle() => CycleVehicle(1);
        public void ShowWheels() { category = Category.Wheels; Refresh(); }
        public void ShowDrivers() { category = Category.Driver; Refresh(); }
        public void ShowTuning() { section = GarageSection.Tuning; Refresh(); }
        public void ShowVehicle() { section = GarageSection.Vehicle; Refresh(); }
        public void ShowUpgrade() { section = GarageSection.Upgrade; Refresh(); }
        public void ShowStore() { section = GarageSection.Store; Refresh(); }
        public void ShowSettings() { section = GarageSection.Settings; Refresh(); }
        public void ShowSkins() { section = GarageSection.Skins; category = Category.Skin; Refresh(); }
        public void SelectOption0() => SelectOption(0);
        public void SelectOption1() => SelectOption(1);
        public void SelectOption2() => SelectOption(2);
        public void SelectOption3() => SelectOption(3);
        public void SelectOption4() => SelectOption(4);
        public void SelectOption5() => SelectOption(5);
        public void UpgradeTune0() => PurchaseTune(0);
        public void UpgradeTune1() => PurchaseTune(1);
        public void UpgradeTune2() => PurchaseTune(2);
        public void UpgradeTune3() => PurchaseTune(3);
        public void UpgradeTune4() => PurchaseTune(4);

        private void CycleVehicle(int direction)
        {
            int count = catalog != null && catalog.vehicles != null ? catalog.vehicles.Length : 0;
            if (count == 0) return;
            vehicleIndex = (vehicleIndex + direction + count) % count;
            SaveManager.SelectedVehicleId = CurrentVehicle.vehicleId;
            Refresh();
        }

        private void PurchaseTune(int slot)
        {
            if (!Application.isPlaying) return;
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null || slot < 0 || slot >= vehicle.GetTuneSlotCount() || catalog?.upgradeEconomy == null) return;
            pendingType = vehicle.GetTuneTypeForSlot(slot);
            VehicleUpgradeState state = SaveManager.LoadUpgrades(vehicle.vehicleId, vehicle);
            int current = state.GetTune(pendingType);
            int maximum = vehicle.GetMaxTunes(pendingType);
            if (current >= maximum) return;
            int cost = catalog.upgradeEconomy.GetCostForNextTune(current);
            var next = new VehicleUpgradeState();
            next.CopyFrom(state);
            next.SetTune(pendingType, current + 1);
            next.ClampTo(vehicle);
            if (confirmationTitle != null)
                confirmationTitle.text = $"UPGRADE {vehicle.GetTuneDisplayName(pendingType)}?";
            if (confirmationValues != null)
                confirmationValues.text = $"CURRENT  {TuneValue(vehicle, state, pendingType)}\nNEXT  {TuneValue(vehicle, next, pendingType)}";
            if (confirmationCost != null) confirmationCost.text = $"COST  {cost:N0} COINS";
            if (confirmationStatus != null) confirmationStatus.text = string.Empty;
            if (confirmationYes != null) confirmationYes.interactable = true;
            if (confirmationModal != null) confirmationModal.SetActive(true);
        }

        public void ConfirmPurchase()
        {
            if (purchaseInFlight || CurrentVehicle == null || catalog?.upgradeEconomy == null) return;
            purchaseInFlight = true;
            if (confirmationYes != null) confirmationYes.interactable = false;
            bool purchased = SaveManager.TryPurchaseTune(CurrentVehicle, pendingType, catalog.upgradeEconomy,
                out _, out TunePurchaseResult result);
            purchaseInFlight = false;
            if (purchased)
            {
                if (confirmationModal != null) confirmationModal.SetActive(false);
                Refresh();
                return;
            }
            if (confirmationStatus != null)
                confirmationStatus.text = result == TunePurchaseResult.NotEnoughCoins
                    ? "NOT ENOUGH COINS" : result == TunePurchaseResult.MaximumLevel ? "MAX" : "UNAVAILABLE";
            if (confirmationYes != null)
                confirmationYes.interactable = result != TunePurchaseResult.MaximumLevel;
            RefreshTuning(CurrentVehicle);
        }

        public void CancelPurchase()
        {
            if (purchaseInFlight) return;
            if (confirmationModal != null) confirmationModal.SetActive(false);
        }

        private void SelectOption(int index)
        {
            VehicleDefinition vehicle = CurrentVehicle;
            if (category == Category.Tuning || vehicle == null || !TryGetOption(index, out string id, out _, out bool unlocked) || !unlocked) return;
            if (category == Category.Skin) SaveManager.SetSelectedSkinId(vehicle, id);
            else if (category == Category.Wheels) SaveManager.SetSelectedWheelId(vehicle, id);
            else SaveManager.SetSelectedDriverId(id);
            Refresh();
        }

        private void Refresh()
        {
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null) return;
            if (vehicleName != null) vehicleName.text = vehicle.displayName.ToUpperInvariant();
            if (sectionTitle != null) sectionTitle.text = section.ToString().ToUpperInvariant();
            if (categoryName != null) categoryName.text = category == Category.Skin ? "VEHICLE SKIN" :
                category == Category.Wheels ? "WHEELS" : category == Category.Driver ? "DRIVER / CHARACTER" : "TUNING";
            for (int i = 0; i < 4; i++)
                if (categoryButtons != null && i < categoryButtons.Length && categoryButtons[i] != null)
                    categoryButtons[i].image.color = i == (int)category
                        ? new Color(.18f, .58f, .82f, 1f) : new Color(.08f, .18f, .30f, .96f);

            bool showingTuning = section == GarageSection.Tuning;
            if (tuningPanel != null) tuningPanel.SetActive(showingTuning);
            if (vehiclePanel != null) vehiclePanel.SetActive(section == GarageSection.Vehicle);
            if (customizationPanel != null) customizationPanel.SetActive(section == GarageSection.Skins);
            if (upgradePanel != null) upgradePanel.SetActive(section == GarageSection.Upgrade);
            if (storePanel != null) storePanel.SetActive(section == GarageSection.Store);
            if (settingsPanel != null) settingsPanel.SetActive(section == GarageSection.Settings);
            int count = section == GarageSection.Skins ? OptionCount() : 0;
            string selected = section == GarageSection.Skins ? SelectedOptionId() : string.Empty;
            for (int i = 0; i < (optionButtons != null ? optionButtons.Length : 0); i++)
            {
                bool visible = i < count;
                optionButtons[i].gameObject.SetActive(visible);
                if (!visible) continue;
                TryGetOption(i, out string id, out Sprite icon, out bool unlocked);
                bool isSelected = id == selected;
                optionButtons[i].interactable = unlocked;
                optionButtons[i].image.color = isSelected ? new Color(.18f, .60f, .82f, 1f) :
                    unlocked ? new Color(.10f, .20f, .32f, 1f) : new Color(.12f, .12f, .14f, .78f);
                if (optionImages != null && i < optionImages.Length && optionImages[i] != null)
                {
                    optionImages[i].sprite = icon; optionImages[i].enabled = icon != null;
                    optionImages[i].preserveAspect = true;
                }
                if (optionLabels != null && i < optionLabels.Length && optionLabels[i] != null)
                    optionLabels[i].text = OptionName(i) + (isSelected ? "\nSELECTED" : unlocked ? "\nSELECT" : "\nLOCKED");
            }

            DriverDefinition driver = SelectedDriver();
            VehicleDefinition.CustomizationOption skin = vehicle.FindSkin(SaveManager.GetSelectedSkinId(vehicle));
            VehicleDefinition.CustomizationOption wheel = vehicle.FindWheel(SaveManager.GetSelectedWheelId(vehicle));
            preview?.Apply(vehicle, skin, wheel, driver);
            if (skinSummary != null) skinSummary.text = "SKIN\n" + CleanName(skin?.displayName, vehicle.displayName);
            if (wheelSummary != null) wheelSummary.text = "WHEELS\n" + CleanName(wheel?.displayName, vehicle.displayName);
            if (driverSummary != null) driverSummary.text = "DRIVER\n" + (driver?.displayName ?? "---");
            if (showingTuning) RefreshTuning(vehicle);
            if (upgradeSummary != null)
            {
                VehicleUpgradeState state = SaveManager.LoadUpgrades(vehicle.vehicleId, vehicle);
                ResolvedVehicleStats stats = vehicle.Resolve(state);
                upgradeSummary.text = $"CURRENT PERFORMANCE\n\nSPEED  {stats.topSpeedMps * 3.6f:0} KM/H\n" +
                    $"ACCELERATION  {stats.zeroToHundredTargetSeconds:0.0} SEC\n" +
                    $"SUSPENSION  {stats.suspensionTravel:0.00} M\n" +
                    $"GRIP  {stats.gripRating} / {VehicleDefinition.GlobalGripMaximum}\n" +
                    $"FUEL  {stats.totalRunFuelDuration:0} SEC";
            }
        }

        private void RefreshTuning(VehicleDefinition vehicle)
        {
            VehicleUpgradeState state = SaveManager.LoadUpgrades(vehicle.vehicleId, vehicle);
            if (tuningCoins != null) tuningCoins.text = $"COINS  {SaveManager.TotalCoins:N0}";
            int count = vehicle.GetTuneSlotCount();
            for (int i = 0; i < (tuningButtons != null ? tuningButtons.Length : 0); i++)
            {
                bool visible = i < count;
                if (tuningButtons[i] == null) continue;
                Transform row = tuningButtons[i].transform.parent;
                (row != null ? row.gameObject : tuningButtons[i].gameObject).SetActive(visible);
                if (!visible) continue;
                VehicleUpgradeType type = vehicle.GetTuneTypeForSlot(i);
                int current = state.GetTune(type);
                int maximum = vehicle.GetMaxTunes(type);
                bool atMax = current >= maximum;
                int cost = atMax || catalog?.upgradeEconomy == null ? -1 : catalog.upgradeEconomy.GetCostForNextTune(current);
                if (tuningNames != null && i < tuningNames.Length) tuningNames[i].text = vehicle.GetTuneDisplayName(type);
                if (tuningLevels != null && i < tuningLevels.Length) tuningLevels[i].text = $"LEVEL {current} / {maximum}";
                var next = new VehicleUpgradeState();
                next.CopyFrom(state);
                next.SetTune(type, current + (atMax ? 0 : 1));
                next.ClampTo(vehicle);
                if (tuningValues != null && i < tuningValues.Length)
                    tuningValues[i].text = atMax
                        ? $"CURRENT  {TuneValue(vehicle, state, type)}\nNEXT  MAX"
                        : $"CURRENT  {TuneValue(vehicle, state, type)}\nNEXT  {TuneValue(vehicle, next, type)}";
                if (tuningCosts != null && i < tuningCosts.Length) tuningCosts[i].text = atMax ? "MAX" : cost < 0 ? "UNAVAILABLE" : $"UPGRADE  {cost:N0}";
                tuningButtons[i].interactable = !atMax && cost >= 0;
            }
        }

        private static string TuneValue(VehicleDefinition vehicle, VehicleUpgradeState state, VehicleUpgradeType type)
        {
            ResolvedVehicleStats stats = vehicle.Resolve(state);
            switch (type)
            {
                case VehicleUpgradeType.Engine: return $"{stats.topSpeedMps * 3.6f:0} km/h";
                case VehicleUpgradeType.Acceleration: return $"0-100  {stats.zeroToHundredTargetSeconds:0.0}s";
                case VehicleUpgradeType.Suspension: return $"TRAVEL  {stats.suspensionTravel:0.00}m";
                case VehicleUpgradeType.Grip: return $"GRIP  {stats.gripRating} / {VehicleDefinition.GlobalGripMaximum}";
                case VehicleUpgradeType.Downforce: return $"{stats.airborneDownforceAcceleration:0.0} m/s²";
                case VehicleUpgradeType.Turbo: return $"0-100  {stats.zeroToHundredTargetSeconds:0.0}s";
                default: return $"RUN TIME  {stats.totalRunFuelDuration:0}s";
            }
        }

        private int OptionCount()
        {
            VehicleDefinition vehicle = CurrentVehicle;
            if (category == Category.Skin) return vehicle?.skins?.Length ?? 0;
            if (category == Category.Wheels) return vehicle?.wheels?.Length ?? 0;
            if (category == Category.Driver) return catalog?.drivers?.Length ?? 0;
            return 0;
        }

        private string SelectedOptionId()
        {
            if (category == Category.Skin) return SaveManager.GetSelectedSkinId(CurrentVehicle);
            if (category == Category.Wheels) return SaveManager.GetSelectedWheelId(CurrentVehicle);
            return SaveManager.GetSelectedDriverId(catalog?.drivers != null && catalog.drivers.Length > 0
                ? catalog.drivers[0].driverId : string.Empty);
        }

        private bool TryGetOption(int index, out string id, out Sprite icon, out bool unlocked)
        {
            id = string.Empty; icon = null; unlocked = false;
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null || index < 0 || index >= OptionCount()) return false;
            if (category == Category.Driver)
            {
                DriverDefinition option = catalog.drivers[index];
                if (option == null) return false;
                id = option.driverId; icon = option.previewSprite;
                unlocked = SaveManager.IsCustomizationUnlocked("global", "Driver", id, option.unlockedByDefault);
                return true;
            }
            VehicleDefinition.CustomizationOption item = category == Category.Skin ? vehicle.skins[index] : vehicle.wheels[index];
            if (item == null) return false;
            id = item.optionId; icon = item.sprite;
            unlocked = SaveManager.IsCustomizationUnlocked(vehicle.vehicleId, category.ToString(), id, item.unlockedByDefault);
            return true;
        }

        private string OptionName(int index)
        {
            if (category == Category.Driver) return catalog.drivers[index].displayName;
            VehicleDefinition.CustomizationOption item = category == Category.Skin
                ? CurrentVehicle.skins[index] : CurrentVehicle.wheels[index];
            return item.displayName;
        }

        private DriverDefinition SelectedDriver()
        {
            string fallback = catalog?.drivers != null && catalog.drivers.Length > 0 ? catalog.drivers[0].driverId : string.Empty;
            return catalog?.FindDriver(SaveManager.GetSelectedDriverId(fallback));
        }

        private static string CleanName(string value, string vehicleName)
        {
            if (string.IsNullOrWhiteSpace(value)) return "---";
            string cleaned = value.ToUpperInvariant().Replace(vehicleName.ToUpperInvariant(), string.Empty).Trim();
            return string.IsNullOrEmpty(cleaned) ? "DEFAULT" : cleaned;
        }

        private void FindSavedVehicle()
        {
            vehicleIndex = 0;
            if (catalog?.vehicles == null) return;
            for (int i = 0; i < catalog.vehicles.Length; i++)
                if (catalog.vehicles[i] != null && catalog.vehicles[i].vehicleId == SaveManager.SelectedVehicleId)
                    vehicleIndex = i;
        }

        public void Assign(GameCatalog gameCatalog, GaragePreviewRenderer previewRenderer, Text vehicleLabel,
            Text categoryLabel, Text skinLabel, Text wheelLabel, Text driverLabel, Button[] categories,
            Button[] options, Image[] icons, Text[] labels, GameObject tunePanel, Text[] tuneNames,
            Text[] tuneLevels, Text[] tuneValues, Text[] tuneCosts, Button[] tuneButtons, Text coinLabel,
            Text targetSectionTitle = null, Text targetUpgradeSummary = null, GameObject targetVehiclePanel = null,
            GameObject targetCustomizationPanel = null, GameObject targetUpgradePanel = null,
            GameObject targetStorePanel = null, GameObject targetSettingsPanel = null,
            GameObject targetConfirmationModal = null, Text targetConfirmationTitle = null,
            Text targetConfirmationValues = null, Text targetConfirmationCost = null,
            Text targetConfirmationStatus = null, Button targetConfirmationYes = null)
        {
            catalog = gameCatalog; preview = previewRenderer; vehicleName = vehicleLabel; categoryName = categoryLabel;
            skinSummary = skinLabel; wheelSummary = wheelLabel; driverSummary = driverLabel;
            categoryButtons = categories; optionButtons = options; optionImages = icons; optionLabels = labels;
            tuningPanel = tunePanel; tuningNames = tuneNames; tuningLevels = tuneLevels; tuningValues = tuneValues;
            tuningCosts = tuneCosts; tuningButtons = tuneButtons; tuningCoins = coinLabel;
            sectionTitle = targetSectionTitle; upgradeSummary = targetUpgradeSummary;
            vehiclePanel = targetVehiclePanel; customizationPanel = targetCustomizationPanel;
            upgradePanel = targetUpgradePanel; storePanel = targetStorePanel; settingsPanel = targetSettingsPanel;
            confirmationModal = targetConfirmationModal; confirmationTitle = targetConfirmationTitle;
            confirmationValues = targetConfirmationValues; confirmationCost = targetConfirmationCost;
            confirmationStatus = targetConfirmationStatus; confirmationYes = targetConfirmationYes;
            FindSavedVehicle();
            Refresh();
        }
    }
}
