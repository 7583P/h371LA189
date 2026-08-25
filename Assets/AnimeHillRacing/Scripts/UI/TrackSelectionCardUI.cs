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

        private Sprite ResolvedLockSprite => lockSprite != null
            ? lockSprite : Resources.Load<Sprite>("UI/LockIcon");

        private int compatibleMapIndex = -1;
        private Action<int> selectionHandler;

        public MapDefinition Map { get; private set; }

        private void Awake()
        {
            ResolveButton();
        }

        private void OnDestroy()
        {
            if (selectionButton != null) selectionButton.onClick.RemoveListener(HandleSelection);
        }

        public void Bind(MapDefinition map, int mapIndex, Action<int> onSelected)
        {
            Map = map;
            compatibleMapIndex = mapIndex;
            selectionHandler = onSelected;
            ResolveButton();

            bool valid = map != null && map.supportedGameMode == GameModeType.Adventure2_5D &&
                         !string.IsNullOrWhiteSpace(map.mapId);
            if (selectionButton != null)
            {
                selectionButton.onClick.RemoveListener(HandleSelection);
                selectionButton.onClick.AddListener(HandleSelection);
                selectionButton.interactable = valid;
            }

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

        private static string DisplayName(MapDefinition map) =>
            !string.IsNullOrWhiteSpace(map?.displayName) ? map.displayName : map?.mapId ?? "TRACK";
    }
}
