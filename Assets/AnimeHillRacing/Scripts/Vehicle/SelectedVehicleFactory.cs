using AnimeHillRacing.CameraSystem;
using AnimeHillRacing.Core;
using AnimeHillRacing.Pickups;
using AnimeHillRacing.Road;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.TimeTrial;
using AnimeHillRacing.UI;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    [DefaultExecutionOrder(-180)]
    public sealed class SelectedVehicleFactory : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private GameModeType mode;
        [SerializeField] private VehicleInputController input;
        [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private Vector3 spawnEulerAngles;

        [Header("Adventure")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameplayHUD gameplayHud;
        [SerializeField] private ResultsUI results;
        [SerializeField] private ResultCaptureService capture;
        [SerializeField] private CanvasGroup controls;
        [SerializeField] private ShifterUI shifter;
        [SerializeField] private TerrainManager terrain;
        [SerializeField] private MapProfile terrainProfile;
        [SerializeField] private RoadTrackGenerator road;
        [SerializeField] private RoadMapProfile roadProfile;
        [SerializeField] private PickupPool pickupPool;
        [SerializeField] private VehicleCamera sideCamera;
        [SerializeField] private RoadChaseCamera roadCamera;

        [Header("Time Trial")]
        [SerializeField] private TimeTrialManager timeTrialManager;
        [SerializeField] private TimeTrialCircuitGenerator circuitGenerator;
        [SerializeField] private CircuitDefinition circuit;
        [SerializeField] private TimeTrialHUD timeTrialHud;

        public VehicleRuntimeBase SpawnedVehicle { get; private set; }
        public VehicleDefinition ActiveDefinition { get; private set; }

        private void Awake()
        {
            ActiveDefinition = catalog != null ? catalog.FindVehicle(GameSession.SelectedVehicleId) : null;
            if (ActiveDefinition == null) { enabled = false; return; }
            if (mode == GameModeType.Adventure2_5D)
            {
                MapProfile selectedProfile = catalog?.FindMap(GameSession.SelectedMapId,
                    GameModeType.Adventure2_5D) as MapProfile;
                if (selectedProfile != null) terrainProfile = selectedProfile;
            }
            GameObject prefab = mode == GameModeType.Adventure2_5D
                ? ActiveDefinition.gameplayPrefab2D : ActiveDefinition.gameplayPrefab3D;
            if (prefab == null) { Debug.LogError($"{ActiveDefinition.displayName}: gameplay prefab missing."); enabled = false; return; }

            GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.Euler(spawnEulerAngles));
            instance.name = ActiveDefinition.displayName + " - Active";
            SpawnedVehicle = instance.GetComponent<VehicleRuntimeBase>();
            if (SpawnedVehicle == null) { Debug.LogError("Selected vehicle prefab has no VehicleRuntimeBase."); return; }
            string defaultDriverId = catalog.drivers != null && catalog.drivers.Length > 0 && catalog.drivers[0] != null
                ? catalog.drivers[0].driverId : string.Empty;
            DriverDefinition selectedDriver = catalog.FindDriver(SaveManager.GetSelectedDriverId(defaultDriverId));
            instance.GetComponentInChildren<VehicleVisual2D>(true)?.ApplyCustomization(selectedDriver);
            instance.GetComponentInChildren<VehicleVisual3D>(true)?.ApplyCustomization(selectedDriver);

            VehicleController climb = SpawnedVehicle as VehicleController;
            if (climb != null)
                climb.AssignRuntimeReferences(ActiveDefinition, input, instance.GetComponentsInChildren<SuspensionController>(true));
            RoadVehicleController roadVehicle = SpawnedVehicle as RoadVehicleController;
            if (roadVehicle != null) roadVehicle.Assign(ActiveDefinition, input);
            instance.GetComponent<CrashDetector>()?.AssignGameManager(gameManager,
                mode == GameModeType.Adventure2_5D ? terrainProfile : null);

            sideCamera?.AssignTarget(SpawnedVehicle);
            roadCamera?.Assign(SpawnedVehicle);
            capture?.SetTarget(SpawnedVehicle.transform);
            shifter?.SetVehicle(SpawnedVehicle);
            gameplayHud?.AssignPickupPool(pickupPool);
            gameManager?.ConfigureChestCatalog(catalog);
            timeTrialManager?.ConfigureChestCatalog(catalog);

            if (mode == GameModeType.Adventure2_5D)
            {
                terrain?.AssignRuntimeReferences(terrainProfile, SpawnedVehicle, pickupPool, gameManager);
                gameManager?.ConfigureMap(terrainProfile);
                FindFirstObjectByType<MountainBackground>()?.ApplyEnvironment(terrainProfile?.environment);
            }
            else if (mode == GameModeType.Adventure3D)
                road?.Assign(roadProfile, SpawnedVehicle, pickupPool);

            if (gameManager != null)
                gameManager.AssignRuntimeReferences(SpawnedVehicle, input, gameplayHud, results, capture, controls);
            if (timeTrialManager != null)
                timeTrialManager.Assign(circuit, SpawnedVehicle, input, circuitGenerator, timeTrialHud,
                    results, capture, controls);
        }

        public void ConfigureAdventure2D(GameCatalog gameCatalog, VehicleInputController targetInput,
            GameManager manager, GameplayHUD hud, ResultsUI resultsUi, ResultCaptureService captureService,
            CanvasGroup gameplayControls, ShifterUI shifterUi, TerrainManager terrainManager,
            MapProfile mapProfile, PickupPool pool, VehicleCamera camera)
        {
            catalog = gameCatalog; mode = GameModeType.Adventure2_5D; input = targetInput;
            gameManager = manager; gameplayHud = hud; results = resultsUi; capture = captureService;
            controls = gameplayControls; shifter = shifterUi; terrain = terrainManager;
            terrainProfile = mapProfile; pickupPool = pool; sideCamera = camera;
        }

        public void ConfigureAdventure3D(GameCatalog gameCatalog, VehicleInputController targetInput,
            GameManager manager, GameplayHUD hud, ResultsUI resultsUi, ResultCaptureService captureService,
            CanvasGroup gameplayControls, ShifterUI shifterUi, RoadTrackGenerator roadGenerator,
            RoadMapProfile mapProfile, PickupPool pool, RoadChaseCamera camera)
        {
            catalog = gameCatalog; mode = GameModeType.Adventure3D; input = targetInput;
            gameManager = manager; gameplayHud = hud; results = resultsUi; capture = captureService;
            controls = gameplayControls; shifter = shifterUi; road = roadGenerator;
            roadProfile = mapProfile; pickupPool = pool; roadCamera = camera;
        }

        public void ConfigureTimeTrial(GameCatalog gameCatalog, VehicleInputController targetInput,
            TimeTrialManager manager, TimeTrialCircuitGenerator generator, CircuitDefinition definition,
            TimeTrialHUD hud, ResultsUI resultsUi, ResultCaptureService captureService,
            CanvasGroup gameplayControls, ShifterUI shifterUi, RoadChaseCamera camera, Vector3 position)
        {
            catalog = gameCatalog; mode = GameModeType.TimeTrial3D; input = targetInput;
            timeTrialManager = manager; circuitGenerator = generator; circuit = definition;
            timeTrialHud = hud; results = resultsUi; capture = captureService; controls = gameplayControls;
            shifter = shifterUi; roadCamera = camera; spawnPosition = position;
        }
    }
}
