using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    [System.Serializable]
    public sealed class HillAdventurerGarageBindings
    {
        public Button startButton;
        public Text pendingTrackLabel;
        public TopNavigationUI topNavigation;

        public Image garageBackground;
        public Sprite defaultGarageBackground;
        public Sprite neonGarageBackground;
        public Sprite animeGarageBackground;
        public Button[] backgroundButtons;
        public Image shopMenuBackground;
        public Sprite defaultMenuBackground;
        public Sprite neonMenuBackground;
        public Button[] menuBackgroundButtons;
        public Button previousVehicleButton;
        public Button nextVehicleButton;

        public Toggle masterAudioToggle;
        public Text masterAudioStatus;

        public Image[] tuneIcons;
        public Image[] tuneCoinIcons;
        public Button[] tuneCardButtons;
        public Sprite engineTuneIcon;
        public Sprite accelerationTuneIcon;
        public Sprite suspensionTuneIcon;
        public Sprite gripTuneIcon;
        public Sprite fuelTuneIcon;
        public Sprite downforceTuneIcon;
        public Sprite thrustersTuneIcon;
        public Sprite buggyInclinationTuneIcon;
        public Sprite tuneAffordableBackground;
        public Sprite tuneUnaffordableBackground;

        public Image confirmationIcon;
        public Image confirmationCoinIcon;
        public Image confirmationPanel;
        public Text confirmationDescription;
    }

    [ExecuteAlways]
    public sealed class GarageUI : MonoBehaviour
    {
        private enum Category { Skin, Wheels, Driver, Characters, Tuning }
        private enum PurchaseKind { None, Vehicle, Tune, Customization, Blueprints }

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
        [SerializeField] private Button startButton;
        [SerializeField] private Text pendingTrackLabel;
        [SerializeField] private TopNavigationUI topNavigation;
        [SerializeField] private Image garageBackground;
        [SerializeField] private Sprite defaultGarageBackground;
        [SerializeField] private Sprite neonGarageBackground;
        [SerializeField] private Sprite animeGarageBackground;
        [SerializeField] private Button[] backgroundButtons;
        [SerializeField] private Image shopMenuBackground;
        [SerializeField] private Sprite defaultMenuBackground;
        [SerializeField] private Sprite neonMenuBackground;
        [SerializeField] private Button[] menuBackgroundButtons;
        [SerializeField] private Button previousVehicleButton;
        [SerializeField] private Button nextVehicleButton;
        [SerializeField, Tooltip("Optional transparent artwork displayed over locked vehicles.")]
        private Sprite vehicleLockSprite;
        [SerializeField] private Toggle masterAudioToggle;
        [SerializeField] private Text masterAudioStatus;
        [SerializeField] private Image[] tuningIcons;
        [SerializeField] private Image[] tuningCoinIcons;
        [SerializeField] private Button[] tuningCardButtons;
        [SerializeField] private Sprite engineTuneIcon;
        [SerializeField] private Sprite accelerationTuneIcon;
        [SerializeField] private Sprite suspensionTuneIcon;
        [SerializeField] private Sprite gripTuneIcon;
        [SerializeField] private Sprite fuelTuneIcon;
        [SerializeField] private Sprite downforceTuneIcon;
        [SerializeField] private Sprite thrustersTuneIcon;
        [SerializeField] private Sprite buggyInclinationTuneIcon;
        [SerializeField] private Sprite tuneAffordableBackground;
        [SerializeField] private Sprite tuneUnaffordableBackground;
        [SerializeField] private Image confirmationIcon;
        [SerializeField] private Image confirmationCoinIcon;
        [SerializeField] private Image confirmationPanel;
        [SerializeField] private Text confirmationDescription;

        private int vehicleIndex;
        private Category category;
        private GarageSection section;
        private GarageSection sectionBeforeSettings = GarageSection.Vehicle;
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
        private bool startInFlight;
        private bool updatingAudioToggle;
        private int lastPurchaseAttemptFrame = -1;
        private int lastTuneCardFrame = -1;
        private int lastTuneCardSlot = -1;
        private int lastObservedCoins = int.MinValue;
        private int lastObservedGems = int.MinValue;
        private float nextEconomyCheck;
        private string pendingVehicleId = string.Empty;
        private int pendingPurchasedTune;
        private int pendingTuneMaximum;
        private int pendingTuneCost = -1;
        private bool tuneCardListenersInitialized;
        private GameObject[] garageSectionTabs;
        private int customizationAccessoryPage;
        private int shopAccessoryPage;
        private GameObject customizationAccessoryPager;
        private GameObject shopAccessoryPager;
        private Text customizationAccessoryPageText;
        private Text shopAccessoryPageText;
        private Button[] customizationAccessoryFlipButtons;
        private Button[] shopAccessoryFlipButtons;
        private Image vehicleLockShade;
        private Image vehicleLockIcon;
        private Button vehiclePurchaseButton;
        private Text vehiclePurchaseLabel;
        private Image[] optionRarityFrames;
        private Image[] optionOwnedBadges;
        private Image[] optionEquippedBadges;
        private Button[] optionReadyButtons;
        private Image[] shopAccessoryRarityFrames;
        private StarterPackShopUI starterPackShop;

        private Sprite ResolvedVehicleLockSprite => vehicleLockSprite != null
            ? vehicleLockSprite : Resources.Load<Sprite>("UI/LockIcon");

        private const string DefaultBackgroundId = "default";
        private const string NeonBackgroundId = "neon";
        private const string AnimeBackgroundId = "anime";

        private VehicleDefinition CurrentVehicle => catalog != null && catalog.vehicles != null && catalog.vehicles.Length > 0
            ? catalog.vehicles[Mathf.Clamp(vehicleIndex, 0, catalog.vehicles.Length - 1)] : null;

        private void OnEnable()
        {
            SaveManager.CurrencyChanged -= HandleCurrencyChanged;
            if (Application.isPlaying) SaveManager.CurrencyChanged += HandleCurrencyChanged;
            if (catalog == null) return;
            startInFlight = false;
            purchaseInFlight = false;
            lastPurchaseAttemptFrame = -1;
            lastTuneCardFrame = -1;
            lastTuneCardSlot = -1;
            nextEconomyCheck = 0f;
            EnsureSharedMenuBackgroundControls();
            EnsureRuntimeTuningRows();
            EnsureTuneAdjustmentButtons();
            EnsureTuneCardButtons();
            EnsureAccessoryPaginationControls();
            EnsureVehiclePurchaseControls();
            EnsureOptionStateVisuals();
            if (Application.isPlaying) starterPackShop = StarterPackShopUI.Attach(storePanel, catalog);
            FindSavedVehicle();
            section = GameSession.RequestedGarageSection;
            preview?.Invalidate();
            if (confirmationModal != null) confirmationModal.SetActive(false);
            Refresh();
            EconomyCounterIconBinder.RefreshActiveScene();
            if (section == GarageSection.Store) ResetStoreScrollToTop();
        }

        private void OnDisable() => SaveManager.CurrencyChanged -= HandleCurrencyChanged;

        private void HandleCurrencyChanged()
        {
            if (!Application.isPlaying || catalog == null) return;
            int coins = SaveManager.TotalCoins;
            int gems = SaveManager.TotalGems;
            bool coinsChanged = coins != lastObservedCoins;
            bool gemsChanged = gems != lastObservedGems;
            lastObservedCoins = coins;
            lastObservedGems = gems;
            RefreshEconomyOnly(coinsChanged, gemsChanged);
            starterPackShop?.Refresh();
        }

        public void Back() => SceneManager.LoadScene("PreStart");
        public void PreviousVehicle() => CycleVehicle(-1);
        public void NextVehicle() => CycleVehicle(1);
        public void ShowWheels() { category = Category.Wheels; Refresh(); }
        public void ShowDrivers() { category = Category.Driver; Refresh(); }
        public void ShowCharacters()
        {
            category = Category.Characters;
            int selectedIndex = SelectedAccessoryIndex();
            int pageSize = Mathf.Max(1, optionButtons?.Length ?? 0);
            customizationAccessoryPage = selectedIndex >= 0 ? selectedIndex / pageSize : 0;
            Refresh();
        }
        public void ShowTuning() => SetSection(GarageSection.Tuning);
        public void ShowVehicle() => SetSection(GarageSection.Vehicle);
        public void ShowUpgrade() => SetSection(GarageSection.Upgrade);
        public void ShowStore() => SetSection(GarageSection.Store);
        public void ShowSettings()
        {
            if (section == GarageSection.Settings)
            {
                GarageSection returnSection = sectionBeforeSettings == GarageSection.Settings
                    ? GarageSection.Vehicle : sectionBeforeSettings;
                SetSection(returnSection);
                return;
            }

            sectionBeforeSettings = section;
            SetSection(GarageSection.Settings);
        }

        public void ShowSkins()
        {
            category = Category.Skin;
            SetSection(GarageSection.Skins);
        }

        public void ToggleSkins()
        {
            if (section == GarageSection.Skins)
            {
                SetSection(GameSession.HasPendingAdventureTrack
                    ? GarageSection.Tuning : GarageSection.Vehicle);
                return;
            }

            ShowSkins();
        }
        public void StartAdventure()
        {
            if (!Application.isPlaying || startInFlight || catalog == null) return;
            startInFlight = true;
            RefreshAdventureContext();
            if (!GameSession.TryCommitPendingAdventureRun(catalog, out string gameplayScene) ||
                string.IsNullOrEmpty(gameplayScene))
            {
                startInFlight = false;
                RefreshAdventureContext();
                return;
            }
            SceneManager.LoadScene(gameplayScene);
        }
        public void SelectGarageBackground0() => SelectGarageBackground(DefaultBackgroundId);
        public void SelectGarageBackground1() => SelectGarageBackground(NeonBackgroundId);
        public void SelectGarageBackground2() => SelectGarageBackground(AnimeBackgroundId);
        public void SelectMenuBackground0() => SelectMenuBackground(DefaultBackgroundId);
        public void SelectMenuBackground1() => SelectMenuBackground(NeonBackgroundId);
        public void ToggleMasterAudio() => SetMasterAudioEnabled(!SaveManager.MasterAudioEnabled);
        public void SetMasterAudioEnabled(bool enabled)
        {
            if (!Application.isPlaying || updatingAudioToggle) return;
            SaveManager.MasterAudioEnabled = enabled;
            RefreshAudioSetting();
        }
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
        public void SelectShopAccessory3() => SelectShopAccessory(3);
        public void PreviousCustomizationAccessoryPage()
        {
            customizationAccessoryPage = Mathf.Max(0, customizationAccessoryPage - 1);
            Refresh();
        }
        public void NextCustomizationAccessoryPage()
        {
            int pageSize = Mathf.Max(1, optionButtons?.Length ?? 0);
            int pages = Mathf.Max(1, Mathf.CeilToInt(OptionCount() / (float)pageSize));
            customizationAccessoryPage = Mathf.Min(pages - 1, customizationAccessoryPage + 1);
            Refresh();
        }
        public void PreviousShopAccessoryPage()
        {
            shopAccessoryPage = Mathf.Max(0, shopAccessoryPage - 1);
            RefreshShopAccessories();
        }
        public void NextShopAccessoryPage()
        {
            int count = SelectedDriver()?.accessories?.Length ?? 0;
            int pageSize = Mathf.Max(1, shopAccessoryButtons?.Length ?? 0);
            int pages = Mathf.Max(1, Mathf.CeilToInt(count / (float)pageSize));
            shopAccessoryPage = Mathf.Min(pages - 1, shopAccessoryPage + 1);
            RefreshShopAccessories();
        }
        public void PreviousBlueprintPage() { blueprintPage = Mathf.Max(0, blueprintPage - 1); RefreshBlueprintShop(); }
        public void NextBlueprintPage()
        {
            int pages = Mathf.Max(1, Mathf.CeilToInt(PremiumBundleCatalog.GetAll(catalog).Count / 6f));
            blueprintPage = Mathf.Min(pages - 1, blueprintPage + 1);
            RefreshBlueprintShop();
        }

        private void Update()
        {
            if (!Application.isPlaying || catalog == null) return;
            if (Time.unscaledTime < nextEconomyCheck) return;
            nextEconomyCheck = Time.unscaledTime + .20f;
            int coins = SaveManager.TotalCoins;
            int gems = SaveManager.TotalGems;
            bool coinsChanged = coins != lastObservedCoins;
            bool gemsChanged = gems != lastObservedGems;
            if (!coinsChanged && !gemsChanged) return;

            lastObservedCoins = coins;
            lastObservedGems = gems;
            RefreshEconomyOnly(coinsChanged, gemsChanged);
        }

        private void SetSection(GarageSection targetSection)
        {
            bool enteringStore = section != GarageSection.Store && targetSection == GarageSection.Store;
            bool previewVisibilityChanged = IsIsolatedSection(section) !=
                IsIsolatedSection(targetSection);
            if (previewVisibilityChanged) preview?.Invalidate();
            section = targetSection;
            GameSession.RequestedGarageSection = targetSection;
            Refresh();
            if (enteringStore) ResetStoreScrollToTop();
        }

        private void CycleVehicle(int direction)
        {
            int count = catalog != null && catalog.vehicles != null ? catalog.vehicles.Length : 0;
            if (count == 0) return;
            int stepDirection = direction < 0 ? -1 : 1;
            int candidateIndex = vehicleIndex;
            for (int step = 0; step < count; step++)
            {
                candidateIndex = (candidateIndex + stepDirection + count) % count;
                VehicleDefinition candidate = catalog.vehicles[candidateIndex];
                if (candidate == null) continue;
                vehicleIndex = candidateIndex;
                if (IsVehicleUnlocked(candidate))
                    SaveManager.SelectedVehicleId = candidate.vehicleId;
                Refresh();
                return;
            }

            // A malformed/future catalog with every vehicle locked remains inspectable without
            // persisting or launching a locked selection.
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
            bool affordable = SaveManager.TotalCoins >= price;
            ConfigureNonTuneConfirmation(true, affordable);
            if (confirmationCost != null)
                confirmationCost.text = confirmationCoinIcon != null ? price.ToString("N0") :
                    $"COST  {price:N0} COINS";
            if (confirmationStatus != null)
                confirmationStatus.text = affordable ? string.Empty : "NOT ENOUGH COINS";
            if (confirmationYes != null) confirmationYes.interactable = affordable;
            SetConfirmationActionLabel("BUY");
            if (confirmationModal != null) confirmationModal.SetActive(true);
        }

        private void PurchaseTune(int slot)
        {
            if (!Application.isPlaying) return;
            int frame = Time.frameCount;
            if (lastTuneCardFrame == frame && lastTuneCardSlot == slot) return;
            lastTuneCardFrame = frame;
            lastTuneCardSlot = slot;

            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null) return;
            if (!IsVehicleUnlocked(vehicle))
            {
                PurchaseCurrentVehicle();
                return;
            }
            if (slot < 0 || slot >= vehicle.GetTuneSlotCount() || catalog?.upgradeEconomy == null) return;
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
            pendingVehicleId = vehicle.vehicleId;
            VehicleUpgradeState state = equippedState;
            int current = purchased;
            int maximum = vehicle.GetMaxTunes(pendingType);
            int cost = current >= maximum ? -1 :
                catalog.upgradeEconomy.GetCostForNextTune(vehicle, pendingType, current);
            pendingPurchasedTune = current;
            pendingTuneMaximum = maximum;
            pendingTuneCost = cost;
            var next = new VehicleUpgradeState();
            next.CopyFrom(state);
            next.SetTune(pendingType, current < maximum ? current + 1 : current);
            next.ClampTo(vehicle);
            if (confirmationTitle != null)
                confirmationTitle.text = vehicle.GetTuneDisplayName(pendingType).ToUpperInvariant();
            if (confirmationValues != null)
                confirmationValues.text = current >= maximum
                    ? $"LEVEL {current} / {maximum}\n{TuneValue(vehicle, state, pendingType)}"
                    : $"LEVEL {current} / {maximum}\nCURRENT  {TuneValue(vehicle, state, pendingType)}\n" +
                      $"NEXT  {TuneValue(vehicle, next, pendingType)}";
            if (confirmationDescription != null)
            {
                confirmationDescription.enabled = true;
                confirmationDescription.text = TuneDescription(vehicle, pendingType);
            }
            if (confirmationIcon != null)
            {
                confirmationIcon.sprite = ResolveTuneIcon(vehicle, pendingType);
                confirmationIcon.enabled = confirmationIcon.sprite != null;
                confirmationIcon.preserveAspect = true;
            }
            RefreshPendingTuneModalState();
            SetConfirmationActionLabel("UPGRADE");
            if (confirmationModal != null) confirmationModal.SetActive(true);
        }

        private void DecreaseTune(int slot)
        {
            if (!Application.isPlaying) return;
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null || !IsVehicleUnlocked(vehicle) || slot < 0 ||
                slot >= vehicle.GetTuneSlotCount()) return;
            SaveManager.TryDecreaseEquippedTune(vehicle, vehicle.GetTuneTypeForSlot(slot), out _);
            Refresh();
        }

        public void ConfirmPurchase()
        {
            if (!Application.isPlaying || purchaseInFlight || CurrentVehicle == null ||
                pendingPurchaseKind == PurchaseKind.None || lastPurchaseAttemptFrame == Time.frameCount) return;
            lastPurchaseAttemptFrame = Time.frameCount;
            purchaseInFlight = true;
            if (confirmationYes != null) confirmationYes.interactable = false;
            if (pendingPurchaseKind == PurchaseKind.Vehicle)
            {
                VehicleDefinition vehicle = CurrentVehicle;
                if (vehicle == null || vehicle.vehicleId != pendingVehicleId)
                {
                    purchaseInFlight = false;
                    if (confirmationStatus != null) confirmationStatus.text = "VEHICLE STATE CHANGED";
                    return;
                }
                bool vehiclePurchased = SaveManager.TryPurchaseVehicle(vehicle,
                    out CoinUnlockPurchaseResult vehicleResult);
                purchaseInFlight = false;
                if (vehiclePurchased || vehicleResult == CoinUnlockPurchaseResult.AlreadyUnlocked)
                {
                    SaveManager.SelectedVehicleId = vehicle.vehicleId;
                    ClearPendingPurchase();
                    if (confirmationModal != null) confirmationModal.SetActive(false);
                    preview?.Invalidate();
                    Refresh();
                    return;
                }
                if (confirmationStatus != null)
                    confirmationStatus.text = vehicleResult == CoinUnlockPurchaseResult.NotEnoughCoins
                        ? "NOT ENOUGH COINS" : "UNAVAILABLE";
                RefreshOpenPurchaseAffordability();
                return;
            }
            if (pendingPurchaseKind == PurchaseKind.Blueprints)
            {
                bool blueprintPurchased = SaveManager.TryPurchaseBlueprints(catalog, pendingBundleId,
                    pendingBlueprintTier, out BlueprintPurchaseResult blueprintResult);
                purchaseInFlight = false;
                if (blueprintPurchased || blueprintResult == BlueprintPurchaseResult.AlreadyUnlocked)
                {
                    ClearPendingPurchase();
                    if (confirmationModal != null) confirmationModal.SetActive(false);
                    Refresh();
                    return;
                }
                if (confirmationStatus != null)
                    confirmationStatus.text = blueprintResult == BlueprintPurchaseResult.NotEnoughCoins
                        ? "NOT ENOUGH COINS" : "UNAVAILABLE";
                if (blueprintResult == BlueprintPurchaseResult.NotEnoughCoins)
                    RefreshOpenPurchaseAffordability();
                else if (confirmationYes != null)
                    confirmationYes.interactable = false;
                RefreshBlueprintShop();
                return;
            }
            if (pendingPurchaseKind == PurchaseKind.Customization)
            {
                if (pendingCategory != Category.Characters &&
                    CurrentVehicle.vehicleId != pendingVehicleId)
                {
                    purchaseInFlight = false;
                    if (confirmationStatus != null) confirmationStatus.text = "CUSTOMIZATION STATE CHANGED";
                    if (confirmationYes != null) confirmationYes.interactable = false;
                    return;
                }
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
                    preview?.Invalidate();
                    ClearPendingPurchase();
                    if (confirmationModal != null) confirmationModal.SetActive(false);
                    Refresh();
                    return;
                }
                if (confirmationStatus != null)
                    confirmationStatus.text = cosmeticResult == CustomizationPurchaseResult.NotEnoughGems
                        ? "NOT ENOUGH GEMS" : "UNAVAILABLE";
                if (cosmeticResult == CustomizationPurchaseResult.NotEnoughGems)
                    RefreshOpenPurchaseAffordability();
                else if (confirmationYes != null)
                    confirmationYes.interactable = false;
                Refresh();
                return;
            }
            if (catalog?.upgradeEconomy == null)
            {
                purchaseInFlight = false;
                return;
            }
            if (!PendingTuneStateIsCurrent(out TunePurchaseResult validationResult))
            {
                purchaseInFlight = false;
                if (confirmationStatus != null)
                    confirmationStatus.text = validationResult == TunePurchaseResult.NotEnoughCoins
                        ? "NOT ENOUGH COINS"
                        : validationResult == TunePurchaseResult.MaximumLevel ? "MAX" : "TUNE STATE CHANGED";
                RefreshPendingTuneModalState();
                return;
            }
            bool purchased = SaveManager.TryPurchaseTune(CurrentVehicle, pendingType, catalog.upgradeEconomy,
                out _, out TunePurchaseResult result);
            purchaseInFlight = false;
            if (purchased)
            {
                ClearPendingPurchase();
                if (confirmationModal != null) confirmationModal.SetActive(false);
                Refresh();
                return;
            }
            if (confirmationStatus != null)
                confirmationStatus.text = result == TunePurchaseResult.NotEnoughCoins
                    ? "NOT ENOUGH COINS" : result == TunePurchaseResult.MaximumLevel ? "MAX" : "UNAVAILABLE";
            RefreshPendingTuneModalState();
            RefreshTuning(CurrentVehicle);
        }

        public void CancelPurchase()
        {
            if (purchaseInFlight) return;
            if (confirmationModal != null) confirmationModal.SetActive(false);
            ClearPendingPurchase();
        }

        private void ClearPendingPurchase()
        {
            pendingPurchaseKind = PurchaseKind.None;
            pendingOptionId = string.Empty;
            pendingBundleId = string.Empty;
            pendingVehicleId = string.Empty;
            pendingPurchasedTune = 0;
            pendingTuneMaximum = 0;
            pendingTuneCost = -1;
        }

        private bool PendingTuneStateIsCurrent(out TunePurchaseResult result)
        {
            VehicleDefinition vehicle = CurrentVehicle;
            if (pendingPurchaseKind != PurchaseKind.Tune || vehicle == null ||
                catalog?.upgradeEconomy == null || vehicle.vehicleId != pendingVehicleId)
            {
                result = TunePurchaseResult.InvalidConfiguration;
                return false;
            }

            int maximum = vehicle.GetMaxTunes(pendingType);
            int purchased = SaveManager.LoadPurchasedUpgrades(vehicle.vehicleId, vehicle).GetTune(pendingType);
            if (maximum <= 0 || purchased >= maximum)
            {
                result = TunePurchaseResult.MaximumLevel;
                return false;
            }

            int cost = catalog.upgradeEconomy.GetCostForNextTune(vehicle, pendingType, purchased);
            if (purchased != pendingPurchasedTune || maximum != pendingTuneMaximum ||
                cost < 0 || cost != pendingTuneCost)
            {
                result = TunePurchaseResult.InvalidConfiguration;
                return false;
            }
            if (SaveManager.TotalCoins < cost)
            {
                result = TunePurchaseResult.NotEnoughCoins;
                return false;
            }

            result = TunePurchaseResult.Success;
            return true;
        }

        private void RefreshPendingTuneModalState()
        {
            if (pendingPurchaseKind != PurchaseKind.Tune) return;
            VehicleDefinition vehicle = CurrentVehicle;
            bool correctVehicle = vehicle != null && vehicle.vehicleId == pendingVehicleId;
            int maximum = correctVehicle ? vehicle.GetMaxTunes(pendingType) : 0;
            int purchased = correctVehicle
                ? SaveManager.LoadPurchasedUpgrades(vehicle.vehicleId, vehicle).GetTune(pendingType)
                : 0;
            bool maximumReached = !correctVehicle || maximum <= 0 || purchased >= maximum;
            int cost = !maximumReached && catalog?.upgradeEconomy != null
                ? catalog.upgradeEconomy.GetCostForNextTune(vehicle, pendingType, purchased)
                : -1;
            bool stateMatches = correctVehicle && purchased == pendingPurchasedTune &&
                maximum == pendingTuneMaximum && cost == pendingTuneCost;
            bool affordable = cost >= 0 && SaveManager.TotalCoins >= cost;

            if (confirmationCost != null)
            {
                confirmationCost.text = maximumReached ? "MAX" : confirmationCoinIcon != null
                    ? cost.ToString("N0") : $"COST  {cost:N0} COINS";
                confirmationCost.color = maximumReached || affordable
                    ? Color.white : new Color(1f, .22f, .18f, 1f);
            }
            if (confirmationCoinIcon != null)
            {
                confirmationCoinIcon.enabled = !maximumReached && cost >= 0;
                confirmationCoinIcon.color = affordable
                    ? Color.white : new Color(1f, .22f, .18f, 1f);
            }
            if (confirmationStatus != null)
                confirmationStatus.text = maximumReached ? "MAX LEVEL" : !stateMatches
                    ? "TUNE STATE CHANGED" : affordable ? string.Empty : "NOT ENOUGH COINS";
            if (confirmationYes != null)
                confirmationYes.interactable = !purchaseInFlight && !maximumReached &&
                    stateMatches && affordable;
        }

        private void ConfigureNonTuneConfirmation(bool usesCoins, bool affordable)
        {
            if (confirmationIcon != null)
            {
                confirmationIcon.sprite = null;
                confirmationIcon.enabled = false;
            }
            if (confirmationDescription != null)
            {
                confirmationDescription.text = string.Empty;
                confirmationDescription.enabled = false;
            }
            if (confirmationCoinIcon != null)
            {
                confirmationCoinIcon.enabled = usesCoins;
                confirmationCoinIcon.color = affordable
                    ? Color.white : new Color(1f, .20f, .16f, 1f);
            }
            if (confirmationCost != null)
                confirmationCost.color = affordable
                    ? Color.white : new Color(1f, .20f, .16f, 1f);
        }

        private void SelectOption(int slot)
        {
            int index = category == Category.Characters
                ? customizationAccessoryPage * Mathf.Max(1, optionButtons?.Length ?? 0) + slot
                : slot;
            VehicleDefinition vehicle = CurrentVehicle;
            if (category == Category.Tuning || vehicle == null || !IsVehicleUnlocked(vehicle) ||
                !TryGetOption(index, out string id, out _, out bool unlocked, out int gemPrice)) return;
            if (!unlocked)
            {
                if (gemPrice <= 0 || category == Category.Driver) return;
                pendingPurchaseKind = PurchaseKind.Customization;
                pendingCategory = category;
                pendingVehicleId = vehicle.vehicleId;
                pendingOptionId = id;
                pendingGemPrice = gemPrice;
                if (confirmationTitle != null) confirmationTitle.text = $"BUY {OptionName(index).ToUpperInvariant()}?";
                if (confirmationValues != null) confirmationValues.text = "PERMANENT COSMETIC UNLOCK";
                if (confirmationCost != null) confirmationCost.text = $"COST  {gemPrice:N0} GEMS";
                bool affordable = SaveManager.TotalGems >= gemPrice;
                ConfigureNonTuneConfirmation(false, affordable);
                if (confirmationStatus != null)
                    confirmationStatus.text = affordable ? string.Empty : "NOT ENOUGH GEMS";
                if (confirmationYes != null) confirmationYes.interactable = affordable;
                SetConfirmationActionLabel("BUY");
                if (confirmationModal != null) confirmationModal.SetActive(true);
                return;
            }
            SetSelectedOption(vehicle, category, id);
            preview?.Invalidate();
            Refresh();
        }

        public void PurchaseCurrentVehicle()
        {
            VehicleDefinition vehicle = CurrentVehicle;
            if (!Application.isPlaying || vehicle == null || IsVehicleUnlocked(vehicle)) return;
            pendingPurchaseKind = PurchaseKind.Vehicle;
            pendingVehicleId = vehicle.vehicleId;
            bool affordable = vehicle.coinPrice > 0 && SaveManager.TotalCoins >= vehicle.coinPrice;
            if (confirmationTitle != null)
                confirmationTitle.text = $"UNLOCK FOR {vehicle.coinPrice:N0} COINS?";
            if (confirmationValues != null)
                confirmationValues.text = $"{vehicle.displayName.ToUpperInvariant()}\nPERMANENT VEHICLE UNLOCK";
            ConfigureNonTuneConfirmation(true, affordable);
            if (confirmationDescription != null)
            {
                confirmationDescription.enabled = true;
                confirmationDescription.text = "Unlock this vehicle permanently and select it now.";
            }
            if (confirmationIcon != null)
            {
                confirmationIcon.sprite = vehicle.previewSprite;
                confirmationIcon.enabled = vehicle.previewSprite != null;
                confirmationIcon.preserveAspect = true;
            }
            if (confirmationCost != null)
                confirmationCost.text = confirmationCoinIcon != null ? vehicle.coinPrice.ToString("N0") :
                    $"COST  {vehicle.coinPrice:N0} COINS";
            if (confirmationStatus != null)
                confirmationStatus.text = affordable ? string.Empty : "NOT ENOUGH COINS";
            if (confirmationYes != null) confirmationYes.interactable = affordable;
            SetConfirmationActionLabel("YES");
            if (confirmationModal != null) confirmationModal.SetActive(true);
        }

        private void EnsureAccessoryPaginationControls()
        {
            if (!Application.isPlaying) return;
            if (customizationAccessoryPager == null && optionButtons != null && optionButtons.Length > 0 &&
                optionButtons[0] != null)
            {
                customizationAccessoryPager = CreateAccessoryPager(optionButtons[0].transform.parent,
                    "Customization Accessory Pages", new Vector2(320f, 205f), false,
                    out customizationAccessoryPageText);
            }
            if (shopAccessoryPager == null && shopAccessoryButtons != null && shopAccessoryButtons.Length > 0 &&
                shopAccessoryButtons[0] != null)
            {
                shopAccessoryPager = CreateAccessoryPager(shopAccessoryButtons[0].transform.parent,
                    "Shop Accessory Pages", new Vector2(260f, 260f), true,
                    out shopAccessoryPageText);
            }
            EnsureAccessoryFlipButtons();
        }

        private void EnsureAccessoryFlipButtons()
        {
            int customizationCount = optionButtons?.Length ?? 0;
            if (customizationAccessoryFlipButtons == null ||
                customizationAccessoryFlipButtons.Length != customizationCount)
                customizationAccessoryFlipButtons = new Button[customizationCount];
            for (int i = 0; i < customizationCount; i++)
            {
                if (optionButtons[i] == null) continue;
                int slot = i;
                customizationAccessoryFlipButtons[i] = FindOrCreateAccessoryFlipButton(
                    optionButtons[i], "Cap Orientation", () => ToggleCustomizationCap(slot));
            }

            int shopCount = shopAccessoryButtons?.Length ?? 0;
            if (shopAccessoryFlipButtons == null || shopAccessoryFlipButtons.Length != shopCount)
                shopAccessoryFlipButtons = new Button[shopCount];
            for (int i = 0; i < shopCount; i++)
            {
                if (shopAccessoryButtons[i] == null) continue;
                int slot = i;
                shopAccessoryFlipButtons[i] = FindOrCreateAccessoryFlipButton(
                    shopAccessoryButtons[i], "Cap Orientation", () => ToggleShopCap(slot));
            }
        }

        private static Button FindOrCreateAccessoryFlipButton(Button card, string name,
            UnityEngine.Events.UnityAction action)
        {
            Transform existing = card.transform.Find(name);
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
            {
                GameObject buttonObject = new GameObject(name, typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image), typeof(Button));
                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                rect.SetParent(card.transform, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
                rect.anchoredPosition = new Vector2(-7f, -7f);
                rect.sizeDelta = new Vector2(44f, 38f);
                button = buttonObject.GetComponent<Button>();
                buttonObject.GetComponent<Image>().color = new Color(.08f, .68f, .95f, .98f);

                GameObject labelObject = new GameObject("Arrow", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Text));
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.SetParent(rect, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.sizeDelta = Vector2.zero;
                Text label = labelObject.GetComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 26;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.raycastTarget = false;
            }

            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
            button.gameObject.SetActive(false);
            return button;
        }

        private void ToggleCustomizationCap(int slot)
        {
            if (category != Category.Characters) return;
            DriverDefinition driver = SelectedDriver();
            int pageSize = Mathf.Max(1, optionButtons?.Length ?? 0);
            int index = customizationAccessoryPage * pageSize + slot;
            if (driver?.accessories == null || index < 0 || index >= driver.accessories.Length) return;
            DriverDefinition.CharacterAccessoryOption option = driver.accessories[index];
            if (option == null || !option.supportsFlipVariant || !IsAccessoryUnlocked(option)) return;
            SaveManager.ToggleCharacterAccessoryFlipX(option.accessoryId, option.flipX);
            Refresh();
        }

        private void ToggleShopCap(int slot)
        {
            DriverDefinition driver = SelectedDriver();
            int pageSize = Mathf.Max(1, shopAccessoryButtons?.Length ?? 0);
            int index = shopAccessoryPage * pageSize + slot;
            if (driver?.accessories == null || index < 0 || index >= driver.accessories.Length) return;
            DriverDefinition.CharacterAccessoryOption option = driver.accessories[index];
            if (option == null || !option.supportsFlipVariant || !IsAccessoryUnlocked(option)) return;
            SaveManager.ToggleCharacterAccessoryFlipX(option.accessoryId, option.flipX);
            Refresh();
        }

        private static void RefreshAccessoryFlipButton(Button button,
            DriverDefinition.CharacterAccessoryOption option, bool interactable)
        {
            if (button == null) return;
            bool visible = option != null && option.supportsFlipVariant;
            button.gameObject.SetActive(visible);
            if (!visible) return;
            bool backwards = SaveManager.GetCharacterAccessoryFlipX(option.accessoryId, option.flipX);
            button.interactable = interactable;
            if (button.image != null)
                button.image.color = backwards ? new Color(.96f, .16f, .66f, .98f) :
                    new Color(.08f, .68f, .95f, .98f);
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.text = backwards ? "<" : ">";
        }

        private GameObject CreateAccessoryPager(Transform parent, string name, Vector2 position,
            bool shopPager, out Text pageText)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(parent, false);
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(.5f, .5f);
            rootRect.anchoredPosition = position;
            rootRect.sizeDelta = new Vector2(190f, 52f);

            Button previous = CreateAccessoryPageButton(rootRect, "Previous", "<", new Vector2(-72f, 0f));
            Button next = CreateAccessoryPageButton(rootRect, "Next", ">", new Vector2(72f, 0f));
            if (shopPager)
            {
                previous.onClick.AddListener(PreviousShopAccessoryPage);
                next.onClick.AddListener(NextShopAccessoryPage);
            }
            else
            {
                previous.onClick.AddListener(PreviousCustomizationAccessoryPage);
                next.onClick.AddListener(NextCustomizationAccessoryPage);
            }

            GameObject labelObject = new GameObject("Page", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Text));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rootRect, false);
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(.5f, .5f);
            labelRect.sizeDelta = new Vector2(82f, 48f);
            pageText = labelObject.GetComponent<Text>();
            pageText.font = optionLabels != null && optionLabels.Length > 0 && optionLabels[0] != null
                ? optionLabels[0].font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pageText.fontSize = 18;
            pageText.fontStyle = FontStyle.Bold;
            pageText.alignment = TextAnchor.MiddleCenter;
            pageText.color = Color.white;
            pageText.raycastTarget = false;
            return root;
        }

        private static Button CreateAccessoryPageButton(Transform parent, string name, string label,
            Vector2 position)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(48f, 48f);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(.10f, .35f, .58f, .98f);
            Button button = buttonObject.GetComponent<Button>();

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Text));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 28;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return button;
        }

        private static void RefreshAccessoryPager(GameObject pager, Text pageText, int page, int count,
            int pageSize)
        {
            int pages = Mathf.Max(1, Mathf.CeilToInt(count / (float)Mathf.Max(1, pageSize)));
            if (pager != null) pager.SetActive(pages > 1);
            if (pageText != null) pageText.text = $"{page + 1}/{pages}";
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
            bool hillLayout = tuningIcons != null && tuningIcons.Length > 0;
            for (int i = 0; i < requiredRows && !hillLayout; i++)
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
            bool hillLayout = tuningIcons != null && tuningIcons.Length > 0;
            if (tuningDownButtons == null || tuningDownButtons.Length != tuningButtons.Length)
                System.Array.Resize(ref tuningDownButtons, tuningButtons.Length);
            for (int i = 0; i < tuningButtons.Length; i++)
            {
                Button up = tuningButtons[i];
                if (up == null || up.transform.parent == null) continue;
                RectTransform upRect = up.transform as RectTransform;
                if (upRect != null && !hillLayout)
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

        private void EnsureSharedMenuBackgroundControls()
        {
            if (!Application.isPlaying || settingsPanel == null) return;
            if (shopMenuBackground == null)
            {
                Image[] images = settingsPanel.transform.root.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < images.Length; i++)
                    if (images[i] != null &&
                        (images[i].name == "Shop Neon Background" ||
                         images[i].name == "Shop Shared Background (Compatibility Layer)"))
                    {
                        shopMenuBackground = images[i];
                        break;
                    }
            }

            PositionSettingsControl("Settings Title", new Vector2(.10f, .88f), new Vector2(.90f, .98f));
            PositionSettingsControl("Audio Label", new Vector2(.12f, .76f), new Vector2(.54f, .86f));
            PositionSettingsControl("Master Audio Status", new Vector2(.62f, .69f), new Vector2(.84f, .75f));
            Transform audioToggle = FindSettingsDescendant("Master Audio Toggle");
            if (audioToggle is RectTransform audioRect)
            {
                audioRect.anchorMin = audioRect.anchorMax = new Vector2(.72f, .81f);
                audioRect.anchoredPosition = Vector2.zero;
            }
            PositionSettingsControl("Garage Background Title", new Vector2(.10f, .31f),
                new Vector2(.90f, .38f));
            PositionSettingsControl("Settings Note", new Vector2(.08f, .015f), new Vector2(.92f, .065f));
            Text settingsNote = FindSettingsDescendant("Settings Note")?.GetComponent<Text>();
            if (settingsNote != null)
                settingsNote.text = "AUDIO AND BACKGROUNDS ARE SAVED ON THIS DEVICE.";

            for (int i = 0; i < (backgroundButtons?.Length ?? 0); i++)
            {
                Button button = backgroundButtons[i];
                if (button == null) continue;
                RectTransform rect = button.transform as RectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .23f);
                rect.anchoredPosition = new Vector2(-180f + i * 180f, 0f);
                rect.sizeDelta = new Vector2(158f, 108f);
            }

            if (menuBackgroundButtons != null && menuBackgroundButtons.Length == 2 &&
                menuBackgroundButtons[0] != null && menuBackgroundButtons[1] != null) return;
            if (backgroundButtons == null || backgroundButtons.Length < 2 ||
                backgroundButtons[0] == null || backgroundButtons[1] == null) return;

            Text garageTitle = FindSettingsDescendant("Garage Background Title")?.GetComponent<Text>();
            if (garageTitle != null && FindSettingsDescendant("Menu Background Title") == null)
            {
                Text title = Instantiate(garageTitle, settingsPanel.transform);
                title.name = "Menu Background Title";
                title.text = "BACKGROUND  —  ADVENTURE / SHOP";
                title.fontSize = 20;
                SetStretchRect(title.rectTransform, new Vector2(.08f, .59f), new Vector2(.92f, .66f));
            }

            menuBackgroundButtons = new Button[2];
            Sprite[] sprites = { defaultMenuBackground, neonMenuBackground };
            for (int i = 0; i < menuBackgroundButtons.Length; i++)
            {
                Button button = Instantiate(backgroundButtons[i], settingsPanel.transform);
                button.name = i == 0 ? "Menu Background DEFAULT" : "Menu Background NEON";
                RectTransform rect = button.transform as RectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .51f);
                rect.anchoredPosition = new Vector2(-105f + i * 210f, 0f);
                rect.sizeDelta = new Vector2(190f, 100f);
                if (button.image != null)
                {
                    button.image.sprite = sprites[i];
                    button.image.color = Color.white;
                }
                // Replace the cloned UnityEvent so the Garage-background persistent listener is
                // not inherited by the separate Adventure/Shop selector.
                button.onClick = new Button.ButtonClickedEvent();
                if (i == 0) button.onClick.AddListener(SelectMenuBackground0);
                else button.onClick.AddListener(SelectMenuBackground1);
                menuBackgroundButtons[i] = button;
            }
        }

        private Transform FindSettingsDescendant(string objectName)
        {
            if (settingsPanel == null) return null;
            Transform[] transforms = settingsPanel.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
                if (transforms[i].name == objectName) return transforms[i];
            return null;
        }

        private void PositionSettingsControl(string objectName, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform rect = FindSettingsDescendant(objectName) as RectTransform;
            SetStretchRect(rect, anchorMin, anchorMax);
        }

        private void EnsureTuneCardButtons()
        {
            if (!Application.isPlaying || tuningButtons == null) return;
            if (tuningCardButtons == null || tuningCardButtons.Length != tuningButtons.Length)
                System.Array.Resize(ref tuningCardButtons, tuningButtons.Length);

            for (int i = 0; i < tuningButtons.Length; i++)
            {
                Button upgrade = tuningButtons[i];
                if (upgrade == null || upgrade.transform.parent == null) continue;
                GameObject row = upgrade.transform.parent.gameObject;
                Image cardImage = row.GetComponent<Image>();
                Button cardButton = row.GetComponent<Button>();
                if (cardButton == null) cardButton = row.AddComponent<Button>();
                cardButton.targetGraphic = cardImage;
                cardButton.transition = Selectable.Transition.ColorTint;
                if (cardImage != null) cardImage.raycastTarget = true;
                tuningCardButtons[i] = cardButton;

                if (!tuneCardListenersInitialized && cardButton.onClick.GetPersistentEventCount() == 0)
                {
                    int capturedSlot = i;
                    cardButton.onClick.AddListener(() => PurchaseTune(capturedSlot));
                }
            }
            tuneCardListenersInitialized = true;
        }

        private void EnsureOptionStateVisuals()
        {
            if (!Application.isPlaying) return;
            int count = optionButtons?.Length ?? 0;
            optionRarityFrames = new Image[count];
            optionOwnedBadges = new Image[count];
            optionEquippedBadges = new Image[count];
            optionReadyButtons = new Button[count];
            for (int i = 0; i < count; i++)
            {
                Button option = optionButtons[i];
                if (option == null) continue;
                optionRarityFrames[i] = EnsureStateImage(option.transform, "Rarity Frame",
                    Vector2.zero, Vector2.one, string.Empty);
                optionRarityFrames[i].transform.SetAsFirstSibling();
                optionOwnedBadges[i] = EnsureStateImage(option.transform, "Owned Badge",
                    new Vector2(.02f, .76f), new Vector2(.40f, .98f), "OWNED");
                optionEquippedBadges[i] = EnsureStateImage(option.transform, "Equipped Badge",
                    new Vector2(.56f, .76f), new Vector2(.98f, .98f), "EQUIPPED");
                optionReadyButtons[i] = EnsureReadyButton(option.transform, i);
            }

            int shopCount = shopAccessoryButtons?.Length ?? 0;
            shopAccessoryRarityFrames = new Image[shopCount];
            for (int i = 0; i < shopCount; i++)
            {
                Button option = shopAccessoryButtons[i];
                if (option == null) continue;
                shopAccessoryRarityFrames[i] = EnsureStateImage(option.transform, "Rarity Frame",
                    Vector2.zero, Vector2.one, string.Empty);
                shopAccessoryRarityFrames[i].transform.SetAsFirstSibling();
            }
        }

        private static Image EnsureStateImage(Transform parent, string objectName, Vector2 anchorMin,
            Vector2 anchorMax, string fallbackLabel)
        {
            Transform existing = parent.Find(objectName);
            GameObject instance = existing != null ? existing.gameObject :
                new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = instance.GetComponent<RectTransform>();
            if (existing == null) rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(3f, 3f);
            rect.offsetMax = new Vector2(-3f, -3f);
            Image image = instance.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            if (!string.IsNullOrEmpty(fallbackLabel) && instance.transform.Find("Fallback Label") == null)
            {
                GameObject labelObject = new GameObject("Fallback Label", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Text));
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.SetParent(instance.transform, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                Text label = labelObject.GetComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.text = fallbackLabel;
                label.fontSize = 12;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 8;
                label.resizeTextMaxSize = 12;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.raycastTarget = false;
            }
            return image;
        }

        private Button EnsureReadyButton(Transform parent, int slot)
        {
            Transform existing = parent.Find("Ready Button");
            GameObject instance = existing != null ? existing.gameObject :
                new GameObject("Ready Button", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(Button));
            RectTransform rect = instance.GetComponent<RectTransform>();
            if (existing == null) rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(.57f, .03f);
            rect.anchorMax = new Vector2(.97f, .23f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = instance.GetComponent<Image>();
            Button button = instance.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick = new Button.ButtonClickedEvent();
            int capturedSlot = slot;
            button.onClick.AddListener(() => SelectOption(capturedSlot));

            Transform labelTransform = instance.transform.Find("Fallback Label");
            if (labelTransform == null)
            {
                GameObject labelObject = new GameObject("Fallback Label", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Text));
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.SetParent(instance.transform, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                Text label = labelObject.GetComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.text = "READY";
                label.fontSize = 11;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 8;
                label.resizeTextMaxSize = 11;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.raycastTarget = false;
            }
            return button;
        }

        private void RefreshOptionStateVisual(int slot, int optionIndex, bool unlocked, bool selected,
            bool vehicleUnlocked)
        {
            if (optionRarityFrames == null || slot < 0 || slot >= optionRarityFrames.Length) return;
            bool isSkin = category == Category.Skin && CurrentVehicle?.skins != null &&
                          optionIndex >= 0 && optionIndex < CurrentVehicle.skins.Length &&
                          CurrentVehicle.skins[optionIndex] != null;
            bool isWheel = category == Category.Wheels && CurrentVehicle?.wheels != null &&
                           optionIndex >= 0 && optionIndex < CurrentVehicle.wheels.Length &&
                           CurrentVehicle.wheels[optionIndex] != null;
            DriverDefinition driver = category == Category.Characters ? SelectedDriver() : null;
            bool isAccessory = driver?.accessories != null && optionIndex >= 0 &&
                               optionIndex < driver.accessories.Length &&
                               driver.accessories[optionIndex] != null;
            HillAdventurerUiCatalog assets = HillAdventurerUiCatalog.Instance;
            bool supportsRarity = isSkin || isWheel || isAccessory;
            ItemRarity rarity = ItemRarity.Common;
            if (isSkin)
            {
                VehicleDefinition.CustomizationOption option = CurrentVehicle.skins[optionIndex];
                rarity = option.ResolveRarity(option.optionId == CurrentVehicle.DefaultSkinId);
            }
            else if (isWheel)
            {
                VehicleDefinition.CustomizationOption option = CurrentVehicle.wheels[optionIndex];
                rarity = option.ResolveRarity(option.optionId == CurrentVehicle.DefaultWheelId);
            }
            else if (isAccessory)
            {
                DriverDefinition.CharacterAccessoryOption option = driver.accessories[optionIndex];
                rarity = option.ResolveRarity(option.accessoryId == driver.DefaultAccessoryId);
            }
            SetStateImage(optionRarityFrames[slot], supportsRarity && assets != null
                ? assets.GetRarityFrame(rarity) : null, supportsRarity, string.Empty,
                ItemRarityUtility.AccentColor(rarity));
            SetStateImage(optionOwnedBadges[slot], assets != null ? assets.ownedBadge : null,
                isSkin && unlocked && !selected, "OWNED", new Color(.12f, .58f, .34f, .96f));
            SetStateImage(optionEquippedBadges[slot], assets != null ? assets.equippedBadge : null,
                isSkin && unlocked && selected, "EQUIPPED", new Color(.12f, .52f, .86f, .96f));

            Button ready = optionReadyButtons[slot];
            if (ready != null)
            {
                bool showReady = isSkin && unlocked && !selected;
                ready.gameObject.SetActive(showReady);
                ready.interactable = showReady && vehicleUnlocked;
                Sprite sprite = assets != null ? assets.readyButton : null;
                ready.image.sprite = sprite;
                ready.image.preserveAspect = sprite != null;
                ready.image.color = sprite != null ? Color.white : new Color(.12f, .58f, .82f, .98f);
                Text label = ready.GetComponentInChildren<Text>(true);
                if (label != null) label.enabled = sprite == null;
            }
        }

        private static void SetStateImage(Image image, Sprite sprite, bool visible, string fallbackLabel,
            Color fallbackColor)
        {
            if (image == null) return;
            image.gameObject.SetActive(visible);
            if (!visible) return;
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : fallbackColor;
            image.preserveAspect = sprite != null;
            Text label = image.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                if (!string.IsNullOrEmpty(fallbackLabel)) label.text = fallbackLabel;
                label.enabled = sprite == null && !string.IsNullOrEmpty(fallbackLabel);
            }
            // A missing rarity sheet must not paint a solid panel over the actual item thumbnail.
            if (sprite == null && string.IsNullOrEmpty(fallbackLabel)) image.color = Color.clear;
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

        private void EnsureVehiclePurchaseControls()
        {
            if (!Application.isPlaying || vehiclePanel == null || vehiclePurchaseButton != null) return;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Transform overlayParent = preview != null && preview.TargetImage != null
                ? preview.TargetImage.transform : vehiclePanel.transform.parent;
            if (overlayParent == null) return;

            GameObject shadeObject = new GameObject("Locked Vehicle Shade", typeof(RectTransform), typeof(Image));
            RectTransform shadeRect = shadeObject.GetComponent<RectTransform>();
            shadeRect.SetParent(overlayParent, false);
            SetStretchRect(shadeRect, new Vector2(.34f, .22f), new Vector2(.66f, .82f));
            vehicleLockShade = shadeObject.GetComponent<Image>();
            vehicleLockShade.color = new Color(0f, 0f, 0f, .20f);
            vehicleLockShade.raycastTarget = false;

            GameObject iconObject = new GameObject("Locked Vehicle Icon", typeof(RectTransform), typeof(Image));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(overlayParent, false);
            SetStretchRect(iconRect, new Vector2(.40f, .34f), new Vector2(.60f, .72f));
            vehicleLockIcon = iconObject.GetComponent<Image>();
            vehicleLockIcon.sprite = ResolvedVehicleLockSprite;
            vehicleLockIcon.preserveAspect = true;
            vehicleLockIcon.raycastTarget = true;
            Button lockButton = iconObject.AddComponent<Button>();
            lockButton.transition = Selectable.Transition.None;
            lockButton.onClick.AddListener(PurchaseCurrentVehicle);
            iconObject.AddComponent<ButtonPressFeedback>();

            GameObject buttonObject = new GameObject("Purchase Vehicle", typeof(RectTransform), typeof(Image),
                typeof(Button));
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(overlayParent, false);
            SetStretchRect(buttonRect, new Vector2(.30f, .04f), new Vector2(.70f, .17f));
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(.08f, .52f, .27f, .98f);
            vehiclePurchaseButton = buttonObject.GetComponent<Button>();
            vehiclePurchaseButton.onClick.AddListener(PurchaseCurrentVehicle);
            buttonObject.AddComponent<ButtonPressFeedback>();

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(buttonRect, false);
            SetStretchRect(labelRect, Vector2.zero, Vector2.one);
            vehiclePurchaseLabel = labelObject.GetComponent<Text>();
            vehiclePurchaseLabel.font = font;
            vehiclePurchaseLabel.fontSize = 22;
            vehiclePurchaseLabel.fontStyle = FontStyle.Bold;
            vehiclePurchaseLabel.alignment = TextAnchor.MiddleCenter;
            vehiclePurchaseLabel.color = Color.white;
            vehiclePurchaseLabel.resizeTextForBestFit = true;
            vehiclePurchaseLabel.resizeTextMinSize = 12;
            vehiclePurchaseLabel.resizeTextMaxSize = 22;
            vehiclePurchaseLabel.raycastTarget = false;
        }

        private void RefreshVehiclePurchaseControls(VehicleDefinition vehicle, bool unlocked)
        {
            bool visible = vehicle != null && !unlocked &&
                section != GarageSection.Store && section != GarageSection.Settings;
            if (vehicleLockShade != null) vehicleLockShade.gameObject.SetActive(visible);
            if (vehicleLockIcon != null)
            {
                Sprite sprite = ResolvedVehicleLockSprite;
                vehicleLockIcon.sprite = sprite;
                vehicleLockIcon.gameObject.SetActive(visible && sprite != null);
            }
            if (vehiclePurchaseButton != null)
            {
                vehiclePurchaseButton.gameObject.SetActive(visible);
                vehiclePurchaseButton.interactable = visible && vehicle.coinPrice > 0;
            }
            if (vehiclePurchaseLabel != null && visible)
                vehiclePurchaseLabel.text = vehicle.coinPrice > 0
                    ? $"UNLOCK  {vehicle.coinPrice:N0} COINS" : "UNAVAILABLE";
        }

        private void Refresh()
        {
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null) return;
            int coins = SaveManager.TotalCoins;
            int gems = SaveManager.TotalGems;
            bool vehicleUnlocked = IsVehicleUnlocked(vehicle);
            lastObservedCoins = coins;
            lastObservedGems = gems;
            if (tuningCoins != null) tuningCoins.text = $"COINS  {coins:N0}";
            if (gemsBalance != null) gemsBalance.text = $"GEMS  {gems:N0}";
            if (vehicleName != null)
            {
                vehicleName.text = vehicle.displayName.ToUpperInvariant() +
                    (vehicleUnlocked ? string.Empty : "  —  LOCKED");
                vehicleName.color = vehicleUnlocked ? Color.white : new Color(1f, .28f, .22f, 1f);
            }
            RefreshVehiclePurchaseControls(vehicle, vehicleUnlocked);
            bool adventurePreparation = GameSession.HasPendingAdventureTrack;
            if (sectionTitle != null) sectionTitle.text = section.ToString().ToUpperInvariant();
            if (adventurePreparation)
                topNavigation?.SetSelectedSection(TopNavigationSection.Adventure);
            else
                topNavigation?.SetGarageSection(section);
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
            RefreshVehiclePresentationLayout();
            if (section == GarageSection.Store) RefreshBlueprintShop();
            int count = section == GarageSection.Skins ? OptionCount() : 0;
            string selected = section == GarageSection.Skins ? SelectedOptionId() : string.Empty;
            int optionPageSize = Mathf.Max(1, optionButtons?.Length ?? 0);
            int optionPages = Mathf.Max(1, Mathf.CeilToInt(count / (float)optionPageSize));
            customizationAccessoryPage = category == Category.Characters
                ? Mathf.Clamp(customizationAccessoryPage, 0, optionPages - 1) : 0;
            RefreshAccessoryPager(customizationAccessoryPager, customizationAccessoryPageText,
                customizationAccessoryPage, category == Category.Characters ? count : 0, optionPageSize);
            for (int i = 0; i < (optionButtons != null ? optionButtons.Length : 0); i++)
            {
                int optionIndex = category == Category.Characters
                    ? customizationAccessoryPage * optionPageSize + i : i;
                bool visible = optionIndex < count;
                optionButtons[i].gameObject.SetActive(visible);
                if (!visible) continue;
                TryGetOption(optionIndex, out string id, out Sprite icon, out bool unlocked, out int gemPrice);
                bool isSelected = id == selected;
                DriverDefinition.CharacterAccessoryOption accessory = category == Category.Characters
                    ? SelectedDriver()?.accessories[optionIndex] : null;
                bool accessoryFlipX = accessory != null && accessory.supportsFlipVariant &&
                    SaveManager.GetCharacterAccessoryFlipX(accessory.accessoryId, accessory.flipX);
                optionButtons[i].interactable = vehicleUnlocked && (unlocked || gemPrice > 0);
                optionButtons[i].image.color = isSelected ? new Color(.18f, .60f, .82f, 1f) :
                    unlocked ? new Color(.10f, .20f, .32f, 1f) : new Color(.12f, .12f, .14f, .78f);
                RefreshAccessoryFlipButton(customizationAccessoryFlipButtons != null &&
                    i < customizationAccessoryFlipButtons.Length ? customizationAccessoryFlipButtons[i] : null,
                    accessory, vehicleUnlocked && unlocked);
                if (optionImages != null && i < optionImages.Length && optionImages[i] != null)
                {
                    optionImages[i].sprite = icon; optionImages[i].enabled = icon != null;
                    optionImages[i].preserveAspect = true;
                    Vector3 imageScale = optionImages[i].rectTransform.localScale;
                    float absoluteScaleX = Mathf.Abs(imageScale.x);
                    imageScale.x = (absoluteScaleX > .001f ? absoluteScaleX : 1f) *
                        (accessoryFlipX ? -1f : 1f);
                    optionImages[i].rectTransform.localScale = imageScale;
                    optionImages[i].material = accessory != null && accessory.animateRainbow
                        ? AccessoryRainbowEffect.GetMaterial(accessory.rainbowSpeed,
                            accessory.rainbowFrequency, accessory.rainbowNeonGlow, accessoryFlipX)
                        : category == Category.Driver &&
                        catalog?.drivers != null && optionIndex < catalog.drivers.Length
                            ? catalog.drivers[optionIndex]?.bodyMaterial : null;
                }
                if (optionLabels != null && i < optionLabels.Length && optionLabels[i] != null)
                {
                    string action = category == Category.Skin
                        ? isSelected ? "EQUIPPED" : unlocked ? "OWNED  •  READY" :
                            gemPrice > 0 ? $"{gemPrice:N0} GEMS" : "LOCKED"
                        : isSelected ? SelectedAccessoryAction(accessory) : unlocked
                            ? "SELECT" : gemPrice > 0 ? $"{gemPrice:N0} GEMS" : "LOCKED";
                    optionLabels[i].text = OptionName(optionIndex) + "\n" + action;
                }
                RefreshOptionStateVisual(i, optionIndex, unlocked, isSelected, vehicleUnlocked);
                RefreshOptionBlueprintProgress(i, optionIndex, unlocked);
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
                string orientation = selectedAccessory != null && selectedAccessory.supportsFlipVariant
                    ? " - " + AccessoryOrientation(selectedAccessory) : string.Empty;
                characterSummary.text = "CHARACTERS\n" +
                    (selectedAccessory?.displayName ?? "DEFAULT") + orientation;
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
            RefreshAdventureContext();
            RefreshSectionIsolation();
            RefreshGarageBackground();
            RefreshAudioSetting();
        }

        private void RefreshAdventureContext()
        {
            bool hasTrack = GameSession.TryResolvePendingAdventureTrack(catalog, out var pendingMap);
            VehicleDefinition vehicle = CurrentVehicle;
            bool vehicleUnlocked = vehicle != null && SaveManager.IsVehicleUnlocked(vehicle.vehicleId,
                vehicle.unlockedByDefault);
            bool showGarageContext = !IsIsolatedSection(section);
            bool showStart = showGarageContext && hasTrack && vehicleUnlocked;
            if (startButton != null)
            {
                startButton.gameObject.SetActive(showStart);
                startButton.interactable = showStart && !startInFlight;
            }
            if (pendingTrackLabel != null)
            {
                pendingTrackLabel.gameObject.SetActive(showGarageContext && hasTrack);
                if (hasTrack)
                    pendingTrackLabel.text = vehicleUnlocked
                        ? $"ADVENTURE TRACK  {pendingMap.displayName.ToUpperInvariant()}"
                        : "SELECT AN UNLOCKED VEHICLE";
            }
        }

        private void SelectGarageBackground(string backgroundId)
        {
            if (!Application.isPlaying) return;
            if (backgroundId != DefaultBackgroundId && backgroundId != NeonBackgroundId &&
                backgroundId != AnimeBackgroundId) backgroundId = DefaultBackgroundId;
            SaveManager.SelectedGarageBackgroundId = backgroundId;
            RefreshGarageBackground();
        }

        private void RefreshSectionIsolation()
        {
            if (!Application.isPlaying) return;
            bool showGarageChrome = !IsIsolatedSection(section);
            bool adventurePreparation = GameSession.HasPendingAdventureTrack;

            ResolveGarageSectionTabs();
            for (int i = 0; i < (garageSectionTabs?.Length ?? 0); i++)
                if (garageSectionTabs[i] != null)
                    garageSectionTabs[i].SetActive(showGarageChrome && !adventurePreparation &&
                        section != GarageSection.Settings);

            if (preview != null && preview.TargetImage != null)
            {
                bool wasVisible = preview.TargetImage.gameObject.activeSelf;
                preview.TargetImage.gameObject.SetActive(showGarageChrome);
                if (showGarageChrome && !wasVisible) preview.RefreshNow();
            }
            SetGraphicActive(vehicleName, showGarageChrome);
            SetGraphicActive(sectionTitle, showGarageChrome && !adventurePreparation);
            SetGraphicActive(skinSummary, showGarageChrome && !adventurePreparation);
            SetGraphicActive(wheelSummary, showGarageChrome && !adventurePreparation);
            SetGraphicActive(driverSummary, showGarageChrome && !adventurePreparation);
            SetGraphicActive(characterSummary, showGarageChrome && !adventurePreparation);
            if (previousVehicleButton != null)
                previousVehicleButton.gameObject.SetActive(showGarageChrome);
            if (nextVehicleButton != null)
                nextVehicleButton.gameObject.SetActive(showGarageChrome);

            Button paintButton = ResolveAdventurePaintButton();
            if (paintButton != null)
                paintButton.gameObject.SetActive(showGarageChrome && !adventurePreparation &&
                    section != GarageSection.Settings);

            if (!showGarageChrome && storePanel != null && storePanel.transform is RectTransform storeRect)
            {
                storeRect.anchorMin = new Vector2(.06f, .15f);
                storeRect.anchorMax = new Vector2(.94f, .84f);
                storeRect.anchoredPosition = Vector2.zero;
                storeRect.sizeDelta = Vector2.zero;
            }

            if (section == GarageSection.Settings && settingsPanel != null &&
                settingsPanel.transform is RectTransform settingsRect)
            {
                settingsRect.anchorMin = new Vector2(.18f, .15f);
                settingsRect.anchorMax = new Vector2(.82f, .84f);
                settingsRect.anchoredPosition = Vector2.zero;
                settingsRect.sizeDelta = Vector2.zero;
            }
        }

        private static bool IsIsolatedSection(GarageSection candidate) =>
            candidate == GarageSection.Store || candidate == GarageSection.Settings;

        private void ResolveGarageSectionTabs()
        {
            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded) return;
            if (garageSectionTabs != null && garageSectionTabs.Length == 4) return;
            string[] names =
            {
                "Garage Vehicle Tab",
                "Garage Tunes Tab",
                "Garage Paint Tab",
                "Garage Stats Tab"
            };
            garageSectionTabs = new GameObject[names.Length];
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform[] descendants = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (int descendantIndex = 0; descendantIndex < descendants.Length; descendantIndex++)
                    for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                        if (garageSectionTabs[nameIndex] == null &&
                            descendants[descendantIndex].name == names[nameIndex])
                            garageSectionTabs[nameIndex] = descendants[descendantIndex].gameObject;
            }
        }

        private Button ResolveAdventurePaintButton()
        {
            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded) return null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform[] descendants = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (int descendantIndex = 0; descendantIndex < descendants.Length; descendantIndex++)
                    if (descendants[descendantIndex].name == "Paint and Skins")
                        return descendants[descendantIndex].GetComponent<Button>();
            }

            return null;
        }

        private static void SetGraphicActive(Graphic graphic, bool active)
        {
            if (graphic != null) graphic.gameObject.SetActive(active);
        }

        private void ResetStoreScrollToTop()
        {
            ScrollRect scroll = storePanel != null ? storePanel.GetComponent<ScrollRect>() : null;
            if (scroll == null) return;
            Canvas.ForceUpdateCanvases();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.verticalNormalizedPosition = 1f;
        }

        private void SelectMenuBackground(string backgroundId)
        {
            if (!Application.isPlaying) return;
            if (backgroundId != DefaultBackgroundId && backgroundId != NeonBackgroundId)
                backgroundId = DefaultBackgroundId;
            SaveManager.SelectedMenuBackgroundId = backgroundId;
            RefreshGarageBackground();
        }

        private void RefreshGarageBackground()
        {
            string storedId = Application.isPlaying
                ? SaveManager.SelectedGarageBackgroundId : DefaultBackgroundId;
            string selectedId = storedId == NeonBackgroundId || storedId == AnimeBackgroundId ||
                storedId == DefaultBackgroundId ? storedId : DefaultBackgroundId;
            Sprite selectedGarageSprite = selectedId == NeonBackgroundId ? neonGarageBackground :
                selectedId == AnimeBackgroundId ? animeGarageBackground : defaultGarageBackground;
            if (selectedGarageSprite == null)
            {
                selectedId = DefaultBackgroundId;
                selectedGarageSprite = defaultGarageBackground ?? neonGarageBackground ?? animeGarageBackground;
            }
            if (Application.isPlaying && selectedId != storedId)
                SaveManager.SelectedGarageBackgroundId = selectedId;

            string storedMenuId = Application.isPlaying
                ? SaveManager.SelectedMenuBackgroundId : DefaultBackgroundId;
            string selectedMenuId = storedMenuId == NeonBackgroundId || storedMenuId == DefaultBackgroundId
                ? storedMenuId : DefaultBackgroundId;
            Sprite selectedMenuSprite = selectedMenuId == NeonBackgroundId
                ? neonMenuBackground : defaultMenuBackground;
            if (selectedMenuSprite == null)
            {
                selectedMenuId = DefaultBackgroundId;
                selectedMenuSprite = defaultMenuBackground ?? neonMenuBackground;
            }
            if (Application.isPlaying && selectedMenuId != storedMenuId)
                SaveManager.SelectedMenuBackgroundId = selectedMenuId;

            Sprite selectedSprite = IsIsolatedSection(section)
                ? selectedMenuSprite : selectedGarageSprite;
            if (garageBackground != null)
            {
                garageBackground.sprite = selectedSprite;
                garageBackground.enabled = selectedSprite != null;
                AspectRatioFitter fitter = garageBackground.GetComponent<AspectRatioFitter>();
                if (fitter != null && selectedSprite != null && selectedSprite.rect.height > 0f)
                    fitter.aspectRatio = selectedSprite.rect.width / selectedSprite.rect.height;
            }

            // Shop now uses the same full-screen background as Adventure. Keep the old nested
            // image disabled so it cannot add a second, independently tinted background layer.
            if (shopMenuBackground != null)
            {
                shopMenuBackground.sprite = selectedMenuSprite;
                shopMenuBackground.enabled = false;
                shopMenuBackground.raycastTarget = false;
            }

            for (int i = 0; i < (backgroundButtons?.Length ?? 0); i++)
            {
                Button button = backgroundButtons[i];
                if (button == null) continue;
                button.gameObject.SetActive(true);
                string buttonId = i == 1 ? NeonBackgroundId : i == 2 ? AnimeBackgroundId : DefaultBackgroundId;
                bool isSelected = buttonId == selectedId;
                // Keep the authored thumbnail colors intact. Selection is communicated by a
                // dedicated badge and outline instead of multiplying the image by a dark tint.
                if (button.image != null) button.image.color = Color.white;
                Outline outline = button.GetComponent<Outline>();
                if (outline != null) outline.enabled = isSelected;
                Transform marker = button.transform.Find("Selected Marker");
                if (marker != null) marker.gameObject.SetActive(isSelected);
            }

            for (int i = 0; i < (menuBackgroundButtons?.Length ?? 0); i++)
            {
                Button button = menuBackgroundButtons[i];
                if (button == null) continue;
                bool isSelected = (i == 1 ? NeonBackgroundId : DefaultBackgroundId) == selectedMenuId;
                if (button.image != null) button.image.color = Color.white;
                Outline outline = button.GetComponent<Outline>();
                if (outline != null) outline.enabled = isSelected;
                Transform marker = button.transform.Find("Selected Marker");
                if (marker != null) marker.gameObject.SetActive(isSelected);
            }
        }

        private void RefreshVehiclePresentationLayout()
        {
            // The scene builder owns the serialized neutral Vehicle layout. GarageUI is
            // ExecuteAlways, so applying a stale GameSession section while F8 rebuilds the scene
            // would otherwise move the preview left immediately before the scene is saved.
            if (!Application.isPlaying) return;
            bool centered = section == GarageSection.Vehicle;
            bool adventureTunes = GameSession.HasPendingAdventureTrack &&
                section == GarageSection.Tuning;
            RectTransform previewRect = preview != null && preview.TargetImage != null
                ? preview.TargetImage.rectTransform : null;
            SetStretchRect(previewRect,
                adventureTunes ? new Vector2(.24f, .10f) :
                    centered ? new Vector2(.16f, .26f) : new Vector2(.08f, .08f),
                adventureTunes ? new Vector2(.76f, .44f) :
                    centered ? new Vector2(.84f, .70f) : new Vector2(.55f, .42f));
            SetStretchRect(vehicleName != null ? vehicleName.rectTransform : null,
                adventureTunes ? new Vector2(.24f, .44f) :
                    centered ? new Vector2(.22f, .70f) : new Vector2(.08f, .43f),
                adventureTunes ? new Vector2(.76f, .50f) :
                    centered ? new Vector2(.78f, .78f) : new Vector2(.55f, .49f));

            Text[] summaries = { skinSummary, wheelSummary, driverSummary, characterSummary };
            for (int i = 0; i < summaries.Length; i++)
            {
                float minimumX = centered ? .18f + i * .16f : .04f + i * .11f;
                SetStretchRect(summaries[i] != null ? summaries[i].rectTransform : null,
                    new Vector2(minimumX, centered ? .13f : .12f),
                    new Vector2(minimumX + (centered ? .16f : .105f), .23f));
            }

            SetAnchoredButton(previousVehicleButton,
                adventureTunes ? new Vector2(-520f, -125f) :
                    centered ? new Vector2(-690f, -80f) : new Vector2(-840f, -120f));
            SetAnchoredButton(nextVehicleButton,
                adventureTunes ? new Vector2(520f, -125f) :
                    centered ? new Vector2(690f, -80f) : new Vector2(130f, -120f));

            Button paintButton = ResolveAdventurePaintButton();
            if (paintButton != null && paintButton.transform is RectTransform paintRect)
            {
                paintRect.anchorMin = paintRect.anchorMax = adventureTunes
                    ? new Vector2(.76f, .43f)
                    : centered ? new Vector2(.80f, .68f) : new Vector2(.50f, .43f);
                paintRect.anchoredPosition = Vector2.zero;
                paintRect.sizeDelta = new Vector2(132f, 98f);
            }
        }

        private static void SetStretchRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void SetAnchoredButton(Button button, Vector2 position)
        {
            if (button == null) return;
            RectTransform rect = button.transform as RectTransform;
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(76f, 100f);
        }

        private void RefreshAudioSetting()
        {
            bool enabled = SaveManager.MasterAudioEnabled;
            if (masterAudioToggle != null && masterAudioToggle.isOn != enabled)
            {
                updatingAudioToggle = true;
                masterAudioToggle.SetIsOnWithoutNotify(enabled);
                updatingAudioToggle = false;
            }
            if (masterAudioStatus != null)
            {
                masterAudioStatus.text = enabled ? "AUDIO ON" : "AUDIO OFF";
                masterAudioStatus.color = enabled
                    ? new Color(.30f, 1f, .58f, 1f) : new Color(1f, .30f, .24f, 1f);
            }
        }

        private void RefreshEconomyOnly(bool coinsChanged, bool gemsChanged)
        {
            if (coinsChanged && tuningCoins != null)
                tuningCoins.text = $"COINS  {lastObservedCoins:N0}";
            if (gemsChanged && gemsBalance != null)
                gemsBalance.text = $"GEMS  {lastObservedGems:N0}";

            VehicleDefinition vehicle = CurrentVehicle;
            if (coinsChanged && section == GarageSection.Tuning && vehicle != null)
                RefreshTuning(vehicle);
            if ((coinsChanged || gemsChanged) && section == GarageSection.Store)
                RefreshBlueprintShop();
            if (confirmationModal != null && confirmationModal.activeInHierarchy)
            {
                if (coinsChanged && pendingPurchaseKind == PurchaseKind.Tune)
                    RefreshPendingTuneModalState();
                else if ((coinsChanged && (pendingPurchaseKind == PurchaseKind.Vehicle ||
                              pendingPurchaseKind == PurchaseKind.Blueprints)) ||
                         (gemsChanged && pendingPurchaseKind == PurchaseKind.Customization))
                    RefreshOpenPurchaseAffordability();
            }
        }

        private void RefreshOpenPurchaseAffordability()
        {
            if (pendingPurchaseKind == PurchaseKind.Vehicle)
            {
                VehicleDefinition vehicle = CurrentVehicle;
                bool stateMatches = vehicle != null && vehicle.vehicleId == pendingVehicleId &&
                    !IsVehicleUnlocked(vehicle) && vehicle.coinPrice > 0;
                bool affordable = stateMatches && SaveManager.TotalCoins >= vehicle.coinPrice;
                ConfigureNonTuneConfirmation(true, affordable);
                if (confirmationCost != null && vehicle != null)
                    confirmationCost.text = confirmationCoinIcon != null ? vehicle.coinPrice.ToString("N0") :
                        $"COST  {vehicle.coinPrice:N0} COINS";
                if (confirmationStatus != null)
                    confirmationStatus.text = !stateMatches ? "UNAVAILABLE" :
                        affordable ? string.Empty : "NOT ENOUGH COINS";
                if (confirmationYes != null) confirmationYes.interactable = !purchaseInFlight && affordable;
                return;
            }
            if (pendingPurchaseKind == PurchaseKind.Blueprints)
            {
                int price = BlueprintPackCatalog.GetCoinPrice(pendingBlueprintTier);
                BundleDefinition bundle = PremiumBundleCatalog.Find(catalog, pendingBundleId);
                bool available = bundle != null && !SaveManager.IsBundleUnlocked(bundle) && price >= 0;
                bool affordable = available && SaveManager.TotalCoins >= price;
                ConfigureNonTuneConfirmation(true, affordable);
                if (confirmationCost != null)
                    confirmationCost.text = confirmationCoinIcon != null ? price.ToString("N0") :
                        $"COST  {price:N0} COINS";
                if (confirmationStatus != null)
                    confirmationStatus.text = !available ? "UNAVAILABLE" :
                        affordable ? string.Empty : "NOT ENOUGH COINS";
                if (confirmationYes != null) confirmationYes.interactable = !purchaseInFlight && affordable;
                return;
            }

            if (pendingPurchaseKind == PurchaseKind.Customization)
            {
                bool stateMatches = pendingCategory == Category.Characters ||
                    (CurrentVehicle != null && CurrentVehicle.vehicleId == pendingVehicleId);
                bool affordable = stateMatches && pendingGemPrice >= 0 &&
                    SaveManager.TotalGems >= pendingGemPrice;
                ConfigureNonTuneConfirmation(false, affordable);
                if (confirmationStatus != null)
                    confirmationStatus.text = !stateMatches ? "CUSTOMIZATION STATE CHANGED" :
                        affordable ? string.Empty : "NOT ENOUGH GEMS";
                if (confirmationYes != null) confirmationYes.interactable = !purchaseInFlight && affordable;
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
            starterPackShop?.Refresh();
        }

        private void RefreshShopAccessories()
        {
            DriverDefinition driver = SelectedDriver();
            int count = driver?.accessories?.Length ?? 0;
            int pageSize = Mathf.Max(1, shopAccessoryButtons?.Length ?? 0);
            int pages = Mathf.Max(1, Mathf.CeilToInt(count / (float)pageSize));
            shopAccessoryPage = Mathf.Clamp(shopAccessoryPage, 0, pages - 1);
            RefreshAccessoryPager(shopAccessoryPager, shopAccessoryPageText, shopAccessoryPage, count, pageSize);
            for (int i = 0; i < (shopAccessoryButtons?.Length ?? 0); i++)
            {
                int accessoryIndex = shopAccessoryPage * pageSize + i;
                bool visible = accessoryIndex < count && driver.accessories[accessoryIndex] != null;
                Button button = shopAccessoryButtons[i];
                if (button == null) continue;
                button.gameObject.SetActive(visible);
                if (!visible) continue;
                DriverDefinition.CharacterAccessoryOption option = driver.accessories[accessoryIndex];
                bool unlocked = IsAccessoryUnlocked(option);
                bool selected = option.accessoryId == SaveManager.GetSelectedCharacterAccessoryId(driver.DefaultAccessoryId);
                bool isDefault = option.accessoryId == driver.DefaultAccessoryId;
                ItemRarity rarity = option.ResolveRarity(isDefault);
                SetStateImage(shopAccessoryRarityFrames != null && i < shopAccessoryRarityFrames.Length
                        ? shopAccessoryRarityFrames[i] : null,
                    HillAdventurerUiCatalog.Instance?.GetRarityFrame(rarity), true, string.Empty,
                    ItemRarityUtility.AccentColor(rarity));
                bool flipX = option.supportsFlipVariant &&
                    SaveManager.GetCharacterAccessoryFlipX(option.accessoryId, option.flipX);
                button.interactable = unlocked || option.gemPrice > 0;
                button.image.color = selected ? new Color(.18f, .60f, .82f, 1f) :
                    unlocked ? new Color(.10f, .30f, .24f, .96f) : new Color(.12f, .12f, .14f, .84f);
                RefreshAccessoryFlipButton(shopAccessoryFlipButtons != null &&
                    i < shopAccessoryFlipButtons.Length ? shopAccessoryFlipButtons[i] : null,
                    option, unlocked);
                if (shopAccessoryImages != null && i < shopAccessoryImages.Length && shopAccessoryImages[i] != null)
                {
                    shopAccessoryImages[i].sprite = option.sprite;
                    shopAccessoryImages[i].enabled = option.sprite != null;
                    shopAccessoryImages[i].preserveAspect = true;
                    Vector3 imageScale = shopAccessoryImages[i].rectTransform.localScale;
                    float absoluteScaleX = Mathf.Abs(imageScale.x);
                    imageScale.x = (absoluteScaleX > .001f ? absoluteScaleX : 1f) *
                        (flipX ? -1f : 1f);
                    shopAccessoryImages[i].rectTransform.localScale = imageScale;
                    shopAccessoryImages[i].material = option.animateRainbow
                        ? AccessoryRainbowEffect.GetMaterial(option.rainbowSpeed, option.rainbowFrequency,
                            option.rainbowNeonGlow, flipX) : null;
                }
                if (shopAccessoryLabels != null && i < shopAccessoryLabels.Length && shopAccessoryLabels[i] != null)
                    shopAccessoryLabels[i].text = option.displayName.ToUpperInvariant() + "\n" +
                        (selected ? SelectedAccessoryAction(option, "EQUIPPED") : unlocked ? "EQUIP" :
                            option.gemPrice > 0 ? $"{option.gemPrice:N0} GEMS" : "LOCKED");
            }
        }

        private void SelectShopAccessory(int slot)
        {
            DriverDefinition driver = SelectedDriver();
            int index = shopAccessoryPage * Mathf.Max(1, shopAccessoryButtons?.Length ?? 0) + slot;
            if (driver?.accessories == null || index < 0 || index >= driver.accessories.Length) return;
            DriverDefinition.CharacterAccessoryOption option = driver.accessories[index];
            if (option == null) return;
            if (IsAccessoryUnlocked(option))
            {
                SaveManager.SetSelectedCharacterAccessoryId(option.accessoryId);
                preview?.Invalidate();
                Refresh();
                return;
            }

            if (option.gemPrice <= 0) return;
            pendingPurchaseKind = PurchaseKind.Customization;
            pendingCategory = Category.Characters;
            pendingVehicleId = CurrentVehicle != null ? CurrentVehicle.vehicleId : string.Empty;
            pendingOptionId = option.accessoryId;
            pendingGemPrice = option.gemPrice;
            if (confirmationTitle != null)
                confirmationTitle.text = $"BUY {option.displayName.ToUpperInvariant()}?";
            if (confirmationValues != null)
                confirmationValues.text = "PERMANENT CHARACTER ACCESSORY UNLOCK";
            if (confirmationCost != null)
                confirmationCost.text = $"COST  {option.gemPrice:N0} GEMS";
            bool affordable = SaveManager.TotalGems >= option.gemPrice;
            ConfigureNonTuneConfirmation(false, affordable);
            if (confirmationStatus != null)
                confirmationStatus.text = affordable ? string.Empty : "NOT ENOUGH GEMS";
            if (confirmationYes != null) confirmationYes.interactable = affordable;
            SetConfirmationActionLabel("BUY");
            if (confirmationModal != null) confirmationModal.SetActive(true);
        }

        private void SelectBlueprintPack(BlueprintPackTier tier)
        {
            if (!BlueprintPackCatalog.IsValid(tier)) return;
            selectedBlueprintTier = tier;
            RefreshBlueprintShop();
        }

        private void RefreshOptionBlueprintProgress(int slot, int optionIndex, bool unlocked)
        {
            Image fill = optionBlueprintFills != null && slot < optionBlueprintFills.Length
                ? optionBlueprintFills[slot] : null;
            Text progressText = optionBlueprintTexts != null && slot < optionBlueprintTexts.Length
                ? optionBlueprintTexts[slot] : null;
            GameObject bar = fill != null && fill.transform.parent != null ? fill.transform.parent.gameObject : null;
            BundleDefinition bundle = GetOptionBundle(optionIndex);
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
            int coins = SaveManager.TotalCoins;
            bool vehicleUnlocked = IsVehicleUnlocked(vehicle);
            if (tuningCoins != null) tuningCoins.text = $"COINS  {coins:N0}";
            int count = vehicle.GetTuneSlotCount();
            CenterVisibleTuneCards(count);
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
                bool insufficient = !canEquipUp && !purchasedMax && cost >= 0 && coins < cost;
                bool blocked = !vehicleUnlocked || insufficient;
                if (tuningNames != null && i < tuningNames.Length) tuningNames[i].text = vehicle.GetTuneDisplayName(type);
                if (tuningLevels != null && i < tuningLevels.Length)
                    tuningLevels[i].text = $"{current} / {maximum}\nLEVEL {current}";
                var next = new VehicleUpgradeState();
                next.CopyFrom(state);
                next.SetTune(type, current + (current < maximum ? 1 : 0));
                next.ClampTo(vehicle);
                if (tuningValues != null && i < tuningValues.Length)
                    tuningValues[i].text = current >= maximum
                        ? $"CURRENT  {TuneValue(vehicle, state, type)}\nNEXT  MAX"
                        : $"CURRENT  {TuneValue(vehicle, state, type)}\nNEXT  {TuneValue(vehicle, next, type)}";
                if (tuningCosts != null && i < tuningCosts.Length)
                {
                    tuningCosts[i].text = !vehicleUnlocked ? "LOCKED" :
                        canEquipUp ? "EQUIP" : purchasedMax ? "MAX" :
                        cost < 0 ? "UNAVAILABLE" : cost.ToString("N0");
                    tuningCosts[i].color = insufficient ? new Color(1f, .20f, .16f, 1f) :
                        vehicleUnlocked ? Color.white : new Color(.70f, .72f, .76f, 1f);
                }
                if (tuningIcons != null && i < tuningIcons.Length && tuningIcons[i] != null)
                {
                    tuningIcons[i].sprite = ResolveTuneIcon(vehicle, type);
                    tuningIcons[i].enabled = tuningIcons[i].sprite != null;
                    tuningIcons[i].preserveAspect = true;
                }
                if (tuningCoinIcons != null && i < tuningCoinIcons.Length && tuningCoinIcons[i] != null)
                {
                    tuningCoinIcons[i].enabled = vehicleUnlocked && !canEquipUp && !purchasedMax && cost >= 0;
                    tuningCoinIcons[i].color = insufficient
                        ? new Color(1f, .20f, .16f, 1f) : Color.white;
                }
                Image cardImage = row != null ? row.GetComponent<Image>() : null;
                if (cardImage != null)
                {
                    Sprite stateSprite = blocked ? tuneUnaffordableBackground : tuneAffordableBackground;
                    if (stateSprite != null)
                    {
                        cardImage.sprite = stateSprite;
                        cardImage.type = Image.Type.Simple;
                        cardImage.preserveAspect = false;
                    }
                    cardImage.color = blocked && stateSprite == null
                        ? new Color(.72f, .08f, .06f, 1f) : Color.white;
                    cardImage.raycastTarget = tuningCardButtons != null &&
                        i < tuningCardButtons.Length && tuningCardButtons[i] != null;
                }
                Image priceHitTarget = tuningButtons[i].image;
                if (priceHitTarget != null)
                {
                    // The supplied state sprite is a complete portrait card, including its price
                    // strip. Keep the actual button transparent over that strip as the hit target.
                    priceHitTarget.sprite = null;
                    priceHitTarget.color = Color.clear;
                }
                // Keep the price strip clickable even when unaffordable or at MAX so it can open
                // the same explanatory modal as the rest of the card; the modal disables purchase.
                tuningButtons[i].interactable = vehicleUnlocked && maximum > 0;
                if (tuningCardButtons != null && i < tuningCardButtons.Length &&
                    tuningCardButtons[i] != null)
                    tuningCardButtons[i].interactable = vehicleUnlocked && maximum > 0;
                if (tuningDownButtons != null && i < tuningDownButtons.Length && tuningDownButtons[i] != null)
                    tuningDownButtons[i].interactable = vehicleUnlocked && current > 1;
            }
        }

        private void CenterVisibleTuneCards(int visibleCount)
        {
            // The Hill Adventurer Garage presents tunes as one horizontal row. Vehicles expose
            // either five or six tune slots, so derive the first X from the visible count instead
            // of retaining the six-card positions after the last card is hidden.
            if (tuningIcons == null || tuningIcons.Length == 0 || tuningButtons == null) return;
            int count = Mathf.Clamp(visibleCount, 0, tuningButtons.Length);
            const float spacing = 240f;
            float firstX = -(count - 1) * spacing * .5f;
            int positioned = 0;
            for (int i = 0; i < tuningButtons.Length && positioned < count; i++)
            {
                Button button = tuningButtons[i];
                RectTransform row = button != null ? button.transform.parent as RectTransform : null;
                if (row == null) continue;
                row.anchorMin = row.anchorMax = new Vector2(.5f, .5f);
                row.anchoredPosition = new Vector2(firstX + positioned * spacing, row.anchoredPosition.y);
                positioned++;
            }
        }

        private static bool IsVehicleUnlocked(VehicleDefinition vehicle) => vehicle != null &&
            SaveManager.IsVehicleUnlocked(vehicle.vehicleId, vehicle.unlockedByDefault);

        private void SetConfirmationActionLabel(string value)
        {
            Text label = confirmationYes != null ? confirmationYes.GetComponentInChildren<Text>(true) : null;
            if (label != null) label.text = value;
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
                case VehicleUpgradeType.Turbo:
                    if (vehicle.archetype == VehicleArchetype.AerialSatelite)
                        return $"THRUST  {Mathf.Lerp(50f, 100f, VehicleDefinition.UpgradeProgress(state.turboTune, vehicle.turboMaxTunes)):0}%";
                    return vehicle.archetype == VehicleArchetype.Buggy
                        ? $"MAX SLOPE  {stats.maximumClimbAngle:0}°"
                        : $"0-100  {stats.zeroToHundredTargetSeconds:0.00}s";
                default: return $"RUN TIME  {stats.totalRunFuelDuration:0.0}s";
            }
        }

        private Sprite ResolveTuneIcon(VehicleDefinition vehicle, VehicleUpgradeType type)
        {
            switch (type)
            {
                case VehicleUpgradeType.Engine: return engineTuneIcon;
                case VehicleUpgradeType.Acceleration: return accelerationTuneIcon;
                case VehicleUpgradeType.Suspension: return suspensionTuneIcon;
                case VehicleUpgradeType.Grip: return gripTuneIcon;
                case VehicleUpgradeType.Fuel: return fuelTuneIcon;
                case VehicleUpgradeType.Downforce: return downforceTuneIcon;
                case VehicleUpgradeType.Turbo:
                    if (vehicle != null && vehicle.archetype == VehicleArchetype.AerialSatelite)
                        return thrustersTuneIcon != null ? thrustersTuneIcon : accelerationTuneIcon;
                    if (vehicle != null && vehicle.archetype == VehicleArchetype.Buggy)
                        return buggyInclinationTuneIcon != null
                            ? buggyInclinationTuneIcon : accelerationTuneIcon;
                    return accelerationTuneIcon;
                case VehicleUpgradeType.AirControl: return accelerationTuneIcon;
                default: return fuelTuneIcon;
            }
        }

        private static string TuneDescription(VehicleDefinition vehicle, VehicleUpgradeType type)
        {
            switch (type)
            {
                case VehicleUpgradeType.Engine:
                    return "Increases tuned top speed and engine power.";
                case VehicleUpgradeType.Acceleration:
                    return "Improves how quickly the vehicle gains speed and reaches its acceleration target.";
                case VehicleUpgradeType.Suspension:
                    return "Increases suspension travel and improves damping for landings and rough terrain.";
                case VehicleUpgradeType.Grip:
                    return "Improves traction and recovery from sliding.";
                case VehicleUpgradeType.Fuel:
                    return "Increases total driving time available from a full fuel tank.";
                case VehicleUpgradeType.Downforce:
                    return "Adds airborne downforce to improve stability while the vehicle is off the ground.";
                case VehicleUpgradeType.Turbo:
                    if (vehicle != null && vehicle.archetype == VehicleArchetype.AerialSatelite)
                        return "Increases thruster force. Active thrusters consume fuel faster.";
                    if (vehicle != null && vehicle.archetype == VehicleArchetype.Buggy)
                        return "Improves steep-slope climb assistance and the maximum supported climb angle.";
                    return "Improves acceleration and recovery when applying power.";
                case VehicleUpgradeType.AirControl:
                    return "Improves airborne rotation control.";
                default:
                    return "Improves this vehicle's authored performance.";
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

        private static string AccessoryOrientation(DriverDefinition.CharacterAccessoryOption option) =>
            SaveManager.GetCharacterAccessoryFlipX(option.accessoryId, option.flipX)
                ? "BACKWARD" : "FORWARD";

        private static string SelectedAccessoryAction(DriverDefinition.CharacterAccessoryOption option,
            string selectedLabel = "SELECTED") => option != null && option.supportsFlipVariant
                ? selectedLabel + " - " + AccessoryOrientation(option)
                : selectedLabel;

        private int SelectedAccessoryIndex()
        {
            DriverDefinition driver = SelectedDriver();
            if (driver?.accessories == null) return -1;
            string selectedId = SaveManager.GetSelectedCharacterAccessoryId(driver.DefaultAccessoryId);
            for (int i = 0; i < driver.accessories.Length; i++)
                if (driver.accessories[i] != null && driver.accessories[i].accessoryId == selectedId)
                    return i;
            return -1;
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
            int firstValid = -1;
            int firstUnlocked = -1;
            for (int i = 0; i < catalog.vehicles.Length; i++)
            {
                VehicleDefinition candidate = catalog.vehicles[i];
                if (candidate == null) continue;
                if (firstValid < 0) firstValid = i;
                bool unlocked = SaveManager.IsVehicleUnlocked(candidate.vehicleId,
                    candidate.unlockedByDefault);
                if (unlocked && firstUnlocked < 0) firstUnlocked = i;
                if (unlocked && candidate.vehicleId == SaveManager.SelectedVehicleId)
                {
                    vehicleIndex = i;
                    return;
                }
            }

            // Prefer a real unlocked fallback. If the catalog is entirely locked, show its first
            // valid preview without writing that locked id into persistent selection.
            vehicleIndex = firstUnlocked >= 0 ? firstUnlocked : Mathf.Max(0, firstValid);
            if (Application.isPlaying && firstUnlocked >= 0)
                SaveManager.SelectedVehicleId = catalog.vehicles[firstUnlocked].vehicleId;
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

        public void AssignHillAdventurer(HillAdventurerGarageBindings bindings)
        {
            if (bindings == null) return;
            startButton = bindings.startButton;
            pendingTrackLabel = bindings.pendingTrackLabel;
            topNavigation = bindings.topNavigation;
            garageBackground = bindings.garageBackground;
            defaultGarageBackground = bindings.defaultGarageBackground;
            neonGarageBackground = bindings.neonGarageBackground;
            animeGarageBackground = bindings.animeGarageBackground;
            backgroundButtons = bindings.backgroundButtons;
            shopMenuBackground = bindings.shopMenuBackground;
            defaultMenuBackground = bindings.defaultMenuBackground;
            neonMenuBackground = bindings.neonMenuBackground;
            menuBackgroundButtons = bindings.menuBackgroundButtons;
            previousVehicleButton = bindings.previousVehicleButton;
            nextVehicleButton = bindings.nextVehicleButton;
            masterAudioToggle = bindings.masterAudioToggle;
            masterAudioStatus = bindings.masterAudioStatus;
            tuningIcons = bindings.tuneIcons;
            tuningCoinIcons = bindings.tuneCoinIcons;
            tuningCardButtons = bindings.tuneCardButtons;
            engineTuneIcon = bindings.engineTuneIcon;
            accelerationTuneIcon = bindings.accelerationTuneIcon;
            suspensionTuneIcon = bindings.suspensionTuneIcon;
            gripTuneIcon = bindings.gripTuneIcon;
            fuelTuneIcon = bindings.fuelTuneIcon;
            downforceTuneIcon = bindings.downforceTuneIcon;
            thrustersTuneIcon = bindings.thrustersTuneIcon;
            buggyInclinationTuneIcon = bindings.buggyInclinationTuneIcon;
            tuneAffordableBackground = bindings.tuneAffordableBackground;
            tuneUnaffordableBackground = bindings.tuneUnaffordableBackground;
            confirmationIcon = bindings.confirmationIcon;
            confirmationCoinIcon = bindings.confirmationCoinIcon;
            confirmationPanel = bindings.confirmationPanel;
            confirmationDescription = bindings.confirmationDescription;
            if (confirmationPanel != null)
                confirmationPanel.color = new Color(.22f, .23f, .26f, .98f);
            Refresh();
        }
    }
}
