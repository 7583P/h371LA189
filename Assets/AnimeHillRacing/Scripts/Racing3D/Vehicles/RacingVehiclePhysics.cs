using System;
using UnityEngine;
using Unity.Profiling;

namespace AnimeHillRacing.Racing3D
{
    public enum MiniTurboStage { None, MiniTurbo, SuperMiniTurbo, UltraMiniTurbo }

    [RequireComponent(typeof(Rigidbody))]
    public sealed partial class RacingVehiclePhysics : MonoBehaviour
    {
        [SerializeField] private string vehicleId = "car-01";
        [SerializeField] private string wheelId = "wheel-default";
        [SerializeField] private LayerMask groundLayer = ~0;
        private Rigidbody body;
        private VehicleRecovery recovery;
        private IVehicleCommandProvider commandProvider;
        private FinalRacingStats stats;
        private readonly RaycastHit[] groundHits = new RaycastHit[12];
        private readonly Vector3[] contactOffsets = { new Vector3(-.86f,.2f,1.02f),new Vector3(.86f,.2f,1.02f),new Vector3(-.86f,.2f,-1.02f),new Vector3(.86f,.2f,-1.02f) };
        private readonly float[] suspension = new float[4];
        private Vector3 groundNormal = Vector3.up;
        private bool isGrounded, isOffroad, isDrifting;
        private int driftDirection;
        private float driftTime, airborneTime, boostTimer, boostSpeedMultiplier = 1f, spinoutTimer;
        private float lancerTimer;
        static readonly ProfilerMarker PhysicsMarker = new ProfilerMarker("Racing.Vehicle.Step");
        static readonly ProfilerMarker ZoneMarker = new ProfilerMarker("Racing.Shortcut.Zone");
        private TrackSplineGraph raceTrack;
        public TrackShortcut ActiveShortcut { get; private set; }
        public float ShortcutSpeedMultiplier => ActiveShortcut != null ? TrackSplineGraph.ShortcutSpeedScale : 1f;
        public float EffectiveLancerTargetKmh => RaceItemUtility.LancerSpeedKmh * ShortcutSpeedMultiplier;
        public void BindRaceTrack(TrackSplineGraph graph) => raceTrack = graph;
        private void Start() { if(raceTrack==null)raceTrack=FindFirstObjectByType<TrackSplineGraph>(); }
        private float spinoutDuration = 1.0f, spinoutElapsed, spinoutDirection = 1f, spinoutImmunityUntil;
        private bool isHeavySpin;
        private MiniTurboStage currentMiniTurboStage;
        private float impactCooldown;
        private bool raceEnabled = true;
        private float jumpCooldownTimer;
        private float coyoteTimer;
        private bool trickPerformed;
        private int trickCount;
        private float trickCooldownTimer;
        private float aerialBoostTimer;
        public const float MaxAerialBoostDuration = 3.0f;
        public bool IsAerialBoosting { get; private set; }
        public float AerialBoostRemaining => Mathf.Max(0f, MaxAerialBoostDuration - aerialBoostTimer);
        public float AerialBoostTimer => aerialBoostTimer;
        Collider[] contacts;bool finishedGhost;
        float lastMiniTurboTime=float.NegativeInfinity,lastJumpTime=float.NegativeInfinity;
        public float LastJumpTime => lastJumpTime;
        public bool UsesHumanInput => commandProvider is PlayerRacingInput;

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
        public int DriftDirection => driftDirection;
        public bool IsPerformingTrick => trickPerformed;
        public int TrickCount => trickCount;
        public MiniTurboStage CurrentMiniTurboStage => currentMiniTurboStage;
        public float DriftCharge => Mathf.Clamp01(driftTime / RacingDriftFeedback.PurpleCharge);
        public float SlipAngle => (body != null || (body = GetComponent<Rigidbody>()) != null) ? Mathf.Atan2(Vector3.Dot(body.linearVelocity, transform.right), Mathf.Max(1f, Mathf.Abs(ForwardSpeedMps))) * Mathf.Rad2Deg : 0f;
        public bool IsBoosting => boostTimer > 0f;
        public float BoostRemainingSeconds => boostTimer;
        public float BoostMultiplier => boostSpeedMultiplier;
        public bool IsLancerActive => lancerTimer > 0f;
        public float LancerRemainingSeconds => lancerTimer;
        public int PhysicalCollisions { get; private set; }
        public int WallCollisions { get; private set; }
        public int RacerCollisions { get; private set; }
        public int ItemHits { get; private set; }
        public int MiniTurbosReleased { get; private set; }
        public int DriftsStarted { get; private set; }
        public int JumpsPerformed { get; private set; }
        public int TricksPerformed { get; private set; }
        public int LandingBoosts { get; private set; }
        public int DriftJumpChains { get; private set; }
        public int JumpTrickLandingChains { get; private set; }
        public bool IsSpunOut => spinoutTimer > 0f;
        public bool IsOffroad => isOffroad;
        public bool RaceEnabled => raceEnabled;
        public Vector3 GroundNormal => groundNormal;
        public VehicleCommands LastCommands { get; private set; }
        public float SuspensionCompression(int wheel) => suspension[Mathf.Clamp(wheel,0,3)];
        public event Action<float> OnImpact;
        public event Action<float> OnLanding;
        public event Action<MiniTurboStage> OnDriftStage;
        public event Action<MiniTurboStage> OnMiniTurboReleased;
        public event Action OnBoost;
        public event Action OnJump;
        public event Action<int> OnTrick;
        public event Action OnLandingBoost;
        public event Action OnAerialBoost;
        public event Action<bool,float> OnPhysicalCollision;
        public int AerialBoostActivations { get; private set; }
        public event Action OnItemHit;
        public event Action<bool> OnLancerChanged;
        public event Action OnItemRequested;

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            recovery = GetComponent<VehicleRecovery>();
            contacts=GetComponents<Collider>();
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
            if (!enabled) { CancelMiniHop();CancelLancer();ActiveShortcut=null; }
            if(enabled && finishedGhost){finishedGhost=false;foreach(var contact in contacts)contact.enabled=true;}
            raceEnabled = enabled;
            if (body == null) body = GetComponent<Rigidbody>();
            if (body != null) body.isKinematic = !enabled;
            LastCommands = VehicleCommands.Idle;
        }
        public void SetFinished()
        {
            ResetVelocities();
            SetRaceEnabled(false);finishedGhost=true;
            foreach(var contact in contacts)contact.enabled=false;
        }
        public void ApplyStats(FinalRacingStats resolved)
        {
            stats = resolved;
            if (body == null) body = GetComponent<Rigidbody>();
            if (body != null) body.mass = resolved.WeightKg;
        }

