using System;
using System.Collections.Generic;
using System.IO;
using AnimeHillRacing.CameraSystem;
using AnimeHillRacing.Core;
using AnimeHillRacing.EditorTools;
using AnimeHillRacing.Road;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    public static class BuggyMountainsInstaller
    {
        private const string SessionKey = "AHR.BuggyMountainsInstaller.v12";
        private const string Root = "Assets/AnimeHillRacing";
        private const string BuggyBodyPath = "Assets/Art/Vehicles/Buggy/Buggy_Body.png";
        private const string BuggyRedBodyPath = "Assets/Art/Vehicles/Buggy/Buggy_Body_Red.png";
        private const string BuggyBlackGoldBodyPath = "Assets/Art/Vehicles/Buggy/Buggy_Body_BlackGold.png";
        private const string BuggyWheelPath = "Assets/Art/Vehicles/Buggy/Buggy_Wheel.png";
        private const string BuggyAssetPath = Root + "/VehicleData/BuggyVehicle.asset";
        private const string Buggy2DPrefabPath = Root + "/Prefabs/Vehicles/Buggy_2D.prefab";
        private const string Buggy3DPrefabPath = Root + "/Prefabs/Vehicles/Buggy_3D.prefab";
        private const string MountainsAssetPath = Root + "/MapProfiles/Mountains.asset";
        private const string MountainsEnvironmentPath = Root + "/MapProfiles/MountainsEnvironment.asset";
        private const string MountainsArtRoot = "Assets/Art/Backgrounds/Mountains";
        private const string MountainsAnimeHorizonPath = MountainsArtRoot + "/Mountains_AnimeHorizon.png";
        private const string MountainsTerrainRoot = "Assets/Art/Terrain/Mountains";
        private const string MountainsGroundPath = MountainsTerrainRoot + "/Ground.png";

        [InitializeOnLoadMethod]
        private static void QueueAutomaticInstall()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetBool(SessionKey, false);
                    return;
                }
                Install();
            };
        }

        [MenuItem("Anime Hill Racing/Content/Install Buggy and Mountains")]
        public static void Install()
        {
            EnsureFolder(MountainsArtRoot);
            EnsureFolder(MountainsTerrainRoot);
            GenerateMountainsTextures();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureSprite(BuggyBodyPath, 2048, true);
            ConfigureSprite(BuggyRedBodyPath, 2048, true);
            ConfigureSprite(BuggyBlackGoldBodyPath, 2048, true);
            ConfigureSprite(BuggyWheelPath, 2048, true);
            ConfigureSprite(MountainsArtRoot + "/Mountains_Sky.png", 1024, false);
            ConfigureSprite(MountainsArtRoot + "/Mountains_Far.png", 1024, true);
            ConfigureSprite(MountainsArtRoot + "/Mountains_Mid.png", 1024, true);
            ConfigureSprite(MountainsArtRoot + "/Mountains_Near.png", 1024, true);
            ConfigureSprite(MountainsAnimeHorizonPath, 2048, false);
            ConfigureTiledTexture(MountainsTerrainRoot + "/Mountains_Rock.png");
            ConfigureAuthoredGroundTexture(MountainsGroundPath);

            Sprite body = AssetDatabase.LoadAssetAtPath<Sprite>(BuggyBodyPath);
            Sprite redBody = AssetDatabase.LoadAssetAtPath<Sprite>(BuggyRedBodyPath);
            Sprite blackGoldBody = AssetDatabase.LoadAssetAtPath<Sprite>(BuggyBlackGoldBodyPath);
            Sprite wheel = AssetDatabase.LoadAssetAtPath<Sprite>(BuggyWheelPath);
            if (body == null || redBody == null || blackGoldBody == null || wheel == null)
                throw new InvalidOperationException("Buggy source sprites could not be imported.");

            VehicleDefinition buggy = LoadOrCreate<VehicleDefinition>(BuggyAssetPath);
            ConfigureBuggy(buggy, body, redBody, blackGoldBody, wheel);
            BuildBuggyPrefabs(buggy);

            EnvironmentDefinition environment = LoadOrCreate<EnvironmentDefinition>(MountainsEnvironmentPath);
            ConfigureMountainsEnvironment(environment);
            MapProfile mountains = LoadOrCreate<MapProfile>(MountainsAssetPath);
            ConfigureMountainsProfile(mountains, environment);
            RegisterInCatalog(buggy, mountains);

            EditorUtility.SetDirty(buggy);
            EditorUtility.SetDirty(environment);
            EditorUtility.SetDirty(mountains);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AdventureBox2DGoldenInstaller.InstallGoldenBox2D();
            ValidateInstallation(buggy, mountains);
            AssetDatabase.SaveAssets();
            Debug.Log("BUGGY_MOUNTAINS_INSTALL_OK|vehicle=buggy-01|skins=3|paidSkinPrice=500|track=mountains|chunks=pooled|seed=40822");
        }

        private static void ConfigureBuggy(VehicleDefinition v, Sprite body, Sprite redBody,
            Sprite blackGoldBody, Sprite wheel)
        {
            v.vehicleId = "buggy-01";
            v.displayName = "BUGGY";
            v.archetype = VehicleArchetype.Buggy;
            v.unlockedByDefault = false;
            v.coinPrice = 30000;
            v.gemPrice = 0;
            v.allowsBlueprintProgression = true;
            v.engineMaxTunes = 20;
            v.accelerationMaxTunes = 15;
            v.suspensionMaxTunes = 15;
            v.gripMaxTunes = 15;
            v.fuelMaxTunes = 16;
            v.airControlMaxTunes = 0;
            v.downforceMaxTunes = 0;
            v.turboMaxTunes = 10;
            v.tuneTargets = new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = Values(128f, 220f, v.engineMaxTunes, .88f),
                accelerationZeroToHundredSeconds = Values(6.8f, 3.8f, v.accelerationMaxTunes, .82f),
                gripRatings = Values(600f, 950f, v.gripMaxTunes, 1.08f),
                airControlTorque = new[] { 3600f },
                airborneDownforceAcceleration = new[] { 0f },
                fuelRunDurationSeconds = Values(36f, 60f, v.fuelMaxTunes, 1f)
            };

            v.mass = 760f;
            v.wheelMass = 36f;
            v.wheelRadius = .53f;
            v.halfWheelbase = 1.80f;
            v.halfTrack = .88f;
            v.topSpeedMps = 220f / 3.6f;
            v.brakeForce = 15500f;
            v.reverseForce = 8000f;
            v.airControlTorque = 3600f;
            v.maxAirAngularVelocity = 2.2f;
            v.airAngularAcceleration = 13f;
            v.airAngularDamping = .55f;
            v.oneWheelAngularDamping = .13f;
            v.twoWheelAngularDamping = .42f;
            v.maximumPassiveAngularVelocity = 7.5f;
            v.collisionPitchInertiaMultiplier = 1.75f;
            v.maximumDepenetrationVelocity = 4f;
            v.chassisAirControlMultiplier = .10f;
            v.accelerationForceCalibration = 1f;
            v.centerOfMass = new Vector3(-.06f, -.43f, 0f);
            v.adaptiveAllWheelDrive = true;
            v.groundedWheelTorqueShare = .90f;
            v.maxClimbAssist = 9.5f;
            v.climbAssistStartAngle = 42f;
            v.climbAssistFullAngle = 80f;
            v.highSpeedDamping = .72f;

            v.engine.idleRpm = 1050f;
            v.engine.redlineRpm = 7200f;
            v.engine.limiterRpm = 7550f;
            v.engine.minTorqueNm = 340f;
            v.engine.maxTorqueNm = 720f;
            v.engine.drivetrainEfficiency = .90f;
            v.engine.torqueCurve = new AnimationCurve(new Keyframe(0f, .82f), new Keyframe(.28f, 1f),
                new Keyframe(.64f, .96f), new Keyframe(1f, .66f));
            v.engine.speedTorqueCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(.48f, 1f),
                new Keyframe(.76f, .82f), new Keyframe(.92f, .38f), new Keyframe(1.02f, 0f));
            v.transmission.forwardGearRatios = new[] { 5.80f, 3.72f, 2.78f, 2.20f, 1.82f, 1.54f, 1.32f };
            v.transmission.reverseRatio = 3.6f;
            v.transmission.finalDrive = 4.40f;
            v.transmission.autoShiftDuration = .30f;
            v.transmission.autoTorqueDuringShift = .20f;
            v.transmission.autoUpshiftRpm = 6650f;
            v.transmission.autoDownshiftRpm = 3100f;
            v.transmission.perfectShiftDuration = .15f;
            v.transmission.badShiftDuration = .38f;
            v.transmission.perfectTorqueDuringShift = .48f;
            v.transmission.badTorqueDuringShift = .08f;
            v.transmission.perfectRpmWindow = new Vector2(6100f, 6900f);

            v.suspension.restLength = .82f;
            v.suspension.minTravel = .48f;
            v.suspension.maxTravel = .70f;
            v.suspension.compressionTravelRatio = .78f;
            v.suspension.targetSag01 = .34f;
            v.suspension.stiffnessTuneScale = new Vector2(.75f, .96f);
            v.suspension.dampingRatio = new Vector2(.92f, 1.18f);
            v.suspension.compressionDampingMultiplier = .88f;
            v.suspension.reboundDampingMultiplier = 1.32f;
            v.suspension.compressionDampingTuneScale = new Vector2(.85f, 1.16f);
            v.suspension.reboundDampingTuneScale = new Vector2(.98f, 1.65f);
            v.suspension.reboundAccelerationTune = new Vector2(1.40f, .60f);
            v.suspension.wheelContactTolerance = new Vector2(.020f, .038f);
            v.suspension.landingDampingScale = new Vector2(1.15f, 1.65f);
            v.suspension.landingDampingDuration = .22f;
            v.suspension.bumpStopStart = .82f;
            v.suspension.progressiveSpringMultiplier = 3.4f;
            v.suspension.hardStopSpringMultiplier = 10f;
            v.suspension.maximumAcceleration = 34f;
            v.suspension.maximumReboundAcceleration = .90f;

            v.grip.minGrip = 4.8f;
            v.grip.maxGrip = 8.2f;
            v.grip.slipThreshold = .10f;
            v.grip.slidingGripMultiplier = .78f;
            v.grip.tractionRecoveryPerSecond = 7.5f;
            v.grip.rollingResistance = .019f;
            v.grip.aerodynamicDrag = .40f;
            v.grip.maximumPhysicsFriction = 8.3f;
            v.fuel.capacityUnits = 100f;
            v.fuel.minimumRunDurationSeconds = 36f;
            v.fuel.maximumRunDurationSeconds = 60f;
            v.fuel.referenceRunDurationSeconds = 60f;

            v.visualWidth = 4.85f;
            v.bodyVisualOffset = new Vector2(0f, .20f);
            if (!v.preserveWheelVisualPositions)
            {
                v.rearWheelVisualPosition = new Vector2(-1.80f, -.53f);
                v.frontWheelVisualPosition = new Vector2(1.80f, -.53f);
                v.rearWheelVisualDiameter = 0f;
                v.frontWheelVisualDiameter = 0f;
            }
            v.wheelVisualDiameter = 1.06f;
            v.flipBodyX = false;
            v.previewSprite = body;
            v.skins = new[]
            {
                Option("buggy-skin-default", "BROWN", body),
                Option("buggy-skin-red", "RACING RED", redBody, false, 500),
                Option("buggy-skin-black-gold", "BLACK & GOLD", blackGoldBody, false, 500)
            };
            v.wheels = new[] { Option("buggy-wheel-default", "MOUNTAIN GRIP", wheel) };

            VehicleDefinition hill = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(
                Root + "/VehicleData/HillAdventurerVehicle.asset");
            if (!v.preserveDriverPose || v.driverPose == null)
            {
                v.driverPose = hill != null && hill.driverPose != null
                    ? JsonUtility.FromJson<VehicleDefinition.DriverPose>(JsonUtility.ToJson(hill.driverPose))
                    : new VehicleDefinition.DriverPose();
                v.driverPose.rootPosition = new Vector2(-.33f, .29f);
                v.driverPose.rootScale = .80f;
            }
        }

        private static VehicleDefinition.CustomizationOption Option(string id, string name, Sprite sprite,
            bool unlockedByDefault = true, int gemPrice = 0)
        {
            Vector2 visibleSize = sprite != null
                ? VehicleVisualNormalizationEditor.MeasureAlphaVisibleSize(sprite)
                : Vector2.one;
            if (visibleSize.sqrMagnitude <= .0001f && sprite != null)
                visibleSize = sprite.bounds.size;
            return new VehicleDefinition.CustomizationOption
            {
                optionId = id, displayName = name, sprite = sprite, unlockedByDefault = unlockedByDefault,
                gemPrice = gemPrice, allowsBlueprintProgression = true,
                visibleSize = visibleSize,
                visualOffset = Vector2.zero
            };
        }

        private static float[] Values(float start, float end, int count, float exponent)
        {
            float[] values = new float[Mathf.Max(1, count)];
            for (int i = 0; i < values.Length; i++)
            {
                float t = values.Length <= 1 ? 1f : i / (float)(values.Length - 1);
                values[i] = Mathf.Lerp(start, end, Mathf.Pow(t, exponent));
            }
            values[values.Length - 1] = end;
            return values;
        }

        private static void BuildBuggyPrefabs(VehicleDefinition definition)
        {
            GameObject source2D = AssetDatabase.LoadAssetAtPath<GameObject>(
                Root + "/Prefabs/Vehicles/HillAdventurer_2D.prefab");
            GameObject source3D = AssetDatabase.LoadAssetAtPath<GameObject>(
                Root + "/Prefabs/Vehicles/HillAdventurer_3D.prefab");
            if (source2D == null || source3D == null)
                throw new InvalidOperationException("Hill Adventurer source prefabs are required to preserve the proven vehicle hierarchy.");

            definition.gameplayPrefab2D = CloneBuggy2D(source2D, definition);
            definition.gameplayPrefab3D = CloneBuggy3D(source3D, definition);
        }

        private static GameObject CloneBuggy2D(GameObject source, VehicleDefinition definition)
        {
            GameObject root = UnityEngine.Object.Instantiate(source);
            root.name = "Buggy_2D";
            try
            {
                VehiclePhysics2DController controller = root.GetComponent<VehiclePhysics2DController>();
                VehicleVisual2D visual = root.GetComponentInChildren<VehicleVisual2D>(true);
                Suspension2DController front = null;
                Suspension2DController rear = null;
                foreach (Suspension2DController point in root.GetComponentsInChildren<Suspension2DController>(true))
                {
                    if (point.name.IndexOf("front", StringComparison.OrdinalIgnoreCase) >= 0) front = point;
                    else if (point.name.IndexOf("rear", StringComparison.OrdinalIgnoreCase) >= 0) rear = point;
                }
                if (controller == null || visual == null || front == null || rear == null)
                    throw new InvalidOperationException("The source 2D prefab is missing its Box2D vehicle hierarchy.");

                SerializedObject controllerData = new SerializedObject(controller);
                controllerData.FindProperty("definition").objectReferenceValue = definition;
                controllerData.FindProperty("frontSuspension").objectReferenceValue = front;
                controllerData.FindProperty("rearSuspension").objectReferenceValue = rear;
                controllerData.FindProperty("suspensionFrequency").floatValue = 3.2f;
                controllerData.FindProperty("suspensionDampingRatio").floatValue = 1.08f;
                controllerData.FindProperty("suspensionTravel").floatValue = .70f;
                controllerData.FindProperty("torqueRampSeconds").floatValue = .13f;
                controllerData.ApplyModifiedPropertiesWithoutUndo();

                SpriteRenderer bodyRenderer = visual.BodyRenderer;
                SpriteRenderer frontRenderer = visual.FrontWheelRenderer;
                SpriteRenderer rearRenderer = visual.RearWheelRenderer;
                ArticulatedDriver2D driver = visual.GetComponentInChildren<ArticulatedDriver2D>(true);
                DriverDefinition defaultDriver = AssetDatabase.LoadAssetAtPath<DriverDefinition>(
                    Root + "/VehicleData/StickmanBlack.asset");
                visual.Assign(controller, bodyRenderer.transform, frontRenderer.transform, rearRenderer.transform,
                    driver != null ? driver.transform : visual.transform, definition.wheelRadius);
                visual.AssignCustomization(definition, bodyRenderer, frontRenderer, rearRenderer, driver, defaultDriver);
                bodyRenderer.gameObject.name = "BUGGY Body";
                rearRenderer.transform.localPosition = definition.rearWheelVisualPosition;
                frontRenderer.transform.localPosition = definition.frontWheelVisualPosition;
                rear.transform.localPosition = definition.rearWheelVisualPosition;
                front.transform.localPosition = definition.frontWheelVisualPosition;

                CapsuleCollider2D chassis = root.GetComponent<CapsuleCollider2D>();
                if (chassis != null)
                {
                    chassis.direction = CapsuleDirection2D.Horizontal;
                    chassis.offset = new Vector2(0f, .17f);
                    chassis.size = new Vector2(3.58f, .63f);
                }
                Rigidbody2D rigidbody = root.GetComponent<Rigidbody2D>();
                if (rigidbody != null) rigidbody.mass = definition.mass;
                return PrefabUtility.SaveAsPrefabAsset(root, Buggy2DPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CloneBuggy3D(GameObject source, VehicleDefinition definition)
        {
            GameObject root = UnityEngine.Object.Instantiate(source);
            root.name = "Buggy_3D";
            try
            {
                RoadVehicleController controller = root.GetComponent<RoadVehicleController>();
                VehicleVisual3D visual = root.GetComponentInChildren<VehicleVisual3D>(true);
                if (controller != null) controller.Assign(definition, null);
                if (visual != null)
                {
                    SerializedObject data = new SerializedObject(visual);
                    SpriteRenderer[] bodies = ObjectArray<SpriteRenderer>(data.FindProperty("bodyRenderers"));
                    SpriteRenderer[] wheels = ObjectArray<SpriteRenderer>(data.FindProperty("wheelRenderers"));
                    for (int i = 0; i < bodies.Length; i++) if (bodies[i] != null) bodies[i].sprite = definition.previewSprite;
                    for (int i = 0; i < wheels.Length; i++) if (wheels[i] != null) wheels[i].sprite = definition.wheels[0].sprite;
                    visual.AssignCustomization(definition, bodies, wheels,
                        visual.GetComponentInChildren<ArticulatedDriver2D>(true),
                        AssetDatabase.LoadAssetAtPath<DriverDefinition>(Root + "/VehicleData/StickmanBlack.asset"));
                }
                CapsuleCollider chassis = root.GetComponent<CapsuleCollider>();
                if (chassis != null)
                {
                    chassis.direction = 2;
                    chassis.center = new Vector3(0f, .33f, 0f);
                    chassis.radius = .51f;
                    chassis.height = 3.62f;
                }
                return PrefabUtility.SaveAsPrefabAsset(root, Buggy3DPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static T[] ObjectArray<T>(SerializedProperty array) where T : UnityEngine.Object
        {
            if (array == null || !array.isArray) return Array.Empty<T>();
            T[] result = new T[array.arraySize];
            for (int i = 0; i < result.Length; i++)
                result[i] = array.GetArrayElementAtIndex(i).objectReferenceValue as T;
            return result;
        }

        private static void ConfigureMountainsEnvironment(EnvironmentDefinition environment)
        {
            environment.environmentId = "mountains";
            environment.sky = AssetDatabase.LoadAssetAtPath<Sprite>(MountainsArtRoot + "/Mountains_Sky.png");
            environment.farMountains = null;
            environment.mountains = AssetDatabase.LoadAssetAtPath<Sprite>(MountainsAnimeHorizonPath);
            environment.nearMountains = null;
            environment.overrideCameraBackground = true;
            environment.cameraBackgroundColor = new Color(.36f, .49f, .61f, 1f);
            environment.useRelativeParallaxSpeeds = true;
            environment.lockVerticalToCamera = true;
            environment.lockFarMountainsVerticalToCamera = true;
            environment.lockNearMountainsVerticalToCamera = true;
            environment.coverViewportVertically = true;
            environment.coverViewportVerticalOffset = 5f;
            environment.farMountainParallaxX = .08f;
            environment.mountainParallaxX = .12f;
            environment.nearMountainParallaxX = .36f;
            environment.farMountainParallaxY = .018f;
            environment.mountainParallaxY = .032f;
            environment.nearMountainParallaxY = .05f;
            environment.farMountainVisualHeight = 17.5f;
            environment.mountainVisualHeight = 24.5f;
            environment.nearMountainVisualHeight = 19.5f;
            environment.farMountainBaseY = -3.3f;
            environment.mountainBaseY = 0f;
            environment.nearMountainBaseY = -4.3f;
            environment.pooledFarMountainSegments = 5;
            environment.pooledMountainSegments = 5;
            environment.pooledNearMountainSegments = 5;
            environment.mirrorAlternatingFarMountainTiles = false;
            environment.mirrorAlternatingMountainTiles = true;
            environment.mirrorAlternatingNearMountainTiles = false;
            environment.farMountainSeamOverlap = .012f;
            environment.seamOverlap = .012f;
            environment.nearMountainSeamOverlap = .012f;
            environment.clouds = Array.Empty<Sprite>();
        }

        private static void ConfigureMountainsProfile(MapProfile profile, EnvironmentDefinition environment)
        {
            profile.mapId = "mountains";
            profile.displayName = "MOUNTAINS";
            profile.unlockedByDefault = false;
            profile.coinPrice = 30000;
            profile.supportedGameMode = GameModeType.Adventure2_5D;
            profile.seed = 40822;
            profile.introEndDistance = 1000f;
            profile.easyEndDistance = 2500f;
            profile.normalEndDistance = 5000f;
            profile.hardEndDistance = 10000f;
            profile.baseHeight = 0f;
            profile.amplitude = 250f;
            profile.maximumDifficulty = 1f;
            profile.introductoryAmplitude = .18f;
            profile.sectionLength = 125f;
            profile.maxSlopeAngle = 60f;
            profile.maxHeightDeltaPerSample = 12f;
            profile.maximumCrestSharpness = 18f;
            profile.smoothingPasses = 2;
            profile.minimumValleyWidth = 22f;
            profile.minimumLandingLength = 18f;
            profile.coinDensityPer100m = 8f;
            profile.initialFuelGap = 5f;
            profile.fuelGapIncrease = 5f;
            profile.fuelPlacementSearchRadius = 30f;
            profile.fuelPlacementSearchStep = 2f;
            profile.fuelMaximumPlacementSlope = 20f;
            profile.useMountainsTerrain = true;
            profile.useArenaTerrain = false;
            profile.useSpaceTerrain = false;
            profile.useCityHighwayTerrain = false;
            profile.supportsLiquidExposure = false;
            profile.mountains = new MountainsSettings
            {
                peakDifficultyDistance = 10000f,
                miniBumpMinimumWavelength = 10f,
                miniBumpMaximumWavelength = 24f,
                initialMiniBumpHeight = .12f,
                maximumMiniBumpHeight = 1f,
                easySlopeRange = new Vector2(12f, 24f),
                mediumSlopeRange = new Vector2(18f, 34f),
                hardSlopeRange = new Vector2(26f, 44f),
                extremeSlopeRange = new Vector2(34f, 54f),
                exceptionalSlope = 58f,
                exceptionalEveryCells = 9,
                crestWidthFraction = .06f,
                recoveryWidthFraction = .10f,
                valleyChance = .22f,
                crestPositionVariation = .22f,
                macroReliefHeight = 30f,
                macroReliefWavelength = 1150f,
                warmupMountainHeight = 4.5f,
                peakMountainHeight = 72f,
                initialFlatSectionChance = .42f,
                peakFlatSectionChance = .16f,
                peakExtremeBumpChance = .28f,
                peakExtremeBumpHeight = 4.8f,
                bumpyClimbChance = .38f,
                hardBumpChance = .18f,
                veryHardBumpChance = .72f,
                veryHardBumpCooldownDistance = 1200f,
                veryHardBumpHeight = 18f
            };
            profile.environment = environment;
            Texture2D rock = AssetDatabase.LoadAssetAtPath<Texture2D>(MountainsTerrainRoot + "/Mountains_Rock.png");
            Texture2D ground = AssetDatabase.LoadAssetAtPath<Texture2D>(MountainsGroundPath);
            if (ground == null) throw new InvalidOperationException("The authored Mountains Ground.png is missing.");
            TextureImporter groundImporter = AssetImporter.GetAtPath(MountainsGroundPath) as TextureImporter;
            if (groundImporter == null)
                throw new InvalidOperationException("The authored Mountains Ground importer is missing.");
            profile.terrainMaterial = LoadOrCreateMaterial(Root + "/Materials/MountainsRock.mat", rock,
                Color.white);
            profile.terrainEdgeMaterial = LoadOrCreateMaterial(Root + "/Materials/MountainsRockSurface.mat", ground,
                Color.white, true);
            if (profile.terrainEdgeMaterial.HasProperty("_HorizontalInset"))
                profile.terrainEdgeMaterial.SetFloat("_HorizontalInset", .004f);
            if (profile.terrainEdgeMaterial.HasProperty("_HorizontalUvRange"))
                profile.terrainEdgeMaterial.SetVector("_HorizontalUvRange", new Vector4(.1f, .9f, 0f, 0f));
            profile.useSeparateTerrainMaterials = true;
            profile.fitAuthoredTerrainTexture = true;
            profile.terrainTextureRepeatWidth = TerrainGenerator.MountainsGroundModuleWidth;
            profile.terrainWallUvRange = new Vector2(0f, 1f);
            profile.terrainSurfaceUvRange = new Vector2(.2f, .677f);
            profile.terrainSurfaceContactV = .54f;
            profile.terrainSurfaceContactVSamples = new float[0];
            // Start the deep rock wall at the painted wheel-contact line behind Ground. This is
            // the same overlap used by City/North Pole and keeps both layers visually connected.
            profile.terrainFillStartV = profile.terrainSurfaceContactV;
            profile.terrainMaximumVisualExtent = TerrainGenerator.MountainsGroundMaximumVisualExtent;
            profile.terrainMaximumVisualRiseAboveSurface =
                TerrainGenerator.MountainsGroundMaximumVisualRise;
            profile.terrainVisualDepth = 360f;
            profile.terrainEdgeWidth = .24f;
            profile.groundPhysicsMaterial = LoadOrCreatePhysicsMaterial(
                Root + "/Materials/MountainsGround.physicMaterial", .95f);
            profile.groundPhysicsMaterial2D = LoadOrCreatePhysicsMaterial2D(
                Root + "/Materials/MountainsGround2D.physicsMaterial2D", 2.8f);
        }

        private static void RegisterInCatalog(VehicleDefinition buggy, MapProfile mountains)
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(Root + "/VehicleData/GameCatalog.asset");
            if (catalog == null) throw new InvalidOperationException("GameCatalog.asset is missing.");
            var vehicles = new List<VehicleDefinition>(catalog.vehicles ?? Array.Empty<VehicleDefinition>());
            vehicles.RemoveAll(item => item == null || item.vehicleId == buggy.vehicleId);
            vehicles.Add(buggy);
            catalog.vehicles = vehicles.ToArray();
            var maps = new List<MapDefinition>(catalog.maps ?? Array.Empty<MapDefinition>());
            maps.RemoveAll(item => item == null || item.mapId == mountains.mapId);
            int roadIndex = maps.FindIndex(item => item != null && item.supportedGameMode != GameModeType.Adventure2_5D);
            if (roadIndex < 0) maps.Add(mountains); else maps.Insert(roadIndex, mountains);
            catalog.maps = maps.ToArray();
            catalog.SortPurchasableContent();
            EditorUtility.SetDirty(catalog);
        }

        private static void ValidateInstallation(VehicleDefinition buggy, MapProfile mountains)
        {
            if (buggy.skins == null || buggy.skins.Length != 3 ||
                buggy.skins[0] == null || !buggy.skins[0].unlockedByDefault || buggy.skins[0].gemPrice != 0 ||
                buggy.skins[1] == null || buggy.skins[1].unlockedByDefault || buggy.skins[1].gemPrice != 500 ||
                buggy.skins[2] == null || buggy.skins[2].unlockedByDefault || buggy.skins[2].gemPrice != 500 ||
                buggy.skins[0].sprite == null || buggy.skins[1].sprite == null || buggy.skins[2].sprite == null ||
                buggy.skins[0].sprite == buggy.skins[1].sprite || buggy.skins[1].sprite == buggy.skins[2].sprite ||
                buggy.skins[0].sprite == buggy.skins[2].sprite)
                throw new InvalidOperationException("Buggy skin catalog does not match the authored contract.");

            var max = new VehicleUpgradeState
            {
                engineTune = buggy.engineMaxTunes, accelerationTune = buggy.accelerationMaxTunes,
                suspensionTune = buggy.suspensionMaxTunes, gripTune = buggy.gripMaxTunes,
                fuelTune = buggy.fuelMaxTunes, turboTune = buggy.turboMaxTunes
            };
            ResolvedVehicleStats stats = buggy.Resolve(max);
            ResolvedVehicleStats baseStats = buggy.Resolve(new VehicleUpgradeState());
            if (Mathf.Abs(stats.topSpeedMps * 3.6f - 220f) > .1f ||
                Mathf.Abs(stats.zeroToHundredTargetSeconds - 3.8f) > .01f ||
                Mathf.Abs(stats.totalRunFuelDuration - 60f) > .01f || stats.suspensionTravel < .69f ||
                stats.grip < 8.1f || stats.gripRating < 940 ||
                Mathf.Abs(stats.maximumClimbAngle - 80f) > .01f || stats.climbAssistAcceleration < 9.49f ||
                baseStats.suspensionTravel < .47f || baseStats.grip < 7.8f ||
                baseStats.maximumClimbAngle < 77.9f || baseStats.climbAssistAcceleration < 4.27f ||
                baseStats.lowSpeedTorqueMultiplier < 1.11f ||
                Mathf.Abs(buggy.visualWidth - 4.85f) > .001f ||
                Mathf.Abs(buggy.wheelRadius - .53f) > .001f ||
                Mathf.Abs(buggy.wheelVisualDiameter - 1.06f) > .001f)
                throw new InvalidOperationException("Buggy resolved statistics do not match the authored contract.");

            float[] maxima = new float[5];
            Mesh mesh = new Mesh();
            float endHeight = 0f;
            float endSlope = 0f;
            float maximumSeamError = 0f;
            bool invalidTerrainSample = false;
            const float chunkLength = 80f;
            const int samples = 96;
            int chunkCount = Mathf.CeilToInt(12000f / chunkLength);
            for (int chunk = 0; chunk < chunkCount; chunk++)
            {
                float expectedStartHeight = endHeight;
                TerrainGenerator.BuildMesh(mesh, mountains, chunk * chunkLength, chunkLength, samples, 10f,
                    chunk > 0, endHeight, endSlope, out float[] heights, out endHeight, out endSlope);
                if (chunk > 0)
                    maximumSeamError = Mathf.Max(maximumSeamError,
                        Mathf.Abs(heights[0] - expectedStartHeight));
                float dx = chunkLength / samples;
                for (int i = 1; i < heights.Length; i++)
                {
                    if (float.IsNaN(heights[i]) || float.IsInfinity(heights[i]))
                        invalidTerrainSample = true;
                    float distance = chunk * chunkLength + i * dx;
                    float angle = Mathf.Abs(Mathf.Atan2(heights[i] - heights[i - 1], dx) * Mathf.Rad2Deg);
                    int band = distance < 1000f ? 0 : distance < 2500f ? 1 : distance < 5000f ? 2 :
                        distance < 10000f ? 3 : 4;
                    maxima[band] = Mathf.Max(maxima[band], angle);
                }
            }

            float[] teleportCentres = { 131000f, 262000f, 544000f };
            float[] teleportMaxima = new float[teleportCentres.Length];
            for (int window = 0; window < teleportCentres.Length; window++)
            {
                float start = teleportCentres[window] - 1000f;
                endHeight = 0f;
                endSlope = 0f;
                for (int chunk = 0; chunk < 25; chunk++)
                {
                    float chunkStart = start + chunk * chunkLength;
                    float expectedStartHeight = endHeight;
                    TerrainGenerator.BuildMesh(mesh, mountains, chunkStart, chunkLength, samples, 10f,
                        chunk > 0, endHeight, endSlope, out float[] heights, out endHeight, out endSlope);
                    if (chunk > 0)
                        maximumSeamError = Mathf.Max(maximumSeamError,
                            Mathf.Abs(heights[0] - expectedStartHeight));
                    float dx = chunkLength / samples;
                    for (int i = 1; i < heights.Length; i++)
                    {
                        if (float.IsNaN(heights[i]) || float.IsInfinity(heights[i]))
                            invalidTerrainSample = true;
                        float angle = Mathf.Abs(Mathf.Atan2(heights[i] - heights[i - 1], dx) * Mathf.Rad2Deg);
                        teleportMaxima[window] = Mathf.Max(teleportMaxima[window], angle);
                    }
                }
            }
            UnityEngine.Object.DestroyImmediate(mesh);
            if (maxima[0] < 7f || maxima[0] > 20f ||
                maxima[1] < 10f || maxima[1] > 28f ||
                maxima[2] < 16f || maxima[2] > 38f ||
                maxima[3] < 40f || maxima[3] > 60.1f ||
                maxima[4] < 30f || maxima[4] > 60.1f ||
                teleportMaxima[0] < 35f || teleportMaxima[0] > 60.1f ||
                teleportMaxima[1] < 35f || teleportMaxima[1] > 60.1f ||
                teleportMaxima[2] < 35f || teleportMaxima[2] > 60.1f ||
                invalidTerrainSample || maximumSeamError > .001f ||
                Mathf.Abs(mountains.introEndDistance - 1000f) > .01f ||
                Mathf.Abs(mountains.hardEndDistance - 10000f) > .01f ||
                mountains.mountains == null ||
                Mathf.Abs(mountains.mountains.peakDifficultyDistance - 10000f) > .01f ||
                Mathf.Abs(mountains.mountains.bumpyClimbChance - .38f) > .001f ||
                Mathf.Abs(mountains.mountains.hardBumpChance - .18f) > .001f ||
                mountains.mountains.veryHardBumpCooldownDistance < 1200f)
                throw new InvalidOperationException($"Mountains slope audit failed: {string.Join(",", maxima)}");
            ValidateMountainsBackdrop(mountains.environment);
            ValidateCatalogTrackOrder();
            Debug.Log($"BUGGY_STATS_OK|vmax={stats.topSpeedMps * 3.6f:F1}|zero100={stats.zeroToHundredTargetSeconds:F2}" +
                      $"|fuel={stats.totalRunFuelDuration:F1}|travel={stats.suspensionTravel:F2}|grip={stats.gripRating}|width={buggy.visualWidth:F2}");
            Debug.Log($"MOUNTAINS_SLOPE_AUDIT_OK|0-1000={maxima[0]:F1}|1000-2500={maxima[1]:F1}" +
                      $"|2500-5000={maxima[2]:F1}|5000-10000={maxima[3]:F1}|10000+={maxima[4]:F1}" +
                      $"|131K={teleportMaxima[0]:F1}|262K={teleportMaxima[1]:F1}|544K={teleportMaxima[2]:F1}" +
                      $"|seam={maximumSeamError:F5}");
        }

        private static void ValidateMountainsBackdrop(EnvironmentDefinition environment)
        {
            if (environment == null || environment.mountains == null || !environment.coverViewportVertically ||
                environment.coverViewportVerticalOffset < 4f)
                throw new InvalidOperationException("Mountains horizon is not configured to remain visible.");
            if (AssetDatabase.GetAssetPath(environment.mountains) != MountainsAnimeHorizonPath)
                throw new InvalidOperationException("Mountains is not using the generated anime horizon panorama.");
            Texture2D horizonTexture = environment.mountains.texture;
            if (horizonTexture == null || horizonTexture.width < 1500 ||
                horizonTexture.width / (float)Mathf.Max(1, horizonTexture.height) < 1.8f)
                throw new InvalidOperationException("Mountains anime horizon must be a wide production panorama.");

            GameObject root = new GameObject("Mountains Backdrop Validation");
            try
            {
                Camera camera = new GameObject("Validation Camera").AddComponent<Camera>();
                camera.transform.SetParent(root.transform, false);
                camera.orthographic = true;
                camera.orthographicSize = 11.5f;
                SpriteRenderer sky = new GameObject("Validation Sky").AddComponent<SpriteRenderer>();
                sky.transform.SetParent(root.transform, false);
                MountainBackground background = root.AddComponent<MountainBackground>();
                background.Assign(camera, environment, sky, Array.Empty<SpriteRenderer>());
                background.ApplyEnvironment(environment);
                int pooledMountains = 0;
                SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null && renderers[i].name.StartsWith("Mountain Segment"))
                        pooledMountains++;
                if (pooledMountains < environment.pooledMountainSegments)
                    throw new InvalidOperationException(
                        $"Mountains background pool has {pooledMountains} segments, expected {environment.pooledMountainSegments}.");
                Debug.Log($"MOUNTAINS_BACKGROUND_OK|asset={MountainsAnimeHorizonPath}|size={horizonTexture.width}x{horizonTexture.height}|parallax={environment.mountainParallaxX:F2}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ValidateCatalogTrackOrder()
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(Root + "/VehicleData/GameCatalog.asset");
            string[] expected =
            {
                "countryside", "city", "neon-city", "mountains", "rainyer", "space", "north-pole"
            };
            if (catalog == null || catalog.CountMaps(GameModeType.Adventure2_5D) != expected.Length)
                throw new InvalidOperationException("Adventure catalog does not contain all seven tracks.");
            for (int i = 0; i < expected.Length; i++)
                if (catalog.GetCompatibleMap(GameModeType.Adventure2_5D, i)?.mapId != expected[i])
                    throw new InvalidOperationException(
                        $"Adventure track {i} is not {expected[i]} in the selector catalog.");
            Debug.Log("ADVENTURE_TRACK_SELECTOR_OK|countryside|city|neon-city|mountains|rainyer|space|north-pole");
        }

        private static Material LoadOrCreateMaterial(string path, Texture texture, Color color,
            bool transparent = false)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = transparent
                ? Shader.Find("AnimeHillRacing/Transparent Texture") ?? Shader.Find("Sprites/Default")
                : Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null) material.shader = shader;
            material.mainTexture = texture;
            material.color = color;
            if (transparent)
            {
                if (material.HasProperty("_SeamlessHorizontalTiling"))
                    material.SetFloat("_SeamlessHorizontalTiling", 1f);
                if (material.HasProperty("_HorizontalInset")) material.SetFloat("_HorizontalInset", .012f);
                if (material.HasProperty("_HorizontalUvRange"))
                    material.SetVector("_HorizontalUvRange", new Vector4(0f, 1f, 0f, 0f));
                if (material.HasProperty("_DepthWrite")) material.SetFloat("_DepthWrite", 1f);
                material.renderQueue = 2451;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureAuthoredGroundTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        private static PhysicsMaterial LoadOrCreatePhysicsMaterial(string path, float friction)
        {
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material == null)
            {
                material = new PhysicsMaterial("MountainsGround");
                AssetDatabase.CreateAsset(material, path);
            }
            material.dynamicFriction = friction;
            material.staticFriction = friction;
            material.bounciness = 0f;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static PhysicsMaterial2D LoadOrCreatePhysicsMaterial2D(string path, float friction)
        {
            PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (material == null)
            {
                material = new PhysicsMaterial2D("MountainsGround2D");
                AssetDatabase.CreateAsset(material, path);
            }
            material.friction = friction;
            material.bounciness = 0f;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void GenerateMountainsTextures()
        {
            WriteSky(MountainsArtRoot + "/Mountains_Sky.png");
            WriteMountainLayer(MountainsArtRoot + "/Mountains_Far.png", 0,
                new Color32(126, 145, 159, 170));
            WriteMountainLayer(MountainsArtRoot + "/Mountains_Mid.png", 1,
                new Color32(82, 101, 116, 225));
            WriteMountainLayer(MountainsArtRoot + "/Mountains_Near.png", 2,
                new Color32(48, 57, 66, 255));
            // Keep the production rock/soil artwork. The procedural fallback is only for a missing checkout;
            // running the installer must never replace the authored terrain with the old flat grey noise.
            string rockPath = MountainsTerrainRoot + "/Mountains_Rock.png";
            if (!File.Exists(Absolute(rockPath))) WriteRockTexture(rockPath);
        }

        private static void WriteSky(string assetPath)
        {
            const int width = 1024;
            const int height = 512;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color top = new Color32(91, 120, 151, 255);
            Color middle = new Color32(169, 184, 190, 255);
            Color horizon = new Color32(238, 220, 165, 255);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                Color color = t < .48f ? Color.Lerp(horizon, middle, t / .48f) :
                    Color.Lerp(middle, top, (t - .48f) / .52f);
                for (int x = 0; x < width; x++) pixels[y * width + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(Absolute(assetPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void WriteMountainLayer(string assetPath, int layer, Color32 baseColor)
        {
            const int width = 1024;
            const int height = 512;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[width * height];
            float baseLine = Mathf.Lerp(.10f, .035f, layer / 2f);
            for (int x = 0; x < width; x++)
            {
                float phase = x / (float)(width - 1) * Mathf.PI * 2f;
                float ridge = baseLine + .20f + layer * .055f +
                              Mathf.Sin(phase * (2 + layer) + .65f) * (.07f + layer * .018f) +
                              Mathf.Sin(phase * (5 + layer) + 1.4f) * (.035f + layer * .012f) +
                              Mathf.Abs(Mathf.Sin(phase * (3 + layer) - .35f)) * (.12f + layer * .035f);
                int ridgePixel = Mathf.Clamp(Mathf.RoundToInt(ridge * height), 1, height - 1);
                for (int y = 0; y <= ridgePixel; y++)
                {
                    float altitude = y / (float)Mathf.Max(1, ridgePixel);
                    float facet = .92f + .08f * Mathf.Sin(phase * 11f + y * .027f);
                    Color color = (Color)baseColor * Mathf.Lerp(.78f, 1.08f, altitude) * facet;
                    color.a = baseColor.a / 255f;
                    pixels[y * width + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(Absolute(assetPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void WriteRockTexture(string assetPath)
        {
            const int width = 512;
            const int height = 256;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1) * Mathf.PI * 2f;
                float v = y / (float)(height - 1) * Mathf.PI * 2f;
                float grain = Mathf.Sin(u * 7f + Mathf.Sin(v * 3f)) * .045f +
                              Mathf.Sin(v * 5f - u * 2f) * .035f +
                              Mathf.Abs(Mathf.Sin(u * 2f + v * 2f)) * .025f;
                float topLight = Mathf.SmoothStep(0f, .12f, y / (float)height) * .05f;
                float value = Mathf.Clamp01(.44f + grain + topLight);
                pixels[y * width + x] = new Color(value * .94f, value * .97f, value, 1f);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(Absolute(assetPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void ConfigureSprite(string path, int maxSize, bool alpha)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = alpha;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }

        private static void ConfigureTiledTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Default;
            // Mirroring guarantees a continuous join for detailed natural artwork and doubles the visible
            // repeat period, avoiding a conspicuous tile edge under the procedural terrain.
            importer.wrapMode = TextureWrapMode.Mirror;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static string Absolute(string assetPath) =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath.Replace('/', Path.DirectorySeparatorChar));

        private static void EnsureFolder(string path)
        {
            string absolute = Absolute(path);
            if (!Directory.Exists(absolute)) Directory.CreateDirectory(absolute);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
