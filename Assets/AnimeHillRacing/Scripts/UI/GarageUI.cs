using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    [ExecuteAlways]
    public sealed class GarageUI : MonoBehaviour
    {
        private enum Category { Skin, Wheels, Driver, Characters, Tuning }
        private enum PurchaseKind { None, Tune, Customization, Blueprints }

        [SerializeField] private GameCatalog catalog;
        [SerializeField] private GaragePreviewRenderer preview;
        [SerializeField] private Text vehicleName;
        [SerializeField] private Text categoryName;
        [SerializeField] private Text skinSummary;
        [SerializeField] private Text wheelSummary;
        [SerializeField] private Text driverSummary;
        [SerializeField] private Text characterSummary;
        [SerializeField] private Button[] categoryButtons;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private Image[] optionImages;
        [SerializeField] private Text[] optionLabels;
        [SerializeField] private Image[] optionBlueprintFills;
        [SerializeField] private Text[] optionBlueprintTexts;
        [SerializeField] private GameObject tuningPanel;
        [SerializeField] private Text[] tuningNames;
        [SerializeField] private Text[] tuningLevels;
        [SerializeField] private Text[] tuningValues;
        [SerializeField] private Text[] tuningCosts;
        [SerializeField] private Button[] tuningButtons;
        [SerializeField] private Button[] tuningDownButtons;
        [SerializeField] private Text tuningCoins;
        [SerializeField] private Text gemsBalance;
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
        [SerializeField] private Button[] blueprintButtons;
        [SerializeField] private Image[] blueprintImages;
        [SerializeField] private Text[] blueprintLabels;
        [SerializeField] private Text blueprintPageText;
        [SerializeField] private Text blueprintCoinsText;
        [SerializeField] private Button[] blueprintPackButtons;
        [SerializeField] private Button[] shopAccessoryButtons;
        [SerializeField] private Image[] shopAccessoryImages;
        [SerializeField] private Text[] shopAccessoryLabels;

        private int vehicleIndex;
        private Category category;
        private GarageSection section;
        private VehicleUpgradeType pendingType;
        private PurchaseKind pendingPurchaseKind;
        private Category pendingCategory;
        private string pendingOptionId;
        private int pendingGemPrice;
        private string pendingBundleId;
        private int blueprintPage;
        private BlueprintPackTier selectedBlueprintTier = BlueprintPackTier.Hundred;
        private BlueprintPackTier pendingBlueprintTier = BlueprintPackTier.Hundred;
        private bool purchaseInFlight;

        private VehicleDefinition CurrentVehicle => catalog != null && catalog.vehicles != null && catalog.vehicles.Length > 0
            ? catalog.vehicles[Mathf.Clamp(vehicleIndex, 0, catalog.vehicles.Length - 1)] : null;

        private void OnEnable()
        {
            if (catalog == null) return;
            EnsureRuntimeTuningRows();
            EnsureTuneAdjustmentButtons();
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
        public void ShowCharacters() { category = Category.Characters; Refresh(); }
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
        public void UpgradeTune5() => PurchaseTune(5);
        public void DecreaseTune0() => DecreaseTune(0);
        public void DecreaseTune1() => DecreaseTune(1);
        public void DecreaseTune2() => DecreaseTune(2);
        public void DecreaseTune3() => DecreaseTune(3);
        public void DecreaseTune4() => DecreaseTune(4);
        public void DecreaseTune5() => DecreaseTune(5);
        public void SelectBlueprint0() => SelectBlueprint(0);
        public void SelectBlueprint1() => SelectBlueprint(1);
        public void SelectBlueprint2() => SelectBlueprint(2);
        public void SelectBlueprint3() => SelectBlueprint(3);
        public void SelectBlueprint4() => SelectBlueprint(4);
        public void SelectBlueprint5() => SelectBlueprint(5);
        public void SelectBlueprintPack1() => SelectBlueprintPack(BlueprintPackTier.One);
        public void SelectBlueprintPack5() => SelectBlueprintPack(BlueprintPackTier.Five);
        public void SelectBlueprintPack10() => SelectBlueprintPack(BlueprintPackTier.Ten);
        public void SelectBlueprintPack100() => SelectBlueprintPack(BlueprintPackTier.Hundred);
        public void SelectShopAccessory0() => SelectShopAccessory(0);
        public void SelectShopAccessory1() => SelectShopAccessory(1);
        public void SelectShopAccessory2() => SelectShopAccessory(2);
        public void PreviousBlueprintPage() { blueprintPage = Mathf.Max(0, blueprintPage - 1); RefreshBlueprintShop(); }
        public void NextBlueprintPage()
        {
            int pages = Mathf.Max(1, Mathf.CeilToInt(PremiumBundleCatalog.GetAll(catalog).Count / 6f));
            blueprintPage = Mathf.Min(pages - 1, blueprintPage + 1);
            RefreshBlueprintShop();
        }

        private void CycleVehicle(int direction)
        {
            int count = catalog != null && catalog.vehicles != null ? catalog.vehicles.Length : 0;
            if (count == 0) return;
            vehicleIndex = (vehicleIndex + direction + count) % count;
            SaveManager.SelectedVehicleId = CurrentVehicle.vehicleId;
            Refresh();
        }

        private void SelectBlueprint(int visibleIndex)
        {
            if (!Application.isPlaying || catalog == null) return;
            List<BundleDefinition> bundles = PremiumBundleCatalog.GetAll(catalog);
            int index = blueprintPage * 6 + visibleIndex;
            if (index < 0 || index >= bundles.Count) return;
            BundleDefinition bundle = bundles[index];
            if (SaveManager.IsBundleUnlocked(bundle)) return;
            pendingPurchaseKind = PurchaseKind.Blueprints;
            pendingBundleId = bundle.BundleId;
            pendingBlueprintTier = selectedBlueprintTier;
            int amount = BlueprintPackCatalog.GetBlueprintAmount(pendingBlueprintTier);
            int price = BlueprintPackCatalog.GetCoinPrice(pendingBlueprintTier);
            if (confirmationTitle != null)
                confirmationTitle.text = $"BUY {amount} BLUEPRINT{(amount == 1 ? string.Empty : "S")}?";
            if (confirmationValues != null)
                confirmationValues.text = $"{bundle.DisplayName.ToUpperInvariant()}\n" +
                    $"{SaveManager.GetBlueprintCount(bundle.BundleId)} / {bundle.BlueprintRequirement} BLUEPRINTS";
            if (confirmationCost != null) confirmationCost.text = $"COST  {price:N0} COINS";
            if (confirmationStatus != null) confirmationStatus.text = string.Empty;
            if (confirmationYes != null) confirmationYes.interactable = true;
            if (confirmationModal != null) confirmationModal.SetActive(true);
        }

        private void PurchaseTune(int slot)
        {
            if (!Application.isPlaying) return;
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null || slot < 0 || slot >= vehicle.GetTuneSlotCount() || catalog?.upgradeEconomy == null) return;
            pendingType = vehicle.GetTuneTypeForSlot(slot);
            VehicleUpgradeState equippedState = SaveManager.LoadUpgrades(vehicle.vehicleId, vehicle);
            VehicleUpgradeState purchasedState = SaveManager.LoadPurchasedUpgrades(vehicle.vehicleId, vehicle);
            int equipped = equippedState.GetTune(pendingType);
            int purchased = purchasedState.GetTune(pendingType);
            if (equipped < purchased)
            {
                SaveManager.TryIncreaseEquippedTune(vehicle, pendingType, catalog.upgradeEconomy,
                    out _, out _);
                Refresh();
                return;
            }
            pendingPurchaseKind = PurchaseKind.Tune;
            VehicleUpgradeState state = equippedState;
            int current = purchased;
            int maximum = vehicle.GetMaxTunes(pendingType);
            if (current >= maximum) return;
            int cost = catalog.upgradeEconomy.GetCostForNextTune(vehicle, pendingType, current);
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

        private void DecreaseTune(int slot)
        {
            if (!Application.isPlaying) return;
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null || slot < 0 || slot >= vehicle.GetTuneSlotCount()) return;
            SaveManager.TryDecreaseEquippedTune(vehicle, vehicle.GetTuneTypeForSlot(slot), out _);
            Refresh();
        }

        public void ConfirmPurchase()
        {
            if (purchaseInFlight || CurrentVehicle == null || pendingPurchaseKind == PurchaseKind.None) return;
            purchaseInFlight = true;
            if (confirmationYes != null) confirmationYes.interactable = false;
            if (pendingPurchaseKind == PurchaseKind.Blueprints)
            {
                bool blueprintPurchased = SaveManager.TryPurchaseBlueprints(catalog, pendingBundleId,
                    pendingBlueprintTier, out BlueprintPurchaseResult blueprintResult);
                purchaseInFlight = false;
                if (blueprintPurchased || blueprintResult == BlueprintPurchaseResult.AlreadyUnlocked)
                {
                    pendingPurchaseKind = PurchaseKind.None;
                    pendingBundleId = string.Empty;
                    if (confirmationModal != null) confirmationModal.SetActive(false);
                    Refresh();
                    return;
                }
                if (confirmationStatus != null)
                    confirmationStatus.text = blueprintResult == BlueprintPurchaseResult.NotEnoughCoins
                        ? "NOT ENOUGH COINS" : "UNAVAILABLE";
                if (confirmationYes != null) confirmationYes.interactable = true;
                RefreshBlueprintShop();
                return;
            }
            if (pendingPurchaseKind == PurchaseKind.Customization)
            {
                string purchaseScope = pendingCategory == Category.Characters ? "global" : CurrentVehicle.vehicleId;
                string purchaseCategory = pendingCategory == Category.Characters
                    ? "CharacterAccessory" : pendingCategory.ToString();
                bool cosmeticPurchased = SaveManager.TryPurchaseCustomization(purchaseScope,
                    purchaseCategory, pendingOptionId, pendingGemPrice,
                    out CustomizationPurchaseResult cosmeticResult);
                purchaseInFlight = false;
                if (cosmeticPurchased || cosmeticResult == CustomizationPurchaseResult.AlreadyUnlocked)
                {
                    SetSelectedOption(CurrentVehicle, pendingCategory, pendingOptionId);
                    pendingPurchaseKind = PurchaseKind.None;
                    if (confirmationModal != null) confirmationModal.SetActive(false);
                    Refresh();
                    return;
                }
                if (confirmationStatus != null)
                    confirmationStatus.text = cosmeticResult == CustomizationPurchaseResult.NotEnoughGems
                        ? "NOT ENOUGH GEMS" : "UNAVAILABLE";
                if (confirmationYes != null) confirmationYes.interactable = true;
                Refresh();
                return;
            }
            if (catalog?.upgradeEconomy == null)
            {
                purchaseInFlight = false;
                return;
            }
            bool purchased = SaveManager.TryPurchaseTune(CurrentVehicle, pendingType, catalog.upgradeEconomy,
                out _, out TunePurchaseResult result);
            purchaseInFlight = false;
            if (purchased)
            {
                pendingPurchaseKind = PurchaseKind.None;
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
            if (category == Category.Tuning || vehicle == null ||
                !TryGetOption(index, out string id, out _, out bool unlocked, out int gemPrice)) return;
            if (!unlocked)
            {
                if (gemPrice <= 0 || category == Category.Driver) return;
                pendingPurchaseKind = PurchaseKind.Customization;
                pendingCategory = category;
                pendingOptionId = id;
                pendingGemPrice = gemPrice;
                if (confirmationTitle != null) confirmationTitle.text = $"BUY {OptionName(index).ToUpperInvariant()}?";
                if (confirmationValues != null) confirmationValues.text = "PERMANENT COSMETIC UNLOCK";
                if (confirmationCost != null) confirmationCost.text = $"COST  {gemPrice:N0} GEMS";
                if (confirmationStatus != null) confirmationStatus.text = string.Empty;
                if (confirmationYes != null) confirmationYes.interactable = true;
                if (confirmationModal != null) confirmationModal.SetActive(true);
                return;
            }
            SetSelectedOption(vehicle, category, id);
            Refresh();
        }

        // Keeps previously generated Garage scenes forward-compatible. Newly rebuilt scenes already
        // serialize six rows; an older five-row scene receives the Formula-only Downforce row at runtime.
        private void EnsureRuntimeTuningRows()
        {
            const int requiredRows = 6;
            if (!Application.isPlaying || tuningPanel == null || tuningButtons == null ||
                tuningButtons.Length >= requiredRows || tuningButtons.Length == 0) return;
            Button templateButton = tuningButtons[tuningButtons.Length - 1];
            if (templateButton == null || templateButton.transform.parent == null) return;
            GameObject templateRow = templateButton.transform.parent.gameObject;
            GameObject row = Instantiate(templateRow, templateRow.transform.parent);
            row.name = "Tune Row 6";
            Button button = row.transform.Find("Upgrade")?.GetComponent<Button>();
            Text name = row.transform.Find("Name")?.GetComponent<Text>();
            Text level = row.transform.Find("Level")?.GetComponent<Text>();
            Text value = row.transform.Find("Value")?.GetComponent<Text>();
            Text cost = button != null ? button.GetComponentInChildren<Text>(true) : null;
            if (button == null || name == null || level == null || value == null || cost == null)
            {
                Destroy(row);
                return;
            }
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(UpgradeTune5);
            System.Array.Resize(ref tuningNames, requiredRows);
            System.Array.Resize(ref tuningLevels, requiredRows);
            System.Array.Resize(ref tuningValues, requiredRows);
            System.Array.Resize(ref tuningCosts, requiredRows);
            System.Array.Resize(ref tuningButtons, requiredRows);
            tuningNames[5] = name;
            tuningLevels[5] = level;
            tuningValues[5] = value;
            tuningCosts[5] = cost;
            tuningButtons[5] = button;
            for (int i = 0; i < requiredRows; i++)
            {
                RectTransform rect = tuningButtons[i] != null
                    ? tuningButtons[i].transform.parent as RectTransform : null;
                if (rect == null) continue;
                rect.anchoredPosition = new Vector2(0f, 260f - i * 104f);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 92f);
            }
        }

        private void EnsureTuneAdjustmentButtons()
        {
            if (!Application.isPlaying || tuningButtons == null) return;
            if (tuningDownButtons == null || tuningDownButtons.Length != tuningButtons.Length)
                System.Array.Resize(ref tuningDownButtons, tuningButtons.Length);
            for (int i = 0; i < tuningButtons.Length; i++)
            {
                Button up = tuningButtons[i];
                if (up == null || up.transform.parent == null) continue;
                RectTransform upRect = up.transform as RectTransform;
                if (upRect != null)
                {
                    upRect.anchoredPosition = new Vector2(350f, 0f);
                    upRect.sizeDelta = new Vector2(150f, 60f);
                }
                Transform existing = up.transform.parent.Find("Decrease");
                Button down = existing != null ? existing.GetComponent<Button>() : null;
                if (down == null) down = CreateTuneDownButton(up, i);
                tuningDownButtons[i] = down;
            }
        }

        private Button CreateTuneDownButton(Button template, int slot)
        {
            GameObject gameObject = new GameObject("Decrease", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(template.transform.parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(235f, 0f);
            rect.sizeDelta = new Vector2(70f, 60f);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = template.image != null ? template.image.sprite : null;
            image.type = template.image != null ? template.image.type : Image.Type.Simple;
            image.color = template.image != null ? template.image.color : new Color(.13f, .30f, .53f, .95f);
            Button button = gameObject.GetComponent<Button>();
            button.colors = template.colors;
            Text source = template.GetComponentInChildren<Text>(true);
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;
            Text label = labelObject.GetComponent<Text>();
            label.font = source != null ? source.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "↓";
            label.fontSize = 34;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            int capturedSlot = slot;
            button.onClick.AddListener(() => DecreaseTune(capturedSlot));
            gameObject.AddComponent<ButtonPressFeedback>();
            return button;
        }

        private void Refresh()
        {
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null) return;
            if (tuningCoins != null) tuningCoins.text = $"COINS  {SaveManager.TotalCoins:N0}";
            if (gemsBalance != null) gemsBalance.text = $"GEMS  {SaveManager.TotalGems:N0}";
            if (vehicleName != null) vehicleName.text = vehicle.displayName.ToUpperInvariant();
            if (sectionTitle != null) sectionTitle.text = section.ToString().ToUpperInvariant();
            if (categoryName != null) categoryName.text = category == Category.Skin ? "VEHICLE SKIN" :
                category == Category.Wheels ? "WHEELS" : category == Category.Driver ? "DRIVER" :
                category == Category.Characters ? "CHARACTERS / ACCESSORIES" : "TUNING";
            for (int i = 0; i < (categoryButtons?.Length ?? 0); i++)
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
            if (section == GarageSection.Store) RefreshBlueprintShop();
            int count = section == GarageSection.Skins ? OptionCount() : 0;
            string selected = section == GarageSection.Skins ? SelectedOptionId() : string.Empty;
            for (int i = 0; i < (optionButtons != null ? optionButtons.Length : 0); i++)
            {
                bool visible = i < count;
                optionButtons[i].gameObject.SetActive(visible);
                if (!visible) continue;
                TryGetOption(i, out string id, out Sprite icon, out bool unlocked, out int gemPrice);
                bool isSelected = id == selected;
                optionButtons[i].interactable = unlocked || gemPrice > 0;
                optionButtons[i].image.color = isSelected ? new Color(.18f, .60f, .82f, 1f) :
                    unlocked ? new Color(.10f, .20f, .32f, 1f) : new Color(.12f, .12f, .14f, .78f);
                if (optionImages != null && i < optionImages.Length && optionImages[i] != null)
                {
                    optionImages[i].sprite = icon; optionImages[i].enabled = icon != null;
                    optionImages[i].preserveAspect = true;
                }
                if (optionLabels != null && i < optionLabels.Length && optionLabels[i] != null)
                    optionLabels[i].text = OptionName(i) + (isSelected ? "\nSELECTED" : unlocked
                        ? "\nSELECT" : gemPrice > 0 ? $"\n{gemPrice:N0} GEMS" : "\nLOCKED");
                RefreshOptionBlueprintProgress(i, unlocked);
            }

            DriverDefinition driver = SelectedDriver();
            VehicleDefinition.CustomizationOption skin = vehicle.FindSkin(SaveManager.GetSelectedSkinId(vehicle));
            VehicleDefinition.CustomizationOption wheel = vehicle.FindWheel(SaveManager.GetSelectedWheelId(vehicle));
            preview?.Apply(vehicle, skin, wheel, driver);
            if (skinSummary != null) skinSummary.text = "SKIN\n" + CleanName(skin?.displayName, vehicle.displayName);
            if (wheelSummary != null) wheelSummary.text = "WHEELS\n" + CleanName(wheel?.displayName, vehicle.displayName);
            if (driverSummary != null) driverSummary.text = "DRIVER\n" + (driver?.displayName ?? "---");
            if (characterSummary != null)
            {
                DriverDefinition.CharacterAccessoryOption selectedAccessory = SelectedAccessory();
                characterSummary.text = "CHARACTERS\n" + (selectedAccessory?.displayName ?? "DEFAULT");
            }
            if (showingTuning) RefreshTuning(vehicle);
            if (upgradeSummary != null)
            {
                VehicleUpgradeState state = SaveManager.LoadUpgrades(vehicle.vehicleId, vehicle);
                ResolvedVehicleStats stats = vehicle.Resolve(state);
                upgradeSummary.text = $"CURRENT PERFORMANCE\n\nSPEED  {stats.topSpeedMps * 3.6f:0} KM/H\n" +
                    $"0-100 KM/H  {stats.zeroToHundredTargetSeconds:0.0} SEC\n" +
                    $"SUSPENSION  {stats.suspensionTravel:0.00} M\n" +
                    $"GRIP  {stats.gripRating} / {VehicleDefinition.GlobalGripMaximum}\n" +
                    $"FUEL  {stats.totalRunFuelDuration:0} SEC";
            }
        }

        private void RefreshBlueprintShop()
        {
            if (blueprintCoinsText != null) blueprintCoinsText.text = $"COINS  {SaveManager.TotalCoins:N0}";
            for (int i = 0; i < (blueprintPackButtons?.Length ?? 0); i++)
            {
                Button packButton = blueprintPackButtons[i];
                if (packButton == null) continue;
                BlueprintPackTier tier = (BlueprintPackTier)i;
                int amount = BlueprintPackCatalog.GetBlueprintAmount(tier);
                int price = BlueprintPackCatalog.GetCoinPrice(tier);
                packButton.image.color = tier == selectedBlueprintTier
                    ? new Color(.18f, .62f, .88f, 1f) : new Color(.07f, .20f, .34f, .96f);
                Text label = packButton.GetComponentInChildren<Text>(true);
                if (label != null) label.text = $"+{amount} BP\n{CompactCoins(price)}";
            }
            List<BundleDefinition> bundles = PremiumBundleCatalog.GetAll(catalog);
            int pages = Mathf.Max(1, Mathf.CeilToInt(bundles.Count / 6f));
            blueprintPage = Mathf.Clamp(blueprintPage, 0, pages - 1);
            if (blueprintPageText != null)
                blueprintPageText.text = bundles.Count == 0 ? "NO PREMIUM BUNDLES CONFIGURED" :
                    $"PAGE {blueprintPage + 1} / {pages}";
            for (int i = 0; i < (blueprintButtons?.Length ?? 0); i++)
            {
                int index = blueprintPage * 6 + i;
                bool visible = index < bundles.Count;
                blueprintButtons[i].gameObject.SetActive(visible);
                if (!visible) continue;
                BundleDefinition bundle = bundles[index];
                bool unlocked = SaveManager.IsBundleUnlocked(bundle);
                int progress = Mathf.Min(SaveManager.GetBlueprintCount(bundle.BundleId), bundle.BlueprintRequirement);
                blueprintButtons[i].interactable = !unlocked;
                blueprintButtons[i].image.color = unlocked ? new Color(.12f, .42f, .22f, .92f) :
                    new Color(.055f, .14f, .25f, .96f);
                if (blueprintImages != null && i < blueprintImages.Length && blueprintImages[i] != null)
                {
                    blueprintImages[i].sprite = bundle.Thumbnail;
                    blueprintImages[i].enabled = bundle.Thumbnail != null;
                    blueprintImages[i].preserveAspect = true;
                }
                if (blueprintLabels != null && i < blueprintLabels.Length && blueprintLabels[i] != null)
                {
                    int amount = BlueprintPackCatalog.GetBlueprintAmount(selectedBlueprintTier);
                    int price = BlueprintPackCatalog.GetCoinPrice(selectedBlueprintTier);
                    blueprintLabels[i].text = $"{bundle.DisplayName.ToUpperInvariant()}\n" +
                        $"{bundle.Type.ToString().ToUpperInvariant()}  {progress} / {bundle.BlueprintRequirement}\n" +
                        (unlocked ? "UNLOCKED" : $"BUY +{amount}  •  {price:N0} COINS");
                }
            }
            RefreshShopAccessories();
        }

        private void RefreshShopAccessories()
        {
            DriverDefinition driver = SelectedDriver();
            int count = driver?.accessories?.Length ?? 0;
            for (int i = 0; i < (shopAccessoryButtons?.Length ?? 0); i++)
            {
                bool visible = i < count && driver.accessories[i] != null;
                Button button = shopAccessoryButtons[i];
                if (button == null) continue;
                button.gameObject.SetActive(visible);
                if (!visible) continue;
                DriverDefinition.CharacterAccessoryOption option = driver.accessories[i];
                bool unlocked = IsAccessoryUnlocked(option);
                bool selected = option.accessoryId == SaveManager.GetSelectedCharacterAccessoryId(driver.DefaultAccessoryId);
                button.interactable = unlocked || option.gemPrice > 0;
                button.image.color = selected ? new Color(.18f, .60f, .82f, 1f) :
                    unlocked ? new Color(.10f, .30f, .24f, .96f) : new Color(.12f, .12f, .14f, .84f);
                if (shopAccessoryImages != null && i < shopAccessoryImages.Length && shopAccessoryImages[i] != null)
                {
                    shopAccessoryImages[i].sprite = option.sprite;
                    shopAccessoryImages[i].enabled = option.sprite != null;
                    shopAccessoryImages[i].preserveAspect = true;
                }
                if (shopAccessoryLabels != null && i < shopAccessoryLabels.Length && shopAccessoryLabels[i] != null)
                    shopAccessoryLabels[i].text = option.displayName.ToUpperInvariant() + "\n" +
                        (selected ? "EQUIPPED" : unlocked ? "EQUIP" : option.gemPrice > 0
                            ? $"{option.gemPrice:N0} GEMS" : "LOCKED");
            }
        }

        private void SelectShopAccessory(int index)
        {
            DriverDefinition driver = SelectedDriver();
            if (driver?.accessories == null || index < 0 || index >= driver.accessories.Length) return;
            DriverDefinition.CharacterAccessoryOption option = driver.accessories[index];
            if (option == null || !IsAccessoryUnlocked(option)) return;
            SaveManager.SetSelectedCharacterAccessoryId(option.accessoryId);
            Refresh();
        }

        private void SelectBlueprintPack(BlueprintPackTier tier)
        {
            if (!BlueprintPackCatalog.IsValid(tier)) return;
            selectedBlueprintTier = tier;
            RefreshBlueprintShop();
        }

        private void RefreshOptionBlueprintProgress(int index, bool unlocked)
        {
            Image fill = optionBlueprintFills != null && index < optionBlueprintFills.Length
                ? optionBlueprintFills[index] : null;
            Text progressText = optionBlueprintTexts != null && index < optionBlueprintTexts.Length
                ? optionBlueprintTexts[index] : null;
            GameObject bar = fill != null && fill.transform.parent != null ? fill.transform.parent.gameObject : null;
            BundleDefinition bundle = GetOptionBundle(index);
            bool visible = bundle != null && !unlocked;
            if (bar != null) bar.SetActive(visible);
            if (!visible) return;
            int progress = Mathf.Min(SaveManager.GetBlueprintCount(bundle.BundleId), bundle.BlueprintRequirement);
            if (fill != null) fill.fillAmount = bundle.BlueprintRequirement <= 0
                ? 0f : progress / (float)bundle.BlueprintRequirement;
            if (progressText != null)
                progressText.text = $"BLUEPRINTS  {progress} / {bundle.BlueprintRequirement}";
        }

        private BundleDefinition GetOptionBundle(int index)
        {
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null || category == Category.Driver || index < 0 || index >= OptionCount()) return null;
            if (category == Category.Characters)
            {
                DriverDefinition.CharacterAccessoryOption accessory = SelectedDriver()?.accessories[index];
                if (accessory == null || !accessory.allowsBlueprintProgression || accessory.gemPrice <= 0) return null;
                return PremiumBundleCatalog.Find(catalog, PremiumBundleCatalog.CosmeticBundleId(
                    "global", "CharacterAccessory", accessory.accessoryId));
            }
            VehicleDefinition.CustomizationOption item = category == Category.Skin
                ? vehicle.skins[index] : vehicle.wheels[index];
            if (item == null || !item.allowsBlueprintProgression || item.gemPrice <= 0) return null;
            return PremiumBundleCatalog.Find(catalog, PremiumBundleCatalog.CosmeticBundleId(
                vehicle.vehicleId, category.ToString(), item.optionId));
        }

        private static string CompactCoins(int value) => value >= 1000000
            ? $"{value / 1000000f:0.#}M" : value >= 1000 ? $"{value / 1000f:0.#}K" : value.ToString("N0");

        private void RefreshTuning(VehicleDefinition vehicle)
        {
            VehicleUpgradeState state = SaveManager.LoadUpgrades(vehicle.vehicleId, vehicle);
            VehicleUpgradeState purchasedState = SaveManager.LoadPurchasedUpgrades(vehicle.vehicleId, vehicle);
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
                int purchased = purchasedState.GetTune(type);
                int maximum = vehicle.GetMaxTunes(type);
                bool purchasedMax = purchased >= maximum;
                bool canEquipUp = current < purchased;
                int cost = purchasedMax || catalog?.upgradeEconomy == null ? -1 :
                    catalog.upgradeEconomy.GetCostForNextTune(vehicle, type, purchased);
                if (tuningNames != null && i < tuningNames.Length) tuningNames[i].text = vehicle.GetTuneDisplayName(type);
                if (tuningLevels != null && i < tuningLevels.Length)
                    tuningLevels[i].text = $"EQUIPPED {current} / {maximum}   BOUGHT {purchased}";
                var next = new VehicleUpgradeState();
                next.CopyFrom(state);
                next.SetTune(type, current + (current < maximum ? 1 : 0));
                next.ClampTo(vehicle);
                if (tuningValues != null && i < tuningValues.Length)
                    tuningValues[i].text = current >= maximum
                        ? $"CURRENT  {TuneValue(vehicle, state, type)}\nNEXT  MAX"
                        : $"CURRENT  {TuneValue(vehicle, state, type)}\nNEXT  {TuneValue(vehicle, next, type)}";
                if (tuningCosts != null && i < tuningCosts.Length)
                    tuningCosts[i].text = canEquipUp ? "↑  EQUIP" : purchasedMax ? "↑  MAX" :
                        cost < 0 ? "UNAVAILABLE" : $"↑  {cost:N0}";
                tuningButtons[i].interactable = current < maximum && (canEquipUp || cost >= 0);
                if (tuningDownButtons != null && i < tuningDownButtons.Length && tuningDownButtons[i] != null)
                    tuningDownButtons[i].interactable = current > 1;
            }
        }

        private static string TuneValue(VehicleDefinition vehicle, VehicleUpgradeState state, VehicleUpgradeType type)
        {
            ResolvedVehicleStats stats = vehicle.Resolve(state);
            switch (type)
            {
                case VehicleUpgradeType.Engine: return $"{stats.topSpeedMps * 3.6f:0} km/h";
                case VehicleUpgradeType.Acceleration: return $"0-100  {stats.zeroToHundredTargetSeconds:0.00}s";
                case VehicleUpgradeType.Suspension: return $"TRAVEL  {stats.suspensionTravel:0.000}m";
                case VehicleUpgradeType.Grip: return $"GRIP  {stats.gripRating} / {VehicleDefinition.GlobalGripMaximum}";
                case VehicleUpgradeType.Downforce: return $"{stats.airborneDownforceAcceleration:0.0} m/s²";
                case VehicleUpgradeType.Turbo: return vehicle.archetype == VehicleArchetype.AerialSatelite
                    ? $"THRUST  {Mathf.Lerp(50f, 100f, VehicleDefinition.UpgradeProgress(state.turboTune, vehicle.turboMaxTunes)):0}%"
                    : $"0-100  {stats.zeroToHundredTargetSeconds:0.00}s";
                default: return $"RUN TIME  {stats.totalRunFuelDuration:0.0}s";
            }
        }

        private int OptionCount()
        {
            VehicleDefinition vehicle = CurrentVehicle;
            if (category == Category.Skin) return vehicle?.skins?.Length ?? 0;
            if (category == Category.Wheels) return vehicle?.wheels?.Length ?? 0;
            if (category == Category.Driver) return catalog?.drivers?.Length ?? 0;
            if (category == Category.Characters) return SelectedDriver()?.accessories?.Length ?? 0;
            return 0;
        }

        private string SelectedOptionId()
        {
            if (category == Category.Skin) return SaveManager.GetSelectedSkinId(CurrentVehicle);
            if (category == Category.Wheels) return SaveManager.GetSelectedWheelId(CurrentVehicle);
            if (category == Category.Characters)
            {
                DriverDefinition driver = SelectedDriver();
                return SaveManager.GetSelectedCharacterAccessoryId(driver?.DefaultAccessoryId ?? "default");
            }
            return SaveManager.GetSelectedDriverId(catalog?.drivers != null && catalog.drivers.Length > 0
                ? catalog.drivers[0].driverId : string.Empty);
        }

        private bool TryGetOption(int index, out string id, out Sprite icon, out bool unlocked, out int gemPrice)
        {
            id = string.Empty; icon = null; unlocked = false; gemPrice = 0;
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
            if (category == Category.Characters)
            {
                DriverDefinition.CharacterAccessoryOption option = SelectedDriver()?.accessories[index];
                if (option == null) return false;
                id = option.accessoryId; icon = option.sprite; gemPrice = option.gemPrice;
                unlocked = IsAccessoryUnlocked(option);
                return true;
            }
            VehicleDefinition.CustomizationOption item = category == Category.Skin ? vehicle.skins[index] : vehicle.wheels[index];
            if (item == null) return false;
            id = item.optionId; icon = item.sprite;
            gemPrice = item.gemPrice;
            unlocked = SaveManager.IsCustomizationUnlocked(vehicle.vehicleId, category.ToString(), id, item.unlockedByDefault);
            return true;
        }

        private static void SetSelectedOption(VehicleDefinition vehicle, Category selectedCategory, string id)
        {
            if (selectedCategory == Category.Skin) SaveManager.SetSelectedSkinId(vehicle, id);
            else if (selectedCategory == Category.Wheels) SaveManager.SetSelectedWheelId(vehicle, id);
            else if (selectedCategory == Category.Driver) SaveManager.SetSelectedDriverId(id);
            else if (selectedCategory == Category.Characters) SaveManager.SetSelectedCharacterAccessoryId(id);
        }

        private string OptionName(int index)
        {
            if (category == Category.Driver) return catalog.drivers[index].displayName;
            if (category == Category.Characters) return SelectedDriver().accessories[index].displayName;
            VehicleDefinition.CustomizationOption item = category == Category.Skin
                ? CurrentVehicle.skins[index] : CurrentVehicle.wheels[index];
            return item.displayName;
        }

        private DriverDefinition SelectedDriver()
        {
            string fallback = catalog?.drivers != null && catalog.drivers.Length > 0 ? catalog.drivers[0].driverId : string.Empty;
            return catalog?.FindDriver(SaveManager.GetSelectedDriverId(fallback));
        }

        private DriverDefinition.CharacterAccessoryOption SelectedAccessory()
        {
            DriverDefinition driver = SelectedDriver();
            return driver?.FindAccessory(SaveManager.GetSelectedCharacterAccessoryId(driver.DefaultAccessoryId));
        }

        private static bool IsAccessoryUnlocked(DriverDefinition.CharacterAccessoryOption option) =>
            option != null && SaveManager.IsCustomizationUnlocked("global", "CharacterAccessory",
                option.accessoryId, option.unlockedByDefault);

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
            Text targetConfirmationStatus = null, Button targetConfirmationYes = null,
            Text targetGemsBalance = null, Button[] targetBlueprintButtons = null,
            Image[] targetBlueprintImages = null, Text[] targetBlueprintLabels = null,
            Text targetBlueprintPage = null, Text targetBlueprintCoins = null,
            Image[] targetOptionBlueprintFills = null, Text[] targetOptionBlueprintTexts = null,
            Button[] targetBlueprintPackButtons = null, Text targetCharacterSummary = null,
            Button[] targetShopAccessoryButtons = null, Image[] targetShopAccessoryImages = null,
            Text[] targetShopAccessoryLabels = null)
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
            gemsBalance = targetGemsBalance;
            blueprintButtons = targetBlueprintButtons; blueprintImages = targetBlueprintImages;
            blueprintLabels = targetBlueprintLabels; blueprintPageText = targetBlueprintPage;
            blueprintCoinsText = targetBlueprintCoins;
            optionBlueprintFills = targetOptionBlueprintFills;
            optionBlueprintTexts = targetOptionBlueprintTexts;
            blueprintPackButtons = targetBlueprintPackButtons;
            characterSummary = targetCharacterSummary;
            shopAccessoryButtons = targetShopAccessoryButtons;
            shopAccessoryImages = targetShopAccessoryImages;
            shopAccessoryLabels = targetShopAccessoryLabels;
            FindSavedVehicle();
            Refresh();
        }
    }
}
