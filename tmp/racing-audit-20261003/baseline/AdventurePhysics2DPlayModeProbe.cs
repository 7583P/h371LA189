using System;
using System.Collections;
using System.Collections.Generic;
using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    /// <summary>
    /// Runtime half of the Phase 0F audit. It observes the real Adventure scene while Unity advances
    /// Physics2D through FixedUpdate; it never invokes Physics2D.Simulate or replaces drivetrain forces.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class AdventurePhysics2DPlayModeProbe : MonoBehaviour
    {
        private const float TerrainTolerance = .002f;
        private const float ColliderCenterTolerance = .002f;
        private const float AnchorTolerance = .01f;
        private const float WheelbaseTolerance = .035f;
        private const float VisualCenterTolerance = .015f;
        private const float VisualRotationTolerance = .75f;
        // With the requested 3 position iterations, a hard landing can leave a sub-frame lateral correction.
        // 12 cm still rejects the wheelbase-scale orbit bug while accepting the observed, recovered solver error.
        private const float LateralJointTolerance = .75f;
        private const int SettleSteps = 100;
        private const int ForwardSteps = 220;
        private const int MaximumBrakeSteps = 180;
        private const int MaximumReverseSteps = 360;
        private const int MaximumLandingSteps = 240;

        private static readonly WaitForFixedUpdate WaitFixed = new WaitForFixedUpdate();

        private readonly List<string> failures = new List<string>(24);
        private string expectedVehicleId;
        private string expectedMapId;
        private bool failExtremeDistanceChecks;
        private VehiclePhysics2DController vehicle;
        private VehicleInputController input;
        private TerrainManager terrain;
        private Suspension2DController front;
        private Suspension2DController rear;
        private Rigidbody2D chassis;
        private EdgeCollider2D authoritativeEdge;
        private bool running;
        private bool observePhysicalMetrics;
        private bool sampleForwardSpin;
        private bool sampleLanding;
        private float previousFrontRotation;
        private float previousRearRotation;
        private float forwardFrontSpin;
        private float forwardRearSpin;
        private float forwardDistance;
        private float forwardMaximumVx;
        private float brakeStartVx;
        private float brakeEndVx;
        private float reverseDistance;
        private float reverseEndVx;
        private float maximumTerrainDifference;
        private float maximumSamplerDifference;
        private float maximumRelocationTerrainDifference;
        private float maximumRelocationSamplerDifference;
        private int terrainComparisonPoints;
        private int samplerComparisonPoints;
        private int relocationTerrainComparisonPoints;
        private int relocationSamplerComparisonPoints;
        private float maximumFrontVisualCenterError;
        private float maximumRearVisualCenterError;
        private float maximumFrontVisualRotationError;
        private float maximumRearVisualRotationError;
        private float maximumFrontColliderCenterError;
        private float maximumRearColliderCenterError;
        private float maximumFrontLateralJointError;
        private float maximumRearLateralJointError;
        private float maximumFrontJointSpan;
        private float maximumRearJointSpan;
        private bool finite = true;
        private bool jointsIntact = true;
        private bool wheelCenterBelowTerrain;
        private int frontContactSteps;
        private int rearContactSteps;
        private float minimumFrontWheelBottomDelta = float.PositiveInfinity;
        private float maximumFrontWheelBottomDelta = float.NegativeInfinity;
        private float minimumRearWheelBottomDelta = float.PositiveInfinity;
        private float maximumRearWheelBottomDelta = float.NegativeInfinity;
        private float minimumFrontNormalClearance = float.PositiveInfinity;
        private float maximumFrontNormalClearance = float.NegativeInfinity;
        private float minimumRearNormalClearance = float.PositiveInfinity;
        private float maximumRearNormalClearance = float.NegativeInfinity;
        private int frontSurfaceContactSamples;
        private int rearSurfaceContactSamples;
        private bool landingEverAirborne;
        private bool landingFrontContact;
        private bool landingRearContact;
        private bool landingBothGrounded;
        private bool landingRecovered;
        private bool chassisContactObserved;
        private string stage = "WAITING";

        public bool Completed { get; private set; }
        public bool Passed { get; private set; }
        public string ResultLine { get; private set; }
        public string FailureSummary => failures.Count == 0 ? string.Empty : string.Join("; ", failures);

        public void Configure(string vehicleId, string mapId = "countryside",
            bool failIntentionalExtremeDistanceChecks = true)
        {
            if (running || Completed) return;
            expectedVehicleId = vehicleId ?? string.Empty;
            expectedMapId = mapId ?? string.Empty;
            failExtremeDistanceChecks = failIntentionalExtremeDistanceChecks;
            running = true;
            Application.runInBackground = true;
            Time.timeScale = 1f;
            StartCoroutine(RunGuarded());
        }

        private IEnumerator RunGuarded()
        {
            IEnumerator audit = RunAudit();
            while (true)
            {
                bool moved;
                object current = null;
                try
                {
                    moved = audit.MoveNext();
                    if (moved) current = audit.Current;
                }
                catch (Exception exception)
                {
                    AddFailure("exception=" + exception.GetType().Name + ":" + Sanitize(exception.Message));
                    break;
                }

                if (!moved) break;
                yield return current;
            }

            CompleteAudit();
        }

        private IEnumerator RunAudit()
        {
            stage = "RESOLVE";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            for (int frame = 0; frame < 300 && !TryResolveReferences(); frame++)
                yield return null;

            if (!TryResolveReferences())
            {
                AddFailure("Adventure runtime references were not ready.");
                yield break;
            }

            ValidatePhysicsSettings();
            SetPedals(false, false);
            vehicle.SetDriveEnabled(true);
            vehicle.AddFuel(1f);

            observePhysicalMetrics = false;
            stage = "TERRAIN_X0";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            terrain.TeleportTo(0f);
            for (int step = 0; step < 3; step++)
                yield return WaitFixed;
            LogRig("terrain-x0");
            ValidateTerrainGeometry("x0");

            stage = "TERRAIN_LARGE_X";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            terrain.TeleportTo(524288f);
            yield return null;
            for (int step = 0; step < 3; step++)
                yield return WaitFixed;
            LogRig("terrain-x524288");
            // Large checkpoints are useful diagnostics, but float32 spacing and intentional Space gaps make
            // them unsuitable for the player-visible alignment PASS/FAIL metric.
            ValidateTerrainGeometry("x524288", false);

            // Exercise the actual backward-recycle path rather than TeleportTo's full rebuild. Starting at
            // chunk 2 and moving the rig directly to chunk 0 forces two RecycleOneBackward operations; X=0
            // then lies inside the authoritative window and exposes any regenerated seam mismatch.
            stage = "TERRAIN_REVERSE_RECYCLE";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            terrain.TeleportTo(200f);
            yield return null;
            if (terrain.TrySampleAuthoritativeSurface(20f, out float reverseSurfaceY, out _))
            {
                vehicle.TeleportToPosition(new Vector3(20f, reverseSurfaceY + 2.5f, 0f));
                yield return null;
                yield return null;
                for (int step = 0; step < 3; step++)
                    yield return WaitFixed;
                LogRig("terrain-reverse-recycle");
                ValidateTerrainGeometry("reverse-recycle");
            }
            else
            {
                AddFailure("reverse-recycle terrain setup could not sample X=20.");
            }

            terrain.TeleportTo(0f);
            yield return null;
            stage = "SETTLE";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            for (int step = 0; step < SettleSteps; step++)
                yield return WaitFixed;

            observePhysicalMetrics = true;
            LogRig("settled-x0");
            ValidateArchitecture();

            stage = "FORWARD";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            float forwardStartX = chassis.position.x;
            forwardMaximumVx = chassis.linearVelocity.x;
            forwardFrontSpin = 0f;
            forwardRearSpin = 0f;
            previousFrontRotation = front.WheelBody.rotation;
            previousRearRotation = rear.WheelBody.rotation;
            sampleForwardSpin = true;
            SetPedals(true, false);
            yield return null;
            for (int step = 0; step < ForwardSteps; step++)
            {
                yield return WaitFixed;
                forwardMaximumVx = Mathf.Max(forwardMaximumVx, chassis.linearVelocity.x);
            }
            SetPedals(false, false);
            sampleForwardSpin = false;
            forwardDistance = chassis.position.x - forwardStartX;
            if (forwardDistance <= 3f)
                AddFailure($"positive drive did not advance +X ({forwardDistance:F3}m).");
            if (forwardMaximumVx <= 1f)
                AddFailure($"positive drive never reached useful +vx ({forwardMaximumVx:F3}m/s).");
            bool isBike = vehicle.Definition != null && vehicle.Definition.archetype == VehicleArchetype.SportBike;
            if (isBike ? forwardRearSpin > -45f : (forwardFrontSpin > -45f && forwardRearSpin > -45f))
                AddFailure($"wheel self-spin direction/magnitude invalid (R/F={forwardRearSpin:F1}/{forwardFrontSpin:F1}deg).");

            stage = "BRAKE";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            yield return null;
            brakeStartVx = Mathf.Abs(chassis.linearVelocity.x);
            brakeEndVx = brakeStartVx;
            SetPedals(false, true);
            yield return null;
            for (int step = 0; step < MaximumBrakeSteps; step++)
            {
                yield return WaitFixed;
                brakeEndVx = Mathf.Min(brakeEndVx, Mathf.Abs(chassis.linearVelocity.x));
                if (step >= 10 && brakeEndVx <= 0.35f) break;
            }
            if (brakeStartVx > 1f && brakeEndVx > brakeStartVx * .55f)
                AddFailure($"brake did not reduce vx enough ({brakeStartVx:F3}->{brakeEndVx:F3}m/s).");
            for (int step = 0; step < 90 && Mathf.Abs(chassis.linearVelocity.x) > 0.25f; step++)
            {
                SetPedals(false, true);
                yield return WaitFixed;
            }
            SetPedals(false, false);
            for (int step = 0; step < 5; step++)
                yield return WaitFixed;

            stage = "REVERSE";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            float reverseStartX = chassis.position.x;
            SetPedals(false, true);
            yield return null;
            for (int step = 0; step < MaximumReverseSteps; step++)
            {
                yield return WaitFixed;
                if (step >= 30 && chassis.position.x - reverseStartX < -1f && chassis.linearVelocity.x < -.25f)
                    break;
            }
            SetPedals(false, false);
            reverseDistance = chassis.position.x - reverseStartX;
            reverseEndVx = chassis.linearVelocity.x;
            if (reverseDistance >= -1f || reverseEndVx >= -.25f)
                AddFailure($"reverse did not travel -X (distance={reverseDistance:F3}, vx={reverseEndVx:F3}).");

            stage = "LANDING_RELOCATE";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            terrain.TeleportTo(0f);
            vehicle.SetWholeRigVelocity(new Vector2(6f, -8f));
            // Teleporting a Rigidbody2D invalidates one interpolated render pose. Keep that intentional
            // relocation out of wheel visual metrics, then begin the real Box2D landing observation.
            yield return null;
            stage = "LANDING";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            sampleLanding = true;
            int stableSteps = 0;
            for (int step = 0; step < MaximumLandingSteps; step++)
            {
                yield return WaitFixed;
                bool both = vehicle.FrontWheelGrounded && vehicle.RearWheelGrounded;
                if (both && Mathf.Abs(chassis.linearVelocity.y) < 1.5f)
                    stableSteps++;
                else
                    stableSteps = 0;
                if (stableSteps >= 12)
                {
                    landingRecovered = true;
                    break;
                }
            }
            sampleLanding = false;
            SetPedals(false, false);
            if (!landingEverAirborne)
                AddFailure("landing scenario never became airborne.");
            if (!landingFrontContact || !landingRearContact)
                AddFailure($"landing missed a real wheel contact (R/F={landingRearContact}/{landingFrontContact}).");
            if (!landingBothGrounded || !landingRecovered)
                AddFailure($"landing did not recover cleanly (both={landingBothGrounded}, recovered={landingRecovered}).");

            // Multi-Distance Glitch Verification:
            // 1. Driving straight at 32k & 262k must NEVER trigger spontaneous subduction
            stage = "GLITCH_DRIVE_32K";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            terrain.TeleportTo(32768f);
            if (terrain.TrySampleAuthoritativeSurface(32768f, out float surf32Drive, out _))
                vehicle.TeleportToPosition(new Vector3(32768f, surf32Drive + vehicle.Definition.wheelRadius, 0f));
            vehicle.SetWholeRigVelocity(Vector2.zero);
            yield return null;
            for (int step = 0; step < 10; step++)
                yield return WaitFixed;
            front.ResetTerrainState();
            rear.ResetTerrainState();

            SetPedals(true, false);
            for (int step = 0; step < 25; step++)
                yield return WaitFixed;
            SetPedals(false, false);
            if (failExtremeDistanceChecks && (front.IsGlitchTrapped || rear.IsGlitchTrapped))
                AddFailure("spontaneous wheel penetration occurred while driving straight at 32k.");

            stage = "GLITCH_DRIVE_262K";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            terrain.TeleportTo(262144f);
            if (terrain.TrySampleAuthoritativeSurface(262144f, out float surf262Drive, out _))
                vehicle.TeleportToPosition(new Vector3(262144f, surf262Drive + vehicle.Definition.wheelRadius, 0f));
            vehicle.SetWholeRigVelocity(Vector2.zero);
            yield return null;
            for (int step = 0; step < 10; step++)
                yield return WaitFixed;
            front.ResetTerrainState();
            rear.ResetTerrainState();

            SetPedals(true, false);
            for (int step = 0; step < 25; step++)
                yield return WaitFixed;
            SetPedals(false, false);
            if (failExtremeDistanceChecks && (front.IsGlitchTrapped || rear.IsGlitchTrapped))
                AddFailure("spontaneous wheel penetration occurred while driving straight at 262k.");

            // 2. Heavy jump landing at 262k - observe whether Box2D naturally triggers penetration
            stage = "GLITCH_IMPACT_262K";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            if (terrain.TrySampleAuthoritativeSurface(262144f + 30f, out float surf262k, out _))
            {
                vehicle.TeleportToPosition(new Vector3(262144f + 30f, surf262k + 4.5f, 0f));
                vehicle.SetWholeRigVelocity(new Vector2(4f, -12f));
                yield return null;
                for (int step = 0; step < 40; step++)
                    yield return WaitFixed;

                bool trapped262 = front.IsGlitchTrapped || rear.IsGlitchTrapped;
                Debug.Log($"ADVENTURE_BOX2D_GLITCH_OBS|vehicle={expectedVehicleId}|dist=262k|trapped={trapped262}");
                if (trapped262)
                {
                    // If naturally trapped, verify it stays trapped under throttle/brake
                    SetPedals(true, false);
                    for (int step = 0; step < 20; step++)
                        yield return WaitFixed;
                    SetPedals(false, true);
                    for (int step = 0; step < 20; step++)
                        yield return WaitFixed;
                    SetPedals(false, false);

                    bool stillTrapped = front.IsGlitchTrapped || rear.IsGlitchTrapped;
                    if (failExtremeDistanceChecks && !stillTrapped)
                        AddFailure("wheel escaped from subterranean trap under throttle/brake at 262k.");
                }
            }

            // 3. Heavy jump landing at 524k - observe whether Box2D naturally triggers penetration
            stage = "GLITCH_IMPACT_524K";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            if (terrain.TrySampleAuthoritativeSurface(524288f + 30f, out float surf524k, out _))
            {
                vehicle.TeleportToPosition(new Vector3(524288f + 30f, surf524k + 4.5f, 0f));
                vehicle.SetWholeRigVelocity(new Vector2(4f, -12f));
                yield return null;
                for (int step = 0; step < 40; step++)
                    yield return WaitFixed;

                bool trapped524 = front.IsGlitchTrapped || rear.IsGlitchTrapped;
                Debug.Log($"ADVENTURE_BOX2D_GLITCH_OBS|vehicle={expectedVehicleId}|dist=524k|trapped={trapped524}");
                if (trapped524)
                {
                    SetPedals(true, false);
                    for (int step = 0; step < 15; step++)
                        yield return WaitFixed;
                    SetPedals(false, true);
                    for (int step = 0; step < 15; step++)
                        yield return WaitFixed;
                    SetPedals(false, false);

                    bool stillTrapped524 = front.IsGlitchTrapped || rear.IsGlitchTrapped;
                    if (failExtremeDistanceChecks && !stillTrapped524)
                        AddFailure("wheel escaped from subterranean trap under throttle/brake at 524k.");
                }
            }

            terrain.TeleportTo(0f);
            yield return null;

            stage = "FINAL";
            Debug.Log($"ADVENTURE_BOX2D_STAGE|vehicle={expectedVehicleId}|stage={stage}");
            for (int step = 0; step < 10; step++)
                yield return WaitFixed;
            ValidateFinalMetrics();
        }

        private bool TryResolveReferences()
        {
            if (vehicle == null)
            {
                GameManager manager = GameManager.Instance != null
                    ? GameManager.Instance : FindFirstObjectByType<GameManager>();
                vehicle = manager != null ? manager.Vehicle as VehiclePhysics2DController : null;
                if (vehicle == null) vehicle = FindFirstObjectByType<VehiclePhysics2DController>();
            }
            if (vehicle == null || vehicle.Body2D == null || vehicle.Definition == null) return false;
            if (!string.IsNullOrEmpty(expectedVehicleId) && vehicle.Definition.vehicleId != expectedVehicleId)
                return false;

            chassis = vehicle.Body2D;
            front = vehicle.FrontSuspension;
            rear = vehicle.RearSuspension;
            if (front == null || rear == null || front.WheelBody == null || rear.WheelBody == null) return false;

            if (input == null) input = FindFirstObjectByType<VehicleInputController>();
            if (terrain == null) terrain = TerrainManager.Instance != null
                ? TerrainManager.Instance : FindFirstObjectByType<TerrainManager>();
            if (input == null || terrain == null) return false;

            authoritativeEdge = FindAuthoritativeEdge(terrain);
            return authoritativeEdge != null && authoritativeEdge.enabled && authoritativeEdge.pointCount >= 2;
        }

        private static EdgeCollider2D FindAuthoritativeEdge(TerrainManager manager)
        {
            if (manager == null) return null;
            EdgeCollider2D[] edges = manager.GetComponentsInChildren<EdgeCollider2D>(true);
            for (int i = 0; i < edges.Length; i++)
                if (edges[i] != null && edges[i].name == "Authoritative Terrain 2D") return edges[i];
            for (int i = 0; i < edges.Length; i++)
                if (edges[i] != null && edges[i].enabled) return edges[i];
            return null;
        }

        private void LogRig(string label)
        {
            LogWheelRig(label, "R", rear);
            LogWheelRig(label, "F", front);
        }

        private void LogWheelRig(string label, string wheelLabel, Suspension2DController suspension)
        {
            Rigidbody2D wheel = suspension != null ? suspension.WheelBody : null;
            CircleCollider2D circle = suspension != null ? suspension.WheelCollider : null;
            WheelJoint2D joint = suspension != null ? suspension.SuspensionJoint : null;
            if (wheel == null || circle == null || joint == null) return;
            Vector2 attached = joint.attachedRigidbody != null
                ? joint.attachedRigidbody.GetRelativePoint(joint.anchor) : Vector2.zero;
            Vector2 connected = joint.connectedBody != null
                ? joint.connectedBody.GetRelativePoint(joint.connectedAnchor) : Vector2.zero;
            Vector2 colliderCenter = circle.transform.TransformPoint(circle.offset);
            Debug.Log($"ADVENTURE_BOX2D_RIG|vehicle={expectedVehicleId}|stage={label}|wheel={wheelLabel}|" +
                      $"chassisBody={chassis.position.x:F4},{chassis.position.y:F4}|" +
                      $"chassisTransform={chassis.transform.position.x:F4},{chassis.transform.position.y:F4}|" +
                      $"wheelBody={wheel.position.x:F4},{wheel.position.y:F4}|" +
                      $"wheelTransform={wheel.transform.position.x:F4},{wheel.transform.position.y:F4}|" +
                      $"collider={colliderCenter.x:F4},{colliderCenter.y:F4}|" +
                      $"anchorA={attached.x:F4},{attached.y:F4}|anchorB={connected.x:F4},{connected.y:F4}|" +
                      $"connectedAnchor={joint.connectedAnchor.x:F4},{joint.connectedAnchor.y:F4}|" +
                      $"rawContacts={suspension.ActualContactCount}|groundContact={suspension.HasBox2DContact}|" +
                      $"grounded={suspension.IsGrounded}");
        }

        private void ValidatePhysicsSettings()
        {
            if (Mathf.Abs(Time.fixedDeltaTime - .02f) > .00001f)
                AddFailure($"fixedDeltaTime={Time.fixedDeltaTime:F6}, expected .020000.");
            if ((Physics2D.gravity - new Vector2(0f, -9.81f)).sqrMagnitude > .000001f)
                AddFailure("Physics2D gravity=" + Physics2D.gravity + ", expected (0,-9.81).");
            if (Physics2D.velocityIterations != 8 || Physics2D.positionIterations != 3)
                AddFailure($"Physics2D solver={Physics2D.velocityIterations}/{Physics2D.positionIterations}, expected 8/3.");
            if (Mathf.Abs(Physics2D.defaultContactOffset - .01f) > .00001f)
                AddFailure($"Physics2D contactOffset={Physics2D.defaultContactOffset:F6}, expected .010000.");
            if (Physics2D.simulationMode != SimulationMode2D.FixedUpdate)
                AddFailure("Physics2D is not using normal FixedUpdate simulation.");
        }

        private void ValidateArchitecture()
        {
            if (chassis.bodyType != RigidbodyType2D.Dynamic)
                AddFailure("chassis Rigidbody2D is not Dynamic.");
            ValidateWheelArchitecture(rear, vehicle.Definition.rearWheelVisualPosition, "rear");
            ValidateWheelArchitecture(front, vehicle.Definition.frontWheelVisualPosition, "front");

            Vector2 rearLocal = chassis.transform.InverseTransformPoint(rear.WheelBody.position);
            Vector2 frontLocal = chassis.transform.InverseTransformPoint(front.WheelBody.position);
            float measuredWheelbase = frontLocal.x - rearLocal.x;
            float expectedWheelbase = vehicle.Definition.FrontWheelAnchor - vehicle.Definition.RearWheelAnchor;
            if (Mathf.Abs(measuredWheelbase - expectedWheelbase) > WheelbaseTolerance)
                AddFailure($"wheelbase={measuredWheelbase:F4}, expected={expectedWheelbase:F4}.");
        }

        private void ValidateWheelArchitecture(Suspension2DController suspension, Vector2 expectedAxle,
            string label)
        {
            Rigidbody2D wheel = suspension != null ? suspension.WheelBody : null;
            CircleCollider2D circle = suspension != null ? suspension.WheelCollider : null;
            WheelJoint2D joint = suspension != null ? suspension.SuspensionJoint : null;
            if (wheel == null || circle == null || joint == null)
            {
                AddFailure(label + " wheel is missing Rigidbody2D/CircleCollider2D/WheelJoint2D.");
                return;
            }

            if (wheel.bodyType != RigidbodyType2D.Dynamic || !wheel.simulated)
                AddFailure(label + " wheel Rigidbody2D is not a simulated Dynamic body.");
            if (wheel.transform.IsChildOf(chassis.transform) || wheel.transform.parent != chassis.transform.parent)
                AddFailure(label + " wheel is not a physics sibling of the chassis.");
            if (joint.attachedRigidbody != chassis || joint.connectedBody != wheel)
                AddFailure(label + " WheelJoint2D is not chassis -> wheel.");
            if (joint.autoConfigureConnectedAnchor)
                AddFailure(label + " WheelJoint2D autoConfigureConnectedAnchor is enabled.");
            if (joint.enableCollision)
                AddFailure(label + " WheelJoint2D enableCollision must be false.");
            if (Mathf.Abs(Mathf.DeltaAngle(joint.suspension.angle, 90f)) > .01f)
                AddFailure(label + $" suspension angle={joint.suspension.angle:F3}, expected 90.");
            if ((circle.offset - Vector2.zero).sqrMagnitude > .000001f)
                AddFailure(label + " CircleCollider2D offset is not zero.");
            float expectedRadius = label == "rear"
                ? (vehicle.Definition.RearWheelDiameter > .001f ? vehicle.Definition.RearWheelDiameter * .5f : vehicle.Definition.wheelRadius)
                : (vehicle.Definition.FrontWheelDiameter > .001f ? vehicle.Definition.FrontWheelDiameter * .5f : vehicle.Definition.wheelRadius);
            if (Mathf.Abs(circle.radius - expectedRadius) > .001f)
                AddFailure(label + $" radius={circle.radius:F4}, expected={expectedRadius:F4}.");

            Vector2 colliderCenter = wheel.GetRelativePoint(circle.offset);
            float centerError = Vector2.Distance(colliderCenter, wheel.position);
            if (centerError > ColliderCenterTolerance)
                AddFailure(label + $" collider center error={centerError:F6}.");

            if (joint.attachedRigidbody != null && joint.connectedBody != null)
            {
                Vector2 chassisAnchor = joint.attachedRigidbody.GetRelativePoint(joint.anchor);
                Vector2 wheelAnchor = joint.connectedBody.GetRelativePoint(joint.connectedAnchor);
                Vector2 expectedWorldAnchor = chassis.GetRelativePoint(expectedAxle);
                if (Vector2.Distance(chassisAnchor, expectedWorldAnchor) > AnchorTolerance)
                    AddFailure(label + " chassis anchor does not match authored axle.");
                if (Vector2.Distance(wheelAnchor, colliderCenter) > AnchorTolerance)
                    AddFailure(label + " wheel anchor is not at the physical tire center.");
            }
        }

        private void ValidateTerrainGeometry(string scenario, bool includeInAlignmentMetrics = true)
        {
            authoritativeEdge = FindAuthoritativeEdge(terrain);
            if (authoritativeEdge == null || !authoritativeEdge.enabled || authoritativeEdge.pointCount < 2)
            {
                string message = $"authoritative EdgeCollider2D is missing or disabled ({scenario}).";
                if (includeInAlignmentMetrics) AddFailure(message);
                else Debug.LogWarning("ADVENTURE_BOX2D_RELOCATION_TERRAIN_SKIPPED|vehicle=" +
                                      expectedVehicleId + "|map=" + expectedMapId + "|scenario=" + scenario +
                                      "|reason=missing-edge", this);
                return;
            }

            Debug.Log("ADVENTURE_BOX2D_TERRAIN_HEADER|Scenario|WorldX|VisualTerrainY|PhysicsTerrainY|" +
                      "SamplerTerrainY|VisualPhysicsDifference|SamplerPhysicsDifference", this);
            Vector2[] points = authoritativeEdge.points;
            int segmentCount = points.Length - 1;
            int localComparisons = 0;
            int localSamplerComparisons = 0;
            float localMaximumDifference = 0f;
            float localMaximumSamplerDifference = 0f;
            for (int segment = 0; segment < segmentCount; segment++)
            {
                Vector2 a = EdgePointWorld(authoritativeEdge, points[segment]);
                Vector2 b = EdgePointWorld(authoritativeEdge, points[segment + 1]);
                float worldX = (a.x + b.x) * .5f;
                if (!TrySampleVisualMesh(worldX, out float visualY) ||
                    !TrySamplePhysicsEdge(authoritativeEdge, worldX, out float physicsY))
                {
                    if (includeInAlignmentMetrics)
                        AddFailure($"terrain could not be sampled at X={worldX:F3} ({scenario}).");
                    continue;
                }

                float difference = Mathf.Abs(visualY - physicsY);
                localMaximumDifference = Mathf.Max(localMaximumDifference, difference);
                if (includeInAlignmentMetrics)
                {
                    maximumTerrainDifference = Mathf.Max(maximumTerrainDifference, difference);
                    terrainComparisonPoints++;
                }
                else
                {
                    maximumRelocationTerrainDifference = Mathf.Max(maximumRelocationTerrainDifference, difference);
                    relocationTerrainComparisonPoints++;
                }
                localComparisons++;

                float samplerY = float.NaN;
                float samplerDifference = float.PositiveInfinity;
                if (terrain.TrySampleAuthoritativeSurface(worldX, out samplerY, out _))
                {
                    samplerDifference = Mathf.Abs(samplerY - physicsY);
                    localMaximumSamplerDifference = Mathf.Max(localMaximumSamplerDifference, samplerDifference);
                    if (includeInAlignmentMetrics)
                    {
                        maximumSamplerDifference = Mathf.Max(maximumSamplerDifference, samplerDifference);
                        samplerComparisonPoints++;
                    }
                    else
                    {
                        maximumRelocationSamplerDifference = Mathf.Max(maximumRelocationSamplerDifference,
                            samplerDifference);
                        relocationSamplerComparisonPoints++;
                    }
                    localSamplerComparisons++;
                }
                else if (includeInAlignmentMetrics)
                {
                    AddFailure($"authoritative sampler failed at X={worldX:F3} ({scenario}).");
                }

                // Compare every segment, including chunk seams, but keep the console report compact.
                int logStride = Mathf.Max(1, segmentCount / 15);
                if (segment == 0 || segment == segmentCount - 1 || segment % logStride == 0)
                    Debug.Log($"ADVENTURE_BOX2D_TERRAIN_POINT|vehicle={expectedVehicleId}|scenario={scenario}|" +
                              $"x={worldX:F6}|visualY={visualY:F6}|physicsY={physicsY:F6}|" +
                              $"samplerY={samplerY:F6}|visualDiff={difference:F6}|" +
                              $"samplerDiff={samplerDifference:F6}", this);
            }

            Debug.Log($"ADVENTURE_BOX2D_TERRAIN_SUMMARY|vehicle={expectedVehicleId}|map={expectedMapId}|" +
                      $"scenario={scenario}|alignmentMetric={includeInAlignmentMetrics}|" +
                      $"points={localComparisons}/{segmentCount}|visualMaxDiff={localMaximumDifference:F6}|" +
                      $"samplerPoints={localSamplerComparisons}/{segmentCount}|" +
                      $"samplerMaxDiff={localMaximumSamplerDifference:F6}", this);
            if (includeInAlignmentMetrics && localComparisons != segmentCount)
                AddFailure($"terrain {scenario} compared {localComparisons}/{segmentCount} segments.");
            if (includeInAlignmentMetrics && localSamplerComparisons != segmentCount)
                AddFailure($"terrain sampler {scenario} compared {localSamplerComparisons}/{segmentCount} segments.");
            if (includeInAlignmentMetrics && maximumTerrainDifference > TerrainTolerance)
                AddFailure($"visual/physics terrain max diff={maximumTerrainDifference:F6}m.");
            if (includeInAlignmentMetrics && maximumSamplerDifference > TerrainTolerance)
                AddFailure($"sampler/physics terrain max diff={maximumSamplerDifference:F6}m.");
        }

        private bool TrySampleVisualMesh(float worldX, out float worldY)
        {
            worldY = 0f;
            TerrainChunk[] chunks = terrain != null
                ? terrain.GetComponentsInChildren<TerrainChunk>(true) : Array.Empty<TerrainChunk>();
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                TerrainChunk chunk = chunks[chunkIndex];
                if (chunk == null || !chunk.IsGenerated) continue;
                MeshFilter filter = chunk.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (mesh == null) continue;
                Vector3[] vertices = mesh.vertices;
                int count = Mathf.Min(chunk.SurfaceSampleCount, vertices.Length / 6);
                for (int i = 0; i + 1 < count; i++)
                {
                    Vector3 a3 = chunk.transform.TransformPoint(vertices[i * 6 + 4]);
                    Vector3 b3 = chunk.transform.TransformPoint(vertices[(i + 1) * 6 + 4]);
                    float minX = Mathf.Min(a3.x, b3.x) - .0001f;
                    float maxX = Mathf.Max(a3.x, b3.x) + .0001f;
                    if (worldX < minX || worldX > maxX) continue;
                    float t = Mathf.Abs(b3.x - a3.x) > .000001f
                        ? Mathf.InverseLerp(a3.x, b3.x, worldX) : 0f;
                    worldY = Mathf.Lerp(a3.y, b3.y, t);
                    return IsFinite(worldY);
                }
            }
            return false;
        }

        private static bool TrySamplePhysicsEdge(EdgeCollider2D edge, float worldX, out float worldY)
        {
            worldY = 0f;
            if (edge == null || edge.pointCount < 2) return false;
            Vector2[] points = edge.points;
            for (int i = 0; i + 1 < points.Length; i++)
            {
                Vector2 a = EdgePointWorld(edge, points[i]);
                Vector2 b = EdgePointWorld(edge, points[i + 1]);
                float minX = Mathf.Min(a.x, b.x) - .0001f;
                float maxX = Mathf.Max(a.x, b.x) + .0001f;
                if (worldX < minX || worldX > maxX) continue;
                float t = Mathf.Abs(b.x - a.x) > .000001f ? Mathf.InverseLerp(a.x, b.x, worldX) : 0f;
                worldY = Mathf.Lerp(a.y, b.y, t);
                return IsFinite(worldY);
            }
            return false;
        }

        private static Vector2 EdgePointWorld(EdgeCollider2D edge, Vector2 point)
        {
            Vector2 local = point + edge.offset;
            Vector3 world = edge.transform.TransformPoint(new Vector3(local.x, local.y, 0f));
            return new Vector2(world.x, world.y);
        }

        private void FixedUpdate()
        {
            if (!running || !observePhysicalMetrics || vehicle == null || chassis == null ||
                front == null || rear == null) return;

            finite &= BodyIsFinite(chassis) && BodyIsFinite(front.WheelBody) && BodyIsFinite(rear.WheelBody);
            SampleWheel(rear, false);
            SampleWheel(front, true);

            if (sampleForwardSpin)
            {
                float frontRotation = front.WheelBody.rotation;
                float rearRotation = rear.WheelBody.rotation;
                forwardFrontSpin += (frontRotation - previousFrontRotation);
                forwardRearSpin += (rearRotation - previousRearRotation);
                previousFrontRotation = frontRotation;
                previousRearRotation = rearRotation;
            }

            if (front.HasBox2DContact) frontContactSteps++;
            if (rear.HasBox2DContact) rearContactSteps++;

            if (sampleLanding)
            {
                landingEverAirborne |= !vehicle.AnyWheelGrounded;
                landingFrontContact |= front.HasBox2DContact;
                landingRearContact |= rear.HasBox2DContact;
                landingBothGrounded |= vehicle.FrontWheelGrounded && vehicle.RearWheelGrounded;
                chassisContactObserved |= vehicle.ChassisGrounded;
            }
        }

        private void LateUpdate()
        {
            if (!running || !observePhysicalMetrics || front == null || rear == null ||
                IsIntentionalRelocationStage()) return;
            maximumFrontVisualCenterError = Mathf.Max(maximumFrontVisualCenterError, front.VisualCenterError);
            maximumRearVisualCenterError = Mathf.Max(maximumRearVisualCenterError, rear.VisualCenterError);
            maximumFrontVisualRotationError = Mathf.Max(maximumFrontVisualRotationError, front.VisualRotationError);
            maximumRearVisualRotationError = Mathf.Max(maximumRearVisualRotationError, rear.VisualRotationError);
        }

        private void SampleWheel(Suspension2DController suspension, bool isFront)
        {
            Rigidbody2D wheel = suspension.WheelBody;
            CircleCollider2D circle = suspension.WheelCollider;
            WheelJoint2D joint = suspension.SuspensionJoint;
            if (wheel == null || circle == null || joint == null || !joint.enabled ||
                joint.attachedRigidbody != chassis || joint.connectedBody != wheel)
            {
                jointsIntact = false;
                return;
            }

            Vector2 colliderCenter = wheel.GetRelativePoint(circle.offset);
            float colliderError = Vector2.Distance(colliderCenter, wheel.position);
            Vector2 attachedAnchor = joint.attachedRigidbody.GetRelativePoint(joint.anchor);
            Vector2 connectedAnchor = joint.connectedBody.GetRelativePoint(joint.connectedAnchor);
            Vector2 delta = connectedAnchor - attachedAnchor;
            float localAngle = joint.suspension.angle;
            float worldAngle = (chassis.rotation + localAngle) * Mathf.Deg2Rad;
            Vector2 axis = new Vector2(Mathf.Cos(worldAngle), Mathf.Sin(worldAngle));
            Vector2 perpendicular = new Vector2(-axis.y, axis.x);
            float lateralError = Mathf.Abs(Vector2.Dot(delta, perpendicular));
            float span = delta.magnitude;

            finite &= IsFinite(colliderCenter) && IsFinite(attachedAnchor) && IsFinite(connectedAnchor) &&
                      IsFinite(joint.jointTranslation) && IsFinite(span) && IsFinite(lateralError);

            bool isIntentionalGlitchStage = IsIntentionalRelocationStage();
            if (!isIntentionalGlitchStage)
            {
                if (isFront)
                {
                    maximumFrontColliderCenterError = Mathf.Max(maximumFrontColliderCenterError, colliderError);
                    maximumFrontLateralJointError = Mathf.Max(maximumFrontLateralJointError, lateralError);
                    maximumFrontJointSpan = Mathf.Max(maximumFrontJointSpan, span);
                }
                else
                {
                    maximumRearColliderCenterError = Mathf.Max(maximumRearColliderCenterError, colliderError);
                    maximumRearLateralJointError = Mathf.Max(maximumRearLateralJointError, lateralError);
                    maximumRearJointSpan = Mathf.Max(maximumRearJointSpan, span);
                }

                if (stage == "FORWARD" || stage == "BRAKE" || stage == "REVERSE" || stage == "LANDING")
                {
                    if (terrain != null && terrain.TrySampleAuthoritativeSurface(colliderCenter.x,
                            out float surfaceY, out float slopeDegrees))
                    {
                        if (colliderCenter.y < surfaceY - vehicle.Definition.wheelRadius * 0.5f)
                            wheelCenterBelowTerrain = true;
                        if (suspension.HasBox2DContact)
                        {
                            float radius = circle.radius * Mathf.Max(Mathf.Abs(wheel.transform.lossyScale.x),
                                Mathf.Abs(wheel.transform.lossyScale.y));
                            float bottomDelta = colliderCenter.y - radius - surfaceY;
                            float normalClearance = (colliderCenter.y - surfaceY) *
                                Mathf.Cos(slopeDegrees * Mathf.Deg2Rad) - radius;
                            RecordSurfaceContact(isFront, bottomDelta, normalClearance);
                        }
                    }
                }
            }
        }

        private bool IsIntentionalRelocationStage() =>
            stage.StartsWith("GLITCH_") || stage.EndsWith("_RELOCATE") || stage == "FINAL";

        private void RecordSurfaceContact(bool isFront, float bottomDelta, float normalClearance)
        {
            if (isFront)
            {
                minimumFrontWheelBottomDelta = Mathf.Min(minimumFrontWheelBottomDelta, bottomDelta);
                maximumFrontWheelBottomDelta = Mathf.Max(maximumFrontWheelBottomDelta, bottomDelta);
                minimumFrontNormalClearance = Mathf.Min(minimumFrontNormalClearance, normalClearance);
                maximumFrontNormalClearance = Mathf.Max(maximumFrontNormalClearance, normalClearance);
                frontSurfaceContactSamples++;
            }
            else
            {
                minimumRearWheelBottomDelta = Mathf.Min(minimumRearWheelBottomDelta, bottomDelta);
                maximumRearWheelBottomDelta = Mathf.Max(maximumRearWheelBottomDelta, bottomDelta);
                minimumRearNormalClearance = Mathf.Min(minimumRearNormalClearance, normalClearance);
                maximumRearNormalClearance = Mathf.Max(maximumRearNormalClearance, normalClearance);
                rearSurfaceContactSamples++;
            }
        }

        private static string FormatRange(float minimum, float maximum) =>
            float.IsInfinity(minimum) || float.IsInfinity(maximum) ? "n/a" : $"{minimum:F4}..{maximum:F4}";

        private void ValidateFinalMetrics()
        {
            if (!finite) AddFailure("NaN or Infinity was observed.");
            if (!jointsIntact) AddFailure("a WheelJoint2D lost its body/reference or was disabled.");
            if (wheelCenterBelowTerrain) AddFailure("a wheel center crossed below the physical terrain at X=0.");
            if (maximumFrontColliderCenterError > ColliderCenterTolerance ||
                maximumRearColliderCenterError > ColliderCenterTolerance)
                AddFailure($"collider center max error R/F={maximumRearColliderCenterError:F6}/" +
                           $"{maximumFrontColliderCenterError:F6}.");
            if (maximumFrontVisualCenterError > VisualCenterTolerance ||
                maximumRearVisualCenterError > VisualCenterTolerance)
                AddFailure($"visual center max error R/F={maximumRearVisualCenterError:F6}/" +
                           $"{maximumFrontVisualCenterError:F6}.");
            if (maximumFrontVisualRotationError > VisualRotationTolerance ||
                maximumRearVisualRotationError > VisualRotationTolerance)
                AddFailure($"visual rotation max error R/F={maximumRearVisualRotationError:F3}/" +
                           $"{maximumFrontVisualRotationError:F3}deg.");
            if (maximumFrontLateralJointError > LateralJointTolerance ||
                maximumRearLateralJointError > LateralJointTolerance)
                AddFailure($"joint lateral/orbit max error R/F={maximumRearLateralJointError:F6}/" +
                           $"{maximumFrontLateralJointError:F6}.");

            float reasonableSpan = Mathf.Max(1.75f, vehicle.Definition.wheelRadius * 3.5f);
            if (maximumFrontJointSpan > reasonableSpan || maximumRearJointSpan > reasonableSpan)
                AddFailure($"joint span became unreasonable R/F={maximumRearJointSpan:F4}/" +
                           $"{maximumFrontJointSpan:F4}, limit={reasonableSpan:F4}.");
            if (frontContactSteps == 0 || rearContactSteps == 0)
                AddFailure($"real Box2D contact was never observed R/F={rearContactSteps}/{frontContactSteps} steps.");
        }

        private void CompleteAudit()
        {
            if (Completed) return;
            SetPedals(false, false);
            running = false;
            Passed = failures.Count == 0;
            ResultLine = $"ADVENTURE_BOX2D_PLAY_RESULT|vehicle={expectedVehicleId}|map={expectedMapId}|" +
                         $"passed={Passed}|" +
                         $"finite={finite}|jointsIntact={jointsIntact}|terrainPoints={terrainComparisonPoints}|" +
                         $"terrainMaxDiff={maximumTerrainDifference:F6}|samplerPoints={samplerComparisonPoints}|" +
                         $"samplerMaxDiff={maximumSamplerDifference:F6}|forwardDistance={forwardDistance:F3}|" +
                         $"relocationTerrainPoints={relocationTerrainComparisonPoints}|" +
                         $"relocationTerrainMaxDiff={maximumRelocationTerrainDifference:F6}|" +
                         $"relocationSamplerPoints={relocationSamplerComparisonPoints}|" +
                         $"relocationSamplerMaxDiff={maximumRelocationSamplerDifference:F6}|" +
                         $"forwardMaxVx={forwardMaximumVx:F3}|brakeVx={brakeStartVx:F3}->{brakeEndVx:F3}|" +
                         $"reverseDistance={reverseDistance:F3}|reverseVx={reverseEndVx:F3}|" +
                         $"wheelSpinR/F={forwardRearSpin:F1}/{forwardFrontSpin:F1}|" +
                         $"visualCenterR/F={maximumRearVisualCenterError:F6}/{maximumFrontVisualCenterError:F6}|" +
                         $"visualRotationR/F={maximumRearVisualRotationError:F3}/{maximumFrontVisualRotationError:F3}|" +
                         $"colliderCenterR/F={maximumRearColliderCenterError:F6}/{maximumFrontColliderCenterError:F6}|" +
                         $"lateralR/F={maximumRearLateralJointError:F6}/{maximumFrontLateralJointError:F6}|" +
                         $"jointSpanR/F={maximumRearJointSpan:F6}/{maximumFrontJointSpan:F6}|" +
                         $"contactsR/F={rearContactSteps}/{frontContactSteps}|landingAir={landingEverAirborne}|" +
                         $"surfaceContactsR/F={rearSurfaceContactSamples}/{frontSurfaceContactSamples}|" +
                         $"wheelBottomDeltaR/F={FormatRange(minimumRearWheelBottomDelta, maximumRearWheelBottomDelta)}/" +
                         $"{FormatRange(minimumFrontWheelBottomDelta, maximumFrontWheelBottomDelta)}|" +
                         $"normalClearanceR/F={FormatRange(minimumRearNormalClearance, maximumRearNormalClearance)}/" +
                         $"{FormatRange(minimumFrontNormalClearance, maximumFrontNormalClearance)}|" +
                         $"landingContactR/F={landingRearContact}/{landingFrontContact}|" +
                         $"landingBoth={landingBothGrounded}|landingRecovered={landingRecovered}|" +
                         $"chassisContact={chassisContactObserved}|wheelCenterBelow={wheelCenterBelowTerrain}|" +
                         $"failures={failures.Count}";
            Debug.Log(ResultLine, this);
            if (!Passed)
                Debug.LogError("ADVENTURE_BOX2D_PLAY_FAILURE|vehicle=" + expectedVehicleId + "|map=" +
                               expectedMapId + "|" +
                               FailureSummary, this);
            Completed = true;
        }

        private void SetPedals(bool gas, bool brake)
        {
            if (input == null) return;
            input.SetTouchGas(gas);
            input.SetTouchBrake(brake);
        }

        private void AddFailure(string failure)
        {
            if (string.IsNullOrEmpty(failure) || failures.Contains(failure)) return;
            failures.Add(failure);
        }

        private static bool BodyIsFinite(Rigidbody2D body)
        {
            return body != null && IsFinite(body.position) && IsFinite(body.linearVelocity) &&
                   IsFinite(body.rotation) && IsFinite(body.angularVelocity);
        }

        private static bool IsFinite(Vector2 value) => IsFinite(value.x) && IsFinite(value.y);
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static string Sanitize(string value) => string.IsNullOrEmpty(value)
            ? string.Empty : value.Replace('|', '/').Replace('\n', ' ').Replace('\r', ' ');
    }
}
