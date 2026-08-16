using System;
using System.Collections;
using AnimeHillRacing.Chests;
using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class ChestRewardModalUI : MonoBehaviour
    {
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Image overlayBackground;
        [SerializeField] private RectTransform modalBox;
        [SerializeField] private Text chestTitleText;
        [SerializeField] private Image chestImage;
        [SerializeField] private Image glowEffect;
        [SerializeField] private GameObject rewardsRoot;
        [SerializeField] private GameObject coinsRewardCard;
        [SerializeField] private Text coinsRewardText;
        [SerializeField] private GameObject blueprintRewardCard;
        [SerializeField] private Image blueprintThumbnail;
        [SerializeField] private Text blueprintRewardText;
        [SerializeField] private Text blueprintDetailsText;
        [SerializeField] private Button claimButton;
        [SerializeField] private Text claimButtonText;

        private Action onClaimClosed;
        private Coroutine animationRoutine;
        private bool isAnimating;

        private void Awake()
        {
            if (claimButton != null)
                claimButton.onClick.AddListener(OnClaimClicked);
            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        public void Open(ChestDefinition definition, ChestClaimResult claim, BundleDefinition bundle, Sprite coinSprite, Action onClose)
        {
            onClaimClosed = onClose;
            if (modalRoot == null)
                BuildDynamicModal(coinSprite);

            modalRoot.SetActive(true);
            if (animationRoutine != null)
                StopCoroutine(animationRoutine);

            animationRoutine = StartCoroutine(AnimateChestOpening(definition, claim, bundle));
        }

        private IEnumerator AnimateChestOpening(ChestDefinition definition, ChestClaimResult claim, BundleDefinition bundle)
        {
            isAnimating = true;
            if (claimButton != null)
                claimButton.gameObject.SetActive(false);
            if (rewardsRoot != null)
                rewardsRoot.SetActive(false);

            // Set Title & Colors
            if (chestTitleText != null)
            {
                chestTitleText.text = definition != null ? $"{definition.displayName.ToUpperInvariant()} CHEST" : "CHEST OPENED";
                chestTitleText.color = definition != null ? definition.accentColor : Color.white;
            }

            // Start with closed sprite
            if (chestImage != null)
            {
                chestImage.sprite = definition != null ? definition.closedSprite : null;
                chestImage.preserveAspect = true;
                chestImage.enabled = chestImage.sprite != null;
                chestImage.rectTransform.localScale = Vector3.one * 0.7f;
            }

            if (glowEffect != null)
            {
                glowEffect.color = new Color(1f, 1f, 1f, 0f);
                glowEffect.gameObject.SetActive(true);
            }

            // Phase 1: Pop in & anticipation scale
            float elapsed = 0f;
            const float popDuration = 0.35f;
            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                float scale = Mathf.Lerp(0.7f, 1.15f, Mathf.Sin(t * Mathf.PI * 0.5f));
                if (chestImage != null)
                    chestImage.rectTransform.localScale = Vector3.one * scale;
                yield return null;
            }

            // Phase 2: Wiggle / shake
            elapsed = 0f;
            const float shakeDuration = 0.25f;
            Vector3 baseScale = Vector3.one * 1.15f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float shake = Mathf.Sin(elapsed * 40f) * 0.08f;
                if (chestImage != null)
                    chestImage.rectTransform.localScale = new Vector3(baseScale.x + shake, baseScale.y - shake, 1f);
                yield return null;
            }

            // Phase 3: Chest Burst & Open!
            if (chestImage != null && definition != null && definition.openSprite != null)
            {
                chestImage.sprite = definition.openSprite;
            }

            // Flash glow
            if (glowEffect != null)
            {
                Color accent = definition != null ? definition.accentColor : new Color(0.2f, 0.8f, 1f, 1f);
                glowEffect.color = new Color(accent.r, accent.g, accent.b, 0.75f);
            }

            elapsed = 0f;
            const float burstDuration = 0.3f;
            while (elapsed < burstDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / burstDuration);
                float scale = Mathf.Lerp(1.25f, 1f, t);
                if (chestImage != null)
                    chestImage.rectTransform.localScale = Vector3.one * scale;
                if (glowEffect != null)
                    glowEffect.color = new Color(glowEffect.color.r, glowEffect.color.g, glowEffect.color.b, Mathf.Lerp(0.75f, 0.25f, t));
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.15f);

            // Phase 4: Reveal Rewards
            SetupRewardCards(claim, bundle);
            if (rewardsRoot != null)
            {
                rewardsRoot.SetActive(true);
                rewardsRoot.transform.localScale = Vector3.one * 0.8f;
            }

            elapsed = 0f;
            const float rewardRevealDuration = 0.25f;
            while (elapsed < rewardRevealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / rewardRevealDuration);
                if (rewardsRoot != null)
                    rewardsRoot.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(true);
                claimButton.transform.localScale = Vector3.one;
            }

            isAnimating = false;
        }

        private void SetupRewardCards(ChestClaimResult claim, BundleDefinition bundle)
        {
            if (claim == null) return;

            // Coins card
            if (coinsRewardCard != null)
            {
                coinsRewardCard.SetActive(claim.Coins > 0);
                if (coinsRewardText != null)
                    coinsRewardText.text = $"+{claim.Coins:N0} COINS";
            }

            // Blueprints card
            if (blueprintRewardCard != null)
            {
                bool hasBlueprints = claim.Blueprints > 0;
                blueprintRewardCard.SetActive(hasBlueprints);
                if (hasBlueprints)
                {
                    if (blueprintThumbnail != null)
                    {
                        blueprintThumbnail.sprite = bundle != null ? bundle.Thumbnail : null;
                        blueprintThumbnail.enabled = blueprintThumbnail.sprite != null;
                        blueprintThumbnail.preserveAspect = true;
                    }
                    if (blueprintRewardText != null)
                    {
                        blueprintRewardText.text = $"+{claim.Blueprints} BLUEPRINTS";
                    }
                    if (blueprintDetailsText != null)
                    {
                        string unlockedTag = claim.BundleUnlocked ? "  <color=#00FF66>UNLOCKED!</color>" : "";
                        blueprintDetailsText.text = $"{claim.BundleName ?? "BUNDLE"}\n{claim.BlueprintTotal} / {claim.BlueprintRequirement}{unlockedTag}";
                    }
                }
            }
        }

        private void OnClaimClicked()
        {
            if (isAnimating) return;
            if (animationRoutine != null)
                StopCoroutine(animationRoutine);

            if (modalRoot != null)
                modalRoot.SetActive(false);

            onClaimClosed?.Invoke();
            onClaimClosed = null;
        }

        public void BuildDynamicModal(Sprite coinIcon)
        {
            Transform parent = transform;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                parent = canvas.transform;

            modalRoot = new GameObject("Chest Reward Modal Root", typeof(RectTransform));
            modalRoot.transform.SetParent(parent, false);
            var rootRt = modalRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.anchoredPosition = Vector2.zero;
            rootRt.sizeDelta = Vector2.zero;

            // 50% Black Overlay - intercepting raycasts to block underlying buttons
            var overlayGo = new GameObject("Dark Overlay (50%)", typeof(RectTransform), typeof(Image));
            overlayGo.transform.SetParent(modalRoot.transform, false);
            var overlayRt = overlayGo.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.anchoredPosition = Vector2.zero;
            overlayRt.sizeDelta = Vector2.zero;
            overlayBackground = overlayGo.GetComponent<Image>();
            overlayBackground.color = new Color(0f, 0f, 0f, 0.50f);
            overlayBackground.raycastTarget = true;

            // Modal Content Box
            var boxGo = new GameObject("Modal Container", typeof(RectTransform), typeof(Image), typeof(Outline));
            boxGo.transform.SetParent(modalRoot.transform, false);
            modalBox = boxGo.GetComponent<RectTransform>();
            modalBox.anchorMin = new Vector2(0.5f, 0.5f);
            modalBox.anchorMax = new Vector2(0.5f, 0.5f);
            modalBox.anchoredPosition = Vector2.zero;
            modalBox.sizeDelta = new Vector2(680f, 640f);

            var boxImage = boxGo.GetComponent<Image>();
            boxImage.color = new Color(0.04f, 0.08f, 0.15f, 0.96f);
            var boxOutline = boxGo.GetComponent<Outline>();
            boxOutline.effectColor = new Color(0.25f, 0.72f, 1f, 0.90f);
            boxOutline.effectDistance = new Vector2(2f, -2f);

            // Title
            var titleGo = new GameObject("Chest Title", typeof(RectTransform), typeof(Text), typeof(Outline));
            titleGo.transform.SetParent(boxGo.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -48f);
            titleRt.sizeDelta = new Vector2(600f, 54f);
            chestTitleText = titleGo.GetComponent<Text>();
            chestTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            chestTitleText.fontSize = 32;
            chestTitleText.fontStyle = FontStyle.Bold;
            chestTitleText.alignment = TextAnchor.MiddleCenter;
            chestTitleText.color = Color.white;
            var titleOutline = titleGo.GetComponent<Outline>();
            titleOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Glow
            var glowGo = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glowGo.transform.SetParent(boxGo.transform, false);
            var glowRt = glowGo.GetComponent<RectTransform>();
            glowRt.anchorMin = new Vector2(0.5f, 0.5f);
            glowRt.anchorMax = new Vector2(0.5f, 0.5f);
            glowRt.anchoredPosition = new Vector2(0f, 85f);
            glowRt.sizeDelta = new Vector2(340f, 340f);
            glowEffect = glowGo.GetComponent<Image>();
            glowEffect.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            glowEffect.raycastTarget = false;

            // Chest Image
            var chestImgGo = new GameObject("Chest Image", typeof(RectTransform), typeof(Image));
            chestImgGo.transform.SetParent(boxGo.transform, false);
            var chestImgRt = chestImgGo.GetComponent<RectTransform>();
            chestImgRt.anchorMin = new Vector2(0.5f, 0.5f);
            chestImgRt.anchorMax = new Vector2(0.5f, 0.5f);
            chestImgRt.anchoredPosition = new Vector2(0f, 85f);
            chestImgRt.sizeDelta = new Vector2(250f, 250f);
            chestImage = chestImgGo.GetComponent<Image>();
            chestImage.preserveAspect = true;
            chestImage.raycastTarget = false;

            // Rewards Root Container
            var rewardsGo = new GameObject("Rewards Container", typeof(RectTransform));
            rewardsGo.transform.SetParent(boxGo.transform, false);
            var rewardsRt = rewardsGo.GetComponent<RectTransform>();
            rewardsRt.anchorMin = new Vector2(0.5f, 0f);
            rewardsRt.anchorMax = new Vector2(0.5f, 0f);
            rewardsRt.anchoredPosition = new Vector2(0f, 140f);
            rewardsRt.sizeDelta = new Vector2(620f, 130f);
            rewardsRoot = rewardsGo;

            // Coins Reward Card
            var coinsCardGo = new GameObject("Coins Reward Card", typeof(RectTransform), typeof(Image), typeof(Outline));
            coinsCardGo.transform.SetParent(rewardsGo.transform, false);
            var coinsCardRt = coinsCardGo.GetComponent<RectTransform>();
            coinsCardRt.anchorMin = new Vector2(0.5f, 0.5f);
            coinsCardRt.anchorMax = new Vector2(0.5f, 0.5f);
            coinsCardRt.anchoredPosition = new Vector2(0f, 25f);
            coinsCardRt.sizeDelta = new Vector2(480f, 52f);
            var coinsBg = coinsCardGo.GetComponent<Image>();
            coinsBg.color = new Color(0.08f, 0.16f, 0.28f, 0.90f);
            var coinsOutline = coinsCardGo.GetComponent<Outline>();
            coinsOutline.effectColor = new Color(1f, 0.85f, 0.25f, 0.75f);
            coinsOutline.effectDistance = new Vector2(1f, -1f);
            coinsRewardCard = coinsCardGo;

            // Coin Icon
            if (coinIcon != null)
            {
                var iconGo = new GameObject("Coin Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(coinsCardGo.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(30f, 0f);
                iconRt.sizeDelta = new Vector2(38f, 38f);
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = coinIcon;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Coin Text
            var coinTxtGo = new GameObject("Coins Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            coinTxtGo.transform.SetParent(coinsCardGo.transform, false);
            var coinTxtRt = coinTxtGo.GetComponent<RectTransform>();
            coinTxtRt.anchorMin = Vector2.zero;
            coinTxtRt.anchorMax = Vector2.one;
            coinTxtRt.anchoredPosition = Vector2.zero;
            coinTxtRt.sizeDelta = Vector2.zero;
            coinsRewardText = coinTxtGo.GetComponent<Text>();
            coinsRewardText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            coinsRewardText.fontSize = 24;
            coinsRewardText.fontStyle = FontStyle.Bold;
            coinsRewardText.alignment = TextAnchor.MiddleCenter;
            coinsRewardText.color = new Color(1f, 0.92f, 0.35f, 1f);
            var coinOutline = coinTxtGo.GetComponent<Outline>();
            coinOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            coinOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Blueprint Reward Card
            var bpCardGo = new GameObject("Blueprint Reward Card", typeof(RectTransform), typeof(Image), typeof(Outline));
            bpCardGo.transform.SetParent(rewardsGo.transform, false);
            var bpCardRt = bpCardGo.GetComponent<RectTransform>();
            bpCardRt.anchorMin = new Vector2(0.5f, 0.5f);
            bpCardRt.anchorMax = new Vector2(0.5f, 0.5f);
            bpCardRt.anchoredPosition = new Vector2(0f, -32f);
            bpCardRt.sizeDelta = new Vector2(480f, 52f);
            var bpBg = bpCardGo.GetComponent<Image>();
            bpBg.color = new Color(0.08f, 0.16f, 0.28f, 0.90f);
            var bpOutline = bpCardGo.GetComponent<Outline>();
            bpOutline.effectColor = new Color(0.38f, 0.88f, 1f, 0.75f);
            bpOutline.effectDistance = new Vector2(1f, -1f);
            blueprintRewardCard = bpCardGo;

            // Blueprint Thumbnail
            var bpThumbGo = new GameObject("Thumbnail", typeof(RectTransform), typeof(Image));
            bpThumbGo.transform.SetParent(bpCardGo.transform, false);
            var bpThumbRt = bpThumbGo.GetComponent<RectTransform>();
            bpThumbRt.anchorMin = new Vector2(0f, 0.5f);
            bpThumbRt.anchorMax = new Vector2(0f, 0.5f);
            bpThumbRt.anchoredPosition = new Vector2(30f, 0f);
            bpThumbRt.sizeDelta = new Vector2(42f, 42f);
            blueprintThumbnail = bpThumbGo.GetComponent<Image>();
            blueprintThumbnail.preserveAspect = true;
            blueprintThumbnail.raycastTarget = false;

            // Blueprint Text
            var bpTxtGo = new GameObject("BP Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            bpTxtGo.transform.SetParent(bpCardGo.transform, false);
            var bpTxtRt = bpTxtGo.GetComponent<RectTransform>();
            bpTxtRt.anchorMin = new Vector2(0.15f, 0f);
            bpTxtRt.anchorMax = new Vector2(0.95f, 1f);
            bpTxtRt.anchoredPosition = Vector2.zero;
            bpTxtRt.sizeDelta = Vector2.zero;
            blueprintRewardText = bpTxtGo.GetComponent<Text>();
            blueprintRewardText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            blueprintRewardText.fontSize = 20;
            blueprintRewardText.fontStyle = FontStyle.Bold;
            blueprintRewardText.alignment = TextAnchor.MiddleCenter;
            blueprintRewardText.color = new Color(0.45f, 0.90f, 1f, 1f);
            var bpTextOutline = bpTxtGo.GetComponent<Outline>();
            bpTextOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            bpTextOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Claim Button
            var btnGo = new GameObject("Claim Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline), typeof(ButtonPressFeedback));
            btnGo.transform.SetParent(boxGo.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, 45f);
            btnRt.sizeDelta = new Vector2(300f, 62f);
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.12f, 0.58f, 0.32f, 0.96f);
            var btnOutline = btnGo.GetComponent<Outline>();
            btnOutline.effectColor = new Color(0.30f, 0.95f, 0.50f, 0.90f);
            btnOutline.effectDistance = new Vector2(1.5f, -1.5f);
            claimButton = btnGo.GetComponent<Button>();
            claimButton.onClick.AddListener(OnClaimClicked);

            var btnTxtGo = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Outline));
            btnTxtGo.transform.SetParent(btnGo.transform, false);
            var btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.anchoredPosition = Vector2.zero;
            btnTxtRt.sizeDelta = Vector2.zero;
            claimButtonText = btnTxtGo.GetComponent<Text>();
            claimButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            claimButtonText.fontSize = 24;
            claimButtonText.fontStyle = FontStyle.Bold;
            claimButtonText.alignment = TextAnchor.MiddleCenter;
            claimButtonText.color = Color.white;
            claimButtonText.text = "CONTINUE";
            var btnTxtOutline = btnTxtGo.GetComponent<Outline>();
            btnTxtOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            btnTxtOutline.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }
}
