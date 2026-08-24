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

        public Toggle masterAudioToggle;
        public Text masterAudioStatus;

        public Image[] tuneIcons;
        public Image[] tuneCoinIcons;
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
        [SerializeField] private Button startButton;
        [SerializeField] private Text pendingTrackLabel;
        [SerializeField] private TopNavigationUI topNavigation;
        [SerializeField] private Image garageBackground;
        [SerializeField] private Sprite defaultGarageBackground;
        [SerializeField] private Sprite neonGarageBackground;
        [SerializeField] private Sprite animeGarageBackground;
        [SerializeField] private Button[] backgroundButtons;
        [SerializeField] private Toggle masterAudioToggle;
        [SerializeField] private Text masterAudioStatus;
        [SerializeField] private Image[] tuningIcons;
        [SerializeField] private Image[] tuningCoinIcons;
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

        private const string DefaultBackgroundId = "default";
        private const string NeonBackgroundId = "neon";
        private const string AnimeBackgroundId = "anime";

        private VehicleDefinition CurrentVehicle => catalog != null && catalog.vehicles != null && catalog.vehicles.Length > 0
            ? catalog.vehicles[Mathf.Clamp(vehicleIndex, 0, catalog.vehicles.Length - 1)] : null;

        private void OnEnable()
        {
            if (catalog == null) return;
            startInFlight = false;
            purchaseInFlight = false;
            lastPurchaseAttemptFrame = -1;
            lastTuneCardFrame = -1;
            lastTuneCardSlot = -1;
            nextEconomyCheck = 0f;
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
        public void ShowTuning() => SetSection(GarageSection.Tuning);
        public void ShowVehicle() => SetSection(GarageSection.Vehicle);
        public void ShowUpgrade() => SetSection(GarageSection.Upgrade);
        public void ShowStore() => SetSection(GarageSection.Store);
        public void ShowSettings() => SetSection(GarageSection.Settings);
        public void ShowSkins() { category = Category.Skin; SetSection(GarageSection.Skins); }
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
            section = targetSection;
            GameSession.RequestedGarageSection = targetSection;
            Refresh();
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
            if (vehicle == null || !IsVehicleUnlocked(vehicle) || slot < 0 ||
                slot >= vehicle.GetTuneSlotCount() || catalog?.upgradeEconomy == null) return;
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

        private void SelectOption(int index)
        {
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
            if (sectionTitle != null) sectionTitle.text = section.ToString().ToUpperInvariant();
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
                optionButtons[i].interactable = vehicleUnlocked && (unlocked || gemPrice > 0);
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
            RefreshAdventureContext();
            RefreshGarageBackground();
            RefreshAudioSetting();
        }

        private void RefreshAdventureContext()
        {
            bool hasTrack = GameSession.TryResolvePendingAdventureTrack(catalog, out var pendingMap);
            VehicleDefinition vehicle = CurrentVehicle;
            bool vehicleUnlocked = vehicle != null && SaveManager.IsVehicleUnlocked(vehicle.vehicleId,
                vehicle.unlockedByDefault);
            bool showStart = hasTrack && vehicleUnlocked;
            if (startButton != null)
            {
                startButton.gameObject.SetActive(showStart);
                startButton.interactable = showStart && !startInFlight;
            }
            if (pendingTrackLabel != null)
            {
                pendingTrackLabel.gameObject.SetActive(hasTrack);
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

        private void RefreshGarageBackground()
        {
            string storedId = SaveManager.SelectedGarageBackgroundId;
            string selectedId = storedId == NeonBackgroundId || storedId == AnimeBackgroundId ||
                storedId == DefaultBackgroundId ? storedId : DefaultBackgroundId;
            Sprite selectedSprite = selectedId == NeonBackgroundId ? neonGarageBackground :
                selectedId == AnimeBackgroundId ? animeGarageBackground : defaultGarageBackground;
            if (selectedSprite == null)
            {
                selectedId = DefaultBackgroundId;
                selectedSprite = defaultGarageBackground ?? neonGarageBackground ?? animeGarageBackground;
            }
            if (Application.isPlaying && selectedId != storedId)
                SaveManager.SelectedGarageBackgroundId = selectedId;
            if (garageBackground != null)
            {
                garageBackground.sprite = selectedSprite;
                garageBackground.enabled = selectedSprite != null;
                AspectRatioFitter fitter = garageBackground.GetComponent<AspectRatioFitter>();
                if (fitter != null && selectedSprite != null && selectedSprite.rect.height > 0f)
                    fitter.aspectRatio = selectedSprite.rect.width / selectedSprite.rect.height;
            }

            for (int i = 0; i < (backgroundButtons?.Length ?? 0); i++)
            {
                Button button = backgroundButtons[i];
                if (button == null) continue;
                button.gameObject.SetActive(true);
                string buttonId = i == 1 ? NeonBackgroundId : i == 2 ? AnimeBackgroundId : DefaultBackgroundId;
                if (button.image != null)
                    button.image.color = buttonId == selectedId
                        ? new Color(.14f, .72f, .92f, 1f)
                        : new Color(.22f, .24f, .28f, .96f);
            }
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
                else if ((coinsChanged && pendingPurchaseKind == PurchaseKind.Blueprints) ||
                         (gemsChanged && pendingPurchaseKind == PurchaseKind.Customization))
                    RefreshOpenPurchaseAffordability();
            }
        }

        private void RefreshOpenPurchaseAffordability()
        {
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
            if (option == null) return;
            if (IsAccessoryUnlocked(option))
            {
                SaveManager.SetSelectedCharacterAccessoryId(option.accessoryId);
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
            int coins = SaveManager.TotalCoins;
            bool vehicleUnlocked = IsVehicleUnlocked(vehicle);
            if (tuningCoins != null) tuningCoins.text = $"COINS  {coins:N0}";
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
                    cardImage.raycastTarget = false;
                }
                Image priceHitTarget = tuningButtons[i].image;
                if (priceHitTarget != null)
                {
                    // The supplied state sprite is a complete portrait card, including its price
                    // strip. Keep the actual button transparent over that strip as the hit target.
                    priceHitTarget.sprite = null;
                    priceHitTarget.color = Color.clear;
                }
                tuningButtons[i].interactable = vehicleUnlocked && maximum > 0 &&
                    (canEquipUp || (!purchasedMax && cost >= 0 && !insufficient));
                if (tuningDownButtons != null && i < tuningDownButtons.Length && tuningDownButtons[i] != null)
                    tuningDownButtons[i].interactable = vehicleUnlocked && current > 1;
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
            masterAudioToggle = bindings.masterAudioToggle;
            masterAudioStatus = bindings.masterAudioStatus;
            tuningIcons = bindings.tuneIcons;
            tuningCoinIcons = bindings.tuneCoinIcons;
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
