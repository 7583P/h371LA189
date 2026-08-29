using System;
using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    /// <summary>A reusable, data-bound Adventure track card.</summary>
    public sealed class TrackSelectionCardUI : MonoBehaviour
    {
        [SerializeField] private Button selectionButton;
        [SerializeField] private Image skyImage;
        [SerializeField] private Image environmentImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bestDistanceText;
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject selectedMarker;
        [SerializeField, Tooltip("Optional transparent lock artwork. Assign the final supplied sprite on the card template.")]
        private Sprite lockSprite;

        private Image lockShade;
        private Image lockImage;
        private bool previewLayoutCaptured;
        private Vector2 previewAnchorMin;
        private Vector2 previewAnchorMax;
        private Vector2 previewPivot;
        private Vector2 previewAnchoredPosition;
        private Vector2 previewSizeDelta;
        private AspectRatioFitter previewAspectFitter;
        private bool previewFitterExisted;
        private bool previewFitterEnabled;
        private AspectRatioFitter.AspectMode previewFitterMode;
        private float previewFitterRatio;
        private Button leaderboardButton;
        private Text leaderboardFallbackText;

        private Sprite ResolvedLockSprite => lockSprite != null
            ? lockSprite : Resources.Load<Sprite>("UI/LockIcon");

        private int compatibleMapIndex = -1;
        private Action<int> selectionHandler;

        public MapDefinition Map { get; private set; }

        private void Awake()
        {
            ResolveButton();
            EnsureLeaderboardButton();
        }

        private void OnDestroy()
        {
            if (selectionButton != null) selectionButton.onClick.RemoveListener(HandleSelection);
            if (leaderboardButton != null) leaderboardButton.onClick.RemoveListener(HandleLeaderboard);
        }

        public void Bind(MapDefinition map, int mapIndex, Action<int> onSelected)
        {
            Map = map;
            compatibleMapIndex = mapIndex;
            selectionHandler = onSelected;
            ResolveButton();
            EnsureLeaderboardButton();

            bool valid = map != null && map.supportedGameMode == GameModeType.Adventure2_5D &&
                         !string.IsNullOrWhiteSpace(map.mapId);
            if (selectionButton != null)
            {
                selectionButton.onClick.RemoveListener(HandleSelection);
                selectionButton.onClick.AddListener(HandleSelection);
                selectionButton.interactable = valid;
            }
            if (leaderboardButton != null) leaderboardButton.interactable = valid;

            if (titleText != null)
                titleText.text = valid ? DisplayName(map).ToUpperInvariant() : "UNAVAILABLE TRACK";
            if (bestDistanceText != null)
                bestDistanceText.text = valid
                    ? $"BEST  {SaveManager.GetBestDistance(GameModeType.Adventure2_5D, map.mapId):N0} m"
                    : string.Empty;

            ApplyPreview(map as MapProfile);
            EnsureLockVisual();
            RefreshSelectedState();
        }

        public void RefreshSelectedState()
        {
            bool unlocked = IsUnlocked;
            bool selected = unlocked && Map != null && Map.mapId == GameSession.SelectedMapId &&
                            GameSession.SelectedGameMode == GameModeType.Adventure2_5D;
            if (selectedMarker != null) selectedMarker.SetActive(selected);
            if (lockShade != null) lockShade.enabled = Map != null && !unlocked;
            if (lockImage != null)
            {
                Sprite sprite = ResolvedLockSprite;
                lockImage.sprite = sprite;
                lockImage.enabled = Map != null && !unlocked && sprite != null;
                lockImage.preserveAspect = true;
                lockImage.raycastTarget = false;
            }
            if (statusText != null && Map != null)
                statusText.text = !unlocked ? $"LOCKED\n{Map.coinPrice:N0} COINS" :
                    selected ? "SELECTED" : "SELECT";
        }

        public bool IsUnlocked => Map != null &&
            SaveManager.IsMapUnlocked(Map.mapId, Map.unlockedByDefault);

        public void Assign(Button button, Image preview, Text title, Text bestDistance, Text status,
            GameObject selectionMarker = null, Image sky = null)
        {
            selectionButton = button;
            environmentImage = preview;
            titleText = title;
            bestDistanceText = bestDistance;
            statusText = status;
            selectedMarker = selectionMarker;
            skyImage = sky;
        }

        private void ResolveButton()
        {
            if (selectionButton == null) selectionButton = GetComponent<Button>();
        }

        private void EnsureLeaderboardButton()
        {
            if (leaderboardButton != null || bestDistanceText == null) return;
            Transform existing = transform.Find("Leaderboard Button");
            GameObject buttonObject = existing != null ? existing.gameObject :
                new GameObject("Leaderboard Button", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            if (existing == null) rect.SetParent(transform, false);

            RectTransform bestRect = bestDistanceText.rectTransform;
            float statusStart = statusText != null ? statusText.rectTransform.anchorMin.x : .96f;
            float buttonMax = Mathf.Clamp(statusStart - .01f, .58f, .94f);
            float buttonMin = Mathf.Max(bestRect.anchorMin.x + .14f, buttonMax - .22f);
            Vector2 bestMax = bestRect.anchorMax;
            bestMax.x = Mathf.Min(bestMax.x, buttonMin - .01f);
            bestRect.anchorMax = bestMax;

            rect.anchorMin = new Vector2(buttonMin, Mathf.Max(0f, bestRect.anchorMin.y - .035f));
            rect.anchorMax = new Vector2(buttonMax, Mathf.Min(1f, bestRect.anchorMax.y + .035f));
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image buttonImage = buttonObject.GetComponent<Image>();
            leaderboardButton = buttonObject.GetComponent<Button>();
            leaderboardButton.targetGraphic = buttonImage;
            leaderboardButton.onClick.RemoveListener(HandleLeaderboard);
            leaderboardButton.onClick.AddListener(HandleLeaderboard);

            HillAdventurerUiCatalog catalog = HillAdventurerUiCatalog.Instance;
            Sprite sprite = catalog != null ? catalog.leaderboardButton : null;
            buttonImage.sprite = sprite;
            buttonImage.preserveAspect = sprite != null;
            buttonImage.color = sprite != null ? Color.white : new Color(.08f, .38f, .62f, .96f);

            Transform labelTransform = buttonObject.transform.Find("Fallback Label");
            if (labelTransform == null)
            {
                GameObject labelObject = new GameObject("Fallback Label", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Text));
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.SetParent(buttonObject.transform, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                leaderboardFallbackText = labelObject.GetComponent<Text>();
                leaderboardFallbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                leaderboardFallbackText.fontSize = 13;
                leaderboardFallbackText.resizeTextForBestFit = true;
                leaderboardFallbackText.resizeTextMinSize = 9;
                leaderboardFallbackText.resizeTextMaxSize = 13;
                leaderboardFallbackText.alignment = TextAnchor.MiddleCenter;
                leaderboardFallbackText.color = Color.white;
                leaderboardFallbackText.raycastTarget = false;
                leaderboardFallbackText.text = "RANK";
            }
            else
                leaderboardFallbackText = labelTransform.GetComponent<Text>();
            if (leaderboardFallbackText != null) leaderboardFallbackText.enabled = sprite == null;
            buttonObject.transform.SetAsLastSibling();
        }

        private void HandleLeaderboard()
        {
            if (Map == null) return;
            if (ExternalServiceProviders.Leaderboards.TryShowLeaderboard(Map.mapId,
                    Map.leaderboardId, out string reason)) return;
            if (statusText != null)
            {
                statusText.text = string.IsNullOrWhiteSpace(reason) ? "LEADERBOARD UNAVAILABLE" : reason;
                CancelInvoke(nameof(RefreshSelectedState));
                Invoke(nameof(RefreshSelectedState), 2f);
            }
        }

        private void EnsureLockVisual()
        {
            Transform preview = environmentImage != null ? environmentImage.transform.parent : transform;
            if (lockShade == null)
            {
                Transform existing = preview.Find("Locked Shade");
                GameObject shadeObject = existing != null ? existing.gameObject :
                    new GameObject("Locked Shade", typeof(RectTransform), typeof(Image));
                RectTransform rect = shadeObject.GetComponent<RectTransform>();
                if (existing == null) rect.SetParent(preview, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                lockShade = shadeObject.GetComponent<Image>();
                lockShade.color = new Color(0f, 0f, 0f, .48f);
                lockShade.raycastTarget = false;
            }
            if (lockImage == null)
            {
                Transform existing = preview.Find("Lock Icon");
                GameObject iconObject = existing != null ? existing.gameObject :
                    new GameObject("Lock Icon", typeof(RectTransform), typeof(Image));
                RectTransform rect = iconObject.GetComponent<RectTransform>();
                if (existing == null) rect.SetParent(preview, false);
                rect.anchorMin = new Vector2(.38f, .24f);
                rect.anchorMax = new Vector2(.62f, .76f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                lockImage = iconObject.GetComponent<Image>();
                lockImage.color = Color.white;
                lockImage.raycastTarget = false;
                lockImage.preserveAspect = true;
            }
            lockShade.transform.SetAsLastSibling();
            lockImage.transform.SetAsLastSibling();
        }

        private void HandleSelection()
        {
            if (Map == null || compatibleMapIndex < 0) return;
            selectionHandler?.Invoke(compatibleMapIndex);
        }

        private void ApplyPreview(MapProfile profile)
        {
            if (profile != null && profile.selectionPreview != null)
            {
                SetSprite(skyImage, null);
                SetSprite(environmentImage, profile.selectionPreview);
                ConfigureDedicatedPreview(profile.selectionPreview);
                return;
            }

            RestoreLayeredPreviewLayout();
            Sprite sky = profile?.environment != null ? profile.environment.sky : null;
            Sprite environment = profile?.environment != null
                ? profile.environment.mountains ?? profile.environment.nearMountains ??
                  profile.environment.farMountains ?? profile.environment.sky
                : null;
            SetSprite(skyImage, sky);
            SetSprite(environmentImage, environment);
        }

        private static void SetSprite(Image target, Sprite sprite)
        {
            if (target == null) return;
            target.sprite = sprite;
            target.enabled = sprite != null;
            target.preserveAspect = true;
            target.raycastTarget = false;
        }

        private void ConfigureDedicatedPreview(Sprite sprite)
        {
            if (environmentImage == null || sprite == null) return;
            CapturePreviewLayout();

            RectTransform rect = environmentImage.rectTransform;
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            if (previewAspectFitter == null)
                previewAspectFitter = environmentImage.gameObject.AddComponent<AspectRatioFitter>();
            previewAspectFitter.enabled = false;
            previewAspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            previewAspectFitter.aspectRatio = Mathf.Max(.01f, sprite.rect.width / sprite.rect.height);
            previewAspectFitter.enabled = true;
        }

        private void CapturePreviewLayout()
        {
            if (previewLayoutCaptured || environmentImage == null) return;
            RectTransform rect = environmentImage.rectTransform;
            previewAnchorMin = rect.anchorMin;
            previewAnchorMax = rect.anchorMax;
            previewPivot = rect.pivot;
            previewAnchoredPosition = rect.anchoredPosition;
            previewSizeDelta = rect.sizeDelta;

            previewAspectFitter = environmentImage.GetComponent<AspectRatioFitter>();
            previewFitterExisted = previewAspectFitter != null;
            if (previewFitterExisted)
            {
                previewFitterEnabled = previewAspectFitter.enabled;
                previewFitterMode = previewAspectFitter.aspectMode;
                previewFitterRatio = previewAspectFitter.aspectRatio;
            }
            previewLayoutCaptured = true;
        }

        private void RestoreLayeredPreviewLayout()
        {
            if (!previewLayoutCaptured || environmentImage == null) return;
            if (previewAspectFitter != null) previewAspectFitter.enabled = false;

            RectTransform rect = environmentImage.rectTransform;
            rect.anchorMin = previewAnchorMin;
            rect.anchorMax = previewAnchorMax;
            rect.pivot = previewPivot;
            rect.anchoredPosition = previewAnchoredPosition;
            rect.sizeDelta = previewSizeDelta;

            if (previewFitterExisted && previewAspectFitter != null)
            {
                previewAspectFitter.aspectMode = previewFitterMode;
                previewAspectFitter.aspectRatio = previewFitterRatio;
                previewAspectFitter.enabled = previewFitterEnabled;
            }
        }

        private static string DisplayName(MapDefinition map) =>
            !string.IsNullOrWhiteSpace(map?.displayName) ? map.displayName : map?.mapId ?? "TRACK";
    }
}
