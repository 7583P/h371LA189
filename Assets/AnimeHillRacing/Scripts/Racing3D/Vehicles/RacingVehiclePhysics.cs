using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public enum MiniTurboStage { None, MiniTurbo, SuperMiniTurbo, UltraMiniTurbo }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class RacingVehiclePhysics : MonoBehaviour
    {
        [SerializeField] private string vehicleId = "car-01";
        [SerializeField] private string wheelId = "wheel-default";
        [SerializeField] private LayerMask groundLayer = ~0;
        private Rigidbody body;
        private IVehicleCommandProvider commandProvider;
        private FinalRacingStats stats;
        private readonly RaycastHit[] groundHits = new RaycastHit[12];
        private readonly Vector3[] contactOffsets = { new Vector3(-.86f,.2f,1.02f),new Vector3(.86f,.2f,1.02f),new Vector3(-.86f,.2f,-1.02f),new Vector3(.86f,.2f,-1.02f) };
        private readonly float[] suspension = new float[4];
        private Vector3 groundNormal = Vector3.up;
        private bool isGrounded, isOffroad, isDrifting;
        private int driftDirection;
        private float driftTime, airborneTime, boostTimer, boostSpeedMultiplier = 1f, spinoutTimer;
        private MiniTurboStage currentMiniTurboStage;
        private float impactCooldown;
        private bool raceEnabled = true;
        private float jumpCooldownTimer;
        private float coyoteTimer;
        private bool trickPerformed;
        private int trickCount;
        private float trickCooldownTimer;

        public Rigidbody Body => body != null ? body : (body = GetComponent<Rigidbody>());
        public FinalRacingStats Stats => stats;
        public string VehicleId => vehicleId;
        public string WheelId => wheelId;
        public float SpeedMps => (body != null || (body = GetComponent<Rigidbody>()) != null) ? Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude : 0f;
        public float SpeedKmh => SpeedMps * 3.6f;
        public float ForwardSpeedMps => (body != null || (body = GetComponent<Rigidbody>()) != null) ? Vector3.Dot(body.linearVelocity, transform.forward) : 0f;
        public float ForwardSpeedKmh => ForwardSpeedMps * 3.6f;
        public bool IsGrounded => isGrounded;
        public float AirborneTime => airborneTime;
        public bool IsDrifting => isDrifting;
        public bool IsPerformingTrick => trickPerformed;
        public int TrickCount => trickCount;
        public MiniTurboStage CurrentMiniTurboStage => currentMiniTurboStage;
        public float DriftCharge => Mathf.Clamp01(driftTime / 2.7f);
        public float SlipAngle => (body != null || (body = GetComponent<Rigidbody>()) != null) ? Mathf.Atan2(Vector3.Dot(body.linearVelocity, transform.right), Mathf.Max(1f, Mathf.Abs(ForwardSpeedMps))) * Mathf.Rad2Deg : 0f;
        public bool IsBoosting => boostTimer > 0f;
        public bool IsSpunOut => spinoutTimer > 0f;
        public bool IsOffroad => isOffroad;
        public bool RaceEnabled => raceEnabled;
        public Vector3 GroundNormal => groundNormal;
        public VehicleCommands LastCommands { get; private set; }
        public float SuspensionCompression(int wheel) => suspension[Mathf.Clamp(wheel,0,3)];
        public event Action<float> OnImpact;
        public event Action<float> OnLanding;
        public event Action<MiniTurboStage> OnDriftStage;
        public event Action OnBoost;
        public event Action OnJump;
        public event Action<int> OnTrick;
        public event Action OnLandingBoost;

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearDamping = .12f;
                body.angularDamping = 3f;
                body.maxAngularVelocity = 3.5f;
                body.maxDepenetrationVelocity = 15f;
                body.solverIterations = 16;
                body.solverVelocityIterations = 8;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.centerOfMass = new Vector3(0,-.12f,-.1f);
            }
            ApplyStats(RacingStatsCalculator.Calculate(vehicleId,wheelId));
        }
        public void Initialize(string vId,string wId,IVehicleCommandProvider provider)
        {
            vehicleId = vId; wheelId = wId; commandProvider = provider;
            ApplyStats(RacingStatsCalculator.Calculate(vId,wId));
        }
        public void Initialize(FinalRacingStats customStats, IVehicleCommandProvider provider = null)
        {
            commandProvider = provider;
            ApplyStats(customStats);
        }
        public void SetCommandProvider(IVehicleCommandProvider provider) { commandProvider = provider; }
        public void SetRaceEnabled(bool enabled)
        {
            raceEnabled = enabled;
            if (body == null) body = GetComponent<Rigidbody>();
            if (body != null) body.isKinematic = !enabled;
            LastCommands = VehicleCommands.Idle;
        }
        public void ApplyStats(FinalRacingStats resolved)
        {
            stats = resolved;
            if (body == null) body = GetComponent<Rigidbody>();
            if (body != null) body.mass = resolved.WeightKg;
        }
        private void FixedUpdate()
        {
            CheckGroundStatus();
            if(!raceEnabled || body.isKinematic) return;
            float dt = Time.fixedDeltaTime;
            boostTimer = Mathf.Max(0,boostTimer-dt);
            if(boostTimer == 0) boostSpeedMultiplier = 1f;
            spinoutTimer = Mathf.Max(0,spinoutTimer-dt);
            jumpCooldownTimer = Mathf.Max(0, jumpCooldownTimer - dt);
            trickCooldownTimer = Mathf.Max(0, trickCooldownTimer - dt);

            var recovery = GetComponent<VehicleRecovery>();
            VehicleCommands cmd = recovery != null && recovery.IsRecovering ? VehicleCommands.Idle : commandProvider?.GetCommands() ?? VehicleCommands.Idle;
            LastCommands = cmd;

            if(spinoutTimer > 0)
            {
                EndDrift(false);
                cmd.Throttle = 0; cmd.Steering = 0;
                body.AddForce(-Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up)*1.6f,ForceMode.Acceleration);
            }

            // Jump action (grounded or coyote grace)
            if(cmd.Jump && jumpCooldownTimer <= 0f && (isGrounded || coyoteTimer > 0f))
            {
                jumpCooldownTimer = 0.42f;
                coyoteTimer = 0f;
                isGrounded = false;
                Vector3 jumpDir = (groundNormal + Vector3.up * 1.5f).normalized;
                body.AddForce(jumpDir * 6.5f, ForceMode.VelocityChange);
                OnJump?.Invoke();
                RacingFeedback.Play(RacingCue.DriftHop);
            }

            // In-air trick / stunt action
            if(!isGrounded && airborneTime > 0.14f && trickCooldownTimer <= 0f && (cmd.Trick || (cmd.Jump && !trickPerformed)))
            {
                trickPerformed = true;
                trickCount = Mathf.Min(trickCount + 1, 2);
                trickCooldownTimer = 0.45f;
                OnTrick?.Invoke(trickCount);
                RacingFeedback.Play(RacingCue.DriftHop);
            }

            UpdateDrift(cmd,dt);
            ApplyDriveForces(cmd);
            ApplySteeringAndFriction(cmd,dt);
            ApplyStabilityForces();
        }
        private void CheckGroundStatus()
        {
            bool wasGrounded = isGrounded;
            int contacts = 0;
            groundNormal = Vector3.zero;
            isOffroad = false;
            Vector3 rayDir = Vector3.Slerp(Vector3.down, -transform.up, 0.42f).normalized;
            for(int w=0;w<4;w++)
            {
                Vector3 origin = transform.TransformPoint(contactOffsets[w]);
                int count = Physics.RaycastNonAlloc(origin,rayDir,groundHits,1.35f,groundLayer,QueryTriggerInteraction.Ignore);
                float closest = float.MaxValue;
                RaycastHit hit = default;
                for(int i=0;i<count;i++)
                {
                    if(groundHits[i].collider.attachedRigidbody != null || groundHits[i].normal.y < .28f) continue;
                    if(groundHits[i].distance < closest) { closest = groundHits[i].distance; hit = groundHits[i]; }
                }
                if(closest == float.MaxValue) { suspension[w] = 0; continue; }
                contacts++;
                groundNormal += hit.normal;
                var surface = hit.collider.GetComponent<RacingSurface>();
                isOffroad |= surface != null && surface.IsOffroad;
                suspension[w] = Mathf.Clamp01((1.05f-closest)/.45f);
                if(raceEnabled && !body.isKinematic)
                {
                    float normalVelocity = Vector3.Dot(body.GetPointVelocity(origin),hit.normal);
                    float springAcceleration = Mathf.Clamp((.95f-closest)*190f-normalVelocity*18f,0,38f);
                    body.AddForceAtPosition(hit.normal*springAcceleration*body.mass*.25f,origin,ForceMode.Force);
                }
            }
            isGrounded = contacts > 0;
            groundNormal = isGrounded ? groundNormal.normalized : Vector3.up;
            if(isGrounded)
            {
                coyoteTimer = 0.12f;
                if(!wasGrounded)
                {
                    if(airborneTime > .18f) OnLanding?.Invoke(Mathf.Clamp01(airborneTime*.5f));
                    if(trickPerformed)
                    {
                        if(Vector3.Dot(transform.up, groundNormal) > 0.40f && spinoutTimer <= 0f)
                        {
                            float boostMult = 1.22f + 0.08f * trickCount;
                            float boostDur = 1.15f + 0.35f * trickCount;
                            ApplySpeedBoost(boostMult, boostDur);
                            OnLandingBoost?.Invoke();
                            RacingFeedback.Play(RacingCue.MiniTurbo);
                        }
                        trickPerformed = false;
                        trickCount = 0;
                    }
                }
                airborneTime = 0;
            }
            else
            {
                coyoteTimer = Mathf.Max(0, coyoteTimer - Time.fixedDeltaTime);
                airborneTime += Time.fixedDeltaTime;
            }
        }
        private void UpdateDrift(VehicleCommands cmd,float dt)
        {
            if(!isGrounded)
            {
                if(airborneTime > .2f && isDrifting) EndDrift(false);
                return;
            }
            if(!isDrifting && cmd.Drift && Mathf.Abs(cmd.Steering) > .22f && ForwardSpeedKmh > 38f)
            {
                isDrifting = true; driftDirection = cmd.Steering > 0 ? 1 : -1; driftTime = 0;
            }
            if(!isDrifting) return;
            if(!cmd.Drift || ForwardSpeedKmh < 22f) { EndDrift(ForwardSpeedKmh >= 22f); return; }
            if(Mathf.Abs(SlipAngle) >= 5f && Mathf.Abs(cmd.Steering) > .05f)
                driftTime += dt*stats.MiniTurboMultiplier*Mathf.Lerp(.7f,1f,Mathf.Clamp01(Mathf.Abs(SlipAngle)/20f));
            MiniTurboStage stage = driftTime >= 2.7f ? MiniTurboStage.UltraMiniTurbo : driftTime >= 1.55f ? MiniTurboStage.SuperMiniTurbo : driftTime >= .65f ? MiniTurboStage.MiniTurbo : MiniTurboStage.None;
            if(stage != currentMiniTurboStage) { currentMiniTurboStage = stage; OnDriftStage?.Invoke(stage); }
        }
        private void EndDrift(bool boost)
        {
            if(boost && currentMiniTurboStage != MiniTurboStage.None)
            {
                int stage = (int)currentMiniTurboStage;
                ApplySpeedBoost(1f+.08f*stage,.45f+.35f*stage);
            }
            isDrifting = false; driftDirection = 0; driftTime = 0; currentMiniTurboStage = MiniTurboStage.None;
        }
        private void ApplyDriveForces(VehicleCommands cmd)
        {
            if(!isGrounded) return;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward,groundNormal).normalized;
            float speed = Vector3.Dot(body.linearVelocity,forward);
            float maxSpeed = stats.SpeedKmh/3.6f * (isOffroad && !IsBoosting ? stats.OffroadRetention : 1f) * boostSpeedMultiplier;
            if(cmd.Throttle > .01f && speed < maxSpeed)
            {
                float taper = 1f-.30f*Mathf.Pow(Mathf.Clamp01(speed/maxSpeed),2);
                body.AddForce(forward*stats.AccelerationMps2*cmd.Throttle*taper*(IsBoosting ? 1.3f : 1f),ForceMode.Acceleration);
            }
            if(speed > maxSpeed) body.AddForce(-forward*Mathf.Min(35f,(speed-maxSpeed)*4f),ForceMode.Acceleration);
            if(cmd.Brake > .01f)
            {
                float brake = speed > 1f ? stats.AccelerationMps2*2.4f : speed > -7f ? stats.AccelerationMps2*.45f : 0;
                body.AddForce(-forward*brake*cmd.Brake,ForceMode.Acceleration);
            }
            if(cmd.Throttle < .01f && cmd.Brake < .01f)
                body.AddForce(-forward*speed*.17f,ForceMode.Acceleration);
        }
        private void ApplySteeringAndFriction(VehicleCommands cmd,float dt)
        {
            if(isGrounded)
            {
                float speed = ForwardSpeedMps;
                float steer = cmd.Steering;
                if(isDrifting) steer = driftDirection*.56f + cmd.Steering*.55f;
                float yaw = steer*stats.HandlingRadPerSec*.5f*Mathf.Clamp01(Mathf.Abs(speed)/8f)*(speed < -1f ? -1f : 1f);
                if(isDrifting) yaw *= Mathf.Lerp(1f,1.3f,Mathf.Clamp01(stats.DriftMultiplier-0.5f));
                Vector3 rotationAxis = groundNormal;
                body.AddTorque(rotationAxis*(yaw-Vector3.Dot(body.angularVelocity,rotationAxis))*12f,ForceMode.Acceleration);
                float lateralSpeed = Vector3.Dot(body.linearVelocity,transform.right);
                float grip = stats.LateralGrip*(isDrifting ? 2.8f : 9f);
                float cancellation = 1f-Mathf.Exp(-grip*dt);
                body.AddForce(-transform.right*lateralSpeed*cancellation,ForceMode.VelocityChange);
            }
            else
            {
                body.AddTorque(transform.right*cmd.Pitch*2f,ForceMode.Acceleration);
                body.AddTorque(Vector3.Cross(transform.up,Vector3.up)*stats.Stability*3f,ForceMode.Acceleration);
            }
        }
        private void ApplyStabilityForces()
        {
            if(!isGrounded) return;
            Vector3 tiltVelocity = Vector3.ProjectOnPlane(body.angularVelocity,groundNormal);
            body.AddTorque(Vector3.Cross(transform.up,groundNormal)*28f*stats.Stability-tiltVelocity*7f,ForceMode.Acceleration);
            body.AddForce(-groundNormal*Mathf.Min(5f,SpeedMps*.09f),ForceMode.Acceleration);
        }
        public void ApplySpeedBoost(float multiplier,float duration)
        {
            if(duration <= 0 || !float.IsFinite(multiplier)) return;
            boostSpeedMultiplier = Mathf.Max(boostSpeedMultiplier,Mathf.Clamp(multiplier,1f,1.5f));
            boostTimer = Mathf.Max(boostTimer,duration);
            if(isGrounded && raceEnabled) body.AddForce(transform.forward*Mathf.Clamp((multiplier-1f)*15f,0,4.5f),ForceMode.VelocityChange);
            OnBoost?.Invoke();
        }
        public void TriggerSpinout(float duration=1.0f)
        {
            if(!raceEnabled) return;
            spinoutTimer = Mathf.Max(spinoutTimer,Mathf.Clamp(duration,0,1.25f));
            EndDrift(false);
            OnImpact?.Invoke(.65f);
        }
        private void OnCollisionEnter(Collision collision)
        {
            if(!raceEnabled || Time.time < impactCooldown) return;
            float intensity = Mathf.Clamp01(collision.relativeVelocity.magnitude/18f);
            if(intensity < .08f) return;
            impactCooldown = Time.time+.18f;
            OnImpact?.Invoke(intensity);
            if(collision.collider.GetComponentInParent<RacingVehiclePhysics>() == null && collision.contactCount > 0)
            {
                Vector3 normal = collision.GetContact(0).normal;
                if(Mathf.Abs(normal.y) < .65f)
                {
                    body.AddForce(normal*2.0f,ForceMode.VelocityChange);
                    body.angularVelocity *= .45f;
                }
            }
        }
        private void OnCollisionStay(Collision collision)
        {
            if(!raceEnabled || body == null || body.isKinematic) return;
            if(collision.collider.GetComponentInParent<RacingVehiclePhysics>() == null && collision.contactCount > 0)
            {
                Vector3 normal = collision.GetContact(0).normal;
                if(Mathf.Abs(normal.y) < .65f)
                {
                    // Gentle glancing deflection along wall normal to prevent snagging and sticking
                    body.AddForce(normal * 3.5f, ForceMode.Acceleration);
                    float steerAway = Vector3.Dot(LastCommands.Steering * transform.right, normal);
                    if(steerAway > 0.05f)
                    {
                        body.AddForce(normal * 6.5f, ForceMode.Acceleration);
                    }
                    if(LastCommands.Brake > 0.1f)
                    {
                        body.AddForce(-transform.forward * 4.0f, ForceMode.Acceleration);
                    }
                }
            }
        }
        public void ResetVelocities()
        {
            if(body != null && !body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            EndDrift(false); boostTimer = 0; boostSpeedMultiplier = 1; spinoutTimer = 0;
            trickPerformed = false; trickCount = 0; jumpCooldownTimer = 0f; coyoteTimer = 0f;
        }
    }
}
