using System;
using AnimeHillRacing.Chests;
using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class ChestPanelUI : MonoBehaviour
    {
        public const int SlotsPerPage = 4;

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
        private static readonly Color GreenProgressColor = new Color(0.16f, 0.86f, 0.32f, 1f);
        private float nextRefresh;
        private bool outlineConfigured;
        private int currentPage;

        public int CurrentPage => currentPage;
        public int PageCount => Mathf.Max(1, Mathf.CeilToInt(ChestSystem.Inventory.Count / (float)SlotsPerPage));

        private void Awake()
        {
            ConfigureVisualStyles();
        }

        private void OnEnable()
        {
            ConfigureVisualStyles();
            ChestSystem.RefreshOpeningStates(DateTime.UtcNow);
            ShowFirstUnseenChest();
            Refresh();
        }

        private void ConfigureVisualStyles()
        {
            if (progressFill != null)
                progressFill.color = GreenProgressColor;

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

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .2f;
            Refresh();
        }

        public void SelectSlot0() => SelectSlot(0);
        public void SelectSlot1() => SelectSlot(1);
        public void SelectSlot2() => SelectSlot(2);
        public void SelectSlot3() => SelectSlot(3);

        public void PreviousPage() => SetPage(currentPage - 1);
        public void NextPage() => SetPage(currentPage + 1);

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
                    SetFeedback($"{claim.Type.ToString().ToUpperInvariant()} CHEST OPENED\n+{claim.Coins:N0} COINS" +
                        blueprintLine, bundle?.Thumbnail);

                    EnsureRewardModal();
                    if (rewardModal != null)
                    {
                        Sprite coinIcon = catalog.chestCatalog?.coinsChestSprite;
                        rewardModal.Open(definition, claim, bundle, coinIcon, Refresh);
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

        private void Refresh()
        {
            ChestSystem.RefreshOpeningStates(DateTime.UtcNow);
            currentPage = Mathf.Clamp(currentPage, 0, PageCount - 1);
            float progress = ChestSystem.GameplayProgress;
            if (progressFill != null)
                progressFill.fillAmount = Mathf.Clamp01(progress / ChestCatalog.GameplayIntervalSeconds);
            if (progressText != null)
            {
                int remaining = Mathf.CeilToInt(ChestCatalog.GameplayIntervalSeconds - progress);
                progressText.text = $"NEXT CHEST  {remaining / 60:00}:{remaining % 60:00}   " +
                                    $"({Mathf.FloorToInt(progress) / 60:00}:{Mathf.FloorToInt(progress) % 60:00} / 30:00)";
            }

            if (pageText != null)
                pageText.text = $"CHESTS  {ChestSystem.Inventory.Count}   {currentPage + 1} / {PageCount}";
            if (previousPageButton != null) previousPageButton.interactable = currentPage > 0;
            if (nextPageButton != null) nextPageButton.interactable = currentPage + 1 < PageCount;
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
                        ? ChestSystem.FormatRemaining(chest.ReadyUtc, DateTime.UtcNow)
                        : chest.state == ChestState.ReadyToClaim ? "READY!  OPEN / CLAIM"
                        : "TAP TO START";
                    slotLabels[i].text = $"{chest.type.ToString().ToUpperInvariant()}\n{state}";
                }
            }
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
            if (Application.isPlaying) Refresh();
        }
    }
}
