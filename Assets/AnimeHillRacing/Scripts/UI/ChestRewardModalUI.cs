using System;
using System.Collections;
using System.Collections.Generic;
using AnimeHillRacing.Chests;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class ChestRewardModalUI : MonoBehaviour
    {
        private enum RewardKind { Coins, Gems, Blueprints, Skin }

        private struct RewardPresentation
        {
            public RewardKind Kind;
            public string Name;
            public string Details;
            public ItemRarity Rarity;
            public Sprite Sprite;
            public Sprite DefaultVehicleSprite;
        }

        [SerializeField] private GameObject modalRoot;
        [SerializeField] private RectTransform modalBox;
        [SerializeField] private Image modalBackground;
        [SerializeField] private Text chestTitleText;
        [SerializeField] private Image chestImage;
        [SerializeField] private Image glowEffect;
        [SerializeField] private Image youGotImage;
        [SerializeField] private Text youGotFallback;
        [SerializeField] private RectTransform rewardRoot;
        [SerializeField] private CanvasGroup rewardCanvasGroup;
        [SerializeField] private Image rarityFrame;
        [SerializeField] private Image rewardImage;
        [SerializeField] private Image defaultVehicleImage;
        [SerializeField] private Text rewardNameText;
        [SerializeField] private Text rewardDetailsText;
        [SerializeField] private Text rarityText;
        [SerializeField] private Text tapHintText;
        [SerializeField] private Button tapAdvanceButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private Text readyFallbackText;

        private readonly List<RewardPresentation> rewards = new List<RewardPresentation>(4);
        private Action onClosed;
        private Coroutine animationRoutine;
        private GameCatalog catalog;
        private int rewardIndex;
        private bool isAnimating;
        private bool waitingForTap;
        private bool rewardsComplete;

        public bool IsAnimating => isAnimating;

        private void Awake()
        {
            BindButtons();
            if (modalRoot != null) modalRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (tapAdvanceButton != null) tapAdvanceButton.onClick.RemoveListener(OnAdvanceTap);
            if (readyButton != null) readyButton.onClick.RemoveListener(OnReadyClicked);
        }

        public void Open(ChestDefinition definition, ChestClaimResult claim, BundleDefinition bundle,
            Sprite legacyCoinSprite, Action closeCallback) =>
            Open(definition, claim, bundle, legacyCoinSprite, null, closeCallback);

        public void Open(ChestDefinition definition, ChestClaimResult claim, BundleDefinition bundle,
            Sprite legacyCoinSprite, GameCatalog gameCatalog, Action closeCallback)
        {
            catalog = gameCatalog;
            onClosed = closeCallback;
            if (modalRoot == null) BuildDynamicModal(legacyCoinSprite);
            BindButtons();
            BuildRewardQueue(definition, claim, bundle);
            ApplySuppliedArtwork();

            rewardIndex = 0;
            waitingForTap = false;
            rewardsComplete = false;
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            modalRoot.SetActive(true);
            modalRoot.transform.SetAsLastSibling();
            animationRoutine = StartCoroutine(AnimateChestOpening(definition));
        }

        private void BuildRewardQueue(ChestDefinition definition, ChestClaimResult claim,
            BundleDefinition bundle)
        {
            rewards.Clear();
            if (claim == null) return;
            ItemRarity currencyRarity = definition != null
                ? definition.ResolveRewardRarity() : ItemRarity.Common;
            HillAdventurerUiCatalog ui = HillAdventurerUiCatalog.Instance;
            if (claim.Coins > 0)
                rewards.Add(new RewardPresentation
                {
                    Kind = RewardKind.Coins,
                    Name = $"+{claim.Coins:N0} COINS",
                    Details = "COIN REWARD",
                    Rarity = currencyRarity,
                    Sprite = ui != null ? ui.coinsRewardIcon : null
                });
            if (claim.Gems > 0)
                rewards.Add(new RewardPresentation
                {
                    Kind = RewardKind.Gems,
                    Name = $"+{claim.Gems:N0} GEMS",
                    Details = "GEM REWARD",
                    Rarity = currencyRarity,
                    Sprite = ui != null ? ui.gemsRewardIcon : null
                });
            if (bundle == null || claim.Blueprints <= 0) return;

            bool unlockedSkin = claim.BundleUnlocked && bundle.Type == BundleType.Cosmetic &&
                                string.Equals(bundle.Category, "Skin", StringComparison.OrdinalIgnoreCase);
            Sprite defaultVehicle = null;
            if (unlockedSkin && catalog != null)
            {
                VehicleDefinition vehicle = catalog.FindVehicle(bundle.ScopeId);
                defaultVehicle = vehicle != null ? vehicle.previewSprite : null;
            }
            rewards.Add(new RewardPresentation
            {
                Kind = unlockedSkin ? RewardKind.Skin : RewardKind.Blueprints,
                Name = unlockedSkin ? (bundle.DisplayName ?? "SKIN").ToUpperInvariant() :
                    $"+{claim.Blueprints:N0} BLUEPRINTS",
                Details = unlockedSkin ? "SKIN UNLOCKED" :
                    $"{(claim.BundleName ?? "BUNDLE").ToUpperInvariant()}\n" +
                    $"{claim.BlueprintTotal:N0} / {claim.BlueprintRequirement:N0}" +
                    (claim.BundleUnlocked ? "  •  UNLOCKED" : string.Empty),
                Rarity = bundle.Rarity,
                Sprite = bundle.Thumbnail,
                DefaultVehicleSprite = defaultVehicle
            });
        }

        private IEnumerator AnimateChestOpening(ChestDefinition definition)
        {
            isAnimating = true;
            waitingForTap = false;
            rewardsComplete = false;
            tapAdvanceButton.gameObject.SetActive(true);
            tapAdvanceButton.interactable = false;
            readyButton.gameObject.SetActive(false);
            rewardRoot.gameObject.SetActive(false);
            tapHintText.gameObject.SetActive(false);
            SetHeaderVisible(false);

            chestTitleText.gameObject.SetActive(true);
            chestTitleText.text = definition != null && !string.IsNullOrWhiteSpace(definition.displayName)
                ? definition.displayName.ToUpperInvariant() + " CHEST" : "CHEST OPENED";
            chestTitleText.color = definition != null ? definition.accentColor : Color.white;
            chestImage.gameObject.SetActive(true);
            chestImage.sprite = definition != null ? definition.closedSprite : null;
            chestImage.enabled = chestImage.sprite != null;
            chestImage.preserveAspect = true;
            chestImage.rectTransform.localScale = Vector3.one * .68f;
            glowEffect.gameObject.SetActive(true);
            glowEffect.color = Color.clear;

            yield return AnimateScale(chestImage.rectTransform, .68f, 1.14f, .34f, false);
            float elapsed = 0f;
            while (elapsed < .24f)
            {
                elapsed += Time.unscaledDeltaTime;
                float shake = Mathf.Sin(elapsed * 42f) * .065f;
                chestImage.rectTransform.localScale = new Vector3(1.14f + shake, 1.14f - shake, 1f);
                yield return null;
            }

            if (definition != null && definition.openSprite != null)
                chestImage.sprite = definition.openSprite;
            Color accent = definition != null ? definition.accentColor : new Color(.2f, .8f, 1f);
            glowEffect.color = new Color(accent.r, accent.g, accent.b, .76f);
            yield return AnimateScale(chestImage.rectTransform, 1.24f, 1f, .28f, false);
            yield return new WaitForSecondsRealtime(.12f);

            chestImage.gameObject.SetActive(false);
            glowEffect.gameObject.SetActive(false);
            chestTitleText.gameObject.SetActive(false);
            SetHeaderVisible(true);
            if (rewards.Count == 0)
                yield return ShowReady();
            else
                yield return RevealCurrentReward();
        }

        private IEnumerator RevealCurrentReward()
        {
            isAnimating = true;
            waitingForTap = false;
            tapAdvanceButton.gameObject.SetActive(true);
            tapAdvanceButton.interactable = false;
            readyButton.gameObject.SetActive(false);

            RewardPresentation reward = rewards[rewardIndex];
            ConfigureReward(reward);
            rewardRoot.gameObject.SetActive(true);
            tapHintText.gameObject.SetActive(true);
            rewardCanvasGroup.alpha = 0f;
            float intensity = Mathf.InverseLerp(0f, ItemRarityUtility.Count - 1, (int)reward.Rarity);
            float duration = Mathf.Lerp(.24f, .38f, intensity);
            float startScale = Mathf.Lerp(.78f, .62f, intensity);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rewardCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                float scale = Mathf.LerpUnclamped(startScale, 1f, EaseOutBack(t, 1.35f + intensity));
                rewardRoot.localScale = Vector3.one * scale;
                yield return null;
            }
            rewardCanvasGroup.alpha = 1f;
            rewardRoot.localScale = Vector3.one;
            tapHintText.text = $"TAP TO CONTINUE   •   {rewardIndex + 1} / {rewards.Count}";
            isAnimating = false;
            waitingForTap = true;
            tapAdvanceButton.interactable = true;
        }

        private void ConfigureReward(RewardPresentation reward)
        {
            HillAdventurerUiCatalog ui = HillAdventurerUiCatalog.Instance;
            Sprite frame = ui != null ? ui.GetRarityFrame(reward.Rarity) : null;
            rarityFrame.sprite = frame;
            rarityFrame.preserveAspect = frame != null;
            Color accent = ItemRarityUtility.AccentColor(reward.Rarity);
            rarityFrame.color = frame != null ? Color.white : new Color(accent.r, accent.g, accent.b, .42f);

            rewardImage.sprite = reward.Sprite;
            rewardImage.enabled = reward.Sprite != null;
            rewardImage.preserveAspect = true;
            rewardImage.rectTransform.localScale = Vector3.one *
                                                   (reward.Kind == RewardKind.Skin ? 1.42f : 1f);
            defaultVehicleImage.sprite = reward.DefaultVehicleSprite;
            defaultVehicleImage.enabled = reward.Kind == RewardKind.Skin && reward.DefaultVehicleSprite != null;
            defaultVehicleImage.preserveAspect = true;
            rewardNameText.text = reward.Name ?? string.Empty;
            rewardDetailsText.text = reward.Details ?? string.Empty;
            rarityText.text = ItemRarityUtility.DisplayName(reward.Rarity);
            rarityText.color = accent;
        }

        private void OnAdvanceTap()
        {
            if (isAnimating || !waitingForTap || rewardsComplete) return;
            isAnimating = true;
            waitingForTap = false;
            tapAdvanceButton.interactable = false;
            animationRoutine = StartCoroutine(AnimateExitAndAdvance());
        }

        private IEnumerator AnimateExitAndAdvance()
        {
            float elapsed = 0f;
            const float duration = .18f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rewardCanvasGroup.alpha = 1f - t;
                rewardRoot.localScale = Vector3.one * Mathf.Lerp(1f, .82f, t);
                yield return null;
            }
            rewardRoot.gameObject.SetActive(false);
            rewardIndex++;
            if (rewardIndex < rewards.Count)
                yield return RevealCurrentReward();
            else
                yield return ShowReady();
        }

        private IEnumerator ShowReady()
        {
            isAnimating = true;
            waitingForTap = false;
            rewardsComplete = true;
            tapAdvanceButton.interactable = false;
            tapAdvanceButton.gameObject.SetActive(false);
            rewardRoot.gameObject.SetActive(false);
            tapHintText.gameObject.SetActive(false);
            readyButton.gameObject.SetActive(true);
            readyButton.interactable = false;
            readyButton.transform.localScale = Vector3.one * .72f;
            yield return AnimateScale(readyButton.transform as RectTransform, .72f, 1f, .26f, true);
            readyButton.transform.localScale = Vector3.one;
            readyButton.interactable = true;
            isAnimating = false;
        }

        private void OnReadyClicked()
        {
            if (isAnimating || !rewardsComplete) return;
            isAnimating = true;
            readyButton.interactable = false;
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            modalRoot.SetActive(false);
            Action callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }

        private static IEnumerator AnimateScale(RectTransform target, float from, float to,
            float duration, bool overshoot)
        {
            if (target == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, duration));
                float eased = overshoot ? EaseOutBack(t, 1.5f) : Mathf.SmoothStep(0f, 1f, t);
                target.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, eased);
                yield return null;
            }
            target.localScale = Vector3.one * to;
        }

        private static float EaseOutBack(float t, float overshoot)
        {
            float shifted = t - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted +
                   overshoot * shifted * shifted;
        }

        private void ApplySuppliedArtwork()
        {
            HillAdventurerUiCatalog ui = HillAdventurerUiCatalog.Instance;
            Sprite background = ui != null ? ui.chestRewardBackground : null;
            modalBackground.sprite = background;
            modalBackground.preserveAspect = background != null;
            modalBackground.color = background != null ? Color.white : new Color(.035f, .055f, .12f, .99f);
            Sprite header = ui != null ? ui.youGotHeader : null;
            youGotImage.sprite = header;
            youGotImage.enabled = header != null;
            youGotImage.preserveAspect = true;
            youGotFallback.enabled = header == null;

            Sprite ready = ui != null ? ui.readyButton : null;
            readyButton.image.sprite = ready;
            readyButton.image.preserveAspect = ready != null;
            readyButton.image.color = ready != null ? Color.white : new Color(.10f, .62f, .35f, .98f);
            readyFallbackText.enabled = ready == null;
        }

        private void SetHeaderVisible(bool visible)
        {
            if (youGotImage != null) youGotImage.gameObject.SetActive(visible);
            if (youGotFallback != null) youGotFallback.gameObject.SetActive(visible);
        }

        private void BindButtons()
        {
            if (tapAdvanceButton != null)
            {
                tapAdvanceButton.onClick.RemoveListener(OnAdvanceTap);
                tapAdvanceButton.onClick.AddListener(OnAdvanceTap);
            }
            if (readyButton != null)
            {
                readyButton.onClick.RemoveListener(OnReadyClicked);
                readyButton.onClick.AddListener(OnReadyClicked);
            }
        }

        public void BuildDynamicModal(Sprite legacyCoinIcon)
        {
            if (modalRoot != null) return;
            Transform parent = transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) parent = canvas.transform;

            modalRoot = CreateRectObject("Chest Reward Modal Root", parent).gameObject;
            SetStretch(modalRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            Image overlay = CreateImage("Dark Overlay", modalRoot.transform, Vector2.zero, Vector2.one);
            overlay.color = new Color(0f, 0f, 0f, .78f);
            overlay.raycastTarget = true;

            GameObject box = CreateRectObject("Reward Experience", modalRoot.transform).gameObject;
            modalBox = box.GetComponent<RectTransform>();
            SetStretch(modalBox, new Vector2(.16f, .055f), new Vector2(.84f, .945f));
            modalBackground = box.AddComponent<Image>();
            modalBackground.raycastTarget = false;
            Outline outline = box.AddComponent<Outline>();
            outline.effectColor = new Color(.26f, .75f, 1f, .9f);
            outline.effectDistance = new Vector2(2f, -2f);

            youGotImage = CreateImage("You Got", box.transform,
                new Vector2(.25f, .865f), new Vector2(.75f, .98f));
            youGotImage.raycastTarget = false;
            youGotFallback = CreateText("You Got Fallback", youGotImage.transform, "YOU GOT", 34,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            youGotFallback.fontStyle = FontStyle.Bold;

            chestTitleText = CreateText("Chest Title", box.transform, "CHEST OPENED", 30,
                TextAnchor.MiddleCenter, new Vector2(.12f, .79f), new Vector2(.88f, .88f));
            chestTitleText.fontStyle = FontStyle.Bold;

            glowEffect = CreateImage("Opening Glow", box.transform,
                new Vector2(.25f, .25f), new Vector2(.75f, .78f));
            glowEffect.raycastTarget = false;
            chestImage = CreateImage("Chest Image", box.transform,
                new Vector2(.30f, .27f), new Vector2(.70f, .74f));
            chestImage.raycastTarget = false;
            chestImage.preserveAspect = true;

            rewardRoot = CreateRectObject("Single Reward", box.transform);
            SetStretch(rewardRoot, new Vector2(.12f, .06f), new Vector2(.88f, .86f));
            rewardCanvasGroup = rewardRoot.gameObject.AddComponent<CanvasGroup>();

            defaultVehicleImage = CreateImage("Default Vehicle Identification", rewardRoot,
                new Vector2(.40f, .76f), new Vector2(.60f, .94f));
            defaultVehicleImage.raycastTarget = false;
            defaultVehicleImage.preserveAspect = true;

            rarityFrame = CreateImage("Rarity Frame", rewardRoot,
                new Vector2(.20f, .20f), new Vector2(.80f, .78f));
            rarityFrame.raycastTarget = false;
            rarityFrame.preserveAspect = true;

            GameObject cropObject = CreateRectObject("Reward Preview Crop", rewardRoot).gameObject;
            RectTransform cropRect = cropObject.GetComponent<RectTransform>();
            SetStretch(cropRect, new Vector2(.30f, .31f), new Vector2(.70f, .68f));
            cropObject.AddComponent<RectMask2D>();
            rewardImage = CreateImage("Reward Artwork", cropObject.transform, Vector2.zero, Vector2.one);
            rewardImage.raycastTarget = false;
            rewardImage.preserveAspect = true;

            rewardNameText = CreateText("Reward Name", rewardRoot, string.Empty, 27,
                TextAnchor.MiddleCenter, new Vector2(.12f, .11f), new Vector2(.88f, .23f));
            rewardNameText.fontStyle = FontStyle.Bold;
            rewardDetailsText = CreateText("Reward Details", rewardRoot, string.Empty, 17,
                TextAnchor.MiddleCenter, new Vector2(.12f, .035f), new Vector2(.88f, .13f));
            rarityText = CreateText("Rarity", rewardRoot, string.Empty, 16,
                TextAnchor.MiddleCenter, new Vector2(.36f, .72f), new Vector2(.64f, .78f));
            rarityText.fontStyle = FontStyle.Bold;
            tapHintText = CreateText("Tap Hint", box.transform, "TAP TO CONTINUE", 15,
                TextAnchor.MiddleCenter, new Vector2(.25f, .012f), new Vector2(.75f, .065f));
            tapHintText.color = new Color(.75f, .84f, .94f);

            GameObject readyObject = CreateRectObject("Ready", box.transform).gameObject;
            RectTransform readyRect = readyObject.GetComponent<RectTransform>();
            SetStretch(readyRect, new Vector2(.34f, .07f), new Vector2(.66f, .20f));
            Image readyImage = readyObject.AddComponent<Image>();
            readyButton = readyObject.AddComponent<Button>();
            readyButton.targetGraphic = readyImage;
            readyFallbackText = CreateText("Ready Fallback", readyObject.transform, "READY", 24,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            readyFallbackText.fontStyle = FontStyle.Bold;

            GameObject tapObject = CreateRectObject("Tap To Advance", modalRoot.transform).gameObject;
            SetStretch(tapObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            Image tapImage = tapObject.AddComponent<Image>();
            tapImage.color = Color.clear;
            tapImage.raycastTarget = true;
            tapAdvanceButton = tapObject.AddComponent<Button>();
            tapAdvanceButton.targetGraphic = tapImage;
            tapAdvanceButton.transition = Selectable.Transition.None;

            BindButtons();
            ApplySuppliedArtwork();
        }

        private static RectTransform CreateRectObject(string objectName, Transform parent)
        {
            GameObject instance = new GameObject(objectName, typeof(RectTransform));
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(string objectName, Transform parent, Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject instance = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetStretch(rect, anchorMin, anchorMax);
            return instance.GetComponent<Image>();
        }

        private static Text CreateText(string objectName, Transform parent, string value, int fontSize,
            TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject instance = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Text));
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetStretch(rect, anchorMin, anchorMax);
            Text text = instance.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                        Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void SetStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
