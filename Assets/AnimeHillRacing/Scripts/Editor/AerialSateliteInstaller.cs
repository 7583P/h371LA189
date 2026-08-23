using System;
using System.Collections.Generic;
using AnimeHillRacing.Core;
using AnimeHillRacing.Road;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    public static class AerialSateliteInstaller
    {
        private const string SessionKey = "AHR.AerialSateliteInstaller.v19";
        private const string Root = "Assets/AnimeHillRacing";
        private const string ArtRoot = "Assets/Art/Vehicles/AerialSatelite";
        private const string DefinitionPath = Root + "/VehicleData/AerialSateliteVehicle.asset";
        private const string Prefab2DPath = Root + "/Prefabs/Vehicles/AerialSatelite_2D.prefab";
        private const string Prefab3DPath = Root + "/Prefabs/Vehicles/AerialSatelite_3D.prefab";
        private const string WhiteBodyPath = ArtRoot + "/AerialSatelite_Body_WhiteGold.png";
        private const string RedBodyPath = ArtRoot + "/AerialSatelite_Body_RedBlackGold.png";
        private const string WheelPath = ArtRoot + "/AerialSatelite_Wheel.png";
        private const string ThrusterPath = ArtRoot + "/AerialSatelite_Thruster.png";
        private const string FlamePath = ArtRoot + "/AerialSatelite_Flame.png";

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

        [MenuItem("Anime Hill Racing/Content/Install Aerial Satelite")]
        public static void Install()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureSprite(WhiteBodyPath, 4096);
            ConfigureSprite(RedBodyPath, 4096);
            ConfigureSprite(WheelPath, 1536);
            ConfigureSprite(ThrusterPath, 1536);
            ConfigureSprite(FlamePath, 1024);

            Sprite whiteBody = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteBodyPath);
            Sprite redBody = AssetDatabase.LoadAssetAtPath<Sprite>(RedBodyPath);
            Sprite wheel = AssetDatabase.LoadAssetAtPath<Sprite>(WheelPath);
            Sprite thruster = AssetDatabase.LoadAssetAtPath<Sprite>(ThrusterPath);
            Sprite flame = AssetDatabase.LoadAssetAtPath<Sprite>(FlamePath);
            if (whiteBody == null || redBody == null || wheel == null || thruster == null || flame == null)
                throw new InvalidOperationException("Aerial Satelite sprites could not be imported.");

            VehicleDefinition definition = LoadOrCreate<VehicleDefinition>(DefinitionPath);
            ConfigureDefinition(definition, whiteBody, redBody, wheel, thruster);
            BuildPrefabs(definition, thruster, flame);
            Register(definition);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Validate(definition);
            AerialSateliteVerifier.Run(true);
            Debug.Log("AERIAL_SATELITE_INSTALL_OK|vehicle=aerial-satelite-01|top=200kmh|zeroTo100=5s|fuel=60s|thrusterFuel=single2x-dual2.5x|groundDualPedal=true|oneWheelLock=true|maxUpgrade=120000");
        }

        private static void ConfigureDefinition(VehicleDefinition v, Sprite whiteBody, Sprite redBody,
            Sprite wheel, Sprite thruster)
        {
            v.vehicleId = "aerial-satelite-01";
            v.displayName = "AERIAL SATELITE";
            v.archetype = VehicleArchetype.AerialSatelite;
            v.unlockedByDefault = true;
            v.gemPrice = 0;
            v.allowsBlueprintProgression = true;
            v.engineMaxTunes = 16;
            v.accelerationMaxTunes = 14;
            v.suspensionMaxTunes = 14;
            v.gripMaxTunes = 14;
            v.fuelMaxTunes = 1;
            v.airControlMaxTunes = 0;
            v.downforceMaxTunes = 0;
            v.turboMaxTunes = 12;
            v.tuneTargets = new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = Values(165f, 200f, v.engineMaxTunes, .86f),
                accelerationZeroToHundredSeconds = Values(7.2f, 5f, v.accelerationMaxTunes, .84f),
                gripRatings = Values(240f, 650f, v.gripMaxTunes, 1.05f),
                airControlTorque = new[] { 2600f },
                airborneDownforceAcceleration = new[] { 0f },
                fuelRunDurationSeconds = new[] { 60f }
            };

            v.mass = 820f;
            v.wheelMass = 34f;
            v.wheelRadius = .59f;
            v.halfWheelbase = 2.28f;
            v.halfTrack = .82f;
            v.topSpeedMps = 200f / 3.6f;
            v.brakeForce = 14200f;
            v.reverseForce = 5200f;
            v.airControlTorque = 2600f;
            v.maxAirAngularVelocity = 2.1f;
            v.airAngularAcceleration = 10.5f;
            v.airAngularDamping = .42f;
            v.oneWheelAngularDamping = .12f;
            v.twoWheelAngularDamping = .38f;
            v.maximumPassiveAngularVelocity = 7.5f;
            v.collisionPitchInertiaMultiplier = 1.7f;
            v.maximumDepenetrationVelocity = 3.5f;
            v.chassisAirControlMultiplier = .08f;
            v.accelerationForceCalibration = 1f;
            v.centerOfMass = new Vector3(.05f, -.31f, 0f);
            v.adaptiveAllWheelDrive = true;
            v.groundedWheelTorqueShare = .78f;
            v.maxClimbAssist = .75f;
            v.climbAssistStartAngle = 60f;
            v.climbAssistFullAngle = 77f;
            v.highSpeedDamping = .62f;

            v.engine.idleRpm = 1050f;
            v.engine.redlineRpm = 7350f;
            v.engine.limiterRpm = 7700f;
            v.engine.minTorqueNm = 245f;
            v.engine.maxTorqueNm = 485f;
            v.engine.drivetrainEfficiency = .86f;
            v.engine.torqueCurve = new AnimationCurve(new Keyframe(0f, .80f), new Keyframe(.30f, 1f),
                new Keyframe(.66f, .95f), new Keyframe(1f, .64f));
            v.engine.speedTorqueCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(.50f, 1f),
                new Keyframe(.78f, .72f), new Keyframe(.94f, .25f), new Keyframe(1.02f, 0f));
            v.transmission.forwardGearRatios = new[] { 5.0f, 3.50f, 2.70f, 2.18f, 1.82f, 1.55f, 1.34f };
            v.transmission.reverseRatio = 3.4f;
            v.transmission.finalDrive = 3.82f;
            v.transmission.autoShiftDuration = .31f;
            v.transmission.autoTorqueDuringShift = .18f;
            v.transmission.autoUpshiftRpm = 6800f;
            v.transmission.autoDownshiftRpm = 3000f;

            v.suspension.restLength = .64f;
            v.suspension.minTravel = .27f;
            v.suspension.maxTravel = .42f;
            v.suspension.compressionTravelRatio = .83f;
            v.suspension.targetSag01 = .30f;
            v.suspension.stiffnessTuneScale = new Vector2(.86f, 1.04f);
            v.suspension.dampingRatio = new Vector2(.84f, 1.06f);
            v.suspension.compressionDampingMultiplier = .84f;
            v.suspension.reboundDampingMultiplier = 1.20f;
            v.suspension.compressionDampingTuneScale = new Vector2(.80f, 1.08f);
            v.suspension.reboundDampingTuneScale = new Vector2(.90f, 1.46f);
            v.suspension.reboundAccelerationTune = new Vector2(1.75f, .68f);
            v.suspension.wheelContactTolerance = new Vector2(.014f, .028f);
            v.suspension.landingDampingScale = new Vector2(1.02f, 1.42f);
            v.suspension.landingDampingDuration = .18f;
            v.suspension.bumpStopStart = .74f;
            v.suspension.progressiveSpringMultiplier = 2.65f;
            v.suspension.hardStopSpringMultiplier = 8.2f;
            v.suspension.maximumAcceleration = 31f;
            v.suspension.maximumReboundAcceleration = .90f;

            v.grip.minGrip = 1.25f;
            v.grip.maxGrip = 2.95f;
            v.grip.slipThreshold = .16f;
            v.grip.slidingGripMultiplier = .62f;
            v.grip.tractionRecoveryPerSecond = 5.0f;
            v.grip.rollingResistance = .018f;
            v.grip.aerodynamicDrag = .38f;
            v.grip.maximumPhysicsFriction = 3.55f;
            v.fuel.capacityUnits = 100f;
            v.fuel.minimumRunDurationSeconds = 60f;
            v.fuel.maximumRunDurationSeconds = 60f;
            v.fuel.referenceRunDurationSeconds = 60f;

            v.visualWidth = 6.15f;
            v.bodyVisualOffset = new Vector2(0f, .31f);
            if (!v.preserveWheelVisualPositions)
            {
                v.rearWheelVisualPosition = new Vector2(-2.28f, -.57f);
                v.frontWheelVisualPosition = new Vector2(2.28f, -.57f);
                v.rearWheelVisualDiameter = 0f;
                v.frontWheelVisualDiameter = 0f;
            }
            v.wheelVisualDiameter = 1.18f;
            v.flipBodyX = false;
            v.previewSprite = whiteBody;
            v.skins = new[]
            {
                Option("aerial-satelite-default", "WHITE & GOLD", whiteBody),
                Option("aerial-satelite-red-black-gold", "RED, BLACK & GOLD", redBody)
            };
            v.wheels = new[] { Option("aerial-satelite-wheel", "AERIAL GRIP", wheel) };
            v.underbodyAttachmentSprite = thruster;
            v.underbodyAttachmentPositions = new[]
            {
                new Vector2(-1.02f, -.38f), new Vector2(1.02f, -.38f)
            };
            v.underbodyAttachmentVisualWidth = .70f;

            VehicleDefinition source = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(
                Root + "/VehicleData/HillAdventurerVehicle.asset");
            if (!v.preserveDriverPose || v.driverPose == null)
            {
                v.driverPose = source != null && source.driverPose != null
                    ? JsonUtility.FromJson<VehicleDefinition.DriverPose>(JsonUtility.ToJson(source.driverPose))
                    : new VehicleDefinition.DriverPose();
                v.driverPose.rootPosition = new Vector2(.30f, .34f);
                v.driverPose.rootScale = .78f;
            }
        }

        private static void BuildPrefabs(VehicleDefinition definition, Sprite thruster, Sprite flame)
        {
            GameObject source2D = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Vehicles/HillAdventurer_2D.prefab");
            GameObject source3D = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Vehicles/HillAdventurer_3D.prefab");
            if (source2D == null || source3D == null)
                throw new InvalidOperationException("Hill Adventurer source prefabs are required.");
            definition.gameplayPrefab2D = Clone2D(source2D, definition, thruster, flame);
            definition.gameplayPrefab3D = Clone3D(source3D, definition);
        }

        private static GameObject Clone2D(GameObject source, VehicleDefinition definition, Sprite thruster,
            Sprite flame)
        {
            GameObject root = UnityEngine.Object.Instantiate(source);
            root.name = "AerialSatelite_2D";
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
                    throw new InvalidOperationException("Aerial source prefab is missing its Box2D hierarchy.");

                SerializedObject data = new SerializedObject(controller);
                data.FindProperty("definition").objectReferenceValue = definition;
                data.FindProperty("frontSuspension").objectReferenceValue = front;
                data.FindProperty("rearSuspension").objectReferenceValue = rear;
                data.FindProperty("suspensionFrequency").floatValue = 4.0f;
                data.FindProperty("suspensionDampingRatio").floatValue = .98f;
                data.FindProperty("suspensionTravel").floatValue = .42f;
                data.FindProperty("torqueRampSeconds").floatValue = .14f;
                data.ApplyModifiedPropertiesWithoutUndo();

                SpriteRenderer bodyRenderer = visual.BodyRenderer;
                SpriteRenderer frontRenderer = visual.FrontWheelRenderer;
                SpriteRenderer rearRenderer = visual.RearWheelRenderer;
                ArticulatedDriver2D driver = visual.GetComponentInChildren<ArticulatedDriver2D>(true);
                DriverDefinition defaultDriver = AssetDatabase.LoadAssetAtPath<DriverDefinition>(Root + "/VehicleData/StickmanBlack.asset");
                visual.Assign(controller, bodyRenderer.transform, frontRenderer.transform, rearRenderer.transform,
                    driver != null ? driver.transform : visual.transform, definition.wheelRadius);
                visual.AssignCustomization(definition, bodyRenderer, frontRenderer, rearRenderer, driver, defaultDriver);
                rearRenderer.transform.localPosition = definition.rearWheelVisualPosition;
                frontRenderer.transform.localPosition = definition.frontWheelVisualPosition;
                rear.transform.localPosition = definition.rearWheelVisualPosition;
                front.transform.localPosition = definition.frontWheelVisualPosition;

                CapsuleCollider2D chassis = root.GetComponent<CapsuleCollider2D>();
                if (chassis != null)
                {
                    chassis.direction = CapsuleDirection2D.Horizontal;
                    chassis.offset = new Vector2(0f, .15f);
                    chassis.size = new Vector2(4.45f, .68f);
                }
                Transform oldRig = root.transform.Find("Aerial Thruster Rig");
                if (oldRig != null) UnityEngine.Object.DestroyImmediate(oldRig.gameObject);
                Transform rig = new GameObject("Aerial Thruster Rig").transform;
                rig.SetParent(root.transform, false);
                CapsuleCollider2D cabin = rig.gameObject.AddComponent<CapsuleCollider2D>();
                cabin.direction = CapsuleDirection2D.Vertical;
                cabin.offset = new Vector2(.65f, .59f);
                cabin.size = new Vector2(.92f, 1.34f);

                Transform left = CreateThruster("Left Thruster Point", rig, new Vector2(-1.02f, -.38f),
                    thruster, flame,
                    out SpriteRenderer leftFlame);
                Transform right = CreateThruster("Right Thruster Point", rig, new Vector2(1.02f, -.38f),
                    thruster, flame,
                    out SpriteRenderer rightFlame);
                AerialThrusterVfx vfx = rig.gameObject.AddComponent<AerialThrusterVfx>();
                vfx.Assign(leftFlame, rightFlame);
                AerialThrusterController aerial = root.GetComponent<AerialThrusterController>() ??
                                                    root.AddComponent<AerialThrusterController>();
                aerial.AssignVisuals(left, right, vfx);
                Rigidbody2D rigidbody = root.GetComponent<Rigidbody2D>();
                if (rigidbody != null) rigidbody.mass = definition.mass;
                return PrefabUtility.SaveAsPrefabAsset(root, Prefab2DPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Transform CreateThruster(string name, Transform parent, Vector2 position,
            Sprite thruster, Sprite flame, out SpriteRenderer flameRenderer)
        {
            Transform point = new GameObject(name).transform;
            point.SetParent(parent, false);
            point.localPosition = position;

            Transform hardwareTransform = new GameObject("Compact Thruster Hardware").transform;
            hardwareTransform.SetParent(point, false);
            SpriteRenderer hardwareRenderer = hardwareTransform.gameObject.AddComponent<SpriteRenderer>();
            hardwareRenderer.sprite = thruster;
            hardwareRenderer.sortingOrder = 12;
            float hardwareScale = VehicleDefinition.SpriteWidthScale(thruster, .70f);
            hardwareTransform.localScale = Vector3.one * hardwareScale;

            Transform flameTransform = new GameObject("White-Cyan Flame").transform;
            flameTransform.SetParent(point, false);
            flameTransform.localPosition = new Vector3(0f, -.44f, 0f);
            flameRenderer = flameTransform.gameObject.AddComponent<SpriteRenderer>();
            flameRenderer.sprite = flame;
            flameRenderer.sortingOrder = 9;
            float flameScale = VehicleDefinition.SpriteWidthScale(flame, .28f);
            flameTransform.localScale = new Vector3(flameScale, flameScale * 2.0f, 1f);
            flameRenderer.enabled = false;
            return point;
        }

        private static GameObject Clone3D(GameObject source, VehicleDefinition definition)
        {
            GameObject root = UnityEngine.Object.Instantiate(source);
            root.name = "AerialSatelite_3D";
            try
            {
                RoadVehicleController controller = root.GetComponent<RoadVehicleController>();
                VehicleVisual3D visual = root.GetComponentInChildren<VehicleVisual3D>(true);
                controller?.Assign(definition, null);
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
                    chassis.center = new Vector3(0f, .40f, 0f);
                    chassis.radius = .60f;
                    chassis.height = 4.45f;
                }
                return PrefabUtility.SaveAsPrefabAsset(root, Prefab3DPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void Register(VehicleDefinition definition)
        {
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(Root + "/VehicleData/GameCatalog.asset");
            if (catalog == null) throw new InvalidOperationException("GameCatalog.asset is missing.");
            var vehicles = new List<VehicleDefinition>(catalog.vehicles ?? Array.Empty<VehicleDefinition>());
            vehicles.RemoveAll(item => item == null || item.vehicleId == definition.vehicleId);
            vehicles.Add(definition);
            catalog.vehicles = vehicles.ToArray();
            if (catalog.upgradeEconomy != null)
            {
                catalog.upgradeEconomy.aerialSateliteFirstUpgradeCost = 2000;
                catalog.upgradeEconomy.aerialSateliteFinalUpgradeCost = 120000;
                EditorUtility.SetDirty(catalog.upgradeEconomy);
            }
            EditorUtility.SetDirty(catalog);
        }

        private static void Validate(VehicleDefinition definition)
        {
            var maximum = new VehicleUpgradeState
            {
                engineTune = definition.engineMaxTunes,
                accelerationTune = definition.accelerationMaxTunes,
                suspensionTune = definition.suspensionMaxTunes,
                gripTune = definition.gripMaxTunes,
                fuelTune = definition.fuelMaxTunes,
                turboTune = definition.turboMaxTunes
            };
            ResolvedVehicleStats stats = definition.Resolve(maximum);
            if (Mathf.Abs(stats.topSpeedMps * 3.6f - 200f) > .1f ||
                Mathf.Abs(stats.zeroToHundredTargetSeconds - 5f) > .01f ||
                Mathf.Abs(stats.totalRunFuelDuration - 60f) > .01f ||
                stats.suspensionTravel >= .50f)
                throw new InvalidOperationException("Aerial Satelite resolved stats do not match the design contract.");
            GameObject prefab = definition.gameplayPrefab2D;
            if (prefab == null || prefab.GetComponent<AerialThrusterController>() == null ||
                prefab.GetComponentsInChildren<Suspension2DController>(true).Length != 2)
                throw new InvalidOperationException("Aerial Satelite 2D prefab is incomplete.");
            Transform thrusterRig = prefab.transform.Find("Aerial Thruster Rig");
            Transform leftThruster = thrusterRig != null ? thrusterRig.Find("Left Thruster Point") : null;
            Transform rightThruster = thrusterRig != null ? thrusterRig.Find("Right Thruster Point") : null;
            if (definition.flipBodyX || definition.skins == null || definition.skins.Length != 2 ||
                definition.previewSprite != definition.skins[0].sprite ||
                definition.skins[0].optionId != "aerial-satelite-default" ||
                definition.skins[1].optionId != "aerial-satelite-red-black-gold" ||
                leftThruster == null || rightThruster == null ||
                leftThruster.localPosition.x > -.95f || rightThruster.localPosition.x < .95f ||
                Mathf.Abs(leftThruster.localPosition.y + .38f) > .01f ||
                Mathf.Abs(rightThruster.localPosition.y + .38f) > .01f ||
                definition.underbodyAttachmentSprite == null ||
                definition.underbodyAttachmentPositions == null ||
                definition.underbodyAttachmentPositions.Length != 2 ||
                definition.underbodyAttachmentVisualWidth < .68f)
                throw new InvalidOperationException(
                    "Aerial Satelite must face right with two medium, separated thrusters between its wheels.");
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(Root + "/VehicleData/GameCatalog.asset");
            if (catalog?.upgradeEconomy == null ||
                catalog.upgradeEconomy.FinalCost(VehicleArchetype.AerialSatelite) != 120000)
                throw new InvalidOperationException("Aerial Satelite maximum upgrade price must be exactly 120000.");

            int catalogEntries = 0;
            int aerialIndex = -1;
            VehicleDefinition[] catalogVehicles = catalog.vehicles ?? Array.Empty<VehicleDefinition>();
            for (int i = 0; i < catalogVehicles.Length; i++)
            {
                VehicleDefinition vehicle = catalogVehicles[i];
                if (vehicle == null || vehicle.vehicleId != definition.vehicleId) continue;
                catalogEntries++;
                aerialIndex = i;
            }
            if (catalogEntries != 1)
                throw new InvalidOperationException(
                    "Aerial Satelite must appear exactly once in GameCatalog for Garage and PreStart selection.");

            Debug.Log($"AERIAL_CATALOG_OK|index={aerialIndex}|vehicles={catalogVehicles.Length}|garage=visible|prestart=visible");
            Debug.Log($"AERIAL_VISUAL_LAYOUT_OK|faces=right|thrusters={leftThruster.localPosition.x:F2},{rightThruster.localPosition.x:F2}|height={leftThruster.localPosition.y:F2}|size=medium|preview=visible");
        }

        private static float[] Values(float start, float end, int count, float exponent)
        {
            float[] result = new float[Mathf.Max(1, count)];
            for (int i = 0; i < result.Length; i++)
            {
                float t = result.Length <= 1 ? 1f : i / (float)(result.Length - 1);
                result[i] = Mathf.Lerp(start, end, Mathf.Pow(t, exponent));
            }
            result[result.Length - 1] = end;
            return result;
        }

        private static VehicleDefinition.CustomizationOption Option(string id, string name, Sprite sprite) =>
            new VehicleDefinition.CustomizationOption
            {
                optionId = id, displayName = name, sprite = sprite, unlockedByDefault = true,
                gemPrice = 0, allowsBlueprintProgression = true,
                visibleSize = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one,
                visualOffset = Vector2.zero
            };

        private static T[] ObjectArray<T>(SerializedProperty array) where T : UnityEngine.Object
        {
            if (array == null || !array.isArray) return Array.Empty<T>();
            T[] result = new T[array.arraySize];
            for (int i = 0; i < result.Length; i++)
                result[i] = array.GetArrayElementAtIndex(i).objectReferenceValue as T;
            return result;
        }

        private static void ConfigureSprite(string path, int maxSize)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
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
