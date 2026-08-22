using AnimeHillRacing.Core;
using AnimeHillRacing.TimeTrial;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class ResultsUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text resultsText;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TimeTrialManager timeTrialManager;

        private const float ResultsBackgroundAlpha = .50f;
        private Text photoMetadataText;

        public Text TitleText => titleText;
        public Text StatsText => resultsText;

        private void Awake() => ApplyVisualStyle();

        private void OnEnable() => ApplyVisualStyle();


        public void Show(GameManager manager)
        {
            gameManager = manager;
            ApplyVisualStyle();
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            string reason;
            if (manager.EndReason == RunEndReason.DriverDownAndOutOfFuel)
                reason = "Driver Down & Out Of Fuel";
            else if (manager.EndReason == RunEndReason.OutOfFuel)
                reason = "OUT OF FUEL";
            else
                reason = "DRIVER DOWN";
            if (titleText != null) titleText.text = reason;
            resultsText.text = $"DISTANCE\n<size=34>{manager.Distance:N0} m</size>\n\n" +
                               $"BEST DISTANCE   {manager.BestDistance:N0} m\n" +
                               $"COINS EARNED   +{manager.RunCoins:N0}\n" +
                               $"TOTAL COINS   {manager.SavedTotalCoins:N0}\n\n" +
                               $"FLIPS   {manager.RunFlips}\n" +
                               $"BACKFLIPS   {manager.RunBackflips}\n" +
                               $"STUNT BONUS   {manager.StuntBonus}\n\n" +
                               $"MAX SPEED   {manager.MaxSpeedKmh:0} km/h\n" +
                               $"{GameModeNames.DisplayName(manager.Mode)}   -   {manager.MapId.ToUpperInvariant()}";
            SetPhotoMetadata($"DISTANCE   {manager.Distance:N0} m\nTRACK      {FormatMapName(manager.MapId)}");
        }

        public void ShowTimeTrial(TimeTrialManager manager)
        {
            timeTrialManager = manager;
            ApplyVisualStyle();
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            if (titleText != null) titleText.text = $"FINISHED - {manager.CircuitName}";
            resultsText.text = $"TOTAL TIME\n<size=34>{TimeTrialManager.FormatTime(manager.ElapsedTime)}</size>\n\n" +
                               $"BEST LAP   {TimeTrialManager.FormatTime(manager.BestLap)}\n" +
                               $"MAX SPEED   {manager.MaxSpeedKmh:0} km/h";
            SetPhotoMetadata(null);
        }

        public void HideImmediate()
        {
            if (root != null) root.SetActive(false);
        }

        public void Retry()
        {
            if (timeTrialManager != null) timeTrialManager.Retry(); else gameManager?.Retry();
        }

        public void Menu()
        {
            if (timeTrialManager != null) timeTrialManager.MainMenu(); else gameManager?.MainMenu();
        }

        public void Assign(GameObject targetRoot, Text targetTitle, Text targetText, GameManager manager)
        {
            root = targetRoot;
            titleText = targetTitle;
            resultsText = targetText;
            gameManager = manager;
            ApplyVisualStyle();
        }

        private void ApplyVisualStyle()
        {
            GameObject backgroundRoot = root != null ? root : gameObject;
            Image background = backgroundRoot.GetComponent<Image>();
            if (background != null)
                background.color = new Color(0f, 0f, 0f, ResultsBackgroundAlpha);

            // Styling must never resize the results panel. Its authored 1540x820 bounds intentionally
            // leave the surrounding map visible.
            Outline outline = backgroundRoot.GetComponent<Outline>();
            if (outline != null)
                outline.effectColor = new Color(0f, 0f, 0f, ResultsBackgroundAlpha);

            Transform photoFrame = backgroundRoot.transform.Find("Result Photo Frame");
            if (photoFrame is RectTransform frameRect)
                frameRect.sizeDelta = new Vector2(650f, 650f);

            Transform photo = photoFrame != null ? photoFrame.Find("Result Photo") : null;
            if (photo is not RectTransform photoRect) return;
            AspectRatioFitter fitter = photo.GetComponent<AspectRatioFitter>();
            if (fitter != null) fitter.aspectMode = AspectRatioFitter.AspectMode.None;
            photoRect.anchorMin = new Vector2(.5f, .5f);
            photoRect.anchorMax = new Vector2(.5f, .5f);
            photoRect.anchoredPosition = Vector2.zero;
            photoRect.sizeDelta = new Vector2(610f, 610f);
        }

        private void SetPhotoMetadata(string value)
        {
            Text label = EnsurePhotoMetadata();
            if (label == null) return;
            label.gameObject.SetActive(!string.IsNullOrEmpty(value));
            if (!string.IsNullOrEmpty(value)) label.text = value;
        }

        private Text EnsurePhotoMetadata()
        {
            if (photoMetadataText != null)
            {
                StylePhotoMetadata(photoMetadataText);
                return photoMetadataText;
            }
            GameObject backgroundRoot = root != null ? root : gameObject;
            Transform photoFrame = backgroundRoot.transform.Find("Result Photo Frame");
            if (photoFrame == null) return null;
            Transform existing = photoFrame.Find("Result Photo Metadata");
            if (existing != null) photoMetadataText = existing.GetComponent<Text>();
            if (photoMetadataText != null)
            {
                StylePhotoMetadata(photoMetadataText);
                return photoMetadataText;
            }

            var metadataObject = new GameObject("Result Photo Metadata", typeof(RectTransform), typeof(Text));
            metadataObject.transform.SetParent(photoFrame, false);
            photoMetadataText = metadataObject.GetComponent<Text>();
            StylePhotoMetadata(photoMetadataText);
            Outline outline = metadataObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, .85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            metadataObject.transform.SetAsLastSibling();
            return photoMetadataText;
        }

        private void StylePhotoMetadata(Text label)
        {
            if (label == null) return;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-28f, -24f);
            rect.sizeDelta = new Vector2(360f, 96f);

            label.font = resultsText != null && resultsText.font != null
                ? resultsText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 25;
            label.fontStyle = FontStyle.Bold;
            label.lineSpacing = 1.18f;
            label.alignment = TextAnchor.UpperRight;
            label.color = Color.white;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static string FormatMapName(string mapId) => string.IsNullOrWhiteSpace(mapId)
            ? "UNKNOWN" : mapId.Replace('-', ' ').Replace('_', ' ').ToUpperInvariant();

        public void AssignTimeTrial(TimeTrialManager manager) => timeTrialManager = manager;
    }
}
