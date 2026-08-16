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

        private void Awake()
        {
            if (useDefaultFuelGradient) ConfigureDefaultFuelGradient();
            RemoveDecorativeCardBackground(distanceText, "Distance Card");
            RemoveDecorativeCardBackground(fuelText, "Fuel Card");
            RemoveDecorativeCardBackground(coinsText, "Coin Balance Card");
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

            if (distanceText != null) distanceText.text = $"DISTANCE\n{gameManager.Distance:N0} m";
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
                if (gameManager.State == RunState.CrashPending)
                    statusText.text = "CRASHED";
                else if (gameManager.ShowGemAward)
                    statusText.text = gameManager.LastGemAward == 1 ? "+1 GEM" : $"+{gameManager.LastGemAward} GEMS";
                else if (gameManager.ShowDistanceBonus)
                    statusText.text = $"DISTANCE BONUS  +{gameManager.LastDistanceBonus}  |  NEXT {gameManager.NextDistanceBonusThreshold:0} m";
                else if (gameManager.State == RunState.OutOfFuelPending)
                    statusText.text = $"OUT OF FUEL  {gameManager.OutOfFuelRemaining:0.0}";
                else if (gameManager.State == RunState.OutOfFuelFinal)
                    statusText.text = "OUT OF FUEL";
                else
                    statusText.text = transmission.ShiftStatus;
            }

            if (showDebug && debugText != null)
            {
                int chunk = terrainManager != null ? terrainManager.CurrentChunkIndex :
                    roadManager != null ? roadManager.CurrentChunkIndex : 0;
                float difficulty = terrainManager != null ? terrainManager.CurrentDifficulty :
                    roadManager != null ? roadManager.CurrentDifficulty : 0f;
                int seed = terrainManager != null && terrainManager.ActiveProfile != null ? terrainManager.ActiveProfile.seed :
                    roadManager != null ? roadManager.Seed : 0;
                string section = terrainManager != null ? terrainManager.CurrentSection.ToString() : "ROAD";
                FuelSystem fuel = vehicle.Fuel;
                RoadVehicleController roadVehicle = vehicle as RoadVehicleController;
                string roadDebug = roadVehicle != null
                    ? $"\nTRACK DISTANCE  {roadVehicle.ProgressPosition:0.0}\n" +
                      $"STEERING INPUT/ANGLE  {roadVehicle.SteeringInput:0.00}/{roadVehicle.ActualSteeringAngle:0.0}°\n" +
                      $"VELOCITY FWD/LAT  {roadVehicle.ForwardVelocity:0.00}/{roadVehicle.LateralVelocity:0.00} m/s\n" +
                      $"WHEEL SLIP  {roadVehicle.WheelSlip:0.00}\nGRIP MULTIPLIER  {roadVehicle.GripMultiplier:0.00}\n" +
                      $"DOWNFORCE  {roadVehicle.CurrentDownforce:0} N"
                    : string.Empty;
                debugText.text = $"RAW SPEED  {vehicle.SpeedMps:0.00} m/s\nRPM  {vehicle.EngineRpm:0}\n" +
                                 $"GEAR  {transmission.CurrentGear}\nFUEL CURRENT/MAX  {(fuel != null ? fuel.CurrentFuel : 0f):0.00}/{(fuel != null ? fuel.MaxFuel : 0f):0.00}\n" +
                                 $"FUEL NORMALIZED  {(fuel != null ? fuel.NormalizedFuel : 0f):0.000}\n" +
                                 $"RAW THROTTLE/BRAKE/STEERING  {(input != null ? input.RawThrottleInput : 0f):0.000}/" +
                                 $"{(input != null ? input.RawBrakeInput : 0f):0.000}/{(input != null ? input.RawSteeringInput : 0f):0.000}\n" +
                                 $"APPLIED THROTTLE  {vehicle.AppliedThrottle:0.000}\n" +
                                 $"RUN FUEL DURATION  {(fuel != null ? fuel.TotalRunDurationSeconds : 0f):0.0}s\n" +
                                 $"CONSUMPTION  {(fuel != null ? fuel.CurrentConsumptionPerSecond : 0f):0.000} units/s ({(fuel != null ? fuel.CurrentConsumptionNormalizedPerSecond : 0f):0.0000}/s)\n" +
                                 $"DISTANCE  {gameManager.Distance:0.0}\nVEHICLE  {vehicle.transform.position}\n" +
                                 $"DIFFICULTY  {difficulty:0.00} {section}\nCHUNK  {chunk}\nSEED  {seed}{roadDebug}";
            }
        }

        public void RefreshImmediate(GameManager gameManager)
        {
            nextRefresh = float.NegativeInfinity;
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
        }

        public void AssignLiquidExposure(GameObject panel, Text label, Image fill)
        {
            liquidExposurePanel = panel;
            liquidExposureText = label;
            liquidExposureFill = fill;
        }

        private static void SetNeedle(RectTransform needle, float normalized, float zeroAngle, float fullAngle)
        {
            if (needle == null) return;
            needle.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Lerp(zeroAngle, fullAngle, Mathf.Clamp01(normalized)));
        }

        public void AssignPickupPool(PickupPool pool) => pickupPool = pool;

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
}
