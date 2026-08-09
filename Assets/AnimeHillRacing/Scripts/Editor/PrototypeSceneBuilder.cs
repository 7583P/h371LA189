#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
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
        private const string FormulaWheelPath = "Assets/Art/Vehicles/Formula/Wheels/Formula_Wheel_01.png";
        private const string FormulaReferencePath = "Assets/Art/Vehicles/Formula/Formula_Reference.png";
        private const string RallyBodyPath = "Assets/Art/Vehicles/Rally/Vehicle/Rally_Skin_Default.png";
        private const string RallyNoRoofPath = "Assets/Art/Vehicles/Rally/Vehicle/Rally_No_Roof_01.png";
        private const string RallyRoofPath = "Assets/Art/Vehicles/Rally/Vehicle/Rally_Roof_01.png";
        private const string RallyWheelPath = "Assets/Art/Vehicles/Rally/Wheels/Rally_Wheel_01.png";
        private const string MainMenuBackgroundPath = "Assets/Art/Interface/Background_Interface.png";
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
            public Sprite formulaBody;
            public Sprite formulaWheel;
            public Sprite jeepBody;
            public Sprite jeepWheel01;
            public Sprite jeepWheel02;
            public Sprite rallyBody;
            public Sprite rallyNoRoof;
            public Sprite rallyRoof;
            public Sprite rallyWheel;
            public Sprite mainMenuBackground;
            public Sprite fuelCan;
            public Sprite coin01;
            public Sprite coin05;
            public DriverDefinition stickmanBlack;
            public EnvironmentDefinition countrysideEnvironment;
            public MapProfile countryside;
            public CoinRouteDefinition countrysideCoinRoute;
            public TerrainElevationProfile elevation;
            public RoadCurvatureProfile curvature;
            public RoadMapProfile countrysideRoad;
            public CircuitDefinition testCircuit;
            public UpgradeEconomyDefinition economy;
            public GameCatalog catalog;
            public Material countrysideMaterial;
            public Material countrysideEdgeMaterial;
            public Material nearMountains;
            public Material farMountains;
            public Material road;
            public Material grass;
            public Material shortcut;
            public PhysicsMaterial chassisPhysics;
            public PhysicsMaterial groundPhysics;
            public PhysicsMaterial tirePhysics;
        }

        [MenuItem("Anime Hill Racing/Build All Game Scenes")]
        public static void BuildAllGameScenes()
        {
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

        private static void BuildAllGameScenesInternal()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            ValidateCustomizationContract(assets);
            ValidateVehiclePhysicsContract(assets);
            ValidateSuspensionContract(assets);
            ValidateFuelSequenceContract(assets);
            ValidateCoinRouteContract(assets);
            ValidateVehiclePerformance(assets);
            ValidateAirControlContract(assets);
            BuildGarage(assets);
            BuildMainMenu(assets);
            BuildPreStart(assets);
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
            ValidateVehiclePerformance(assets);
            ValidateAirControlContract(assets);
            LogResolvedPhysicsProfiles(assets);
            AssetDatabase.SaveAssets();
            Debug.Log("COMPLETE VEHICLE PHYSICS AUDIT PASS");
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
                if (mainMenus.Length == 1) valid &= RequireReference(scene, mainMenus[0], "coinsText");
                Text[] labels = SceneComponents<Text>(scene);
                bool play = false, timeTrial = false, garage = false, tuning = false;
                for (int i = 0; i < labels.Length; i++)
                {
                    string value = labels[i] != null ? labels[i].text.Trim().ToUpperInvariant() : string.Empty;
                    play |= value == "PLAY" || value == "PLAY / ADVENTURE";
                    timeTrial |= value == "TIME TRIAL";
                    garage |= value == "GARAGE" || value == "GARAGE / CUSTOMIZE";
                    tuning |= value == "TUNING";
                }
                if (!play || !timeTrial || !garage || tuning)
                {
                    Debug.LogError("MainMenu navigation must expose PLAY / TIME TRIAL / GARAGE and no standalone TUNING.");
                    valid = false;
                }
                Image[] images = SceneComponents<Image>(scene);
                bool correctBackground = false;
                Sprite expectedBackground = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuBackgroundPath);
                for (int i = 0; i < images.Length; i++)
                    correctBackground |= images[i] != null && images[i].name == "Background Interface" &&
                                         images[i].sprite == expectedBackground;
                if (!correctBackground)
                {
                    Debug.LogError("MainMenu must use Assets/Art/Interface/Background_Interface.png.");
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
                        "tuningCosts", "tuningButtons", "tuningCoins" };
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
                    SerializedObject serialized = new SerializedObject(preStarts[0]);
                    SerializedProperty cards = serialized.FindProperty("modeCards");
                    SerializedProperty tuningPanel = serialized.FindProperty("tuningPanel");
                    if (cards == null || cards.arraySize != 2 ||
                        (tuningPanel != null && tuningPanel.objectReferenceValue != null))
                    {
                        Debug.LogError("PreStart must contain only the two Adventure mode cards and no editable tuning panel.");
                        valid = false;
                    }
                    InvokePrivate(preStarts[0], "Start");
                    SerializedProperty panels = serialized.FindProperty("stepPanels");
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
                    Text[] labels = SceneComponents<Text>(scene);
                    for (int i = 0; i < labels.Length; i++)
                        if (labels[i] != null && labels[i].text.Trim().ToUpperInvariant() == "TIME TRIAL")
                        {
                            Debug.LogError("TIME TRIAL must not appear inside the PLAY / PreStart flow.");
                            valid = false;
                        }
                }
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
                            "timeTrialHud", "results", "capture", "controls", "shifter", "roadCamera" };
                        for (int i = 0; i < fields.Length; i++)
                            valid &= RequireReference(scene, timeTrialFactories[0], fields[i]);
                    }
                    valid &= RequireCount(scene, SceneComponents<TimeTrialManager>(scene), 1, "TimeTrialManager");
                    valid &= RequireCount(scene, SceneComponents<TimeTrialHUD>(scene), 1, "TimeTrialHUD");
                    valid &= RequireCount(scene, SceneComponents<TimeTrialCircuitGenerator>(scene), 1, "TimeTrialCircuitGenerator");
                    valid &= RequireCount(scene, SceneComponents<ShifterUI>(scene), 1, "ShifterUI");
                    Text[] timeTrialLabels = SceneComponents<Text>(scene);
                    bool hasJump = false;
                    for (int i = 0; i < timeTrialLabels.Length; i++)
                        hasJump |= timeTrialLabels[i] != null &&
                                   timeTrialLabels[i].text.Contains("HORIZONTAL JUMP");
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
                        "capture", "controls", "shifter", "pickupPool" };
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
                        "speedText", "rpmText", "gearText", "transmissionText", "debugPanel", "debugText" };
                    for (int i = 0; i < fields.Length; i++) valid &= RequireReference(scene, huds[0], fields[i]);
                    Image fuelFill = new SerializedObject(huds[0]).FindProperty("fuelFill")?.objectReferenceValue as Image;
                    if (fuelFill == null || fuelFill.type != Image.Type.Filled || fuelFill.sprite == null)
                    {
                        Debug.LogError($"{scene.name}: Fuel Fill requires a sprite-backed Filled Image.");
                        valid = false;
                    }
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
            importer.spritePixelsPerUnit = 100f;
            importer.spritePivot = new Vector2(.5f, .5f);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = maximumSize;
            importer.SaveAndReimport();
        }

        private static VehicleDefinition.CustomizationOption Customization(string id, string name, Sprite sprite) =>
            new VehicleDefinition.CustomizationOption
            {
                optionId = id, displayName = name, sprite = sprite, unlockedByDefault = true
            };

        private static void ValidateCustomizationContract(BuiltAssets assets)
        {
            if (assets.catalog == null || assets.catalog.vehicles == null || assets.catalog.vehicles.Length != 3)
                throw new System.InvalidOperationException("Garage requires exactly Jeep, Formula and Rally in the catalog.");
            if (assets.jeep.skins == null || assets.jeep.skins.Length != 1 || assets.jeep.wheels == null ||
                assets.jeep.wheels.Length != 2 || assets.jeep.wheels[0].sprite == assets.jeep.wheels[1].sprite)
                throw new System.InvalidOperationException("Jeep requires one real skin and two distinct wheel alternatives.");
            if (assets.formula.skins == null || assets.formula.skins.Length != 1 || assets.formula.wheels == null ||
                assets.formula.wheels.Length != 1)
                throw new System.InvalidOperationException("Formula requires its real default skin and base wheel.");
            if (assets.rally.skins == null || assets.rally.skins.Length != 1 || assets.rally.wheels == null ||
                assets.rally.wheels.Length != 1 || assets.rally.skins[0].sprite != assets.rallyBody ||
                assets.rally.damagedBodySprite != assets.rallyNoRoof || assets.rally.detachableRoofSprite != assets.rallyRoof)
                throw new System.InvalidOperationException("Rally customization must expose only the intact skin and its base wheel.");
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
            Debug.Log($"Countryside Coin Route validation | snapshot={deterministicHashA}, " +
                      $"density={earlyDensity:0.0}->{middleDensity:0.0}->{lateDensity:0.0} coins/100m, " +
                      $"maxGroups={route.MaximumGroupsPer100Meters:0.00}/100m, values=1/5, deterministic=OK.");
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
            VehicleDefinition[] definitions = { assets.jeep, assets.formula, assets.rally };
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
                    physicalWheels &= suspension[wheel].GetComponent<Rigidbody>() != null &&
                                      suspension[wheel].GetComponent<SphereCollider>() != null &&
                                      suspension[wheel].GetComponent<ConfigurableJoint>() != null;
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
            }
            if (Mathf.Abs(assets.jeep.FrontWheelAnchor - 1.24f) > .001f ||
                assets.countryside.groundPhysicsMaterial != assets.groundPhysics ||
                assets.countrysideRoad.groundPhysicsMaterial != assets.groundPhysics)
                throw new System.InvalidOperationException("Jeep front axle or stable terrain physics material is not configured.");
            Debug.Log("Vehicle physics validation: rounded zero-bounce chassis, bounded suspension and visual/physical wheel anchors aligned.");
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
            VehicleDefinition[] definitions = { assets.jeep, assets.formula, assets.rally };
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
                float visualRadius = definition.wheelVisualDiameter * .5f;
                if (Mathf.Abs(visualRadius - definition.wheelRadius) > .005f)
                    failures.Add($"{definition.displayName} ALL LEVELS: wheel sprite radius {visualRadius:0.000}m " +
                                 $"!= physical radius {definition.wheelRadius:0.000}m");
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
            if (body.sprite != assets.formulaBody || rear.sprite != assets.formulaWheel || front.sprite != assets.formulaWheel)
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
            preview.Apply(assets.jeep, assets.jeep.skins[0], assets.jeep.wheels[0], assets.stickmanBlack);
            if (!formulaLeftThigh.enabled || !formulaRightThigh.enabled)
                throw new System.InvalidOperationException("Jeep driver pose must restore partially visible legs.");
            Debug.Log("Garage live composition validation: Jeep Wheel 02, Formula pose, and intact Rally/head-only pose are correct.");
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

        private static VehicleDefinition.TuneTargets CreateFormulaTuneTargets()
        {
            float[] speed = new float[21];
            float[] acceleration = new float[21];
            float[] downforce = new float[21];
            float[] fuel = new float[26];
            for (int i = 0; i < 21; i++)
            {
                speed[i] = 160f + i * 7f;
                acceleration[i] = 5f - i * .15f;
                downforce[i] = 1.5f + i * .6f;
            }
            for (int i = 0; i < fuel.Length; i++) fuel[i] = 20f + i;
            return new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = speed,
                accelerationZeroToHundredSeconds = acceleration,
                gripRatings = new float[] { .90f },
                airControlTorque = new float[] { 9000f },
                airborneDownforceAcceleration = downforce,
                fuelRunDurationSeconds = fuel
            };
        }

        private static VehicleDefinition.TuneTargets CreateRallyTuneTargets()
        {
            float[] speed = new float[20];
            float[] turbo = new float[20];
            float[] grip = new float[14];
            for (int i = 0; i < speed.Length; i++) speed[i] = 145f + i * (135f / 19f);
            for (int i = 0; i < turbo.Length; i++) turbo[i] = 5.8f - i * (3.3f / 19f);
            for (int i = 0; i < grip.Length; i++) grip[i] = .75f + i * (.20f / 13f);
            return new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = speed,
                accelerationZeroToHundredSeconds = turbo,
                gripRatings = grip,
                airControlTorque = new[] { 6500f },
                airborneDownforceAcceleration = new[] { 0f },
                fuelRunDurationSeconds = new[] { 50f }
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
            EditorUtility.SetDirty(assets.jeep);
            EditorUtility.SetDirty(assets.formula);
            EditorUtility.SetDirty(assets.rally);
        }

        private static GameObject SaveVehiclePrefab(GameObject root, string path)
        {
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
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
            Debug.Log($"Vehicle flat validation (fixed 0.02 s) | JEEP max={jeep.maximumSpeedKmh:0.0} km/h, " +
                      $"target={jeep.resolvedTopSpeedKmh:0.0}, grounded={jeep.groundedRatio:P0}, " +
                      $"0-100={jeep.zeroToHundredSeconds:0.00} s, fuel={jeep.fuelDurationSeconds:0.00} s, " +
                      $"reverse={jeep.reverseSpeedKmh:0.0} km/h | " +
                      $"FORMULA max={formula.maximumSpeedKmh:0.0} km/h, 0-100={formula.zeroToHundredSeconds:0.00} s, " +
                      $"target={formula.resolvedTopSpeedKmh:0.0}, grounded={formula.groundedRatio:P0}, " +
                       $"fuel={formula.fuelDurationSeconds:0.00} s, reverse={formula.reverseSpeedKmh:0.0} km/h | " +
                       $"RALLY max={rally.maximumSpeedKmh:0.0} km/h, 0-100={rally.zeroToHundredSeconds:0.00} s, " +
                       $"fuel={rally.fuelDurationSeconds:0.00} s, reverse={rally.reverseSpeedKmh:0.0} km/h.");
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
                    : vehicle.archetype == VehicleArchetype.Rally ? new[] { 200f, 280f } : new[] { 200f, 300f };
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
                             $"@ {result.speedKmh:0}km/h: " + string.Join("; ", conditions));
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
            VehicleDefinition[] definitions = { assets.jeep, assets.formula, assets.rally };
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
                          $"motor={stats.motorTorqueNm:0}Nm, grip={stats.gripRating01 * 100f:0}/100 ({stats.grip:0.00}), " +
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
            VehicleUpgradeType[] rallyTypes = { VehicleUpgradeType.Engine, VehicleUpgradeType.Suspension,
                VehicleUpgradeType.Grip, VehicleUpgradeType.Turbo };
            int[] rallyMax = { 20, 14, 14, 20 };
            if (assets.rally.GetTuneSlotCount() != 4 || assets.rally.accelerationMaxTunes != 0 ||
                assets.rally.fuelMaxTunes != 0) throw new System.InvalidOperationException("Rally must expose exactly four tunes.");
            for (int i = 0; i < rallyTypes.Length; i++)
                if (assets.rally.GetTuneTypeForSlot(i) != rallyTypes[i] ||
                    assets.rally.GetMaxTunes(rallyTypes[i]) != rallyMax[i] || fresh.GetTune(rallyTypes[i]) != 1)
                    throw new System.InvalidOperationException("Rally tuning order, maxima or fresh Level 1 defaults are invalid.");
            Debug.Log("Tuning validation: Jeep 14/15/12/12/15; Formula 20/20/20/20/25; Rally Speed20/Suspension14/Grip14/Turbo20; fresh=1.");
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
                FormulaWheelPath, FormulaReferencePath, RallyBodyPath, RallyNoRoofPath, RallyRoofPath,
                RallyWheelPath, MainMenuBackgroundPath, DriverPreviewPath, DriverHeadPath, DriverTorsoPath,
                DriverUpperArmPath, DriverForearmPath, DriverThighPath, DriverLowerLegPath,
                CountrysideSkyPath, CountrysideMountainsPath, FuelCanPath, Coin01Path, Coin05Path };
            for (int i = 0; i < spritePaths.Length; i++)
                ConfigureFormulaSprite(spritePaths[i], spritePaths[i] == CountrysideSkyPath ||
                    spritePaths[i] == CountrysideMountainsPath || spritePaths[i] == MainMenuBackgroundPath ? 4096 :
                    spritePaths[i] == FuelCanPath || spritePaths[i] == Coin01Path || spritePaths[i] == Coin05Path ? 512 : 2048);
            assets.jeepBody = AssetDatabase.LoadAssetAtPath<Sprite>(JeepBodyPath);
            assets.jeepWheel01 = AssetDatabase.LoadAssetAtPath<Sprite>(JeepWheel01Path);
            assets.jeepWheel02 = AssetDatabase.LoadAssetAtPath<Sprite>(JeepWheel02Path);
            assets.formulaBody = AssetDatabase.LoadAssetAtPath<Sprite>(FormulaBodyPath);
            assets.formulaWheel = AssetDatabase.LoadAssetAtPath<Sprite>(FormulaWheelPath);
            assets.rallyBody = AssetDatabase.LoadAssetAtPath<Sprite>(RallyBodyPath);
            assets.rallyNoRoof = AssetDatabase.LoadAssetAtPath<Sprite>(RallyNoRoofPath);
            assets.rallyRoof = AssetDatabase.LoadAssetAtPath<Sprite>(RallyRoofPath);
            assets.rallyWheel = AssetDatabase.LoadAssetAtPath<Sprite>(RallyWheelPath);
            assets.mainMenuBackground = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuBackgroundPath);
            assets.fuelCan = AssetDatabase.LoadAssetAtPath<Sprite>(FuelCanPath);
            assets.coin01 = AssetDatabase.LoadAssetAtPath<Sprite>(Coin01Path);
            assets.coin05 = AssetDatabase.LoadAssetAtPath<Sprite>(Coin05Path);
            assets.countrysideMaterial = UnlitMaterialAsset("CountrysideGround", new Color(0.09f, 0.34f, 0.15f));
            assets.countrysideEdgeMaterial = UnlitMaterialAsset("CountrysideEdge", new Color(0.42f, 0.86f, 0.28f));
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
                gripRatings = new float[] { .68f, .70f, .72f, .74f, .76f, .78f, .80f, .82f, .84f, .85f, .86f, .87f, .88f },
                airControlTorque = new float[] { 3200f }, airborneDownforceAcceleration = new float[] { 0f },
                fuelRunDurationSeconds = new float[] { 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 }
            };
            assets.jeep.suspension.minTravel = .44f; assets.jeep.suspension.maxTravel = .44f;
            assets.jeep.suspension.restLength = .52f;
            assets.jeep.suspension.compressionTravelRatio = .90f;
            assets.jeep.suspension.targetSag01 = .30f;
            assets.jeep.suspension.stiffnessTuneScale = new Vector2(.94f, 1.06f);
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
            assets.formula.suspensionMaxTunes = 20; assets.formula.gripMaxTunes = 0;
            assets.formula.airControlMaxTunes = 0; assets.formula.downforceMaxTunes = 20; assets.formula.fuelMaxTunes = 25;
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
            assets.formula.suspension.restLength = .38f; assets.formula.suspension.minTravel = .25f;
            assets.formula.suspension.maxTravel = .25f; assets.formula.suspension.compressionTravelRatio = .90f;
            assets.formula.suspension.targetSag01 = .29f;
            assets.formula.suspension.stiffnessTuneScale = new Vector2(.97f, 1.10f);
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
            assets.formula.wheels = new[] { Customization("formula-wheel-01", "FORMULA WHEEL 01", assets.formulaWheel) };
            assets.formula.visualWidth = 4.80f;
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
            assets.rally.fuelMaxTunes = 0; assets.rally.turboMaxTunes = 20;
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
            assets.rally.suspension.restLength = .56f; assets.rally.suspension.minTravel = .40f;
            assets.rally.suspension.maxTravel = .40f; assets.rally.suspension.compressionTravelRatio = .89f;
            assets.rally.suspension.targetSag01 = .30f;
            assets.rally.suspension.stiffnessTuneScale = new Vector2(.94f, 1.06f);
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
            assets.rally.bodyVisualOffset = new Vector2(0f, .16f);
            assets.rally.rearWheelVisualPosition = new Vector2(-1.32f, -.49f);
            assets.rally.frontWheelVisualPosition = new Vector2(1.34f, -.49f);
            assets.rally.wheelVisualDiameter = 1.02f;
            assets.rally.driverPose = CreateRallyDriverPose();
            EditorUtility.SetDirty(assets.rally);

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
            assets.catalog.maps = new MapDefinition[] { assets.countryside, assets.countrysideRoad, assets.testCircuit };
            assets.catalog.vehicles = new[] { assets.jeep, assets.formula, assets.rally };
            assets.catalog.drivers = new[] { assets.stickmanBlack };
            assets.catalog.upgradeEconomy = assets.economy;
            EditorUtility.SetDirty(assets.catalog);
            return assets;
        }

        private static void BuildMainMenu(BuiltAssets assets)
        {
            Scene scene = NewScene();
            Color background = new Color(0.015f, 0.025f, 0.08f);
            CreateMenuCamera(background);
            Canvas canvas = CreateCanvas(Color.clear, false);
            Image backdrop = CreateImage("Background Interface", canvas.transform, Color.white, Vector2.zero,
                Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>();
            backdrop.sprite = assets.mainMenuBackground; backdrop.preserveAspect = false; backdrop.raycastTarget = false;
            CreateImage("Interface Dark Overlay", canvas.transform, new Color(.01f, .02f, .08f, .42f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>().raycastTarget = false;
            Transform safe = CreateSafeArea(canvas.transform);
            MainMenuUI menu = new GameObject("Main Menu Controller").AddComponent<MainMenuUI>();
            Text title = CreateText("Title", safe, "CLIMB ANIME", 82, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(900f, 130f));
            title.fontStyle = FontStyle.Bold;
            GameObject coinCard = CreateHudCard("Total Coins Card", safe, Vector2.one,
                new Vector2(-175f, -52f), new Vector2(300f, 72f));
            CreateSpriteImage("Coin Icon", coinCard.transform, assets.coin01, new Vector2(.5f, .5f),
                new Vector2(-112f, 0f), new Vector2(52f, 52f));
            Text coins = CreateText("Total Coins", coinCard.transform, "COINS\n0", 27, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(-15f, 0f), new Vector2(-30f, 0f));
            menu.Assign(coins);
            Button play = CreateModernMenuButton("Play Adventure", safe, "PLAY / ADVENTURE", new Vector2(0f, 105f));
            Button threeD = CreateModernMenuButton("3D", safe, "3D", new Vector2(0f, -5f));
            Button timeTrial = CreateModernMenuButton("Time Trial", safe, "TIME TRIAL", new Vector2(0f, -115f));
            Button vehicles = CreateModernMenuButton("Garage", safe, "GARAGE / CUSTOMIZE", new Vector2(0f, -225f));
            Bind(play, menu.Play); Bind(threeD, menu.ThreeD); Bind(vehicles, menu.Garage); Bind(timeTrial, menu.TimeTrial);
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
            Transform rearWheel = CreateSpriteVisual("Preview Rear Wheel", previewRoot, assets.jeepWheel01,
                new Vector3(initial.rearWheelVisualPosition.x, initial.rearWheelVisualPosition.y, -.02f),
                initial.wheelVisualDiameter, 11);
            Transform frontWheel = CreateSpriteVisual("Preview Front Wheel", previewRoot, assets.jeepWheel01,
                new Vector3(initial.frontWheelVisualPosition.x, initial.frontWheelVisualPosition.y, -.02f),
                initial.wheelVisualDiameter, 11);
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
            systems.AddComponent<GameplayBootstrap>().Configure(GameModeType.Adventure2_5D, assets.countryside.mapId);
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
            ShifterUI shifter = CreateShifter(safe, null, input, GameModeType.Adventure2_5D);
            ResultCaptureService capture = CreateResultCapture(null, resultImage, true);
            systems.AddComponent<SelectedVehicleFactory>().ConfigureAdventure2D(assets.catalog, input, manager, hud,
                results, capture, controls, shifter, terrain, assets.countryside, pool, camera);
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
            ShifterUI shifter = CreateShifter(safe, null, input, GameModeType.Adventure3D);
            ResultCaptureService capture = CreateResultCapture(null, resultImage);
            systems.AddComponent<SelectedVehicleFactory>().ConfigureAdventure3D(assets.catalog, input, manager, hud,
                results, capture, controls, shifter, road, assets.countrysideRoad, pool, camera);
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
            ShifterUI shifter = CreateShifter(safe, null, input, GameModeType.TimeTrial3D);
            ResultCaptureService capture = CreateResultCapture(null, resultImage);
            systems.AddComponent<SelectedVehicleFactory>().ConfigureTimeTrial(assets.catalog, input, manager, circuit,
                assets.testCircuit, hud, results, capture, controls, shifter, camera, new Vector3(radius, 3f, -5f));
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, TimeTrialPath);
        }

        private static VehicleController CreateClimbVehicle(VehicleDefinition definition, VehicleInputController input,
            GameManager manager, PhysicsMaterial chassisMaterial = null, PhysicsMaterial tireMaterial = null)
        {
            bool formula = definition.archetype == VehicleArchetype.Formula;
            bool rally = definition.archetype == VehicleArchetype.Rally;
            GameObject root = new GameObject("VehiclePhysics - " + definition.displayName);
            root.transform.position = Vector3.zero;
            root.AddComponent<Rigidbody>();
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 0;
            collider.center = new Vector3(formula ? 0f : rally ? .01f : .06f,
                formula ? .24f : rally ? .33f : .24f, 0f);
            collider.radius = formula ? .29f : rally ? .38f : .40f;
            collider.height = formula ? 4.15f : rally ? 4.05f : 2.90f;
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
            GameObject root = new GameObject("VehiclePhysics - " + definition.displayName);
            root.transform.position = Vector3.zero;
            root.AddComponent<Rigidbody>();
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 2;
            collider.center = new Vector3(0f, formula ? .38f : rally ? .40f : .36f, 0f);
            collider.radius = formula ? .62f : rally ? .66f : .72f;
            collider.height = formula ? 4.20f : rally ? 4.35f : 3.05f;
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
                if (side == 1) bodyRenderers[side].flipX = true;
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
                Transform wheel = CreateSpriteVisual(front ? "Front Wheel" : "Rear Wheel", visual, wheelSprite,
                    new Vector3(xs[x], wheelPosition.y, wheelPosition.x), definition.wheelVisualDiameter, 11);
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
            Transform rearWheel = CreateSpriteVisual("Rear Wheel", visual, wheelSprite,
                new Vector3(definition.rearWheelVisualPosition.x, definition.rearWheelVisualPosition.y, -.04f),
                definition.wheelVisualDiameter, 11);
            Transform frontWheel = CreateSpriteVisual("Front Wheel", visual, wheelSprite,
                new Vector3(definition.frontWheelVisualPosition.x, definition.frontWheelVisualPosition.y, -.04f),
                definition.wheelVisualDiameter, 11);
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
            ConfigurableJoint joint = origin.AddComponent<ConfigurableJoint>();
            joint.connectedBody = root.GetComponent<Rigidbody>();
            joint.enableCollision = false;
            SuspensionController suspension = origin.AddComponent<SuspensionController>();
            suspension.AssignWheelVisual(wheelVisual);
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
            Text rpm = CreateText("RPM", root.transform, "RPM\n1100", 26, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 465f), new Vector2(260f, 70f));
            Text speed = CreateText("Speed", root.transform, "0\n<size=24>KM/H</size>", 52, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 385f), new Vector2(270f, 100f));
            Text gear = CreateText("Gear", root.transform, "GEAR 1", 29, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 310f), new Vector2(260f, 40f));
            Text transmission = CreateText("Transmission", root.transform, "AUTO", 24, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 270f), new Vector2(220f, 35f));
            Text status = CreateText("Status", root.transform, "", 34, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(500f, 60f));
            GameObject debugPanel = CreateImage("Debug Panel", root.transform, new Color(0f, 0f, 0f, 0.75f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-25f, 80f), new Vector2(540f, 570f));
            Text debug = CreateText("Debug Text", debugPanel.transform, "", 20, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(15f, -15f), new Vector2(-30f, -30f));
            GameplayHUD hud = root.AddComponent<GameplayHUD>();
            hud.Assign(distance, best, coins, coinIcon, nextFuel, nextFuelIcon, fuelText, fuelIcon, fuelFill,
                speed, rpm, gear, transmission, status,
                debugPanel, debug, input);
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
            Button brake = CreateButton("Brake", root.transform, "BRAKE", new Vector2(170f, 115f), new Vector2(300f, 170f), Vector2.zero);
            Button gas = CreateButton("Gas", root.transform, "GAS", new Vector2(-170f, 115f), new Vector2(300f, 170f), new Vector2(1f, 0f));
            brake.gameObject.AddComponent<HoldButton>().Assign(input, HoldControl.Brake);
            gas.gameObject.AddComponent<HoldButton>().Assign(input, HoldControl.Gas);
            return group;
        }

        private static CanvasGroup CreateRoadControls(Transform safe, VehicleInputController input)
        {
            GameObject root = UIObject("Gameplay Controls", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            Button left = CreateButton("Left", root.transform, "LEFT", new Vector2(155f, 290f), new Vector2(250f, 120f), Vector2.zero);
            Button right = CreateButton("Right", root.transform, "RIGHT", new Vector2(435f, 290f), new Vector2(250f, 120f), Vector2.zero);
            Button brake = CreateButton("Brake", root.transform, "BRAKE", new Vector2(155f, 115f), new Vector2(250f, 140f), Vector2.zero);
            Button gas = CreateButton("Gas", root.transform, "GAS", new Vector2(-155f, 115f), new Vector2(280f, 150f), new Vector2(1f, 0f));
            left.gameObject.AddComponent<HoldButton>().Assign(input, HoldControl.Left);
            right.gameObject.AddComponent<HoldButton>().Assign(input, HoldControl.Right);
            brake.gameObject.AddComponent<HoldButton>().Assign(input, HoldControl.Brake);
            gas.gameObject.AddComponent<HoldButton>().Assign(input, HoldControl.Gas);
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
            Text rpm = CreateText("RPM", root.transform, "RPM\n1100", 25, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 465f), new Vector2(250f, 70f));
            Text speed = CreateText("Speed", root.transform, "0\n<size=24>KM/H</size>", 50, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 385f), new Vector2(260f, 100f));
            Text gear = CreateText("Gear", root.transform, "GEAR 1", 28, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 310f), new Vector2(230f, 40f));
            Text transmission = CreateText("Transmission", root.transform, "AUTO", 24, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 270f), new Vector2(200f, 35f));
            GameObject debugPanel = CreateImage("Debug Panel", root.transform, new Color(0f, 0f, 0f, 0.75f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-25f, 80f), new Vector2(500f, 340f));
            Text debug = CreateText("Debug Text", debugPanel.transform, "", 20, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(15f, -15f), new Vector2(-30f, -30f));
            TimeTrialHUD hud = root.AddComponent<TimeTrialHUD>();
            hud.Assign(timer, lap, best, speed, rpm, gear, transmission, debugPanel, debug, input);
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
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
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
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Unlit_" + name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color; EditorUtility.SetDirty(material); return material;
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