        public void StepPhysics() => FixedUpdate();

        private void FixedUpdate()
        { using(PhysicsMarker.Auto()) StepVehicle(); }
        private void StepVehicle()
        {
            CheckGroundStatus();
            if(!raceEnabled || body.isKinematic) return;
            using(ZoneMarker.Auto()) ActiveShortcut = recovery != null && recovery.IsRecovering ? null : raceTrack?.OfficialShortcutAt(body.position);
            float dt = Time.fixedDeltaTime;
            if (lancerTimer > 0f) {
                if (lancerTimer <= dt + .00001f) CancelLancer();
                else lancerTimer -= dt;
            }
            boostTimer = Mathf.Max(0,boostTimer-dt);
            if(boostTimer == 0) boostSpeedMultiplier = 1f;
            spinoutTimer = Mathf.Max(0,spinoutTimer-dt);
            jumpCooldownTimer = Mathf.Max(0, jumpCooldownTimer - dt);
            trickCooldownTimer = Mathf.Max(0, trickCooldownTimer - dt);

            if (recovery == null) recovery = GetComponent<VehicleRecovery>();
            VehicleCommands cmd = recovery != null && recovery.IsRecovering ? VehicleCommands.Idle : commandProvider?.GetCommands() ?? VehicleCommands.Idle;
            LastCommands = cmd;
            if(cmd.UseItem) OnItemRequested?.Invoke();
            // Rocket propulsion is exclusive with other thrust/drift boosts.
            // Steering, braking, gravity and collision handling remain shared.
            if (IsLancerActive) { cmd.Drift = false; cmd.MiniHop = false; cmd.AerialBoost = false; cmd.Trick = false; }
            LastCommands = cmd;

            if(spinoutTimer > 0)
            {
                CancelMiniHop();
                EndDrift(false);
                cmd.Throttle = 0; cmd.Steering = 0; cmd.Brake = 0; cmd.Jump = false; cmd.Trick = false;
                spinoutElapsed += dt;
                float spinActiveTime = spinoutDuration * 0.82f;
                float progress = Mathf.Clamp01(spinoutElapsed / Mathf.Max(0.1f, spinoutDuration));

                // 1. Force strict upright orientation: eliminate all pitch & roll angular velocity components
                Vector3 nonYawAngularVel = Vector3.ProjectOnPlane(body.angularVelocity, groundNormal);
                body.angularVelocity -= nonYawAngularVel;

                // Strong upright orientation torque so vehicle chassis stays flat on its wheels
                Vector3 tiltCorrection = Vector3.Cross(transform.up, groundNormal);
                if (tiltCorrection.sqrMagnitude > 0.0001f)
                {
                    body.AddTorque(tiltCorrection * 85f, ForceMode.Acceleration);
                }

                // 2. Pure vertical-axis yaw spin (360-degree rotation around groundNormal only)
                if (spinoutElapsed < spinActiveTime)
                {
                    // Constant yaw rate to complete exactly 360 degrees
                    float targetSpinSpeed = spinoutDirection * (360f * Mathf.Deg2Rad) / spinActiveTime;
                    body.angularVelocity = groundNormal * targetSpinSpeed;

                    // Controlled planar momentum decay: slide smoothly along track without erratic jerks
                    Vector3 groundPlanarVel = Vector3.ProjectOnPlane(body.linearVelocity, groundNormal);
                    float dragRate = isHeavySpin ? 3.5f : 2.2f;
                    body.linearVelocity = groundPlanarVel * Mathf.Clamp01(1f - dt * dragRate) + Vector3.Project(body.linearVelocity, groundNormal);
                }
                else
                {
                    // 3. Crisp recovery phase: damp yaw rotation smoothly to zero
                    Vector3 currentYaw = Vector3.Project(body.angularVelocity, groundNormal);
                    body.angularVelocity -= currentYaw * Mathf.Min(1f, dt * 25f);
                }
            }

            StepMiniHop(cmd, dt);

            // Jump action (grounded or coyote grace)
            if(cmd.Jump && jumpCooldownTimer <= 0f && (isGrounded || coyoteTimer > 0f))
            {
                jumpCooldownTimer = 0.42f;
                coyoteTimer = 0f;
                isGrounded = false;
                Vector3 jumpDir = (groundNormal + Vector3.up * 1.5f).normalized;
                body.AddForce(jumpDir * 3.6f, ForceMode.VelocityChange);
                NotifyJump();
                RacingFeedback.Play(RacingCue.DriftHop);
            }

            // In-air trick / stunt action
            if(!IsMiniHopping && !isGrounded && airborneTime > 0.14f && trickCount<2 && trickCooldownTimer <= 0f && (cmd.Trick || (cmd.Jump && !trickPerformed)))
            {
                trickPerformed = true;
                trickCount = Mathf.Min(trickCount + 1, 2);
                TricksPerformed++;
                trickCooldownTimer = 0.45f;
                OnTrick?.Invoke(trickCount);
                RacingFeedback.Play(RacingCue.DriftHop);
            }

            // Aerial Boost execution (airborne only, max 3.0s duration)
            if (isGrounded)
            {
                aerialBoostTimer = 0f;
                IsAerialBoosting = false;
            }
            else if (!IsMiniHopping && airborneTime > 0.05f)
            {
                if (cmd.AerialBoost && aerialBoostTimer < MaxAerialBoostDuration && spinoutTimer <= 0f)
                {
                    aerialBoostTimer += dt;
                    if(!IsAerialBoosting){AerialBoostActivations++;OnAerialBoost?.Invoke();}
                    IsAerialBoosting = true;

                    // Physical thrust strictly follows vehicle 3D orientation
                    Vector3 thrustDirection = transform.forward;
                    float thrustPower = stats.AccelerationMps2 * 2.4f;
                    body.AddForce(thrustDirection * thrustPower, ForceMode.Acceleration);

                    // Clamp extreme aerial velocities (no infinite flight or map escaping)
                    float maxAirSpeed = (stats.SpeedKmh * 1.35f) / 3.6f;
                    if (body.linearVelocity.magnitude > maxAirSpeed)
                    {
                        body.linearVelocity = body.linearVelocity.normalized * maxAirSpeed;
                    }
                    if (body.linearVelocity.y > 26f)
                    {
                        Vector3 v = body.linearVelocity;
                        v.y = 26f;
                        body.linearVelocity = v;
                    }
                }
                else
                {
                    IsAerialBoosting = false;
                }
            }

            UpdateDrift(cmd,dt);
            ApplyDriveForces(cmd);
            ApplyShortcutSpeedLimit();
            ApplySteeringAndFriction(cmd,dt);
            ApplyStabilityForces();
        }
        private void CheckGroundStatus()
        {
            int contacts = 0;
            isOffroad = false;
            groundNormal = Vector3.zero;
            bool wasGrounded = isGrounded;
            for(int w=0; w<4; w++)
            {
                Vector3 origin = transform.TransformPoint(contactOffsets[w]);
                int count = Physics.RaycastNonAlloc(origin,-transform.up,groundHits,1.15f,groundLayer,QueryTriggerInteraction.Ignore);
                float closest = float.MaxValue;
                RaycastHit hit = default;
                for(int i=0; i<count; i++)
                {
                    if(groundHits[i].collider.transform.root == transform.root) continue;
                    if(groundHits[i].distance < closest) { closest = groundHits[i].distance; hit = groundHits[i]; }
                }
                if(closest == float.MaxValue) { suspension[w] = 0; continue; }
                float normalVelocity = Vector3.Dot(body.GetPointVelocity(origin),hit.normal);
                if (closest <= 1.10f && normalVelocity < (IsMiniHopping ? .2f : 1.5f) && (!IsMiniHopping || miniHopElapsed >= .12f))
                { contacts++; groundNormal += hit.normal; }
                var surface = hit.collider.GetComponent<RacingSurface>();
                isOffroad |= surface != null && surface.IsOffroad;
                suspension[w] = Mathf.Clamp01((1.05f-closest)/.45f);
                if(raceEnabled && !body.isKinematic && (!IsMiniHopping || miniHopElapsed >= .12f))
                {
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
                            LandingBoosts++;
                            if(Time.time-lastJumpTime<3)JumpTrickLandingChains++;
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
                DriftsStarted++;
            }
            if(!isDrifting) return;
            if(!cmd.Drift || ForwardSpeedKmh < 22f) { EndDrift(ForwardSpeedKmh >= 22f); return; }
            if(Mathf.Abs(SlipAngle) >= 5f && Mathf.Abs(cmd.Steering) > .05f)
                driftTime += dt*stats.MiniTurboMultiplier*Mathf.Lerp(.7f,1f,Mathf.Clamp01(Mathf.Abs(SlipAngle)/20f));
            MiniTurboStage stage = RacingDriftFeedback.StageForCharge(driftTime);
            if(stage != currentMiniTurboStage) { currentMiniTurboStage = stage; OnDriftStage?.Invoke(stage); }
        }
        private void EndDrift(bool boost)
        {
            if(boost && currentMiniTurboStage != MiniTurboStage.None)
            {
                int stage = (int)currentMiniTurboStage;
                MiniTurbosReleased++;
                lastMiniTurboTime = Time.time;
                float duration = stage == 3 ? stats.MiniTurboDuration3 : stage == 2 ? stats.MiniTurboDuration2 : stats.MiniTurboDuration1;
                float boostMult = 1f + .08f * stage;
                ApplySpeedBoost(boostMult, duration);
                OnMiniTurboReleased?.Invoke(currentMiniTurboStage);
                if (stage == 3) RacingFeedback.Play(RacingCue.SuperTurbo);
                else if (stage == 2) RacingFeedback.Play(RacingCue.MediumTurbo);
                else RacingFeedback.Play(RacingCue.MiniTurbo);
            }
            isDrifting = false; driftDirection = 0; driftTime = 0; currentMiniTurboStage = MiniTurboStage.None;
        }
        private void ApplyDriveForces(VehicleCommands cmd)
        {
            if (IsLancerActive) { ApplyLancerPropulsion(cmd); return; }
            if(!isGrounded || spinoutTimer > 0f) return;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward,groundNormal).normalized;
            float speed = Vector3.Dot(body.linearVelocity,forward);
            float maxSpeed = stats.SpeedKmh/3.6f * (isOffroad && !IsBoosting ? stats.OffroadRetention : 1f) * boostSpeedMultiplier * ShortcutSpeedMultiplier;
            if(cmd.Throttle > .01f && speed < maxSpeed)
            {
                float taper = 1f-.30f*Mathf.Pow(Mathf.Clamp01(speed/maxSpeed),2);
                body.AddForce(forward*stats.AccelerationMps2*cmd.Throttle*taper*(IsBoosting ? 1.3f : 1f)*ShortcutSpeedMultiplier,ForceMode.Acceleration);
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
        private void ApplyLancerPropulsion(VehicleCommands cmd)
        {
            Vector3 normal = isGrounded ? groundNormal : Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
            if (forward.sqrMagnitude < .5f || spinoutTimer > 0f) return;
            float speed = Vector3.Dot(body.linearVelocity, forward);
            if (cmd.Brake > .01f) {
                float brake = speed > 1f ? stats.AccelerationMps2 * 2.4f : speed > -7f ? stats.AccelerationMps2 * .45f : 0;
                body.AddForce(-forward * brake * cmd.Brake, ForceMode.Acceleration);
                return;
            }
            // A bounded, explicit item motor, identical for every racer. No
            // teleport, stats rewrite, checkpoint assist or collision bypass.
            float dt = Time.fixedDeltaTime;
            float damping = Mathf.Max(.01f, 1f - body.linearDamping * dt);
            float gravityStep = body.useGravity ? Vector3.Dot(Physics.gravity, forward) * dt : 0f;
            float delta = EffectiveLancerTargetKmh / 3.6f / damping - speed - gravityStep;
            body.AddForce(forward * Mathf.Clamp(delta, -35f * dt, 90f * dt), ForceMode.VelocityChange);
        }
        private void ApplyShortcutSpeedLimit()
        {
            if(ActiveShortcut==null)return;
            // Bounded planar deceleration; preserve vertical velocity, contacts,
            // steering and gravity. No instantaneous velocity rewrite on entry.
            Vector3 planar=Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up);float speed=planar.magnitude;
            float limit=(IsLancerActive?RaceItemUtility.LancerSpeedKmh:stats.SpeedKmh*boostSpeedMultiplier)*ShortcutSpeedMultiplier/3.6f;
            if(speed>limit&&speed>.01f)body.AddForce(-planar/speed*Mathf.Min(150f,(speed-limit)*12f),ForceMode.Acceleration);
        }
        public bool TryActivateLancer()
        {
            if (!raceEnabled || !isActiveAndEnabled || Body.isKinematic || IsLancerActive || IsSpunOut ||
                (recovery != null && recovery.IsRecovering)) return false;
            EndDrift(false);
            boostTimer = 0f; boostSpeedMultiplier = 1f; IsAerialBoosting = false;
            lancerTimer = RaceItemUtility.LancerDurationSeconds;
            OnLancerChanged?.Invoke(true);
            return true;
        }
        public void CancelLancer()
        {
            if (lancerTimer <= 0f) return;
            lancerTimer = 0f;
            OnLancerChanged?.Invoke(false);
        }
        private void OnDisable() { CancelMiniHop();CancelLancer(); }
        private void ApplySteeringAndFriction(VehicleCommands cmd,float dt)
        {
            if (spinoutTimer > 0f) return;
            if (IsMiniHopping)
            {
                ApplyMiniHopSteering(cmd);
                return;
            }
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
                // 1. Pitch torque (W/S or Left Stick Y)
                body.AddTorque(transform.right * cmd.Pitch * 3.6f, ForceMode.Acceleration);

                // 2. Air Roll torque (Q/E or Bumpers) - Q rolls left, E rolls right
                body.AddTorque(-transform.forward * cmd.AirRoll * 7.2f, ForceMode.Acceleration);

                // 3. Upright stabilization assist when neutral controls
                if (Mathf.Abs(cmd.AirRoll) < 0.05f && Mathf.Abs(cmd.Pitch) < 0.05f)
                {
                    Vector3 upright = Vector3.Cross(transform.up, Vector3.up);
                    body.AddTorque(upright * (stats.Stability * 4.2f), ForceMode.Acceleration);
                }

                // 4. Angular velocity damping around longitudinal and pitch axes
                Vector3 localAngularVel = transform.InverseTransformDirection(body.angularVelocity);
                localAngularVel.z *= Mathf.Clamp01(1f - dt * 3.5f);
                localAngularVel.x *= Mathf.Clamp01(1f - dt * 3.0f);
                body.angularVelocity = transform.TransformDirection(localAngularVel);
            }
        }
        private void ApplyStabilityForces()
        {
            if(!isGrounded || spinoutTimer > 0f) return;
            Vector3 tiltVelocity = Vector3.ProjectOnPlane(body.angularVelocity,groundNormal);
            body.AddTorque(Vector3.Cross(transform.up,groundNormal)*28f*stats.Stability-tiltVelocity*7f,ForceMode.Acceleration);
            body.AddForce(-groundNormal*Mathf.Min(5f,SpeedMps*.09f),ForceMode.Acceleration);
        }
        public void ApplySpeedBoost(float multiplier,float duration)
        {
            if (IsLancerActive) return;
            if(duration <= 0 || !float.IsFinite(multiplier)) return;
            boostSpeedMultiplier = Mathf.Max(boostSpeedMultiplier,Mathf.Clamp(multiplier,1f,1.5f));
            boostTimer = Mathf.Max(boostTimer,duration);
            if(isGrounded && raceEnabled) body.AddForce(transform.forward*Mathf.Clamp((multiplier-1f)*15f,0,4.5f),ForceMode.VelocityChange);
            OnBoost?.Invoke();
        }
        public void Trigger360Spin(float duration = 1.0f, bool isHeavy = false)
        {
            if(!raceEnabled || Time.time < spinoutImmunityUntil) return;
            CancelLancer();
            spinoutImmunityUntil = Time.time + duration + 1.25f; // Prevent infinite stun lock
            spinoutDuration = Mathf.Clamp(duration, 0.4f, 1.8f);
            spinoutTimer = spinoutDuration;
            spinoutElapsed = 0f;
            spinoutDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
            isHeavySpin = isHeavy;
            ItemHits++;
            OnItemHit?.Invoke();
            EndDrift(false);
            OnImpact?.Invoke(isHeavy ? .85f : .65f);
            RacingFeedback.Play(RacingCue.Spinout, transform.position);
        }
        public void TriggerSpinout(float duration = 1.0f) => Trigger360Spin(duration, false);

