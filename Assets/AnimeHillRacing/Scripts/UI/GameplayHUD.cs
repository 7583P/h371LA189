using AnimeHillRacing.Core;
using AnimeHillRacing.Pickups;
using AnimeHillRacing.Road;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class GameplayHUD : MonoBehaviour
    {
        [SerializeField] private Text distanceText;
        [SerializeField] private Text bestText;
        [SerializeField] private Text coinsText;
        [SerializeField] private Image coinIcon;
        [SerializeField] private Text nextFuelText;
        [SerializeField] private Image nextFuelIcon;
        [SerializeField] private GameObject nextFuelPanel;
        [SerializeField] private Text fuelText;
        [SerializeField] private Image fuelIcon;
        [SerializeField] private Image fuelFill;
        [SerializeField] private Image lowFuelWarning;
        [SerializeField, Min(0f)] private float lowFuelWarningSeconds = 5f;
        [SerializeField] private GameObject liquidExposurePanel;
        [SerializeField] private Text liquidExposureText;
        [SerializeField] private Image liquidExposureFill;
        [SerializeField] private Gradient fuelGradient = new Gradient();
        [SerializeField] private bool useDefaultFuelGradient = true;
        [SerializeField] private RectTransform speedNeedle;
        [SerializeField] private RectTransform rpmNeedle;
        [SerializeField] private float speedNeedleZeroAngle = 172f;
        [SerializeField] private float speedNeedleFullAngle = -63f;
        [SerializeField] private float rpmNeedleZeroAngle = 159f;
        [SerializeField] private float rpmNeedleFullAngle = -49f;
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private Text debugText;
        [SerializeField] private VehicleInputController input;
        [SerializeField, Min(0.02f)] private float refreshInterval = 0.10f;

        private TerrainManager terrainManager;
        private RoadTrackGenerator roadManager;
        private PickupPool pickupPool;
        private float nextRefresh;
        private bool showDebug;
        private RectTransform fuelFillRect;
        private Vector2 fuelFullAnchorMax;
        private Vector2 fuelFullOffsetMax;
        private Vector2 fuelEmptyOffsetMax;
        private int displayedDistance;

        private void Awake()
        {
            if (useDefaultFuelGradient) ConfigureDefaultFuelGradient();
            RemoveDecorativeCardBackground(distanceText, "Distance Card");
            RemoveDecorativeCardBackground(fuelText, "Fuel Card");
            RemoveDecorativeCardBackground(coinsText, "Coin Balance Card");
            RemoveDecorativeCardBackground(bestText, "Best Card");
            AddTextOutline(distanceText);
            AddTextOutline(fuelText);
            AddTextOutline(coinsText);
            AddTextOutline(bestText);
            terrainManager = FindFirstObjectByType<TerrainManager>();
            roadManager = FindFirstObjectByType<RoadTrackGenerator>();
            if (input == null)
                input = FindFirstObjectByType<VehicleInputController>();
            if (nextFuelPanel == null && nextFuelText != null && nextFuelText.transform.parent != null)
                nextFuelPanel = nextFuelText.transform.parent.gameObject;
            if (fuelFill != null)
            {
                fuelFillRect = fuelFill.rectTransform;
                fuelFullAnchorMax = fuelFillRect.anchorMax;
                fuelFullOffsetMax = fuelFillRect.offsetMax;
                fuelEmptyOffsetMax = fuelFullOffsetMax;
                fuelEmptyOffsetMax.x = fuelFillRect.offsetMin.x;
            }
            debugPanel?.SetActive(false);
            // Long-distance teleport belongs to the procedural 2D gameplay. Road/3D tracks use
            // a different coordinate system and intentionally do not expose this control.
            if (terrainManager != null)
            {
                teleportMenu = GameplayTeleportMenu.Create(transform,
                    distanceText != null ? distanceText.font : null);
                EnsureTeleportButton();
            }
            EnsureLowFuelWarning();
            SetLowFuelWarningVisible(false);
        }

        private GameplayTeleportMenu teleportMenu;

        private void EnsureTeleportButton()
        {
            if (transform.Find("Teleport Button") != null) return;

            GameObject buttonObj = new GameObject("Teleport Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(transform, false);

            Canvas btnCanvas = buttonObj.AddComponent<Canvas>();
            btnCanvas.overrideSorting = true;
            btnCanvas.sortingOrder = 50;
            buttonObj.AddComponent<GraphicRaycaster>();

            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-330f, -22f);
            rect.sizeDelta = new Vector2(185f, 44f);

            Image img = buttonObj.GetComponent<Image>();
            img.color = new Color(0.08f, 0.12f, 0.25f, 0.95f);
            img.raycastTarget = true;

            Outline outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.85f, 1f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            Text label = textObj.GetComponent<Text>();
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (distanceText != null) defaultFont = distanceText.font;
            label.font = defaultFont;
            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 0.94f, 0.25f, 1f);
            label.text = "TELEPORT";
            label.raycastTarget = false;

            Button btn = buttonObj.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(.72f, .72f, .72f, 1f);
            colors.selectedColor = colors.highlightedColor;
            btn.colors = colors;
            btn.onClick.AddListener(OnTeleportClicked);

            buttonObj.AddComponent<ButtonPressFeedback>();
        }

        public void OnTeleportClicked()
        {
            teleportMenu?.Toggle();
        }

        private static void RemoveDecorativeCardBackground(Text content, string expectedCardName)
        {
            if (content == null || content.transform.parent == null) return;
            GameObject card = content.transform.parent.gameObject;
            if (card.name != expectedCardName) return;

            Image background = card.GetComponent<Image>();
            if (background != null) background.enabled = false;
            Outline outline = card.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
        }

        private void Update()
        {
            if (input != null && input.ToggleDebugPressedThisFrame)
            {
                showDebug = !showDebug;
                debugPanel?.SetActive(showDebug);
            }
        }

        public void Refresh(GameManager gameManager)
        {
            if (Time.unscaledTime < nextRefresh || gameManager.Vehicle == null)
                return;
            nextRefresh = Time.unscaledTime + refreshInterval;
            VehicleRuntimeBase vehicle = gameManager.Vehicle;
            TransmissionController transmission = vehicle.Transmission;

            if (distanceText != null)
            {
                int realMeters = Mathf.RoundToInt(gameManager.Distance);
                if (realMeters != displayedDistance)
                {
                    displayedDistance = realMeters;
                    distanceText.text = $"{displayedDistance:N0} m";
                }
            }
            if (bestText != null) bestText.text = $"BEST\n{Mathf.Max(gameManager.BestDistance, gameManager.Distance):N0} m";
            if (coinsText != null) coinsText.text = $"{gameManager.DisplayCoins:N0}";
            if (nextFuelText != null)
            {
                float progress = vehicle.ProgressPosition;
                float fuelDistance = 0f;
                bool showNextFuel = pickupPool != null &&
                    pickupPool.TryGetNextFuelDistance(progress, out fuelDistance) && fuelDistance <= 200f;
                if (nextFuelPanel != null) nextFuelPanel.SetActive(showNextFuel);
                if (showNextFuel)
                    nextFuelText.text = $"NEXT FUEL   {Mathf.CeilToInt(fuelDistance):N0} m";
            }
            FuelSystem runtimeFuel = vehicle.Fuel;
            float normalizedFuel = runtimeFuel != null ? runtimeFuel.NormalizedFuel : 0f;
            SetLowFuelWarningVisible(runtimeFuel != null && ShouldDisplayLowFuel(
                runtimeFuel.CurrentFuel, runtimeFuel.CurrentConsumptionPerSecond,
                lowFuelWarningSeconds));
            if (fuelText != null) fuelText.text = $"{normalizedFuel * 100f:0}%";
            if (fuelFill != null)
            {
                fuelFill.fillAmount = normalizedFuel;
                fuelFill.color = fuelGradient.Evaluate(normalizedFuel);
                if (fuelFill.sprite == null && fuelFillRect != null)
                {
                    Vector2 anchorMax = fuelFullAnchorMax;
                    anchorMax.x = Mathf.Lerp(fuelFillRect.anchorMin.x, fuelFullAnchorMax.x, normalizedFuel);
                    fuelFillRect.anchorMax = anchorMax;
                    fuelFillRect.offsetMax = Vector2.Lerp(fuelEmptyOffsetMax, fuelFullOffsetMax, normalizedFuel);
                }
            }
            if (liquidExposurePanel != null)
                liquidExposurePanel.SetActive(gameManager.SupportsLiquidExposure);
            if (gameManager.SupportsLiquidExposure)
            {
                float exposure = gameManager.TotalLiquidExposureThisRun;
                float limit = gameManager.LiquidExposureLimit;
                if (liquidExposureText != null)
                    liquidExposureText.text = $"LIQUID  {exposure:0.0} / {limit:0.0}s";
                if (liquidExposureFill != null)
                    liquidExposureFill.fillAmount = Mathf.Clamp01(exposure / Mathf.Max(.01f, limit));
            }
            SetNeedle(speedNeedle, Mathf.Abs(vehicle.SpeedKmh) / 400f,
                speedNeedleZeroAngle, speedNeedleFullAngle);
            SetNeedle(rpmNeedle, vehicle.EngineRpm / 10000f,
                rpmNeedleZeroAngle, rpmNeedleFullAngle);
            if (statusText != null)
            {
                if (gameManager.State == RunState.CrashPending || gameManager.State == RunState.OutOfFuelPending)
                {
                    if (gameManager.EndReason == RunEndReason.DriverDownAndOutOfFuel)
                        statusText.text = $"DRIVER DOWN & OUT OF FUEL  {gameManager.GameOverRemaining:0.0}";
                    else if (gameManager.EndReason == RunEndReason.OutOfFuel)
                        statusText.text = $"OUT OF FUEL  {gameManager.GameOverRemaining:0.0}";
                    else
                        statusText.text = $"DRIVER DOWN  {gameManager.GameOverRemaining:0.0}";
                }
                else if (gameManager.State == RunState.OutOfFuelFinal)
                {
                    statusText.text = gameManager.EndReason == RunEndReason.DriverDownAndOutOfFuel
                        ? "DRIVER DOWN & OUT OF FUEL" : "OUT OF FUEL";
                }
                else if (gameManager.ShowGemAward)
                    statusText.text = gameManager.LastGemAward == 1 ? "+1 GEM" : $"+{gameManager.LastGemAward} GEMS";
                else if (gameManager.ShowDistanceBonus)
                    statusText.text = $"DISTANCE BONUS  +{gameManager.LastDistanceBonus}  |  NEXT {gameManager.NextDistanceBonusThreshold:0} m";
                else
                    statusText.text = transmission.ShiftStatus;
            }
        }

        public void RefreshImmediate(GameManager gameManager)
        {
            nextRefresh = float.NegativeInfinity;
            // Force the distance label to be written instead of preloading the same cache value.
            displayedDistance = int.MinValue;
            Refresh(gameManager);
        }

        public void Assign(Text distance, Text best, Text coins, Image targetCoinIcon, Text nextFuel,
            Image targetNextFuelIcon, Text fuelLabel, Image targetFuelIcon, Image fuelBar,
            RectTransform targetSpeedNeedle, RectTransform targetRpmNeedle,
            Text status, GameObject targetDebugPanel, Text targetDebugText,
            VehicleInputController targetInput, GameObject targetNextFuelPanel = null)
        {
            distanceText = distance;
            bestText = best;
            coinsText = coins;
            coinIcon = targetCoinIcon;
            nextFuelText = nextFuel;
            nextFuelIcon = targetNextFuelIcon;
            nextFuelPanel = targetNextFuelPanel;
            fuelText = fuelLabel;
            fuelIcon = targetFuelIcon;
            fuelFill = fuelBar;
            speedNeedle = targetSpeedNeedle;
            rpmNeedle = targetRpmNeedle;
            statusText = status;
            debugPanel = targetDebugPanel;
            debugText = targetDebugText;
            input = targetInput;
            RemoveDecorativeCardBackground(distanceText, "Distance Card");
            RemoveDecorativeCardBackground(fuelText, "Fuel Card");
            RemoveDecorativeCardBackground(coinsText, "Coin Balance Card");
            RemoveDecorativeCardBackground(bestText, "Best Card");
            AddTextOutline(distanceText);
            AddTextOutline(fuelText);
            AddTextOutline(coinsText);
            AddTextOutline(bestText);
        }

        public void AssignLiquidExposure(GameObject panel, Text label, Image fill)
        {
            liquidExposurePanel = panel;
            liquidExposureText = label;
            liquidExposureFill = fill;
        }

        public void AssignLowFuelWarning(Image warning)
        {
            if (warning == lowFuelWarning) return;
            if (lowFuelWarning != null) lowFuelWarning.gameObject.SetActive(false);
            lowFuelWarning = warning;
            SetLowFuelWarningVisible(false);
        }

        public static bool ShouldDisplayLowFuel(float currentFuel,
            float currentConsumptionPerSecond, float warningSeconds = 5f)
        {
            if (currentFuel <= 0f || currentConsumptionPerSecond <= 0f || warningSeconds < 0f ||
                float.IsNaN(currentFuel) || float.IsInfinity(currentFuel) ||
                float.IsNaN(currentConsumptionPerSecond) ||
                float.IsInfinity(currentConsumptionPerSecond) ||
                float.IsNaN(warningSeconds) || float.IsInfinity(warningSeconds))
                return false;

            float remainingSeconds = currentFuel / currentConsumptionPerSecond;
            return remainingSeconds <= warningSeconds;
        }

        private static void SetNeedle(RectTransform needle, float normalized, float zeroAngle, float fullAngle)
        {
            if (needle == null) return;
            needle.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Lerp(zeroAngle, fullAngle, Mathf.Clamp01(normalized)));
        }

        public void AssignPickupPool(PickupPool pool) => pickupPool = pool;

        public void ResetDisplayedDistance() => displayedDistance = int.MinValue;

        private void EnsureLowFuelWarning()
        {
            if (lowFuelWarning != null) return;

            Transform existing = transform.Find("Low Fuel Warning");
            if (existing != null)
                lowFuelWarning = existing.GetComponent<Image>();
            if (lowFuelWarning != null) return;

            GameObject warningObject = new GameObject("Low Fuel Warning",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            warningObject.transform.SetParent(transform, false);
            RectTransform warningRect = warningObject.GetComponent<RectTransform>();
            warningRect.anchorMin = new Vector2(0.5f, 1f);
            warningRect.anchorMax = new Vector2(0.5f, 1f);
            warningRect.pivot = new Vector2(0.5f, 1f);
            warningRect.anchoredPosition = new Vector2(0f, -110f);
            warningRect.sizeDelta = new Vector2(280f, 84f);

            lowFuelWarning = warningObject.GetComponent<Image>();
            lowFuelWarning.color = new Color(0.78f, 0.025f, 0.015f, 0.92f);
            lowFuelWarning.raycastTarget = false;

            GameObject labelObject = new GameObject("Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(warningObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelObject.GetComponent<Text>();
            label.font = distanceText != null && distanceText.font != null
                ? distanceText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 28;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "LOW FUEL";
            label.raycastTarget = false;
            AddTextOutline(label);
        }

        private void SetLowFuelWarningVisible(bool visible)
        {
            if (lowFuelWarning != null && lowFuelWarning.gameObject.activeSelf != visible)
                lowFuelWarning.gameObject.SetActive(visible);
        }

        private static void AddTextOutline(Text text)
        {
            if (text == null) return;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (text.GetComponent<Outline>() == null)
            {
                Outline outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        private void ConfigureDefaultFuelGradient()
        {
            fuelGradient.SetKeys(new[]
            {
                new GradientColorKey(new Color(0.72f, 0.01f, 0.01f), 0f),
                new GradientColorKey(new Color(0.95f, 0.08f, 0.02f), 0.10f),
                new GradientColorKey(new Color(1f, 0.38f, 0.02f), 0.20f),
                new GradientColorKey(new Color(1f, 0.82f, 0.05f), 0.40f),
                new GradientColorKey(new Color(0.55f, 0.85f, 0.08f), 0.60f),
                new GradientColorKey(new Color(0.08f, 0.86f, 0.22f), 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        }
    }

    /// <summary>
    /// Runtime mouse/touch menu for the supported long-distance destinations.
    /// It is created under the existing gameplay Canvas, so scenes need no migration.
    /// </summary>
    internal sealed class GameplayTeleportMenu : MonoBehaviour
    {
        private GameObject modalContainer;
        private bool isOpen;
        private float previousTimeScale = 1f;

        public static GameplayTeleportMenu Create(Transform parent, Font preferredFont)
        {
            Canvas rootCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            Transform targetParent = rootCanvas != null ? rootCanvas.transform : parent;
            GameObject root = new GameObject("Gameplay Teleport Menu", typeof(RectTransform),
                typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(targetParent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            GameplayTeleportMenu menu = root.AddComponent<GameplayTeleportMenu>();
            menu.Build(preferredFont);
            return menu;
        }

        private void Build(Font preferredFont)
        {
            Font font = preferredFont != null
                ? preferredFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            modalContainer = CreateImageObject("Modal Blocker", transform,
                new Color(0f, 0f, .04f, .76f), Vector2.zero, Vector2.one);
            GameObject window = CreateImageObject("Window", modalContainer.transform,
                new Color(.035f, .065f, .13f, .99f), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.sizeDelta = new Vector2(570f, 300f);
            Outline windowOutline = window.AddComponent<Outline>();
            windowOutline.effectColor = new Color(.12f, .86f, 1f, .98f);
            windowOutline.effectDistance = new Vector2(3f, -3f);

            CreateText(window.transform, "Title", "TELEPORT", 30, FontStyle.Bold,
                new Vector2(0f, 102f), new Vector2(480f, 48f), Color.white, font);
            CreateText(window.transform, "Instructions", "SELECT A DISTANCE", 17, FontStyle.Bold,
                new Vector2(0f, 58f), new Vector2(480f, 30f),
                new Color(.55f, .88f, 1f, 1f), font);

            CreateTeleportButton(window.transform, "Teleport 131K", "131K", -180f, 131000f, font);
            CreateTeleportButton(window.transform, "Teleport 262K", "262K", 0f, 262000f, font);
            CreateTeleportButton(window.transform, "Teleport 544K", "544K", 180f, 544000f, font);
            CreateButton(window.transform, "Close", "CLOSE", new Vector2(0f, -105f),
                new Vector2(210f, 44f), new Color(.55f, .10f, .14f, 1f), font, Close, 17);

            modalContainer.SetActive(false);
        }

        private void CreateTeleportButton(Transform parent, string name, string label,
            float x, float distance, Font font)
        {
            CreateButton(parent, name, label, new Vector2(x, -12f), new Vector2(160f, 76f),
                new Color(.06f, .46f, .72f, 1f), font, () => Teleport(distance), 24);
        }

        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (modalContainer == null) return;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isOpen = true;
            modalContainer.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            Time.timeScale = previousTimeScale > .001f ? previousTimeScale : 1f;
            if (modalContainer != null) modalContainer.SetActive(false);
        }

        private void Teleport(float distance)
        {
            Close();
            GameManager manager = GameManager.Instance != null
                ? GameManager.Instance
                : FindFirstObjectByType<GameManager>();
            manager?.TeleportTo(distance);
        }

        private void OnDestroy()
        {
            if (isOpen) Time.timeScale = previousTimeScale > .001f ? previousTimeScale : 1f;
        }

        private static GameObject CreateImageObject(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(Image));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = target.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return target;
        }

        private static Text CreateText(Transform parent, string name, string content, int size,
            FontStyle style, Vector2 position, Vector2 dimensions, Color color, Font font)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(Text));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = target.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 position, Vector2 dimensions, Color color, Font font,
            UnityEngine.Events.UnityAction onClick, int fontSize)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Image image = target.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            Button button = target.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(.72f, .72f, .72f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(onClick);
            target.AddComponent<ButtonPressFeedback>();
            CreateText(target.transform, "Label", label, fontSize, FontStyle.Bold,
                Vector2.zero, dimensions, Color.white, font);
            return button;
        }
    }
}
