using System.IO;
using AnimeHillRacing.Core;
using AnimeHillRacing.EditorTools;
using AnimeHillRacing.Road;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    public static class HillAdventurerInstaller
    {
        private const string Root = "Assets/AnimeHillRacing";
        private const string PrefabsRoot = Root + "/Prefabs/Vehicles";
        private const string VehicleDataRoot = Root + "/VehicleData";
        private const string HillSkinPath = "Assets/Art/Vehicles/Hill Adventurer 4x4/Skins/Default.png";
        private const string HillWheelPath = "Assets/Art/Vehicles/Hill Adventurer 4x4/Wheels/Default.png";
        private const string HillVehicleAssetPath = VehicleDataRoot + "/HillAdventurerVehicle.asset";
        private const string FormulaVehicleAssetPath = VehicleDataRoot + "/FormulaVehicle.asset";
        private const string CatalogAssetPath = VehicleDataRoot + "/GameCatalog.asset";
        private const string StickmanDriverPath = VehicleDataRoot + "/StickmanBlack.asset";
        private const string ChassisPhysicMaterialPath = Root + "/Materials/VehicleBodyLowBounce.physicMaterial";
        private const string TirePhysicMaterialPath = Root + "/Materials/VehicleTireArcade.physicMaterial";

        [MenuItem("Anime Hill Racing/Vehicles/Install Hill Adventurer 4x4 and Update Formula")]
        public static void InstallAndValidateAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before running installer.");
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureSprite(HillSkinPath);
            ConfigureSprite(HillWheelPath);

            PhysicsMaterial chassisMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(ChassisPhysicMaterialPath);
            PhysicsMaterial tireMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(TirePhysicMaterialPath);
            DriverDefinition defaultDriver = AssetDatabase.LoadAssetAtPath<DriverDefinition>(StickmanDriverPath);

            Sprite hillSkinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HillSkinPath);
            Sprite hillWheelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HillWheelPath);

            // 1. Setup Hill Adventurer 4x4 VehicleDefinition
            VehicleDefinition hillVehicle = LoadOrCreate<VehicleDefinition>(HillVehicleAssetPath);
            ConfigureHillAdventurerDefinition(hillVehicle, hillSkinSprite, hillWheelSprite);
            EditorUtility.SetDirty(hillVehicle);

            // 2. Create Hill Adventurer 2D & 3D Prefabs
            if (!Directory.Exists(PrefabsRoot)) Directory.CreateDirectory(PrefabsRoot);

            GameObject hill2DRoot = CreateClimbVehicle(hillVehicle, chassisMaterial, tireMaterial, defaultDriver);
            hillVehicle.gameplayPrefab2D = SaveVehiclePrefab(hill2DRoot, PrefabsRoot + "/HillAdventurer_2D.prefab");

            GameObject hill3DRoot = CreateRoadVehicle(hillVehicle, chassisMaterial, defaultDriver);
            hillVehicle.gameplayPrefab3D = SaveVehiclePrefab(hill3DRoot, PrefabsRoot + "/HillAdventurer_3D.prefab");

            EditorUtility.SetDirty(hillVehicle);

            // 3. Update Formula Definition & Prefabs
            VehicleDefinition formulaVehicle = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(FormulaVehicleAssetPath);
            if (formulaVehicle != null)
            {
                ConfigureFormulaDefinition(formulaVehicle);
                EditorUtility.SetDirty(formulaVehicle);

                GameObject formula2DRoot = CreateClimbVehicle(formulaVehicle, chassisMaterial, tireMaterial, defaultDriver);
                formulaVehicle.gameplayPrefab2D = SaveVehiclePrefab(formula2DRoot, PrefabsRoot + "/Formula_2D.prefab");

                GameObject formula3DRoot = CreateRoadVehicle(formulaVehicle, chassisMaterial, defaultDriver);
                formulaVehicle.gameplayPrefab3D = SaveVehiclePrefab(formula3DRoot, PrefabsRoot + "/Formula_3D.prefab");

                EditorUtility.SetDirty(formulaVehicle);
            }

            // 4. Register in GameCatalog
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogAssetPath);
            if (catalog != null)
            {
                bool containsHill = false;
                if (catalog.vehicles != null)
                {
                    for (int i = 0; i < catalog.vehicles.Length; i++)
                    {
                        if (catalog.vehicles[i] == hillVehicle)
                        {
                            containsHill = true;
                            break;
                        }
                    }
                }

                if (!containsHill)
                {
                    var list = new System.Collections.Generic.List<VehicleDefinition>(catalog.vehicles ?? new VehicleDefinition[0]);
                    list.Add(hillVehicle);
                    catalog.vehicles = list.ToArray();
                    EditorUtility.SetDirty(catalog);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AdventureBox2DGoldenInstaller.InstallGoldenBox2D();

            Debug.Log("<color=green><b>HILL ADVENTURER 4x4 & FORMULA RESCALING INSTALLED SUCCESSFULLY!</b></color>");
        }

        private static void ConfigureSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    changed = true;
                }
                if (importer.maxTextureSize < 2048)
                {
                    importer.maxTextureSize = 2048;
                    changed = true;
                }
                if (changed)
                {
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }
            }
        }

        private static void ConfigureHillAdventurerDefinition(VehicleDefinition v, Sprite bodySprite, Sprite wheelSprite)
        {
            v.vehicleId = "hill-adventurer-01";
            v.displayName = "HILL ADVENTURER 4x4";
            v.archetype = VehicleArchetype.HillAdventurer;
            v.unlockedByDefault = true;
            v.gemPrice = 0;
            v.allowsBlueprintProgression = true;

            // Tune Limits: Speed (20), Accel (14), Susp (14), Grip (14), Fuel (18)
            v.engineMaxTunes = 20;
            v.accelerationMaxTunes = 14;
            v.suspensionMaxTunes = 14;
            v.gripMaxTunes = 14;
            v.fuelMaxTunes = 18;
            v.airControlMaxTunes = 0;
            v.downforceMaxTunes = 0;
            v.turboMaxTunes = 0;

            v.tuneTargets = new VehicleDefinition.TuneTargets
            {
                // Speed: Lv1 to Lv20 (190 km/h)
                engineTopSpeedKmh = new float[]
                {
                    110f, 115f, 120f, 125f, 130f, 135f, 140f, 145f, 150f, 155f,
                    160f, 165f, 170f, 175f, 180f, 183f, 186f, 188f, 189f, 190f
                },
                // Acceleration: Lv1 to Lv14 (4.6s 0-100)
                accelerationZeroToHundredSeconds = new float[]
                {
                    8.8f, 8.4f, 8.0f, 7.6f, 7.2f, 6.8f, 6.4f, 6.0f, 5.6f, 5.3f,
                    5.0f, 4.8f, 4.7f, 4.6f
                },
                // Grip: Lv1 to Lv14 (620 / 1000)
                gripRatings = new float[]
                {
                    180f, 215f, 250f, 290f, 330f, 375f, 420f, 465f, 510f, 550f,
                    580f, 600f, 612f, 620f
                },
                airControlTorque = new float[] { 3400f },
                airborneDownforceAcceleration = new float[] { 0f },
                // Fuel: Lv1 to Lv18 (55s)
                fuelRunDurationSeconds = new float[]
                {
                    28f, 29f, 30f, 31f, 33f, 35f, 37f, 39f, 41f, 43f,
                    45f, 47f, 49f, 51f, 52f, 53f, 54f, 55f
                }
            };

            // Physical stats (Positioned between Jeep and Rally, heavy off-road AWD)
            v.mass = 1280f;
            v.wheelMass = 48f;
            v.wheelRadius = 0.55f;
            v.topSpeedMps = 190f / 3.6f;
            v.brakeForce = 15000f;
            v.reverseForce = 5200f;
            v.airControlTorque = 3400f;
            v.maxAirAngularVelocity = 2.0f;
            v.airAngularAcceleration = 12f;
            v.airAngularDamping = 0.40f;
            v.oneWheelAngularDamping = 0.09f;
            v.twoWheelAngularDamping = 0.34f;
            v.maximumPassiveAngularVelocity = 8f;
            v.accelerationForceCalibration = 0.38f;
            v.centerOfMass = new Vector3(0.05f, -0.10f, 0f);
            v.collisionPitchInertiaMultiplier = 1.25f;
            v.maximumDepenetrationVelocity = 5.0f;
            v.chassisAirControlMultiplier = 0.10f;
            v.downforceReferenceSpeedMps = 30f;
            v.maximumDownforceAcceleration = 0f;

            // Engine & Transmission
            v.engine.idleRpm = 1100f;
            v.engine.redlineRpm = 7000f;
            v.engine.limiterRpm = 7400f;
            v.engine.minTorqueNm = 240f;
            v.engine.maxTorqueNm = 520f;
            v.engine.drivetrainEfficiency = 0.84f;
            v.engine.torqueCurve = new AnimationCurve(
                new Keyframe(0f, 0.75f),
                new Keyframe(0.3f, 0.98f),
                new Keyframe(0.6f, 1f),
                new Keyframe(1f, 0.70f)
            );
            v.engine.speedTorqueCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.40f, 1f),
                new Keyframe(0.70f, 0.85f),
                new Keyframe(0.90f, 0.45f),
                new Keyframe(1.02f, 0f)
            );

            v.transmission.forwardGearRatios = new float[] { 4.50f, 3.30f, 2.60f, 2.15f, 1.85f, 1.62f, 1.40f };
            v.transmission.reverseRatio = 3.2f;
            v.transmission.finalDrive = 3.45f;
            v.transmission.autoShiftDuration = 0.35f;
            v.transmission.autoTorqueDuringShift = 0.15f;
            v.transmission.autoUpshiftRpm = 6400f;
            v.transmission.autoDownshiftRpm = 2700f;
            v.transmission.perfectShiftDuration = 0.16f;
            v.transmission.badShiftDuration = 0.40f;
            v.transmission.perfectTorqueDuringShift = 0.45f;
            v.transmission.badTorqueDuringShift = 0.05f;
            v.transmission.perfectRpmWindow = new Vector2(5900f, 6700f);

            // Suspension (Long travel off-road: 0.24m - 0.38m travel, high stability)
            v.suspension.minTravel = 0.24f;
            v.suspension.maxTravel = 0.38f;
            v.suspension.restLength = 0.62f;
            v.suspension.compressionTravelRatio = 0.90f;
            v.suspension.targetSag01 = 0.30f;
            v.suspension.stiffnessTuneScale = new Vector2(0.84f, 1.05f);
            v.suspension.dampingRatio = new Vector2(0.75f, 1.10f);
            v.suspension.compressionDampingMultiplier = 0.85f;
            v.suspension.reboundDampingMultiplier = 1.20f;
            v.suspension.compressionDampingTuneScale = new Vector2(0.76f, 1.10f);
            v.suspension.reboundDampingTuneScale = new Vector2(0.88f, 1.45f);
            v.suspension.reboundAccelerationTune = new Vector2(1.8f, 0.60f);
            v.suspension.wheelContactTolerance = new Vector2(0.010f, 0.024f);
            v.suspension.landingDampingScale = new Vector2(1.05f, 1.40f);
            v.suspension.landingDampingDuration = 0.16f;
            v.suspension.springMin = 32000f;
            v.suspension.springMax = 46000f;
            v.suspension.dampingMin = 4500f;
            v.suspension.dampingMax = 6500f;
            v.suspension.bumpStopStart = 0.70f;
            v.suspension.progressiveSpringMultiplier = 2.0f;
            v.suspension.hardStopSpringMultiplier = 7.5f;
            v.suspension.maximumAcceleration = 28f;
            v.suspension.maximumReboundAcceleration = 1.0f;

            // Grip & Fuel
            v.grip.minGrip = 0.14f;
            v.grip.maxGrip = 2.20f;
            v.grip.slipThreshold = 0.17f;
            v.grip.slidingGripMultiplier = 0.48f;
            v.grip.tractionRecoveryPerSecond = 4.5f;
            v.grip.rollingResistance = 0.016f;
            v.grip.aerodynamicDrag = 0.42f;

            v.fuel.capacityUnits = 100f;
            v.fuel.minimumRunDurationSeconds = 28f;
            v.fuel.maximumRunDurationSeconds = 55f;
            v.fuel.referenceRunDurationSeconds = 55f;

            // Visual Geometry based on Visible Content Alpha Bounds & Wheel Arches
            v.visualWidth = 6.20f;
            v.wheelVisualDiameter = 1.38f;
            v.rearWheelVisualDiameter = 0f;
            v.frontWheelVisualDiameter = 0f;
            v.flipBodyX = false;
            v.bodyVisualOffset = new Vector2(0f, 0.35f);
            v.rearWheelVisualPosition = new Vector2(-1.88f, -0.65f);
            v.frontWheelVisualPosition = new Vector2(2.10f, -0.65f);
            v.halfWheelbase = 1.99f;
            v.halfTrack = 0.82f;

            v.previewSprite = bodySprite;
            v.skins = new[]
            {
                new VehicleDefinition.CustomizationOption
                {
                    optionId = "hill-adventurer-skin-default",
                    displayName = "DEFAULT",
                    sprite = bodySprite,
                    unlockedByDefault = true,
                    gemPrice = 0,
                    allowsBlueprintProgression = true,
                    visibleSize = new Vector2(21.72f, 6.91f),
                    visualOffset = Vector2.zero
                }
            };
            v.wheels = new[]
            {
                new VehicleDefinition.CustomizationOption
                {
                    optionId = "hill-adventurer-wheel-default",
                    displayName = "DEFAULT WHEEL",
                    sprite = wheelSprite,
                    unlockedByDefault = true,
                    gemPrice = 0,
                    allowsBlueprintProgression = true,
                    visibleSize = new Vector2(7.45f, 7.57f),
                    visualOffset = Vector2.zero
                }
            };

            // Driver Pose in Pickup Cab. A pose authored with Driver Pose Editor is permanent.
            if (!v.preserveDriverPose || v.driverPose == null)
                v.driverPose = new VehicleDefinition.DriverPose
            {
                rootPosition = new Vector2(-0.25f, 0.32f),
                rootScale = 0.92f,
                torso = VehicleDefinition.DriverPartPose.Create(-0.01f, 0.22f, -8f, 0.029f, 0.031f, true, 3),
                head = VehicleDefinition.DriverPartPose.Create(-0.03f, 0.62f, -3f, 0.031f, 0.033f, true, 5),
                leftUpperArm = VehicleDefinition.DriverPartPose.Create(0.18f, 0.34f, -6f, 0.038f, 0.029f, true, 4),
                leftForearm = VehicleDefinition.DriverPartPose.Create(0.36f, 0.24f, 0f, 0.036f, 0.029f, true, 4),
                rightUpperArm = VehicleDefinition.DriverPartPose.Create(0.10f, 0.30f, 2f, 0.038f, 0.029f, true, 2),
                rightForearm = VehicleDefinition.DriverPartPose.Create(0.28f, 0.20f, 6f, 0.036f, 0.029f, true, 2),
                leftThigh = VehicleDefinition.DriverPartPose.Create(0.12f, -0.06f, 12f, 0.038f, 0.029f, true, 1),
                leftLowerLeg = VehicleDefinition.DriverPartPose.Create(0.26f, -0.32f, -14f, 0.036f, 0.029f, true, 1),
                rightThigh = VehicleDefinition.DriverPartPose.Create(0.06f, -0.08f, 16f, 0.038f, 0.029f, true, 0),
                rightLowerLeg = VehicleDefinition.DriverPartPose.Create(0.20f, -0.34f, -10f, 0.036f, 0.029f, true, 0),
                accessory = VehicleDefinition.DriverPartPose.Create(-0.03f, 0.62f, -3f, 1f, 1f, true, 6)
            };
        }

        private static void ConfigureFormulaDefinition(VehicleDefinition v)
        {
            v.visualWidth = 5.60f;
            v.bodyVisualOffset = new Vector2(0f, 0.08f);

            v.wheelVisualDiameter = 1.08f;
            v.rearWheelVisualDiameter = 0f;
            v.frontWheelVisualDiameter = 0f;

            // Recalibrate driver pose
            if (!v.preserveDriverPose && v.driverPose != null)
            {
                v.driverPose.rootPosition = new Vector2(0.16f, 0.22f);
                v.driverPose.rootScale = 0.95f;
            }
            v.rearWheelVisualPosition = new Vector2(-1.75f, -0.28f);
            v.frontWheelVisualPosition = new Vector2(1.75f, -0.28f);
            v.halfWheelbase = 1.75f;

            // Physics, mass, stats, tunes: ALL UNCHANGED
        }

        private static GameObject CreateClimbVehicle(VehicleDefinition definition, PhysicsMaterial chassisMaterial,
            PhysicsMaterial tireMaterial, DriverDefinition defaultDriver)
        {
            bool formula = definition.archetype == VehicleArchetype.Formula;
            bool rally = definition.archetype == VehicleArchetype.Rally;
            bool sportBike = definition.archetype == VehicleArchetype.SportBike;
            bool hill = definition.archetype == VehicleArchetype.HillAdventurer;

            GameObject root = new GameObject("VehiclePhysics - " + definition.displayName);
            root.transform.position = Vector3.zero;
            root.AddComponent<Rigidbody>();

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 0; // X axis
            collider.center = new Vector3(
                hill ? 0.08f : (formula ? 0f : (rally ? .01f : (sportBike ? -.03f : .06f))),
                hill ? 0.36f : (formula ? .29f : (rally ? .33f : (sportBike ? .18f : .24f))),
                0f);
            collider.radius = hill ? 0.44f : (formula ? .34f : (rally ? .38f : (sportBike ? .27f : .40f)));
            collider.height = hill ? 4.60f : (formula ? 3.65f : (rally ? 4.05f : (sportBike ? 2.95f : 2.90f)));
            collider.sharedMaterial = chassisMaterial;

            root.AddComponent<MovementPlaneConstraint>();
            root.AddComponent<FuelSystem>();
            root.AddComponent<EngineController>();
            root.AddComponent<TransmissionController>();

            VehicleController controller = root.AddComponent<VehicleController>();
            root.AddComponent<CrashDetector>();

            VehicleVisual2D visual2D = CreateVehicleVisual2D(root.transform, controller, definition, defaultDriver);

            SuspensionController[] points = new SuspensionController[2];
            points[0] = CreateClimbSuspension(root.transform, definition, definition.RearWheelAnchor,
                visual2D.RearWheelSprite, tireMaterial);
            points[1] = CreateClimbSuspension(root.transform, definition, definition.FrontWheelAnchor,
                visual2D.FrontWheelSprite, tireMaterial);

            controller.AssignRuntimeReferences(definition, null, points);
            return root;
        }

        private static GameObject CreateRoadVehicle(VehicleDefinition definition, PhysicsMaterial chassisMaterial,
            DriverDefinition defaultDriver)
        {
            bool formula = definition.archetype == VehicleArchetype.Formula;
            bool rally = definition.archetype == VehicleArchetype.Rally;
            bool sportBike = definition.archetype == VehicleArchetype.SportBike;
            bool hill = definition.archetype == VehicleArchetype.HillAdventurer;

            GameObject root = new GameObject("VehiclePhysics - " + definition.displayName);
            root.transform.position = Vector3.zero;
            root.AddComponent<Rigidbody>();

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 2; // Z axis
            collider.center = new Vector3(0f,
                hill ? 0.42f : (formula ? .38f : (rally ? .40f : (sportBike ? .30f : .36f))),
                0f);
            collider.radius = hill ? 0.72f : (formula ? .62f : (rally ? .66f : (sportBike ? .48f : .72f)));
            collider.height = hill ? 4.80f : (formula ? 4.20f : (rally ? 4.35f : (sportBike ? 3.05f : 3.05f)));
            collider.sharedMaterial = chassisMaterial;

            root.AddComponent<FuelSystem>();
            root.AddComponent<EngineController>();
            root.AddComponent<TransmissionController>();

            RoadVehicleController controller = root.AddComponent<RoadVehicleController>();
            root.AddComponent<CrashDetector>();
            controller.Assign(definition, null);

            CreateVehicleVisual3D(root.transform, controller, definition, defaultDriver);
            return root;
        }

        private static VehicleVisual2D CreateVehicleVisual2D(Transform root, VehicleRuntimeBase vehicle,
            VehicleDefinition definition, DriverDefinition defaultDriver)
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
                VehicleDefinition.WheelSourceDiameter(definition.wheels != null && definition.wheels.Length > 0 ? definition.wheels[0] : null));

            Transform frontWheel = CreateWheelVisual("Front Wheel", visual, wheelSprite,
                new Vector3(definition.frontWheelVisualPosition.x, definition.frontWheelVisualPosition.y, -.04f),
                definition.FrontWheelDiameter, 11,
                VehicleDefinition.WheelSourceDiameter(definition.wheels != null && definition.wheels.Length > 0 ? definition.wheels[0] : null));

            ArticulatedDriver2D articulated = CreateArticulatedDriver(visual, defaultDriver, definition,
                root.GetComponent<Rigidbody>(), true);

            VehicleVisual2D component = visual.gameObject.AddComponent<VehicleVisual2D>();
            component.Assign(vehicle, body, frontWheel, rearWheel, articulated.transform, definition.wheelRadius);
            component.AssignCustomization(definition, body.GetComponent<SpriteRenderer>(),
                frontWheel.GetComponent<SpriteRenderer>(), rearWheel.GetComponent<SpriteRenderer>(), articulated, defaultDriver);

            return component;
        }

        private static void CreateVehicleVisual3D(Transform root, RoadVehicleController controller,
            VehicleDefinition definition, DriverDefinition defaultDriver)
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
            for (int z = 0; z < 2; z++)
            {
                for (int x = 0; x < 2; x++)
                {
                    bool front = z == 1;
                    Vector2 wheelPosition = front ? definition.frontWheelVisualPosition : definition.rearWheelVisualPosition;
                    Transform wheel = CreateWheelVisual(front ? "Front Wheel" : "Rear Wheel", visual, wheelSprite,
                        new Vector3(xs[x], wheelPosition.y, wheelPosition.x),
                        front ? definition.FrontWheelDiameter : definition.RearWheelDiameter, 11,
                        VehicleDefinition.WheelSourceDiameter(definition.wheels != null && definition.wheels.Length > 0 ? definition.wheels[0] : null));
                    wheel.localRotation = Quaternion.Euler(0f, x == 0 ? -90f : 90f, 0f);
                    wheelRenderers[wheelIndex] = wheel.GetComponent<SpriteRenderer>();
                    if (x == 1) wheelRenderers[wheelIndex].flipX = true;
                    wheels[wheelIndex] = wheel;
                    frontWheels[wheelIndex] = z == 1;
                    wheelIndex++;
                }
            }

            ArticulatedDriver2D driver = CreateArticulatedDriver(visual, defaultDriver, definition, root.GetComponent<Rigidbody>(), true);
            driver.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            VehicleVisual3D component = visual.gameObject.AddComponent<VehicleVisual3D>();
            component.Assign(controller, wheels, frontWheels, definition.wheelRadius, Vector3.forward);
            component.AssignCustomization(definition, bodyRenderers, wheelRenderers, driver, defaultDriver);
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
            gameObject.transform.localScale = Vector3.one *
                VehicleDefinition.SpriteWidthScale(sprite, targetWidth);
            return gameObject.transform;
        }

        private static Transform CreateWheelVisual(string name, Transform parent, Sprite sprite,
            Vector3 localPosition, float targetDiameter, int sortingOrder, float visibleDiameter = 0f)
        {
            Transform wheel = CreateSpriteVisual(name, parent, sprite, localPosition, targetDiameter, sortingOrder);
            wheel.localScale = Vector3.one *
                VehicleDefinition.WheelVisualScale(sprite, targetDiameter, visibleDiameter);
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
            SpriteRenderer accessory = Part("Accessory Anchor");

            GameObject impactObject = new GameObject("Head Terrain Reaction Trigger");
            impactObject.transform.SetParent(head.transform, false);
            impactObject.transform.localPosition = Vector3.zero;
            SphereCollider impactCollider = impactObject.AddComponent<SphereCollider>();
            impactCollider.isTrigger = true;
            impactCollider.radius = .27f;
            DriverHeadImpact impact = impactObject.AddComponent<DriverHeadImpact>();

            ArticulatedDriver2D articulated = root.gameObject.AddComponent<ArticulatedDriver2D>();
            articulated.AssignParts(torso, head, leftUpperArm, leftForearm, rightUpperArm, rightForearm,
                leftThigh, leftLowerLeg, rightThigh, rightLowerLeg, impact);
            articulated.AssignAccessory(accessory);
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
            wheelBody.solverIterations = 12;
            wheelBody.solverVelocityIterations = 8;
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

        private static GameObject SaveVehiclePrefab(GameObject root, string path)
        {
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
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