        private void OnCollisionEnter(Collision collision)
        {
            if(!raceEnabled) return;
            bool racerContact=collision.collider.GetComponentInParent<RacingVehiclePhysics>() != null;
            bool wallContact=false;
            for(int i=0;i<collision.contactCount;i++)wallContact|=Mathf.Abs(collision.GetContact(i).normal.y)<.65f;
            if(!racerContact && !wallContact)return;
            float intensity = Mathf.Clamp01(collision.relativeVelocity.magnitude/18f);
            if(intensity < .08f) return;
            OnPhysicalCollision?.Invoke(!racerContact,intensity);
            if(Time.time < impactCooldown)return;
            PhysicalCollisions++;
            if (racerContact) RacerCollisions++;
            else WallCollisions++;
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
            CancelMiniHop();
            ActiveShortcut=null;
            CancelLancer();
            if(body != null && !body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            EndDrift(false); boostTimer = 0; boostSpeedMultiplier = 1; spinoutTimer = 0;
            trickPerformed = false; trickCount = 0; jumpCooldownTimer = trickCooldownTimer = airborneTime = 0f; coyoteTimer = 0f;
            lastMiniTurboTime = lastJumpTime = float.NegativeInfinity;
        }
        public void NotifyJump() { JumpsPerformed++;lastJumpTime=Time.time;if(Time.time-lastMiniTurboTime<2)DriftJumpChains++;OnJump?.Invoke(); }
    }
}
