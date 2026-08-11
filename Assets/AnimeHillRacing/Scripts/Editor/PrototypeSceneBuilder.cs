#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AnimeHillRacing.CameraSystem;
using AnimeHillRacing.Core;
using AnimeHillRacing.Pickups;
using AnimeHillRacing.Road;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.TimeTrial;
using AnimeHillRacing.UI;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string Root = "Assets/AnimeHillRacing";
        private const string Scenes = Root + "/Scenes";
        private const string MainMenuPath = Scenes + "/MainMenu.unity";
        private const string PreStartPath = Scenes + "/PreStart.unity";
        private const string GaragePath = Scenes + "/Garage.unity";
        private const string Adventure2DPath = Scenes + "/Adventure2_5D.unity";
        private const string Adventure3DPath = Scenes + "/Adventure3D.unity";
        private const string TimeTrialPath = Scenes + "/TimeTrial3D.unity";
        private const string Prefabs = Root + "/Prefabs/Vehicles";
        private const string JeepBodyPath = "Assets/Art/Vehicles/Jeep/Skins/Jeep_Skin_Default.png";
        private const string JeepWheel01Path = "Assets/Art/Vehicles/Jeep/Wheels/Jeep_Wheel_01.png";
        private const string JeepWheel02Path = "Assets/Art/Vehicles/Jeep/Wheels/Jeep_Wheel_02.png";
        private const string FormulaBodyPath = "Assets/Art/Vehicles/Formula/Skins/Formula_skin_Default.png";
        private const string FormulaWheel01Path = "Assets/Art/Vehicles/Formula/Wheels/Formula_Wheel_01.png";
        private const string FormulaWheel02Path = "Assets/Art/Vehicles/Formula/Wheels/Formula_Wheel_02.png";
        private const string FormulaReferencePath = "Assets/Art/Vehicles/Formula/Formula_Reference.png";
        private const string RallyBodyPath = "Assets/Art/Vehicles/Rally/Vehicle/Rally_Skin_Default.png";
        private const string RallyNoRoofPath = "Assets/Art/Vehicles/Rally/Vehicle/Rally_No_Roof_01.png";
        private const string RallyRoofPath = "Assets/Art/Vehicles/Rally/Vehicle/Rally_Roof_01.png";
        private const string RallyWheelPath = "Assets/Art/Vehicles/Rally/Wheels/Rally_Wheel_01.png";
        private const string SportBikeBodyPath = "Assets/Art/Vehicles/Sport Bike/Vehicle/Bike_Skin_Default.png";
        private const string SportBikeRetroPath = "Assets/Art/Vehicles/Sport Bike/Vehicle/Bike_Retro.png";
        private const string SportBikeWheel01Path = "Assets/Art/Vehicles/Sport Bike/Wheels/Bike_Wheel_01.png";
        private const string SportBikeWheel02Path = "Assets/Art/Vehicles/Sport Bike/Wheels/Bike_Wheel_02.png";
        private const string SportBikeGoldWheelPath = "Assets/Art/Vehicles/Sport Bike/Wheels/Gold_Wheel.png";
        private const string NeonRoot = "Assets/Art/Backgrounds/Neon_CIty";
        private const string NeonSkyPath = NeonRoot + "/Sky.png";
        private const string NeonBackgroundPath = NeonRoot + "/Background.png";
        private const string NeonTerrainPath = NeonRoot + "/Terrain.png";
        private const string NeonGroundPath = NeonRoot + "/Ground.png";
        private const string NeonLiquidPath = NeonRoot + "/Liquid.png";
        private const string NeonCautionPath = NeonRoot + "/Caution.png";
        private const string ButtonRoot = "Assets/Art/Buttons";
        private const string MainMenuBackgroundPath = ButtonRoot + "/mainmenu.png";
        private const string HomeButtonPath = ButtonRoot + "/home.png";
        private const string RaceButtonPath = ButtonRoot + "/race.png";
        private const string SettingsButtonPath = ButtonRoot + "/settings.png";
        private const string SkinsButtonPath = ButtonRoot + "/Skins.png";
        private const string StoreButtonPath = ButtonRoot + "/store.png";
        private const string TuningButtonPath = ButtonRoot + "/tuning.png";
        private const string UpgradeButtonPath = ButtonRoot + "/upgrade.png";
        private const string VehicleButtonPath = ButtonRoot + "/vehicle_button.png";
        private const string GameUiRoot = "Assets/Art/Game";
        private const string BrakePedalPath = GameUiRoot + "/brake.png";
        private const string BrakePedalPressedPath = GameUiRoot + "/brake_push.png";
        private const string GasPedalPath = GameUiRoot + "/gas.png";
        private const string GasPedalPressedPath = GameUiRoot + "/gas_push.png";
        private const string RpmGaugePath = GameUiRoot + "/rpm.png";
        private const string RpmNeedlePath = GameUiRoot + "/rpm_arrow.png";
        private const string SpeedGaugePath = GameUiRoot + "/kmh.png";
        private const string SpeedNeedlePath = GameUiRoot + "/kmh_arrow.png";
        private const string FuelCanPath = "Assets/Art/Collectibles/Fuel/Fuel_Can_Default.png";
        private const string Coin01Path = "Assets/Art/Collectibles/Coins/Coin_01.png";
        private const string Coin05Path = "Assets/Art/Collectibles/Coins/Coin_05.png";
        private const string DriverRoot = "Assets/Art/Characters/Stickman_Black";
        private const string DriverPreviewPath = DriverRoot + "/Stickman_Black_Preview.png";
        private const string DriverHeadPath = DriverRoot + "/Head/Stickman_Black_Head.png";
        private const string DriverTorsoPath = DriverRoot + "/Torso/Stickman_Black_Torso.png";
        private const string DriverUpperArmPath = DriverRoot + "/Arms/Stickman_Black_UpperArm.png";
        private const string DriverForearmPath = DriverRoot + "/Arms/Stickman_Black_Forearm.png";
        private const string DriverThighPath = DriverRoot + "/Legs/Stickman_Black_Thigh.png";
        private const string DriverLowerLegPath = DriverRoot + "/Legs/Stickman_Black_LowerLeg.png";
        private const string CountrysideSkyPath = "Assets/Art/Backgrounds/Countryside/Countryside_Sky.png";
        private const string CountrysideMountainsPath = "Assets/Art/Backgrounds/Countryside/Countryside_Mountains.png";
        private static Font font;

        private sealed class BuiltAssets
        {
            public VehicleDefinition jeep;
            public VehicleDefinition formula;
            public VehicleDefinition rally;
            public VehicleDefinition sportBike;
            public Sprite formulaBody;
            public Sprite formulaWheel01;
            public Sprite formulaWheel02;
            public Sprite jeepBody;
            public Sprite jeepWheel01;
            public Sprite jeepWheel02;
            public Sprite rallyBody;
            public Sprite rallyNoRoof;
            public Sprite rallyRoof;
            public Sprite rallyWheel;
            public Sprite sportBikeBody;
            public Sprite sportBikeRetro;
            public Sprite sportBikeWheel01;
            public Sprite sportBikeWheel02;
            public Sprite sportBikeGoldWheel;
            public Sprite neonSky;
            public Sprite neonBackground;
            public Sprite neonTerrain;
            public Sprite neonGround;
            public Sprite neonLiquid;
            public Sprite neonCaution;
            public Sprite mainMenuBackground;
            public Sprite homeButton;
            public Sprite raceButton;
            public Sprite settingsButton;
            public Sprite skinsButton;
            public Sprite storeButton;
            public Sprite tuningButton;
            public Sprite upgradeButton;
            public Sprite vehicleButton;
            public Sprite brakePedal;
            public Sprite brakePedalPressed;
            public Sprite gasPedal;
            public Sprite gasPedalPressed;
            public Sprite rpmGauge;
            public Sprite rpmNeedle;
            public Sprite speedGauge;
            public Sprite speedNeedle;
            public Sprite fuelCan;
            public Sprite coin01;
            public Sprite coin05;
            public DriverDefinition stickmanBlack;
            public EnvironmentDefinition countrysideEnvironment;
            public EnvironmentDefinition neonCityEnvironment;
            public MapProfile countryside;
            public MapProfile neonCity;
            public CoinRouteDefinition countrysideCoinRoute;
            public TerrainElevationProfile elevation;
            public RoadCurvatureProfile curvature;
            public RoadMapProfile countrysideRoad;
            public CircuitDefinition testCircuit;
            public UpgradeEconomyDefinition economy;
            public GameCatalog catalog;
            public Material countrysideMaterial;
            public Material countrysideEdgeMaterial;
            public Material neonCityMaterial;
            public Material neonCityEdgeMaterial;
            public Material nearMountains;
            public Material farMountains;
            public Material road;
            public Material grass;
            public Material shortcut;
            public PhysicsMaterial chassisPhysics;
            public PhysicsMaterial groundPhysics;
            public PhysicsMaterial tirePhysics;
        }

        [MenuItem("Anime Hill Racing/Build All Game Scenes _F8")]
        public static void BuildAllGameScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play Mode before rebuilding scenes.");
                return;
            }
            string previousScenePath = SceneManager.GetActiveScene().path;
            try
            {
                BuildAllGameScenesInternal();
            }
            catch
            {
                string recoveryPath = !string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)
                    ? previousScenePath
                    : File.Exists(MainMenuPath) ? MainMenuPath : string.Empty;
                if (!string.IsNullOrEmpty(recoveryPath))
                    EditorSceneManager.OpenScene(recoveryPath, OpenSceneMode.Single);
                throw;
            }
        }

        [MenuItem("Anime Hill Racing/Rebuild Neon Recovery Content")]
        public static void RebuildNeonRecoveryContent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Exit Play Mode before rebuilding recovery content.");
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            ValidateCustomizationContract(assets);
            ValidateNeonCityContract(assets);
            ValidateVehiclePhysicsContract(assets);
            ValidateTuningContract(assets);
            ValidateVehiclePerformance(assets);
            ValidateSuspensionContract(assets);
            ValidateFuelSequenceContract(assets);
            ValidateCoinRouteContract(assets);
            BuildGarageModern(assets);
            BuildMainMenu(assets);
            BuildPreStartModern(assets);
            BuildAdventure2_5D(assets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            Debug.Log("NEON_RECOVERY_BUILD_OK: four required scenes rebuilt and validated.");
        }

        private static void BuildAllGameScenesInternal()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            ValidateCustomizationContract(assets);
            ValidateVehiclePhysicsContract(assets);
            ValidateFuelSequenceContract(assets);
            ValidateCoinRouteContract(assets);
            BuildGarageModern(assets);
            BuildMainMenu(assets);
            BuildPreStartModern(assets);
            BuildAdventure2_5D(assets);
            BuildAdventure3D(assets);
            BuildTimeTrial3D(assets);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(GaragePath, true),
                new EditorBuildSettingsScene(PreStartPath, true),
                new EditorBuildSettingsScene(Adventure2DPath, true),
                new EditorBuildSettingsScene(Adventure3DPath, true),
                new EditorBuildSettingsScene(TimeTrialPath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateAllGameScenes();
            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
            Debug.Log("Anime Hill Racing: all six definitive game scenes built successfully.");
        }

        [MenuItem("Anime Hill Racing/Build Prototype Scene")]
        public static void BuildPrototypeScene() => BuildAllGameScenes();

        [MenuItem("Anime Hill Racing/Validate Suspension Audit")]
        public static void ValidateSuspensionAudit()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            ValidateVehiclePhysicsContract(assets);
            ValidateSuspensionContract(assets);
            LogResolvedPhysicsProfiles(assets);
            AssetDatabase.SaveAssets();
            Debug.Log("COMPLETE VEHICLE PHYSICS AUDIT PASS");
        }

        [MenuItem("Anime Hill Racing/Validate Vehicle Performance Audit")]
        public static void ValidateVehiclePerformanceAudit()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            ValidateVehiclePerformance(assets);
            ValidateAirControlContract(assets);
            Debug.Log("COMPLETE VEHICLE PERFORMANCE AUDIT PASS");
        }

        [MenuItem("Anime Hill Racing/Validate Countryside Geometry")]
        public static void ValidateCountrysideGeometry()
        {
            MapProfile profile = AssetDatabase.LoadAssetAtPath<MapProfile>(Root + "/MapProfiles/Countryside.asset");
            if (profile == null) { Debug.LogError("Run Build All Game Scenes first."); return; }
            Mesh mesh = new Mesh();
            float endHeight = 0f;
            float endSlope = 0f;
            float maxSlope = 0f;
            float maxDelta = 0f;
            for (int chunk = 0; chunk <= 125; chunk++)
            {
                TerrainGenerator.BuildMesh(mesh, profile, chunk * 80f, 80f, 48, 10f, chunk > 0,
                    endHeight, endSlope, out float[] heights, out endHeight, out endSlope);
                for (int i = 1; i < heights.Length; i++)
                {
                    float delta = Mathf.Abs(heights[i] - heights[i - 1]);
                    maxDelta = Mathf.Max(maxDelta, delta);
                    maxSlope = Mathf.Max(maxSlope, Mathf.Atan2(delta, 80f / 48f) * Mathf.Rad2Deg);
                }
            }
            Object.DestroyImmediate(mesh);
            float d100 = profile.EvaluateDifficulty(100f);
            float d2000 = profile.EvaluateDifficulty(2000f);
            float d5000 = profile.EvaluateDifficulty(5000f);
            float d9000 = profile.EvaluateDifficulty(9000f);
            float d10000 = profile.EvaluateDifficulty(10000f);
            float d11000 = profile.EvaluateDifficulty(11000f);
            bool progressive = d100 < d2000 && d2000 < d5000 && d5000 < d9000 && d9000 <= d10000;
            bool capped = Mathf.Approximately(d10000, d11000);
            Debug.Log($"Countryside 0-10km: maxSlope={maxSlope:0.0}° maxDelta={maxDelta:0.00}m; " +
                      $"difficulty 100/2000/5000/9000/10000/11000m=" +
                      $"{d100:0.00}/{d2000:0.00}/{d5000:0.00}/{d9000:0.00}/{d10000:0.00}/{d11000:0.00}; " +
                      $"progression={(progressive ? "OK" : "FAILED")}; cap={(capped ? "OK" : "FAILED")}.");
        }

        [MenuItem("Anime Hill Racing/Validate Game Scenes")]
        public static void ValidateAllGameScenes()
        {
            string[] paths = { MainMenuPath, GaragePath, PreStartPath, Adventure2DPath, Adventure3DPath, TimeTrialPath };
            bool valid = true;
            for (int i = 0; i < paths.Length; i++)
            {
                if (!File.Exists(paths[i]))
                {
                    Debug.LogError($"Missing required scene: {paths[i]}");
                    valid = false;
                    continue;
                }
                Scene scene = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Single);
                valid &= ValidateScene(scene);
            }
            if (valid)
                Debug.Log("Anime Hill Racing validation: all six scenes and runtime references are connected.");
            else
                Debug.LogError("Anime Hill Racing validation failed. Review the errors above.");
        }

        [MenuItem("Anime Hill Racing/Validate Gameplay Scenes")]
        public static void ValidateGameplayScenes()
        {
            string[] paths = { Adventure2DPath, Adventure3DPath, TimeTrialPath };
            bool valid = true;
            for (int i = 0; i < paths.Length; i++)
            {
                Scene scene = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Single);
                valid &= ValidateScene(scene);
            }
            if (!valid)
                throw new System.InvalidOperationException("Gameplay scene validation failed.");
            Debug.Log("GAMEPLAY SCENE VALIDATION PASS: HUD calibration, authoritative references, " +
                      "half-screen multitouch and automatic-only transmission UI.");
        }

        private static bool ValidateScene(Scene scene)
        {
            bool valid = true;
            valid &= RequireCount(scene, SceneComponents<Canvas>(scene), 1, "Canvas");
            valid &= RequireCount(scene, SceneComponents<SafeArea>(scene), 1, "SafeArea");
            MainMenuUI[] mainMenus = SceneComponents<MainMenuUI>(scene);
            PreStartMenuUI[] preStarts = SceneComponents<PreStartMenuUI>(scene);
            GarageUI[] garages = SceneComponents<GarageUI>(scene);
            GameManager[] managers = SceneComponents<GameManager>(scene);
            GameplayHUD[] huds = SceneComponents<GameplayHUD>(scene);
            ResultsUI[] results = SceneComponents<ResultsUI>(scene);
            if (scene.name == "MainMenu")
            {
                valid &= RequireCount(scene, mainMenus, 1, "MainMenuUI");
                if (mainMenus.Length == 1)
                {
                    string[] fields = { "catalog", "preview", "coinsText", "vehicleNameText" };
                    for (int i = 0; i < fields.Length; i++) valid &= RequireReference(scene, mainMenus[0], fields[i]);
                }
                valid &= RequireCount(scene, SceneComponents<GaragePreviewRenderer>(scene), 1, "selected vehicle preview");
                Image[] images = SceneComponents<Image>(scene);
                bool correctBackground = false;
                Sprite expectedBackground = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuBackgroundPath);
                for (int i = 0; i < images.Length; i++)
                    correctBackground |= images[i] != null && images[i].name == "Garage Background" &&
                                         images[i].sprite == expectedBackground;
                if (!correctBackground)
                {
                    Debug.LogError("MainMenu must use the recovered mainmenu.png background.");
                    valid = false;
                }
                valid &= ValidateDisplayCamera(scene, false);
            }
            else if (scene.name == "Garage")
            {
                valid &= RequireCount(scene, garages, 1, "GarageUI");
                valid &= RequireCount(scene, SceneComponents<GaragePreviewRenderer>(scene), 1, "GaragePreviewRenderer");
                if (garages.Length == 1)
                {
                    string[] fields = { "catalog", "preview", "vehicleName", "categoryName", "skinSummary",
                        "wheelSummary", "driverSummary", "categoryButtons", "optionButtons", "optionImages",
                        "optionLabels", "tuningPanel", "tuningNames", "tuningLevels", "tuningValues",
                        "tuningCosts", "tuningButtons", "tuningCoins", "sectionTitle", "upgradeSummary",
                        "vehiclePanel", "customizationPanel", "upgradePanel", "storePanel", "settingsPanel",
                        "confirmationModal", "confirmationTitle", "confirmationValues", "confirmationCost",
                        "confirmationStatus", "confirmationYes" };
                    for (int i = 0; i < fields.Length; i++) valid &= RequireReference(scene, garages[0], fields[i]);
                }
                valid &= ValidateDisplayCamera(scene, false);
            }
            else if (scene.name == "PreStart")
            {
                valid &= RequireCount(scene, preStarts, 1, "PreStartMenuUI");
                if (preStarts.Length == 1)
                {
                    valid &= RequireReference(scene, preStarts[0], "catalog");
                    valid &= RequireReference(scene, preStarts[0], "modeText");
                    valid &= RequireReference(scene, preStarts[0], "mapText");
                    valid &= RequireReference(scene, preStarts[0], "vehicleText");
                    valid &= RequireReference(scene, preStarts[0], "vehiclePreviewRenderer");
                    SerializedObject serialized = new SerializedObject(preStarts[0]);
                    SerializedProperty cards = serialized.FindProperty("modeCards");
                    SerializedProperty tuningPanel = serialized.FindProperty("tuningPanel");
                    SerializedProperty panels = serialized.FindProperty("stepPanels");
                    if (cards == null || cards.arraySize != 3 || panels == null || panels.arraySize != 2 ||
                        (tuningPanel != null && tuningPanel.objectReferenceValue != null))
                    {
                        Debug.LogError("PreStart must contain MODE -> VEHICLE, all three existing modes, and no tuning/transmission selector.");
                        valid = false;
                    }
                    InvokePrivate(preStarts[0], "Start");
                    int activePanels = 0;
                    if (panels != null)
                        for (int i = 0; i < panels.arraySize; i++)
                        {
                            GameObject panel = panels.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                            if (panel != null && panel.activeSelf) activePanels++;
                        }
                    if (activePanels != 1)
                    {
                        Debug.LogError($"PreStart runtime initialization must leave exactly one step visible, found {activePanels}.");
                        valid = false;
                    }
                }
                valid &= RequireCount(scene, SceneComponents<GaragePreviewRenderer>(scene), 1, "selected vehicle preview");
                valid &= RequireCount(scene, SceneComponents<ShifterUI>(scene), 0, "transmission selector");
                valid &= ValidateDisplayCamera(scene, false);
            }
            else
            {
                if (scene.name == "TimeTrial3D")
                {
                    valid &= RequireCount(scene, SceneComponents<ResultsUI>(scene), 1, "ResultsUI");
                    SelectedVehicleFactory[] timeTrialFactories = SceneComponents<SelectedVehicleFactory>(scene);
                    valid &= RequireCount(scene, timeTrialFactories, 1, "SelectedVehicleFactory");
                    if (timeTrialFactories.Length == 1)
                    {
                        string[] fields = { "catalog", "input", "timeTrialManager", "circuitGenerator", "circuit",
                            "timeTrialHud", "results", "capture", "controls", "roadCamera" };
                        for (int i = 0; i < fields.Length; i++)
                            valid &= RequireReference(scene, timeTrialFactories[0], fields[i]);
                    }
                    valid &= RequireCount(scene, SceneComponents<TimeTrialManager>(scene), 1, "TimeTrialManager");
                    TimeTrialHUD[] timeTrialHuds = SceneComponents<TimeTrialHUD>(scene);
                    valid &= RequireCount(scene, timeTrialHuds, 1, "TimeTrialHUD");
                    if (timeTrialHuds.Length == 1)
                    {
                        valid &= RequireReference(scene, timeTrialHuds[0], "speedNeedle");
                        valid &= RequireReference(scene, timeTrialHuds[0], "rpmNeedle");
                        valid &= ValidateVehicleGauge(scene, timeTrialHuds[0], "rpmNeedle", "RPM Gauge",
                            new Vector2(-150f, 118f), new Vector2(.499694f, .357008f),
                            new Vector2(.136906f, .202061f), 104f, 159f, -49f);
                        valid &= ValidateVehicleGauge(scene, timeTrialHuds[0], "speedNeedle", "KMH Gauge",
                            new Vector2(150f, 118f), new Vector2(.499704f, .316062f),
                            new Vector2(.138627f, .166769f), 116f, 172f, -63f);
                    }
                    valid &= RequireCount(scene, SceneComponents<TimeTrialCircuitGenerator>(scene), 1, "TimeTrialCircuitGenerator");
                    valid &= RequireCount(scene, SceneComponents<ShifterUI>(scene), 0, "manual transmission selector");
                    Text[] timeTrialLabels = SceneComponents<Text>(scene);
                    bool hasJump = false;
                    for (int i = 0; i < timeTrialLabels.Length; i++)
                    {
                        hasJump |= timeTrialLabels[i] != null &&
                                   timeTrialLabels[i].text.Contains("HORIZONTAL JUMP");
                        string objectName = timeTrialLabels[i] != null
                            ? timeTrialLabels[i].name.ToUpperInvariant() : string.Empty;
                        if (objectName == "GEAR" || objectName == "TRANSMISSION")
                        {
                            Debug.LogError($"{scene.name}: legacy {objectName} HUD text still exists.");
                            valid = false;
                        }
                    }
                    CircuitDefinition builtCircuit = AssetDatabase.LoadAssetAtPath<CircuitDefinition>(
                        Root + "/MapProfiles/ProceduralTestCircuit.asset");
                    if (!hasJump || builtCircuit == null || builtCircuit.laps != 1)
                    {
                        Debug.LogError("TimeTrial requires one lap and the +2m Horizontal Jump control.");
                        valid = false;
                    }
                    return valid;
                }
                valid &= RequireCount(scene, managers, 1, "GameManager");
                valid &= RequireCount(scene, huds, 1, "GameplayHUD");
                valid &= RequireCount(scene, results, 1, "ResultsUI");
                SelectedVehicleFactory[] factories = SceneComponents<SelectedVehicleFactory>(scene);
                valid &= RequireCount(scene, factories, 1, "SelectedVehicleFactory");
                if (factories.Length == 1)
                {
                    string[] factoryFields = { "catalog", "input", "gameManager", "gameplayHud", "results",
                        "capture", "controls", "pickupPool" };
                    for (int i = 0; i < factoryFields.Length; i++)
                        valid &= RequireReference(scene, factories[0], factoryFields[i]);
                    string[] modeFields = scene.name == "Adventure2_5D"
                        ? new[] { "terrain", "terrainProfile", "sideCamera" }
                        : new[] { "road", "roadProfile", "roadCamera" };
                    for (int i = 0; i < modeFields.Length; i++)
                        valid &= RequireReference(scene, factories[0], modeFields[i]);
                }
                if (huds.Length == 1)
                {
                    string[] fields = { "distanceText", "bestText", "coinsText", "coinIcon", "nextFuelText",
                        "nextFuelIcon", "fuelText", "fuelIcon", "fuelFill",
                        "speedNeedle", "rpmNeedle", "debugPanel", "debugText" };
                    for (int i = 0; i < fields.Length; i++) valid &= RequireReference(scene, huds[0], fields[i]);
                    Image fuelFill = new SerializedObject(huds[0]).FindProperty("fuelFill")?.objectReferenceValue as Image;
                    if (fuelFill == null || fuelFill.type != Image.Type.Filled || fuelFill.sprite == null)
                    {
                        Debug.LogError($"{scene.name}: Fuel Fill requires a sprite-backed Filled Image.");
                        valid = false;
                    }
                    Text[] hudLabels = huds[0].GetComponentsInChildren<Text>(true);
                    for (int i = 0; i < hudLabels.Length; i++)
                    {
                        string objectName = hudLabels[i].name.ToUpperInvariant();
                        if (objectName != "RPM" && objectName != "SPEED" && objectName != "GEAR" &&
                            objectName != "TRANSMISSION") continue;
                        Debug.LogError($"{scene.name}: legacy {objectName} HUD text still exists.");
                        valid = false;
                    }
                    valid &= ValidateVehicleGauge(scene, huds[0], "rpmNeedle", "RPM Gauge",
                        new Vector2(-150f, 118f), new Vector2(.499694f, .357008f),
                        new Vector2(.136906f, .202061f), 104f, 159f, -49f);
                    valid &= ValidateVehicleGauge(scene, huds[0], "speedNeedle", "KMH Gauge",
                        new Vector2(150f, 118f), new Vector2(.499704f, .316062f),
                        new Vector2(.138627f, .166769f), 116f, 172f, -63f);
                }
                if (scene.name == "Adventure2_5D")
                {
                    valid &= RequireCount(scene, SceneComponents<VehicleController>(scene), 0, "scene VehicleController");
                    valid &= RequireCount(scene, SceneComponents<VehicleVisual2D>(scene), 0, "scene VehicleVisual2D");
                    valid &= RequireCount(scene, SceneComponents<VehicleVisual3D>(scene), 0, "VehicleVisual3D");
                    valid &= RequireCount(scene, SceneComponents<TerrainManager>(scene), 1, "TerrainManager");
                    valid &= RequireCount(scene, SceneComponents<MountainBackground>(scene), 1, "MountainBackground");
                    MountainBackground[] backgrounds = SceneComponents<MountainBackground>(scene);
                    if (backgrounds.Length == 1) valid &= ValidateCountrysideBackground(scene, backgrounds[0]);
                    valid &= ValidateDisplayCamera(scene, true);
                    ResultCaptureService[] captures = SceneComponents<ResultCaptureService>(scene);
                    valid &= RequireCount(scene, captures, 1, "ResultCaptureService");
                    if (captures.Length == 1)
                    {
                        Camera captureCamera = captures[0].CaptureCamera;
                        RawImage resultImage = captures[0].ResultImage;
                        if (captureCamera == null || !captureCamera.orthographic ||
                            captures[0].SideViewOrthographicSize < 12f)
                        {
                            Debug.LogError($"{scene.name}: ResultCaptureCamera must use the wide 2D orthographic setup.");
                            valid = false;
                        }
                        if (resultImage == null || resultImage.rectTransform.sizeDelta.x < 560f)
                        {
                            Debug.LogError($"{scene.name}: Results photo must be at least 560 px wide.");
                            valid = false;
                        }
                    }
                    HoldButton[] touchZones = SceneComponents<HoldButton>(scene);
                    if (touchZones.Length != 2)
                    {
                        Debug.LogError($"{scene.name}: expected exactly two independent Brake/Gas touch zones, found {touchZones.Length}.");
                        valid = false;
                    }
                    for (int i = 0; i < touchZones.Length; i++)
                    {
                        valid &= RequireReference(scene, touchZones[i], "visual");
                        valid &= RequireReference(scene, touchZones[i], "normalSprite");
                        valid &= RequireReference(scene, touchZones[i], "pressedSprite");
                        RectTransform zone = touchZones[i].transform as RectTransform;
                        bool brakeZone = touchZones[i].name.StartsWith("Brake");
                        Vector2 expectedMin = brakeZone ? Vector2.zero : new Vector2(.5f, 0f);
                        Vector2 expectedMax = brakeZone ? new Vector2(.5f, 1f) : Vector2.one;
                        if (zone == null || Vector2.Distance(zone.anchorMin, expectedMin) > .001f ||
                            Vector2.Distance(zone.anchorMax, expectedMax) > .001f)
                        {
                            Debug.LogError($"{scene.name}: {touchZones[i].name} must cover its exact half of the screen.");
                            valid = false;
                        }
                        SerializedProperty visualProperty = new SerializedObject(touchZones[i]).FindProperty("visual");
                        Image pedalVisual = visualProperty?.objectReferenceValue as Image;
                        if (pedalVisual == null || Vector2.Distance(pedalVisual.rectTransform.sizeDelta,
                                new Vector2(265f, 334f)) > .1f || !pedalVisual.preserveAspect)
                        {
                            Debug.LogError($"{scene.name}: {touchZones[i].name} visual must be 265x334 with preserved aspect ratio.");
                            valid = false;
                        }
                    }
                    VehicleInputController[] touchInputs = SceneComponents<VehicleInputController>(scene);
                    EventSystem[] eventSystems = SceneComponents<EventSystem>(scene);
                    if (touchZones.Length == 2 && touchInputs.Length == 1 && eventSystems.Length == 1)
                    {
                        HoldButton brake = touchZones[0].name.StartsWith("Brake") ? touchZones[0] : touchZones[1];
                        HoldButton gas = brake == touchZones[0] ? touchZones[1] : touchZones[0];
                        var brakePointer = new PointerEventData(eventSystems[0]) { pointerId = 101 };
                        var gasPointer = new PointerEventData(eventSystems[0]) { pointerId = 202 };
                        FieldInfo brakeState = typeof(VehicleInputController).GetField("touchBrake",
                            BindingFlags.Instance | BindingFlags.NonPublic);
                        FieldInfo gasState = typeof(VehicleInputController).GetField("touchGas",
                            BindingFlags.Instance | BindingFlags.NonPublic);
                        bool hasTouchState = brakeState != null && gasState != null;
                        brake.OnPointerDown(brakePointer);
                        gas.OnPointerDown(gasPointer);
                        bool simultaneous = hasTouchState &&
                                            (bool)brakeState.GetValue(touchInputs[0]) &&
                                            (bool)gasState.GetValue(touchInputs[0]);
                        brake.OnPointerUp(brakePointer);
                        bool independentRelease = hasTouchState &&
                                                  !(bool)brakeState.GetValue(touchInputs[0]) &&
                                                  (bool)gasState.GetValue(touchInputs[0]);
                        gas.OnPointerUp(gasPointer);
                        bool fullyReleased = hasTouchState &&
                                             !(bool)brakeState.GetValue(touchInputs[0]) &&
                                             !(bool)gasState.GetValue(touchInputs[0]);
                        if (!simultaneous || !independentRelease || !fullyReleased)
                        {
                            Debug.LogError($"{scene.name}: Brake/Gas multitouch independence test failed.");
                            valid = false;
                        }
                    }
                    else
                    {
                        Debug.LogError($"{scene.name}: touch input test requires 2 zones, 1 input and 1 EventSystem.");
                        valid = false;
                    }
                }
                else if (scene.name == "Adventure3D")
                {
                    valid &= RequireCount(scene, SceneComponents<RoadVehicleController>(scene), 0, "scene RoadVehicleController");
                    valid &= RequireCount(scene, SceneComponents<VehicleVisual3D>(scene), 0, "scene VehicleVisual3D");
                    valid &= RequireCount(scene, SceneComponents<RoadTrackGenerator>(scene), 1, "RoadTrackGenerator");
                }

                FuelPickup[] fuelPickups = SceneComponents<FuelPickup>(scene);
                CoinPickup[] coinPickups = SceneComponents<CoinPickup>(scene);
                if (fuelPickups.Length != 1)
                {
                    Debug.LogError($"{scene.name}: expected exactly one pooled FuelPickup template, found {fuelPickups.Length}.");
                    valid = false;
                }
                else
                {
                    BoxCollider fuelTrigger = fuelPickups[0].GetComponent<BoxCollider>();
                    SpriteRenderer fuelSprite = fuelPickups[0].GetComponentInChildren<SpriteRenderer>(true);
                    Sprite expectedFuel = AssetDatabase.LoadAssetAtPath<Sprite>(FuelCanPath);
                    if (fuelTrigger == null || !fuelTrigger.isTrigger || fuelTrigger.size.x > 1.05f ||
                        fuelTrigger.size.y > 1.15f || fuelTrigger.size.z > .75f || fuelSprite == null ||
                        fuelSprite.sprite != expectedFuel)
                    {
                        Debug.LogError($"{scene.name}: FuelPickup requires the real Fuel sprite and a tight physical trigger.");
                        valid = false;
                    }
                }
                if (coinPickups.Length != 2)
                {
                    Debug.LogError($"{scene.name}: expected Coin 01 and Coin 05 pooled templates.");
                    valid = false;
                }
                else
                {
                    bool one = false, five = false;
                    for (int i = 0; i < coinPickups.Length; i++)
                    {
                        SphereCollider trigger = coinPickups[i].GetComponent<SphereCollider>();
                        SpriteRenderer sprite = coinPickups[i].GetComponentInChildren<SpriteRenderer>(true);
                        one |= coinPickups[i].Value == 1 && sprite != null &&
                               sprite.sprite == AssetDatabase.LoadAssetAtPath<Sprite>(Coin01Path);
                        five |= coinPickups[i].Value == 5 && sprite != null &&
                                sprite.sprite == AssetDatabase.LoadAssetAtPath<Sprite>(Coin05Path);
                        if (trigger == null || !trigger.isTrigger || trigger.radius > .42f)
                        {
                            Debug.LogError($"{scene.name}: Coin collider must tightly match its visible 0.82m sprite.");
                            valid = false;
                        }
                    }
                    if (!one || !five)
                    {
                        Debug.LogError($"{scene.name}: Coin template sprites/values must be exactly 01/+1 and 05/+5.");
                        valid = false;
                    }
                }
            }

            Button[] buttons = SceneComponents<Button>(scene);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].GetComponent<ButtonPressFeedback>() == null)
                {
                    Debug.LogError($"{scene.name}: button '{buttons[i].name}' is missing immediate mobile press feedback.");
                    valid = false;
                }
                if (buttons[i].GetComponent<HoldButton>() == null && buttons[i].onClick.GetPersistentEventCount() == 0)
                {
                    Debug.LogError($"{scene.name}: button '{buttons[i].name}' has no persistent action.");
                    valid = false;
                }
            }
            valid &= ValidateButtonSpacing(scene, buttons);
            return valid;
        }

        private static bool ValidateButtonSpacing(Scene scene, Button[] buttons)
        {
            Canvas.ForceUpdateCanvases();
            bool valid = true;
            var aCorners = new Vector3[4];
            var bCorners = new Vector3[4];
            for (int a = 0; a < buttons.Length; a++)
            {
                if (buttons[a] == null) continue;
                Transform aStep = FindNamedAncestor(buttons[a].transform, "Step ");
                for (int b = a + 1; b < buttons.Length; b++)
                {
                    if (buttons[b] == null || buttons[a].transform.IsChildOf(buttons[b].transform) ||
                        buttons[b].transform.IsChildOf(buttons[a].transform)) continue;
                    Transform bStep = FindNamedAncestor(buttons[b].transform, "Step ");
                    bool sameInactiveStep = aStep != null && aStep == bStep;
                    if (!sameInactiveStep && (!buttons[a].gameObject.activeInHierarchy ||
                                              !buttons[b].gameObject.activeInHierarchy)) continue;
                    if (aStep != bStep && (aStep != null || bStep != null)) continue;
                    RectTransform aRect = buttons[a].transform as RectTransform;
                    RectTransform bRect = buttons[b].transform as RectTransform;
                    if (aRect == null || bRect == null) continue;
                    aRect.GetWorldCorners(aCorners);
                    bRect.GetWorldCorners(bCorners);
                    float overlapX = Mathf.Min(aCorners[2].x, bCorners[2].x) -
                                     Mathf.Max(aCorners[0].x, bCorners[0].x);
                    float overlapY = Mathf.Min(aCorners[2].y, bCorners[2].y) -
                                     Mathf.Max(aCorners[0].y, bCorners[0].y);
                    if (overlapX <= 2f || overlapY <= 2f) continue;
                    Debug.LogError($"{scene.name}: UI buttons '{buttons[a].name}' and '{buttons[b].name}' overlap " +
                                   $"by {overlapX:0.#} x {overlapY:0.#} px.");
                    valid = false;
                }
            }
            return valid;
        }

        private static Transform FindNamedAncestor(Transform child, string prefix)
        {
            for (Transform current = child; current != null; current = current.parent)
                if (current.name.StartsWith(prefix)) return current;
            return null;
        }

        private static T[] SceneComponents<T>(Scene scene) where T : Component
        {
            var found = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++) found.AddRange(roots[i].GetComponentsInChildren<T>(true));
            return found.ToArray();
        }

        private static bool ValidateDisplayCamera(Scene scene, bool requireOrthographic)
        {
            Camera[] cameras = SceneComponents<Camera>(scene);
            int renderingCount = 0;
            Camera renderingCamera = null;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (!cameras[i].enabled || cameras[i].targetDisplay != 0 || cameras[i].targetTexture != null) continue;
                renderingCount++;
                renderingCamera = cameras[i];
            }
            bool valid = renderingCount == 1 && (!requireOrthographic || renderingCamera.orthographic);
            if (!valid)
                Debug.LogError($"{scene.name}: expected one valid Display 1 " +
                               $"{(requireOrthographic ? "orthographic " : string.Empty)}camera, found {renderingCount}.");
            return valid;
        }

        private static bool ValidateCountrysideBackground(Scene scene, MountainBackground background)
        {
            bool valid = true;
            SerializedObject serialized = new SerializedObject(background);
            SerializedProperty segments = serialized.FindProperty("mountainSegments");
            if (segments == null || segments.arraySize < 5)
            {
                Debug.LogError($"{scene.name}: Countryside requires at least five pooled mountain segments.");
                valid = false;
            }
            else
                for (int i = 0; i < segments.arraySize; i++)
                    if (segments.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        Debug.LogError($"{scene.name}: pooled mountain segment {i} is not connected.");
                        valid = false;
                    }
            if (background.GetComponentsInChildren<Collider>(true).Length != 0 ||
                background.GetComponentsInChildren<Rigidbody>(true).Length != 0)
            {
                Debug.LogError($"{scene.name}: sky/mountains must have no Collider or Rigidbody.");
                valid = false;
            }
            return valid;
        }

        private static bool RequireCount<T>(Scene scene, T[] values, int expected, string label)
        {
            if (values.Length == expected) return true;
            Debug.LogError($"{scene.name}: expected {expected} {label}, found {values.Length}.");
            return false;
        }

        private static bool RequireReference(Scene scene, Component component, string field)
        {
            SerializedProperty property = new SerializedObject(component).FindProperty(field);
            if (property != null && property.isArray)
            {
                if (property.arraySize == 0)
                {
                    Debug.LogError($"{scene.name}: {component.GetType().Name}.{field} is empty.");
                    return false;
                }
                for (int i = 0; i < property.arraySize; i++)
                {
                    SerializedProperty element = property.GetArrayElementAtIndex(i);
                    if (element.propertyType == SerializedPropertyType.ObjectReference &&
                        element.objectReferenceValue != null) continue;
                    Debug.LogError($"{scene.name}: {component.GetType().Name}.{field}[{i}] is not connected.");
                    return false;
                }
                return true;
            }
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference &&
                property.objectReferenceValue != null) return true;
            Debug.LogError($"{scene.name}: {component.GetType().Name}.{field} is not connected.");
            return false;
        }

        [MenuItem("Anime Hill Racing/Validate Wheel Stability Diagnostics")]
        public static void ValidateWheelStabilityDiagnostics()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            ValidateVehiclePhysicsContract(assets);
            ValidateTerrainSeamContinuity(assets.countryside);
            VehicleDefinition[] vehicles = { assets.jeep, assets.formula, assets.rally, assets.sportBike };
            var failures = new List<string>();
            for (int v = 0; v < vehicles.Length; v++)
            {
                int maximum = Mathf.Max(0, vehicles[v].suspensionMaxTunes);
                int[] levels = { 0, maximum / 2, maximum };
                for (int i = 0; i < levels.Length; i++)
                {
                    if (i > 0 && levels[i] == levels[i - 1]) continue;
                    WheelStabilityResult result = SimulateWheelStability(vehicles[v], levels[i]);
                    Debug.Log($"Wheel stability {vehicles[v].displayName} L{levels[i]} | " +
                              $"axis={result.maximumAxisDrift:0.0000}deg, anchors={result.maximumAnchorDrift:0.000000}m, " +
                              $"settledSpan={result.settledCompressionSpan:P2}, relWheelV={result.maximumRelativeWheelVelocity:0.000}m/s, " +
                              $"phaseV={result.stationaryRelativeWheelVelocity:0.000}/{result.coastRelativeWheelVelocity:0.000}/" +
                              $"{result.seamRelativeWheelVelocity:0.000}/{result.motorRelativeWheelVelocity:0.000}/" +
                              $"{result.reverseRelativeWheelVelocity:0.000}m/s, " +
                              $"chassisV={result.maximumChassisVerticalVelocity:0.000}m/s, " +
                              $"travelExcess={result.maximumTravelExcess:0.0000}m, motor+reverse+5/20/40/80kmh=RUN.");
                    if (result.maximumAxisDrift > .02f) failures.Add($"{vehicles[v].displayName} L{levels[i]} axis drift");
                    if (result.maximumAnchorDrift > .0001f) failures.Add($"{vehicles[v].displayName} L{levels[i]} anchor drift");
                    if (result.settledCompressionSpan > .10f) failures.Add($"{vehicles[v].displayName} L{levels[i]} stationary jitter");
                    if (result.maximumRelativeWheelVelocity > 2.5f) failures.Add($"{vehicles[v].displayName} L{levels[i]} wheel velocity spike");
                    if (result.maximumChassisVerticalVelocity > 1.5f) failures.Add($"{vehicles[v].displayName} L{levels[i]} chassis vertical spike");
                    if (result.maximumTravelExcess > .04f) failures.Add($"{vehicles[v].displayName} L{levels[i]} joint over-travel");
                }
            }
            if (failures.Count > 0)
                throw new System.InvalidOperationException("WHEEL STABILITY DIAGNOSTICS FAILED: " +
                                                           string.Join("; ", failures));
            Debug.Log("WHEEL STABILITY DIAGNOSTICS PASS: JEEP/FORMULA/RALLY level 0/mid/max, " +
                      "stationary, motor, reverse, 5/20/40/80 km/h and a collider seam.");
        }

        [MenuItem("Anime Hill Racing/Capture HUD Calibration Frames")]
        public static void CaptureHudCalibrationFrames()
        {
            Scene scene = EditorSceneManager.OpenScene(Adventure2DPath, OpenSceneMode.Single);
            Canvas canvas = SceneComponents<Canvas>(scene).FirstOrDefault();
            GameplayHUD hud = SceneComponents<GameplayHUD>(scene).FirstOrDefault();
            Camera camera = SceneComponents<Camera>(scene).FirstOrDefault(c => c.CompareTag("MainCamera"));
            if (canvas == null || hud == null || camera == null)
                throw new System.InvalidOperationException("Adventure2_5D HUD capture dependencies are missing.");
            RectTransform rpm = new SerializedObject(hud).FindProperty("rpmNeedle")?.objectReferenceValue as RectTransform;
            RectTransform speed = new SerializedObject(hud).FindProperty("speedNeedle")?.objectReferenceValue as RectTransform;
            if (rpm == null || speed == null)
                throw new System.InvalidOperationException("HUD needles are not connected for calibration capture.");

            RenderMode oldRenderMode = canvas.renderMode;
            Camera oldCanvasCamera = canvas.worldCamera;
            RenderTexture oldTarget = camera.targetTexture;
            var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            camera.targetTexture = target;
            string directory = Path.Combine(Path.GetTempPath(), "AnimeHillRacing_HUD_Calibration");
            Directory.CreateDirectory(directory);
            for (int sample = 0; sample <= 4; sample++)
            {
                float normalized = sample * .25f;
                rpm.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(159f, -49f, normalized));
                speed.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(172f, -63f, normalized));
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(Path.Combine(directory, $"hud_{sample * 25:000}.png"), image.EncodeToPNG());
            }
            RenderTexture.active = null;
            camera.targetTexture = oldTarget;
            canvas.worldCamera = oldCanvasCamera;
            canvas.renderMode = oldRenderMode;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Debug.Log("HUD calibration frames captured at " + directory);
        }

        private static bool ValidateVehicleGauge(Scene scene, Component hud, string needleField, string gaugeName,
            Vector2 expectedPosition, Vector2 expectedFaceCenter, Vector2 expectedPivot,
            float expectedTipLength, float zeroAngle, float fullAngle)
        {
            RectTransform needle = new SerializedObject(hud).FindProperty(needleField)?.objectReferenceValue as RectTransform;
            RectTransform gauge = needle != null ? needle.parent as RectTransform : null;
            Image face = gauge != null ? gauge.GetComponent<Image>() : null;
            bool valid = needle != null && gauge != null && gauge.name == gaugeName && face != null &&
                         face.sprite != null && face.preserveAspect &&
                         Mathf.Abs(gauge.sizeDelta.y - 200f) <= .1f &&
                         Vector2.Distance(gauge.anchoredPosition, expectedPosition) <= .1f &&
                         Vector2.Distance(needle.anchorMin, expectedFaceCenter) <= .0005f &&
                         Vector2.Distance(needle.anchorMax, expectedFaceCenter) <= .0005f &&
                         Vector2.Distance(needle.pivot, expectedPivot) <= .0005f &&
                         Vector2.Distance(needle.anchoredPosition, Vector2.zero) <= .01f &&
                         Vector3.Distance(needle.localScale, Vector3.one) <= .001f;
            if (!valid)
            {
                Debug.LogError($"{scene.name}: {gaugeName} layout/pivot/scale calibration is invalid.");
                return false;
            }

            Vector2 sourceTip = new Vector2((1f - needle.pivot.x) * needle.rect.width,
                (1f - needle.pivot.y) * needle.rect.height);
            if (Mathf.Abs(sourceTip.magnitude - expectedTipLength) > .75f)
            {
                Debug.LogError($"{scene.name}: {gaugeName} needle tip length is {sourceTip.magnitude:0.00}, expected {expectedTipLength:0.00} px.");
                return false;
            }

            Vector2 hub = Vector2.Scale(expectedFaceCenter - new Vector2(.5f, .5f), gauge.rect.size);
            for (int sample = 0; sample <= 4; sample++)
            {
                float normalized = sample * .25f;
                float radians = Mathf.Lerp(zeroAngle, fullAngle, normalized) * Mathf.Deg2Rad;
                Vector2 rotatedTip = new Vector2(
                    sourceTip.x * Mathf.Cos(radians) - sourceTip.y * Mathf.Sin(radians),
                    sourceTip.x * Mathf.Sin(radians) + sourceTip.y * Mathf.Cos(radians));
                Vector2 tip = hub + rotatedTip;
                if (Mathf.Abs(tip.x) <= gauge.rect.width * .5f + .5f &&
                    Mathf.Abs(tip.y) <= gauge.rect.height * .5f + .5f) continue;
                Debug.LogError($"{scene.name}: {gaugeName} needle leaves the dial at {normalized:P0} ({tip}).");
                return false;
            }
            return true;
        }

        private static void ConfigureFormulaSprite(string path, int maximumSize)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }
            if (importer == null) throw new FileNotFoundException("Formula texture not found.", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);
            importer.spritePixelsPerUnit = 100f;
            importer.spritePivot = new Vector2(.5f, .5f);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            bool tiled = path == NeonLiquidPath || path == NeonTerrainPath || path == NeonGroundPath;
            importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = maximumSize;
            importer.SaveAndReimport();
        }

        private static VehicleDefinition.CustomizationOption Customization(string id, string name, Sprite sprite,
            bool unlockedByDefault = true, int gemPrice = 0, float visibleDiameter = 0f) =>
            new VehicleDefinition.CustomizationOption
            {
                optionId = id, displayName = name, sprite = sprite,
                unlockedByDefault = unlockedByDefault, gemPrice = gemPrice,
                visibleDiameter = visibleDiameter
            };

        private static Vector2 VisibleSpriteSize(Sprite sprite)
        {
            if (sprite == null) return Vector2.one;
            string path = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return sprite.bounds.size;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
            {
                Object.DestroyImmediate(texture);
                return sprite.bounds.size;
            }
            Color32[] pixels = texture.GetPixels32();
            int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                {
                    // Every non-transparent pixel belongs to the authored visible bounds. A threshold
                    // would incorrectly discard the Sport Bike's faint antialiased silhouette.
                    if (pixels[y * texture.width + x].a == 0) continue;
                    minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                }
            Object.DestroyImmediate(texture);
            if (maxX < minX || maxY < minY) return sprite.bounds.size;
            float pixelsPerUnit = Mathf.Max(.001f, sprite.pixelsPerUnit);
            return new Vector2((maxX - minX + 1) / pixelsPerUnit,
                (maxY - minY + 1) / pixelsPerUnit);
        }

        private static float VisibleSpriteDiameter(Sprite sprite)
        {
            Vector2 size = VisibleSpriteSize(sprite);
            return Mathf.Max(size.x, size.y);
        }

        private static void ValidateCustomizationContract(BuiltAssets assets)
        {
            if (assets.catalog == null || assets.catalog.vehicles == null || assets.catalog.vehicles.Length != 4)
                throw new System.InvalidOperationException("Garage requires Jeep, Formula, Rally and Sport Bike.");
            if (assets.jeep.skins == null || assets.jeep.skins.Length != 1 || assets.jeep.wheels == null ||
                assets.jeep.wheels.Length != 2 || assets.jeep.wheels[0].sprite == assets.jeep.wheels[1].sprite)
                throw new System.InvalidOperationException("Jeep requires one real skin and two distinct wheel alternatives.");
            if (assets.formula.skins == null || assets.formula.skins.Length != 1 || assets.formula.wheels == null ||
                assets.formula.wheels.Length != 2 || assets.formula.wheels[0].sprite != assets.formulaWheel02 ||
                assets.formula.wheels[1].sprite != assets.formulaWheel01 ||
                assets.formula.wheels[1].unlockedByDefault || assets.formula.wheels[1].gemPrice != 500)
                throw new System.InvalidOperationException("Formula Wheel 02 must be default and Ghosty must cost 500 Gems.");
            if (assets.rally.skins == null || assets.rally.skins.Length != 1 || assets.rally.wheels == null ||
                assets.rally.wheels.Length != 1 || assets.rally.skins[0].sprite != assets.rallyBody ||
                assets.rally.damagedBodySprite != assets.rallyNoRoof || assets.rally.detachableRoofSprite != assets.rallyRoof)
                throw new System.InvalidOperationException("Rally customization must expose only the intact skin and its base wheel.");
            if (assets.sportBike == null || assets.sportBike.skins == null || assets.sportBike.skins.Length != 2 ||
                assets.sportBike.wheels == null || assets.sportBike.wheels.Length != 3 ||
                assets.sportBike.skins[1].unlockedByDefault || assets.sportBike.skins[1].gemPrice != 1000 ||
                assets.sportBike.wheels[2].unlockedByDefault || assets.sportBike.wheels[2].gemPrice != 500)
                throw new System.InvalidOperationException("Sport Bike Retro/Gold cosmetic contract is invalid.");
            if (assets.stickmanBlack == null || assets.stickmanBlack.head == null || assets.stickmanBlack.torso == null ||
                assets.stickmanBlack.upperArm == null || assets.stickmanBlack.forearm == null ||
                assets.stickmanBlack.thigh == null || assets.stickmanBlack.lowerLeg == null)
                throw new System.InvalidOperationException("Stickman Black articulated parts are incomplete.");
            if (assets.countrysideEnvironment == null || assets.countrysideEnvironment.sky == null ||
                assets.countrysideEnvironment.mountains == null ||
                assets.countrysideEnvironment.mountainParallaxX < .15f ||
                assets.countrysideEnvironment.mountainParallaxX > .25f ||
                assets.countrysideEnvironment.mountainParallaxY < .03f ||
                assets.countrysideEnvironment.mountainParallaxY > .06f)
                throw new System.InvalidOperationException("Countryside environment assets or parallax bounds are invalid.");
            if (AssetDatabase.LoadAssetAtPath<Sprite>(FormulaReferencePath) == assets.formula.previewSprite)
                throw new System.InvalidOperationException("Formula_Reference must never be used as the selectable vehicle skin.");
            Debug.Log("Garage validation: real vehicle/driver/environment assets, independent options and poses are connected.");
        }

        private static void ValidateNeonCityContract(BuiltAssets assets)
        {
            MapProfile profile = assets.neonCity;
            if (assets.catalog.CountMaps(GameModeType.Adventure2_5D) != 2 ||
                assets.catalog.GetCompatibleMap(GameModeType.Adventure2_5D, 0)?.mapId != "countryside" ||
                assets.catalog.GetCompatibleMap(GameModeType.Adventure2_5D, 1)?.mapId != "neon-city")
                throw new System.InvalidOperationException(
                    "Adventure track selector requires exactly Countryside and Neon City profiles.");
            if (profile == null || !profile.supportsLiquidExposure || profile.liquidChallenges == null ||
                profile.liquidChallenges.cycleLength != 6000f ||
                profile.liquidChallenges.challengesPerCycle != 8 ||
                profile.liquidChallenges.liquidSprite != assets.neonLiquid ||
                profile.liquidChallenges.cautionSprite != assets.neonCaution)
                throw new System.InvalidOperationException("Neon City profile or required artwork is incomplete.");
            LiquidChallengeSettings settings = profile.liquidChallenges;
            float damping = -Mathf.Log(settings.entrySpeedRemaining) / settings.entrySlowdownSeconds;
            float measuredRemaining = Mathf.Exp(-damping * settings.entrySlowdownSeconds);
            float liquidTerminalSpeed = settings.liquidDriveAcceleration /
                                        Mathf.Max(.001f, settings.continuousViscosity);
            if (Mathf.Abs(measuredRemaining - .10f) > .005f || settings.normalAlpha != 1f ||
                Mathf.Abs(settings.submergedAlpha - .65f) > .001f ||
                liquidTerminalSpeed * (settings.exposureLimitSeconds - .7f) < settings.maximumPoolLength)
                throw new System.InvalidOperationException(
                    "Neon Liquid slowdown, alpha, or Level-1 escape envelope is invalid.");
            float[] repeatedRunUps = new float[LiquidChallengeSettings.RequiredChallengesPerCycle];
            for (int cycle = 0; cycle < 4; cycle++)
            {
                int detected = 0;
                float previousRecoveryEnd = cycle * 6000f;
                for (int slot = 0; slot < 8; slot++)
                {
                    LiquidChallengeLayout layout = TerrainGenerator.GetLiquidChallenge(profile, cycle, slot);
                    LiquidChallengeLayout repeat = TerrainGenerator.GetLiquidChallenge(profile, cycle, slot);
                    if (Mathf.Abs(layout.poolStart - repeat.poolStart) > .0001f ||
                        Mathf.Abs(layout.poolLength - repeat.poolLength) > .0001f)
                        throw new System.InvalidOperationException("Neon City generation is not deterministic.");
                    float localRunUp = layout.runUpStart - cycle * LiquidChallengeSettings.RequiredCycleLength;
                    if (cycle == 0) repeatedRunUps[slot] = localRunUp;
                    else if (Mathf.Abs(localRunUp - repeatedRunUps[slot]) > .001f)
                        throw new System.InvalidOperationException(
                            $"Neon City challenge {slot + 1} changed logical position in cycle {cycle}.");
                    if (layout.runUpStart < previousRecoveryEnd || layout.transitionStart <= layout.runUpStart ||
                        layout.rampStart <= layout.transitionStart || layout.poolStart <= layout.rampStart ||
                        layout.poolEnd <= layout.poolStart || layout.landingEnd <= layout.poolEnd ||
                        layout.recoveryEnd <= layout.landingEnd)
                        throw new System.InvalidOperationException(
                            $"Neon City cycle {cycle} challenge {slot + 1} grammar/spacing is invalid.");
                    if (!TerrainGenerator.TryGetLiquidChallengeAtDistance(profile,
                            (layout.poolStart + layout.poolEnd) * .5f, out LiquidChallengeLayout found) ||
                        found.challengeIndex != slot)
                        throw new System.InvalidOperationException("Neon City pool lookup failed.");
                    previousRecoveryEnd = layout.recoveryEnd;
                    detected++;
                }
                if (detected != 8)
                    throw new System.InvalidOperationException($"Neon City cycle {cycle}: {detected}/8 liquid challenges.");
                Debug.Log($"NEON_LIQUID_COUNT Cycle {cycle}: 8/8");
            }

            float minimumHeight = float.PositiveInfinity;
            float maximumHeight = float.NegativeInfinity;
            float maximumSlope = 0f;
            const float terrainAuditStep = 2f;
            float previousHeight = TerrainGenerator.SampleNeonTemplateHeight(profile, 6000f);
            for (float distance = 6000f + terrainAuditStep; distance <= 24000f; distance += terrainAuditStep)
            {
                float height = TerrainGenerator.SampleNeonTemplateHeight(profile, distance);
                minimumHeight = Mathf.Min(minimumHeight, height);
                maximumHeight = Mathf.Max(maximumHeight, height);
                maximumSlope = Mathf.Max(maximumSlope,
                    Mathf.Atan2(Mathf.Abs(height - previousHeight), terrainAuditStep) * Mathf.Rad2Deg);
                previousHeight = height;
            }
            for (float local = 200f; local < 6000f; local += 25f)
            {
                float cycle0 = (TerrainGenerator.SampleNeonTemplateHeight(profile, local) - profile.baseHeight) /
                               Mathf.Max(.001f, TerrainGenerator.EvaluateNeonAmplitude(profile, local));
                float cycle1 = (TerrainGenerator.SampleNeonTemplateHeight(profile, local + 6000f) - profile.baseHeight) /
                               Mathf.Max(.001f, TerrainGenerator.EvaluateNeonAmplitude(profile, local + 6000f));
                if (Mathf.Abs(cycle0 - cycle1) > .0001f)
                    throw new System.InvalidOperationException("Neon City cycle template does not repeat.");
            }
            if (TerrainGenerator.NeonTemplatePeakCount != 16 || TerrainGenerator.NeonTemplateValleyCount != 16 ||
                maximumSlope > profile.maxSlopeAngle + .25f || maximumHeight - minimumHeight < 30f)
                throw new System.InvalidOperationException(
                    $"Neon terrain grammar invalid: peaks={TerrainGenerator.NeonTemplatePeakCount}, " +
                    $"valleys={TerrainGenerator.NeonTemplateValleyCount}, slope={maximumSlope:0.0}, " +
                    $"range={minimumHeight:0.0}..{maximumHeight:0.0}m.");
            Debug.Log($"NEON_TERRAIN_PATTERN: 16 peaks, 16 valleys, repeated cycle shape, " +
                      $"height {minimumHeight:0.0}..{maximumHeight:0.0}m, max slope {maximumSlope:0.0} degrees.");

            var exposure = new LiquidExposureTracker();
            exposure.Reset(10f);
            exposure.SetContact(101, true); exposure.Tick(3f); exposure.SetContact(101, false); exposure.Tick(5f);
            if (Mathf.Abs(exposure.TotalSeconds - 3f) > .001f || exposure.LimitReached)
                throw new System.InvalidOperationException("Liquid exposure did not pause at 3 seconds after exit.");
            exposure.SetContact(202, true); exposure.Tick(4f); exposure.SetContact(202, false);
            exposure.SetContact(303, true); exposure.Tick(2.9f);
            if (exposure.LimitReached || Mathf.Abs(exposure.TotalSeconds - 9.9f) > .001f)
                throw new System.InvalidOperationException("Liquid exposure crashed before 10 seconds.");
            exposure.Tick(.1f);
            if (!exposure.LimitReached || Mathf.Abs(exposure.TotalSeconds - 10f) > .001f)
                throw new System.InvalidOperationException("Liquid exposure did not crash at 10 seconds.");

            var gems = new GemMilestoneTracker();
            if (gems.ClaimThrough(999f) != 0 || gems.ClaimThrough(1000f) != 1 ||
                gems.ClaimThrough(999f) != 0 || gems.ClaimThrough(2000f) != 1)
                throw new System.InvalidOperationException("Gem milestone anti-exploit contract failed.");
            gems.Reset();
            if (gems.ClaimThrough(1950f) != 1 || gems.ClaimThrough(3050f) != 2)
                throw new System.InvalidOperationException("Skipped Gem milestone recovery failed.");
            if (SaveManager.CanAffordGems(999, 1000) || !SaveManager.CanAffordGems(1000, 1000) ||
                SaveManager.CanAffordGems(499, 500) || !SaveManager.CanAffordGems(500, 500))
                throw new System.InvalidOperationException("Premium cosmetic Gem thresholds are invalid.");
            Debug.Log("Neon validation: cycles 0-3=8/8, grammar complete, exposure 3+4+3=10s, Gems milestones stable.");
        }

        private static void ValidateFuelSequenceContract(BuiltAssets assets)
        {
            var sequence = new ProgressiveFuelSequence();
            sequence.Reset(0f, 5f, 5f);
            float[] expectedPositions = { 5f, 15f, 30f, 50f, 75f };
            float[] expectedSpacings = { 5f, 10f, 15f, 20f, 25f };
            for (int i = 0; i < expectedPositions.Length; i++)
            {
                if (Mathf.Abs(sequence.NextPosition - expectedPositions[i]) > .001f ||
                    Mathf.Abs(sequence.CurrentSpacing - expectedSpacings[i]) > .001f)
                    throw new System.InvalidOperationException("Fuel spacing accumulator is not 5/10/15/20/25 meters.");
                sequence.Advance(sequence.NextPosition);
            }
            sequence.Reset(0f, 5f, 5f);
            if (sequence.NextPosition != 5f || sequence.CurrentSpacing != 5f ||
                assets.countryside.initialFuelGap != 5f || assets.countrysideRoad.initialFuelGap != 5f)
                throw new System.InvalidOperationException("A new run must reset the Fuel sequence to exactly 5 meters.");
            Debug.Log("Fuel sequence validation: accumulator spacing=5/10/15/20/25m, positions=5/15/30/50/75m, reset=5m.");
        }

        private static void ValidateCoinRouteContract(BuiltAssets assets)
        {
            CoinRouteDefinition route = assets.countrysideCoinRoute;
            if (route == null || assets.countryside.coinRoute != route || assets.countrysideRoad.coinRoute != route)
                throw new System.InvalidOperationException("Countryside 2D and 3D must share one Coin Route asset.");
            int snapshotLastCell = route.LastCellForDistance(3200f);
            int deterministicHashA = RouteSnapshotHash(route, 0, snapshotLastCell);
            int deterministicHashB = RouteSnapshotHash(route, 0, snapshotLastCell);
            if (deterministicHashA != deterministicHashB)
                throw new System.InvalidOperationException("Countryside Coin Route changed between identical generations.");
            float earlyDensity = CountCoinsPer100(route, 0f, 300f);
            float middleDensity = CountCoinsPer100(route, 600f, 1200f);
            float lateDensity = CountCoinsPer100(route, 2000f, 3000f);
            if (earlyDensity <= 0f || middleDensity <= earlyDensity || lateDensity <= middleDensity ||
                lateDensity > route.MaximumGroupsPer100Meters * route.MaximumCoinsPerGroup + .01f)
                throw new System.InvalidOperationException($"Coin density progression invalid: " +
                    $"early={earlyDensity:0.0}, middle={middleDensity:0.0}, late={lateDensity:0.0} coins/100m.");
            bool foundMixedGroup = false;
            int firstMixedCell = route.FirstCellForDistance(route.sparseEndDistance);
            int lastMixedCell = route.LastCellForDistance(route.allFiveStartDistance - 1f);
            for (int cell = firstMixedCell; cell <= lastMixedCell; cell++)
            {
                if (!route.TryGetGroup(cell, out CoinRouteGroup group) || group.anchorDistance >= route.allFiveStartDistance)
                    continue;
                foundMixedGroup = true;
                for (int point = 0; point < group.coinCount; point++)
                {
                    int expected = point == group.coinCount - 1 ? 5 : 1;
                    if (route.GetPoint(group, point).value != expected)
                        throw new System.InvalidOperationException("Every mixed coin group must end with its only Coin 05.");
                }
            }
            if (!foundMixedGroup)
                throw new System.InvalidOperationException("Coin Route has no mixed Coin 01/Coin 05 phase.");
            int firstFinalCell = route.FirstCellForDistance(route.allFiveStartDistance);
            int lastFinalCell = route.LastCellForDistance(route.allFiveStartDistance + 1000f);
            bool foundFinalGroup = false;
            for (int cell = firstFinalCell; cell <= lastFinalCell; cell++)
            {
                if (!route.TryGetGroup(cell, out CoinRouteGroup group) || group.anchorDistance < route.allFiveStartDistance)
                    continue;
                foundFinalGroup = true;
                for (int point = 0; point < group.coinCount; point++)
                    if (route.GetPoint(group, point).value != 5)
                        throw new System.InvalidOperationException("All coins in the final route phase must be Coin 05.");
            }
            if (!foundFinalGroup)
                throw new System.InvalidOperationException("Coin Route has no all-Coin-05 final phase.");
            Debug.Log($"Countryside Coin Route validation | snapshot={deterministicHashA}, " +
                      $"density={earlyDensity:0.0}->{middleDensity:0.0}->{lateDensity:0.0} coins/100m, " +
                      $"maxGroups={route.MaximumGroupsPer100Meters:0.00}/100m, mixed-final=05, final-phase=all-05, deterministic=OK.");
        }

        private static int RouteSnapshotHash(CoinRouteDefinition route, int firstCell, int lastCell)
        {
            unchecked
            {
                int hash = 17;
                for (int cell = firstCell; cell <= lastCell; cell++)
                {
                    if (!route.TryGetGroup(cell, out CoinRouteGroup group)) continue;
                    hash = hash * 31 + group.cellIndex;
                    hash = hash * 31 + Mathf.RoundToInt(group.anchorDistance * 1000f);
                    hash = hash * 31 + (int)group.pattern;
                    for (int point = 0; point < group.coinCount; point++)
                    {
                        CoinRoutePoint routePoint = route.GetPoint(group, point);
                        if (routePoint.value != 1 && routePoint.value != 5)
                            throw new System.InvalidOperationException("Coin Route contains a value other than 1 or 5.");
                        hash = hash * 31 + routePoint.value;
                        hash = hash * 31 + Mathf.RoundToInt(routePoint.distanceOffset * 100f);
                        hash = hash * 31 + Mathf.RoundToInt(routePoint.heightOffset * 100f);
                    }
                }
                return hash;
            }
        }

        private static float CountCoinsPer100(CoinRouteDefinition route, float start, float end)
        {
            int coins = 0;
            int firstCell = route.FirstCellForDistance(start);
            int lastCell = route.LastCellForDistance(end);
            for (int cell = firstCell; cell <= lastCell; cell++)
            {
                if (!route.TryGetGroup(cell, out CoinRouteGroup group)) continue;
                for (int point = 0; point < group.coinCount; point++)
                {
                    float distance = group.anchorDistance + route.GetPoint(group, point).distanceOffset;
                    if (distance >= start && distance < end) coins++;
                }
            }
            return coins * 100f / Mathf.Max(1f, end - start);
        }

        private static void ValidateVehiclePhysicsContract(BuiltAssets assets)
        {
            VehicleDefinition[] definitions = { assets.jeep, assets.formula, assets.rally, assets.sportBike };
            for (int d = 0; d < definitions.Length; d++)
            {
                VehicleDefinition definition = definitions[d];
                GameObject prefab = definition.gameplayPrefab2D;
                CapsuleCollider chassis = prefab != null ? prefab.GetComponent<CapsuleCollider>() : null;
                if (chassis == null || chassis.sharedMaterial != assets.chassisPhysics ||
                    chassis.sharedMaterial.bounciness > .001f)
                    throw new System.InvalidOperationException($"{definition.displayName} requires a rounded zero-bounce chassis collider.");
                SuspensionController[] suspension = prefab.GetComponentsInChildren<SuspensionController>(true);
                Rigidbody[] rigidbodies = prefab.GetComponentsInChildren<Rigidbody>(true);
                ConfigurableJoint[] joints = prefab.GetComponentsInChildren<ConfigurableJoint>(true);
                bool physicalWheels = suspension.Length == 2;
                for (int wheel = 0; wheel < suspension.Length; wheel++)
                {
                    ConfigurableJoint joint = suspension[wheel].SuspensionJoint;
                    physicalWheels &= suspension[wheel].GetComponent<Rigidbody>() != null &&
                                      suspension[wheel].GetComponent<SphereCollider>() != null &&
                                      joint != null && joint.gameObject == prefab &&
                                      joint.connectedBody == suspension[wheel].GetComponent<Rigidbody>() &&
                                      joint.secondaryAxis == Vector3.up &&
                                      !joint.configuredInWorldSpace;
                }
                if (rigidbodies.Length != 3 || joints.Length != 2 || !physicalWheels)
                    throw new System.InvalidOperationException($"{definition.displayName} requires one chassis and two independent physical wheel rigidbodies/colliders/joints.");
                Transform rear = null;
                Transform front = null;
                for (int i = 0; i < suspension.Length; i++)
                {
                    if (suspension[i].name.StartsWith("Rear")) rear = suspension[i].transform;
                    if (suspension[i].name.StartsWith("Front")) front = suspension[i].transform;
                }
                if (rear == null || front == null ||
                    Mathf.Abs(rear.localPosition.x - definition.RearWheelAnchor) > .001f ||
                    Mathf.Abs(front.localPosition.x - definition.FrontWheelAnchor) > .001f)
                    throw new System.InvalidOperationException($"{definition.displayName} visual wheels and physical anchors are misaligned.");
                VehicleVisual2D visual = prefab.GetComponentInChildren<VehicleVisual2D>(true);
                SpriteRenderer bodyRenderer = visual != null ? visual.BodyRenderer : null;
                SpriteRenderer frontWheelRenderer = visual != null ? visual.FrontWheelRenderer : null;
                SpriteRenderer rearWheelRenderer = visual != null ? visual.RearWheelRenderer : null;
                if (bodyRenderer == null || frontWheelRenderer == null || rearWheelRenderer == null ||
                    frontWheelRenderer.sortingLayerID != bodyRenderer.sortingLayerID ||
                    rearWheelRenderer.sortingLayerID != bodyRenderer.sortingLayerID ||
                    frontWheelRenderer.sortingOrder <= bodyRenderer.sortingOrder ||
                    rearWheelRenderer.sortingOrder <= bodyRenderer.sortingOrder)
                    throw new System.InvalidOperationException($"{definition.displayName} wheels must render on the chassis layer with a greater sorting order.");
            }
            if (Mathf.Abs(assets.jeep.FrontWheelAnchor - 1.24f) > .001f ||
                assets.countryside.groundPhysicsMaterial != assets.groundPhysics ||
                assets.countrysideRoad.groundPhysicsMaterial != assets.groundPhysics)
                throw new System.InvalidOperationException("Jeep front axle or stable terrain physics material is not configured.");
            Debug.Log("Vehicle physics validation: chassis-relative joint axes, rounded zero-bounce chassis, " +
                      "bounded suspension, visual/physical wheel anchors aligned and 2D wheels sorted above chassis.");
        }

        private struct SuspensionAuditResult
        {
            public int level;
            public float travel;
            public float maximumCompression;
            public float maximumExtension;
            public float spring;
            public float damping;
            public float staticCompression;
            public float predictedSag01;
            public float measuredSag01;
            public float settledOscillation;
            public float dampingRatio;
            public float minimumLength;
            public float progressiveFactor;
            public float compressionResistance;
            public float rideHeight;
        }

        private static void ValidateSuspensionContract(BuiltAssets assets)
        {
            VehicleDefinition[] definitions = { assets.jeep, assets.formula, assets.rally, assets.sportBike };
            var failures = new List<string>();
            var summaries = new List<string>();
            int totalPassed = 0;
            int totalLevels = 0;
            for (int i = 0; i < definitions.Length; i++)
            {
                VehicleDefinition definition = definitions[i];
                int levelCount = Mathf.Max(1, definition.suspensionMaxTunes);
                int vehiclePassed = 0;
                SuspensionAuditResult previous = default;
                float minimumRideHeight = float.PositiveInfinity;
                float maximumRideHeight = float.NegativeInfinity;
                for (int level = 1; level <= levelCount; level++)
                {
                    SuspensionAuditResult result = AuditSuspension(definition, level);
                    var conditions = new List<string>();
                    CollectSuspensionFailures(definition, result, conditions);
                    if (level > 1)
                        CollectMonotonicFailures(previous, result, conditions);
                    minimumRideHeight = Mathf.Min(minimumRideHeight, result.rideHeight);
                    maximumRideHeight = Mathf.Max(maximumRideHeight, result.rideHeight);
                    if (conditions.Count == 0) vehiclePassed++;
                    else failures.Add($"{definition.displayName} LEVEL {level}: " + string.Join("; ", conditions) +
                                      $" | sag predicted/measured={result.predictedSag01:P1}/{result.measuredSag01:P1}, " +
                                      $"oscillation={result.settledOscillation:P1}, dampingRatio={result.dampingRatio:0.00}, " +
                                      $"minLength={result.minimumLength:0.000}m, progressive={result.progressiveFactor:0.00}x");
                    previous = result;
                }
                if (maximumRideHeight - minimumRideHeight > .04f)
                    failures.Add($"{definition.displayName} ALL LEVELS: ride-height drift " +
                                 $"{maximumRideHeight - minimumRideHeight:0.000}m > 0.040m");
                float visualRadius = Mathf.Max(definition.RearWheelDiameter,
                    definition.FrontWheelDiameter) * .5f;
                if (definition.archetype != VehicleArchetype.SportBike &&
                    Mathf.Abs(visualRadius - definition.wheelRadius) > .005f)
                    failures.Add($"{definition.displayName} ALL LEVELS: wheel sprite radius {visualRadius:0.000}m " +
                                 $"!= physical radius {definition.wheelRadius:0.000}m");
                if (definition.archetype == VehicleArchetype.SportBike &&
                    Mathf.Abs(definition.wheelRadius - .38f) > .001f)
                    failures.Add("SPORT BIKE: visual wheel enlargement changed the physical wheel radius");
                totalPassed += vehiclePassed;
                totalLevels += levelCount;
                summaries.Add($"{definition.displayName} {vehiclePassed}/{levelCount}");
            }
            CollectSuspensionDropFailures(definitions, failures);
            string summary = string.Join(" | ", summaries) + $" | TOTAL {totalPassed}/{totalLevels}";
            if (failures.Count > 0)
                throw new System.InvalidOperationException("SUSPENSION AUDIT FAILED\n" + summary +
                    "\nFAILED CONDITIONS:\n- " + string.Join("\n- ", failures));
            Debug.Log("Suspension Audit PASS: " + summary +
                      " | static 46-level matrix + min/mid/max drop/compression + max-speed landings PASS; " +
                      "predicted sag 25-35%, measured sag 24-36%, damping ratio 0.65-1.35 and monotonic curves valid.");
        }

        private static SuspensionAuditResult AuditSuspension(VehicleDefinition definition, int level)
        {
            var upgrades = new VehicleUpgradeState();
            upgrades.suspensionTune = level;
            ResolvedVehicleStats stats = definition.Resolve(upgrades);
            SuspensionPhysics.GetTravelLimits(definition.suspension, stats.suspensionTravel,
                out float maximumCompression, out float maximumExtension);
            float supportedMass = definition.mass * .5f;
            float staticCompression = supportedMass * Physics.gravity.magnitude / Mathf.Max(1f, stats.spring);
            float predictedSag = SuspensionPhysics.CalculateSag01(staticCompression,
                maximumCompression, maximumExtension);
            float dampingRatio = stats.damping /
                                 Mathf.Max(.001f, 2f * Mathf.Sqrt(stats.spring * supportedMass));
            SimulateStaticSag(definition, level, out float measuredSag, out float settledOscillation);
            float minimumLength = definition.suspension.restLength - maximumCompression;
            float linearForce = maximumCompression * .9f * stats.spring;
            float progressiveForce = SuspensionPhysics.CalculateNormalForce(maximumCompression * .9f, 0f,
                stats.spring, stats.damping, maximumCompression, maximumExtension,
                definition.suspension.bumpStopStart, stats.progressiveSpringMultiplier,
                definition.suspension.hardStopSpringMultiplier, float.MaxValue);
            return new SuspensionAuditResult
            {
                level = level,
                travel = stats.suspensionTravel,
                maximumCompression = maximumCompression,
                maximumExtension = maximumExtension,
                spring = stats.spring,
                damping = stats.damping,
                staticCompression = staticCompression,
                predictedSag01 = predictedSag,
                measuredSag01 = measuredSag,
                settledOscillation = settledOscillation,
                dampingRatio = dampingRatio,
                minimumLength = minimumLength,
                progressiveFactor = progressiveForce / Mathf.Max(1f, linearForce),
                compressionResistance = progressiveForce,
                rideHeight = definition.wheelRadius + definition.suspension.restLength - staticCompression
            };
        }

        private static void CollectSuspensionFailures(VehicleDefinition definition, SuspensionAuditResult result,
            List<string> failures)
        {
            if (result.predictedSag01 < .25f) failures.Add($"predictedSag {result.predictedSag01:P1} < 25.0%");
            if (result.predictedSag01 > .35f) failures.Add($"predictedSag {result.predictedSag01:P1} > 35.0%");
            if (result.measuredSag01 < .24f) failures.Add($"measuredSag {result.measuredSag01:P1} < 24.0%");
            if (result.measuredSag01 > .36f) failures.Add($"measuredSag {result.measuredSag01:P1} > 36.0%");
            float sagDifference = Mathf.Abs(result.predictedSag01 - result.measuredSag01);
            if (sagDifference > .04f) failures.Add($"predicted/measured sag difference {sagDifference:P1} > 4.0%");
            if (result.settledOscillation > .08f)
                failures.Add($"settled oscillation {result.settledOscillation:P1} > 8.0%");
            if (result.dampingRatio < .65f) failures.Add($"dampingRatio {result.dampingRatio:0.00} < 0.65");
            if (result.dampingRatio > 1.35f) failures.Add($"dampingRatio {result.dampingRatio:0.00} > 1.35");
            if (result.minimumLength < .08f) failures.Add($"minimumLength {result.minimumLength:0.000}m < 0.080m");
            if (result.progressiveFactor <= 1.05f)
                failures.Add($"progressive resistance {result.progressiveFactor:0.00}x <= 1.05x");
            if (definition.suspension.hardStopSpringMultiplier <= 0f)
                failures.Add("hard bump-stop multiplier is not positive");
            if (result.staticCompression > result.maximumCompression * .75f)
                failures.Add($"static compression uses {result.staticCompression / result.maximumCompression:P1} of max travel > 75.0%");
            if (definition.suspension.maximumAcceleration < 12f || definition.suspension.maximumAcceleration > 40f)
                failures.Add($"normal-force cap {definition.suspension.maximumAcceleration:0.0}g outside 12-40g");
            if (definition.suspension.maximumReboundAcceleration < .5f ||
                definition.suspension.maximumReboundAcceleration > 6f)
                failures.Add($"rebound acceleration cap {definition.suspension.maximumReboundAcceleration:0.0}g outside 0.5-6g");
        }

        private static void CollectMonotonicFailures(SuspensionAuditResult previous,
            SuspensionAuditResult current, List<string> failures)
        {
            if (current.travel + .0001f < previous.travel) failures.Add("travel decreased from previous level");
            if (current.spring + .1f < previous.spring) failures.Add("spring stiffness decreased from previous level");
            if (current.damping + .1f < previous.damping) failures.Add("damping decreased from previous level");
            if (current.compressionResistance + 1f < previous.compressionResistance)
                failures.Add("progressive compression resistance decreased from previous level");
            if (current.dampingRatio + .02f < previous.dampingRatio)
                failures.Add("damping stability decreased from previous level");
        }

        private static void SimulateStaticSag(VehicleDefinition source, int level,
            out float measuredSag, out float settledOscillation)
        {
            NewScene();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition testDefinition = CreateSuspensionLevelClone(source, level);
            GameObject ground = new GameObject("Static Suspension Ground");
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            groundCollider.center = new Vector3(0f, -.5f, 0f);
            groundCollider.size = new Vector3(30f, 1f, 12f);
            GameObject instance = Object.Instantiate(source.gameplayPrefab2D,
                new Vector3(0f, 1.25f, 0f), Quaternion.identity);
            VehicleController controller = instance.GetComponent<VehicleController>();
            SuspensionController[] points = instance.GetComponentsInChildren<SuspensionController>(true);
            controller.AssignRuntimeReferences(testDefinition, null, points);
            InvokePrivate(controller, "Awake");
            MovementPlaneConstraint plane = instance.GetComponent<MovementPlaneConstraint>();
            if (plane != null) InvokePrivate(plane, "Awake");
            Physics.SyncTransforms();

            float total = 0f;
            int samples = 0;
            float minimum = 1f;
            float maximum = 0f;
            const float step = .02f;
            for (int frame = 0; frame < 350; frame++)
            {
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
                if (frame < 250) continue;
                float frameSag = 0f;
                for (int p = 0; p < points.Length; p++) frameSag += points[p].Compression01;
                frameSag /= Mathf.Max(1, points.Length);
                total += frameSag;
                samples++;
                minimum = Mathf.Min(minimum, frameSag);
                maximum = Mathf.Max(maximum, frameSag);
            }
            measuredSag = samples > 0 ? total / samples : 0f;
            settledOscillation = maximum - minimum;
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(ground);
            Object.DestroyImmediate(testDefinition);
            Physics.simulationMode = previousMode;
        }

        private static void ValidateGarageComposition(BuiltAssets assets)
        {
            GaragePreviewRenderer[] previews = Object.FindObjectsByType<GaragePreviewRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (previews.Length != 1)
                throw new System.InvalidOperationException("Garage requires exactly one composed preview renderer.");
            GaragePreviewRenderer preview = previews[0];
            SpriteRenderer body = preview.BodyRenderer;
            SpriteRenderer rear = preview.RearWheelRenderer;
            SpriteRenderer front = preview.FrontWheelRenderer;
            ArticulatedDriver2D driver = preview.Driver;
            preview.Apply(assets.jeep, assets.jeep.skins[0], assets.jeep.wheels[1], assets.stickmanBlack);
            if (body == null) throw new System.InvalidOperationException("Garage preview body renderer is missing.");
            if (rear == null || front == null) throw new System.InvalidOperationException("Garage preview wheel renderers are missing.");
            if (body.sprite != assets.jeepBody)
                throw new System.InvalidOperationException($"Garage Jeep body mismatch: actual={body.sprite?.name}, expected={assets.jeepBody?.name}.");
            if (rear.sprite != assets.jeepWheel02 || front.sprite != assets.jeepWheel02)
                throw new System.InvalidOperationException($"Garage Wheel 02 mismatch: rear={rear.sprite?.name}, front={front.sprite?.name}, " +
                    $"option={assets.jeep.wheels[1].sprite?.name}, expected={assets.jeepWheel02?.name}.");
            preview.Apply(assets.formula, assets.formula.skins[0], assets.formula.wheels[0], assets.stickmanBlack);
            if (body.sprite != assets.formulaBody || rear.sprite != assets.formulaWheel02 ||
                front.sprite != assets.formulaWheel02 || rear.sortingOrder <= body.sortingOrder ||
                front.sortingOrder <= body.sortingOrder)
                throw new System.InvalidOperationException("Garage Formula composition did not replace all Jeep pieces.");
            SerializedObject articulated = new SerializedObject(driver);
            SpriteRenderer formulaHead = articulated.FindProperty("head").objectReferenceValue as SpriteRenderer;
            SpriteRenderer formulaTorso = articulated.FindProperty("torso").objectReferenceValue as SpriteRenderer;
            SpriteRenderer formulaLeftThigh = articulated.FindProperty("leftThigh").objectReferenceValue as SpriteRenderer;
            SpriteRenderer formulaRightThigh = articulated.FindProperty("rightThigh").objectReferenceValue as SpriteRenderer;
            if (formulaHead == null || !formulaHead.enabled || formulaTorso == null || !formulaTorso.enabled ||
                formulaLeftThigh == null || formulaLeftThigh.enabled || formulaRightThigh == null || formulaRightThigh.enabled)
                throw new System.InvalidOperationException("Formula driver pose must show head/torso and hide both thighs.");
            preview.Apply(assets.rally, assets.rally.skins[0], assets.rally.wheels[0], assets.stickmanBlack);
            if (body.sprite != assets.rallyBody || rear.sprite != assets.rallyWheel || front.sprite != assets.rallyWheel ||
                !formulaHead.enabled || formulaTorso.enabled || formulaLeftThigh.enabled || formulaRightThigh.enabled)
                throw new System.InvalidOperationException("Rally garage must stay intact, use both Rally wheels, and show only the driver head.");
            preview.Apply(assets.sportBike, assets.sportBike.skins[0], assets.sportBike.wheels[0], assets.stickmanBlack);
            Vector2 bodyVisibleSize = VisibleSpriteSize(assets.sportBikeBody);
            float bodyVisibleHeight = bodyVisibleSize.y * body.transform.localScale.y;
            float frontPercent = assets.sportBike.FrontWheelDiameter / Mathf.Max(.001f, bodyVisibleHeight);
            float rearPercent = assets.sportBike.RearWheelDiameter / Mathf.Max(.001f, bodyVisibleHeight);
            bool sportCompositionValid = body.sprite == assets.sportBikeBody &&
                rear.sprite == assets.sportBikeWheel01 && front.sprite == assets.sportBikeWheel01 && body.flipX;
            bool sportProportionsValid = frontPercent >= .31f && frontPercent <= .34f &&
                                         rearPercent >= .33f && rearPercent <= .36f;
            bool sportMountsValid = (Vector2)front.transform.localPosition ==
                                    assets.sportBike.frontWheelVisualPosition &&
                                    (Vector2)rear.transform.localPosition ==
                                    assets.sportBike.rearWheelVisualPosition;
            if (!sportCompositionValid || !sportProportionsValid || !sportMountsValid)
                throw new System.InvalidOperationException(
                    $"Sport Bike preview invalid: composition={sportCompositionValid}, " +
                    $"front={frontPercent:P2}, rear={rearPercent:P2}, mounts={sportMountsValid}, " +
                    $"visibleBodyHeight={bodyVisibleHeight:0.000}m.");
            for (int wheelIndex = 0; wheelIndex < assets.sportBike.wheels.Length; wheelIndex++)
            {
                VehicleDefinition.CustomizationOption wheelOption = assets.sportBike.wheels[wheelIndex];
                preview.Apply(assets.sportBike, assets.sportBike.skins[0], wheelOption, assets.stickmanBlack);
                float apparentFront = front.transform.localScale.x * wheelOption.visibleDiameter;
                float apparentRear = rear.transform.localScale.x * wheelOption.visibleDiameter;
                if (wheelOption.visibleDiameter <= 0f ||
                    Mathf.Abs(apparentFront - assets.sportBike.FrontWheelDiameter) > .001f ||
                    Mathf.Abs(apparentRear - assets.sportBike.RearWheelDiameter) > .001f ||
                    front.sortingOrder <= body.sortingOrder || rear.sortingOrder <= body.sortingOrder)
                    throw new System.InvalidOperationException(
                        $"Sport Bike wheel option {wheelOption.displayName} is not visibly normalized.");
            }
            preview.Apply(assets.jeep, assets.jeep.skins[0], assets.jeep.wheels[0], assets.stickmanBlack);
            if (!formulaLeftThigh.enabled || !formulaRightThigh.enabled)
                throw new System.InvalidOperationException("Jeep driver pose must restore partially visible legs.");
            Debug.Log($"Garage live composition validation: Formula Wheel 02 correct; Sport Bike front " +
                      $"{assets.sportBike.FrontWheelDiameter:0.00}m ({frontPercent:P1}), rear " +
                      $"{assets.sportBike.RearWheelDiameter:0.00}m ({rearPercent:P1}), all three wheel skins normalized.");
        }

        private static VehicleDefinition.DriverPose CreateJeepDriverPose()
        {
            VehicleDefinition.DriverPartPose P(float x, float y, float r, float sx = .042f,
                float sy = .042f, bool visible = true, int order = 3) =>
                VehicleDefinition.DriverPartPose.Create(x, y, r, sx, sy, visible, order);
            return new VehicleDefinition.DriverPose
            {
                rootPosition = new Vector2(-.38f, .42f), rootScale = 1f,
                torso = P(-.01f, .25f, -12f, .030f, .030f, true, 3),
                head = P(-.06f, .70f, -4f, .033f, .033f, true, 4),
                leftUpperArm = P(.16f, .42f, -34f, .022f, .022f, true, 4),
                leftForearm = P(.39f, .27f, -58f, .021f, .021f, true, 4),
                rightUpperArm = P(.11f, .39f, -29f, .022f, .022f, true, 2),
                rightForearm = P(.35f, .25f, -55f, .021f, .021f, true, 2),
                leftThigh = P(-.04f, -.03f, -79f, .023f, .023f, true, 2),
                leftLowerLeg = P(.17f, -.28f, -15f, .021f, .021f, true, 2),
                rightThigh = P(-.11f, -.02f, -70f, .023f, .023f, true, 1),
                rightLowerLeg = P(.09f, -.27f, -8f, .021f, .021f, true, 1)
            };
        }

        private static VehicleDefinition.DriverPose CreateFormulaDriverPose()
        {
            VehicleDefinition.DriverPartPose P(float x, float y, float r, float sx = .038f,
                float sy = .038f, bool visible = true, int order = 3) =>
                VehicleDefinition.DriverPartPose.Create(x, y, r, sx, sy, visible, order);
            return new VehicleDefinition.DriverPose
            {
                rootPosition = new Vector2(-.30f, .26f), rootScale = .82f,
                torso = P(-.02f, .12f, -38f, .025f, .025f, true, 3),
                head = P(-.18f, .45f, -16f, .032f, .032f, true, 4),
                leftUpperArm = P(.14f, .19f, -62f, .019f, .019f, true, 4),
                leftForearm = P(.37f, .05f, -72f, .018f, .018f, true, 4),
                rightUpperArm = P(.09f, .16f, -57f, .019f, .019f, true, 2),
                rightForearm = P(.33f, .03f, -70f, .018f, .018f, true, 2),
                leftThigh = P(0f, 0f, 0f, .02f, .02f, false, 1),
                leftLowerLeg = P(0f, 0f, 0f, .02f, .02f, false, 1),
                rightThigh = P(0f, 0f, 0f, .02f, .02f, false, 1),
                rightLowerLeg = P(0f, 0f, 0f, .02f, .02f, false, 1)
            };
        }

        private static VehicleDefinition.DriverPose CreateRallyDriverPose()
        {
            VehicleDefinition.DriverPartPose P(float x, float y, float r, float scale = .03f,
                bool visible = false, int order = 3) =>
                VehicleDefinition.DriverPartPose.Create(x, y, r, scale, scale, visible, order);
            return new VehicleDefinition.DriverPose
            {
                rootPosition = new Vector2(-.34f, .28f), rootScale = .90f,
                torso = P(0f, 0f, 0f),
                head = P(-.03f, .54f, -3f, .032f, true, 9),
                leftUpperArm = P(0f, 0f, 0f), leftForearm = P(0f, 0f, 0f),
                rightUpperArm = P(0f, 0f, 0f), rightForearm = P(0f, 0f, 0f),
                leftThigh = P(0f, 0f, 0f), leftLowerLeg = P(0f, 0f, 0f),
                rightThigh = P(0f, 0f, 0f), rightLowerLeg = P(0f, 0f, 0f)
            };
        }

        private static VehicleDefinition.DriverPose CreateSportBikeDriverPose()
        {
            VehicleDefinition.DriverPartPose P(float x, float y, float r, float scale = .020f,
                bool visible = true, int order = 4) =>
                VehicleDefinition.DriverPartPose.Create(x, y, r, scale, scale, visible, order);
            return new VehicleDefinition.DriverPose
            {
                rootPosition = new Vector2(-.34f, .39f), rootScale = .82f,
                torso = P(.10f, .24f, -38f, .024f, true, 3),
                head = P(.24f, .58f, -16f, .029f, true, 5),
                leftUpperArm = P(.28f, .35f, -56f),
                leftForearm = P(.52f, .18f, -68f, .019f),
                rightUpperArm = P(.23f, .32f, -52f, .020f, true, 2),
                rightForearm = P(.48f, .16f, -66f, .019f, true, 2),
                leftThigh = P(-.04f, .02f, -70f, .022f, true, 3),
                leftLowerLeg = P(.18f, -.22f, -18f, .020f, true, 3),
                rightThigh = P(-.10f, 0f, -66f, .022f, true, 2),
                rightLowerLeg = P(.12f, -.24f, -14f, .020f, true, 2)
            };
        }

        private static VehicleDefinition.TuneTargets CreateFormulaTuneTargets()
        {
            float[] speed = new float[21];
            float[] acceleration = new float[21];
            float[] grip = new float[21];
            float[] downforce = new float[21];
            float[] fuel = new float[26];
            for (int i = 0; i < 21; i++)
            {
                speed[i] = 160f + i * 7f;
                acceleration[i] = 5f - i * .15f;
                grip[i] = Mathf.Lerp(200f, 520f, i / 20f);
                downforce[i] = 1.5f + i * .6f;
            }
            for (int i = 0; i < fuel.Length; i++) fuel[i] = 20f + i;
            return new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = speed,
                accelerationZeroToHundredSeconds = acceleration,
                gripRatings = grip,
                airControlTorque = new float[] { 9000f },
                airborneDownforceAcceleration = downforce,
                fuelRunDurationSeconds = fuel
            };
        }

        private static VehicleDefinition.TuneTargets CreateRallyTuneTargets()
        {
            float[] speed = new float[20];
            float[] turbo = new float[20];
            float[] grip = new float[15];
            float[] fuel = new float[16];
            for (int i = 0; i < speed.Length; i++) speed[i] = 145f + i * (135f / 19f);
            for (int i = 0; i < turbo.Length; i++) turbo[i] = 5.8f - i * (3.3f / 19f);
            for (int i = 0; i < grip.Length; i++) grip[i] = Mathf.Lerp(167f, 750f, i / 14f);
            for (int i = 0; i < fuel.Length; i++) fuel[i] = Mathf.Lerp(25f, 50f, i / 15f);
            return new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = speed,
                accelerationZeroToHundredSeconds = turbo,
                gripRatings = grip,
                airControlTorque = new[] { 6500f },
                airborneDownforceAcceleration = new[] { 0f },
                fuelRunDurationSeconds = fuel
            };
        }

        private static VehicleDefinition.TuneTargets CreateSportBikeTuneTargets()
        {
            float[] speed = new float[20];
            float[] acceleration = new float[20];
            float[] grip = new float[20];
            float[] fuel = new float[15];
            for (int i = 0; i < speed.Length; i++)
            {
                float t = i / 19f;
                speed[i] = Mathf.Lerp(20f, 360f, t);
                acceleration[i] = Mathf.Lerp(20f, 2.5f, t);
                grip[i] = Mathf.Lerp(20f, 500f, t);
            }
            for (int i = 0; i < fuel.Length; i++) fuel[i] = Mathf.Lerp(15f, 45f, i / 14f);
            return new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = speed,
                accelerationZeroToHundredSeconds = acceleration,
                gripRatings = grip,
                airControlTorque = new[] { 7600f },
                airborneDownforceAcceleration = new[] { 0f },
                fuelRunDurationSeconds = fuel
            };
        }

        private static void CreateVehiclePrefabs(BuiltAssets assets)
        {
            assets.jeep.gameplayPrefab2D = SaveVehiclePrefab(
                CreateClimbVehicle(assets.jeep, null, null, assets.chassisPhysics, assets.tirePhysics).gameObject, Prefabs + "/Jeep_2D.prefab");
            assets.jeep.gameplayPrefab3D = SaveVehiclePrefab(
                CreateRoadVehicle(assets.jeep, null, null, assets.chassisPhysics).gameObject, Prefabs + "/Jeep_3D.prefab");
            assets.formula.gameplayPrefab2D = SaveVehiclePrefab(
                CreateClimbVehicle(assets.formula, null, null, assets.chassisPhysics, assets.tirePhysics).gameObject, Prefabs + "/Formula_2D.prefab");
            assets.formula.gameplayPrefab3D = SaveVehiclePrefab(
                CreateRoadVehicle(assets.formula, null, null, assets.chassisPhysics).gameObject, Prefabs + "/Formula_3D.prefab");
            assets.rally.gameplayPrefab2D = SaveVehiclePrefab(
                CreateClimbVehicle(assets.rally, null, null, assets.chassisPhysics, assets.tirePhysics).gameObject, Prefabs + "/Rally_2D.prefab");
            assets.rally.gameplayPrefab3D = SaveVehiclePrefab(
                CreateRoadVehicle(assets.rally, null, null, assets.chassisPhysics).gameObject, Prefabs + "/Rally_3D.prefab");
            assets.sportBike.gameplayPrefab2D = SaveVehiclePrefab(
                CreateClimbVehicle(assets.sportBike, null, null, assets.chassisPhysics, assets.tirePhysics).gameObject,
                Prefabs + "/SportBike_2D.prefab");
            assets.sportBike.gameplayPrefab3D = SaveVehiclePrefab(
                CreateRoadVehicle(assets.sportBike, null, null, assets.chassisPhysics).gameObject,
                Prefabs + "/SportBike_3D.prefab");
            EditorUtility.SetDirty(assets.jeep);
            EditorUtility.SetDirty(assets.formula);
            EditorUtility.SetDirty(assets.rally);
            EditorUtility.SetDirty(assets.sportBike);
        }

        private static GameObject SaveVehiclePrefab(GameObject root, string path)
        {
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private struct WheelStabilityResult
        {
            public float maximumAxisDrift;
            public float maximumAnchorDrift;
            public float settledCompressionSpan;
            public float maximumRelativeWheelVelocity;
            public float stationaryRelativeWheelVelocity;
            public float coastRelativeWheelVelocity;
            public float seamRelativeWheelVelocity;
            public float motorRelativeWheelVelocity;
            public float reverseRelativeWheelVelocity;
            public float maximumChassisVerticalVelocity;
            public float maximumTravelExcess;
        }

        private static void ValidateTerrainSeamContinuity(MapProfile profile)
        {
            Mesh first = new Mesh();
            Mesh second = new Mesh();
            TerrainGenerator.BuildMesh(first, profile, 0f, 80f, 48, 10f, false, 0f, 0f,
                out float[] firstHeights, out float endHeight, out float endSlope);
            TerrainGenerator.BuildMesh(second, profile, 80f, 80f, 48, 10f, true, endHeight, endSlope,
                out float[] secondHeights, out _, out _);
            float dx = 80f / 48f;
            float outgoingSlope = (firstHeights[firstHeights.Length - 1] -
                                   firstHeights[firstHeights.Length - 2]) / dx;
            float incomingSlope = (secondHeights[1] - secondHeights[0]) / dx;
            float heightGap = Mathf.Abs(firstHeights[firstHeights.Length - 1] - secondHeights[0]);
            float slopeGap = Mathf.Abs(outgoingSlope - incomingSlope);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            if (heightGap > .00001f || slopeGap > .00001f)
                throw new System.InvalidOperationException($"Terrain seam discontinuity: height={heightGap:0.000000}m, slope={slopeGap:0.000000}.");
            Debug.Log($"Terrain seam continuity PASS: heightGap={heightGap:0.000000}m, slopeGap={slopeGap:0.000000}; " +
                      "one MeshCollider surface plus a non-physical LineRenderer edge.");
        }

        private static WheelStabilityResult SimulateWheelStability(VehicleDefinition source, int level)
        {
            NewScene();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition testDefinition = CreateSuspensionLevelClone(source, level);
            PhysicsMaterial terrainMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(
                Root + "/Materials/TerrainStable.physicMaterial");
            GameObject firstGround = new GameObject("Single Authoritative Flat Diagnostic Ground");
            MeshCollider firstCollider = firstGround.AddComponent<MeshCollider>();
            Mesh diagnosticGroundMesh = new Mesh { name = "Diagnostic Welded Seam Mesh" };
            diagnosticGroundMesh.vertices = new[]
            {
                new Vector3(-1000f, 0f, -10f), new Vector3(-1000f, 0f, 10f),
                new Vector3(100f, 0f, -10f), new Vector3(100f, 0f, 10f),
                new Vector3(1000f, 0f, -10f), new Vector3(1000f, 0f, 10f)
            };
            diagnosticGroundMesh.triangles = new[] { 0, 1, 2, 1, 3, 2, 2, 3, 4, 3, 5, 4 };
            diagnosticGroundMesh.RecalculateNormals();
            diagnosticGroundMesh.RecalculateBounds();
            firstCollider.sharedMesh = diagnosticGroundMesh;
            firstCollider.sharedMaterial = terrainMaterial;
            GameObject inputObject = new GameObject("Wheel Diagnostic Input");
            VehicleInputController input = inputObject.AddComponent<VehicleInputController>();
            input.SetInputMode(GameModeType.Adventure2_5D);
            GameObject instance = Object.Instantiate(source.gameplayPrefab2D,
                new Vector3(0f, 1.25f, 0f), Quaternion.identity);
            VehicleController controller = instance.GetComponent<VehicleController>();
            SuspensionController[] points = instance.GetComponentsInChildren<SuspensionController>(true);
            controller.AssignRuntimeReferences(testDefinition, input, points);
            InvokePrivate(controller, "Awake");
            MovementPlaneConstraint plane = instance.GetComponent<MovementPlaneConstraint>();
            if (plane != null) InvokePrivate(plane, "Awake");
            Physics.SyncTransforms();

            Vector3[] expectedAnchors = new Vector3[points.Length];
            Vector3[] expectedConnectedAnchors = new Vector3[points.Length];
            float[] minimumCompression = new float[points.Length];
            float[] maximumCompression = new float[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                ConfigurableJoint joint = points[i].SuspensionJoint;
                if (joint == null || joint.gameObject != instance || joint.connectedBody != points[i].WheelBody)
                    throw new System.InvalidOperationException($"{source.displayName} L{level}: joint is not chassis-relative.");
                expectedAnchors[i] = joint.anchor;
                expectedConnectedAnchors[i] = joint.connectedAnchor;
                minimumCompression[i] = float.PositiveInfinity;
                maximumCompression[i] = float.NegativeInfinity;
            }

            WheelStabilityResult result = default;
            for (int frame = 0; frame < 350; frame++)
            {
                StepWheelDiagnostic(controller, plane, input);
                if (frame >= 250)
                    SampleWheelDiagnostic(controller, points, expectedAnchors, expectedConnectedAnchors,
                        ref result, minimumCompression, maximumCompression, 0);
            }

            float[] speedsKmh = { 5f, 20f, 40f, 80f };
            for (int speedIndex = 0; speedIndex < speedsKmh.Length; speedIndex++)
            {
                float speed = speedsKmh[speedIndex] / 3.6f;
                controller.Body.linearVelocity = controller.transform.right * speed;
                for (int p = 0; p < points.Length; p++)
                {
                    points[p].WheelBody.linearVelocity = controller.Body.linearVelocity;
                    points[p].WheelBody.angularVelocity = controller.Body.angularVelocity +
                        Vector3.back * (speed / Mathf.Max(.05f, source.wheelRadius));
                }
                for (int frame = 0; frame < 100; frame++)
                {
                    StepWheelDiagnostic(controller, plane, input);
                    if (frame >= 30)
                        SampleWheelDiagnostic(controller, points, expectedAnchors, expectedConnectedAnchors,
                            ref result, minimumCompression, maximumCompression, 1);
                }
            }

            // Isolate the collider-boundary test from the speed sweeps: approach the exact x=100 seam slowly.
            Vector3 relocation = new Vector3(98f - controller.Body.position.x, 0f, 0f);
            controller.Body.position += relocation;
            controller.Body.linearVelocity = Vector3.zero;
            controller.Body.angularVelocity = Vector3.zero;
            for (int p = 0; p < points.Length; p++)
            {
                points[p].WheelBody.position += relocation;
                points[p].WheelBody.linearVelocity = Vector3.zero;
                points[p].WheelBody.angularVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();
            for (int frame = 0; frame < 200; frame++)
                StepWheelDiagnostic(controller, plane, input);
            controller.Body.linearVelocity = controller.transform.right * (5f / 3.6f);
            for (int p = 0; p < points.Length; p++)
            {
                points[p].WheelBody.linearVelocity = controller.Body.linearVelocity;
                points[p].WheelBody.angularVelocity = controller.Body.angularVelocity +
                    Vector3.back * ((5f / 3.6f) / Mathf.Max(.05f, source.wheelRadius));
            }
            for (int frame = 0; frame < 120; frame++)
            {
                StepWheelDiagnostic(controller, plane, input);
                if (frame >= 20)
                    SampleWheelDiagnostic(controller, points, expectedAnchors, expectedConnectedAnchors,
                        ref result, minimumCompression, maximumCompression, 4);
            }

            input.SetTouchGas(true);
            for (int frame = 0; frame < 250; frame++)
            {
                StepWheelDiagnostic(controller, plane, input);
                if (frame >= 50)
                    SampleWheelDiagnostic(controller, points, expectedAnchors, expectedConnectedAnchors,
                        ref result, minimumCompression, maximumCompression, 2);
            }
            input.SetTouchGas(false);
            input.SetTouchBrake(true);
            for (int frame = 0; frame < 250; frame++)
            {
                StepWheelDiagnostic(controller, plane, input);
                if (frame >= 50)
                    SampleWheelDiagnostic(controller, points, expectedAnchors, expectedConnectedAnchors,
                        ref result, minimumCompression, maximumCompression, 3);
            }

            for (int i = 0; i < points.Length; i++)
                result.settledCompressionSpan = Mathf.Max(result.settledCompressionSpan,
                    maximumCompression[i] - minimumCompression[i]);
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(inputObject);
            Object.DestroyImmediate(firstGround);
            Object.DestroyImmediate(diagnosticGroundMesh);
            Object.DestroyImmediate(testDefinition);
            Physics.simulationMode = previousMode;
            return result;
        }

        private static void StepWheelDiagnostic(VehicleController controller, MovementPlaneConstraint plane,
            VehicleInputController input)
        {
            InvokePrivate(input, "Update");
            InvokePrivate(controller, "FixedUpdate");
            if (plane != null) InvokePrivate(plane, "FixedUpdate");
            Physics.Simulate(.02f);
        }

        private static void SampleWheelDiagnostic(VehicleController controller, SuspensionController[] points,
            Vector3[] expectedAnchors, Vector3[] expectedConnectedAnchors, ref WheelStabilityResult result,
            float[] minimumCompression, float[] maximumCompression, int phase)
        {
            result.maximumChassisVerticalVelocity = Mathf.Max(result.maximumChassisVerticalVelocity,
                Mathf.Abs(Vector3.Dot(controller.Body.linearVelocity, controller.transform.up)));
            for (int i = 0; i < points.Length; i++)
            {
                ConfigurableJoint joint = points[i].SuspensionJoint;
                result.maximumAxisDrift = Mathf.Max(result.maximumAxisDrift,
                    Vector3.Angle(points[i].SuspensionAxisWorld, controller.transform.up));
                result.maximumAnchorDrift = Mathf.Max(result.maximumAnchorDrift,
                    Vector3.Distance(joint.anchor, expectedAnchors[i]),
                    Vector3.Distance(joint.connectedAnchor, expectedConnectedAnchors[i]));
                Vector3 relativeVelocity = points[i].WheelBody.linearVelocity -
                                           controller.Body.GetPointVelocity(points[i].WheelBody.position);
                float verticalWheelVelocity = Mathf.Abs(Vector3.Dot(relativeVelocity, controller.transform.up));
                if (points[i].IsGrounded)
                {
                    result.maximumRelativeWheelVelocity = Mathf.Max(result.maximumRelativeWheelVelocity,
                        verticalWheelVelocity);
                    if (phase == 0) result.stationaryRelativeWheelVelocity = Mathf.Max(
                        result.stationaryRelativeWheelVelocity, verticalWheelVelocity);
                    else if (phase == 1) result.coastRelativeWheelVelocity = Mathf.Max(
                        result.coastRelativeWheelVelocity, verticalWheelVelocity);
                    else if (phase == 2) result.motorRelativeWheelVelocity = Mathf.Max(
                        result.motorRelativeWheelVelocity, verticalWheelVelocity);
                    else if (phase == 3) result.reverseRelativeWheelVelocity = Mathf.Max(
                        result.reverseRelativeWheelVelocity, verticalWheelVelocity);
                    else result.seamRelativeWheelVelocity = Mathf.Max(
                        result.seamRelativeWheelVelocity, verticalWheelVelocity);
                }
                float halfTravel = (points[i].MaximumCompression + points[i].MaximumExtension) * .5f;
                result.maximumTravelExcess = Mathf.Max(result.maximumTravelExcess,
                    Mathf.Max(0f, Mathf.Abs(points[i].JointTranslation) - halfTravel));
                if (phase != 0) continue;
                minimumCompression[i] = Mathf.Min(minimumCompression[i], points[i].Compression01);
                maximumCompression[i] = Mathf.Max(maximumCompression[i], points[i].Compression01);
            }
        }

        private struct VehiclePerformanceResult
        {
            public float maximumSpeedKmh;
            public float resolvedTopSpeedKmh;
            public float zeroToHundredSeconds;
            public float fuelDurationSeconds;
            public float reverseSpeedKmh;
            public float groundedRatio;
        }

        private static void ValidateVehiclePerformance(BuiltAssets assets)
        {
            VehiclePerformanceResult jeep = SimulateFlatPerformance(assets.jeep);
            VehiclePerformanceResult formula = SimulateFlatPerformance(assets.formula);
            VehiclePerformanceResult rally = SimulateFlatPerformance(assets.rally);
            VehiclePerformanceResult sportBike = SimulateFlatPerformance(assets.sportBike);
            if (jeep.maximumSpeedKmh < 98f || jeep.maximumSpeedKmh > 103f ||
                jeep.zeroToHundredSeconds < 6.7f || jeep.zeroToHundredSeconds > 7.3f ||
                jeep.fuelDurationSeconds < 29.9f || jeep.fuelDurationSeconds > 30.1f || jeep.reverseSpeedKmh > -10f)
                throw new System.InvalidOperationException("Jeep performance contract failed (100 km/h, 7 s, 30 s fuel, working reverse).");
            if (formula.maximumSpeedKmh < 295f || formula.maximumSpeedKmh > 305f ||
                formula.zeroToHundredSeconds < 1.85f || formula.zeroToHundredSeconds > 2.15f ||
                formula.fuelDurationSeconds < 44.9f || formula.fuelDurationSeconds > 45.1f || formula.reverseSpeedKmh > -10f)
                throw new System.InvalidOperationException("Formula performance contract failed (300 km/h, 2 s, 45 s fuel, working reverse).");
            if (rally.maximumSpeedKmh < 275f || rally.maximumSpeedKmh > 285f ||
                rally.zeroToHundredSeconds < 2.35f || rally.zeroToHundredSeconds > 2.65f ||
                rally.fuelDurationSeconds < 49.9f || rally.fuelDurationSeconds > 50.1f || rally.reverseSpeedKmh > -10f)
                throw new System.InvalidOperationException("Rally performance contract failed (280 km/h, 2.5 s Turbo, 50 s fixed fuel, reverse).");
            if (sportBike.maximumSpeedKmh < 355f || sportBike.maximumSpeedKmh > 365f ||
                sportBike.zeroToHundredSeconds < 2.35f || sportBike.zeroToHundredSeconds > 2.65f ||
                sportBike.fuelDurationSeconds < 44.9f || sportBike.fuelDurationSeconds > 45.1f ||
                sportBike.reverseSpeedKmh > -10f)
                throw new System.InvalidOperationException(
                    "Sport Bike performance contract failed (360 km/h, 2.5 s, 45 s fuel, working reverse).");
            Debug.Log($"Vehicle flat validation (fixed 0.02 s) | JEEP max={jeep.maximumSpeedKmh:0.0} km/h, " +
                      $"target={jeep.resolvedTopSpeedKmh:0.0}, grounded={jeep.groundedRatio:P0}, " +
                      $"0-100={jeep.zeroToHundredSeconds:0.00} s, fuel={jeep.fuelDurationSeconds:0.00} s, " +
                      $"reverse={jeep.reverseSpeedKmh:0.0} km/h | " +
                      $"FORMULA max={formula.maximumSpeedKmh:0.0} km/h, 0-100={formula.zeroToHundredSeconds:0.00} s, " +
                      $"target={formula.resolvedTopSpeedKmh:0.0}, grounded={formula.groundedRatio:P0}, " +
                       $"fuel={formula.fuelDurationSeconds:0.00} s, reverse={formula.reverseSpeedKmh:0.0} km/h | " +
                       $"RALLY max={rally.maximumSpeedKmh:0.0} km/h, 0-100={rally.zeroToHundredSeconds:0.00} s, " +
                       $"fuel={rally.fuelDurationSeconds:0.00} s, reverse={rally.reverseSpeedKmh:0.0} km/h | " +
                       $"SPORT BIKE max={sportBike.maximumSpeedKmh:0.0} km/h, " +
                       $"0-100={sportBike.zeroToHundredSeconds:0.00} s, " +
                       $"fuel={sportBike.fuelDurationSeconds:0.00} s, reverse={sportBike.reverseSpeedKmh:0.0} km/h.");
        }

        private static VehiclePerformanceResult SimulateFlatPerformance(VehicleDefinition source)
        {
            Scene simulationScene = NewScene();
            Physics.SyncTransforms();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition testDefinition = CreateMaximumTuneClone(source);
            GameObject ground = new GameObject("Flat Performance Ground");
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            groundCollider.center = new Vector3(0f, -.5f, 0f);
            groundCollider.size = new Vector3(10000f, 1f, 20f);
            GameObject inputObject = new GameObject("Performance Input");
            VehicleInputController input = inputObject.AddComponent<VehicleInputController>();
            input.SetInputMode(GameModeType.Adventure2_5D);
            GameObject instance = Object.Instantiate(source.gameplayPrefab2D, new Vector3(0f, 1f, 0f), Quaternion.identity);
            VehicleController controller = instance.GetComponent<VehicleController>();
            controller.AssignRuntimeReferences(testDefinition, input,
                instance.GetComponentsInChildren<SuspensionController>(true));
            InvokePrivate(controller, "Awake");
            MovementPlaneConstraint plane = instance.GetComponent<MovementPlaneConstraint>();
            if (plane != null) InvokePrivate(plane, "Awake");

            const float step = .02f;
            input.SetTouchGas(false);
            for (int i = 0; i < 100; i++)
            {
                InvokePrivate(input, "Update");
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
            }

            input.SetTouchGas(true);
            float elapsed = 0f;
            float zeroToHundred = -1f;
            float maximumSpeed = 0f;
            int groundedSteps = 0;
            for (int i = 0; i < 1500; i++)
            {
                InvokePrivate(input, "Update");
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
                elapsed += step;
                float speed = controller.SpeedKmh;
                maximumSpeed = Mathf.Max(maximumSpeed, speed);
                if (controller.AnyGrounded) groundedSteps++;
                if (zeroToHundred < 0f && speed >= 100f) zeroToHundred = elapsed;
            }

            input.SetTouchGas(false);
            input.SetTouchBrake(true);
            float reverseSpeed = 0f;
            for (int i = 0; i < 1000; i++)
            {
                InvokePrivate(input, "Update");
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
                reverseSpeed = Mathf.Min(reverseSpeed, controller.SpeedKmh);
            }

            FuelSystem fuel = controller.Fuel;
            fuel.RefillFull();
            float fuelElapsed = 0f;
            while (fuel.NormalizedFuel > 0f && fuelElapsed < 120f)
            {
                fuel.TickRun(step, true);
                fuelElapsed += step;
            }

            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(inputObject);
            Object.DestroyImmediate(ground);
            Object.DestroyImmediate(testDefinition);
            Physics.simulationMode = previousMode;
            return new VehiclePerformanceResult
            {
                maximumSpeedKmh = maximumSpeed,
                resolvedTopSpeedKmh = controller.Stats.topSpeedMps * 3.6f,
                zeroToHundredSeconds = zeroToHundred,
                fuelDurationSeconds = fuelElapsed,
                reverseSpeedKmh = reverseSpeed,
                groundedRatio = groundedSteps / 1500f
            };
        }

        private struct LandingResult
        {
            public int level;
            public float speedKmh;
            public float maximumReboundMps;
            public float maximumAngularVelocity;
            public float maximumGroundPenetration;
            public float maximumCompression01;
            public float impactSpeedMps;
            public float firstReboundPeakMps;
            public float secondReboundPeakMps;
            public float settledVerticalSpeed;
            public float settledAngularVelocity;
            public int reboundEpisodes;
            public bool touchedDown;
            public bool bodyTunneled;
        }

        private static void CollectSuspensionDropFailures(VehicleDefinition[] vehicles, List<string> failures)
        {
            for (int vehicleIndex = 0; vehicleIndex < vehicles.Length; vehicleIndex++)
            {
                VehicleDefinition vehicle = vehicles[vehicleIndex];
                int maximumLevel = Mathf.Max(1, vehicle.suspensionMaxTunes);
                int middleLevel = Mathf.Max(1, (maximumLevel + 1) / 2);
                int[] levels = { 1, middleLevel, maximumLevel };
                float representativeSpeed = vehicle.archetype == VehicleArchetype.Jeep ? 40f : 100f;
                for (int i = 0; i < levels.Length; i++)
                {
                    if (i > 0 && levels[i] == levels[i - 1]) continue;
                    CollectLandingFailures(vehicle,
                        SimulateFlatLanding(vehicle, levels[i], representativeSpeed, -5f), false, failures);
                    CollectLandingFailures(vehicle,
                        SimulateFlatLanding(vehicle, levels[i], representativeSpeed, -12f), true, failures);
                }
                float[] maximumLevelSpeeds = vehicle.archetype == VehicleArchetype.Jeep
                    ? new[] { 100f }
                    : vehicle.archetype == VehicleArchetype.Rally ? new[] { 200f, 280f }
                    : vehicle.archetype == VehicleArchetype.SportBike ? new[] { 200f, 360f }
                    : new[] { 200f, 300f };
                for (int i = 0; i < maximumLevelSpeeds.Length; i++)
                {
                    CollectLandingFailures(vehicle,
                        SimulateFlatLanding(vehicle, maximumLevel, maximumLevelSpeeds[i], -5f), false, failures);
                    CollectLandingFailures(vehicle,
                        SimulateFlatLanding(vehicle, maximumLevel, maximumLevelSpeeds[i], -12f), true, failures);
                }
            }
        }

        private static void CollectLandingFailures(VehicleDefinition vehicle, LandingResult result, bool hard,
            List<string> failures)
        {
            var conditions = new List<string>();
            float penetrationLimit = hard ? .15f : .05f;
            float reboundLimit = hard ? 7.5f : 5f;
            if (!result.touchedDown) conditions.Add("wheel-first contact was never detected");
            if (result.bodyTunneled) conditions.Add("body tunneled through terrain");
            if (result.maximumGroundPenetration > penetrationLimit)
                conditions.Add($"body penetration {result.maximumGroundPenetration:0.000}m > {penetrationLimit:0.000}m");
            if (result.maximumReboundMps > reboundLimit)
                conditions.Add($"rebound {result.maximumReboundMps:0.00}m/s > {reboundLimit:0.00}m/s");
            if (result.maximumAngularVelocity > 1.2f)
                conditions.Add($"pitch velocity {result.maximumAngularVelocity:0.00}rad/s > 1.20rad/s");
            if (hard && result.maximumCompression01 < .72f)
                conditions.Add($"hard-impact compression {result.maximumCompression01:P0} < 72%");
            if (result.reboundEpisodes > 1)
                conditions.Add($"{result.reboundEpisodes} significant rebound episodes > 1 " +
                               $"(impact={result.impactSpeedMps:0.00}m/s, peaks=" +
                               $"{result.firstReboundPeakMps:0.00}/{result.secondReboundPeakMps:0.00}m/s)");
            if (result.settledVerticalSpeed > .75f)
                conditions.Add($"settled vertical speed {result.settledVerticalSpeed:0.00}m/s > 0.75m/s");
            if (result.settledAngularVelocity > .35f)
                conditions.Add($"settled pitch speed {result.settledAngularVelocity:0.00}rad/s > 0.35rad/s");
            if (conditions.Count > 0)
                failures.Add($"{vehicle.displayName} LEVEL {result.level} {(hard ? "HARD DROP" : "NORMAL DROP")} " +
                             $"@ {result.speedKmh:0}km/h: " + string.Join("; ", conditions) +
                             $" | metrics impact={result.impactSpeedMps:0.00}m/s, " +
                             $"maxRebound={result.maximumReboundMps:0.00}m/s, " +
                             $"pitch={result.maximumAngularVelocity:0.00}rad/s, " +
                             $"compression={result.maximumCompression01:P0}, " +
                             $"penetration={result.maximumGroundPenetration:0.000}m, " +
                             $"episodes={result.reboundEpisodes}, " +
                             $"settledV={result.settledVerticalSpeed:0.00}m/s, " +
                             $"settledW={result.settledAngularVelocity:0.00}rad/s");
        }

        private static LandingResult SimulateFlatLanding(VehicleDefinition source, int level, float speedKmh,
            float verticalSpeedMps)
        {
            NewScene();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition testDefinition = CreateSuspensionLevelClone(source, level);
            GameObject ground = new GameObject("High Speed Landing Ground");
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            groundCollider.center = new Vector3(0f, -.5f, 0f);
            groundCollider.size = new Vector3(10000f, 1f, 20f);
            groundCollider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(
                Root + "/Materials/TerrainStable.physicMaterial");
            GameObject inputObject = new GameObject("Landing Input");
            VehicleInputController input = inputObject.AddComponent<VehicleInputController>();
            input.SetInputMode(GameModeType.Adventure2_5D);
            GameObject instance = Object.Instantiate(source.gameplayPrefab2D, new Vector3(0f, 2.1f, 0f), Quaternion.identity);
            VehicleController controller = instance.GetComponent<VehicleController>();
            SuspensionController[] points = instance.GetComponentsInChildren<SuspensionController>(true);
            controller.AssignRuntimeReferences(testDefinition, input, points);
            InvokePrivate(controller, "Awake");
            MovementPlaneConstraint plane = instance.GetComponent<MovementPlaneConstraint>();
            if (plane != null) InvokePrivate(plane, "Awake");
            controller.Body.linearVelocity = new Vector3(speedKmh / 3.6f, verticalSpeedMps, 0f);
            bool touched = false;
            float rebound = 0f;
            float angular = 0f;
            float maximumPenetration = 0f;
            float maximumCompression = 0f;
            float impactSpeed = 0f;
            float firstReboundPeak = 0f;
            float secondReboundPeak = 0f;
            float settledVertical = 0f;
            float settledAngular = 0f;
            int reboundEpisodes = 0;
            bool upwardEpisode = false;
            float significantReboundThreshold = Mathf.Max(.75f, Mathf.Abs(verticalSpeedMps) * .15f);
            bool bodyTunneled = false;
            CapsuleCollider chassis = instance.GetComponent<CapsuleCollider>();
            const float step = .02f;
            for (int i = 0; i < 150; i++)
            {
                InvokePrivate(input, "Update");
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
                bool wasTouched = touched;
                touched |= controller.AnyGrounded;
                if (touched)
                {
                    float vertical = controller.Body.linearVelocity.y;
                    if (!wasTouched) impactSpeed = Mathf.Abs(vertical);
                    rebound = Mathf.Max(rebound, vertical);
                    bool upwardNow = vertical > significantReboundThreshold;
                    if (upwardNow && !upwardEpisode) reboundEpisodes++;
                    if (reboundEpisodes == 1) firstReboundPeak = Mathf.Max(firstReboundPeak, vertical);
                    else if (reboundEpisodes >= 2) secondReboundPeak = Mathf.Max(secondReboundPeak, vertical);
                    upwardEpisode = upwardNow || upwardEpisode && vertical > significantReboundThreshold * .4f;
                }
                angular = Mathf.Max(angular, Mathf.Abs(controller.Body.angularVelocity.z));
                for (int p = 0; p < points.Length; p++)
                    maximumCompression = Mathf.Max(maximumCompression, points[p].Compression01);
                maximumPenetration = Mathf.Max(maximumPenetration, Mathf.Max(0f, -chassis.bounds.min.y));
                bodyTunneled |= chassis.bounds.max.y < 0f;
                if (i >= 125)
                {
                    settledVertical = Mathf.Max(settledVertical, Mathf.Abs(controller.Body.linearVelocity.y));
                    settledAngular = Mathf.Max(settledAngular, Mathf.Abs(controller.Body.angularVelocity.z));
                }
            }
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(inputObject);
            Object.DestroyImmediate(ground);
            Object.DestroyImmediate(testDefinition);
            Physics.simulationMode = previousMode;
            return new LandingResult
            {
                level = level, speedKmh = speedKmh, maximumReboundMps = rebound,
                maximumAngularVelocity = angular, maximumGroundPenetration = maximumPenetration,
                maximumCompression01 = maximumCompression, reboundEpisodes = reboundEpisodes,
                impactSpeedMps = impactSpeed, firstReboundPeakMps = firstReboundPeak,
                secondReboundPeakMps = secondReboundPeak,
                settledVerticalSpeed = settledVertical, settledAngularVelocity = settledAngular,
                touchedDown = touched, bodyTunneled = bodyTunneled
            };
        }

        private static VehicleDefinition CreateMaximumTuneClone(VehicleDefinition source)
        {
            VehicleDefinition clone = Object.Instantiate(source);
            VehicleDefinition.TuneTargets values = source.tuneTargets;
            clone.engineMaxTunes = clone.accelerationMaxTunes = clone.suspensionMaxTunes = clone.gripMaxTunes = 0;
            clone.airControlMaxTunes = clone.downforceMaxTunes = clone.fuelMaxTunes = clone.turboMaxTunes = 0;
            clone.tuneTargets = new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = LastOnly(values.engineTopSpeedKmh),
                accelerationZeroToHundredSeconds = LastOnly(values.accelerationZeroToHundredSeconds),
                gripRatings = LastOnly(values.gripRatings),
                airControlTorque = LastOnly(values.airControlTorque),
                airborneDownforceAcceleration = LastOnly(values.airborneDownforceAcceleration),
                fuelRunDurationSeconds = LastOnly(values.fuelRunDurationSeconds)
            };
            clone.suspension.minTravel = clone.suspension.maxTravel = source.suspension.maxTravel;
            clone.suspension.stiffnessTuneScale = Vector2.one * source.suspension.stiffnessTuneScale.y;
            clone.suspension.dampingRatio = Vector2.one * source.suspension.dampingRatio.y;
            clone.engine.minTorqueNm = clone.engine.maxTorqueNm = source.engine.maxTorqueNm;
            return clone;
        }

        private static void LogResolvedPhysicsProfiles(BuiltAssets assets)
        {
            VehicleDefinition[] definitions = { assets.jeep, assets.formula, assets.rally, assets.sportBike };
            for (int i = 0; i < definitions.Length; i++)
            {
                VehicleDefinition definition = definitions[i];
                var upgrades = new VehicleUpgradeState
                {
                    engineTune = Mathf.Max(1, definition.engineMaxTunes),
                    accelerationTune = Mathf.Max(1, definition.accelerationMaxTunes),
                    suspensionTune = Mathf.Max(1, definition.suspensionMaxTunes),
                    gripTune = Mathf.Max(1, definition.gripMaxTunes),
                    airControlTune = Mathf.Max(1, definition.airControlMaxTunes),
                    downforceTune = Mathf.Max(1, definition.downforceMaxTunes),
                    turboTune = Mathf.Max(1, definition.turboMaxTunes),
                    fuelTune = Mathf.Max(1, definition.fuelMaxTunes)
                };
                ResolvedVehicleStats stats = definition.Resolve(upgrades);
                SuspensionPhysics.GetTravelLimits(definition.suspension, stats.suspensionTravel,
                    out float compression, out float extension);
                float staticCompression = definition.mass * .5f * Physics.gravity.magnitude /
                                          Mathf.Max(1f, stats.spring);
                float sag = SuspensionPhysics.CalculateSag01(staticCompression, compression, extension);
                Debug.Log($"PHYSICS PROFILE {definition.displayName} | chassis={definition.mass:0}kg, " +
                          $"wheel={definition.wheelMass:0}kg x2, radius={definition.wheelRadius:0.000}m, " +
                          $"COM={definition.centerOfMass}, rest={definition.suspension.restLength:0.000}m, " +
                          $"compression={compression:0.000}m, extension={extension:0.000}m, sag={sag:P1}, " +
                          $"spring={stats.spring:0}N/m, damping={stats.damping:0}Ns/m, " +
                          $"progressive={stats.progressiveSpringMultiplier:0.00}x, bumpStart={definition.suspension.bumpStopStart:P0}, " +
                          $"motor={stats.motorTorqueNm:0}Nm, grip={stats.gripRating}/{VehicleDefinition.GlobalGripMaximum} ({stats.grip:0.00}), " +
                          $"air={stats.airControlTorque:0}Nm, maxAir={definition.maxAirAngularVelocity:0.00}rad/s, " +
                          $"airDamping={definition.airAngularDamping:0.00}, downforce={stats.airborneDownforceAcceleration:0.00}m/s2");
            }
        }

        private static VehicleDefinition CreateSuspensionLevelClone(VehicleDefinition source, int level)
        {
            var upgrades = new VehicleUpgradeState { suspensionTune = level };
            ResolvedVehicleStats stats = source.Resolve(upgrades);
            VehicleDefinition clone = Object.Instantiate(source);
            clone.suspensionMaxTunes = 0;
            clone.suspension.minTravel = clone.suspension.maxTravel = stats.suspensionTravel;
            SuspensionPhysics.GetTravelLimits(clone.suspension, stats.suspensionTravel,
                out _, out float extension);
            float compression = Mathf.Max(.015f,
                clone.suspension.targetSag01 * stats.suspensionTravel - extension);
            float baseSpring = clone.mass * .5f * Physics.gravity.magnitude / compression;
            clone.suspension.stiffnessTuneScale = Vector2.one * (stats.spring / Mathf.Max(1f, baseSpring));
            float ratio = stats.damping / Mathf.Max(.001f,
                2f * Mathf.Sqrt(stats.spring * clone.mass * .5f));
            clone.suspension.dampingRatio = Vector2.one * ratio;
            clone.suspension.progressiveSpringMultiplier = stats.progressiveSpringMultiplier / .90f;
            return clone;
        }

        private static void ValidateAirControlContract(BuiltAssets assets)
        {
            ValidateTuningContract(assets);
            if (assets.formula.airControlMaxTunes != 0 || assets.formula.GetTuneSlotCount() != 5)
                throw new System.InvalidOperationException("Formula Air Control must be fixed and absent from tuning.");
            for (int i = 0; i < assets.formula.GetTuneSlotCount(); i++)
                if (assets.formula.GetTuneTypeForSlot(i) == VehicleUpgradeType.AirControl)
                    throw new System.InvalidOperationException("Formula tuning UI still exposes Air Control.");

            float jeepGas2D = SimulateAirTorque(assets.jeep, false, true);
            float jeepBrake2D = SimulateAirTorque(assets.jeep, false, false);
            float formulaGas2D = SimulateAirTorque(assets.formula, false, true);
            float formulaBrake2D = SimulateAirTorque(assets.formula, false, false);
            float formulaGas3D = SimulateAirTorque(assets.formula, true, true);
            float formulaBrake3D = SimulateAirTorque(assets.formula, true, false);
            float emptyJeepGas2D = SimulateAirTorque(assets.jeep, false, true, true);
            float emptyJeepBrake2D = SimulateAirTorque(assets.jeep, false, false, true);
            float emptyFormulaGas2D = SimulateAirTorque(assets.formula, false, true, true);
            float emptyFormulaBrake2D = SimulateAirTorque(assets.formula, false, false, true);
            float emptyFormulaGas3D = SimulateAirTorque(assets.formula, true, true, true);
            float emptyFormulaBrake3D = SimulateAirTorque(assets.formula, true, false, true);
            if (jeepGas2D <= 0f || jeepBrake2D >= 0f || formulaGas2D <= 0f || formulaBrake2D >= 0f ||
                formulaGas3D >= 0f || formulaBrake3D <= 0f)
                throw new System.InvalidOperationException("Air-control torque sign does not match GAS nose-up / BRAKE nose-down.");
            if (emptyJeepGas2D <= 0f || emptyJeepBrake2D >= 0f || emptyFormulaGas2D <= 0f ||
                emptyFormulaBrake2D >= 0f || emptyFormulaGas3D >= 0f || emptyFormulaBrake3D <= 0f)
                throw new System.InvalidOperationException("Fuel-zero air control must retain GAS nose-up / BRAKE nose-down.");
            if (Mathf.Abs(formulaGas2D) <= Mathf.Abs(jeepGas2D))
                throw new System.InvalidOperationException("Formula must have more airborne authority than Jeep.");

            AirLimitResult jeepLimit = SimulateAirLimit(assets.jeep, false);
            AirLimitResult formulaLimit2D = SimulateAirLimit(assets.formula, false);
            AirLimitResult formulaLimit3D = SimulateAirLimit(assets.formula, true);
            ValidateAirLimitResult(assets.jeep, jeepLimit, "JEEP 2D");
            ValidateAirLimitResult(assets.formula, formulaLimit2D, "FORMULA 2D");
            ValidateAirLimitResult(assets.formula, formulaLimit3D, "FORMULA 3D");

            Debug.Log($"Air control validation | JEEP 2D gasZ={jeepGas2D:0.000}, brakeZ={jeepBrake2D:0.000} | " +
                      $"FORMULA 2D gasZ={formulaGas2D:0.000}, brakeZ={formulaBrake2D:0.000} | " +
                      $"FORMULA 3D gasPitch={formulaGas3D:0.000}, brakePitch={formulaBrake3D:0.000} | " +
                      $"limits J={jeepLimit.held:0.00}, F2D={formulaLimit2D.held:0.00}, F3D={formulaLimit3D.held:0.00} rad/s | " +
                      "mapping=OK, damping=OK, fuel-zero rotation=OK, Formula fixed tuning=OK.");
        }

        private struct AirLimitResult
        {
            public float held;
            public float released;
        }

        private static void ValidateAirLimitResult(VehicleDefinition definition, AirLimitResult result, string label)
        {
            float maximum = definition.maxAirAngularVelocity;
            if (result.held > maximum + .06f || result.held < maximum * .72f)
                throw new System.InvalidOperationException($"{label} did not converge to its air angular limit: {result.held:0.00}/{maximum:0.00}.");
            if (result.released >= result.held * .65f || result.released <= .001f)
                throw new System.InvalidOperationException($"{label} airborne release damping is ineffective or freezes rotation.");
        }

        private static AirLimitResult SimulateAirLimit(VehicleDefinition source, bool roadMode)
        {
            NewScene();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition testDefinition = CreateMaximumTuneClone(source);
            GameObject inputObject = new GameObject("Air Limit Input");
            VehicleInputController input = inputObject.AddComponent<VehicleInputController>();
            input.SetInputMode(roadMode ? GameModeType.Adventure3D : GameModeType.Adventure2_5D);
            GameObject prefab = roadMode ? source.gameplayPrefab3D : source.gameplayPrefab2D;
            GameObject instance = Object.Instantiate(prefab, new Vector3(0f, 10000f, 0f), Quaternion.identity);
            VehicleRuntimeBase controller;
            MovementPlaneConstraint plane = null;
            if (roadMode)
            {
                RoadVehicleController road = instance.GetComponent<RoadVehicleController>();
                road.Assign(testDefinition, input);
                InvokePrivate(road, "Awake");
                controller = road;
            }
            else
            {
                VehicleController climb = instance.GetComponent<VehicleController>();
                climb.AssignRuntimeReferences(testDefinition, input,
                    instance.GetComponentsInChildren<SuspensionController>(true));
                InvokePrivate(climb, "Awake");
                plane = instance.GetComponent<MovementPlaneConstraint>();
                if (plane != null) InvokePrivate(plane, "Awake");
                controller = climb;
            }
            input.SetTouchGas(true);
            const float step = .02f;
            for (int i = 0; i < 250; i++)
            {
                InvokePrivate(input, "Update");
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
            }
            Vector3 axis = roadMode ? -instance.transform.right : Vector3.forward;
            float held = Mathf.Abs(Vector3.Dot(controller.Body.angularVelocity, axis));
            input.SetTouchGas(false);
            for (int i = 0; i < 50; i++)
            {
                InvokePrivate(input, "Update");
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
            }
            axis = roadMode ? -instance.transform.right : Vector3.forward;
            float released = Mathf.Abs(Vector3.Dot(controller.Body.angularVelocity, axis));
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(inputObject);
            Object.DestroyImmediate(testDefinition);
            Physics.simulationMode = previousMode;
            return new AirLimitResult { held = held, released = released };
        }

        private static void ValidateTuningContract(BuiltAssets assets)
        {
            int[] jeepMax = { 14, 15, 12, 12, 15 };
            int[] formulaMax = { 20, 20, 20, 20, 25 };
            var fresh = new VehicleUpgradeState();
            for (int i = 0; i < 5; i++)
            {
                VehicleUpgradeType jeepType = assets.jeep.GetTuneTypeForSlot(i);
                VehicleUpgradeType formulaType = assets.formula.GetTuneTypeForSlot(i);
                if (assets.jeep.GetMaxTunes(jeepType) != jeepMax[i] ||
                    assets.formula.GetMaxTunes(formulaType) != formulaMax[i] ||
                    fresh.GetTune(jeepType) != 1 || fresh.GetTune(formulaType) != 1)
                    throw new System.InvalidOperationException("Vehicle tuning slots, maxima or fresh Level 1 defaults are invalid.");
            }
            VehicleUpgradeType[] rallyTypes = { VehicleUpgradeType.Engine, VehicleUpgradeType.Turbo,
                VehicleUpgradeType.Suspension, VehicleUpgradeType.Grip, VehicleUpgradeType.Fuel };
            int[] rallyMax = { 20, 20, 14, 14, 15 };
            if (assets.rally.GetTuneSlotCount() != 5 || assets.rally.accelerationMaxTunes != 0)
                throw new System.InvalidOperationException("Rally must expose the five common performance stats.");
            for (int i = 0; i < rallyTypes.Length; i++)
                if (assets.rally.GetTuneTypeForSlot(i) != rallyTypes[i] ||
                    assets.rally.GetMaxTunes(rallyTypes[i]) != rallyMax[i] || fresh.GetTune(rallyTypes[i]) != 1)
                    throw new System.InvalidOperationException("Rally tuning order, maxima or fresh Level 1 defaults are invalid.");
            int[] sportMax = { 20, 20, 15, 20, 15 };
            for (int i = 0; i < 5; i++)
            {
                VehicleUpgradeType type = assets.sportBike.GetTuneTypeForSlot(i);
                if (assets.sportBike.GetMaxTunes(type) != sportMax[i] || fresh.GetTune(type) != 1)
                    throw new System.InvalidOperationException("Sport Bike tune maxima or Level 1 defaults are invalid.");
            }
            if (VehicleDefinition.GlobalGripMaximum != 1000 ||
                ResolveMaximumGrip(assets.jeep) != 300 || ResolveMaximumGrip(assets.formula) != 520 ||
                ResolveMaximumGrip(assets.rally) != 750 || ResolveMaximumGrip(assets.sportBike) != 500)
                throw new System.InvalidOperationException("Global 0-1000 Grip contract is invalid.");
            Debug.Log("Tuning validation: all fresh tunes=Level 1; Grip max Jeep/Formula/Rally/Sport=300/520/750/500 over 1000.");
        }

        private static int ResolveMaximumGrip(VehicleDefinition definition)
        {
            var state = new VehicleUpgradeState();
            state.gripTune = definition.gripMaxTunes;
            state.engineTune = Mathf.Max(1, definition.engineMaxTunes);
            state.accelerationTune = Mathf.Max(1, definition.accelerationMaxTunes);
            state.suspensionTune = Mathf.Max(1, definition.suspensionMaxTunes);
            state.fuelTune = Mathf.Max(1, definition.fuelMaxTunes);
            state.turboTune = Mathf.Max(1, definition.turboMaxTunes);
            return definition.Resolve(state).gripRating;
        }

        private static float SimulateAirTorque(VehicleDefinition source, bool roadMode, bool gas,
            bool emptyFuel = false)
        {
            NewScene();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition testDefinition = CreateMaximumTuneClone(source);
            GameObject inputObject = new GameObject("Air Control Input");
            VehicleInputController input = inputObject.AddComponent<VehicleInputController>();
            input.SetInputMode(roadMode ? GameModeType.Adventure3D : GameModeType.Adventure2_5D);
            GameObject prefab = roadMode ? source.gameplayPrefab3D : source.gameplayPrefab2D;
            GameObject instance = Object.Instantiate(prefab, new Vector3(0f, 10000f, 0f), Quaternion.identity);
            VehicleRuntimeBase controller;
            if (roadMode)
            {
                RoadVehicleController road = instance.GetComponent<RoadVehicleController>();
                road.Assign(testDefinition, input);
                InvokePrivate(road, "Awake");
                controller = road;
            }
            else
            {
                VehicleController climb = instance.GetComponent<VehicleController>();
                climb.AssignRuntimeReferences(testDefinition, input,
                    instance.GetComponentsInChildren<SuspensionController>(true));
                InvokePrivate(climb, "Awake");
                MovementPlaneConstraint plane = instance.GetComponent<MovementPlaneConstraint>();
                if (plane != null) InvokePrivate(plane, "Awake");
                controller = climb;
            }
            if (emptyFuel) controller.Fuel.SetNormalized(0f);
            input.SetTouchGas(gas);
            input.SetTouchBrake(!gas);
            InvokePrivate(input, "Update");
            InvokePrivate(controller, "FixedUpdate");
            Physics.Simulate(.02f);
            float longitudinalVelocity = roadMode
                ? Vector3.Dot(controller.Body.linearVelocity, instance.transform.forward)
                : controller.Body.linearVelocity.x;
            if (emptyFuel && Mathf.Abs(longitudinalVelocity) > .01f)
                throw new System.InvalidOperationException("Fuel-zero airborne input generated horizontal propulsion.");
            float angularVelocity = roadMode
                ? Vector3.Dot(controller.Body.angularVelocity, instance.transform.right)
                : controller.Body.angularVelocity.z;
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(inputObject);
            Object.DestroyImmediate(testDefinition);
            Physics.simulationMode = previousMode;
            return angularVelocity;
        }

        private static float[] LastOnly(float[] values) =>
            new[] { values != null && values.Length > 0 ? values[values.Length - 1] : 0f };

        private static float[] FirstOnly(float[] values) =>
            new[] { values != null && values.Length > 0 ? values[0] : 0f };

        private static void InvokePrivate(object target, string method)
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }

        private static BuiltAssets CreateAssets()
        {
            var assets = new BuiltAssets();
            string[] spritePaths = { JeepBodyPath, JeepWheel01Path, JeepWheel02Path, FormulaBodyPath,
                FormulaWheel01Path, FormulaWheel02Path, FormulaReferencePath, RallyBodyPath, RallyNoRoofPath, RallyRoofPath,
                RallyWheelPath, SportBikeBodyPath, SportBikeRetroPath, SportBikeWheel01Path, SportBikeWheel02Path,
                SportBikeGoldWheelPath, NeonSkyPath, NeonBackgroundPath, NeonTerrainPath, NeonGroundPath,
                NeonLiquidPath, NeonCautionPath, MainMenuBackgroundPath, HomeButtonPath, RaceButtonPath, SettingsButtonPath,
                SkinsButtonPath, StoreButtonPath, TuningButtonPath, UpgradeButtonPath, VehicleButtonPath,
                BrakePedalPath, BrakePedalPressedPath, GasPedalPath, GasPedalPressedPath,
                RpmGaugePath, RpmNeedlePath, SpeedGaugePath, SpeedNeedlePath,
                DriverPreviewPath, DriverHeadPath, DriverTorsoPath,
                DriverUpperArmPath, DriverForearmPath, DriverThighPath, DriverLowerLegPath,
                CountrysideSkyPath, CountrysideMountainsPath, FuelCanPath, Coin01Path, Coin05Path };
            for (int i = 0; i < spritePaths.Length; i++)
                ConfigureFormulaSprite(spritePaths[i], spritePaths[i] == CountrysideSkyPath ||
                    spritePaths[i] == CountrysideMountainsPath || spritePaths[i].StartsWith(NeonRoot) ||
                    spritePaths[i].StartsWith(ButtonRoot) ||
                    spritePaths[i].StartsWith(GameUiRoot) ? 4096 :
                    spritePaths[i] == FuelCanPath || spritePaths[i] == Coin01Path || spritePaths[i] == Coin05Path ? 512 : 2048);
            assets.jeepBody = AssetDatabase.LoadAssetAtPath<Sprite>(JeepBodyPath);
            assets.jeepWheel01 = AssetDatabase.LoadAssetAtPath<Sprite>(JeepWheel01Path);
            assets.jeepWheel02 = AssetDatabase.LoadAssetAtPath<Sprite>(JeepWheel02Path);
            assets.formulaBody = AssetDatabase.LoadAssetAtPath<Sprite>(FormulaBodyPath);
            assets.formulaWheel01 = AssetDatabase.LoadAssetAtPath<Sprite>(FormulaWheel01Path);
            assets.formulaWheel02 = AssetDatabase.LoadAssetAtPath<Sprite>(FormulaWheel02Path);
            assets.rallyBody = AssetDatabase.LoadAssetAtPath<Sprite>(RallyBodyPath);
            assets.rallyNoRoof = AssetDatabase.LoadAssetAtPath<Sprite>(RallyNoRoofPath);
            assets.rallyRoof = AssetDatabase.LoadAssetAtPath<Sprite>(RallyRoofPath);
            assets.rallyWheel = AssetDatabase.LoadAssetAtPath<Sprite>(RallyWheelPath);
            assets.sportBikeBody = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeBodyPath);
            assets.sportBikeRetro = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeRetroPath);
            assets.sportBikeWheel01 = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeWheel01Path);
            assets.sportBikeWheel02 = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeWheel02Path);
            assets.sportBikeGoldWheel = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeGoldWheelPath);
            assets.neonSky = AssetDatabase.LoadAssetAtPath<Sprite>(NeonSkyPath);
            assets.neonBackground = AssetDatabase.LoadAssetAtPath<Sprite>(NeonBackgroundPath);
            assets.neonTerrain = AssetDatabase.LoadAssetAtPath<Sprite>(NeonTerrainPath);
            assets.neonGround = AssetDatabase.LoadAssetAtPath<Sprite>(NeonGroundPath);
            assets.neonLiquid = AssetDatabase.LoadAssetAtPath<Sprite>(NeonLiquidPath);
            assets.neonCaution = AssetDatabase.LoadAssetAtPath<Sprite>(NeonCautionPath);
            assets.mainMenuBackground = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuBackgroundPath);
            assets.homeButton = AssetDatabase.LoadAssetAtPath<Sprite>(HomeButtonPath);
            assets.raceButton = AssetDatabase.LoadAssetAtPath<Sprite>(RaceButtonPath);
            assets.settingsButton = AssetDatabase.LoadAssetAtPath<Sprite>(SettingsButtonPath);
            assets.skinsButton = AssetDatabase.LoadAssetAtPath<Sprite>(SkinsButtonPath);
            assets.storeButton = AssetDatabase.LoadAssetAtPath<Sprite>(StoreButtonPath);
            assets.tuningButton = AssetDatabase.LoadAssetAtPath<Sprite>(TuningButtonPath);
            assets.upgradeButton = AssetDatabase.LoadAssetAtPath<Sprite>(UpgradeButtonPath);
            assets.vehicleButton = AssetDatabase.LoadAssetAtPath<Sprite>(VehicleButtonPath);
            assets.brakePedal = AssetDatabase.LoadAssetAtPath<Sprite>(BrakePedalPath);
            assets.brakePedalPressed = AssetDatabase.LoadAssetAtPath<Sprite>(BrakePedalPressedPath);
            assets.gasPedal = AssetDatabase.LoadAssetAtPath<Sprite>(GasPedalPath);
            assets.gasPedalPressed = AssetDatabase.LoadAssetAtPath<Sprite>(GasPedalPressedPath);
            assets.rpmGauge = AssetDatabase.LoadAssetAtPath<Sprite>(RpmGaugePath);
            assets.rpmNeedle = AssetDatabase.LoadAssetAtPath<Sprite>(RpmNeedlePath);
            assets.speedGauge = AssetDatabase.LoadAssetAtPath<Sprite>(SpeedGaugePath);
            assets.speedNeedle = AssetDatabase.LoadAssetAtPath<Sprite>(SpeedNeedlePath);
            assets.fuelCan = AssetDatabase.LoadAssetAtPath<Sprite>(FuelCanPath);
            assets.coin01 = AssetDatabase.LoadAssetAtPath<Sprite>(Coin01Path);
            assets.coin05 = AssetDatabase.LoadAssetAtPath<Sprite>(Coin05Path);
            assets.countrysideMaterial = UnlitMaterialAsset("CountrysideGround", new Color(0.34f, 0.16f, 0.07f));
            assets.countrysideEdgeMaterial = UnlitMaterialAsset("CountrysideEdge", new Color(0.30f, 0.78f, 0.20f));
            assets.neonCityMaterial = UnlitTextureMaterialAsset("NeonCityTerrain", assets.neonTerrain != null
                ? assets.neonTerrain.texture : null);
            assets.neonCityEdgeMaterial = UnlitTextureMaterialAsset("NeonCityEdge", assets.neonGround != null
                ? assets.neonGround.texture : null);
            assets.nearMountains = MaterialAsset("MountainsNear", new Color(0.12f, 0.32f, 0.24f));
            assets.farMountains = MaterialAsset("MountainsFar", new Color(0.22f, 0.38f, 0.46f));
            assets.road = MaterialAsset("Road", new Color(0.17f, 0.18f, 0.20f));
            assets.grass = MaterialAsset("RoadGrass", new Color(0.18f, 0.42f, 0.19f));
            assets.shortcut = MaterialAsset("ShortcutTech", new Color(0.72f, 0.12f, 0.68f));
            // Virtual wheels own all longitudinal grip. Chassis friction only creates a pitch impulse
            // when the rounded body skims terrain during a hard landing, so keep the body frictionless.
            assets.chassisPhysics = PhysicsMaterialAsset("VehicleBodyLowBounce", 0f, 0f);
            assets.groundPhysics = PhysicsMaterialAsset("TerrainStable", .72f, 0f);
            assets.tirePhysics = PhysicsMaterialAsset("VehicleTireArcade", 1f, 0f);
            assets.tirePhysics.frictionCombine = PhysicsMaterialCombine.Maximum;
            EditorUtility.SetDirty(assets.tirePhysics);

            assets.jeep = LoadOrCreate<VehicleDefinition>(Root + "/VehicleData/MainVehicle.asset");
            assets.jeep.vehicleId = "car-01";
            assets.jeep.displayName = "JEEP";
            assets.jeep.archetype = VehicleArchetype.Jeep;
            assets.jeep.engineMaxTunes = 14; assets.jeep.accelerationMaxTunes = 15;
            assets.jeep.suspensionMaxTunes = 12; assets.jeep.gripMaxTunes = 12;
            assets.jeep.airControlMaxTunes = 0; assets.jeep.downforceMaxTunes = 0; assets.jeep.fuelMaxTunes = 15;
            assets.jeep.tuneTargets = new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = new float[] { 60, 63, 66, 69, 72, 75, 78, 81, 84, 87, 90, 93, 96, 98, 100 },
                accelerationZeroToHundredSeconds = new float[] { 12f, 11.6f, 11.2f, 10.8f, 10.4f, 10f, 9.6f, 9.2f, 8.8f, 8.5f, 8.2f, 7.9f, 7.6f, 7.4f, 7.2f, 7f },
                gripRatings = new float[] { 50f, 72f, 94f, 116f, 138f, 160f, 182f, 204f, 226f, 248f, 266f, 284f, 300f },
                airControlTorque = new float[] { 3200f }, airborneDownforceAcceleration = new float[] { 0f },
                fuelRunDurationSeconds = new float[] { 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 }
            };
            assets.jeep.suspension.minTravel = .28f; assets.jeep.suspension.maxTravel = .44f;
            assets.jeep.suspension.restLength = .52f;
            assets.jeep.suspension.compressionTravelRatio = .90f;
            assets.jeep.suspension.targetSag01 = .30f;
            assets.jeep.suspension.stiffnessTuneScale = new Vector2(.82f, 1.32f);
            assets.jeep.suspension.dampingRatio = new Vector2(.80f, .94f);
            assets.jeep.suspension.springMin = 70000f; assets.jeep.suspension.springMax = 82000f;
            assets.jeep.suspension.dampingMin = 10000f; assets.jeep.suspension.dampingMax = 12000f;
            assets.jeep.suspension.bumpStopStart = .70f;
            assets.jeep.suspension.progressiveSpringMultiplier = 2f;
            assets.jeep.suspension.hardStopSpringMultiplier = 8f;
            assets.jeep.suspension.maximumAcceleration = 24f;
            assets.jeep.suspension.maximumReboundAcceleration = 3f;
            assets.jeep.grip.minGrip = 1.05f; assets.jeep.grip.maxGrip = 1.40f;
            assets.jeep.grip.slipThreshold = .20f; assets.jeep.grip.slidingGripMultiplier = .68f;
            assets.jeep.grip.tractionRecoveryPerSecond = 4.5f;
            assets.jeep.engine.minTorqueNm = 155f; assets.jeep.engine.maxTorqueNm = 230f;
            assets.jeep.fuel.capacityUnits = 100f; assets.jeep.fuel.minimumRunDurationSeconds = 15f;
            assets.jeep.fuel.maximumRunDurationSeconds = 30f; assets.jeep.fuel.referenceRunDurationSeconds = 30f;
            assets.jeep.mass = 1150f; assets.jeep.wheelMass = 48f; assets.jeep.wheelRadius = .45f;
            assets.jeep.engine.speedTorqueCurve = ArcadeSpeedTorqueCurve();
            assets.jeep.collisionPitchInertiaMultiplier = 1.55f;
            assets.jeep.maximumDepenetrationVelocity = 3f;
            assets.jeep.halfWheelbase = 1.18f; assets.jeep.halfTrack = .78f;
            assets.jeep.topSpeedMps = 100f / 3.6f; assets.jeep.airControlTorque = 3200f;
            assets.jeep.maxAirAngularVelocity = 2.5f; assets.jeep.airAngularAcceleration = 14f;
            // The visual/physical Jeep axles sit at -1.12 / +1.24 m. Center the mass between them so
            // equal wheel-first landing forces cannot manufacture a pitch impulse on flat terrain.
            assets.jeep.airAngularDamping = 1.8f; assets.jeep.centerOfMass = new Vector3(.06f, -.20f, 0f);
            assets.jeep.accelerationForceCalibration = 1.02f;
            assets.jeep.previewSprite = assets.jeepBody;
            assets.jeep.skins = new[] { Customization("jeep-skin-default", "JEEP DEFAULT", assets.jeepBody) };
            assets.jeep.wheels = new[]
            {
                Customization("jeep-wheel-01", "JEEP WHEEL 01", assets.jeepWheel01),
                Customization("jeep-wheel-02", "JEEP WHEEL 02", assets.jeepWheel02)
            };
            assets.jeep.visualWidth = 3.80f;
            assets.jeep.flipBodyX = false;
            assets.jeep.rearWheelVisualDiameter = 0f; assets.jeep.frontWheelVisualDiameter = 0f;
            assets.jeep.bodyVisualOffset = new Vector2(0f, .16f);
            assets.jeep.rearWheelVisualPosition = new Vector2(-1.12f, -.44f);
            assets.jeep.frontWheelVisualPosition = new Vector2(1.24f, -.44f);
            assets.jeep.wheelVisualDiameter = .90f;
            assets.jeep.driverPose = CreateJeepDriverPose();
            EditorUtility.SetDirty(assets.jeep);

            assets.formula = LoadOrCreate<VehicleDefinition>(Root + "/VehicleData/FormulaVehicle.asset");
            assets.formula.vehicleId = "formula-01"; assets.formula.displayName = "FORMULA";
            assets.formula.archetype = VehicleArchetype.Formula; assets.formula.previewSprite = assets.formulaBody;
            assets.formula.engineMaxTunes = 20; assets.formula.accelerationMaxTunes = 20;
            assets.formula.suspensionMaxTunes = 20; assets.formula.gripMaxTunes = 20;
            assets.formula.airControlMaxTunes = 0; assets.formula.downforceMaxTunes = 0; assets.formula.fuelMaxTunes = 25;
            assets.formula.tuneTargets = CreateFormulaTuneTargets();
            assets.formula.mass = 780f; assets.formula.wheelMass = 28f; assets.formula.wheelRadius = .48f;
            assets.formula.engine.speedTorqueCurve = ArcadeSpeedTorqueCurve();
            assets.formula.collisionPitchInertiaMultiplier = 4.25f;
            assets.formula.maximumDepenetrationVelocity = 3f;
            assets.formula.halfWheelbase = 1.46f; assets.formula.halfTrack = .78f;
            assets.formula.topSpeedMps = 300f / 3.6f;
            assets.formula.brakeForce = 18000f; assets.formula.reverseForce = 6000f;
            assets.formula.accelerationForceCalibration = 1.01f;
            assets.formula.airControlTorque = 9000f;
            assets.formula.maxAirAngularVelocity = 3.5f; assets.formula.airAngularAcceleration = 24f;
            assets.formula.airAngularDamping = 1.5f; assets.formula.centerOfMass = new Vector3(0f, -.30f, 0f);
            assets.formula.engine.minTorqueNm = 300f; assets.formula.engine.maxTorqueNm = 700f;
            assets.formula.engine.drivetrainEfficiency = .88f;
            assets.formula.transmission.forwardGearRatios = new[] { 3.80f, 2.75f, 2.15f, 1.75f, 1.48f, 1.30f, 1.18f };
            assets.formula.suspension.restLength = .38f; assets.formula.suspension.minTravel = .16f;
            assets.formula.suspension.maxTravel = .25f; assets.formula.suspension.compressionTravelRatio = .90f;
            assets.formula.suspension.targetSag01 = .29f;
            assets.formula.suspension.stiffnessTuneScale = new Vector2(.78f, 1.25f);
            assets.formula.suspension.dampingRatio = new Vector2(.96f, 1.08f);
            assets.formula.suspension.springMin = 82000f; assets.formula.suspension.springMax = 96000f;
            assets.formula.suspension.dampingMin = 14000f; assets.formula.suspension.dampingMax = 16000f;
            assets.formula.suspension.bumpStopStart = .65f;
            assets.formula.suspension.progressiveSpringMultiplier = 3f;
            assets.formula.suspension.hardStopSpringMultiplier = 10f;
            assets.formula.suspension.maximumAcceleration = 20f;
            assets.formula.suspension.maximumReboundAcceleration = .5f;
            assets.formula.grip.minGrip = 1.90f; assets.formula.grip.maxGrip = 1.90f;
            assets.formula.grip.slipThreshold = .14f; assets.formula.grip.slidingGripMultiplier = .74f;
            assets.formula.grip.tractionRecoveryPerSecond = 7f;
            assets.formula.grip.rollingResistance = .012f; assets.formula.grip.aerodynamicDrag = .28f;
            assets.formula.fuel.capacityUnits = 100f; assets.formula.fuel.minimumRunDurationSeconds = 20f;
            assets.formula.fuel.maximumRunDurationSeconds = 45f; assets.formula.fuel.referenceRunDurationSeconds = 45f;
            assets.formula.skins = new[] { Customization("formula-skin-default", "FORMULA DEFAULT", assets.formulaBody) };
            assets.formula.wheels = new[]
            {
                Customization("formula-wheel-02", "FORMULA WHEEL 02", assets.formulaWheel02),
                Customization("formula-wheel-01", "GHOSTY WHEELS", assets.formulaWheel01, false, 500)
            };
            assets.formula.visualWidth = 4.80f;
            assets.formula.flipBodyX = false;
            assets.formula.rearWheelVisualDiameter = 0f; assets.formula.frontWheelVisualDiameter = 0f;
            assets.formula.bodyVisualOffset = new Vector2(0f, .10f);
            assets.formula.rearWheelVisualPosition = new Vector2(-1.46f, -.33f);
            assets.formula.frontWheelVisualPosition = new Vector2(1.46f, -.33f);
            assets.formula.wheelVisualDiameter = .96f;
            assets.formula.driverPose = CreateFormulaDriverPose();
            EditorUtility.SetDirty(assets.formula);

            assets.rally = LoadOrCreate<VehicleDefinition>(Root + "/VehicleData/RallyVehicle.asset");
            assets.rally.vehicleId = "rally-01"; assets.rally.displayName = "RALLY";
            assets.rally.archetype = VehicleArchetype.Rally; assets.rally.previewSprite = assets.rallyBody;
            assets.rally.engineMaxTunes = 20; assets.rally.accelerationMaxTunes = 0;
            assets.rally.suspensionMaxTunes = 14; assets.rally.gripMaxTunes = 14;
            assets.rally.airControlMaxTunes = 0; assets.rally.downforceMaxTunes = 0;
            assets.rally.fuelMaxTunes = 15; assets.rally.turboMaxTunes = 20;
            assets.rally.tuneTargets = CreateRallyTuneTargets();
            assets.rally.mass = 1030f; assets.rally.wheelMass = 36f; assets.rally.wheelRadius = .51f;
            assets.rally.engine.speedTorqueCurve = ArcadeSpeedTorqueCurve();
            assets.rally.collisionPitchInertiaMultiplier = 3.75f;
            assets.rally.maximumDepenetrationVelocity = 3f;
            assets.rally.halfWheelbase = 1.35f; assets.rally.halfTrack = .78f;
            assets.rally.topSpeedMps = 280f / 3.6f;
            assets.rally.brakeForce = 16500f; assets.rally.reverseForce = 5800f;
            assets.rally.accelerationForceCalibration = 1f;
            assets.rally.airControlTorque = 6500f;
            assets.rally.maxAirAngularVelocity = 3.05f; assets.rally.airAngularAcceleration = 20f;
            assets.rally.airAngularDamping = 1.6f; assets.rally.centerOfMass = new Vector3(.01f, -.28f, 0f);
            assets.rally.engine.minTorqueNm = 280f; assets.rally.engine.maxTorqueNm = 650f;
            assets.rally.engine.drivetrainEfficiency = .86f;
            assets.rally.transmission.forwardGearRatios = new[] { 4.20f, 3.10f, 2.50f, 2.10f, 1.82f, 1.58f, 1.38f };
            assets.rally.suspension.restLength = .56f; assets.rally.suspension.minTravel = .25f;
            assets.rally.suspension.maxTravel = .40f; assets.rally.suspension.compressionTravelRatio = .89f;
            assets.rally.suspension.targetSag01 = .30f;
            assets.rally.suspension.stiffnessTuneScale = new Vector2(.82f, 1.33f);
            assets.rally.suspension.dampingRatio = new Vector2(.90f, 1.02f);
            assets.rally.suspension.springMin = 72000f; assets.rally.suspension.springMax = 84000f;
            assets.rally.suspension.dampingMin = 12500f; assets.rally.suspension.dampingMax = 15000f;
            assets.rally.suspension.bumpStopStart = .62f;
            assets.rally.suspension.progressiveSpringMultiplier = 3.4f;
            assets.rally.suspension.hardStopSpringMultiplier = 9f;
            assets.rally.suspension.maximumAcceleration = 26f;
            assets.rally.suspension.maximumReboundAcceleration = .5f;
            assets.rally.grip.minGrip = 1.25f; assets.rally.grip.maxGrip = 1.70f;
            assets.rally.grip.slipThreshold = .17f; assets.rally.grip.slidingGripMultiplier = .75f;
            assets.rally.grip.tractionRecoveryPerSecond = 6.5f;
            assets.rally.grip.rollingResistance = .014f; assets.rally.grip.aerodynamicDrag = .34f;
            assets.rally.fuel.capacityUnits = 100f; assets.rally.fuel.minimumRunDurationSeconds = 50f;
            assets.rally.fuel.maximumRunDurationSeconds = 50f; assets.rally.fuel.referenceRunDurationSeconds = 50f;
            assets.rally.skins = new[] { Customization("rally-skin-default", "RALLY DEFAULT", assets.rallyBody) };
            assets.rally.wheels = new[] { Customization("rally-wheel-01", "RALLY WHEEL 01", assets.rallyWheel) };
            assets.rally.damagedBodySprite = assets.rallyNoRoof;
            assets.rally.detachableRoofSprite = assets.rallyRoof;
            assets.rally.visualWidth = 4.65f;
            assets.rally.flipBodyX = false;
            assets.rally.rearWheelVisualDiameter = 0f; assets.rally.frontWheelVisualDiameter = 0f;
            assets.rally.bodyVisualOffset = new Vector2(0f, .16f);
            assets.rally.rearWheelVisualPosition = new Vector2(-1.32f, -.49f);
            assets.rally.frontWheelVisualPosition = new Vector2(1.34f, -.49f);
            assets.rally.wheelVisualDiameter = 1.02f;
            assets.rally.driverPose = CreateRallyDriverPose();
            EditorUtility.SetDirty(assets.rally);

            assets.sportBike = LoadOrCreate<VehicleDefinition>(Root + "/VehicleData/SportBikeVehicle.asset");
            assets.sportBike.vehicleId = "sport-bike-01";
            assets.sportBike.displayName = "SPORT BIKE";
            assets.sportBike.archetype = VehicleArchetype.SportBike;
            assets.sportBike.previewSprite = assets.sportBikeBody;
            assets.sportBike.engineMaxTunes = 20;
            assets.sportBike.accelerationMaxTunes = 20;
            assets.sportBike.suspensionMaxTunes = 15;
            assets.sportBike.gripMaxTunes = 20;
            assets.sportBike.airControlMaxTunes = 0;
            assets.sportBike.downforceMaxTunes = 0;
            assets.sportBike.fuelMaxTunes = 15;
            assets.sportBike.turboMaxTunes = 0;
            assets.sportBike.tuneTargets = CreateSportBikeTuneTargets();
            assets.sportBike.mass = 260f;
            assets.sportBike.wheelMass = 15f;
            assets.sportBike.wheelRadius = .38f;
            assets.sportBike.halfWheelbase = 1.58f;
            assets.sportBike.halfTrack = .46f;
            assets.sportBike.topSpeedMps = 360f / 3.6f;
            assets.sportBike.brakeForce = 9800f;
            assets.sportBike.reverseForce = 3300f;
            assets.sportBike.airControlTorque = 7600f;
            assets.sportBike.collisionPitchInertiaMultiplier = 4.1f;
            assets.sportBike.maximumDepenetrationVelocity = 2.8f;
            assets.sportBike.maxAirAngularVelocity = 3.2f;
            assets.sportBike.airAngularAcceleration = 21f;
            assets.sportBike.airAngularDamping = 1.7f;
            assets.sportBike.accelerationForceCalibration = 1f;
            assets.sportBike.centerOfMass = new Vector3(-.04f, -.24f, 0f);
            assets.sportBike.engine.minTorqueNm = 85f;
            assets.sportBike.engine.maxTorqueNm = 260f;
            assets.sportBike.engine.drivetrainEfficiency = .86f;
            assets.sportBike.engine.speedTorqueCurve = ArcadeSpeedTorqueCurve();
            assets.sportBike.transmission.forwardGearRatios =
                new[] { 3.60f, 2.65f, 2.08f, 1.70f, 1.44f, 1.25f, 1.10f };
            assets.sportBike.transmission.finalDrive = 3.05f;
            assets.sportBike.suspension.restLength = .40f;
            assets.sportBike.suspension.minTravel = .15f;
            assets.sportBike.suspension.maxTravel = .27f;
            assets.sportBike.suspension.compressionTravelRatio = .88f;
            assets.sportBike.suspension.targetSag01 = .29f;
            assets.sportBike.suspension.stiffnessTuneScale = new Vector2(.88f, 1.12f);
            assets.sportBike.suspension.dampingRatio = new Vector2(.94f, 1.08f);
            assets.sportBike.suspension.springMin = 18000f;
            assets.sportBike.suspension.springMax = 26000f;
            assets.sportBike.suspension.dampingMin = 2500f;
            assets.sportBike.suspension.dampingMax = 3900f;
            assets.sportBike.suspension.bumpStopStart = .68f;
            assets.sportBike.suspension.progressiveSpringMultiplier = 3f;
            assets.sportBike.suspension.hardStopSpringMultiplier = 9f;
            assets.sportBike.suspension.maximumAcceleration = 24f;
            assets.sportBike.suspension.maximumReboundAcceleration = 1.2f;
            assets.sportBike.grip.minGrip = 1.02f;
            assets.sportBike.grip.maxGrip = 1.65f;
            assets.sportBike.grip.slipThreshold = .15f;
            assets.sportBike.grip.slidingGripMultiplier = .72f;
            assets.sportBike.grip.tractionRecoveryPerSecond = 6.8f;
            assets.sportBike.grip.rollingResistance = .010f;
            assets.sportBike.grip.aerodynamicDrag = .20f;
            assets.sportBike.fuel.capacityUnits = 100f;
            assets.sportBike.fuel.minimumRunDurationSeconds = 15f;
            assets.sportBike.fuel.maximumRunDurationSeconds = 45f;
            assets.sportBike.fuel.referenceRunDurationSeconds = 45f;
            assets.sportBike.skins = new[]
            {
                Customization("sport-bike-default", "SPORT BIKE DEFAULT", assets.sportBikeBody),
                Customization("sport-bike-retro", "BIKE RETRO", assets.sportBikeRetro, false, 1000)
            };
            assets.sportBike.wheels = new[]
            {
                Customization("sport-bike-wheel-01", "BIKE WHEEL 01", assets.sportBikeWheel01, true, 0,
                    VisibleSpriteDiameter(assets.sportBikeWheel01)),
                Customization("sport-bike-wheel-02", "BIKE WHEEL 02", assets.sportBikeWheel02, true, 0,
                    VisibleSpriteDiameter(assets.sportBikeWheel02)),
                Customization("sport-bike-gold-wheel", "GOLD WHEEL", assets.sportBikeGoldWheel, false, 500,
                    VisibleSpriteDiameter(assets.sportBikeGoldWheel))
            };
            assets.sportBike.visualWidth = 3.75f;
            assets.sportBike.flipBodyX = true;
            assets.sportBike.bodyVisualOffset = new Vector2(0f, .10f);
            assets.sportBike.rearWheelVisualPosition = new Vector2(-1.59f, -.48f);
            assets.sportBike.frontWheelVisualPosition = new Vector2(1.56f, -.49f);
            assets.sportBike.wheelVisualDiameter = .85f;
            assets.sportBike.rearWheelVisualDiameter = .88f;
            assets.sportBike.frontWheelVisualDiameter = .82f;
            assets.sportBike.driverPose = CreateSportBikeDriverPose();
            EditorUtility.SetDirty(assets.sportBike);

            assets.stickmanBlack = LoadOrCreate<DriverDefinition>(Root + "/VehicleData/StickmanBlack.asset");
            assets.stickmanBlack.driverId = "stickman-black";
            assets.stickmanBlack.displayName = "STICKMAN BLACK";
            assets.stickmanBlack.unlockedByDefault = true;
            assets.stickmanBlack.previewSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DriverPreviewPath);
            assets.stickmanBlack.head = AssetDatabase.LoadAssetAtPath<Sprite>(DriverHeadPath);
            assets.stickmanBlack.torso = AssetDatabase.LoadAssetAtPath<Sprite>(DriverTorsoPath);
            assets.stickmanBlack.upperArm = AssetDatabase.LoadAssetAtPath<Sprite>(DriverUpperArmPath);
            assets.stickmanBlack.forearm = AssetDatabase.LoadAssetAtPath<Sprite>(DriverForearmPath);
            assets.stickmanBlack.thigh = AssetDatabase.LoadAssetAtPath<Sprite>(DriverThighPath);
            assets.stickmanBlack.lowerLeg = AssetDatabase.LoadAssetAtPath<Sprite>(DriverLowerLegPath);
            EditorUtility.SetDirty(assets.stickmanBlack);

            CreateVehiclePrefabs(assets);

            assets.countrysideCoinRoute = LoadOrCreate<CoinRouteDefinition>(
                Root + "/MapProfiles/CountrysideCoinRoute.asset");
            assets.countrysideCoinRoute.routeSeed = 9137;
            assets.countrysideCoinRoute.cellLength = 48f;
            assets.countrysideCoinRoute.firstCoinDistance = 20f;
            assets.countrysideCoinRoute.anchorJitter = 5f;
            assets.countrysideCoinRoute.surfaceOffset = 1.05f;
            assets.countrysideCoinRoute.sparseEndDistance = 300f;
            assets.countrysideCoinRoute.growingEndDistance = 600f;
            assets.countrysideCoinRoute.variedEndDistance = 1200f;
            assets.countrysideCoinRoute.allFiveStartDistance = 2600f;
            EditorUtility.SetDirty(assets.countrysideCoinRoute);

            assets.countryside = LoadOrCreate<MapProfile>(Root + "/MapProfiles/Countryside.asset");
            assets.countryside.mapId = "countryside";
            assets.countryside.displayName = "COUNTRYSIDE";
            assets.countryside.supportedGameMode = GameModeType.Adventure2_5D;
            assets.countryside.seed = 18472;
            assets.countryside.coinRoute = assets.countrysideCoinRoute;
            assets.countryside.amplitude = 15.5f;
            assets.countryside.frequency = 0.019f;
            assets.countryside.introductoryAmplitude = 0.12f;
            assets.countryside.introEndDistance = 500f;
            assets.countryside.easyEndDistance = 2000f;
            assets.countryside.normalEndDistance = 4000f;
            assets.countryside.hardEndDistance = 10000f;
            assets.countryside.difficultyCurve = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.25f, 0.22f), new Keyframe(0.60f, 0.55f),
                new Keyframe(0.80f, 0.82f), new Keyframe(1f, 1f));
            assets.countryside.maximumDifficulty = 1f;
            assets.countryside.sectionLength = 64f;
            assets.countryside.technicalSectionChance = 0.42f;
            assets.countryside.technicalDetailStrength = 0.70f;
            assets.countryside.flatFeatureWeight = 0.06f;
            assets.countryside.hillFeatureWeight = 0.47f;
            assets.countryside.valleyFeatureWeight = 0.25f;
            assets.countryside.technicalFeatureWeight = 0.22f;
            assets.countryside.maxSlopeAngle = 32f;
            assets.countryside.maxHeightDeltaPerSample = Mathf.Tan(32f * Mathf.Deg2Rad) * (80f / 48f);
            assets.countryside.minimumValleyWidth = 20f;
            assets.countryside.minimumLandingLength = 16f;
            assets.countryside.maximumCrestSharpness = 6f;
            assets.countryside.smoothingPasses = 1;
            assets.countryside.initialFuelGap = 5f;
            assets.countryside.fuelGapIncrease = 5f;
            assets.countryside.terrainMaterial = assets.countrysideMaterial;
            assets.countryside.terrainEdgeMaterial = assets.countrysideEdgeMaterial;
            assets.countryside.groundPhysicsMaterial = assets.groundPhysics;
            assets.countryside.terrainEdgeWidth = 0.18f;
            assets.countryside.terrainVisualDepth = 20f;
            assets.countryside.supportsLiquidExposure = false;
            assets.countrysideEnvironment = LoadOrCreate<EnvironmentDefinition>(Root + "/MapProfiles/CountrysideEnvironment.asset");
            assets.countrysideEnvironment.environmentId = "countryside";
            assets.countrysideEnvironment.sky = AssetDatabase.LoadAssetAtPath<Sprite>(CountrysideSkyPath);
            assets.countrysideEnvironment.mountains = AssetDatabase.LoadAssetAtPath<Sprite>(CountrysideMountainsPath);
            assets.countrysideEnvironment.mountainParallaxX = .20f;
            assets.countrysideEnvironment.mountainParallaxY = .04f;
            assets.countrysideEnvironment.mountainVisualHeight = 17f;
            assets.countrysideEnvironment.mountainBaseY = -4f;
            assets.countrysideEnvironment.pooledMountainSegments = 5;
            assets.countrysideEnvironment.seamOverlap = .012f;
            assets.countryside.environment = assets.countrysideEnvironment;
            EditorUtility.SetDirty(assets.countrysideEnvironment);

            assets.neonCityEnvironment = LoadOrCreate<EnvironmentDefinition>(
                Root + "/MapProfiles/NeonCityEnvironment.asset");
            assets.neonCityEnvironment.environmentId = "neon-city";
            assets.neonCityEnvironment.sky = assets.neonSky;
            assets.neonCityEnvironment.mountains = assets.neonBackground;
            assets.neonCityEnvironment.mountainParallaxX = .18f;
            assets.neonCityEnvironment.mountainParallaxY = .035f;
            assets.neonCityEnvironment.mountainVisualHeight = 15.5f;
            assets.neonCityEnvironment.mountainBaseY = -3.2f;
            assets.neonCityEnvironment.pooledMountainSegments = 5;
            assets.neonCityEnvironment.seamOverlap = .018f;
            EditorUtility.SetDirty(assets.neonCityEnvironment);

            assets.neonCity = LoadOrCreate<MapProfile>(Root + "/MapProfiles/NeonCity.asset");
            assets.neonCity.mapId = "neon-city";
            assets.neonCity.displayName = "NEON CITY";
            assets.neonCity.supportedGameMode = GameModeType.Adventure2_5D;
            assets.neonCity.seed = 620031;
            assets.neonCity.coinRoute = assets.countrysideCoinRoute;
            assets.neonCity.elevationProfile = null;
            assets.neonCity.baseHeight = 0f;
            assets.neonCity.amplitude = 22f;
            assets.neonCity.neonMaximumAmplitude = 28f;
            assets.neonCity.neonAmplitudeSaturationCycles = 3f;
            assets.neonCity.frequency = .0105f;
            assets.neonCity.introductoryAmplitude = .45f;
            assets.neonCity.introEndDistance = 500f;
            assets.neonCity.easyEndDistance = 6000f;
            assets.neonCity.normalEndDistance = 12000f;
            assets.neonCity.hardEndDistance = 24000f;
            assets.neonCity.maximumDifficulty = 1f;
            assets.neonCity.sectionLength = 92f;
            assets.neonCity.recoveryEverySections = 4;
            assets.neonCity.technicalSectionChance = .20f;
            assets.neonCity.technicalDetailStrength = .34f;
            assets.neonCity.flatFeatureWeight = .24f;
            assets.neonCity.hillFeatureWeight = .48f;
            assets.neonCity.valleyFeatureWeight = .16f;
            assets.neonCity.technicalFeatureWeight = .12f;
            assets.neonCity.maxSlopeAngle = 28f;
            assets.neonCity.maxHeightDeltaPerSample = Mathf.Tan(28f * Mathf.Deg2Rad) * (80f / 48f);
            assets.neonCity.minimumValleyWidth = 38f;
            assets.neonCity.minimumLandingLength = 26f;
            assets.neonCity.maximumCrestSharpness = 4f;
            assets.neonCity.smoothingPasses = 2;
            assets.neonCity.initialFuelGap = 55f;
            assets.neonCity.fuelGapIncrease = 10f;
            assets.neonCity.fuelPlacementSearchRadius = 28f;
            assets.neonCity.fuelPlacementSearchStep = 2f;
            assets.neonCity.fuelMaximumPlacementSlope = 14f;
            assets.neonCity.environment = assets.neonCityEnvironment;
            assets.neonCity.terrainMaterial = assets.neonCityMaterial;
            assets.neonCity.terrainEdgeMaterial = assets.neonCityEdgeMaterial;
            assets.neonCity.groundPhysicsMaterial = assets.groundPhysics;
            assets.neonCity.terrainEdgeWidth = .18f;
            assets.neonCity.terrainVisualDepth = 22f;
            assets.neonCity.supportsLiquidExposure = true;
            assets.neonCity.liquidChallenges = new LiquidChallengeSettings
            {
                cycleLength = 6000f,
                challengesPerCycle = 8,
                cycleStartSafetyMargin = 260f,
                slotJitter = 120f,
                runUpLength = 42f,
                rampTransitionLength = 6f,
                rampHorizontalLength = 12f,
                rampAngleDegrees = 20f,
                landingLength = 26f,
                recoveryLength = 48f,
                basePoolLength = 18f,
                minimumPoolLength = 16f,
                maximumPoolLength = 34f,
                cycleGrowth = .85f,
                growthSaturation = 2.5f,
                deterministicLengthVariation = .12f,
                visualDepth = 7f,
                surfaceOffset = .15f,
                cautionSprite = assets.neonCaution,
                cautionDistanceBeforeRamp = 10f,
                cautionBelowSurface = 1.65f,
                cautionWidth = 3.2f,
                cautionSortingOrder = 4,
                liquidSprite = assets.neonLiquid,
                liquidSortingOrder = 3,
                normalAlpha = 1f,
                submergedAlpha = .65f,
                alphaLerpSpeed = 7f,
                exposureLimitSeconds = 10f,
                entrySpeedRemaining = .10f,
                entrySlowdownSeconds = .35f,
                continuousViscosity = 2.4f,
                liquidDriveAcceleration = 9f
            };
            EditorUtility.SetDirty(assets.neonCity);
            assets.elevation = LoadOrCreate<TerrainElevationProfile>(Root + "/MapProfiles/CountrysideElevation.asset");
            assets.elevation.AssignSource(assets.countryside);
            assets.countryside.elevationProfile = assets.elevation;
            EditorUtility.SetDirty(assets.elevation);
            EditorUtility.SetDirty(assets.countryside);

            assets.curvature = LoadOrCreate<RoadCurvatureProfile>(Root + "/MapProfiles/CountrysideCurvature.asset");
            assets.curvature.seed = 18472; assets.curvature.intensity = 10f; assets.curvature.frequency = 0.006f;
            EditorUtility.SetDirty(assets.curvature);
            assets.countrysideRoad = LoadOrCreate<RoadMapProfile>(Root + "/MapProfiles/CountrysideRoad.asset");
            assets.countrysideRoad.mapId = "countryside";
            assets.countrysideRoad.displayName = "COUNTRYSIDE";
            assets.countrysideRoad.supportedGameMode = GameModeType.Adventure3D;
            assets.countrysideRoad.seed = 18472;
            assets.countrysideRoad.coinRoute = assets.countrysideCoinRoute;
            assets.countrysideRoad.roadWidth = 12f;
            assets.countrysideRoad.curveIntensity = 10f;
            assets.countrysideRoad.elevationIntensity = 8.5f;
            assets.countrysideRoad.introEndDistance = 150f;
            assets.countrysideRoad.easyEndDistance = 500f;
            assets.countrysideRoad.normalEndDistance = 1200f;
            assets.countrysideRoad.hardEndDistance = 2500f;
            assets.countrysideRoad.difficultyCurve = new AnimationCurve(
                new Keyframe(0f, 0.15f), new Keyframe(0.2f, 0.3f),
                new Keyframe(0.5f, 0.65f), new Keyframe(1f, 1f));
            assets.countrysideRoad.initialFuelGap = 5f;
            assets.countrysideRoad.fuelGapIncrease = 5f;
            assets.countrysideRoad.elevationProfile = assets.elevation;
            assets.countrysideRoad.curvatureProfile = assets.curvature;
            assets.countrysideRoad.roadMaterial = assets.road;
            assets.countrysideRoad.shoulderMaterial = assets.grass;
            assets.countrysideRoad.groundPhysicsMaterial = assets.groundPhysics;
            EditorUtility.SetDirty(assets.countrysideRoad);

            assets.testCircuit = LoadOrCreate<CircuitDefinition>(Root + "/MapProfiles/ProceduralTestCircuit.asset");
            assets.testCircuit.mapId = "procedural-test-circuit"; assets.testCircuit.displayName = "PROCEDURAL TEST CIRCUIT";
            assets.testCircuit.supportedGameMode = GameModeType.TimeTrial3D; assets.testCircuit.seed = 12345;
            assets.testCircuit.laps = 1; assets.testCircuit.fuelEnabled = false;
            assets.testCircuit.difficulty = CircuitDifficulty.Normal; assets.testCircuit.respawnPenaltySeconds = 3f;
            assets.testCircuit.shortcutChance = 0.65f; assets.testCircuit.techChance = 0.18f;
            assets.testCircuit.roadMaterial = assets.road; assets.testCircuit.shortcutMaterial = assets.shortcut;
            EditorUtility.SetDirty(assets.testCircuit);

            assets.economy = LoadOrCreate<UpgradeEconomyDefinition>(Root + "/VehicleData/UpgradeEconomy.asset");
            assets.economy.costs = new[] { 100, 200, 350, 550, 800, 1150, 1550, 2050, 2700, 3500, 4500, 5700, 7200, 9000, 11200, 13800, 16800, 20200, 24000, 28200, 32800, 37800, 43200, 49000, 55200 };
            EditorUtility.SetDirty(assets.economy);
            assets.catalog = LoadOrCreate<GameCatalog>(Root + "/VehicleData/GameCatalog.asset");
            assets.catalog.maps = new MapDefinition[]
                { assets.countryside, assets.neonCity, assets.countrysideRoad, assets.testCircuit };
            assets.catalog.vehicles = new[] { assets.jeep, assets.formula, assets.rally, assets.sportBike };
            assets.catalog.drivers = new[] { assets.stickmanBlack };
            assets.catalog.upgradeEconomy = assets.economy;
            EditorUtility.SetDirty(assets.catalog);
            return assets;
        }

        private static void BuildMainMenu(BuiltAssets assets)
        {
            Scene scene = NewScene();
            CreateMenuCamera(new Color(.01f, .015f, .03f));
            Canvas canvas = CreateCanvas(Color.clear, false);
            CreateAspectCoverBackground(canvas.transform, assets.mainMenuBackground);
            CreateImage("Readability Overlay", canvas.transform, new Color(.005f, .015f, .04f, .12f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>().raycastTarget = false;
            Transform safe = CreateSafeArea(canvas.transform);
            MainMenuUI menu = new GameObject("Main Menu Controller").AddComponent<MainMenuUI>();
            GameObject coinCard = CreateHudCard("Total Coins Card", safe, Vector2.one,
                new Vector2(-175f, -56f), new Vector2(300f, 76f));
            CreateSpriteImage("Coin Icon", coinCard.transform, assets.coin01, new Vector2(.5f, .5f),
                new Vector2(-112f, 0f), new Vector2(52f, 52f));
            Text coins = CreateText("Total Coins", coinCard.transform, "COINS  0", 27, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(-15f, 0f), new Vector2(-30f, 0f));
            GameObject gemCard = CreateHudCard("Total Gems Card", safe, Vector2.one,
                new Vector2(-175f, -134f), new Vector2(300f, 64f));
            Text gemIcon = CreateText("Gem Icon", gemCard.transform, "◆", 34, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(.28f, 1f), Vector2.zero, Vector2.zero);
            gemIcon.color = new Color(.18f, .92f, 1f);
            Text gems = CreateText("Total Gems", gemCard.transform, "GEMS  0", 25, TextAnchor.MiddleRight,
                new Vector2(.20f, 0f), Vector2.one, new Vector2(-15f, 0f), new Vector2(-30f, 0f));

            Button skins = CreateSpriteButton("Skins", safe, assets.skinsButton, new Vector2(0f, 1f),
                new Vector2(155f, -62f), new Vector2(260f, 98f));
            Button store = CreateSpriteButton("Store", safe, assets.storeButton, new Vector2(0f, 1f),
                new Vector2(430f, -62f), new Vector2(260f, 98f));
            Button settings = CreateSpriteButton("Settings", safe, assets.settingsButton, new Vector2(0f, 1f),
                new Vector2(705f, -62f), new Vector2(260f, 98f));

            Text vehicleName = CreateText("Selected Vehicle Name", safe, "JEEP", 42, TextAnchor.MiddleCenter,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 178f), new Vector2(520f, 66f));
            vehicleName.fontStyle = FontStyle.Bold;
            RawImage previewImage = UIObject("Selected Vehicle Preview", safe, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), new Vector2(0f, -52f), new Vector2(790f, 390f)).AddComponent<RawImage>();
            previewImage.color = Color.white;
            previewImage.raycastTarget = false;
            Button previous = CreateButton("Previous Vehicle", safe, "<", new Vector2(-470f, -45f),
                new Vector2(88f, 110f));
            Button next = CreateButton("Next Vehicle", safe, ">", new Vector2(470f, -45f),
                new Vector2(88f, 110f));

            Button vehicle = CreateSpriteButton("Vehicle", safe, assets.vehicleButton, new Vector2(0f, 0f),
                new Vector2(245f, 64f), new Vector2(430f, 150f));
            Button tuning = CreateSpriteButton("Tuning", safe, assets.tuningButton, new Vector2(0f, 0f),
                new Vector2(675f, 64f), new Vector2(400f, 150f));
            Button upgrade = CreateSpriteButton("Upgrade", safe, assets.upgradeButton, new Vector2(1f, 0f),
                new Vector2(-665f, 64f), new Vector2(400f, 150f));
            Button race = CreateSpriteButton("Race", safe, assets.raceButton, new Vector2(1f, 0f),
                new Vector2(-225f, 70f), new Vector2(440f, 164f));

            Bind(skins, menu.Skins); Bind(store, menu.Store); Bind(settings, menu.Settings);
            Bind(previous, menu.PreviousVehicle); Bind(next, menu.NextVehicle);
            Bind(vehicle, menu.Vehicle); Bind(tuning, menu.Tuning); Bind(upgrade, menu.Upgrade); Bind(race, menu.Race);
            GaragePreviewRenderer preview = CreateGarageVehiclePreview(assets, previewImage, "Home");
            menu.Assign(assets.catalog, preview, coins, vehicleName, gems);
            VehicleDefinition.CustomizationOption initialSkin = assets.jeep.skins != null && assets.jeep.skins.Length > 0
                ? assets.jeep.skins[0] : null;
            VehicleDefinition.CustomizationOption initialWheel = assets.jeep.wheels != null && assets.jeep.wheels.Length > 0
                ? assets.jeep.wheels[0] : null;
            preview.Apply(assets.jeep, initialSkin, initialWheel, assets.stickmanBlack);
            vehicleName.text = assets.jeep.displayName.ToUpperInvariant();
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, MainMenuPath);
        }

        private static void BuildGarage(BuiltAssets assets)
        {
            Scene scene = NewScene();
            Color background = new Color(.035f, .065f, .11f);
            CreateMenuCamera(background);
            Canvas canvas = CreateCanvas(background);
            Transform safe = CreateSafeArea(canvas.transform);
            GarageUI menu = new GameObject("Garage Controller").AddComponent<GarageUI>();
            CreateText("Heading", safe, "GARAGE / CUSTOMIZE", 46, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, -52f), new Vector2(680f, 72f));
            Text garageSections = CreateText("Section Guide", safe,
                "CUSTOMIZE  •  SKIN / WHEELS / DRIVER        PERFORMANCE  •  TUNING", 20,
                TextAnchor.MiddleRight, new Vector2(.46f, 1f), Vector2.one,
                new Vector2(-35f, -58f), new Vector2(-45f, 48f));
            garageSections.color = new Color(.63f, .82f, 1f, 1f);
            Text vehicleName = CreateText("Vehicle Name", safe, "JEEP", 38, TextAnchor.MiddleCenter,
                new Vector2(.05f, .82f), new Vector2(.48f, .94f), Vector2.zero, Vector2.zero);
            GameObject previewPanel = CreateImage("Live Composed Preview", safe, new Color(.055f, .105f, .17f, 1f),
                new Vector2(.04f, .22f), new Vector2(.49f, .82f), Vector2.zero, Vector2.zero);
            RawImage previewImage = UIObject("Preview Render", previewPanel.transform, new Vector2(.03f, .06f),
                new Vector2(.97f, .94f), Vector2.zero, Vector2.zero).AddComponent<RawImage>();
            previewImage.color = Color.white;
            Button previous = CreateButton("Previous Vehicle", safe, "<", new Vector2(-690f, 347f), new Vector2(76f, 64f));
            Button next = CreateButton("Next Vehicle", safe, ">", new Vector2(-95f, 347f), new Vector2(76f, 64f));
            Text skinSummary = CreateText("Skin Summary", safe, "SKIN\nDEFAULT", 18, TextAnchor.MiddleCenter,
                new Vector2(.04f, .105f), new Vector2(.185f, .205f), Vector2.zero, Vector2.zero);
            Text wheelSummary = CreateText("Wheel Summary", safe, "WHEELS\nWHEEL 01", 18, TextAnchor.MiddleCenter,
                new Vector2(.19f, .105f), new Vector2(.34f, .205f), Vector2.zero, Vector2.zero);
            Text driverSummary = CreateText("Driver Summary", safe, "DRIVER\nSTICKMAN BLACK", 18, TextAnchor.MiddleCenter,
                new Vector2(.345f, .105f), new Vector2(.49f, .205f), Vector2.zero, Vector2.zero);

            var categories = new Button[4];
            categories[0] = CreateButton("Vehicle Skin Category", safe, "VEHICLE SKIN", new Vector2(125f, 365f), new Vector2(205f, 66f));
            categories[1] = CreateButton("Wheels Category", safe, "WHEELS", new Vector2(345f, 365f), new Vector2(205f, 66f));
            categories[2] = CreateButton("Driver Category", safe, "DRIVER", new Vector2(565f, 365f), new Vector2(205f, 66f));
            categories[3] = CreateButton("Tuning Category", safe, "TUNING", new Vector2(785f, 365f), new Vector2(205f, 66f));
            Bind(categories[0], menu.ShowSkins); Bind(categories[1], menu.ShowWheels); Bind(categories[2], menu.ShowDrivers);
            Bind(categories[3], menu.ShowTuning);
            Text categoryName = CreateText("Category Name", safe, "VEHICLE SKIN", 28, TextAnchor.MiddleLeft,
                new Vector2(.53f, .75f), new Vector2(.95f, .82f), Vector2.zero, Vector2.zero);

            var optionButtons = new Button[6];
            var optionImages = new Image[6];
            var optionLabels = new Text[6];
            UnityAction[] selections = { menu.SelectOption0, menu.SelectOption1, menu.SelectOption2,
                menu.SelectOption3, menu.SelectOption4, menu.SelectOption5 };
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int column = i % 3; int row = i / 3;
                float x = 215f + column * 285f; float y = 120f - row * 285f;
                optionButtons[i] = CreateButton("Option " + (i + 1), safe, "", new Vector2(x, y), new Vector2(260f, 255f));
                optionImages[i] = CreateImage("Icon", optionButtons[i].transform, Color.white,
                    new Vector2(.12f, .30f), new Vector2(.88f, .92f), Vector2.zero, Vector2.zero).GetComponent<Image>();
                optionImages[i].raycastTarget = false; optionImages[i].preserveAspect = true;
                optionLabels[i] = CreateText("Status", optionButtons[i].transform, "OPTION", 19, TextAnchor.MiddleCenter,
                    new Vector2(.04f, .02f), new Vector2(.96f, .28f), Vector2.zero, Vector2.zero);
                Bind(optionButtons[i], selections[i]);
            }

            GameObject tuningPanel = UIObject("Garage Tuning Panel", safe, new Vector2(.52f, .12f),
                new Vector2(.97f, .75f), Vector2.zero, Vector2.zero);
            Text tuningCoins = CreateText("Tuning Coins", tuningPanel.transform, "COINS  0", 22, TextAnchor.MiddleRight,
                new Vector2(.62f, .90f), new Vector2(.98f, .99f), Vector2.zero, Vector2.zero);
            var tuningNames = new Text[5]; var tuningLevels = new Text[5]; var tuningValues = new Text[5];
            var tuningCosts = new Text[5]; var tuningButtons = new Button[5];
            UnityAction[] upgrades = { menu.UpgradeTune0, menu.UpgradeTune1, menu.UpgradeTune2, menu.UpgradeTune3, menu.UpgradeTune4 };
            for (int i = 0; i < 5; i++)
            {
                float y = 240f - i * 115f;
                GameObject row = CreateImage("Tune Row " + (i + 1), tuningPanel.transform,
                    new Color(.065f, .125f, .20f, .98f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    new Vector2(0f, y), new Vector2(810f, 98f));
                tuningNames[i] = CreateText("Name", row.transform, "SPEED", 20, TextAnchor.MiddleLeft,
                    new Vector2(.03f, .50f), new Vector2(.25f, .96f), Vector2.zero, Vector2.zero);
                tuningLevels[i] = CreateText("Level", row.transform, "LEVEL 1 / 20", 18, TextAnchor.MiddleLeft,
                    new Vector2(.25f, .50f), new Vector2(.48f, .96f), Vector2.zero, Vector2.zero);
                tuningValues[i] = CreateText("Value", row.transform, "0", 17, TextAnchor.MiddleLeft,
                    new Vector2(.48f, .50f), new Vector2(.69f, .96f), Vector2.zero, Vector2.zero);
                tuningButtons[i] = CreateButton("Upgrade", row.transform, "UPGRADE", new Vector2(285f, 0f), new Vector2(220f, 64f));
                tuningCosts[i] = tuningButtons[i].GetComponentInChildren<Text>(true);
                Bind(tuningButtons[i], upgrades[i]);
            }
            Button back = CreateButton("Back", safe, "BACK", new Vector2(-720f, -455f), new Vector2(230f, 66f));
            Bind(back, menu.Back); Bind(previous, menu.PreviousVehicle); Bind(next, menu.NextVehicle);

            GameObject previewCameraObject = new GameObject("Garage Preview Camera");
            previewCameraObject.transform.position = new Vector3(1000f, 0f, -10f);
            Camera previewCamera = previewCameraObject.AddComponent<Camera>();
            previewCamera.orthographic = true; previewCamera.orthographicSize = 2.35f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(.07f, .13f, .20f, 1f);
            previewCamera.enabled = false;
            Transform previewRoot = new GameObject("Reusable Composed Vehicle Preview").transform;
            previewRoot.position = new Vector3(1000f, 0f, 0f);
            VehicleDefinition initial = assets.jeep;
            Transform body = CreateSpriteVisual("Preview Body", previewRoot, assets.jeepBody,
                new Vector3(initial.bodyVisualOffset.x, initial.bodyVisualOffset.y, 0f), initial.visualWidth, 10);
            Transform rearWheel = CreateWheelVisual("Preview Rear Wheel", previewRoot, assets.jeepWheel01,
                new Vector3(initial.rearWheelVisualPosition.x, initial.rearWheelVisualPosition.y, -.02f),
                initial.RearWheelDiameter, 11);
            Transform frontWheel = CreateWheelVisual("Preview Front Wheel", previewRoot, assets.jeepWheel01,
                new Vector3(initial.frontWheelVisualPosition.x, initial.frontWheelVisualPosition.y, -.02f),
                initial.FrontWheelDiameter, 11);
            ArticulatedDriver2D driver = CreateArticulatedDriver(previewRoot, assets.stickmanBlack, initial, null, false);
            GaragePreviewRenderer preview = new GameObject("Garage Preview Renderer").AddComponent<GaragePreviewRenderer>();
            preview.Assign(previewCamera, previewImage, previewRoot, body.GetComponent<SpriteRenderer>(),
                rearWheel.GetComponent<SpriteRenderer>(), frontWheel.GetComponent<SpriteRenderer>(), driver);
            menu.Assign(assets.catalog, preview, vehicleName, categoryName, skinSummary, wheelSummary, driverSummary,
                categories, optionButtons, optionImages, optionLabels, tuningPanel, tuningNames, tuningLevels,
                tuningValues, tuningCosts, tuningButtons, tuningCoins);
            CreateEventSystem();
            ValidateGarageComposition(assets);
            EditorSceneManager.SaveScene(scene, GaragePath);
        }

        private static void BuildGarageModern(BuiltAssets assets)
        {
            Scene scene = NewScene();
            CreateMenuCamera(new Color(.01f, .015f, .03f));
            Canvas canvas = CreateCanvas(Color.clear, false);
            CreateAspectCoverBackground(canvas.transform, assets.mainMenuBackground);
            CreateImage("Garage Overlay", canvas.transform, new Color(.005f, .015f, .04f, .30f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>().raycastTarget = false;
            Transform safe = CreateSafeArea(canvas.transform);
            GarageUI menu = new GameObject("Garage Controller").AddComponent<GarageUI>();
            Button home = CreateSpriteButton("Home", safe, assets.homeButton, new Vector2(0f, 1f),
                new Vector2(165f, -62f), new Vector2(285f, 110f));
            Text sectionTitle = CreateText("Section Title", safe, "VEHICLE", 42, TextAnchor.MiddleCenter,
                new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -58f), new Vector2(520f, 72f));
            sectionTitle.fontStyle = FontStyle.Bold;
            GameObject coinCard = CreateHudCard("Coins Card", safe, Vector2.one,
                new Vector2(-175f, -56f), new Vector2(300f, 76f));
            CreateSpriteImage("Coin Icon", coinCard.transform, assets.coin01, new Vector2(.5f, .5f),
                new Vector2(-112f, 0f), new Vector2(52f, 52f));
            Text tuningCoins = CreateText("Tuning Coins", coinCard.transform, "COINS  0", 27,
                TextAnchor.MiddleRight, Vector2.zero, Vector2.one, new Vector2(-15f, 0f), new Vector2(-30f, 0f));
            GameObject gemsCard = CreateHudCard("Gems Card", safe, Vector2.one,
                new Vector2(-175f, -132f), new Vector2(300f, 62f));
            Text gemMark = CreateText("Gem Mark", gemsCard.transform, "◆", 32, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(.26f, 1f), Vector2.zero, Vector2.zero);
            gemMark.color = new Color(.18f, .92f, 1f);
            Text gemsBalance = CreateText("Gems Balance", gemsCard.transform, "GEMS  0", 24,
                TextAnchor.MiddleRight, new Vector2(.18f, 0f), Vector2.one,
                new Vector2(-15f, 0f), new Vector2(-30f, 0f));
            Text vehicleName = CreateText("Vehicle Name", safe, "JEEP", 38, TextAnchor.MiddleCenter,
                new Vector2(.04f, .80f), new Vector2(.48f, .91f), Vector2.zero, Vector2.zero);
            RawImage previewImage = UIObject("Live Composed Preview", safe, new Vector2(.04f, .25f),
                new Vector2(.48f, .80f), Vector2.zero, Vector2.zero).AddComponent<RawImage>();
            previewImage.color = Color.white; previewImage.raycastTarget = false;
            Button previous = CreateButton("Previous Vehicle", safe, "<", new Vector2(-820f, -30f), new Vector2(76f, 100f));
            Button next = CreateButton("Next Vehicle", safe, ">", new Vector2(-80f, -30f), new Vector2(76f, 100f));
            Text skinSummary = CreateText("Skin Summary", safe, "SKIN\nDEFAULT", 17, TextAnchor.MiddleCenter,
                new Vector2(.04f, .12f), new Vector2(.18f, .23f), Vector2.zero, Vector2.zero);
            Text wheelSummary = CreateText("Wheel Summary", safe, "WHEELS\nWHEEL 01", 17, TextAnchor.MiddleCenter,
                new Vector2(.19f, .12f), new Vector2(.33f, .23f), Vector2.zero, Vector2.zero);
            Text driverSummary = CreateText("Driver Summary", safe, "DRIVER\nSTICKMAN BLACK", 17, TextAnchor.MiddleCenter,
                new Vector2(.34f, .12f), new Vector2(.48f, .23f), Vector2.zero, Vector2.zero);

            GameObject vehiclePanel = CreateImage("Vehicle Panel", safe, new Color(.025f, .06f, .12f, .88f),
                new Vector2(.52f, .16f), new Vector2(.97f, .84f), Vector2.zero, Vector2.zero);
            CreateText("Title", vehiclePanel.transform, "VEHICLE SELECTION", 34, TextAnchor.MiddleCenter,
                new Vector2(.08f, .78f), new Vector2(.92f, .94f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            CreateText("Description", vehiclePanel.transform,
                "USE THE ARROWS TO SELECT A VEHICLE\n\nALL AVAILABLE VEHICLES USE THEIR REAL SAVED STATS, SKIN AND WHEELS.",
                24, TextAnchor.MiddleCenter, new Vector2(.10f, .25f), new Vector2(.90f, .72f), Vector2.zero, Vector2.zero);

            GameObject customizationPanel = UIObject("Skins Panel", safe, new Vector2(.51f, .13f),
                new Vector2(.98f, .86f), Vector2.zero, Vector2.zero);
            var categories = new Button[3];
            categories[0] = CreateButton("Vehicle Skin Category", customizationPanel.transform, "VEHICLE SKIN", new Vector2(-250f, 280f), new Vector2(220f, 64f));
            categories[1] = CreateButton("Wheels Category", customizationPanel.transform, "WHEELS", new Vector2(0f, 280f), new Vector2(220f, 64f));
            categories[2] = CreateButton("Driver Category", customizationPanel.transform, "CHARACTER", new Vector2(250f, 280f), new Vector2(220f, 64f));
            Bind(categories[0], menu.ShowSkins); Bind(categories[1], menu.ShowWheels); Bind(categories[2], menu.ShowDrivers);
            Text categoryName = CreateText("Category Name", customizationPanel.transform, "VEHICLE SKIN", 28,
                TextAnchor.MiddleCenter, new Vector2(.15f, .69f), new Vector2(.85f, .80f), Vector2.zero, Vector2.zero);
            var optionButtons = new Button[6]; var optionImages = new Image[6]; var optionLabels = new Text[6];
            UnityAction[] selections = { menu.SelectOption0, menu.SelectOption1, menu.SelectOption2,
                menu.SelectOption3, menu.SelectOption4, menu.SelectOption5 };
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int column = i % 3; int rowIndex = i / 3;
                float x = -260f + column * 260f; float y = 65f - rowIndex * 235f;
                optionButtons[i] = CreateButton("Option " + (i + 1), customizationPanel.transform, "", new Vector2(x, y), new Vector2(235f, 210f));
                optionImages[i] = CreateImage("Icon", optionButtons[i].transform, Color.white,
                    new Vector2(.12f, .30f), new Vector2(.88f, .92f), Vector2.zero, Vector2.zero).GetComponent<Image>();
                optionImages[i].raycastTarget = false; optionImages[i].preserveAspect = true;
                optionLabels[i] = CreateText("Status", optionButtons[i].transform, "OPTION", 19, TextAnchor.MiddleCenter,
                    new Vector2(.04f, .02f), new Vector2(.96f, .28f), Vector2.zero, Vector2.zero);
                Bind(optionButtons[i], selections[i]);
            }

            GameObject tuningPanel = UIObject("Garage Tuning Panel", safe, new Vector2(.50f, .10f),
                new Vector2(.98f, .86f), Vector2.zero, Vector2.zero);
            var tuningNames = new Text[5]; var tuningLevels = new Text[5]; var tuningValues = new Text[5];
            var tuningCosts = new Text[5]; var tuningButtons = new Button[5];
            UnityAction[] upgrades = { menu.UpgradeTune0, menu.UpgradeTune1, menu.UpgradeTune2, menu.UpgradeTune3, menu.UpgradeTune4 };
            for (int i = 0; i < 5; i++)
            {
                float y = 270f - i * 135f;
                GameObject row = CreateImage("Tune Row " + (i + 1), tuningPanel.transform,
                    new Color(.065f, .125f, .20f, .98f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    new Vector2(0f, y), new Vector2(870f, 118f));
                tuningNames[i] = CreateText("Name", row.transform, "SPEED", 20, TextAnchor.MiddleLeft,
                    new Vector2(.02f, .52f), new Vector2(.25f, .96f), Vector2.zero, Vector2.zero);
                tuningLevels[i] = CreateText("Level", row.transform, "LEVEL 1 / 20", 18, TextAnchor.MiddleLeft,
                    new Vector2(.02f, .06f), new Vector2(.25f, .52f), Vector2.zero, Vector2.zero);
                tuningValues[i] = CreateText("Value", row.transform, "CURRENT\nNEXT", 16, TextAnchor.MiddleLeft,
                    new Vector2(.27f, .05f), new Vector2(.72f, .95f), Vector2.zero, Vector2.zero);
                tuningButtons[i] = CreateButton("Upgrade", row.transform, "PRICE", new Vector2(330f, 0f), new Vector2(190f, 70f));
                tuningCosts[i] = tuningButtons[i].GetComponentInChildren<Text>(true);
                Bind(tuningButtons[i], upgrades[i]);
            }

            GameObject upgradePanel = CreateImage("Upgrade Panel", safe, new Color(.025f, .06f, .12f, .90f),
                new Vector2(.52f, .16f), new Vector2(.97f, .84f), Vector2.zero, Vector2.zero);
            Text upgradeSummary = CreateText("Upgrade Summary", upgradePanel.transform, "CURRENT PERFORMANCE", 27,
                TextAnchor.UpperLeft, new Vector2(.10f, .12f), new Vector2(.90f, .88f), Vector2.zero, Vector2.zero);
            GameObject storePanel = CreateImage("Store Panel", safe, new Color(.025f, .06f, .12f, .90f),
                new Vector2(.52f, .16f), new Vector2(.97f, .84f), Vector2.zero, Vector2.zero);
            CreateText("Store Placeholder", storePanel.transform, "STORE\n\nCOMING SOON\nNO IAP IS CONFIGURED", 34,
                TextAnchor.MiddleCenter, new Vector2(.08f, .12f), new Vector2(.92f, .88f), Vector2.zero, Vector2.zero);
            GameObject settingsPanel = CreateImage("Settings Panel", safe, new Color(.025f, .06f, .12f, .90f),
                new Vector2(.52f, .16f), new Vector2(.97f, .84f), Vector2.zero, Vector2.zero);
            CreateText("Settings Placeholder", settingsPanel.transform, "SETTINGS", 42, TextAnchor.MiddleCenter,
                new Vector2(.08f, .12f), new Vector2(.92f, .88f), Vector2.zero, Vector2.zero);

            GameObject modal = CreateImage("Purchase Confirmation", safe, new Color(.01f, .025f, .055f, .985f),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(760f, 470f));
            modal.AddComponent<Outline>().effectColor = new Color(.20f, .74f, 1f, 1f);
            Text modalTitle = CreateText("Title", modal.transform, "UPGRADE SPEED?", 36, TextAnchor.MiddleCenter,
                new Vector2(.08f, .76f), new Vector2(.92f, .94f), Vector2.zero, Vector2.zero);
            Text modalValues = CreateText("Values", modal.transform, "CURRENT\nNEXT", 25, TextAnchor.MiddleCenter,
                new Vector2(.08f, .39f), new Vector2(.92f, .75f), Vector2.zero, Vector2.zero);
            Text modalCost = CreateText("Cost", modal.transform, "COST", 26, TextAnchor.MiddleCenter,
                new Vector2(.08f, .25f), new Vector2(.92f, .40f), Vector2.zero, Vector2.zero);
            Text modalStatus = CreateText("Status", modal.transform, "", 22, TextAnchor.MiddleCenter,
                new Vector2(.08f, .15f), new Vector2(.92f, .26f), Vector2.zero, Vector2.zero);
            modalStatus.color = new Color(1f, .30f, .28f);
            Button yes = CreateButton("Yes", modal.transform, "YES", new Vector2(-170f, -175f), new Vector2(260f, 72f));
            Button no = CreateButton("No", modal.transform, "NO", new Vector2(170f, -175f), new Vector2(260f, 72f));
            yes.image.color = new Color(.16f, .72f, .26f, 1f); no.image.color = new Color(.82f, .16f, .14f, 1f);
            Bind(yes, menu.ConfirmPurchase); Bind(no, menu.CancelPurchase); modal.SetActive(false);

            Bind(home, menu.Back); Bind(previous, menu.PreviousVehicle); Bind(next, menu.NextVehicle);
            GaragePreviewRenderer preview = CreateGarageVehiclePreview(assets, previewImage, "Garage");
            menu.Assign(assets.catalog, preview, vehicleName, categoryName, skinSummary, wheelSummary, driverSummary,
                categories, optionButtons, optionImages, optionLabels, tuningPanel, tuningNames, tuningLevels,
                tuningValues, tuningCosts, tuningButtons, tuningCoins, sectionTitle, upgradeSummary, vehiclePanel,
                customizationPanel, upgradePanel, storePanel, settingsPanel, modal, modalTitle, modalValues,
                modalCost, modalStatus, yes, gemsBalance);
            CreateEventSystem();
            ValidateGarageComposition(assets);
            EditorSceneManager.SaveScene(scene, GaragePath);
        }

        private static void BuildPreStartModern(BuiltAssets assets)
        {
            Scene scene = NewScene();
            CreateMenuCamera(new Color(.01f, .015f, .03f));
            Canvas canvas = CreateCanvas(Color.clear, false);
            CreateAspectCoverBackground(canvas.transform, assets.mainMenuBackground);
            CreateImage("Race Overlay", canvas.transform, new Color(.005f, .015f, .04f, .36f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>().raycastTarget = false;
            Transform safe = CreateSafeArea(canvas.transform);
            PreStartMenuUI menu = new GameObject("Pre-Start Controller").AddComponent<PreStartMenuUI>();
            Button home = CreateSpriteButton("Home", safe, assets.homeButton, new Vector2(0f, 1f),
                new Vector2(165f, -62f), new Vector2(285f, 110f));
            Text coins = CreateText("Coins", safe, "COINS  0", 27, TextAnchor.MiddleRight,
                Vector2.one, Vector2.one, new Vector2(-190f, -55f), new Vector2(340f, 70f));
            Text progress = CreateText("Progress", safe, "1 MODE   >   2 VEHICLE", 25,
                TextAnchor.MiddleCenter, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
                new Vector2(0f, -62f), new Vector2(800f, 52f));

            var panels = new GameObject[2];
            for (int i = 0; i < panels.Length; i++)
                panels[i] = UIObject($"Step {i + 1}", safe, Vector2.zero, Vector2.one,
                    new Vector2(0f, -20f), new Vector2(-80f, -180f));

            CreateText("Title", panels[0].transform, "SELECT RACE MODE", 45, TextAnchor.MiddleCenter,
                new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -52f), new Vector2(700f, 72f));
            GameObject gridRoot = UIObject("Mode Card Grid", panels[0].transform, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), new Vector2(0f, -15f), new Vector2(1510f, 535f));
            GridLayoutGroup grid = gridRoot.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(455f, 510f); grid.spacing = new Vector2(42f, 0f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.MiddleCenter;
            var cards = new Image[3]; var cardRects = new RectTransform[3]; var statuses = new Text[3];
            CreateModeCard(gridRoot.transform, "Adventure", "SIDE-VIEW CLIMB",
                "COUNTRYSIDE / NEON CITY\nDISTANCE RUN\n2.5D CAMERA", menu.SelectAdventure2_5D,
                out cards[0], out cardRects[0], out statuses[0]);
            CreateModeCard(gridRoot.transform, "3D", "OPEN ROAD",
                "COUNTRYSIDE\nDISTANCE RUN\nCHASE CAMERA", menu.SelectAdventure3D,
                out cards[1], out cardRects[1], out statuses[1]);
            CreateModeCard(gridRoot.transform, "Time Trial", "PROCEDURAL CIRCUIT",
                "ONE LAP\nBEST TIME\nCHASE CAMERA", menu.SelectTimeTrial3D,
                out cards[2], out cardRects[2], out statuses[2]);
            Text mode = CreateText("Selected Mode", panels[0].transform, "ADVENTURE", 22,
                TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(-555f, -300f), new Vector2(250f, 90f));
            GameObject trackSelector = UIObject("Adventure Track Selector", panels[0].transform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Button previousTrack = CreateButton("Previous Adventure Track", trackSelector.transform, "<",
                new Vector2(-315f, -300f), new Vector2(76f, 68f));
            Text map = CreateText("Selected Adventure Track", trackSelector.transform,
                "TRACK\nCOUNTRYSIDE", 23, TextAnchor.MiddleCenter,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -300f),
                new Vector2(470f, 100f));
            Button nextTrack = CreateButton("Next Adventure Track", trackSelector.transform, ">",
                new Vector2(315f, -300f), new Vector2(76f, 68f));
            Button continueMode = CreateButton("Continue", panels[0].transform, "CONTINUE",
                new Vector2(0f, -395f), new Vector2(310f, 72f));
            Bind(previousTrack, menu.PreviousMap); Bind(nextTrack, menu.NextMap);
            Bind(home, menu.BackStep); Bind(continueMode, menu.ContinueStep);

            CreateText("Title", panels[1].transform, "SELECT VEHICLE", 45, TextAnchor.MiddleCenter,
                new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -52f), new Vector2(700f, 72f));
            GameObject vehicleCard = CreateImage("Vehicle Card", panels[1].transform,
                new Color(.025f, .06f, .12f, .90f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(0f, -20f), new Vector2(1310f, 600f));
            RawImage previewImage = UIObject("Selected Vehicle Preview", vehicleCard.transform,
                new Vector2(.04f, .10f), new Vector2(.57f, .88f), Vector2.zero, Vector2.zero).AddComponent<RawImage>();
            previewImage.color = Color.white; previewImage.raycastTarget = false;
            Text vehicle = CreateText("Vehicle", vehicleCard.transform, "JEEP", 38, TextAnchor.UpperLeft,
                new Vector2(.61f, .78f), new Vector2(.95f, .93f), Vector2.zero, Vector2.zero);
            Text stats = CreateText("Vehicle Stats", vehicleCard.transform, "", 23, TextAnchor.UpperLeft,
                new Vector2(.61f, .16f), new Vector2(.95f, .77f), Vector2.zero, Vector2.zero);
            CreateText("Transmission", vehicleCard.transform, "AUTOMATIC TRANSMISSION", 21,
                TextAnchor.MiddleCenter, new Vector2(.61f, .05f), new Vector2(.95f, .15f), Vector2.zero, Vector2.zero)
                .color = new Color(.38f, .88f, 1f);
            Button previous = CreateButton("Previous Vehicle", panels[1].transform, "<",
                new Vector2(-730f, -20f), new Vector2(90f, 110f));
            Button next = CreateButton("Next Vehicle", panels[1].transform, ">",
                new Vector2(730f, -20f), new Vector2(90f, 110f));
            Button back = CreateButton("Back", panels[1].transform, "BACK",
                new Vector2(-520f, -365f), new Vector2(250f, 68f));
            Button start = CreateButton("Start Race", panels[1].transform, "START RACE",
                new Vector2(520f, -365f), new Vector2(340f, 76f));
            Bind(previous, menu.PreviousVehicle); Bind(next, menu.NextVehicle);
            Bind(back, menu.BackStep); Bind(start, menu.ContinueStep);
            GaragePreviewRenderer preview = CreateGarageVehiclePreview(assets, previewImage, "PreStart");
            menu.Assign(assets.catalog, coins, progress, panels, cards, cardRects, statuses, mode, map, vehicle,
                null, null, stats, null, null, null, null, null, null, null, null, null, preview, trackSelector);
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, PreStartPath);
        }

        private static void BuildPreStart(BuiltAssets assets)
        {
            Scene scene = NewScene();
            Color background = new Color(0.10f, 0.17f, 0.29f);
            CreateMenuCamera(background);
            Canvas canvas = CreateCanvas(background);
            Transform safe = CreateSafeArea(canvas.transform);
            PreStartMenuUI menu = new GameObject("Pre-Start Controller").AddComponent<PreStartMenuUI>();
            CreateText("Heading", safe, "CLIMB ANIME  /  PRE-START", 40, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(390f, -46f), new Vector2(700f, 60f));
            Text coins = CreateText("Coins", safe, "COINS  0", 27, TextAnchor.MiddleRight,
                Vector2.one, Vector2.one, new Vector2(-190f, -46f), new Vector2(340f, 60f));
            Text progress = CreateText("Progress", safe, "1 MODE   >   2 MAP   >   3 VEHICLE   >   4 SETUP", 23,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(1000f, 44f));

            var panels = new GameObject[4];
            for (int i = 0; i < panels.Length; i++)
                panels[i] = UIObject($"Step {i + 1}", safe, Vector2.zero, Vector2.one, new Vector2(0f, -30f), new Vector2(-90f, -190f));

            CreateText("Title", panels[0].transform, "SELECT MODE", 46, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(600f, 70f));
            GameObject cardGrid = UIObject("Mode Card Grid", panels[0].transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f), new Vector2(1510f, 550f));
            GridLayoutGroup grid = cardGrid.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(470f, 530f); grid.spacing = new Vector2(45f, 0f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.MiddleCenter;
            var cards = new Image[2]; var cardRects = new RectTransform[2]; var statuses = new Text[2];
            CreateModeCard(cardGrid.transform, "Adventure", "SIDE-VIEW CLIMB", "GOAL  DISTANCE\nCAMERA  SIDE VIEW\nInfinite adventure.",
                menu.SelectAdventure2_5D, out cards[0], out cardRects[0], out statuses[0]);
            CreateModeCard(cardGrid.transform, "3D", "OPEN ROAD", "GOAL  DISTANCE\nCAMERA  CHASE 3D\nDrive as far as possible.",
                menu.SelectAdventure3D, out cards[1], out cardRects[1], out statuses[1]);
            Text mode = CreateText("Selected Mode", panels[0].transform, "ADVENTURE", 22, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 83f), new Vector2(520f, 38f));
            Button modeBack = CreateButton("Back", panels[0].transform, "BACK", new Vector2(-660f, -350f), new Vector2(220f, 64f));
            Button modeNext = CreateButton("Continue", panels[0].transform, "CONTINUE", new Vector2(660f, -350f), new Vector2(260f, 64f));
            Bind(modeBack, menu.BackStep); Bind(modeNext, menu.ContinueStep);

            CreateText("Title", panels[1].transform, "SELECT MAP", 46, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(600f, 70f));
            GameObject mapCard = CreateImage("Map Card", panels[1].transform, new Color(0.055f, 0.11f, 0.19f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(980f, 530f));
            Image mapSky = CreateImage("Countryside Sky Preview", mapCard.transform, Color.white,
                new Vector2(.03f, .35f), new Vector2(.97f, .96f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            mapSky.sprite = assets.countrysideEnvironment.sky; mapSky.preserveAspect = true; mapSky.raycastTarget = false;
            Image mapMountains = CreateImage("Countryside Mountains Preview", mapCard.transform, Color.white,
                new Vector2(.03f, .35f), new Vector2(.97f, .70f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            mapMountains.sprite = assets.countrysideEnvironment.mountains; mapMountains.preserveAspect = true;
            mapMountains.raycastTarget = false;
            Text map = CreateText("Map Details", mapCard.transform, "COUNTRYSIDE", 27, TextAnchor.UpperCenter,
                Vector2.zero, new Vector2(1f, 0.35f), new Vector2(0f, 15f), new Vector2(-60f, -20f));
            Button mapPrev = CreateButton("Previous Map", panels[1].transform, "<", new Vector2(-590f, -20f), new Vector2(90f, 90f));
            Button mapNext = CreateButton("Next Map", panels[1].transform, ">", new Vector2(590f, -20f), new Vector2(90f, 90f));
            Button mapBack = CreateButton("Back", panels[1].transform, "BACK", new Vector2(-660f, -350f), new Vector2(220f, 64f));
            Button mapContinue = CreateButton("Continue", panels[1].transform, "CONTINUE", new Vector2(660f, -350f), new Vector2(260f, 64f));
            Bind(mapPrev, menu.PreviousMap); Bind(mapNext, menu.NextMap); Bind(mapBack, menu.BackStep); Bind(mapContinue, menu.ContinueStep);

            CreateText("Title", panels[2].transform, "SELECT VEHICLE", 46, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(600f, 70f));
            GameObject vehicleCard = CreateImage("Vehicle Card", panels[2].transform, new Color(0.055f, 0.11f, 0.19f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1120f, 540f));
            GameObject previewRoot = CreateImage("Vehicle Preview", vehicleCard.transform, Color.white,
                new Vector2(0.04f, 0.12f), new Vector2(0.53f, 0.88f), Vector2.zero, Vector2.zero);
            Image vehiclePreview = previewRoot.GetComponent<Image>(); vehiclePreview.preserveAspect = true;
            GameObject jeepPreview = UIObject("Jeep Preview Fallback", previewRoot.transform, Vector2.zero,
                Vector2.one, Vector2.zero, Vector2.zero);
            CreateImage("Jeep Body", jeepPreview.transform, new Color(.82f, .16f, .22f),
                new Vector2(.16f, .31f), new Vector2(.84f, .58f), Vector2.zero, Vector2.zero);
            CreateImage("Jeep Cabin", jeepPreview.transform, new Color(.12f, .34f, .68f),
                new Vector2(.30f, .58f), new Vector2(.62f, .78f), Vector2.zero, Vector2.zero);
            CreateImage("Rear Wheel", jeepPreview.transform, Color.black,
                new Vector2(.24f, .20f), new Vector2(.37f, .38f), Vector2.zero, Vector2.zero);
            CreateImage("Front Wheel", jeepPreview.transform, Color.black,
                new Vector2(.65f, .20f), new Vector2(.78f, .38f), Vector2.zero, Vector2.zero);
            CreateText("Jeep Preview Label", jeepPreview.transform, "JEEP  •  ORIGINAL", 25,
                TextAnchor.MiddleCenter, new Vector2(0f, .03f), new Vector2(1f, .18f), Vector2.zero, Vector2.zero).color =
                new Color(.12f, .25f, .40f);
            Text vehicle = CreateText("Vehicle", vehicleCard.transform, "CAR 01", 32, TextAnchor.UpperLeft,
                new Vector2(0.57f, 0.78f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);
            Text stats = CreateText("Vehicle Stats", vehicleCard.transform, "", 22, TextAnchor.UpperLeft,
                new Vector2(0.57f, 0.08f), new Vector2(0.95f, 0.78f), Vector2.zero, Vector2.zero);
            Text selectedVehicle = CreateText("Selected Vehicle", vehicleCard.transform, "✓  SELECTED VEHICLE", 21,
                TextAnchor.MiddleCenter, new Vector2(.60f, .01f), new Vector2(.93f, .10f), Vector2.zero, Vector2.zero);
            selectedVehicle.color = new Color(.38f, .88f, 1f);
            Button vehiclePrev = CreateButton("Previous Vehicle", panels[2].transform, "<", new Vector2(-660f, -20f), new Vector2(90f, 90f));
            Button vehicleNext = CreateButton("Next Vehicle", panels[2].transform, ">", new Vector2(660f, -20f), new Vector2(90f, 90f));
            Button vehicleBack = CreateButton("Back", panels[2].transform, "BACK", new Vector2(-660f, -350f), new Vector2(220f, 64f));
            Button vehicleContinue = CreateButton("Continue", panels[2].transform, "SELECT", new Vector2(660f, -350f), new Vector2(300f, 64f));
            Bind(vehiclePrev, menu.PreviousVehicle); Bind(vehicleNext, menu.NextVehicle); Bind(vehicleBack, menu.BackStep); Bind(vehicleContinue, menu.ContinueStep);

            CreateText("Title", panels[3].transform, "SETUP", 46, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(600f, 70f));
            GameObject setupCard = CreateImage("Setup Card", panels[3].transform, new Color(0.055f, 0.11f, 0.19f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1160f, 520f));
            Text summary = CreateText("Tuning Summary", setupCard.transform, "", 25, TextAnchor.UpperLeft,
                new Vector2(0.05f, 0.08f), new Vector2(0.52f, 0.92f), Vector2.zero, Vector2.zero);
            CreateText("Transmission Label", setupCard.transform, "TRANSMISSION", 23, TextAnchor.MiddleCenter,
                new Vector2(0.57f, 0.67f), new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);
            Text transmission = CreateText("Transmission", setupCard.transform, "AUTO", 42, TextAnchor.MiddleCenter,
                new Vector2(0.57f, 0.51f), new Vector2(0.95f, 0.67f), Vector2.zero, Vector2.zero);
            Button transmissionButton = CreateButton("Toggle Transmission", setupCard.transform, "TOGGLE AUTO / MANUAL",
                new Vector2(300f, -55f), new Vector2(390f, 66f));
            Button setupBack = CreateButton("Back", panels[3].transform, "BACK", new Vector2(-660f, -350f), new Vector2(220f, 64f));
            Button start = CreateButton("Start", panels[3].transform, "START", new Vector2(660f, -350f), new Vector2(300f, 72f));
            Bind(transmissionButton, menu.ToggleTransmission);
            Bind(setupBack, menu.BackStep); Bind(start, menu.ContinueStep);

            menu.Assign(assets.catalog, coins, progress, panels, cards, cardRects, statuses, mode, map, vehicle,
                vehiclePreview, jeepPreview, stats, summary, transmission, null, null, null, null,
                null, null, null);
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, PreStartPath);
        }

        private static void CreateModeCard(Transform parent, string title, string subtitle, string description,
            UnityAction action, out Image image, out RectTransform rect, out Text status)
        {
            GameObject card = CreateImage(title + " Card", parent, new Color(0.055f, 0.10f, 0.18f, 0.98f),
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            image = card.GetComponent<Image>(); rect = card.GetComponent<RectTransform>();
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.30f, 0.42f, 0.75f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            CreateText("Title", card.transform, title.ToUpperInvariant(), 31, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.94f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            CreateText("Preview", card.transform, subtitle, 25, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.75f), Vector2.zero, Vector2.zero).color = new Color(0.55f, 0.82f, 0.96f);
            CreateText("Description", card.transform, description, 21, TextAnchor.UpperCenter,
                new Vector2(0.08f, 0.19f), new Vector2(0.92f, 0.42f), Vector2.zero, Vector2.zero);
            Button select = CreateButton("Select", card.transform, "SELECT", new Vector2(0f, -218f), new Vector2(290f, 62f));
            status = select.GetComponentInChildren<Text>(); Bind(select, action);
        }

        private static void BuildTuningPanel(Transform safe, PreStartMenuUI menu, out GameObject panel,
            out Text[] nameTexts, out Text[] levels, out Text[] values, out Text[] costs, out Button[] buttons, out Text coins)
        {
            panel = CreateImage("Tuning Panel", safe, new Color(0.035f, 0.06f, 0.11f, 0.98f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1450f, 900f));
            CreateText("Title", panel.transform, "TUNE CAR 01", 42, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(320f, -52f), new Vector2(520f, 70f));
            coins = CreateText("Coins", panel.transform, "COINS 0", 28, TextAnchor.UpperRight,
                Vector2.one, Vector2.one, new Vector2(-190f, -38f), new Vector2(330f, 60f));
            string[] labels = { "SPEED", "ACCELERATION", "SUSPENSION", "GRIP", "FUEL" };
            UnityAction[] actions = { menu.UpgradeEngine, menu.UpgradeAcceleration, menu.UpgradeSuspension,
                menu.UpgradeFourth, menu.UpgradeFuel };
            actions[2] = menu.UpgradeThird;
            nameTexts = new Text[5];
            levels = new Text[5];
            values = new Text[5];
            costs = new Text[5];
            buttons = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                float y = 300f - i * 142f;
                GameObject row = CreateImage(labels[i] + " Row", panel.transform, new Color(0.065f, 0.12f, 0.20f, 0.96f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(1330f, 122f));
                nameTexts[i] = CreateText(labels[i] + " Name", row.transform, labels[i], 27, TextAnchor.MiddleLeft,
                    new Vector2(0.025f, 0f), new Vector2(0.23f, 1f), Vector2.zero, Vector2.zero);
                nameTexts[i].fontStyle = FontStyle.Bold;
                levels[i] = CreateText(labels[i] + " Tune", row.transform, "TUNE 0 / 0", 22, TextAnchor.MiddleLeft,
                    new Vector2(0.23f, 0f), new Vector2(0.40f, 1f), Vector2.zero, Vector2.zero);
                values[i] = CreateText(labels[i] + " Values", row.transform, "CURRENT -> NEXT", 20, TextAnchor.MiddleLeft,
                    new Vector2(0.40f, 0f), new Vector2(0.76f, 1f), Vector2.zero, Vector2.zero);
                buttons[i] = CreateButton(labels[i] + " Upgrade", row.transform, "TUNE", new Vector2(530f, 0f), new Vector2(285f, 78f));
                costs[i] = buttons[i].GetComponentInChildren<Text>();
                Bind(buttons[i], actions[i]);
            }
            Button close = CreateButton("Close", panel.transform, "BACK TO SETUP", new Vector2(0f, -405f), new Vector2(360f, 66f));
            Bind(close, menu.CloseTuning);
        }

        private static void BuildAdventure2_5D(BuiltAssets assets)
        {
            Scene scene = NewScene();
            GameObject systems = new GameObject("Game Systems");
            systems.AddComponent<GameplayBootstrap>().Configure(GameModeType.Adventure2_5D,
                assets.countryside.mapId, true);
            VehicleInputController input = systems.AddComponent<VehicleInputController>();
            input.SetInputMode(GameModeType.Adventure2_5D);
            GameManager manager = systems.AddComponent<GameManager>();
            PickupPool pool = CreatePickupPool(assets);
            TerrainManager terrain = new GameObject("Terrain Manager").AddComponent<TerrainManager>();
            terrain.AssignRuntimeReferences(assets.countryside, null, pool);
            VehicleCamera camera = CreateClimbCamera(null);
            CreateCountrysideBackground(camera.GetComponent<Camera>(), assets.countrysideEnvironment);
            CreateSunsetLighting(true);
            Canvas canvas = CreateCanvas(Color.clear, false);
            Transform safe = CreateSafeArea(canvas.transform);
            GameplayHUD hud = CreateGameplayHud(safe, input, assets);
            ResultsUI results = CreateResults(safe, manager, out RawImage resultImage);
            CanvasGroup controls = CreateClimbControls(safe, input);
            ResultCaptureService capture = CreateResultCapture(null, resultImage, true);
            systems.AddComponent<SelectedVehicleFactory>().ConfigureAdventure2D(assets.catalog, input, manager, hud,
                results, capture, controls, null, terrain, assets.countryside, pool, camera);
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, Adventure2DPath);
        }

        private static void BuildAdventure3D(BuiltAssets assets)
        {
            Scene scene = NewScene();
            GameObject systems = new GameObject("Game Systems");
            systems.AddComponent<GameplayBootstrap>().Configure(GameModeType.Adventure3D, assets.countrysideRoad.mapId);
            VehicleInputController input = systems.AddComponent<VehicleInputController>();
            input.SetInputMode(GameModeType.Adventure3D);
            GameManager manager = systems.AddComponent<GameManager>();
            PickupPool pool = CreatePickupPool(assets);
            RoadTrackGenerator road = new GameObject("Road Track Generator").AddComponent<RoadTrackGenerator>();
            road.Assign(assets.countrysideRoad, null, pool);
            RoadChaseCamera camera = CreateRoadCamera(null);
            CreateSunsetLighting(false);
            Canvas canvas = CreateCanvas(Color.clear, false);
            Transform safe = CreateSafeArea(canvas.transform);
            GameplayHUD hud = CreateGameplayHud(safe, input, assets);
            ResultsUI results = CreateResults(safe, manager, out RawImage resultImage);
            CanvasGroup controls = CreateRoadControls(safe, input);
            ResultCaptureService capture = CreateResultCapture(null, resultImage);
            systems.AddComponent<SelectedVehicleFactory>().ConfigureAdventure3D(assets.catalog, input, manager, hud,
                results, capture, controls, null, road, assets.countrysideRoad, pool, camera);
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, Adventure3DPath);
        }

        private static void BuildTimeTrial3D(BuiltAssets assets)
        {
            Scene scene = NewScene();
            GameObject systems = new GameObject("Time Trial Systems");
            systems.AddComponent<GameplayBootstrap>().Configure(GameModeType.TimeTrial3D, assets.testCircuit.mapId);
            VehicleInputController input = systems.AddComponent<VehicleInputController>();
            input.SetInputMode(GameModeType.TimeTrial3D);
            TimeTrialManager manager = systems.AddComponent<TimeTrialManager>();
            float radius = assets.testCircuit.targetLength / (Mathf.PI * 2f);
            TimeTrialCircuitGenerator circuit = new GameObject("Procedural Test Circuit").AddComponent<TimeTrialCircuitGenerator>();
            circuit.Assign(assets.testCircuit, manager);
            RoadChaseCamera camera = CreateRoadCamera(null);
            CreateSunsetLighting(false);
            Canvas canvas = CreateCanvas(Color.clear, false);
            Transform safe = CreateSafeArea(canvas.transform);
            TimeTrialHUD hud = CreateTimeTrialHud(safe, input);
            ResultsUI results = CreateResults(safe, null, out RawImage resultImage);
            results.AssignTimeTrial(results.gameObject, results.GetComponentInChildren<Text>(true), manager);
            CanvasGroup controls = CreateRoadControls(safe, input);
            Button respawn = CreateButton("Respawn", controls.transform, "RESPAWN +3s", new Vector2(0f, 310f), new Vector2(270f, 70f));
            Bind(respawn, manager.Respawn);
            Button horizontalJump = CreateButton("Horizontal Jump", controls.transform, "HORIZONTAL JUMP  +2m",
                new Vector2(0f, 225f), new Vector2(330f, 70f));
            horizontalJump.image.color = new Color(.48f, .16f, .68f, .98f);
            horizontalJump.GetComponent<Outline>().effectColor = new Color(.94f, .45f, 1f, .95f);
            respawn.image.color = new Color(.42f, .22f, .08f, .96f);
            respawn.GetComponent<Outline>().effectColor = new Color(1f, .62f, .20f, .9f);
            Bind(horizontalJump, manager.HorizontalJump);
            ResultCaptureService capture = CreateResultCapture(null, resultImage);
            systems.AddComponent<SelectedVehicleFactory>().ConfigureTimeTrial(assets.catalog, input, manager, circuit,
                assets.testCircuit, hud, results, capture, controls, null, camera, new Vector3(radius, 3f, -5f));
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, TimeTrialPath);
        }

        private static VehicleController CreateClimbVehicle(VehicleDefinition definition, VehicleInputController input,
            GameManager manager, PhysicsMaterial chassisMaterial = null, PhysicsMaterial tireMaterial = null)
        {
            bool formula = definition.archetype == VehicleArchetype.Formula;
            bool rally = definition.archetype == VehicleArchetype.Rally;
            bool sportBike = definition.archetype == VehicleArchetype.SportBike;
            GameObject root = new GameObject("VehiclePhysics - " + definition.displayName);
            root.transform.position = Vector3.zero;
            root.AddComponent<Rigidbody>();
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 0;
            collider.center = new Vector3(formula ? 0f : rally ? .01f : sportBike ? -.03f : .06f,
                formula ? .24f : rally ? .33f : sportBike ? .18f : .24f, 0f);
            collider.radius = formula ? .29f : rally ? .38f : sportBike ? .27f : .40f;
            collider.height = formula ? 4.15f : rally ? 4.05f : sportBike ? 3.35f : 2.90f;
            collider.sharedMaterial = chassisMaterial;
            root.AddComponent<MovementPlaneConstraint>(); root.AddComponent<FuelSystem>();
            root.AddComponent<EngineController>(); root.AddComponent<TransmissionController>();
            VehicleController controller = root.AddComponent<VehicleController>();
            CrashDetector crash = root.AddComponent<CrashDetector>(); crash.AssignGameManager(manager);
            VehicleVisual2D visual2D = CreateVehicleVisual2D(root.transform, controller, definition);
            SuspensionController[] points = new SuspensionController[2];
            points[0] = CreateClimbSuspension(root.transform, definition, definition.RearWheelAnchor,
                visual2D.RearWheelSprite, tireMaterial);
            points[1] = CreateClimbSuspension(root.transform, definition, definition.FrontWheelAnchor,
                visual2D.FrontWheelSprite, tireMaterial);
            controller.AssignRuntimeReferences(definition, input, points);
            return controller;
        }

        private static RoadVehicleController CreateRoadVehicle(VehicleDefinition definition, VehicleInputController input,
            GameManager manager, PhysicsMaterial chassisMaterial = null)
        {
            bool formula = definition.archetype == VehicleArchetype.Formula;
            bool rally = definition.archetype == VehicleArchetype.Rally;
            bool sportBike = definition.archetype == VehicleArchetype.SportBike;
            GameObject root = new GameObject("VehiclePhysics - " + definition.displayName);
            root.transform.position = Vector3.zero;
            root.AddComponent<Rigidbody>();
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 2;
            collider.center = new Vector3(0f, formula ? .38f : rally ? .40f : sportBike ? .30f : .36f, 0f);
            collider.radius = formula ? .62f : rally ? .66f : sportBike ? .48f : .72f;
            collider.height = formula ? 4.20f : rally ? 4.35f : sportBike ? 3.45f : 3.05f;
            collider.sharedMaterial = chassisMaterial;
            root.AddComponent<FuelSystem>(); root.AddComponent<EngineController>(); root.AddComponent<TransmissionController>();
            RoadVehicleController controller = root.AddComponent<RoadVehicleController>();
            CrashDetector crash = root.AddComponent<CrashDetector>(); crash.AssignGameManager(manager);
            controller.Assign(definition, input);
            CreateVehicleVisual3D(root.transform, controller, definition);
            return controller;
        }

        private static void CreateVehicleVisual3D(Transform root, RoadVehicleController controller,
            VehicleDefinition definition)
        {
            Transform visual = new GameObject("VehicleVisual3D - " + definition.displayName).transform;
            visual.SetParent(root, false);
            Sprite bodySprite = definition.skins != null && definition.skins.Length > 0 ? definition.skins[0].sprite : definition.previewSprite;
            Sprite wheelSprite = definition.wheels != null && definition.wheels.Length > 0 ? definition.wheels[0].sprite : null;
            SpriteRenderer[] bodyRenderers = new SpriteRenderer[2];
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? -.06f : .06f;
                Transform body = CreateSpriteVisual(definition.displayName + (side == 0 ? " Body Left" : " Body Right"),
                    visual, bodySprite, new Vector3(x, definition.bodyVisualOffset.y, definition.bodyVisualOffset.x),
                    definition.visualWidth, 10);
                body.localRotation = Quaternion.Euler(0f, side == 0 ? -90f : 90f, 0f);
                bodyRenderers[side] = body.GetComponent<SpriteRenderer>();
                bodyRenderers[side].flipX = definition.flipBodyX ^ (side == 1);
            }
            float[] xs = { -definition.halfTrack - .03f, definition.halfTrack + .03f };
            Transform[] wheels = new Transform[4];
            SpriteRenderer[] wheelRenderers = new SpriteRenderer[4];
            bool[] frontWheels = new bool[4];
            int wheelIndex = 0;
            for (int z = 0; z < 2; z++) for (int x = 0; x < 2; x++)
            {
                bool front = z == 1;
                Vector2 wheelPosition = front ? definition.frontWheelVisualPosition : definition.rearWheelVisualPosition;
                Transform wheel = CreateWheelVisual(front ? "Front Wheel" : "Rear Wheel", visual, wheelSprite,
                    new Vector3(xs[x], wheelPosition.y, wheelPosition.x),
                    front ? definition.FrontWheelDiameter : definition.RearWheelDiameter, 11,
                    VehicleDefinition.WheelSourceDiameter(definition.wheels[0]));
                wheel.localRotation = Quaternion.Euler(0f, x == 0 ? -90f : 90f, 0f);
                wheelRenderers[wheelIndex] = wheel.GetComponent<SpriteRenderer>();
                if (x == 1) wheelRenderers[wheelIndex].flipX = true;
                wheels[wheelIndex] = wheel;
                frontWheels[wheelIndex] = z == 1;
                wheelIndex++;
            }
            DriverDefinition defaultDriver = AssetDatabase.LoadAssetAtPath<DriverDefinition>(Root + "/VehicleData/StickmanBlack.asset");
            ArticulatedDriver2D driver = CreateArticulatedDriver(visual, defaultDriver, definition, root.GetComponent<Rigidbody>(), true);
            driver.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            VehicleVisual3D component = visual.gameObject.AddComponent<VehicleVisual3D>();
            component.Assign(controller, wheels, frontWheels, definition.wheelRadius, Vector3.forward);
            component.AssignCustomization(definition, bodyRenderers, wheelRenderers, driver, defaultDriver);
            CreateRallyRoofSensor(root, visual, definition, bodyRenderers, true);
        }

        private static VehicleVisual2D CreateVehicleVisual2D(Transform root, VehicleRuntimeBase vehicle,
            VehicleDefinition definition)
        {
            Transform visual = new GameObject("VehicleVisual2D - " + definition.displayName).transform;
            visual.SetParent(root, false);
            Sprite bodySprite = definition.skins != null && definition.skins.Length > 0 ? definition.skins[0].sprite : definition.previewSprite;
            Sprite wheelSprite = definition.wheels != null && definition.wheels.Length > 0 ? definition.wheels[0].sprite : null;
            Transform body = CreateSpriteVisual(definition.displayName + " Body", visual, bodySprite,
                new Vector3(definition.bodyVisualOffset.x, definition.bodyVisualOffset.y, 0f), definition.visualWidth, 10);
            body.GetComponent<SpriteRenderer>().flipX = definition.flipBodyX;
            Transform rearWheel = CreateWheelVisual("Rear Wheel", visual, wheelSprite,
                new Vector3(definition.rearWheelVisualPosition.x, definition.rearWheelVisualPosition.y, -.04f),
                definition.RearWheelDiameter, 11,
                VehicleDefinition.WheelSourceDiameter(definition.wheels[0]));
            Transform frontWheel = CreateWheelVisual("Front Wheel", visual, wheelSprite,
                new Vector3(definition.frontWheelVisualPosition.x, definition.frontWheelVisualPosition.y, -.04f),
                definition.FrontWheelDiameter, 11,
                VehicleDefinition.WheelSourceDiameter(definition.wheels[0]));
            DriverDefinition defaultDriver = AssetDatabase.LoadAssetAtPath<DriverDefinition>(Root + "/VehicleData/StickmanBlack.asset");
            ArticulatedDriver2D articulated = CreateArticulatedDriver(visual, defaultDriver, definition,
                root.GetComponent<Rigidbody>(), true);
            VehicleVisual2D component = visual.gameObject.AddComponent<VehicleVisual2D>();
            component.Assign(vehicle, body, frontWheel, rearWheel, articulated.transform, definition.wheelRadius);
            component.AssignCustomization(definition, body.GetComponent<SpriteRenderer>(),
                frontWheel.GetComponent<SpriteRenderer>(), rearWheel.GetComponent<SpriteRenderer>(), articulated, defaultDriver);
            CreateRallyRoofSensor(root, visual, definition,
                new[] { body.GetComponent<SpriteRenderer>() }, false);
            return component;
        }

        private static void CreateRallyRoofSensor(Transform vehicleRoot, Transform visualRoot,
            VehicleDefinition definition, SpriteRenderer[] bodyRenderers, bool roadMode)
        {
            if (definition.archetype != VehicleArchetype.Rally) return;
            GameObject sensor = new GameObject("Rally Roof Terrain Impact Sensor");
            sensor.transform.SetParent(vehicleRoot, false);
            sensor.transform.localPosition = roadMode ? new Vector3(0f, .92f, 0f) : new Vector3(-.05f, .92f, 0f);
            BoxCollider trigger = sensor.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = roadMode ? new Vector3(1.25f, .18f, 1.55f) : new Vector3(1.85f, .18f, .5f);
            RallyRoofDamage damage = sensor.AddComponent<RallyRoofDamage>();
            damage.Assign(definition, vehicleRoot.GetComponent<Rigidbody>(), bodyRenderers, visualRoot);
        }

        private static Transform CreateSpriteVisual(string name, Transform parent, Sprite sprite,
            Vector3 localPosition, float targetWidth, int sortingOrder)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            float sourceWidth = sprite != null ? sprite.bounds.size.x : 1f;
            gameObject.transform.localScale = Vector3.one * (targetWidth / Mathf.Max(.001f, sourceWidth));
            return gameObject.transform;
        }

        private static Transform CreateWheelVisual(string name, Transform parent, Sprite sprite,
            Vector3 localPosition, float targetDiameter, int sortingOrder, float visibleDiameter = 0f)
        {
            Transform wheel = CreateSpriteVisual(name, parent, sprite, localPosition, targetDiameter, sortingOrder);
            float sourceDiameter = visibleDiameter > .001f ? visibleDiameter :
                sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
            wheel.localScale = Vector3.one * (targetDiameter / Mathf.Max(.001f, sourceDiameter));
            return wheel;
        }

        private static ArticulatedDriver2D CreateArticulatedDriver(Transform parent, DriverDefinition definition,
            VehicleDefinition vehicle, Rigidbody body, bool physicalReaction)
        {
            Transform root = new GameObject("Driver - Articulated").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, 0f, -.03f);
            SpriteRenderer Part(string name)
            {
                GameObject part = new GameObject(name);
                part.transform.SetParent(root, false);
                return part.AddComponent<SpriteRenderer>();
            }
            SpriteRenderer torso = Part("Torso");
            SpriteRenderer head = Part("Head");
            SpriteRenderer leftUpperArm = Part("Left Upper Arm");
            SpriteRenderer leftForearm = Part("Left Forearm / Hand");
            SpriteRenderer rightUpperArm = Part("Right Upper Arm");
            SpriteRenderer rightForearm = Part("Right Forearm / Hand");
            SpriteRenderer leftThigh = Part("Left Thigh");
            SpriteRenderer leftLowerLeg = Part("Left Lower Leg");
            SpriteRenderer rightThigh = Part("Right Thigh");
            SpriteRenderer rightLowerLeg = Part("Right Lower Leg");
            GameObject impactObject = new GameObject("Head Terrain Reaction Trigger");
            impactObject.transform.SetParent(root, false);
            impactObject.transform.localPosition = new Vector3(vehicle.driverPose.head.position.x,
                vehicle.driverPose.head.position.y, 0f);
            SphereCollider impactCollider = impactObject.AddComponent<SphereCollider>();
            impactCollider.isTrigger = true; impactCollider.radius = .27f;
            DriverHeadImpact impact = impactObject.AddComponent<DriverHeadImpact>();
            ArticulatedDriver2D articulated = root.gameObject.AddComponent<ArticulatedDriver2D>();
            articulated.AssignParts(torso, head, leftUpperArm, leftForearm, rightUpperArm, rightForearm,
                leftThigh, leftLowerLeg, rightThigh, rightLowerLeg, impact);
            articulated.Apply(definition, vehicle.driverPose, body, physicalReaction);
            return articulated;
        }

        private static SuspensionController CreateClimbSuspension(Transform root, VehicleDefinition definition,
            float x, Transform wheelVisual, PhysicsMaterial tireMaterial)
        {
            GameObject origin = new GameObject(x < 0f ? "Rear Suspension" : "Front Suspension");
            origin.transform.SetParent(root, false);
            origin.transform.localPosition = new Vector3(x, -definition.suspension.restLength, 0f);
            Rigidbody wheelBody = origin.AddComponent<Rigidbody>();
            wheelBody.mass = definition.wheelMass;
            wheelBody.interpolation = RigidbodyInterpolation.Interpolate;
            wheelBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            wheelBody.constraints = RigidbodyConstraints.FreezePositionZ |
                                    RigidbodyConstraints.FreezeRotationX |
                                    RigidbodyConstraints.FreezeRotationY;
            SphereCollider wheelCollider = origin.AddComponent<SphereCollider>();
            wheelCollider.radius = definition.wheelRadius;
            wheelCollider.sharedMaterial = tireMaterial;
            ConfigurableJoint joint = root.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = wheelBody;
            joint.enableCollision = false;
            SuspensionController suspension = origin.AddComponent<SuspensionController>();
            suspension.AssignWheelVisual(wheelVisual);
            suspension.AssignPhysicalReferences(wheelBody, wheelCollider, joint);
            return suspension;
        }

        private static AnimationCurve ArcadeSpeedTorqueCurve() => new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(.55f, 1f), new Keyframe(.82f, .68f),
            new Keyframe(.96f, .22f), new Keyframe(1.04f, 0f));

        private static PickupPool CreatePickupPool(BuiltAssets assets)
        {
            GameObject root = new GameObject("Pickup Pool");
            PickupPool pool = root.AddComponent<PickupPool>();
            CoinPickup coinOne = CreateCoinTemplate("Coin 01 Template", root.transform, assets.coin01, 1);
            CoinPickup coinFive = CreateCoinTemplate("Coin 05 Template", root.transform, assets.coin05, 5);
            GameObject fuelObject = new GameObject("Fuel Can Template");
            fuelObject.transform.SetParent(root.transform, false);
            GameObject fuelVisual = new GameObject("Fuel Can Visual");
            fuelVisual.transform.SetParent(fuelObject.transform, false);
            SpriteRenderer fuelRenderer = fuelVisual.AddComponent<SpriteRenderer>();
            fuelRenderer.sprite = assets.fuelCan;
            fuelRenderer.sortingOrder = 12;
            float fuelSourceHeight = assets.fuelCan != null ? assets.fuelCan.bounds.size.y : 1f;
            fuelVisual.transform.localScale = Vector3.one * (1.10f / Mathf.Max(.001f, fuelSourceHeight));
            BoxCollider fuelTrigger = fuelObject.AddComponent<BoxCollider>();
            fuelTrigger.isTrigger = true;
            fuelTrigger.size = new Vector3(.96f, 1.06f, .66f);
            FuelPickup fuel = fuelObject.AddComponent<FuelPickup>();
            pool.AssignPrefabs(coinOne, coinFive, fuel);
            coinOne.gameObject.SetActive(false); coinFive.gameObject.SetActive(false); fuelObject.SetActive(false);
            return pool;
        }

        private static CoinPickup CreateCoinTemplate(string name, Transform parent, Sprite sprite, int value)
        {
            GameObject coinObject = new GameObject(name);
            coinObject.transform.SetParent(parent, false);
            GameObject visual = new GameObject("Coin Visual");
            visual.transform.SetParent(coinObject.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 12;
            float sourceDiameter = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
            visual.transform.localScale = Vector3.one * (.82f / Mathf.Max(.001f, sourceDiameter));
            SphereCollider trigger = coinObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = .40f;
            CoinPickup pickup = coinObject.AddComponent<CoinPickup>();
            pickup.AssignValue(value);
            return pickup;
        }

        private static GameplayHUD CreateGameplayHud(Transform safe, VehicleInputController input, BuiltAssets assets)
        {
            GameObject root = UIObject("Gameplay HUD", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject distanceCard = CreateHudCard("Distance Card", root.transform, new Vector2(.5f, 1f),
                new Vector2(0f, -58f), new Vector2(430f, 84f));
            Text distance = CreateText("Distance", distanceCard.transform, "DISTANCE\n0 m", 32,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject bestCard = CreateHudCard("Best Card", root.transform, new Vector2(0f, 1f),
                new Vector2(170f, -48f), new Vector2(280f, 70f));
            Text best = CreateText("Best", bestCard.transform, "BEST\n0 m", 25, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject coinCard = CreateHudCard("Coin Balance Card", root.transform, Vector2.one,
                new Vector2(-170f, -48f), new Vector2(280f, 70f));
            Image coinIcon = CreateSpriteImage("Coin Icon", coinCard.transform, assets.coin01,
                new Vector2(.5f, .5f), new Vector2(-98f, 0f), new Vector2(52f, 52f));
            Text coins = CreateText("Coins", coinCard.transform, "0", 30, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(-22f, 0f), new Vector2(-28f, 0f));
            GameObject nextFuelCard = CreateHudCard("Next Fuel Card", root.transform, new Vector2(.5f, 1f),
                new Vector2(0f, -148f), new Vector2(390f, 62f));
            Image nextFuelIcon = CreateSpriteImage("Next Fuel Icon", nextFuelCard.transform, assets.fuelCan,
                new Vector2(.5f, .5f), new Vector2(-155f, 0f), new Vector2(48f, 48f));
            Text nextFuel = CreateText("Next Fuel", nextFuelCard.transform, "NEXT FUEL   ---", 23,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(25f, 0f), new Vector2(-60f, 0f));
            GameObject fuelCard = CreateHudCard("Fuel Card", root.transform, new Vector2(0f, 1f),
                new Vector2(205f, -132f), new Vector2(350f, 70f));
            Image fuelIcon = CreateSpriteImage("Fuel Icon", fuelCard.transform, assets.fuelCan,
                new Vector2(.5f, .5f), new Vector2(-145f, 0f), new Vector2(52f, 52f));
            Text fuelText = CreateText("Fuel Text", fuelCard.transform, "100%", 21, TextAnchor.MiddleCenter,
                new Vector2(.12f, 0f), new Vector2(.35f, 1f), Vector2.zero, Vector2.zero);
            GameObject fuelBack = CreateImage("Fuel Background", fuelCard.transform,
                new Color(0.015f, 0.02f, 0.035f, 0.92f), new Vector2(.36f, .5f), new Vector2(.36f, .5f),
                new Vector2(92f, 0f), new Vector2(190f, 20f));
            GameObject fuelFillObject = CreateImage("Fuel Fill", fuelBack.transform, new Color(0.18f, 0.88f, 0.34f),
                Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-8f, -8f));
            Image fuelFill = fuelFillObject.GetComponent<Image>(); fuelFill.type = Image.Type.Filled;
            fuelFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fuelFill.fillMethod = Image.FillMethod.Horizontal;
            fuelFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fuelFill.fillAmount = 1f;
            GameObject liquidCard = CreateHudCard("Liquid Exposure Card", root.transform, new Vector2(0f, 1f),
                new Vector2(205f, -205f), new Vector2(350f, 58f));
            Text liquidText = CreateText("Liquid Exposure Text", liquidCard.transform, "LIQUID  0.0 / 10.0s", 18,
                TextAnchor.MiddleLeft, new Vector2(.05f, .46f), new Vector2(.95f, .95f), Vector2.zero, Vector2.zero);
            GameObject liquidBack = CreateImage("Liquid Background", liquidCard.transform,
                new Color(.01f, .03f, .06f, .95f), new Vector2(.05f, .16f), new Vector2(.95f, .43f),
                Vector2.zero, Vector2.zero);
            Image liquidFill = CreateImage("Liquid Fill", liquidBack.transform,
                new Color(.08f, .88f, 1f, 1f), Vector2.zero, Vector2.one,
                new Vector2(3f, 3f), new Vector2(-6f, -6f)).GetComponent<Image>();
            liquidFill.type = Image.Type.Filled;
            liquidFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            liquidFill.fillMethod = Image.FillMethod.Horizontal;
            liquidFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            liquidFill.fillAmount = 0f;
            RectTransform rpmNeedle = CreateVehicleGauge("RPM Gauge", root.transform,
                assets.rpmGauge, assets.rpmNeedle, new Vector2(-150f, 118f), 200f,
                new Vector2(.499694f, .357008f), new Vector2(.136906f, .202061f), 104f, 159f);
            RectTransform speedNeedle = CreateVehicleGauge("KMH Gauge", root.transform,
                assets.speedGauge, assets.speedNeedle, new Vector2(150f, 118f), 200f,
                new Vector2(.499704f, .316062f), new Vector2(.138627f, .166769f), 116f, 172f);
            Text status = CreateText("Status", root.transform, "", 34, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(500f, 60f));
            GameObject debugPanel = CreateImage("Debug Panel", root.transform, new Color(0f, 0f, 0f, 0.75f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-25f, 80f), new Vector2(540f, 570f));
            Text debug = CreateText("Debug Text", debugPanel.transform, "", 20, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(15f, -15f), new Vector2(-30f, -30f));
            GameplayHUD hud = root.AddComponent<GameplayHUD>();
            hud.Assign(distance, best, coins, coinIcon, nextFuel, nextFuelIcon, fuelText, fuelIcon, fuelFill,
                speedNeedle, rpmNeedle, status,
                debugPanel, debug, input, nextFuelCard);
            hud.AssignLiquidExposure(liquidCard, liquidText, liquidFill);
            debugPanel.SetActive(false);
            return hud;
        }

        private static ResultsUI CreateResults(Transform safe, GameManager manager, out RawImage resultImage)
        {
            GameObject panel = CreateImage("Results Panel", safe, new Color(0.025f, 0.035f, 0.065f, 0.96f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 1000f));
            Outline resultsOutline = panel.AddComponent<Outline>();
            resultsOutline.effectColor = new Color(.22f, .62f, .94f, .85f);
            resultsOutline.effectDistance = new Vector2(2f, -2f);
            GameObject photo = UIObject("Result Photo", panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -310f), new Vector2(580f, 580f));
            resultImage = photo.AddComponent<RawImage>(); resultImage.color = Color.white;
            resultImage.raycastTarget = false;
            Text text = CreateText("Results", panel.transform, "RUN OVER", 28, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -730f), new Vector2(940f, 240f));
            ResultsUI results = panel.AddComponent<ResultsUI>(); results.Assign(panel, text, manager);
            Button retry = CreateButton("Retry", panel.transform, "RETRY", new Vector2(-230f, -440f), new Vector2(390f, 72f));
            Button menu = CreateButton("Main Menu", panel.transform, "MAIN MENU", new Vector2(230f, -440f), new Vector2(390f, 72f));
            Bind(retry, results.Retry); Bind(menu, results.Menu); panel.SetActive(false);
            return results;
        }

        private static CanvasGroup CreateClimbControls(Transform safe, VehicleInputController input)
        {
            GameObject root = UIObject("Gameplay Controls", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            CreateHalfScreenPedal("Brake", root.transform, input, HoldControl.Brake,
                AssetDatabase.LoadAssetAtPath<Sprite>(BrakePedalPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(BrakePedalPressedPath),
                Vector2.zero, new Vector2(.5f, 1f), Vector2.zero, new Vector2(165f, 177f));
            CreateHalfScreenPedal("Gas", root.transform, input, HoldControl.Gas,
                AssetDatabase.LoadAssetAtPath<Sprite>(GasPedalPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(GasPedalPressedPath),
                new Vector2(.5f, 0f), Vector2.one, new Vector2(1f, 0f), new Vector2(-165f, 177f));
            return group;
        }

        private static CanvasGroup CreateRoadControls(Transform safe, VehicleInputController input)
        {
            GameObject root = UIObject("Gameplay Controls", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            Button left = CreateButton("Left", root.transform, "LEFT", new Vector2(155f, 290f), new Vector2(250f, 120f), Vector2.zero);
            Button right = CreateButton("Right", root.transform, "RIGHT", new Vector2(435f, 290f), new Vector2(250f, 120f), Vector2.zero);
            left.gameObject.AddComponent<HoldButton>().Assign(input, HoldControl.Left);
            right.gameObject.AddComponent<HoldButton>().Assign(input, HoldControl.Right);
            CreatePedalButton("Brake", root.transform, input, HoldControl.Brake,
                AssetDatabase.LoadAssetAtPath<Sprite>(BrakePedalPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(BrakePedalPressedPath),
                Vector2.zero, new Vector2(156f, 147f), new Vector2(219f, 274f));
            CreatePedalButton("Gas", root.transform, input, HoldControl.Gas,
                AssetDatabase.LoadAssetAtPath<Sprite>(GasPedalPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(GasPedalPressedPath),
                new Vector2(1f, 0f), new Vector2(-156f, 147f), new Vector2(219f, 274f));
            return group;
        }

        private static ShifterUI CreateShifter(Transform safe, VehicleRuntimeBase vehicle, VehicleInputController input,
            GameModeType mode)
        {
            Vector2 size = mode == GameModeType.Adventure2_5D ? new Vector2(330f, 120f) : new Vector2(145f, 250f);
            GameObject root = CreateImage("Visible Shifter", safe, new Color(0.04f, 0.05f, 0.08f, 0.88f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), size);
            Text directions = CreateText("Directions", root.transform, "", 22, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject knobObject = CreateImage("Shifter Knob", root.transform, new Color(0.92f, 0.22f, 0.18f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f));
            ShifterUI shifter = root.AddComponent<ShifterUI>();
            shifter.Assign(vehicle, input, knobObject.GetComponent<RectTransform>(), directions, mode);
            return shifter;
        }

        private static ResultCaptureService CreateResultCapture(VehicleRuntimeBase vehicle, RawImage resultImage,
            bool sideView = false)
        {
            GameObject root = new GameObject("Result Capture Service");
            Camera camera = new GameObject("ResultCaptureCamera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform, false); camera.fieldOfView = 52f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.11f, 0.17f, 0.25f);
            camera.orthographic = sideView;
            if (sideView) camera.orthographicSize = 12.5f;
            ResultCaptureService service = root.AddComponent<ResultCaptureService>();
            service.Assign(camera, vehicle != null ? vehicle.transform : null, resultImage);
            return service;
        }

        private static TimeTrialHUD CreateTimeTrialHud(Transform safe, VehicleInputController input)
        {
            GameObject root = UIObject("Time Trial HUD", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject timerCard = CreateHudCard("Timer Card", root.transform, new Vector2(.5f, 1f),
                new Vector2(0f, -58f), new Vector2(430f, 92f));
            Text timer = CreateText("Time", timerCard.transform, "TIME\n00:00.000", 36, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject lapCard = CreateHudCard("Lap Card", root.transform, Vector2.up,
                new Vector2(155f, -52f), new Vector2(270f, 78f));
            Text lap = CreateText("Lap", lapCard.transform, "LAP\n1 / 1", 28, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject bestCard = CreateHudCard("Best Lap Card", root.transform, Vector2.one,
                new Vector2(-180f, -52f), new Vector2(320f, 78f));
            Text best = CreateText("Best Lap", bestCard.transform, "BEST LAP\n--:--.---", 25,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Sprite rpmGauge = AssetDatabase.LoadAssetAtPath<Sprite>(RpmGaugePath);
            Sprite rpmArrow = AssetDatabase.LoadAssetAtPath<Sprite>(RpmNeedlePath);
            Sprite speedGauge = AssetDatabase.LoadAssetAtPath<Sprite>(SpeedGaugePath);
            Sprite speedArrow = AssetDatabase.LoadAssetAtPath<Sprite>(SpeedNeedlePath);
            RectTransform rpmNeedle = CreateVehicleGauge("RPM Gauge", root.transform, rpmGauge, rpmArrow,
                new Vector2(-150f, 118f), 200f, new Vector2(.499694f, .357008f),
                new Vector2(.136906f, .202061f), 104f, 159f);
            RectTransform speedNeedle = CreateVehicleGauge("KMH Gauge", root.transform, speedGauge, speedArrow,
                new Vector2(150f, 118f), 200f, new Vector2(.499704f, .316062f),
                new Vector2(.138627f, .166769f), 116f, 172f);
            GameObject debugPanel = CreateImage("Debug Panel", root.transform, new Color(0f, 0f, 0f, 0.75f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-25f, 80f), new Vector2(500f, 340f));
            Text debug = CreateText("Debug Text", debugPanel.transform, "", 20, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(15f, -15f), new Vector2(-30f, -30f));
            TimeTrialHUD hud = root.AddComponent<TimeTrialHUD>();
            hud.Assign(timer, lap, best, speedNeedle, rpmNeedle, debugPanel, debug, input);
            debugPanel.SetActive(false); return hud;
        }

        private static VehicleCamera CreateClimbCamera(VehicleRuntimeBase target)
        {
            GameObject cameraObject = CameraObject(new Color(0.92f, 0.48f, 0.35f));
            cameraObject.transform.position = new Vector3(7.4f, 4.8f, -20f);
            cameraObject.transform.rotation = Quaternion.identity;
            Camera displayCamera = cameraObject.GetComponent<Camera>();
            displayCamera.orthographic = true;
            displayCamera.orthographicSize = 11.5f;
            VehicleCamera followCamera = cameraObject.AddComponent<VehicleCamera>(); followCamera.AssignTarget(target);
            return followCamera;
        }

        private static MountainBackground CreateCountrysideBackground(Camera camera, EnvironmentDefinition environment)
        {
            GameObject root = new GameObject("Countryside Background - Recycled");
            GameObject skyObject = new GameObject("Sky - No Physics");
            skyObject.transform.SetParent(root.transform, false);
            SpriteRenderer sky = skyObject.AddComponent<SpriteRenderer>();
            sky.sprite = environment.sky; sky.sortingOrder = -100;
            int count = Mathf.Clamp(environment.pooledMountainSegments, 3, 7);
            SpriteRenderer[] mountains = new SpriteRenderer[count];
            for (int i = 0; i < count; i++)
            {
                GameObject segment = new GameObject("Mountain Segment " + (i + 1) + " - Pooled");
                segment.transform.SetParent(root.transform, false);
                mountains[i] = segment.AddComponent<SpriteRenderer>();
                mountains[i].sprite = environment.mountains;
                mountains[i].sortingOrder = -50;
            }
            MountainBackground background = root.AddComponent<MountainBackground>();
            background.Assign(camera, environment, sky, mountains);
            return background;
        }

        private static RoadChaseCamera CreateRoadCamera(VehicleRuntimeBase target)
        {
            GameObject cameraObject = CameraObject(new Color(0.86f, 0.43f, 0.36f));
            cameraObject.transform.position = new Vector3(0f, 9f, -13f);
            cameraObject.GetComponent<Camera>().fieldOfView = 58f;
            RoadChaseCamera camera = cameraObject.AddComponent<RoadChaseCamera>(); camera.Assign(target);
            return camera;
        }

        private static GameObject CameraObject(Color background)
        {
            GameObject cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background; cameraObject.AddComponent<AudioListener>(); return cameraObject;
        }

        private static void CreateMenuCamera(Color background)
        {
            GameObject cameraObject = CameraObject(background);
            cameraObject.name = "MainMenuCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.transform.rotation = Quaternion.identity;
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.targetDisplay = 0;
            camera.enabled = true;
        }

        private static void CreateSunsetLighting(bool addSunDisc)
        {
            GameObject lightObject = new GameObject("Sun Light"); lightObject.transform.rotation = Quaternion.Euler(70f, -25f, 0f);
            Light light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 0.95f;
            light.shadows = LightShadows.None; RenderSettings.ambientLight = new Color(0.48f, 0.38f, 0.40f);
            if (addSunDisc)
            {
                GameObject sun = Primitive(PrimitiveType.Sphere, "Sun", null, new Vector3(30f, 11f, 9f),
                    Vector3.one * 4.5f, new Color(1f, 0.68f, 0.20f)); RemoveCollider(sun);
            }
        }

        private static Canvas CreateCanvas(Color background, bool addBackground = true)
        {
            GameObject canvasObject = new GameObject("UI Canvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            if (addBackground) CreateImage("Background", canvas.transform, background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return canvas;
        }

        private static Transform CreateSafeArea(Transform canvas)
        {
            GameObject safe = UIObject("Safe Area", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.AddComponent<SafeArea>(); return safe.transform;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem"); eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }

        private static Material MaterialAsset(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");
            if (shader == null) throw new System.InvalidOperationException("No compatible built-in lit shader was found.");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader) material.shader = shader;
            material.color = color; EditorUtility.SetDirty(material); return material;
        }

        private static PhysicsMaterial PhysicsMaterialAsset(string name, float friction, float bounciness)
        {
            string path = Root + "/Materials/" + name + ".physicMaterial";
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material == null)
            {
                material = new PhysicsMaterial(name);
                AssetDatabase.CreateAsset(material, path);
            }
            material.dynamicFriction = friction;
            material.staticFriction = friction;
            material.bounciness = bounciness;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material UnlitMaterialAsset(string name, Color color)
        {
            string path = Root + "/Materials/Unlit_" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            if (shader == null) throw new System.InvalidOperationException("No compatible built-in unlit shader was found.");
            if (material == null)
            {
                material = new Material(shader) { name = "Unlit_" + name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader) material.shader = shader;
            material.color = color; EditorUtility.SetDirty(material); return material;
        }

        private static Material UnlitTextureMaterialAsset(string name, Texture texture)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            if (shader == null) throw new System.InvalidOperationException("No compatible unlit texture shader was found.");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader) material.shader = shader;
            material.mainTexture = texture;
            material.color = Color.white;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject FlatQuad(string name, Transform parent, Vector3 position, Vector2 size, Color color)
        {
            GameObject quad = Primitive(PrimitiveType.Quad, name, parent, position,
                new Vector3(size.x, size.y, 1f), color);
            RemoveCollider(quad);
            quad.GetComponent<Renderer>().sharedMaterial = UnlitMaterialAsset(ColorUtility.ToHtmlStringRGB(color), color);
            return quad;
        }

        private static GameObject FlatDisc(string name, Transform parent, Vector3 position, float diameter, Color color)
        {
            GameObject disc = new GameObject(name);
            disc.transform.SetParent(parent, false); disc.transform.localPosition = position;
            disc.transform.localScale = new Vector3(diameter, diameter, 1f);
            disc.AddComponent<MeshFilter>().sharedMesh = LoadOrCreateDiscMesh();
            disc.AddComponent<MeshRenderer>().sharedMaterial = UnlitMaterialAsset(ColorUtility.ToHtmlStringRGB(color), color);
            return disc;
        }

        private static Mesh LoadOrCreateDiscMesh()
        {
            string path = Root + "/Materials/FlatDisc2D.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            const int segments = 32;
            Vector3[] vertices = new Vector3[segments + 1];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[segments * 3];
            vertices[0] = Vector3.zero; uvs[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, Mathf.Sin(angle) * 0.5f, 0f);
                uvs[i + 1] = vertices[i + 1] + Vector3.one * 0.5f;
                int next = (i + 1) % segments;
                triangles[i * 3] = 0; triangles[i * 3 + 1] = next + 1; triangles[i * 3 + 2] = i + 1;
            }
            mesh = new Mesh { name = "FlatDisc2D" };
            mesh.vertices = vertices; mesh.uv = uvs; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position,
            Vector3 scale, Color color)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type); gameObject.name = name;
            if (parent != null) { gameObject.transform.SetParent(parent, false); gameObject.transform.localPosition = position; }
            else gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = MaterialAsset("Primitive_" + ColorUtility.ToHtmlStringRGB(color), color);
            return gameObject;
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>(); if (collider != null) Object.DestroyImmediate(collider);
        }

        private static GameObject UIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform)); RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
            rect.anchoredPosition = position; rect.sizeDelta = size; return gameObject;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject gameObject = UIObject(name, parent, anchorMin, anchorMax, position, size);
            gameObject.AddComponent<Image>().color = color; return gameObject;
        }

        private static Image CreateSpriteImage(string name, Transform parent, Sprite sprite, Vector2 anchor,
            Vector2 position, Vector2 size)
        {
            GameObject gameObject = UIObject(name, parent, anchor, anchor, position, size);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform CreateVehicleGauge(string name, Transform parent, Sprite face, Sprite needle,
            Vector2 position, float faceHeight, Vector2 faceCenter, Vector2 needlePivot,
            float needleTipLength, float zeroAngle)
        {
            float faceAspect = face != null && face.rect.height > 0f ? face.rect.width / face.rect.height : 1.3f;
            GameObject gaugeObject = UIObject(name, parent, new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                position, new Vector2(faceHeight * faceAspect, faceHeight));
            Image faceImage = gaugeObject.AddComponent<Image>();
            faceImage.sprite = face; faceImage.preserveAspect = true; faceImage.raycastTarget = false;

            Vector2 sourceTip = needle != null
                ? new Vector2((1f - needlePivot.x) * needle.rect.width,
                    (1f - needlePivot.y) * needle.rect.height)
                : Vector2.one;
            float needleScale = needleTipLength / Mathf.Max(1f, sourceTip.magnitude);
            Vector2 needleSize = needle != null
                ? new Vector2(needle.rect.width * needleScale, needle.rect.height * needleScale)
                : new Vector2(needleTipLength, needleTipLength * .7f);
            GameObject needleObject = UIObject(name + " Needle", gaugeObject.transform,
                faceCenter, faceCenter, Vector2.zero, needleSize);
            RectTransform needleRect = needleObject.GetComponent<RectTransform>();
            needleRect.pivot = needlePivot;
            needleRect.localRotation = Quaternion.Euler(0f, 0f, zeroAngle);
            Image needleImage = needleObject.AddComponent<Image>();
            needleImage.sprite = needle; needleImage.preserveAspect = true; needleImage.raycastTarget = false;
            return needleRect;
        }

        private static HoldButton CreateHalfScreenPedal(string name, Transform parent,
            VehicleInputController input, HoldControl control, Sprite normal, Sprite pressed,
            Vector2 zoneAnchorMin, Vector2 zoneAnchorMax, Vector2 visualAnchor, Vector2 visualPosition)
        {
            GameObject zone = UIObject(name + " Touch Zone", parent, zoneAnchorMin, zoneAnchorMax,
                Vector2.zero, Vector2.zero);
            Image hitArea = zone.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, .001f);
            hitArea.raycastTarget = true;
            Image visual = CreateSpriteImage(name + " Pedal", zone.transform, normal, visualAnchor,
                visualPosition, new Vector2(265f, 334f));
            HoldButton hold = zone.AddComponent<HoldButton>();
            hold.Assign(input, control, visual, normal, pressed);
            return hold;
        }

        private static HoldButton CreatePedalButton(string name, Transform parent, VehicleInputController input,
            HoldControl control, Sprite normal, Sprite pressed, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject pedal = UIObject(name, parent, anchor, anchor, position, size);
            Image visual = pedal.AddComponent<Image>();
            visual.sprite = normal; visual.preserveAspect = true; visual.raycastTarget = true;
            HoldButton hold = pedal.AddComponent<HoldButton>();
            hold.Assign(input, control, visual, normal, pressed);
            return hold;
        }

        private static Image CreateAspectCoverBackground(Transform parent, Sprite sprite)
        {
            GameObject gameObject = UIObject("Garage Background", parent, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            AspectRatioFitter fitter = gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite != null && sprite.rect.height > 0f
                ? sprite.rect.width / sprite.rect.height : 1.5f;
            return image;
        }

        private static Button CreateSpriteButton(string name, Transform parent, Sprite sprite, Vector2 anchor,
            Vector2 position, Vector2 size)
        {
            GameObject gameObject = UIObject(name, parent, anchor, anchor, position, size);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            Button button = gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(.72f, .86f, 1f, 1f);
            colors.disabledColor = new Color(.35f, .38f, .42f, .65f);
            colors.fadeDuration = .06f;
            button.colors = colors;
            gameObject.AddComponent<ButtonPressFeedback>();
            return button;
        }

        private static GaragePreviewRenderer CreateGarageVehiclePreview(BuiltAssets assets, RawImage target,
            string context)
        {
            GameObject previewCameraObject = new GameObject(context + " Preview Camera");
            previewCameraObject.transform.position = new Vector3(1000f, 0f, -10f);
            Camera previewCamera = previewCameraObject.AddComponent<Camera>();
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 2.35f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            previewCamera.enabled = false;
            Transform previewRoot = new GameObject(context + " Composed Vehicle Preview").transform;
            previewRoot.position = new Vector3(1000f, 0f, 0f);
            VehicleDefinition initial = assets.jeep;
            Transform body = CreateSpriteVisual("Preview Body", previewRoot, assets.jeepBody,
                new Vector3(initial.bodyVisualOffset.x, initial.bodyVisualOffset.y, 0f), initial.visualWidth, 10);
            Transform rearWheel = CreateWheelVisual("Preview Rear Wheel", previewRoot, assets.jeepWheel01,
                new Vector3(initial.rearWheelVisualPosition.x, initial.rearWheelVisualPosition.y, -.02f),
                initial.RearWheelDiameter, 11);
            Transform frontWheel = CreateWheelVisual("Preview Front Wheel", previewRoot, assets.jeepWheel01,
                new Vector3(initial.frontWheelVisualPosition.x, initial.frontWheelVisualPosition.y, -.02f),
                initial.FrontWheelDiameter, 11);
            ArticulatedDriver2D driver = CreateArticulatedDriver(previewRoot, assets.stickmanBlack, initial, null, false);
            GaragePreviewRenderer preview = new GameObject(context + " Preview Renderer").AddComponent<GaragePreviewRenderer>();
            preview.Assign(previewCamera, target, previewRoot, body.GetComponent<SpriteRenderer>(),
                rearWheel.GetComponent<SpriteRenderer>(), frontWheel.GetComponent<SpriteRenderer>(), driver);
            return preview;
        }

        private static GameObject CreateHudCard(string name, Transform parent, Vector2 anchor, Vector2 position,
            Vector2 size)
        {
            GameObject card = CreateImage(name, parent, new Color(.025f, .06f, .12f, .88f),
                anchor, anchor, position, size);
            Image image = card.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(.18f, .56f, .84f, .72f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return card;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions)
        {
            GameObject gameObject = UIObject(name, parent, anchorMin, anchorMax, position, dimensions);
            Text text = gameObject.AddComponent<Text>(); font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font; text.text = value; text.fontSize = size; text.alignment = alignment; text.color = Color.white;
            text.supportRichText = true; text.resizeTextForBestFit = true; text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = size; text.raycastTarget = false; return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size,
            Vector2? anchor = null)
        {
            Vector2 selectedAnchor = anchor ?? new Vector2(0.5f, 0.5f);
            GameObject gameObject = CreateImage(name, parent, new Color(0.13f, 0.30f, 0.53f, 0.95f),
                selectedAnchor, selectedAnchor, position, size);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            Button button = gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(.72f, .84f, 1f, 1f);
            colors.selectedColor = new Color(.82f, .94f, 1f, 1f);
            colors.disabledColor = new Color(.34f, .38f, .45f, .72f);
            colors.fadeDuration = .06f;
            button.colors = colors;
            Outline outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.20f, .60f, .90f, .78f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            Shadow shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .55f);
            shadow.effectDistance = new Vector2(0f, -5f);
            gameObject.AddComponent<ButtonPressFeedback>();
            Text text = CreateText("Label", gameObject.transform,
                label, 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.fontStyle = FontStyle.Bold; return button;
        }

        private static Button CreateModernMenuButton(string name, Transform parent, string label, Vector2 position)
        {
            Button button = CreateButton(name, parent, label, position, new Vector2(510f, 88f));
            Image image = button.image;
            image.color = new Color(.055f, .16f, .34f, .96f);
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(.72f, .82f, 1f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = .08f;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            Outline border = button.GetComponent<Outline>();
            border.effectColor = new Color(.25f, .72f, 1f, .92f);
            border.effectDistance = new Vector2(2f, -2f);
            Shadow[] effects = button.GetComponents<Shadow>();
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i].GetType() != typeof(Shadow)) continue;
                effects[i].effectColor = new Color(0f, 0f, 0f, .65f);
                effects[i].effectDistance = new Vector2(0f, -7f);
            }
            Text text = button.GetComponentInChildren<Text>();
            if (text != null) { text.fontSize = 29; text.fontStyle = FontStyle.Bold; }
            return button;
        }

        private static void Bind(Button button, UnityAction action)
        {
            UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }

        private static void EnsureFolders()
        {
            string[] paths = { Root, Scenes, Root + "/VehicleData", Root + "/MapProfiles", Root + "/Materials", Prefabs };
            for (int i = 0; i < paths.Length; i++) if (!Directory.Exists(paths[i])) Directory.CreateDirectory(paths[i]);
        }
    }
}
#endif
