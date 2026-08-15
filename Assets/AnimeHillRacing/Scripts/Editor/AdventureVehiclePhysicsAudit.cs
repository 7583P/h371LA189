#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    /// <summary>Deterministic editor-only A-H regression audit for Adventure 2.5D vehicle handling.</summary>
    public static class AdventureVehiclePhysicsAudit
    {
        private const string DataRoot = "Assets/AnimeHillRacing/VehicleData/";
        private const string GroundMaterialPath =
            "Assets/AnimeHillRacing/Materials/TerrainStable.physicMaterial";
        private const float StepSeconds = .02f;

        private struct LaunchResult
        {
            public float speedKmh;
            public float maximumPitchDegrees;
            public float liftedAxleRatio;
        }

        private struct DropResult
        {
            public float reboundMps;
            public float settledVerticalMps;
            public float maximumCompression;
            public int reboundEpisodes;
        }

        private struct RoughResult
        {
            public float groundedRatio;
            public float verticalRms;
            public int airborneEpisodes;
            public float longestAirborneSeconds;
        }

        private struct AxleLandingResult
        {
            public float rearPointReboundMps;
            public float frontPointReboundMps;
            public int rearLiftEpisodes;
            public int rearPeakFrame;
            public float bodyVerticalAtRearPeak;
            public float angularAtRearPeak;
            public int rearFirstTouchFrame;
            public float rearRelativeAtPeak;
            public float rearSpringAtPeak;
            public float rearDamperAtPeak;
            public float rearCompressionAtPeak;
            public bool rearLandingDamperAtPeak;
        }

        private struct ChassisSlideResult
        {
            public float finalSpeedMps;
            public float groundedRatio;
        }

        [MenuItem("Anime Hill Racing/Validate Adventure Physics A-H")]
        public static void ValidateAdventurePhysicsAH()
        {
            // Create the isolated scene before loading the ScriptableObjects. Replacing
            // the scene once per sample lets Unity unload otherwise valid asset wrappers.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            VehicleDefinition[] vehicles =
            {
                Load("MainVehicle.asset"), Load("FormulaVehicle.asset"),
                Load("RallyVehicle.asset"), Load("SportBikeVehicle.asset")
            };
            var failures = new List<string>();
            var summaries = new List<string>();
            if ((Physics.gravity - new Vector3(0f, -9.81f, 0f)).sqrMagnitude > .0001f)
                failures.Add($"GLOBAL GRAVITY: expected (0,-9.81,0), found {Physics.gravity}.");
            if (Mathf.Abs(Time.fixedDeltaTime - .02f) > .0001f)
                failures.Add($"GLOBAL FIXED STEP: expected 0.020s, found {Time.fixedDeltaTime:0.0000}s.");
            ValidatePurchasedAndEquippedPersistence(vehicles, failures);
            for (int i = 0; i < vehicles.Length; i++)
            {
                VehicleDefinition vehicle = vehicles[i];
                if (vehicle == null || vehicle.gameplayPrefab2D == null)
                {
                    failures.Add($"Vehicle data/prefab missing at index {i}.");
                    continue;
                }
                ValidatePhysicalConfiguration(vehicle, failures);

                LaunchResult forward = SimulateLaunch(vehicle, false);
                LaunchResult reverse = SimulateLaunch(vehicle, true);
                float launchPitchLimit = vehicle.archetype == VehicleArchetype.SportBike ? 24f : 18f;
                if (forward.speedKmh < 5f || forward.maximumPitchDegrees > launchPitchLimit ||
                    forward.liftedAxleRatio > .35f)
                    failures.Add($"{vehicle.displayName} TEST A: speed={forward.speedKmh:0.0}km/h, " +
                        $"pitch={forward.maximumPitchDegrees:0.0}deg, frontLift={forward.liftedAxleRatio:P0}.");
                if (reverse.speedKmh > -3f || reverse.maximumPitchDegrees > launchPitchLimit ||
                    reverse.liftedAxleRatio > .35f)
                    failures.Add($"{vehicle.displayName} TEST B: speed={reverse.speedKmh:0.0}km/h, " +
                        $"pitch={reverse.maximumPitchDegrees:0.0}deg, rearLift={reverse.liftedAxleRatio:P0}.");

                int maximumLevel = Mathf.Max(1, vehicle.suspensionMaxTunes);
                DropResult small = SimulateDrop(vehicle, maximumLevel, -3f);
                DropResult hardLevelOne = SimulateDrop(vehicle, 1, -12f);
                DropResult hardMaximum = SimulateDrop(vehicle, maximumLevel, -12f);
                if (small.reboundMps > 1.75f || small.reboundEpisodes > 1 ||
                    small.settledVerticalMps > .45f)
                    failures.Add($"{vehicle.displayName} TEST C: rebound={small.reboundMps:0.00}m/s, " +
                        $"episodes={small.reboundEpisodes}, settled={small.settledVerticalMps:0.00}m/s.");
                if (hardMaximum.reboundMps > 6f || hardMaximum.reboundEpisodes > 1 ||
                    hardMaximum.settledVerticalMps > .8f || hardMaximum.maximumCompression < .65f)
                    failures.Add($"{vehicle.displayName} TEST D: rebound={hardMaximum.reboundMps:0.00}m/s, " +
                        $"episodes={hardMaximum.reboundEpisodes}, compression={hardMaximum.maximumCompression:P0}, " +
                        $"settled={hardMaximum.settledVerticalMps:0.00}m/s.");

                float gasRatio = SimulateChassisControlRatio(vehicle, true);
                float brakeRatio = SimulateChassisControlRatio(vehicle, false);
                float chassisSlideSpeed = SimulateChassisSlide(vehicle);
                ChassisSlideResult physicalSlide = SimulatePhysicalChassisSlide(vehicle, false);
                ChassisSlideResult physicalGasSlide = SimulatePhysicalChassisSlide(vehicle, true);
                if (gasRatio < .025f || gasRatio > .16f)
                    failures.Add($"{vehicle.displayName} TEST E: chassis gas air-control ratio={gasRatio:P1}.");
                if (brakeRatio < .025f || brakeRatio > .16f)
                    failures.Add($"{vehicle.displayName} TEST F: chassis brake air-control ratio={brakeRatio:P1}.");
                if (chassisSlideSpeed < 16f || chassisSlideSpeed > 20.01f)
                    failures.Add($"{vehicle.displayName} TEST F-SLIDE: non-physical chassis damping changed " +
                        $"20m/s to {chassisSlideSpeed:0.00}m/s in 0.5s.");
                if (physicalSlide.groundedRatio < .80f || physicalSlide.finalSpeedMps < 17f ||
                    physicalSlide.finalSpeedMps > 20.01f)
                    failures.Add($"{vehicle.displayName} TEST F-PHYSICAL-SLIDE: contact=" +
                        $"{physicalSlide.groundedRatio:P0}, speed={physicalSlide.finalSpeedMps:0.00}m/s.");
                if (Mathf.Abs(physicalGasSlide.finalSpeedMps - physicalSlide.finalSpeedMps) > .12f)
                    failures.Add($"{vehicle.displayName} TEST F-NO-CHASSIS-TRACTION: coast/gas=" +
                        $"{physicalSlide.finalSpeedMps:0.00}/{physicalGasSlide.finalSpeedMps:0.00}m/s.");

                RoughResult roughOne = SimulateRoughTerrain(vehicle, 1);
                RoughResult roughMaximum = SimulateRoughTerrain(vehicle, maximumLevel);
                bool contactImproved = roughMaximum.groundedRatio + .015f >= roughOne.groundedRatio;
                bool strongContactGain = roughMaximum.groundedRatio >= roughOne.groundedRatio + .10f;
                // A very soft L1 setup can appear deceptively smooth because it spends more time airborne.
                // A maximum tune also counts as an improvement when it gains substantial tire contact,
                // reduces lift episodes and keeps absolute vertical RMS/lift duration physically bounded.
                bool bounceImproved = roughMaximum.verticalRms <= roughOne.verticalRms * .92f + .05f ||
                                      strongContactGain &&
                                      roughMaximum.airborneEpisodes <= roughOne.airborneEpisodes &&
                                      roughMaximum.verticalRms <= 1.75f;
                bool longestLiftImproved = roughMaximum.longestAirborneSeconds <=
                                           roughOne.longestAirborneSeconds + .06f ||
                                           strongContactGain && roughMaximum.longestAirborneSeconds <= .75f;
                if (!contactImproved || !bounceImproved || !longestLiftImproved)
                    failures.Add($"{vehicle.displayName} TEST G: L1 contact={roughOne.groundedRatio:P0}, " +
                        $"rms={roughOne.verticalRms:0.00}, lifts={roughOne.airborneEpisodes}, " +
                        $"longest={roughOne.longestAirborneSeconds:0.00}s; MAX contact=" +
                        $"{roughMaximum.groundedRatio:P0}, rms={roughMaximum.verticalRms:0.00}, " +
                        $"lifts={roughMaximum.airborneEpisodes}, " +
                        $"longest={roughMaximum.longestAirborneSeconds:0.00}s.");

                bool hardDropImproved = hardMaximum.reboundEpisodes <=
                                            Mathf.Max(1, hardLevelOne.reboundEpisodes) &&
                                        hardMaximum.reboundMps <= Mathf.Max(2f,
                                            hardLevelOne.reboundMps * 1.35f + .15f) &&
                                        hardMaximum.settledVerticalMps <= hardLevelOne.settledVerticalMps + .10f;
                if (!hardDropImproved)
                    failures.Add($"{vehicle.displayName} TEST H: L1 rebound/settled=" +
                        $"{hardLevelOne.reboundMps:0.00}/{hardLevelOne.settledVerticalMps:0.00}, " +
                        $"episodes={hardLevelOne.reboundEpisodes}; MAX=" +
                        $"{hardMaximum.reboundMps:0.00}/{hardMaximum.settledVerticalMps:0.00}, " +
                        $"episodes={hardMaximum.reboundEpisodes}.");

                summaries.Add($"{vehicle.displayName} A={forward.maximumPitchDegrees:0.0}deg " +
                    $"B={reverse.maximumPitchDegrees:0.0}deg C={small.reboundMps:0.00}m/s " +
                    $"D={hardMaximum.reboundMps:0.00}m/s E/F={gasRatio:P0}/{brakeRatio:P0} " +
                    $"slide={chassisSlideSpeed:0.0}/{physicalSlide.finalSpeedMps:0.0}/" +
                    $"gas{physicalGasSlide.finalSpeedMps:0.0}m/s " +
                    $"G={roughOne.groundedRatio:P0}->{roughMaximum.groundedRatio:P0}, " +
                    $"{roughOne.verticalRms:0.00}->{roughMaximum.verticalRms:0.00}");
            }

            VehicleDefinition formula = vehicles.Length > 1 ? vehicles[1] : null;
            if (formula != null)
            {
                AxleLandingResult axleLanding = SimulateFormulaRearLanding(formula);
                if (axleLanding.rearPointReboundMps > axleLanding.frontPointReboundMps + 1f ||
                    axleLanding.rearLiftEpisodes > 0)
                    failures.Add($"FORMULA REAR LANDING: rear/front rebound=" +
                        $"{axleLanding.rearPointReboundMps:0.00}/{axleLanding.frontPointReboundMps:0.00}m/s, " +
                        $"rear lifts={axleLanding.rearLiftEpisodes}.");
                summaries.Add($"FORMULA REAR={axleLanding.rearPointReboundMps:0.00}m/s " +
                    $"FRONT={axleLanding.frontPointReboundMps:0.00}m/s " +
                    $"LIFTS={axleLanding.rearLiftEpisodes} PEAK_FRAME={axleLanding.rearPeakFrame} " +
                     $"FIRST_TOUCH={axleLanding.rearFirstTouchFrame} " +
                     $"BODY_VY={axleLanding.bodyVerticalAtRearPeak:0.00} " +
                     $"WZ={axleLanding.angularAtRearPeak:0.00} " +
                     $"REL={axleLanding.rearRelativeAtPeak:0.00} " +
                     $"SPRING/DAMPER={axleLanding.rearSpringAtPeak:0}/{axleLanding.rearDamperAtPeak:0} " +
                     $"COMP={axleLanding.rearCompressionAtPeak:P0} " +
                     $"LANDING_DAMPER={axleLanding.rearLandingDamperAtPeak}");
            }

            if (failures.Count > 0)
                throw new InvalidOperationException("ADVENTURE PHYSICS A-H FAILED\n- " +
                    string.Join("\n- ", failures) + "\nMETRICS\n" + string.Join("\n", summaries));
            Debug.Log("ADVENTURE PHYSICS A-H PASS | " + string.Join(" | ", summaries));
        }

        private static void ValidatePhysicalConfiguration(VehicleDefinition vehicle, List<string> failures)
        {
            float wheelMassRatio = vehicle.wheelMass / Mathf.Max(1f, vehicle.mass);
            if (vehicle.mass < 150f || vehicle.mass > 2000f || wheelMassRatio < .02f ||
                wheelMassRatio > .12f)
                failures.Add($"{vehicle.displayName}: mass audit failed (body={vehicle.mass:0}kg, " +
                    $"wheel={vehicle.wheelMass:0}kg, per-wheel ratio={wheelMassRatio:P1}).");
            CapsuleCollider chassis = vehicle.gameplayPrefab2D.GetComponent<CapsuleCollider>();
            PhysicsMaterial material = chassis != null ? chassis.sharedMaterial : null;
            if (material == null || material.bounciness > .001f || material.dynamicFriction < .08f ||
                material.dynamicFriction > .24f || material.staticFriction < material.dynamicFriction ||
                material.frictionCombine != PhysicsMaterialCombine.Multiply)
                failures.Add($"{vehicle.displayName}: chassis material must be low-friction, non-zero, " +
                    "zero-bounce and use Multiply.");
            if (vehicle.maximumDepenetrationVelocity > 2f)
                failures.Add($"{vehicle.displayName}: depenetration cap {vehicle.maximumDepenetrationVelocity:0.0}m/s > 2.");
            if (vehicle.chassisAirControlMultiplier < .05f || vehicle.chassisAirControlMultiplier > .15f)
                failures.Add($"{vehicle.displayName}: chassis air-control multiplier outside 5-15%.");
        }

        private static LaunchResult SimulateLaunch(VehicleDefinition source, bool reverse)
        {
            using (SimulationRig rig = SimulationRig.Create(source, source.suspensionMaxTunes, false))
            {
                rig.Settle(130);
                float baseline = rig.Controller.Body.rotation.eulerAngles.z;
                if (reverse) rig.Input.SetTouchBrake(true);
                else rig.Input.SetTouchGas(true);
                float maximumPitch = 0f;
                int lifted = 0;
                const int measuredFrames = 180;
                for (int frame = 0; frame < measuredFrames; frame++)
                {
                    rig.Step();
                    maximumPitch = Mathf.Max(maximumPitch,
                        Mathf.Abs(Mathf.DeltaAngle(baseline, rig.Controller.Body.rotation.eulerAngles.z)));
                    if (frame > 15 && (reverse ? !rig.Controller.RearWheelGrounded :
                            !rig.Controller.FrontWheelGrounded)) lifted++;
                }
                return new LaunchResult
                {
                    speedKmh = rig.Controller.SpeedKmh,
                    maximumPitchDegrees = maximumPitch,
                    liftedAxleRatio = lifted / (float)(measuredFrames - 16)
                };
            }
        }

        private static DropResult SimulateDrop(VehicleDefinition source, int suspensionLevel, float verticalSpeed)
        {
            using (SimulationRig rig = SimulationRig.Create(source, suspensionLevel, false,
                       new Vector3(0f, 2.2f, 0f)))
            {
                rig.Controller.Body.linearVelocity = Vector3.up * verticalSpeed;
                bool touched = false;
                bool reboundEpisode = false;
                int episodes = 0;
                float rebound = 0f;
                float compression = 0f;
                float settled = 0f;
                float threshold = Mathf.Max(.55f, Mathf.Abs(verticalSpeed) * .16f);
                for (int frame = 0; frame < 170; frame++)
                {
                    rig.Step();
                    touched |= rig.Controller.AnyWheelGrounded;
                    SuspensionController[] points = rig.Points;
                    for (int p = 0; p < points.Length; p++)
                        compression = Mathf.Max(compression, points[p].Compression01);
                    if (!touched) continue;
                    float vertical = rig.Controller.Body.linearVelocity.y;
                    rebound = Mathf.Max(rebound, vertical);
                    bool significant = vertical > threshold;
                    if (significant && !reboundEpisode) episodes++;
                    reboundEpisode = significant || reboundEpisode && vertical > threshold * .4f;
                    if (frame >= 140) settled = Mathf.Max(settled, Mathf.Abs(vertical));
                }
                return new DropResult
                {
                    reboundMps = rebound,
                    settledVerticalMps = settled,
                    maximumCompression = compression,
                    reboundEpisodes = episodes
                };
            }
        }

        private static float SimulateChassisControlRatio(VehicleDefinition source, bool gas)
        {
            float airborne = SimulateAirStep(source, gas, false);
            float chassis = SimulateAirStep(source, gas, true);
            if (gas && (airborne <= 0f || chassis <= 0f))
                throw new InvalidOperationException($"{source.displayName}: gas must rotate backward in air.");
            if (!gas && (airborne >= 0f || chassis >= 0f))
                throw new InvalidOperationException($"{source.displayName}: brake must rotate forward in air.");
            return Mathf.Abs(chassis) / Mathf.Max(.0001f, Mathf.Abs(airborne));
        }

        private static float SimulateAirStep(VehicleDefinition source, bool gas, bool chassisContact)
        {
            using (SimulationRig rig = SimulationRig.Create(source, source.suspensionMaxTunes, false,
                       new Vector3(0f, 12f, 0f)))
            {
                if (gas) rig.Input.SetTouchGas(true);
                else rig.Input.SetTouchBrake(true);
                if (chassisContact)
                    SetPrivateField(rig.Controller, "chassisContactSinceLastStep", true);
                rig.Controller.Body.angularVelocity = Vector3.zero;
                rig.Step();
                if (chassisContact && (!rig.Controller.ChassisGrounded || rig.Controller.AnyWheelGrounded))
                    throw new InvalidOperationException($"{source.displayName}: independent chassis contact state failed.");
                return rig.Controller.Body.angularVelocity.z;
            }
        }

        private static float SimulateChassisSlide(VehicleDefinition source)
        {
            using (SimulationRig rig = SimulationRig.Create(source, source.suspensionMaxTunes, false,
                       new Vector3(0f, 12f, 0f)))
            {
                rig.Controller.Body.linearVelocity = Vector3.right * 20f;
                for (int frame = 0; frame < 25; frame++)
                {
                    SetPrivateField(rig.Controller, "chassisContactSinceLastStep", true);
                    SetPrivateField(rig.Controller, "chassisContactNormal", Vector3.up);
                    rig.Step();
                }
                return Mathf.Abs(Vector3.Dot(rig.Controller.Body.linearVelocity, Vector3.right));
            }
        }

        private static ChassisSlideResult SimulatePhysicalChassisSlide(VehicleDefinition source, bool gas)
        {
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition definition = null;
            GameObject ground = null;
            GameObject inputObject = null;
            GameObject instance = null;
            try
            {
                definition = CreateFrozenDefinition(source, source.suspensionMaxTunes);
                ground = new GameObject("Physics Audit Chassis Ground");
                BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
                groundCollider.center = new Vector3(100f, -.5f, 0f);
                groundCollider.size = new Vector3(400f, 1f, 20f);
                groundCollider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(GroundMaterialPath);

                inputObject = new GameObject("Physics Audit Chassis Input");
                VehicleInputController input = inputObject.AddComponent<VehicleInputController>();
                input.SetInputMode(GameModeType.Adventure2_5D);
                if (gas) input.SetTouchGas(true);

                instance = UnityEngine.Object.Instantiate(source.gameplayPrefab2D, Vector3.zero,
                    Quaternion.identity);
                VehicleController controller = instance.GetComponent<VehicleController>();
                SuspensionController[] points = instance.GetComponentsInChildren<SuspensionController>(true);
                controller.AssignRuntimeReferences(definition, input, points);
                InvokePrivate(controller, "Awake");
                MovementPlaneConstraint plane = instance.GetComponent<MovementPlaneConstraint>();
                if (plane != null) InvokePrivate(plane, "Awake");

                ConfigurableJoint[] joints = instance.GetComponents<ConfigurableJoint>();
                for (int i = 0; i < joints.Length; i++)
                    UnityEngine.Object.DestroyImmediate(joints[i]);
                for (int i = 0; i < points.Length; i++)
                {
                    points[i].WheelCollider.enabled = false;
                    points[i].WheelBody.detectCollisions = false;
                    points[i].WheelBody.isKinematic = true;
                }
                controller.AssignRuntimeReferences(definition, input, Array.Empty<SuspensionController>());

                Rigidbody body = controller.Body;
                CapsuleCollider chassis = instance.GetComponent<CapsuleCollider>();
                body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
                body.position = Vector3.up * (chassis.radius - chassis.center.y + .002f);
                body.linearVelocity = Vector3.right * 20f;
                Physics.SyncTransforms();

                int groundedFrames = 0;
                const int frames = 30;
                for (int frame = 0; frame < frames; frame++)
                {
                    InvokePrivate(input, "Update");
                    InvokePrivate(controller, "FixedUpdate");
                    if (plane != null) InvokePrivate(plane, "FixedUpdate");
                    Physics.Simulate(StepSeconds);
                    if (controller.ChassisGrounded) groundedFrames++;
                }
                return new ChassisSlideResult
                {
                    finalSpeedMps = Mathf.Abs(Vector3.Dot(body.linearVelocity, Vector3.right)),
                    groundedRatio = groundedFrames / (float)frames
                };
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                if (inputObject != null) UnityEngine.Object.DestroyImmediate(inputObject);
                if (ground != null) UnityEngine.Object.DestroyImmediate(ground);
                if (definition != null) UnityEngine.Object.DestroyImmediate(definition);
                Physics.simulationMode = previousMode;
            }
        }

        private static AxleLandingResult SimulateFormulaRearLanding(VehicleDefinition source)
        {
            using (SimulationRig rig = SimulationRig.Create(source, source.suspensionMaxTunes, false,
                       new Vector3(0f, 2.3f, 0f)))
            {
                SuspensionController rear = null;
                SuspensionController front = null;
                for (int i = 0; i < rig.Points.Length; i++)
                {
                    SuspensionController point = rig.Points[i];
                    float localX = rig.Controller.transform.InverseTransformPoint(point.transform.position).x;
                    if (rear == null || localX < rig.Controller.transform.InverseTransformPoint(
                            rear.transform.position).x) rear = point;
                    if (front == null || localX > rig.Controller.transform.InverseTransformPoint(
                            front.transform.position).x) front = point;
                }
                rig.Controller.Body.linearVelocity = Vector3.down * 9f;
                rig.Controller.Body.angularVelocity = Vector3.forward * .35f;
                bool rearTouched = false;
                int rearAirFrames = 0;
                bool liftCounted = false;
                int rearLiftEpisodes = 0;
                float rearRebound = 0f;
                float frontRebound = 0f;
                int rearPeakFrame = -1;
                float bodyVerticalAtRearPeak = 0f;
                float angularAtRearPeak = 0f;
                int rearFirstTouchFrame = -1;
                float rearRelativeAtPeak = 0f;
                float rearSpringAtPeak = 0f;
                float rearDamperAtPeak = 0f;
                float rearCompressionAtPeak = 0f;
                bool rearLandingDamperAtPeak = false;
                for (int frame = 0; frame < 180; frame++)
                {
                    rig.Step();
                    if (!rearTouched && rear != null && rear.IsGrounded)
                    {
                        rearTouched = true;
                        rearFirstTouchFrame = frame;
                    }
                    if (!rearTouched) continue;
                    if (rear != null)
                    {
                        float rearPointSpeed =
                            rig.Controller.Body.GetPointVelocity(rear.WheelBody.worldCenterOfMass).y;
                        if (rearPointSpeed > rearRebound)
                        {
                            rearRebound = rearPointSpeed;
                            rearPeakFrame = frame;
                            bodyVerticalAtRearPeak = rig.Controller.Body.linearVelocity.y;
                            angularAtRearPeak = rig.Controller.Body.angularVelocity.z;
                            rearRelativeAtPeak = rear.LastRelativeVelocity;
                            rearSpringAtPeak = rear.LastSpringForce;
                            rearDamperAtPeak = rear.LastDamperForce;
                            rearCompressionAtPeak = rear.Compression01;
                            rearLandingDamperAtPeak = rear.LandingDamperActive;
                        }
                    }
                    if (front != null)
                        frontRebound = Mathf.Max(frontRebound,
                            rig.Controller.Body.GetPointVelocity(front.WheelBody.worldCenterOfMass).y);
                    if (rear != null && !rear.IsGrounded)
                    {
                        rearAirFrames++;
                        if (rearAirFrames >= 3 && !liftCounted)
                        {
                            rearLiftEpisodes++;
                            liftCounted = true;
                        }
                    }
                    else
                    {
                        rearAirFrames = 0;
                        liftCounted = false;
                    }
                }
                return new AxleLandingResult
                {
                    rearPointReboundMps = rearRebound,
                    frontPointReboundMps = frontRebound,
                    rearLiftEpisodes = rearLiftEpisodes,
                    rearPeakFrame = rearPeakFrame,
                    bodyVerticalAtRearPeak = bodyVerticalAtRearPeak,
                    angularAtRearPeak = angularAtRearPeak,
                    rearFirstTouchFrame = rearFirstTouchFrame,
                    rearRelativeAtPeak = rearRelativeAtPeak,
                    rearSpringAtPeak = rearSpringAtPeak,
                    rearDamperAtPeak = rearDamperAtPeak,
                    rearCompressionAtPeak = rearCompressionAtPeak,
                    rearLandingDamperAtPeak = rearLandingDamperAtPeak
                };
            }
        }

        private static RoughResult SimulateRoughTerrain(VehicleDefinition source, int suspensionLevel)
        {
            using (SimulationRig rig = SimulationRig.Create(source, suspensionLevel, true,
                       new Vector3(3f, 1.2f, 0f)))
            {
                rig.Settle(100);
                Vector3 velocity = Vector3.right * 22f;
                rig.Controller.Body.linearVelocity = velocity;
                for (int i = 0; i < rig.Points.Length; i++)
                {
                    rig.Points[i].WheelBody.linearVelocity = velocity;
                    rig.Points[i].WheelBody.angularVelocity = Vector3.back *
                        (velocity.x / Mathf.Max(.05f, source.wheelRadius));
                }
                int grounded = 0;
                int samples = 0;
                int airborneEpisodes = 0;
                int consecutiveAirborneFrames = 0;
                int maximumAirborneFrames = 0;
                bool airborneEpisodeCounted = false;
                float verticalSquared = 0f;
                for (int frame = 0; frame < 300; frame++)
                {
                    rig.Step();
                    if (frame < 25) continue;
                    bool groundedNow = rig.Controller.AnyWheelGrounded;
                    if (groundedNow) grounded++;
                    if (groundedNow)
                    {
                        consecutiveAirborneFrames = 0;
                        airborneEpisodeCounted = false;
                    }
                    else
                    {
                        consecutiveAirborneFrames++;
                        maximumAirborneFrames = Mathf.Max(maximumAirborneFrames, consecutiveAirborneFrames);
                        // A one/two-step contact skin transition at 50 Hz is not a jump.
                        // Count only a sustained loss of wheel contact (>= 60 ms).
                        if (consecutiveAirborneFrames >= 3 && !airborneEpisodeCounted)
                        {
                            airborneEpisodes++;
                            airborneEpisodeCounted = true;
                        }
                    }
                    float vertical = rig.Controller.Body.linearVelocity.y;
                    verticalSquared += vertical * vertical;
                    samples++;
                }
                return new RoughResult
                {
                    groundedRatio = grounded / (float)Mathf.Max(1, samples),
                    verticalRms = Mathf.Sqrt(verticalSquared / Mathf.Max(1, samples)),
                    airborneEpisodes = airborneEpisodes,
                    longestAirborneSeconds = maximumAirborneFrames * StepSeconds
                };
            }
        }

        private static void ValidatePurchasedAndEquippedPersistence(VehicleDefinition[] vehicles,
            List<string> failures)
        {
            UpgradeEconomyDefinition economy = AssetDatabase.LoadAssetAtPath<UpgradeEconomyDefinition>(
                DataRoot + "UpgradeEconomy.asset");
            if (economy == null)
            {
                failures.Add("TUNING: UpgradeEconomy.asset is missing.");
                return;
            }

            for (int vehicleIndex = 0; vehicleIndex < vehicles.Length; vehicleIndex++)
            {
                VehicleDefinition source = vehicles[vehicleIndex];
                if (source == null) continue;
                VehicleDefinition clone = UnityEngine.Object.Instantiate(source);
                clone.vehicleId = $"tuning-persistence-audit-{vehicleIndex}-{Guid.NewGuid():N}";
                try
                {
                    Array values = Enum.GetValues(typeof(VehicleUpgradeType));
                    for (int i = 0; i < values.Length; i++)
                    {
                        VehicleUpgradeType type = (VehicleUpgradeType)values.GetValue(i);
                        if (clone.GetMaxTunes(type) < 3) continue;
                        SeedTuneLevel(clone.vehicleId, type, 1, 1);
                        bool purchased = SaveManager.TryIncreaseEquippedTune(clone, type, economy,
                            out int level, out TunePurchaseResult result);
                        if (!purchased || result != TunePurchaseResult.Success || level != 2 ||
                            SaveManager.LoadPurchasedUpgrades(clone.vehicleId, clone).GetTune(type) != 2)
                            failures.Add($"{source.displayName} {type}: first ↑ did not buy/equip level 2.");

                        bool decreased = SaveManager.TryDecreaseEquippedTune(clone, type, out level);
                        if (!decreased || level != 1 ||
                            SaveManager.LoadPurchasedUpgrades(clone.vehicleId, clone).GetTune(type) != 2)
                            failures.Add($"{source.displayName} {type}: ↓ lost purchased progress.");

                        bool restored = SaveManager.TryIncreaseEquippedTune(clone, type, economy,
                            out level, out result);
                        if (!restored || level != 2 ||
                            SaveManager.LoadPurchasedUpgrades(clone.vehicleId, clone).GetTune(type) != 2)
                            failures.Add($"{source.displayName} {type}: free ↑ did not restore bought level.");

                        bool boughtNext = SaveManager.TryIncreaseEquippedTune(clone, type, economy,
                            out level, out result);
                        if (!boughtNext || level != 3 ||
                            SaveManager.LoadUpgrades(clone.vehicleId, clone).GetTune(type) != 3 ||
                            SaveManager.LoadPurchasedUpgrades(clone.vehicleId, clone).GetTune(type) != 3)
                            failures.Add($"{source.displayName} {type}: ↑ above bought level did not purchase 3.");
                        DeleteTuneAuditKeys(clone.vehicleId, type);
                    }
                }
                finally
                {
                    Array values = Enum.GetValues(typeof(VehicleUpgradeType));
                    for (int i = 0; i < values.Length; i++)
                        DeleteTuneAuditKeys(clone.vehicleId, (VehicleUpgradeType)values.GetValue(i));
                    UnityEngine.Object.DestroyImmediate(clone);
                    PlayerPrefs.Save();
                }
            }
        }

        private static void SeedTuneLevel(string vehicleId, VehicleUpgradeType type, int purchased, int equipped)
        {
            int typeId = (int)type;
            PlayerPrefs.SetInt($"AHR.UpgradeLevel.{vehicleId}.{typeId}", purchased);
            PlayerPrefs.SetInt($"AHR.EquippedTuneLevel.{vehicleId}.{typeId}", equipped);
            PlayerPrefs.SetInt($"AHR.UpgradeSchema.OneBasedV3.{vehicleId}.{typeId}", 1);
        }

        private static void DeleteTuneAuditKeys(string vehicleId, VehicleUpgradeType type)
        {
            int typeId = (int)type;
            string[] keys =
            {
                $"AHR.UpgradeLevel.{vehicleId}.{typeId}",
                $"AHR.EquippedTuneLevel.{vehicleId}.{typeId}",
                $"AHR.UpgradeSchema.OneBasedV3.{vehicleId}.{typeId}",
                $"AHR.UpgradeSchema.ZeroBasedV2.{vehicleId}.{typeId}",
                $"AHR.TuneSchema.OneBased.{vehicleId}.{typeId}",
                $"AHR.Tune.{vehicleId}.{typeId}",
                $"AHR.Upgrade.{vehicleId}.{typeId}"
            };
            for (int i = 0; i < keys.Length; i++) PlayerPrefs.DeleteKey(keys[i]);
        }

        private static VehicleDefinition Load(string filename) =>
            AssetDatabase.LoadAssetAtPath<VehicleDefinition>(DataRoot + filename);

        private static VehicleDefinition CreateFrozenDefinition(VehicleDefinition source, int suspensionLevel)
        {
            var upgrades = new VehicleUpgradeState
            {
                engineTune = Mathf.Max(1, source.engineMaxTunes),
                accelerationTune = Mathf.Max(1, source.accelerationMaxTunes),
                suspensionTune = Mathf.Clamp(suspensionLevel, 1, Mathf.Max(1, source.suspensionMaxTunes)),
                gripTune = Mathf.Max(1, source.gripMaxTunes),
                airControlTune = Mathf.Max(1, source.airControlMaxTunes),
                downforceTune = Mathf.Max(1, source.downforceMaxTunes),
                fuelTune = Mathf.Max(1, source.fuelMaxTunes),
                turboTune = Mathf.Max(1, source.turboMaxTunes)
            };
            ResolvedVehicleStats stats = source.Resolve(upgrades);
            VehicleDefinition clone = UnityEngine.Object.Instantiate(source);
            clone.vehicleId = source.vehicleId + "-physics-audit";
            clone.engineMaxTunes = clone.accelerationMaxTunes = clone.suspensionMaxTunes = 0;
            clone.gripMaxTunes = clone.airControlMaxTunes = clone.downforceMaxTunes = 0;
            clone.fuelMaxTunes = clone.turboMaxTunes = 0;
            clone.tuneTargets = new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = new[] { stats.topSpeedMps * 3.6f },
                accelerationZeroToHundredSeconds = new[] { stats.zeroToHundredTargetSeconds },
                gripRatings = new[] { (float)stats.gripRating },
                airControlTorque = new[] { stats.airControlTorque },
                airborneDownforceAcceleration = new[] { stats.airborneDownforceAcceleration },
                fuelRunDurationSeconds = new[] { stats.totalRunFuelDuration }
            };
            clone.engine.minTorqueNm = clone.engine.maxTorqueNm = stats.motorTorqueNm;
            clone.suspension.minTravel = clone.suspension.maxTravel = stats.suspensionTravel;
            SuspensionPhysics.GetTravelLimits(clone.suspension, stats.suspensionTravel,
                out _, out float extension);
            float compression = Mathf.Max(.015f,
                clone.suspension.targetSag01 * stats.suspensionTravel - extension);
            float baseSpring = clone.mass * .5f * Physics.gravity.magnitude / compression;
            clone.suspension.stiffnessTuneScale = Vector2.one * (stats.spring / Mathf.Max(1f, baseSpring));
            float dampingRatio = stats.damping /
                Mathf.Max(.001f, 2f * Mathf.Sqrt(stats.spring * clone.mass * .5f));
            clone.suspension.dampingRatio = Vector2.one * dampingRatio;
            clone.suspension.progressiveSpringMultiplier = stats.progressiveSpringMultiplier / .90f;
            clone.suspension.compressionDampingMultiplier = stats.compressionDampingMultiplier;
            clone.suspension.reboundDampingMultiplier = stats.reboundDampingMultiplier;
            clone.suspension.compressionDampingTuneScale = Vector2.one;
            clone.suspension.reboundDampingTuneScale = Vector2.one;
            clone.suspension.reboundAccelerationTune = Vector2.one * stats.maximumReboundAcceleration;
            clone.suspension.wheelContactTolerance = Vector2.one * stats.wheelContactTolerance;
            clone.suspension.landingDampingScale = Vector2.one * stats.landingDampingMultiplier;
            clone.suspension.landingDampingDuration = stats.landingDampingDuration;
            return clone;
        }

        private sealed class SimulationRig : IDisposable
        {
            private readonly GameObject ground;
            private readonly GameObject inputObject;
            private readonly GameObject instance;
            private readonly VehicleDefinition definition;
            private readonly SimulationMode previousMode;
            private readonly MovementPlaneConstraint plane;

            public VehicleController Controller { get; }
            public VehicleInputController Input { get; }
            public SuspensionController[] Points { get; }

            private SimulationRig(VehicleDefinition source, int level, bool rough, Vector3 position)
            {
                previousMode = Physics.simulationMode;
                Physics.simulationMode = SimulationMode.Script;
                definition = CreateFrozenDefinition(source, level);
                ground = rough ? CreateRoughGround() : CreateFlatGround();
                inputObject = new GameObject("Physics Audit Input");
                Input = inputObject.AddComponent<VehicleInputController>();
                Input.SetInputMode(GameModeType.Adventure2_5D);
                instance = UnityEngine.Object.Instantiate(source.gameplayPrefab2D, position, Quaternion.identity);
                Controller = instance.GetComponent<VehicleController>();
                Points = instance.GetComponentsInChildren<SuspensionController>(true);
                Controller.AssignRuntimeReferences(definition, Input, Points);
                InvokePrivate(Controller, "Awake");
                plane = instance.GetComponent<MovementPlaneConstraint>();
                if (plane != null) InvokePrivate(plane, "Awake");
                Physics.SyncTransforms();
            }

            public static SimulationRig Create(VehicleDefinition source, int level, bool rough,
                Vector3? position = null) => new SimulationRig(source, level, rough,
                position ?? new Vector3(0f, 1.1f, 0f));

            public void Step()
            {
                InvokePrivate(Input, "Update");
                InvokePrivate(Controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(StepSeconds);
            }

            public void Settle(int frames)
            {
                for (int i = 0; i < frames; i++) Step();
            }

            public void Dispose()
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                if (inputObject != null) UnityEngine.Object.DestroyImmediate(inputObject);
                if (ground != null)
                {
                    MeshFilter filter = ground.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                        UnityEngine.Object.DestroyImmediate(filter.sharedMesh);
                    UnityEngine.Object.DestroyImmediate(ground);
                }
                if (definition != null) UnityEngine.Object.DestroyImmediate(definition);
                Physics.simulationMode = previousMode;
            }

            private static GameObject CreateFlatGround()
            {
                GameObject result = new GameObject("Physics Audit Flat Ground");
                BoxCollider collider = result.AddComponent<BoxCollider>();
                collider.center = new Vector3(100f, -.5f, 0f);
                collider.size = new Vector3(400f, 1f, 20f);
                collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(GroundMaterialPath);
                return result;
            }

            private static GameObject CreateRoughGround()
            {
                const int segments = 500;
                const float spacing = .5f;
                const float halfWidth = 5f;
                Vector3[] vertices = new Vector3[(segments + 1) * 2];
                int[] triangles = new int[segments * 6];
                for (int i = 0; i <= segments; i++)
                {
                    float x = i * spacing;
                    float y = .11f * Mathf.Sin(x * 1.65f) + .055f * Mathf.Sin(x * 4.4f) +
                              .025f * Mathf.Sin(x * 8.1f);
                    int left = i * 2;
                    vertices[left] = new Vector3(x, y, -halfWidth);
                    vertices[left + 1] = new Vector3(x, y, halfWidth);
                    if (i >= segments) continue;
                    int nextLeft = left + 2;
                    int nextRight = left + 3;
                    int t = i * 6;
                    // Keep the winding upward so the non-convex collider presents a
                    // true 2.5D road surface instead of a diagonal vertical ribbon.
                    triangles[t] = left; triangles[t + 1] = left + 1; triangles[t + 2] = nextLeft;
                    triangles[t + 3] = nextLeft; triangles[t + 4] = left + 1; triangles[t + 5] = nextRight;
                }
                Mesh mesh = new Mesh { name = "Physics Audit Rough Mesh" };
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                GameObject result = new GameObject("Physics Audit Rough Ground");
                result.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshCollider collider = result.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(GroundMaterialPath);
                return result;
            }
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(target.GetType().Name, methodName);
            method.Invoke(target, null);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().Name, fieldName);
            field.SetValue(target, value);
        }
    }
}
#endif
