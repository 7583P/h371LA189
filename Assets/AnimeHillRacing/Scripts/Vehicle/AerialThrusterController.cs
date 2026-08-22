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
        private float bothGroundedTimer;

        public AerialFlightState State { get; private set; } = AerialFlightState.Grounded;
        public float CurrentLeftPower { get; private set; }
        public float CurrentRightPower { get; private set; }
        public float CurrentPower => Mathf.Max(CurrentLeftPower, CurrentRightPower);
        public bool ThrustersActive => CurrentPower > .01f && fuel != null && fuel.FuelNormalized > 0f;
        public bool OverridesGroundDrive => State != AerialFlightState.Grounded;

        public void Initialize(VehiclePhysics2DController owner, VehicleInputController inputController,
            FuelSystem fuelSystem)
        {
            vehicle = owner;
            input = inputController;
            body = owner != null ? owner.Body2D : GetComponent<Rigidbody2D>();
            fuel = fuelSystem != null ? fuelSystem : GetComponent<FuelSystem>();
            State = AerialFlightState.Grounded;
            airborneTimer = bothGroundedTimer = CurrentLeftPower = CurrentRightPower = 0f;
            fuel?.SetConsumptionMultiplier(1f);
            thrusterVfx?.SetPower(0f, 0f, TuneProgress());
        }

        public void AssignVisuals(Transform leftPoint, Transform rightPoint, AerialThrusterVfx vfx)
        {
            leftThrusterPoint = leftPoint;
            rightThrusterPoint = rightPoint;
            thrusterVfx = vfx;
        }

        public bool StepPhysics(float dt, bool driveEnabled, bool gasPressed, bool brakePressed)
        {
            if (vehicle == null || body == null || fuel == null) return false;

            bool anyGrounded = vehicle.AnyWheelGrounded || vehicle.ChassisGrounded;
            bool bothGrounded = vehicle.BothWheelsGrounded;
            bool airborne = !anyGrounded;
            airborneTimer = airborne ? airborneTimer + dt : 0f;
            bothGroundedTimer = bothGrounded ? bothGroundedTimer + dt : 0f;
            bool dualPedals = gasPressed && brakePressed;
            bool hasFuel = fuel.FuelNormalized > 0f;

            switch (State)
            {
                case AerialFlightState.Grounded:
                    if (driveEnabled && dualPedals && hasFuel) State = AerialFlightState.Takeoff;
                    else if (airborneTimer >= airborneConfirmationSeconds) State = AerialFlightState.Flying;
                    break;
                case AerialFlightState.Takeoff:
                    if (airborneTimer >= airborneConfirmationSeconds) State = AerialFlightState.Flying;
                    else if (!dualPedals && anyGrounded) State = AerialFlightState.Landing;
                    break;
                case AerialFlightState.Flying:
                    if (anyGrounded) State = AerialFlightState.Landing;
                    break;
                case AerialFlightState.Landing:
                    if (airborneTimer >= airborneConfirmationSeconds) State = AerialFlightState.Flying;
                    else if (driveEnabled && dualPedals && hasFuel) State = AerialFlightState.Takeoff;
                    else if (bothGroundedTimer >= landingConfirmationSeconds) State = AerialFlightState.Grounded;
                    break;
            }

            bool flightControlsEnabled = State == AerialFlightState.Takeoff ||
                                         State == AerialFlightState.Flying ||
                                         State == AerialFlightState.Landing;
            float targetLeft = driveEnabled && hasFuel && flightControlsEnabled && brakePressed ? 1f : 0f;
            float targetRight = driveEnabled && hasFuel && flightControlsEnabled && gasPressed ? 1f : 0f;
            CurrentLeftPower = MovePower(CurrentLeftPower, targetLeft, dt);
            CurrentRightPower = MovePower(CurrentRightPower, targetRight, dt);

            float tune = TuneProgress();
            fuel.SetConsumptionMultiplier(CurrentPower > .01f ? 2f : 1f);
            thrusterVfx?.SetPower(CurrentLeftPower, CurrentRightPower, tune);
            if (CurrentPower > .001f && hasFuel)
                ApplyIndependentThrust(tune);

            return OverridesGroundDrive;
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
