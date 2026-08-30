using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public enum AerialFlightState
    {
        Grounded,
        Takeoff,
        Flying,
        Landing
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(FuelSystem))]
    public sealed class AerialThrusterController : MonoBehaviour
    {
        [Header("Physical thrust points")]
        [SerializeField] private Transform leftThrusterPoint;
        [SerializeField] private Transform rightThrusterPoint;
        [SerializeField] private AerialThrusterVfx thrusterVfx;

        [Header("Acceleration produced by the pair at maximum tune")]
        [SerializeField, Min(9.82f)] private float maximumPairVerticalAcceleration = 22f;
        [SerializeField, Range(.1f, 1f)] private float initialTuneAuthority = .50f;
        [SerializeField, Range(.05f, .3f)] private float ignitionFadeSeconds = .15f;
        [SerializeField, Range(.05f, .3f)] private float airborneConfirmationSeconds = .14f;
        [SerializeField, Range(.05f, .35f)] private float landingConfirmationSeconds = .18f;

        [Header("Soft flight stabilization")]
        [SerializeField, Min(0f)] private float uprightAngularAcceleration = 2.1f;
        [SerializeField, Min(0f)] private float angularVelocityDamping = .62f;
        [SerializeField] private Vector2 verticalVelocityAttenuationRange = new Vector2(7f, 18f);
        [SerializeField, Range(.1f, 1f)] private float minimumHighRiseAuthority = .35f;

        private VehiclePhysics2DController vehicle;
        private VehicleInputController input;
        private Rigidbody2D body;
        private FuelSystem fuel;
        private float airborneTimer;
        private float groundedTimer;

        public AerialFlightState State { get; private set; } = AerialFlightState.Grounded;
        public float CurrentLeftPower { get; private set; }
        public float CurrentRightPower { get; private set; }
        public float CurrentPower => Mathf.Max(CurrentLeftPower, CurrentRightPower);
        public bool ThrustersActive => CurrentPower > .01f && fuel != null && fuel.FuelNormalized > 0f;
        public bool OverridesGroundDrive => State == AerialFlightState.Flying ||
                                            State == AerialFlightState.Takeoff;

        public void Initialize(VehiclePhysics2DController owner, VehicleInputController inputController,
            FuelSystem fuelSystem)
        {
            vehicle = owner;
            input = inputController;
            body = owner != null ? owner.Body2D : GetComponent<Rigidbody2D>();
            fuel = fuelSystem != null ? fuelSystem : GetComponent<FuelSystem>();
            ApplyAuthoredThrusterPositions();
            State = AerialFlightState.Grounded;
            airborneTimer = groundedTimer = CurrentLeftPower = CurrentRightPower = 0f;
            fuel?.SetConsumptionMultiplier(1f);
            thrusterVfx?.SetPower(0f, 0f, TuneProgress());
        }

        public void AssignVisuals(Transform leftPoint, Transform rightPoint, AerialThrusterVfx vfx)
        {
            leftThrusterPoint = leftPoint;
            rightThrusterPoint = rightPoint;
            thrusterVfx = vfx;
        }

        private void ApplyAuthoredThrusterPositions()
        {
            VehicleDefinition definition = vehicle != null ? vehicle.Definition : null;
            if (definition == null) return;
            Vector2[] positions = definition.underbodyAttachmentPositions;
            if (positions != null && positions.Length >= 2)
            {
                if (leftThrusterPoint != null) leftThrusterPoint.localPosition = positions[0];
                if (rightThrusterPoint != null) rightThrusterPoint.localPosition = positions[1];
            }

            EnforceThrusterForegroundDepth();

            float visualWidth = Mathf.Max(.05f, definition.underbodyAttachmentVisualWidth);
            ApplyVisualWidth(leftThrusterPoint, visualWidth);
            ApplyVisualWidth(rightThrusterPoint, visualWidth);
        }

        private void EnforceThrusterForegroundDepth()
        {
            Transform leftParent = leftThrusterPoint != null ? leftThrusterPoint.parent : null;
            Transform rightParent = rightThrusterPoint != null ? rightThrusterPoint.parent : null;
            Transform rig = leftParent != null && leftParent == rightParent ? leftParent : null;
            if (rig != null)
            {
                Vector3 rigPosition = rig.position;
                rigPosition.z = VehicleVisual2D.VehicleForegroundZ;
                rig.position = rigPosition;
                return;
            }

            SetForegroundDepth(leftThrusterPoint);
            SetForegroundDepth(rightThrusterPoint);
        }

        private static void SetForegroundDepth(Transform point)
        {
            if (point == null) return;
            Vector3 position = point.position;
            position.z = VehicleVisual2D.VehicleForegroundZ;
            point.position = position;
        }

        private static void ApplyVisualWidth(Transform point, float visualWidth)
        {
            Transform hardware = point != null ? point.Find("Compact Thruster Hardware") : null;
            SpriteRenderer renderer = hardware != null ? hardware.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || renderer.sprite == null) return;
            float scale = VehicleDefinition.SpriteWidthScale(renderer.sprite, visualWidth);
            hardware.localScale = Vector3.one * scale;
        }

        public bool StepPhysics(float dt, bool driveEnabled, bool gasPressed, bool brakePressed)
        {
            if (vehicle == null || body == null || fuel == null) return false;

            bool anyGrounded = vehicle.AnyWheelGrounded || vehicle.ChassisGrounded;
            bool bothWheelsGrounded = vehicle.BothWheelsGrounded;
            bool airborne = !anyGrounded;
            airborneTimer = airborne ? airborneTimer + dt : 0f;
            groundedTimer = anyGrounded ? groundedTimer + dt : 0f;
            bool hasFuel = fuel.FuelNormalized > 0f;

            switch (State)
            {
                case AerialFlightState.Grounded:
                    if (airborneTimer >= airborneConfirmationSeconds) State = AerialFlightState.Flying;
                    break;
                case AerialFlightState.Takeoff:
                    // Keep the legacy state transition safe. Dual-pedal ground thrust is permission-based
                    // and does not need to enter Takeoff before the vehicle becomes airborne.
                    if (airborneTimer >= airborneConfirmationSeconds) State = AerialFlightState.Flying;
                    else if (anyGrounded) State = AerialFlightState.Grounded;
                    break;
                case AerialFlightState.Flying:
                    if (anyGrounded) State = AerialFlightState.Landing;
                    break;
                case AerialFlightState.Landing:
                    if (airborneTimer >= airborneConfirmationSeconds) State = AerialFlightState.Flying;
                    else if (groundedTimer >= landingConfirmationSeconds) State = AerialFlightState.Grounded;
                    break;
            }

            bool thrustPermitted = IsThrustPermitted(driveEnabled, anyGrounded,
                bothWheelsGrounded, gasPressed, brakePressed, hasFuel, State);
            if (!thrustPermitted)
            {
                // Ground thrust is an intentional dual-pedal takeoff action and requires both tires to
                // remain planted. Losing either contact cancels both thrusters immediately.
                CurrentLeftPower = CurrentRightPower = 0f;
            }
            else
            {
                CurrentLeftPower = MovePower(CurrentLeftPower, brakePressed ? 1f : 0f, dt);
                CurrentRightPower = MovePower(CurrentRightPower, gasPressed ? 1f : 0f, dt);
            }

            float tune = TuneProgress();
            fuel.SetConsumptionMultiplier(ResolveConsumptionMultiplier(CurrentLeftPower, CurrentRightPower));
            thrusterVfx?.SetPower(CurrentLeftPower, CurrentRightPower, tune);
            if (thrustPermitted && CurrentPower > .001f)
                ApplyIndependentThrust(tune);

            return OverridesGroundDrive;
        }

        public static bool IsThrustPermitted(bool driveEnabled, bool anyGroundContact,
            bool bothWheelsGrounded, bool gasPressed, bool brakePressed, bool hasFuel,
            AerialFlightState state)
        {
            if (!driveEnabled || !hasFuel) return false;

            bool airborneFlight = !anyGroundContact && state == AerialFlightState.Flying;
            bool groundedDualPedalTakeoff = anyGroundContact && bothWheelsGrounded &&
                                             gasPressed && brakePressed;
            return airborneFlight || groundedDualPedalTakeoff;
        }

        public static float ResolveConsumptionMultiplier(float leftPower, float rightPower)
        {
            bool leftActive = leftPower > .01f;
            bool rightActive = rightPower > .01f;
            if (leftActive && rightActive) return 3f;
            return leftActive || rightActive ? 2.5f : 1f;
        }

        private float MovePower(float current, float target, float dt)
        {
            float fade = target > current ? ignitionFadeSeconds : .09f;
            return Mathf.MoveTowards(current, target, dt / Mathf.Max(.02f, fade));
        }

        private float TuneProgress()
        {
            return vehicle != null && vehicle.Definition != null
                ? VehicleDefinition.UpgradeProgress(vehicle.UpgradeState.turboTune,
                    vehicle.Definition.turboMaxTunes)
                : 0f;
        }

        private void ApplyIndependentThrust(float tune)
        {
            float tuneAuthority = Mathf.Lerp(initialTuneAuthority, 1f, tune);
            float riseAuthority = Mathf.Lerp(1f, minimumHighRiseAuthority,
                Mathf.InverseLerp(verticalVelocityAttenuationRange.x,
                    verticalVelocityAttenuationRange.y, body.linearVelocity.y));
            Vector2 forcePerThruster = (Vector2)transform.up *
                                       (maximumPairVerticalAcceleration * tuneAuthority * riseAuthority *
                                        body.mass * .5f);

            Vector2 leftPosition = leftThrusterPoint != null ? leftThrusterPoint.position :
                body.worldCenterOfMass + (Vector2)transform.right * -.55f;
            Vector2 rightPosition = rightThrusterPoint != null ? rightThrusterPoint.position :
                body.worldCenterOfMass + (Vector2)transform.right * .55f;
            if (CurrentLeftPower > .001f)
                body.AddForceAtPosition(forcePerThruster * CurrentLeftPower, leftPosition, ForceMode2D.Force);
            if (CurrentRightPower > .001f)
                body.AddForceAtPosition(forcePerThruster * CurrentRightPower, rightPosition, ForceMode2D.Force);

            // Stabilization is intentionally weak and only acts while both pedals are balanced. A single
            // pedal therefore rotates the car in the air instead of being cancelled by the autopilot.
            float balancedPower = Mathf.Min(CurrentLeftPower, CurrentRightPower);
            float angleRadians = Mathf.DeltaAngle(0f, body.rotation) * Mathf.Deg2Rad;
            float angularVelocityRadians = body.angularVelocity * Mathf.Deg2Rad;
            float correctiveTorque = -body.inertia *
                                     (angleRadians * uprightAngularAcceleration +
                                      angularVelocityRadians * angularVelocityDamping) * balancedPower * .32f;
            body.AddTorque(correctiveTorque, ForceMode2D.Force);
        }

        private void OnDisable()
        {
            CurrentLeftPower = CurrentRightPower = 0f;
            fuel?.SetConsumptionMultiplier(1f);
            thrusterVfx?.SetPower(0f, 0f, TuneProgress());
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.2f, .95f, 1f, .95f);
            DrawPoint(leftThrusterPoint);
            DrawPoint(rightThrusterPoint);
        }

        private static void DrawPoint(Transform point)
        {
            if (point == null) return;
            Gizmos.DrawWireSphere(point.position, .12f);
            Gizmos.DrawLine(point.position, point.position + -point.up * .75f);
        }
#endif
    }
}
