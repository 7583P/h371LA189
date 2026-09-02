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
            string vehicleId = !string.IsNullOrEmpty(GameSession.SelectedVehicleId)
                ? GameSession.SelectedVehicleId : SaveManager.SelectedVehicleId;
            ActiveDefinition = ResolveUnlockedVehicle(vehicleId);
            if (ActiveDefinition == null) { enabled = false; return; }
            GameSession.SelectedVehicleId = ActiveDefinition.vehicleId;
            if (mode == GameModeType.Adventure2_5D)
            {
                MapProfile selectedProfile = catalog?.FindMap(GameSession.SelectedMapId,
                    GameModeType.Adventure2_5D) as MapProfile;
                if (selectedProfile != null) terrainProfile = selectedProfile;
            }
            GameObject prefab = mode == GameModeType.Adventure2_5D
                ? ActiveDefinition.gameplayPrefab2D : ActiveDefinition.gameplayPrefab3D;
            if (prefab == null) { Debug.LogError($"{ActiveDefinition.displayName}: gameplay prefab missing."); enabled = false; return; }

            Vector3 resolvedSpawnPosition = spawnPosition;
            if (mode == GameModeType.Adventure2_5D && terrainProfile != null)
            {
                float surfaceHeight = TerrainGenerator.SampleHeight(terrainProfile, spawnPosition.x);
                resolvedSpawnPosition.y = Mathf.Max(spawnPosition.y, surfaceHeight + 2.8f);
            }
            GameObject instance = Instantiate(prefab, resolvedSpawnPosition, Quaternion.Euler(spawnEulerAngles));
            instance.name = ActiveDefinition.displayName + " - Active";
            SpawnedVehicle = instance.GetComponent<VehicleRuntimeBase>();
            if (SpawnedVehicle == null) { Debug.LogError("Selected vehicle prefab has no VehicleRuntimeBase."); return; }
            string defaultDriverId = catalog.drivers != null && catalog.drivers.Length > 0 && catalog.drivers[0] != null
                ? catalog.drivers[0].driverId : string.Empty;
            DriverDefinition selectedDriver = catalog.FindDriver(SaveManager.GetSelectedDriverId(defaultDriverId));
            instance.GetComponentInChildren<VehicleVisual2D>(true)?.ApplyCustomization(selectedDriver);
            instance.GetComponentInChildren<VehicleVisual3D>(true)?.ApplyCustomization(selectedDriver);
            if (mode == GameModeType.Adventure2_5D)
                LastHorizonVehicleAppearance.Apply(instance, terrainProfile);

            VehicleController climb = SpawnedVehicle as VehicleController;
            if (climb != null)
            {
                climb.AssignRuntimeReferences(ActiveDefinition, input, instance.GetComponentsInChildren<SuspensionController>(true));
                climb.SetSurfaceGripMultiplier(terrainProfile != null && terrainProfile.rainstorm != null &&
                                               terrainProfile.rainstorm.permanentRain
                    ? terrainProfile.rainstorm.surfaceGripMultiplier : 1f);
            }

            VehiclePhysics2DController vehicle2D = SpawnedVehicle as VehiclePhysics2DController;
            if (vehicle2D != null)
            {
                // Initialize() detaches the dynamic wheel bodies from the dynamic chassis so Box2D owns
                // their world transforms. Preserve the controller's serialized/runtime references first;
                // a hierarchy lookup is only a compatibility fallback for older prefabs.
                Suspension2DController front = vehicle2D.FrontSuspension;
                Suspension2DController rear = vehicle2D.RearSuspension;
                if (front == null || rear == null)
                {
                    foreach (var s in instance.GetComponentsInChildren<Suspension2DController>(true))
                    {
                        if (s.name.ToLower().Contains("front")) front = s;
                        else if (s.name.ToLower().Contains("rear")) rear = s;
                    }
                }
                vehicle2D.AssignRuntimeReferences(ActiveDefinition, input, front, rear);
                vehicle2D.SetTerrainProfile(terrainProfile);
                vehicle2D.SetSurfaceGripMultiplier(terrainProfile != null && terrainProfile.rainstorm != null &&
                                                   terrainProfile.rainstorm.permanentRain
                    ? terrainProfile.rainstorm.surfaceGripMultiplier : 1f);
            }

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
                RainstormController.ApplyForMap(terrainProfile, sideCamera != null
                    ? sideCamera.GetComponent<Camera>() : Camera.main);
            }
            else if (mode == GameModeType.Adventure3D)
                road?.Assign(roadProfile, SpawnedVehicle, pickupPool);

            if (gameManager != null)
                gameManager.AssignRuntimeReferences(SpawnedVehicle, input, gameplayHud, results, capture, controls);
            if (timeTrialManager != null)
                timeTrialManager.Assign(circuit, SpawnedVehicle, input, circuitGenerator, timeTrialHud,
                    results, capture, controls);
        }

        private VehicleDefinition ResolveUnlockedVehicle(string requestedVehicleId)
        {
            if (catalog?.vehicles == null) return null;

            for (int i = 0; i < catalog.vehicles.Length; i++)
            {
                VehicleDefinition candidate = catalog.vehicles[i];
                if (candidate == null || candidate.vehicleId != requestedVehicleId) continue;
                if (SaveManager.IsVehicleUnlocked(candidate.vehicleId, candidate.unlockedByDefault))
                    return candidate;
                break;
            }

            for (int i = 0; i < catalog.vehicles.Length; i++)
            {
                VehicleDefinition candidate = catalog.vehicles[i];
                if (candidate != null && SaveManager.IsVehicleUnlocked(candidate.vehicleId,
                        candidate.unlockedByDefault)) return candidate;
            }

            return null;
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
