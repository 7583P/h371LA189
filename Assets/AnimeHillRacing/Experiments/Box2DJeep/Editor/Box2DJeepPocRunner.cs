using System;
using System.Collections.Generic;
using System.Globalization;
using AnimeHillRacing.Experiments.Box2DJeep;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.EditorTools
{
    public static class Box2DJeepPocRunner
    {
        private const string RootFolder = "Assets/AnimeHillRacing/Experiments/Box2DJeep";
        private const string ScenePath = RootFolder + "/Box2DJeepTest.unity";
        private const string PrefabPath = RootFolder + "/Box2DJeepRig.prefab";
        private const string MaterialPath = RootFolder + "/Box2DJeepLine.mat";
        private const float FixedStep = 0.02f;
        private const float RenderStep = 1f / 60f;
        private const float ScenarioDuration = 12f;
        private const float ImpactScenarioDuration = 8f;
        private const float ImpactVelocityY = -10f;
        private const float GoldenTopSpeedMps = 27.777779f;

        private static readonly float[] WorldCoordinates =
        {
            0f,
            32768f,
            65536f,
            131072f,
            262144f,
            524288f
        };

        private static readonly float[] DriveComparisonCoordinates =
        {
            0f,
            131072f,
            262144f,
            524288f
        };

        private static readonly float[] DirectVelocityThresholds =
        {
            25f,
            GoldenTopSpeedMps,
            50f,
            75f,
            100f
        };

        private static readonly float[] VerticalImpactThresholds =
        {
            -5f,
            -10f,
            -15f,
            -20f
        };

        private enum ExperimentalDriveMode
        {
            ModernPhysicalDrive,
            OldLegacyChassisDrive
        }

        [MenuItem("Tools/Anime Hill Racing/Run Box2D Jeep Vertical Impact Threshold")]
        public static void RunVerticalImpactThreshold()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SimulationMode2D previousMode = Physics2D.simulationMode;
            bool previousAutoSync = Physics2D.simulationMode == SimulationMode2D.Script && Physics2D.autoSyncTransforms;
            Physics2D.simulationMode = SimulationMode2D.Script;
            Physics2D.autoSyncTransforms = false;

            try
            {
                Debug.Log(
                    "BOX2D_POC_VERTICAL_IMPACT_BEGIN|classification=REAL_UNITY_PHYSICS2D_SCRIPT_SIMULATION|" +
                    "fixedStep=0.02|requestedVx=27.777779");

                int resultCount = 0;
                for (int impactIndex = 0; impactIndex < VerticalImpactThresholds.Length; impactIndex++)
                {
                    float impactVy = VerticalImpactThresholds[impactIndex];
                    for (int coordinateIndex = 0; coordinateIndex < DriveComparisonCoordinates.Length; coordinateIndex++)
                    {
                        float worldX = DriveComparisonCoordinates[coordinateIndex];
                        ImpactScenarioResult result = RunImpactScenario(
                            worldX,
                            ExperimentalDriveMode.OldLegacyChassisDrive,
                            GoldenTopSpeedMps,
                            impactVy,
                            true);
                        LogVerticalImpactResult(result);
                        resultCount++;
                    }
                }

                Debug.Log($"BOX2D_POC_VERTICAL_IMPACT_END|count={resultCount}");
            }
            finally
            {
                Physics2D.simulationMode = previousMode;
                if (previousMode == SimulationMode2D.Script)
                {
                    Physics2D.autoSyncTransforms = previousAutoSync;
                }
            }
        }

        [MenuItem("Tools/Anime Hill Racing/Run Box2D Jeep Direct Velocity Threshold")]
        public static void RunDirectVelocityThreshold()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SimulationMode2D previousMode = Physics2D.simulationMode;
            bool previousAutoSync = Physics2D.simulationMode == SimulationMode2D.Script && Physics2D.autoSyncTransforms;
            Physics2D.simulationMode = SimulationMode2D.Script;
            Physics2D.autoSyncTransforms = false;

            try
            {
                Debug.Log(
                    "BOX2D_POC_VX_THRESHOLD_BEGIN|classification=REAL_UNITY_PHYSICS2D_SCRIPT_SIMULATION|" +
                    "fixedStep=0.02|impactVelocityY=-10.00|goldenTopSpeedMps=27.777779");

                int resultCount = 0;
                for (int speedIndex = 0; speedIndex < DirectVelocityThresholds.Length; speedIndex++)
                {
                    float requestedVx = DirectVelocityThresholds[speedIndex];
                    for (int coordinateIndex = 0; coordinateIndex < DriveComparisonCoordinates.Length; coordinateIndex++)
                    {
                        float worldX = DriveComparisonCoordinates[coordinateIndex];
                        ImpactScenarioResult result = RunImpactScenario(
                            worldX,
                            ExperimentalDriveMode.OldLegacyChassisDrive,
                            requestedVx);
                        LogVelocityThresholdResult(result);
                        resultCount++;
                    }
                }

                Debug.Log($"BOX2D_POC_VX_THRESHOLD_END|count={resultCount}");
            }
            finally
            {
                Physics2D.simulationMode = previousMode;
                if (previousMode == SimulationMode2D.Script)
                {
                    Physics2D.autoSyncTransforms = previousAutoSync;
                }
            }
        }

        [MenuItem("Tools/Anime Hill Racing/Verify Strong Impact X=524288")]
        public static void VerifyStrongImpact524288()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SimulationMode2D previousMode = Physics2D.simulationMode;
            bool previousAutoSync = Physics2D.simulationMode == SimulationMode2D.Script && Physics2D.autoSyncTransforms;
            Physics2D.simulationMode = SimulationMode2D.Script;
            Physics2D.autoSyncTransforms = false;

            try
            {
                Debug.Log("BOX2D_STRONG_IMPACT_VERIFY_BEGIN|x=524288|requestedVx=100|impactVy=-40");
                ImpactScenarioResult result = RunImpactScenario(
                    524288f,
                    ExperimentalDriveMode.OldLegacyChassisDrive,
                    100f,
                    -40f,
                    true);
                LogVelocityThresholdResult(result);
                Debug.Log(
                    $"BOX2D_STRONG_IMPACT_VERIFY_END|rearWheelStuck={result.rear.wheelStuck}|" +
                    $"frontWheelStuck={result.front.wheelStuck}|rearBelow={result.rear.centerBelowSeconds:F3}|" +
                    $"frontBelow={result.front.centerBelowSeconds:F3}");
            }
            finally
            {
                Physics2D.simulationMode = previousMode;
                if (previousMode == SimulationMode2D.Script)
                {
                    Physics2D.autoSyncTransforms = previousAutoSync;
                }
            }
        }

        [MenuItem("Tools/Anime Hill Racing/Run Box2D Jeep Legacy Drive A-B")]
        public static void RunLegacyDriveComparison()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SimulationMode2D previousMode = Physics2D.simulationMode;
            bool previousAutoSync = Physics2D.simulationMode == SimulationMode2D.Script && Physics2D.autoSyncTransforms;
            Physics2D.simulationMode = SimulationMode2D.Script;
            Physics2D.autoSyncTransforms = false;

            try
            {
                Debug.Log(
                    "BOX2D_POC_DRIVE_AB_BEGIN|classification=REAL_UNITY_PHYSICS2D_SCRIPT_SIMULATION|" +
                    "fixedStep=0.02|impactVelocityY=-10.00|legacyRequestedPositiveVx=5000|legacyRequestedNegativeVx=-50");

                int resultCount = 0;
                for (int i = 0; i < DriveComparisonCoordinates.Length; i++)
                {
                    float worldX = DriveComparisonCoordinates[i];
                    ImpactScenarioResult modern = RunImpactScenario(worldX, ExperimentalDriveMode.ModernPhysicalDrive);
                    LogDriveComparisonResult(modern);
                    resultCount++;

                    ImpactScenarioResult legacy = RunImpactScenario(worldX, ExperimentalDriveMode.OldLegacyChassisDrive);
                    LogDriveComparisonResult(legacy);
                    resultCount++;

                    if (i == 0 && !legacy.finite)
                    {
                        throw new InvalidOperationException(
                            "X=0 legacy drivetrain was non-finite; large-coordinate comparison was intentionally stopped.");
                    }
                }

                Debug.Log($"BOX2D_POC_DRIVE_AB_END|count={resultCount}");
            }
            finally
            {
                Physics2D.simulationMode = previousMode;
                if (previousMode == SimulationMode2D.Script)
                {
                    Physics2D.autoSyncTransforms = previousAutoSync;
                }
            }
        }

        [MenuItem("Tools/Anime Hill Racing/Build and Run Box2D Jeep POC")]
        public static void BuildAndRun()
        {
            BuildAssets();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SimulationMode2D previousMode = Physics2D.simulationMode;
            bool previousAutoSync = Physics2D.simulationMode == SimulationMode2D.Script && Physics2D.autoSyncTransforms;
            Physics2D.simulationMode = SimulationMode2D.Script;
            Physics2D.autoSyncTransforms = false;

            try
            {
                Debug.Log("BOX2D_POC_BEGIN|classification=REAL_UNITY_PHYSICS2D_SCRIPT_SIMULATION|fixedStep=0.02|renderStep=0.0166667");
                List<ScenarioResult> results = new List<ScenarioResult>(WorldCoordinates.Length);

                ScenarioResult origin = RunScenario(WorldCoordinates[0]);
                results.Add(origin);
                LogResult(origin);

                if (!origin.finite || origin.driveDistance < 3f || !origin.jointsIntact)
                {
                    throw new InvalidOperationException(
                        "X=0 handling validation failed; large-coordinate tests were intentionally not executed. " +
                        $"finite={origin.finite}, driveDistance={origin.driveDistance:F3}, jointsIntact={origin.jointsIntact}");
                }

                for (int i = 1; i < WorldCoordinates.Length; i++)
                {
                    ScenarioResult result = RunScenario(WorldCoordinates[i]);
                    results.Add(result);
                    LogResult(result);
                }

                bool monotonicSignal = HasIncreasingDegradation(results);
                Debug.Log($"BOX2D_POC_END|originPassed=true|increasingDegradation={monotonicSignal}|count={results.Count}");

                List<ImpactScenarioResult> impactResults = new List<ImpactScenarioResult>(WorldCoordinates.Length);
                ImpactScenarioResult originImpact = RunImpactScenario(WorldCoordinates[0]);
                impactResults.Add(originImpact);
                LogImpactResult(originImpact);

                if (!originImpact.finite || !originImpact.jointsIntact ||
                    originImpact.rear.centerBelowSeconds > 0f || originImpact.front.centerBelowSeconds > 0f ||
                    originImpact.rear.wheelStuck || originImpact.front.wheelStuck)
                {
                    throw new InvalidOperationException(
                        "X=0 impact validation failed; large-coordinate impact tests were intentionally not executed. " +
                        $"finite={originImpact.finite}, jointsIntact={originImpact.jointsIntact}, " +
                        $"rearCenterBelow={originImpact.rear.centerBelowSeconds:F3}, " +
                        $"frontCenterBelow={originImpact.front.centerBelowSeconds:F3}, " +
                        $"rearStuck={originImpact.rear.wheelStuck}, frontStuck={originImpact.front.wheelStuck}");
                }

                for (int i = 1; i < WorldCoordinates.Length; i++)
                {
                    ImpactScenarioResult impact = RunImpactScenario(WorldCoordinates[i]);
                    impactResults.Add(impact);
                    LogImpactResult(impact);
                }

                bool anyUnderTerrain = impactResults.Exists(result =>
                    result.rear.centerBelowSeconds > 0f || result.front.centerBelowSeconds > 0f);
                bool anyWheelStuck = impactResults.Exists(result => result.rear.wheelStuck || result.front.wheelStuck);
                Debug.Log(
                    $"BOX2D_POC_IMPACT_END|originPassed=true|anyWheelUnderTerrain={anyUnderTerrain}|" +
                    $"anyWheelStuck={anyWheelStuck}|count={impactResults.Count}");
            }
            finally
            {
                Physics2D.simulationMode = previousMode;
                if (previousMode == SimulationMode2D.Script)
                {
                    Physics2D.autoSyncTransforms = previousAutoSync;
                }
            }
        }

        private static void BuildAssets()
        {
            Material lineMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (lineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                if (shader == null)
                {
                    throw new InvalidOperationException("No compatible unlit shader was found for POC debug lines.");
                }

                lineMaterial = new Material(shader) { name = "Box2D Jeep Line" };
                AssetDatabase.CreateAsset(lineMaterial, MaterialPath);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Box2DJeepRig rig = Box2DJeepFactory.Create(0f, lineMaterial, true);
            GameObject prefabRoot = PrefabUtility.SaveAsPrefabAssetAndConnect(
                rig.gameObject,
                PrefabPath,
                InteractionMode.AutomatedAction);

            if (prefabRoot == null)
            {
                throw new InvalidOperationException("Could not create the isolated Box2D Jeep prefab.");
            }

            GameObject cameraObject = new GameObject("Direct Follow Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);
            cameraObject.tag = "MainCamera";
            SimpleBox2DCamera follow = cameraObject.AddComponent<SimpleBox2DCamera>();
            follow.Configure(rig.Chassis.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Could not save the isolated Box2D Jeep test scene.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"BOX2D_POC_ASSETS|scene={ScenePath}|prefab={PrefabPath}|material={MaterialPath}");
        }

        private static ScenarioResult RunScenario(float worldX)
        {
            Box2DJeepRig rig = Box2DJeepFactory.Create(worldX, null, false);
            float heightOffset = AbsoluteEdgeTerrain2D.SampleHeight(worldX) - AbsoluteEdgeTerrain2D.SampleHeight(0f);
            rig.Chassis.position += Vector2.up * heightOffset;
            rig.RearWheel.position += Vector2.up * heightOffset;
            rig.FrontWheel.position += Vector2.up * heightOffset;
            rig.Terrain.RebuildNow();
            Physics2D.SyncTransforms();

            ScenarioResult result = new ScenarioResult { worldX = worldX, finite = true, jointsIntact = true };
            EdgeCollider2D edge = rig.Terrain.TerrainCollider;
            float accumulator = 0f;
            float physicsTime = 0f;
            float driveStartX = rig.Chassis.position.x;
            float driveEndX = driveStartX;
            int rearBelowStreak = 0;
            int frontBelowStreak = 0;
            int rearMaxBelowStreak = 0;
            int frontMaxBelowStreak = 0;
            bool lastRearContact = false;
            bool lastFrontContact = false;
            bool hasStableContactSample = false;
            int stableSamples = 0;
            int rearContactSamples = 0;
            int frontContactSamples = 0;
            List<float> chassisClearance = new List<float>(110);
            List<float> rearClearance = new List<float>(110);
            List<float> frontClearance = new List<float>(110);

            int renderFrames = Mathf.CeilToInt(ScenarioDuration / RenderStep);
            for (int frame = 0; frame < renderFrames; frame++)
            {
                // This represents the historical unconditional per-render Update rebuild.
                rig.Terrain.RebuildNow();
                accumulator += RenderStep;

                while (accumulator + 0.000001f >= FixedStep)
                {
                    float drive = physicsTime >= 3f && physicsTime < 8f ? 0.65f : 0f;
                    float brake = physicsTime >= 8f ? 1f : 0f;
                    if (physicsTime >= 3f && physicsTime < 3f + FixedStep)
                    {
                        driveStartX = rig.Chassis.position.x;
                    }

                    rig.StepDrive(drive, brake);
                    Physics2D.Simulate(FixedStep);
                    physicsTime += FixedStep;
                    accumulator -= FixedStep;

                    if (physicsTime <= 8f)
                    {
                        driveEndX = rig.Chassis.position.x;
                    }

                    bool rearContact = IsContact(rig.RearCollider, edge);
                    bool frontContact = IsContact(rig.FrontCollider, edge);
                    float rearPenetration = Penetration(rig.RearCollider, edge);
                    float frontPenetration = Penetration(rig.FrontCollider, edge);
                    float chassisPenetration = Penetration(rig.Chassis.GetComponent<BoxCollider2D>(), edge);
                    result.maxPenetration = Mathf.Max(result.maxPenetration, rearPenetration, frontPenetration, chassisPenetration);

                    float rearSurface = EdgeHeightAt(edge, rig.RearWheel.position.x);
                    float frontSurface = EdgeHeightAt(edge, rig.FrontWheel.position.x);
                    if (physicsTime >= 3f && physicsTime <= 8f)
                    {
                        rearBelowStreak = rig.RearWheel.position.y < rearSurface - 0.02f ? rearBelowStreak + 1 : 0;
                        frontBelowStreak = rig.FrontWheel.position.y < frontSurface - 0.02f ? frontBelowStreak + 1 : 0;
                        rearMaxBelowStreak = Mathf.Max(rearMaxBelowStreak, rearBelowStreak);
                        frontMaxBelowStreak = Mathf.Max(frontMaxBelowStreak, frontBelowStreak);
                    }

                    if (physicsTime >= 10f)
                    {
                        float chassisSurface = EdgeHeightAt(edge, rig.Chassis.position.x);
                        chassisClearance.Add(rig.Chassis.position.y - chassisSurface);
                        rearClearance.Add(rig.RearWheel.position.y - rearSurface);
                        frontClearance.Add(rig.FrontWheel.position.y - frontSurface);
                        stableSamples++;
                        if (rearContact) rearContactSamples++;
                        if (frontContact) frontContactSamples++;

                        if (hasStableContactSample)
                        {
                            if (rearContact != lastRearContact) result.rearContactToggles++;
                            if (frontContact != lastFrontContact) result.frontContactToggles++;
                        }

                        lastRearContact = rearContact;
                        lastFrontContact = frontContact;
                        hasStableContactSample = true;
                    }

                    result.maxJointSpan = Mathf.Max(
                        result.maxJointSpan,
                        JointSpan(rig.RearJoint),
                        JointSpan(rig.FrontJoint));
                    result.jointsIntact &= JointIsIntact(rig.RearJoint) && JointIsIntact(rig.FrontJoint);
                    result.finite &= IsFinite(rig.Chassis) && IsFinite(rig.RearWheel) && IsFinite(rig.FrontWheel);
                    if (!result.finite)
                    {
                        break;
                    }
                }

                if (!result.finite)
                {
                    break;
                }
            }

            result.driveDistance = driveEndX - driveStartX;
            result.chassisJitter = PeakToPeak(chassisClearance);
            result.rearJitter = PeakToPeak(rearClearance);
            result.frontJitter = PeakToPeak(frontClearance);
            result.rearContactRatio = stableSamples > 0 ? (float)rearContactSamples / stableSamples : 0f;
            result.frontContactRatio = stableSamples > 0 ? (float)frontContactSamples / stableSamples : 0f;
            result.rearBelowSeconds = rearMaxBelowStreak * FixedStep;
            result.frontBelowSeconds = frontMaxBelowStreak * FixedStep;
            bool unableToRecover = result.driveDistance < 1f;
            result.wheelStuck = unableToRecover && result.jointsIntact &&
                (result.rearBelowSeconds >= 0.5f || result.frontBelowSeconds >= 0.5f);

            UnityEngine.Object.DestroyImmediate(rig.gameObject);
            Physics2D.SyncTransforms();
            return result;
        }

        private static ImpactScenarioResult RunImpactScenario(
            float worldX,
            ExperimentalDriveMode driveMode = ExperimentalDriveMode.ModernPhysicalDrive,
            float requestedChassisVx = 5000f,
            float requestedImpactVy = ImpactVelocityY,
            bool directDriveFromStart = false)
        {
            Box2DJeepRig rig = Box2DJeepFactory.Create(worldX, null, false);
            EdgeCollider2D edge = rig.Terrain.TerrainCollider;
            List<Vector2> impactPoints = new List<Vector2>(AbsoluteEdgeTerrain2D.PointCount);
            RebuildImpactTerrain(rig, worldX, impactPoints);

            Vector2 impactVelocity = new Vector2(0f, requestedImpactVy);
            rig.Chassis.linearVelocity = impactVelocity;
            rig.RearWheel.linearVelocity = impactVelocity;
            rig.FrontWheel.linearVelocity = impactVelocity;
            rig.Chassis.angularVelocity = 0f;
            rig.RearWheel.angularVelocity = 0f;
            rig.FrontWheel.angularVelocity = 0f;
            Physics2D.SyncTransforms();

            ImpactScenarioResult result = new ImpactScenarioResult
            {
                worldX = worldX,
                driveMode = driveMode,
                requestedChassisVx = requestedChassisVx,
                requestedImpactVy = requestedImpactVy,
                finite = true,
                jointsIntact = true,
                rear = new WheelImpactMetrics(Box2DJeepFactory.WheelRadius),
                front = new WheelImpactMetrics(Box2DJeepFactory.WheelRadius)
            };

            float accumulator = 0f;
            float physicsTime = 0f;
            float driveStartX = rig.Chassis.position.x;
            float driveEndX = driveStartX;
            int renderFrames = Mathf.CeilToInt(ImpactScenarioDuration / RenderStep);

            for (int frame = 0; frame < renderFrames; frame++)
            {
                // Same continuous profile at every X; collider vertices remain absolute.
                RebuildImpactTerrain(rig, worldX, impactPoints);
                accumulator += RenderStep;

                while (accumulator + 0.000001f >= FixedStep)
                {
                    float drive = directDriveFromStart
                        ? (physicsTime < 6f ? 1f : 0f)
                        : (physicsTime >= 3f && physicsTime < 6f ? 0.65f : 0f);
                    float brake = physicsTime >= 6f ? 1f : 0f;
                    float driveStartTime = directDriveFromStart ? 0f : 3f;
                    if (physicsTime >= driveStartTime && physicsTime < driveStartTime + FixedStep)
                    {
                        driveStartX = rig.Chassis.position.x;
                    }

                    float preContactChassisVy = rig.Chassis.linearVelocity.y;
                    float preContactRearVy = rig.RearWheel.linearVelocity.y;
                    float preContactFrontVy = rig.FrontWheel.linearVelocity.y;

                    if (driveMode == ExperimentalDriveMode.OldLegacyChassisDrive)
                    {
                        StepLegacyChassisDrive(rig, drive, requestedChassisVx);
                    }
                    else
                    {
                        rig.StepDrive(drive, brake);
                    }

                    Physics2D.Simulate(FixedStep);
                    physicsTime += FixedStep;
                    accumulator -= FixedStep;

                    if (drive > 0f)
                    {
                        result.actualChassisVx = rig.Chassis.linearVelocity.x;
                        result.maxActualChassisVx = Mathf.Max(result.maxActualChassisVx, rig.Chassis.linearVelocity.x);
                    }

                    if (physicsTime <= 6f)
                    {
                        driveEndX = rig.Chassis.position.x;
                    }

                    SampleWheelImpact(
                        result.rear,
                        rig.RearWheel,
                        rig.RearCollider,
                        edge,
                        preContactChassisVy,
                        preContactRearVy);
                    SampleWheelImpact(
                        result.front,
                        rig.FrontWheel,
                        rig.FrontCollider,
                        edge,
                        preContactChassisVy,
                        preContactFrontVy);
                    float rearJointSpan = JointSpan(rig.RearJoint);
                    float frontJointSpan = JointSpan(rig.FrontJoint);
                    result.rear.maxJointSpan = Mathf.Max(result.rear.maxJointSpan, rearJointSpan);
                    result.front.maxJointSpan = Mathf.Max(result.front.maxJointSpan, frontJointSpan);
                    result.maxJointSpan = Mathf.Max(result.maxJointSpan, rearJointSpan, frontJointSpan);
                    result.jointsIntact &= JointIsIntact(rig.RearJoint) && JointIsIntact(rig.FrontJoint);
                    result.finite &= IsFinite(rig.Chassis) && IsFinite(rig.RearWheel) && IsFinite(rig.FrontWheel);
                    if (!result.finite)
                    {
                        break;
                    }
                }

                if (!result.finite)
                {
                    break;
                }
            }

            result.driveDistance = driveEndX - driveStartX;
            FinalizeWheelImpact(result.rear, result.driveDistance, JointIsIntact(rig.RearJoint));
            FinalizeWheelImpact(result.front, result.driveDistance, JointIsIntact(rig.FrontJoint));

            UnityEngine.Object.DestroyImmediate(rig.gameObject);
            Physics2D.SyncTransforms();
            return result;
        }

        private static void StepLegacyChassisDrive(Box2DJeepRig rig, float drive, float requestedPositiveVx)
        {
            rig.RearJoint.useMotor = false;
            rig.FrontJoint.useMotor = false;

            Vector2 velocity = rig.Chassis.linearVelocity;
            velocity.x = drive > 0f ? requestedPositiveVx : drive < 0f ? -50f : 0f;
            rig.Chassis.linearVelocity = velocity;
        }

        private static void RebuildImpactTerrain(Box2DJeepRig rig, float profileOriginX, List<Vector2> points)
        {
            EdgeCollider2D edge = rig.Terrain.TerrainCollider;
            float localTargetX = rig.Chassis.position.x - profileOriginX;
            int centerIndex = Mathf.FloorToInt(localTargetX / AbsoluteEdgeTerrain2D.StepX);
            int firstIndex = centerIndex - AbsoluteEdgeTerrain2D.PointsBehind;

            points.Clear();
            for (int i = 0; i < AbsoluteEdgeTerrain2D.PointCount; i++)
            {
                float profileX = (firstIndex + i) * AbsoluteEdgeTerrain2D.StepX;
                float absoluteX = profileOriginX + profileX;
                points.Add(new Vector2(absoluteX, AbsoluteEdgeTerrain2D.SampleHeight(profileX)));
            }

            edge.SetPoints(points);
        }

        private static void SampleWheelImpact(
            WheelImpactMetrics metrics,
            Rigidbody2D wheel,
            CircleCollider2D collider,
            EdgeCollider2D edge,
            float preContactChassisVy,
            float preContactWheelVy)
        {
            float centerY = wheel.position.y;
            float surfaceY = EdgeHeightAt(edge, wheel.position.x);
            float centerClearance = centerY - surfaceY;
            bool contact = IsContact(collider, edge);
            float overlap = Penetration(collider, edge);
            bool centerBelow = centerY < surfaceY;
            bool partiallyBelow = centerY - metrics.radius < surfaceY;

            if (contact && !metrics.firstContactRecorded)
            {
                metrics.firstContactRecorded = true;
                metrics.chassisVyBeforeFirstContact = preContactChassisVy;
                metrics.wheelVyBeforeFirstContact = preContactWheelVy;
            }

            if (centerClearance < metrics.minimumCenterClearance)
            {
                metrics.minimumCenterClearance = centerClearance;
                metrics.centerYAtDeepest = centerY;
                metrics.surfaceYAtDeepest = surfaceY;
                metrics.contactAtDeepest = contact;
            }

            metrics.maxActualOverlap = Mathf.Max(metrics.maxActualOverlap, overlap);
            metrics.finalContact = contact;
            metrics.contactSamples++;
            if (contact) metrics.contactCount++;
            if (metrics.hasContactSample && contact != metrics.lastContact) metrics.contactToggles++;
            metrics.lastContact = contact;
            metrics.hasContactSample = true;

            if (centerBelow)
            {
                metrics.centerBelowSeconds += FixedStep;
                metrics.centerBelowStreak++;
                metrics.aboveSurfaceStreak = 0;
                metrics.maxCenterBelowStreak = Mathf.Max(metrics.maxCenterBelowStreak, metrics.centerBelowStreak);
            }
            else
            {
                metrics.centerBelowStreak = 0;
                metrics.aboveSurfaceStreak++;
            }

            if (partiallyBelow)
            {
                metrics.partiallyBelowSeconds += FixedStep;
            }
        }

        private static void FinalizeWheelImpact(WheelImpactMetrics metrics, float driveDistance, bool jointIntact)
        {
            metrics.contactRatio = metrics.contactSamples > 0
                ? (float)metrics.contactCount / metrics.contactSamples
                : 0f;
            metrics.recovered = metrics.aboveSurfaceStreak * FixedStep >= 0.5f;
            bool persistentCenterBelow = metrics.maxCenterBelowStreak * FixedStep >= 0.5f;
            bool unableToRecover = driveDistance < 1f || !metrics.recovered;
            metrics.wheelStuck = persistentCenterBelow && unableToRecover && jointIntact;
        }

        private static bool IsContact(Collider2D first, Collider2D second)
        {
            ColliderDistance2D distance = Physics2D.Distance(first, second);
            return distance.isValid && distance.distance <= Physics2D.defaultContactOffset * 2f;
        }

        private static float Penetration(Collider2D first, Collider2D second)
        {
            ColliderDistance2D distance = Physics2D.Distance(first, second);
            return distance.isValid && distance.isOverlapped ? Mathf.Max(0f, -distance.distance) : 0f;
        }

        private static float EdgeHeightAt(EdgeCollider2D edge, float worldX)
        {
            Vector2[] points = edge.points;
            if (points.Length == 0)
            {
                return float.NegativeInfinity;
            }

            float localX = edge.transform.InverseTransformPoint(new Vector3(worldX, 0f, 0f)).x;
            if (localX <= points[0].x) return edge.transform.TransformPoint(points[0]).y;
            if (localX >= points[points.Length - 1].x) return edge.transform.TransformPoint(points[points.Length - 1]).y;

            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].x < localX) continue;
                float t = Mathf.InverseLerp(points[i - 1].x, points[i].x, localX);
                Vector2 local = Vector2.Lerp(points[i - 1], points[i], t);
                return edge.transform.TransformPoint(local).y;
            }

            return edge.transform.TransformPoint(points[points.Length - 1]).y;
        }

        private static float JointSpan(WheelJoint2D joint)
        {
            if (!JointIsIntact(joint)) return float.PositiveInfinity;
            Vector2 wheelAnchor = joint.attachedRigidbody.transform.TransformPoint(joint.anchor);
            Vector2 chassisAnchor = joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
            return Vector2.Distance(wheelAnchor, chassisAnchor);
        }

        private static bool JointIsIntact(WheelJoint2D joint)
        {
            return joint != null && joint.enabled && joint.attachedRigidbody != null && joint.connectedBody != null;
        }

        private static bool IsFinite(Rigidbody2D body)
        {
            Vector2 position = body.position;
            Vector2 velocity = body.linearVelocity;
            return float.IsFinite(position.x) && float.IsFinite(position.y) &&
                   float.IsFinite(velocity.x) && float.IsFinite(velocity.y) &&
                   float.IsFinite(body.rotation) && float.IsFinite(body.angularVelocity);
        }

        private static float PeakToPeak(List<float> values)
        {
            if (values.Count == 0) return float.PositiveInfinity;
            float minimum = values[0];
            float maximum = values[0];
            for (int i = 1; i < values.Count; i++)
            {
                minimum = Mathf.Min(minimum, values[i]);
                maximum = Mathf.Max(maximum, values[i]);
            }

            return maximum - minimum;
        }

        private static bool HasIncreasingDegradation(List<ScenarioResult> results)
        {
            if (results.Count < 3) return false;
            float origin = results[0].chassisJitter + results[0].frontJitter + results[0].rearJitter;
            float far = results[results.Count - 1].chassisJitter + results[results.Count - 1].frontJitter + results[results.Count - 1].rearJitter;
            bool strongerJitter = far > origin * 1.5f + 0.01f;
            bool strongerPenetration = results[results.Count - 1].maxPenetration > results[0].maxPenetration + 0.01f;
            bool newStuck = !results[0].wheelStuck && results[results.Count - 1].wheelStuck;
            return strongerJitter || strongerPenetration || newStuck;
        }

        private static void LogResult(ScenarioResult result)
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            Debug.Log(
                "BOX2D_POC_RESULT" +
                "|x=" + result.worldX.ToString("F0", c) +
                "|finite=" + result.finite +
                "|driveDistance=" + result.driveDistance.ToString("F5", c) +
                "|chassisJitter=" + result.chassisJitter.ToString("F6", c) +
                "|rearJitter=" + result.rearJitter.ToString("F6", c) +
                "|frontJitter=" + result.frontJitter.ToString("F6", c) +
                "|rearContactRatio=" + result.rearContactRatio.ToString("F4", c) +
                "|frontContactRatio=" + result.frontContactRatio.ToString("F4", c) +
                "|rearContactToggles=" + result.rearContactToggles +
                "|frontContactToggles=" + result.frontContactToggles +
                "|maxPenetration=" + result.maxPenetration.ToString("F6", c) +
                "|maxJointSpan=" + result.maxJointSpan.ToString("F6", c) +
                "|rearBelowSeconds=" + result.rearBelowSeconds.ToString("F3", c) +
                "|frontBelowSeconds=" + result.frontBelowSeconds.ToString("F3", c) +
                "|jointsIntact=" + result.jointsIntact +
                "|wheelStuck=" + result.wheelStuck);
        }

        private static void LogImpactResult(ImpactScenarioResult result)
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            Debug.Log(
                "BOX2D_POC_IMPACT_RESULT" +
                "|x=" + result.worldX.ToString("F0", c) +
                "|impactVelocityY=" + ImpactVelocityY.ToString("F2", c) +
                "|finite=" + result.finite +
                "|driveDistance=" + result.driveDistance.ToString("F5", c) +
                "|maxJointSpan=" + result.maxJointSpan.ToString("F6", c) +
                "|jointsIntact=" + result.jointsIntact +
                WheelLog("rear", result.rear, c) +
                WheelLog("front", result.front, c));
        }

        private static void LogDriveComparisonResult(ImpactScenarioResult result)
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            string mode = result.driveMode == ExperimentalDriveMode.ModernPhysicalDrive
                ? "MODERN_PHYSICAL_DRIVE"
                : "OLD_LEGACY_CHASSIS_DRIVE";
            Debug.Log(
                "BOX2D_POC_DRIVE_AB_RESULT" +
                "|x=" + result.worldX.ToString("F0", c) +
                "|driveMode=" + mode +
                "|finite=" + result.finite +
                "|actualChassisVx=" + result.maxActualChassisVx.ToString("F6", c) +
                "|lastChassisVx=" + result.actualChassisVx.ToString("F6", c) +
                "|driveDistance=" + result.driveDistance.ToString("F5", c) +
                "|rearMaxOverlap=" + result.rear.maxActualOverlap.ToString("F6", c) +
                "|frontMaxOverlap=" + result.front.maxActualOverlap.ToString("F6", c) +
                "|rearCenterBelowSeconds=" + result.rear.centerBelowSeconds.ToString("F3", c) +
                "|frontCenterBelowSeconds=" + result.front.centerBelowSeconds.ToString("F3", c) +
                "|rearPartiallyBelowSeconds=" + result.rear.partiallyBelowSeconds.ToString("F3", c) +
                "|frontPartiallyBelowSeconds=" + result.front.partiallyBelowSeconds.ToString("F3", c) +
                "|rearContactRatio=" + result.rear.contactRatio.ToString("F4", c) +
                "|frontContactRatio=" + result.front.contactRatio.ToString("F4", c) +
                "|rearContactToggles=" + result.rear.contactToggles +
                "|frontContactToggles=" + result.front.contactToggles +
                "|rearMaxJointSpan=" + result.rear.maxJointSpan.ToString("F6", c) +
                "|frontMaxJointSpan=" + result.front.maxJointSpan.ToString("F6", c) +
                "|rearRecovered=" + result.rear.recovered +
                "|frontRecovered=" + result.front.recovered +
                "|rearWheelStuck=" + result.rear.wheelStuck +
                "|frontWheelStuck=" + result.front.wheelStuck);
        }

        private static void LogVelocityThresholdResult(ImpactScenarioResult result)
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            Debug.Log(
                "BOX2D_POC_VX_THRESHOLD_RESULT" +
                "|x=" + result.worldX.ToString("F0", c) +
                "|requestedVx=" + result.requestedChassisVx.ToString("F6", c) +
                "|actualChassisVx=" + result.maxActualChassisVx.ToString("F6", c) +
                "|finite=" + result.finite +
                "|rearMaxOverlap=" + result.rear.maxActualOverlap.ToString("F6", c) +
                "|frontMaxOverlap=" + result.front.maxActualOverlap.ToString("F6", c) +
                "|rearCenterBelowSeconds=" + result.rear.centerBelowSeconds.ToString("F3", c) +
                "|frontCenterBelowSeconds=" + result.front.centerBelowSeconds.ToString("F3", c) +
                "|rearPartiallyBelowSeconds=" + result.rear.partiallyBelowSeconds.ToString("F3", c) +
                "|frontPartiallyBelowSeconds=" + result.front.partiallyBelowSeconds.ToString("F3", c) +
                "|rearContactRatio=" + result.rear.contactRatio.ToString("F4", c) +
                "|frontContactRatio=" + result.front.contactRatio.ToString("F4", c) +
                "|rearMaxJointSpan=" + result.rear.maxJointSpan.ToString("F6", c) +
                "|frontMaxJointSpan=" + result.front.maxJointSpan.ToString("F6", c) +
                "|rearRecovered=" + result.rear.recovered +
                "|frontRecovered=" + result.front.recovered +
                "|rearWheelStuck=" + result.rear.wheelStuck +
                "|frontWheelStuck=" + result.front.wheelStuck);
        }

        private static void LogVerticalImpactResult(ImpactScenarioResult result)
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            Debug.Log(
                "BOX2D_POC_VERTICAL_IMPACT_RESULT" +
                "|x=" + result.worldX.ToString("F0", c) +
                "|impactVy=" + result.requestedImpactVy.ToString("F2", c) +
                "|rearMaxOverlap=" + result.rear.maxActualOverlap.ToString("F6", c) +
                "|frontMaxOverlap=" + result.front.maxActualOverlap.ToString("F6", c) +
                "|rearCenterBelowSeconds=" + result.rear.centerBelowSeconds.ToString("F3", c) +
                "|frontCenterBelowSeconds=" + result.front.centerBelowSeconds.ToString("F3", c) +
                "|rearPartiallyBelowSeconds=" + result.rear.partiallyBelowSeconds.ToString("F3", c) +
                "|frontPartiallyBelowSeconds=" + result.front.partiallyBelowSeconds.ToString("F3", c) +
                "|rearContactRatio=" + result.rear.contactRatio.ToString("F4", c) +
                "|frontContactRatio=" + result.front.contactRatio.ToString("F4", c) +
                "|rearContactToggles=" + result.rear.contactToggles +
                "|frontContactToggles=" + result.front.contactToggles +
                "|rearMaxJointSpan=" + result.rear.maxJointSpan.ToString("F6", c) +
                "|frontMaxJointSpan=" + result.front.maxJointSpan.ToString("F6", c) +
                "|rearChassisVyBeforeContact=" + result.rear.chassisVyBeforeFirstContact.ToString("F6", c) +
                "|frontChassisVyBeforeContact=" + result.front.chassisVyBeforeFirstContact.ToString("F6", c) +
                "|rearWheelVyBeforeContact=" + result.rear.wheelVyBeforeFirstContact.ToString("F6", c) +
                "|frontWheelVyBeforeContact=" + result.front.wheelVyBeforeFirstContact.ToString("F6", c) +
                "|rearRecovered=" + result.rear.recovered +
                "|frontRecovered=" + result.front.recovered +
                "|rearWheelStuck=" + result.rear.wheelStuck +
                "|frontWheelStuck=" + result.front.wheelStuck);
        }

        private static string WheelLog(string prefix, WheelImpactMetrics wheel, CultureInfo culture)
        {
            return
                "|" + prefix + "CenterY=" + wheel.centerYAtDeepest.ToString("F6", culture) +
                "|" + prefix + "SurfaceY=" + wheel.surfaceYAtDeepest.ToString("F6", culture) +
                "|" + prefix + "Radius=" + wheel.radius.ToString("F3", culture) +
                "|" + prefix + "ContactAtDeepest=" + wheel.contactAtDeepest +
                "|" + prefix + "FinalContact=" + wheel.finalContact +
                "|" + prefix + "ContactRatio=" + wheel.contactRatio.ToString("F4", culture) +
                "|" + prefix + "ContactToggles=" + wheel.contactToggles +
                "|" + prefix + "ActualOverlap=" + wheel.maxActualOverlap.ToString("F6", culture) +
                "|" + prefix + "MaxJointSpan=" + wheel.maxJointSpan.ToString("F6", culture) +
                "|" + prefix + "CenterBelowSeconds=" + wheel.centerBelowSeconds.ToString("F3", culture) +
                "|" + prefix + "PartiallyBelowSeconds=" + wheel.partiallyBelowSeconds.ToString("F3", culture) +
                "|" + prefix + "Recovered=" + wheel.recovered +
                "|" + prefix + "WheelStuck=" + wheel.wheelStuck;
        }

        private sealed class ScenarioResult
        {
            public float worldX;
            public bool finite;
            public float driveDistance;
            public float chassisJitter;
            public float rearJitter;
            public float frontJitter;
            public float rearContactRatio;
            public float frontContactRatio;
            public int rearContactToggles;
            public int frontContactToggles;
            public float maxPenetration;
            public float maxJointSpan;
            public float rearBelowSeconds;
            public float frontBelowSeconds;
            public bool jointsIntact;
            public bool wheelStuck;
        }

        private sealed class ImpactScenarioResult
        {
            public float worldX;
            public ExperimentalDriveMode driveMode;
            public float requestedChassisVx;
            public float requestedImpactVy;
            public bool finite;
            public float driveDistance;
            public float actualChassisVx;
            public float maxActualChassisVx = float.NegativeInfinity;
            public float maxJointSpan;
            public bool jointsIntact;
            public WheelImpactMetrics rear;
            public WheelImpactMetrics front;
        }

        private sealed class WheelImpactMetrics
        {
            public readonly float radius;
            public float minimumCenterClearance = float.PositiveInfinity;
            public float centerYAtDeepest;
            public float surfaceYAtDeepest;
            public bool contactAtDeepest;
            public bool finalContact;
            public bool firstContactRecorded;
            public float chassisVyBeforeFirstContact;
            public float wheelVyBeforeFirstContact;
            public bool hasContactSample;
            public bool lastContact;
            public int contactSamples;
            public int contactCount;
            public int contactToggles;
            public float contactRatio;
            public float maxActualOverlap;
            public float maxJointSpan;
            public float centerBelowSeconds;
            public float partiallyBelowSeconds;
            public int centerBelowStreak;
            public int maxCenterBelowStreak;
            public int aboveSurfaceStreak;
            public bool recovered;
            public bool wheelStuck;

            public WheelImpactMetrics(float wheelRadius)
            {
                radius = wheelRadius;
            }
        }
    }
}
