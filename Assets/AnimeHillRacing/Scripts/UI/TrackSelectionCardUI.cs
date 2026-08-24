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
            if (statusText != null) statusText.text = valid ? "SELECT" : "UNAVAILABLE";

            ApplyPreview(map as MapProfile);
            RefreshSelectedState();
        }

        public void RefreshSelectedState()
        {
            bool selected = Map != null && Map.mapId == GameSession.SelectedMapId &&
                            GameSession.SelectedGameMode == GameModeType.Adventure2_5D;
            if (selectedMarker != null) selectedMarker.SetActive(selected);
            if (statusText != null && Map != null) statusText.text = selected ? "SELECTED" : "SELECT";
        }

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
