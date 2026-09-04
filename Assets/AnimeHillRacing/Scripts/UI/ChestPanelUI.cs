using System;
using AnimeHillRacing.Chests;
using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class ChestPanelUI : MonoBehaviour
    {
        public const int SlotsPerPage = 3;

        [SerializeField] private GameCatalog catalog;
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private Image[] slotImages;
        [SerializeField] private Text[] slotLabels;
        [SerializeField] private Image progressFill;
        [SerializeField] private Text progressText;
        [SerializeField] private Text pageText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Image feedbackBundleImage;
        [SerializeField] private ChestRewardModalUI rewardModal;
        [SerializeField] private Button previousPageButton;
        [SerializeField] private Button nextPageButton;
        private Button contentsButton;
        private GameObject contentsIndexRoot;
        private Text contentsIndexText;
        private Button chestAdButton;
        private Button coinAdButton;
        private Text chestAdCounter;
        private Text coinAdCounter;
        private static readonly Color GreenProgressColor = new Color(0.16f, 0.86f, 0.32f, 1f);
        private float nextRefresh;
        private bool outlineConfigured;
        private bool adControlsConfigured;
        private bool adRequestInFlight;
        private int currentPage;

        public int CurrentPage => currentPage;
        public int PageCount => Mathf.Max(1, Mathf.CeilToInt(ChestSystem.Inventory.Count / (float)SlotsPerPage));

        private void Awake()
        {
            ConfigureVisualStyles();
            EnsureContentsIndexUI();
            EnsureRewardedAdControls();
        }

        private void OnEnable()
        {
            ConfigureVisualStyles();
            EnsureContentsIndexUI();
            EnsureRewardedAdControls();
            ChestSystem.RefreshOpeningStates(DateTime.UtcNow);
            ShowFirstUnseenChest();
            Refresh();
        }

        private void ConfigureVisualStyles()
        {
            if (progressFill != null)
                progressFill.color = GreenProgressColor;

            ConfigurePagedSlotLayout();

            if (!outlineConfigured && progressText != null)
            {
                outlineConfigured = true;
                if (progressText.GetComponent<Outline>() == null)
                {
                    Outline outline = progressText.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }
        }

        private void ConfigurePagedSlotLayout()
        {
            // The track-selection chest shelf is paged. Normalize legacy scenes that still serialize
            // four small slots into the current three large-slot layout at runtime.
            if (previousPageButton == null && nextPageButton == null) return;

            float[] slotX = { -400f, 0f, 400f };
            int count = slotButtons?.Length ?? 0;
            for (int i = 0; i < count; i++)
            {
                Button slot = slotButtons[i];
                if (slot == null) continue;
                bool used = i < SlotsPerPage;
                slot.gameObject.SetActive(used);
                if (!used) continue;

                RectTransform slotRect = slot.GetComponent<RectTransform>();
                if (slotRect != null)
                {
                    slotRect.anchoredPosition = new Vector2(slotX[i], 8f);
                    slotRect.sizeDelta = new Vector2(370f, 145f);
                }

                if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
                {
                    RectTransform imageRect = slotImages[i].rectTransform;
                    imageRect.anchorMin = new Vector2(.20f, .12f);
                    imageRect.anchorMax = new Vector2(.80f, .98f);
                    imageRect.offsetMin = Vector2.zero;
                    imageRect.offsetMax = Vector2.zero;
                }

                if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
                {
                    Text label = slotLabels[i];
                    RectTransform labelRect = label.rectTransform;
                    labelRect.anchorMin = new Vector2(0f, 0f);
                    labelRect.anchorMax = new Vector2(1f, 0f);
                    labelRect.pivot = new Vector2(.5f, 1f);
                    labelRect.offsetMin = new Vector2(8f, -62f);
                    labelRect.offsetMax = new Vector2(-8f, -4f);
                    label.alignment = TextAnchor.UpperCenter;
                    label.fontSize = 15;
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 11;
                    label.resizeTextMaxSize = 15;
                }
            }

            if (previousPageButton != null)
                previousPageButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-650f, 8f);
            if (nextPageButton != null)
                nextPageButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(650f, 8f);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .2f;
            Refresh();
        }

        public void SelectSlot0() => SelectSlot(0);
        public void SelectSlot1() => SelectSlot(1);
        public void SelectSlot2() => SelectSlot(2);
        // Kept only so legacy serialized Button events remain resolvable; slot four is always hidden.
        public void SelectSlot3() => SelectSlot(3);

        public void PreviousPage() => SetPage(currentPage - 1);
        public void NextPage() => SetPage(currentPage + 1);
        public void ShowContentsIndex()
        {
            EnsureContentsIndexUI();
            RefreshContentsIndex();
            if (contentsIndexRoot == null) return;
            contentsIndexRoot.SetActive(true);
            contentsIndexRoot.transform.SetAsLastSibling();
        }

        public void HideContentsIndex()
        {
            if (contentsIndexRoot != null) contentsIndexRoot.SetActive(false);
        }

        public void WatchCoinAd()
        {
            if (adRequestInFlight) return;
            RewardedAdQuota quota = ChestSystem.GetCoinAdQuota(DateTime.UtcNow);
            if (!quota.IsAvailable)
            {
                SetFeedback("COIN ADS RESET IN " + FormatQuotaRemaining(quota), null);
                return;
            }
            if (!RewardedAdService.IsReady)
            {
                SetFeedback("REWARDED AD IS NOT READY", null);
                return;
            }

            adRequestInFlight = true;
            RewardedAdService.Show(completed =>
            {
                adRequestInFlight = false;
                if (!completed)
                {
                    SetFeedback("AD WAS NOT COMPLETED", null);
                    Refresh();
                    return;
                }
                if (ChestSystem.TryClaimCoinAdReward(DateTime.UtcNow, out int coins,
                        out RewardedAdQuota updated))
                    SetFeedback($"+{coins:N0} COINS   ADS {updated.Used}/{updated.Limit}", null);
                else
                    SetFeedback("COIN ADS RESET IN " + FormatQuotaRemaining(updated), null);
                Refresh();
            });
        }

        public void WatchChestAd()
        {
            if (adRequestInFlight) return;
            ChestRecord opening = ChestSystem.GetOpeningChest(DateTime.UtcNow);
            if (opening == null)
            {
                SetFeedback("START OPENING A CHEST FIRST", null);
                return;
            }
            RewardedAdQuota quota = ChestSystem.GetChestAdQuota(DateTime.UtcNow);
            if (!quota.IsAvailable)
            {
                SetFeedback("CHEST ADS RESET IN " + FormatQuotaRemaining(quota), null);
                return;
            }
            if (!RewardedAdService.IsReady)
            {
                SetFeedback("REWARDED AD IS NOT READY", null);
                return;
            }

            adRequestInFlight = true;
            RewardedAdService.Show(completed =>
            {
                adRequestInFlight = false;
                if (!completed)
                {
                    SetFeedback("AD WAS NOT COMPLETED", null);
                    Refresh();
                    return;
                }
                if (ChestSystem.TryApplyAdToOpeningChest(DateTime.UtcNow,
                        out ChestAdProgressResult progress))
                {
                    string status = progress.ReadyToClaim
                        ? "READY TO CLAIM!"
                        : $"ADS {progress.CompletedViews}/{progress.RequiredViews}";
                    SetFeedback($"{progress.Type.ToString().ToUpperInvariant()} CHEST  {status}", null);
                }
                else
                    SetFeedback(progress.Quota.IsAvailable ? "NO CHEST IS OPENING" :
                        "CHEST ADS RESET IN " + FormatQuotaRemaining(progress.Quota), null);
                Refresh();
            });
        }

        public void SetPage(int page)
        {
            currentPage = Mathf.Clamp(page, 0, PageCount - 1);
            Refresh();
        }

        private void SelectSlot(int visibleIndex)
        {
            if (visibleIndex < 0 || visibleIndex >= SlotsPerPage) return;
            int index = currentPage * SlotsPerPage + visibleIndex;
            if (index < 0 || index >= ChestSystem.Inventory.Count || catalog == null) return;
            ChestRecord chest = ChestSystem.Inventory[index];
            if (chest.state == ChestState.Pending)
            {
                bool started = ChestSystem.StartOpening(chest.chestId, catalog, DateTime.UtcNow);
                SetFeedback(started ? $"{chest.type.ToString().ToUpperInvariant()} CHEST OPENING STARTED"
                    : "ANOTHER CHEST IS ALREADY OPENING", null);
            }
            else if (chest.state == ChestState.Opening)
            {
                SetFeedback("OPENING  " + ChestSystem.FormatRemaining(chest.ReadyUtc, DateTime.UtcNow), null);
            }
            else if (chest.state == ChestState.ReadyToClaim)
            {
                ChestDefinition definition = catalog.chestCatalog?.Find(chest.type);
                if (ChestSystem.Claim(chest.chestId, catalog, DateTime.UtcNow, out ChestClaimResult claim))
                {
                    BundleDefinition bundle = PremiumBundleCatalog.Find(catalog, claim.BundleId);
                    string blueprintLine = claim.Blueprints > 0
                        ? $"\n{claim.BundleName}\n+{claim.Blueprints} BLUEPRINTS\n" +
                          $"{claim.BlueprintTotal} / {claim.BlueprintRequirement}" +
                          (claim.BundleUnlocked ? "  UNLOCKED!" : string.Empty)
                        : claim.Type == ChestType.Master || claim.Type == ChestType.Legendary
                            ? "\nNO ELIGIBLE BLUEPRINT BUNDLE" : string.Empty;
                    string gemsLine = claim.Gems > 0 ? $"\n+{claim.Gems:N0} GEMS" : string.Empty;
                    SetFeedback($"{claim.Type.ToString().ToUpperInvariant()} CHEST OPENED\n+{claim.Coins:N0} COINS" +
                        gemsLine +
                        blueprintLine, bundle?.Thumbnail);

                    EnsureRewardModal();
                    if (rewardModal != null)
                    {
                        Sprite coinIcon = catalog.chestCatalog?.coinsChestSprite;
                        rewardModal.Open(definition, claim, bundle, coinIcon, catalog, Refresh);
                    }
                }
            }
            Refresh();
        }

        private void EnsureRewardModal()
        {
            if (rewardModal != null) return;
            rewardModal = GetComponentInChildren<ChestRewardModalUI>(true);
            if (rewardModal != null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                rewardModal = canvas.GetComponentInChildren<ChestRewardModalUI>(true);

            if (rewardModal == null)
            {
                GameObject modalGo = new GameObject("Chest Reward Modal Controller", typeof(ChestRewardModalUI));
                modalGo.transform.SetParent(transform, false);
                rewardModal = modalGo.GetComponent<ChestRewardModalUI>();
            }
        }

        private void EnsureContentsIndexUI()
        {
            if (!Application.isPlaying || contentsIndexRoot != null) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject buttonObject = new GameObject("Chest Contents Button", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(transform, false);
            bool pagedLayout = previousPageButton != null || nextPageButton != null;
            buttonRect.anchorMin = buttonRect.anchorMax = pagedLayout ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            buttonRect.pivot = pagedLayout ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            buttonRect.anchoredPosition = pagedLayout ? new Vector2(18f, -18f) : new Vector2(-18f, -18f);
            buttonRect.sizeDelta = new Vector2(pagedLayout ? 200f : 220f, pagedLayout ? 48f : 56f);
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(.02f, .36f, .62f, .98f);
            contentsButton = buttonObject.GetComponent<Button>();
            contentsButton.targetGraphic = buttonImage;
            contentsButton.onClick.AddListener(ShowContentsIndex);
            Text buttonLabel = CreateIndexText("Label", buttonObject.transform, font, "CHEST CONTENTS",
                19, FontStyle.Bold, Vector2.zero, Vector2.one);
            buttonLabel.color = Color.white;

            contentsIndexRoot = new GameObject("Chest Contents Index", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            RectTransform rootRect = contentsIndexRoot.GetComponent<RectTransform>();
            rootRect.SetParent(canvas.transform, false);
            Stretch(rootRect, Vector2.zero, Vector2.one);
            Image blocker = contentsIndexRoot.GetComponent<Image>();
            blocker.color = new Color(0f, .01f, .03f, .84f);
            blocker.raycastTarget = true;

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(contentsIndexRoot.transform, false);
            Stretch(panelRect, new Vector2(.12f, .07f), new Vector2(.88f, .93f));
            panel.GetComponent<Image>().color = new Color(.025f, .07f, .13f, .995f);
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(.18f, .75f, 1f, .95f);
            outline.effectDistance = new Vector2(3f, -3f);

            Text title = CreateIndexText("Title", panel.transform, font, "CHEST CONTENTS", 31,
                FontStyle.Bold, new Vector2(.05f, .895f), new Vector2(.82f, .985f));
            title.alignment = TextAnchor.MiddleLeft;
            title.color = new Color(.32f, .9f, 1f);
            contentsIndexText = CreateIndexText("Contents", panel.transform, font, string.Empty, 20,
                FontStyle.Bold, new Vector2(.05f, .08f), new Vector2(.95f, .89f));
            contentsIndexText.alignment = TextAnchor.MiddleLeft;
            contentsIndexText.horizontalOverflow = HorizontalWrapMode.Wrap;
            contentsIndexText.verticalOverflow = VerticalWrapMode.Overflow;

            Button close = CreateIndexButton("Close", panel.transform, font, "X",
                new Vector2(.86f, .90f), new Vector2(.96f, .98f), new Color(.62f, .12f, .16f, 1f));
            close.onClick.AddListener(HideContentsIndex);
            RefreshContentsIndex();
            contentsIndexRoot.SetActive(false);
        }

        private void RefreshContentsIndex()
        {
            if (contentsIndexText == null) return;
            if (catalog?.chestCatalog?.chests == null)
            {
                contentsIndexText.text = "CHEST INFORMATION IS LOADING...";
                return;
            }

            var lines = new System.Text.StringBuilder();
            ChestDefinition[] definitions = catalog.chestCatalog.chests;
            for (int i = 0; i < definitions.Length; i++)
            {
                ChestDefinition value = definitions[i];
                if (value == null) continue;
                int hours = Mathf.Max(1, value.openingSeconds) / 3600;
                string blueprints = value.blueprintReward > 0
                    ? $"  •  {value.blueprintReward:N0} BLUEPRINTS"
                    : "  •  NO BLUEPRINTS";
                if (lines.Length > 0) lines.Append("\n\n");
                lines.Append(value.displayName).Append("   •   OPEN ").Append(hours).Append("H\n")
                    .Append(value.minimumCoins.ToString("N0")).Append("-")
                    .Append(value.maximumCoins.ToString("N0")).Append(" COINS  •  ")
                    .Append(value.minimumGems.ToString("N0")).Append("-")
                    .Append(value.maximumGems.ToString("N0")).Append(" GEMS")
                    .Append(blueprints);
            }
            contentsIndexText.text = lines.ToString();
        }

        private static Text CreateIndexText(string name, Transform parent, Font font, string value,
            int size, FontStyle style, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject result = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform rect = result.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect, anchorMin, anchorMax);
            Text text = result.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 11;
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateIndexButton(string name, Transform parent, Font font, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject result = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            RectTransform rect = result.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect, anchorMin, anchorMax);
            Image image = result.GetComponent<Image>();
            image.color = color;
            Button button = result.GetComponent<Button>();
            button.targetGraphic = image;
            CreateIndexText("Label", result.transform, font, label, 24, FontStyle.Bold,
                Vector2.zero, Vector2.one);
            return button;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void Refresh()
        {
            EnsureRewardedAdControls();
            ChestSystem.RefreshOpeningStates(DateTime.UtcNow);
            currentPage = Mathf.Clamp(currentPage, 0, PageCount - 1);
            float progress = ChestSystem.GameplayProgress;
            bool inventoryFull = ChestSystem.Inventory.Count >= ChestCatalog.MaximumInventorySize;
            if (progressFill != null)
                progressFill.fillAmount = inventoryFull
                    ? 1f
                    : Mathf.Clamp01(progress / ChestCatalog.GameplayIntervalSeconds);
            if (progressText != null)
            {
                if (inventoryFull)
                {
                    progressText.text = $"CHEST STORAGE FULL  {ChestCatalog.MaximumInventorySize} / " +
                                        ChestCatalog.MaximumInventorySize;
                }
                else
                {
                    int interval = Mathf.RoundToInt(ChestCatalog.GameplayIntervalSeconds);
                    int elapsed = Mathf.FloorToInt(progress);
                    int remaining = Mathf.CeilToInt(ChestCatalog.GameplayIntervalSeconds - progress);
                    progressText.text = $"NEXT CHEST  {remaining / 60:00}:{remaining % 60:00}   " +
                                        $"({elapsed / 60:00}:{elapsed % 60:00} / " +
                                        $"{interval / 60:00}:{interval % 60:00})";
                }
            }

            if (pageText != null)
                pageText.text = $"CHESTS  {ChestSystem.Inventory.Count} / {ChestCatalog.MaximumInventorySize}   " +
                                $"{currentPage + 1} / {PageCount}";
            if (previousPageButton != null) previousPageButton.interactable = currentPage > 0;
            if (nextPageButton != null) nextPageButton.interactable = currentPage + 1 < PageCount;
            RefreshRewardedAdControls();
            for (int i = 0; i < (slotButtons?.Length ?? 0); i++)
            {
                if (slotButtons[i] == null) continue;
                if (i >= SlotsPerPage)
                {
                    slotButtons[i].gameObject.SetActive(false);
                    continue;
                }

                int index = currentPage * SlotsPerPage + i;
                bool visible = index < ChestSystem.Inventory.Count;
                slotButtons[i].gameObject.SetActive(true);
                slotButtons[i].interactable = visible;
                if (!visible)
                {
                    if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
                    {
                        slotImages[i].sprite = null;
                        slotImages[i].enabled = false;
                    }
                    if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
                        slotLabels[i].text = "EMPTY";
                    continue;
                }
                ChestRecord chest = ChestSystem.Inventory[index];
                ChestDefinition definition = catalog?.chestCatalog?.Find(chest.type);
                bool openVisual = chest.state == ChestState.ReadyToClaim;
                if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
                {
                    slotImages[i].sprite = openVisual ? definition?.openSprite : definition?.closedSprite;
                    slotImages[i].preserveAspect = true;
                    slotImages[i].enabled = slotImages[i].sprite != null;
                }
                if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
                {
                    string state = chest.state == ChestState.Opening
                        ? ChestSystem.FormatRemaining(chest.ReadyUtc, DateTime.UtcNow) +
                          $"  ADS {chest.rewardedAdViews}/{ChestSystem.GetRequiredAdViews(chest.type)}"
                        : chest.state == ChestState.ReadyToClaim ? "READY!  OPEN / CLAIM"
                        : "TAP TO START";
                    slotLabels[i].text = $"{chest.type.ToString().ToUpperInvariant()}\n{state}";
                }
            }
        }

        private void EnsureRewardedAdControls()
        {
            if (!Application.isPlaying || adControlsConfigured) return;
            Transform chestTransform = transform.Find("Open Clapperboard Badge");
            Transform coinTransform = transform.Find("Coin Reward Clapperboard Badge");
            if (chestTransform == null && coinTransform == null) return;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (chestTransform != null)
                chestAdButton = ConfigureRewardedAdButton(chestTransform, WatchChestAd, font,
                    out chestAdCounter);
            if (coinTransform != null)
                coinAdButton = ConfigureRewardedAdButton(coinTransform, WatchCoinAd, font,
                    out coinAdCounter);
            adControlsConfigured = true;
        }

        private static Button ConfigureRewardedAdButton(Transform target, UnityAction action, Font font,
            out Text counter)
        {
            Image image = target.GetComponent<Image>();
            Button button = target.GetComponent<Button>();
            if (button == null) button = target.gameObject.AddComponent<Button>();
            if (image != null)
            {
                image.raycastTarget = true;
                button.targetGraphic = image;
            }
            button.transition = Selectable.Transition.ColorTint;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);

            Transform existing = target.Find("Quota Counter");
            if (existing != null)
                counter = existing.GetComponent<Text>();
            else
            {
                counter = CreateIndexText("Quota Counter", target, font, string.Empty, 16,
                    FontStyle.Bold, new Vector2(.06f, .01f), new Vector2(.94f, .27f));
                Outline outline = counter.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, .98f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return button;
        }

        private void RefreshRewardedAdControls()
        {
            DateTime now = DateTime.UtcNow;
            RewardedAdQuota coinQuota = ChestSystem.GetCoinAdQuota(now);
            RewardedAdQuota chestQuota = ChestSystem.GetChestAdQuota(now);
            ChestRecord opening = ChestSystem.GetOpeningChest(now);
            if (coinAdCounter != null) coinAdCounter.text = $"{coinQuota.Used}/{coinQuota.Limit}";
            if (chestAdCounter != null)
                chestAdCounter.text = opening != null
                    ? $"{opening.rewardedAdViews}/{ChestSystem.GetRequiredAdViews(opening.type)}"
                    : $"{chestQuota.Used}/{chestQuota.Limit}";
            bool ready = RewardedAdService.IsReady && !adRequestInFlight;
            if (coinAdButton != null) coinAdButton.interactable = ready && coinQuota.IsAvailable;
            if (chestAdButton != null)
                chestAdButton.interactable = ready && chestQuota.IsAvailable && opening != null;
        }

        private static string FormatQuotaRemaining(RewardedAdQuota quota)
        {
            TimeSpan remaining = quota.ResetUtc - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            return $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }

        private void ShowFirstUnseenChest()
        {
            for (int i = 0; i < ChestSystem.Inventory.Count; i++)
            {
                ChestRecord chest = ChestSystem.Inventory[i];
                if (chest.seen) continue;
                currentPage = i / SlotsPerPage;
                SetFeedback($"NEW CHEST!\n{chest.type.ToString().ToUpperInvariant()} CHEST", null);
                ChestSystem.MarkSeen(chest.chestId);
                break;
            }
        }

        private void SetFeedback(string value, Sprite bundleSprite)
        {
            if (feedbackText != null) feedbackText.text = value ?? string.Empty;
            if (feedbackBundleImage != null)
            {
                feedbackBundleImage.sprite = bundleSprite;
                feedbackBundleImage.enabled = bundleSprite != null;
                feedbackBundleImage.preserveAspect = true;
            }
        }

        public void Assign(GameCatalog gameCatalog, Button[] buttons, Image[] images, Text[] labels,
            Image fill, Text progress, Text pageLabel, Text feedback, Image bundleImage,
            Button previousPage = null, Button nextPage = null)
        {
            catalog = gameCatalog;
            slotButtons = buttons;
            slotImages = images;
            slotLabels = labels;
            progressFill = fill;
            progressText = progress;
            pageText = pageLabel;
            feedbackText = feedback;
            feedbackBundleImage = bundleImage;
            previousPageButton = previousPage;
            nextPageButton = nextPage;
            ConfigureVisualStyles();
            EnsureContentsIndexUI();
            EnsureRewardedAdControls();
            RefreshContentsIndex();
            if (Application.isPlaying) Refresh();
        }
    }
}
