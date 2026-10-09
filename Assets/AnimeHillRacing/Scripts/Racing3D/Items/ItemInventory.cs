using System;
using UnityEngine;
using Unity.Profiling;
using AnimeHillRacing.Core;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(RacingVehiclePhysics))]
    public sealed class ItemInventory : MonoBehaviour
    {
        static readonly ProfilerMarker TickMarker=new ProfilerMarker("Racing.Items.Tick");
        private RacingVehiclePhysics vehicle;
        private RacingItemPool pool;
        private TrackProgressTracker tracker;
        private RaceItemType heldItem = RaceItemType.None, pendingItem;
        private bool rolling, shieldActive;
        private float rollStarted, shieldUntil, lastUse, aerialRecoveryUntil;
        private int charges;
        private RacingItemVisualSet visuals;
        private PlayerRacingInput playerInput;
        private bool coinRewardsDirty;
        public int SharedCoinsAwarded { get; private set; }
        public int ChargesRemaining => charges;
        public RaceItemType PreviousItem { get; private set; }
        public RaceItemType PreviousPreviousItem { get; private set; }
        private readonly Collider[] hits = new Collider[64];
        private readonly RaycastHit[] sightHits = new RaycastHit[32];
        private readonly RacingVehiclePhysics[] affected = new RacingVehiclePhysics[12];

        public RaceItemType HeldItem => heldItem;
        public bool HasItem => heldItem != RaceItemType.None && !rolling;
        public bool IsRolling => rolling;
        public float RouletteProgress => rolling ? Mathf.Clamp01((Time.time - rollStarted) / .9f) : 1f;
        public bool HasShield => shieldActive && Time.time < shieldUntil;
        public float ShieldRemaining => HasShield ? shieldUntil - Time.time : 0;
        public DeterministicRng ItemRng { get; private set; }
        public int ItemsUsed { get; private set; }
        public int AcquisitionSerial { get; private set; }
        public float LastAcquiredTime { get; private set; }
        public bool HasMissileTarget => FindTarget() != null;
        public bool HasPredictedFiringWindow(float horizon,float maxAngle)
        {
            Transform target=FindTarget();if(target==null)return false;
            var rival=target.GetComponent<RacingVehiclePhysics>();if(rival==null)return false;
            Vector3 relative=rival.Body.linearVelocity-vehicle.Body.linearVelocity;
            Vector3 future=target.position-transform.position+relative*Mathf.Clamp(horizon,0,.8f);
            return Vector3.Dot(future,transform.forward)>2&&Vector3.Angle(Vector3.ProjectOnPlane(future,Vector3.up),Vector3.ProjectOnPlane(transform.forward,Vector3.up))<maxAngle;
        }

        public event Action<RaceItemType> OnItemReceived;
        public event Action<RaceItemType> OnItemUsed;

        private void Awake() {
            vehicle = GetComponent<RacingVehiclePhysics>(); playerInput = GetComponent<PlayerRacingInput>();
            vehicle.OnItemRequested += UseCommandItem;
            // Prepare the transformation before the race, before the held-item
            // holograms exist. Activation only toggles cached renderers.
            if (GetComponent<RacingLancerVisuals>() == null) gameObject.AddComponent<RacingLancerVisuals>();
            visuals = gameObject.AddComponent<RacingItemVisualSet>(); visuals.Show(RaceItemType.None, false);
        }
        public void SetSeed(int seed) { ItemRng = new DeterministicRng(seed); pool = RacingItemPool.ForScene(); playerInput = GetComponent<PlayerRacingInput>(); }
        public void BindRaceProgress(TrackProgressTracker progress) { tracker=progress; }

        private void Update()
        { using(TickMarker.Auto()) TickInventory(); }
        private void TickInventory()
        {
            if (shieldActive && Time.time >= shieldUntil) shieldActive = false;
            if (rolling && Time.time - rollStarted >= .9f)
            {
                rolling = false;
                heldItem = pendingItem; RememberAcquisition(heldItem);
                AcquisitionSerial++;
                LastAcquiredTime = Time.time;
                OnItemReceived?.Invoke(heldItem);
                if (playerInput != null) RacingFeedback.Play(RacingCue.ItemReady);
            }
            visuals.Show(HasItem ? heldItem : HasShield ? RaceItemType.MagneticShield : RaceItemType.None, HasShield);
        }

        public void AcquireItem(RaceItemType type, bool rollRoulette = true)
        {
            if (HasItem || rolling || type == RaceItemType.None) return;
            if (rollRoulette) { rolling = true; pendingItem = type; rollStarted = Time.time; }
            else { heldItem = type; RememberAcquisition(type); AcquisitionSerial++; LastAcquiredTime = Time.time; OnItemReceived?.Invoke(type); }
        }

        void RememberAcquisition(RaceItemType type) {
            PreviousPreviousItem = PreviousItem; PreviousItem = type;
            charges = type == RaceItemType.TripleTurbo ? 3 : 1;
        }
        void FixedUpdate() {
            if (Time.time >= aerialRecoveryUntil || vehicle.IsGrounded || !vehicle.RaceEnabled) return;
            // Universal, bounded item assist: damp pitch/roll while retaining horizontal
            // momentum. It cannot relocate the racer or bypass checkpoints.
            Vector3 tilt = Vector3.Cross(transform.up, Vector3.up);
            Vector3 omega = Vector3.ProjectOnPlane(vehicle.Body.angularVelocity, Vector3.up);
            vehicle.Body.AddTorque(tilt * 12f - omega * 5f, ForceMode.Acceleration);
        }

        public bool UseItem()
        {
            if (!HasItem || vehicle == null || !vehicle.RaceEnabled || Time.time - lastUse < .18f) return false;
            if (pool == null) pool = RacingItemPool.ForScene();
            if (heldItem == RaceItemType.AerialRecovery && vehicle.IsGrounded) return false;
            if (heldItem == RaceItemType.TripleTurbo && Time.time - lastUse < .45f) return false;
            if (vehicle.IsLancerActive && RaceItemUtility.IsBoost(heldItem)) return false;
            if (heldItem == RaceItemType.Lancer && !vehicle.TryActivateLancer()) return false;
            Transform goldenTarget=heldItem==RaceItemType.GoldenRock?FindTarget(RaceItemType.GoldenRock,vehicle.LastCommands.ItemBackward):null;
            if(heldItem==RaceItemType.GoldenRock&&goldenTarget==null)return false;
            var used = heldItem; if (--charges <= 0) heldItem = RaceItemType.None; lastUse = Time.time; ItemsUsed++;
            switch (used)
            {
                case RaceItemType.GoldenRock:
                    Vector3 goldenDirection=vehicle.LastCommands.ItemBackward?-transform.forward:transform.forward;
                    Vector3 goldenOrigin=transform.position+Vector3.up*.35f;
                    float goldenMuzzle=2.2f;
                    int goldenObstacles=Physics.SphereCastNonAlloc(goldenOrigin,.28f,goldenDirection,sightHits,goldenMuzzle,~0,QueryTriggerInteraction.Ignore);
                    for(int i=0;i<goldenObstacles;i++)if(sightHits[i].collider.GetComponentInParent<RacingVehiclePhysics>()!=vehicle)
                        goldenMuzzle=Mathf.Min(goldenMuzzle,Mathf.Max(.25f,sightHits[i].distance-.35f));
                    var golden=pool.Fire(vehicle,goldenOrigin+goldenDirection*goldenMuzzle,Quaternion.LookRotation(goldenDirection,Vector3.up),used);
                    if(golden!=null)golden.SetTarget(goldenTarget);
                    break;
                case RaceItemType.Lancer:
                    RacingFeedback.Play(RacingCue.SuperTurbo, transform.position);
                    break;
                case RaceItemType.Coin10:
                    // Consume the inventory charge before crediting. Repeated touch/keyboard
                    // callbacks cannot award again; AI never credits the human wallet.
                    if (playerInput != null) {
                        SaveManager.AddCoins(RaceItemUtility.CoinReward, false);
                        SharedCoinsAwarded += RaceItemUtility.CoinReward;
                        coinRewardsDirty = true;
                    }
                    break;
                case RaceItemType.MiniTurbo:
                    vehicle.ApplySpeedBoost(1.20f, .8f);
                    RacingFeedback.Play(RacingCue.ItemTurbo, transform.position);
                    break;
                case RaceItemType.TurboPulse:
                    vehicle.ApplySpeedBoost(1.35f, 2.0f); // Exactly 2.0s duration per Requirement 12
                    RacingFeedback.Play(RacingCue.ItemTurbo, transform.position);
                    break;
                case RaceItemType.TripleTurbo:
                    vehicle.ApplySpeedBoost(1.32f, 1.1f);
                    RacingFeedback.Play(RacingCue.ItemTurbo, transform.position);
                    break;
                case RaceItemType.AerialRecovery:
                    aerialRecoveryUntil = Time.time + 1.1f;
                    vehicle.Body.AddForce(Vector3.up * 3.2f, ForceMode.VelocityChange);
                    vehicle.ApplySpeedBoost(1.12f, 1.0f);
                    shieldActive = true; shieldUntil = Time.time + 1.1f;
                    break;
                case RaceItemType.PulseMine:
                    Vector3 minePosition = transform.position - transform.forward * 2.4f;
                    if (Physics.Raycast(minePosition + Vector3.up * 2, Vector3.down, out var mineGround, 5, ~0, QueryTriggerInteraction.Ignore)) minePosition = mineGround.point + Vector3.up * .18f;
                    pool.DropPulseMine(vehicle, minePosition, Quaternion.identity);
                    break;
                case RaceItemType.HyperCharge:
                    vehicle.ApplySpeedBoost(1.45f, 2.8f);
                    shieldActive = true;
                    shieldUntil = Time.time + 2.8f;
                    break;
                case RaceItemType.MagneticShield:
                    shieldActive = true;
                    shieldUntil = Time.time + 5.5f;
                    break;
                case RaceItemType.TwinRock:
                case RaceItemType.ElectroSpike: // Rock Item
                    bool throwBackward = vehicle.LastCommands.ItemBackward;
                    Vector3 rockPos = throwBackward ? transform.position - transform.forward * 2.1f : transform.position + transform.forward * 2.4f + Vector3.up * 0.4f;
                    Vector3 rockDir = throwBackward ? -transform.forward : transform.forward;
                    if (Physics.Raycast(rockPos + Vector3.up * 1.5f, Vector3.down, out var ground, 4, ~0, QueryTriggerInteraction.Ignore))
                        rockPos = ground.point + Vector3.up * 0.15f;
                    if (used == RaceItemType.TwinRock) {
                        pool.ThrowRock(vehicle, rockPos - transform.right * .65f, rockDir, throwBackward);
                        pool.ThrowRock(vehicle, rockPos + transform.right * .65f, rockDir, throwBackward);
                    } else pool.ThrowRock(vehicle, rockPos, rockDir, throwBackward);
                    break;
                case RaceItemType.HomingRocket:
                case RaceItemType.TrackingDart:
                case RaceItemType.KineticMissile: // Dart Item
                    Vector3 fireDirection = vehicle.LastCommands.ItemBackward ? -transform.forward : transform.forward;
                    Vector3 origin = transform.position + Vector3.up * .35f;
                    float muzzleDistance = 2.2f;
                    int obstacles = Physics.SphereCastNonAlloc(origin, .18f, fireDirection, sightHits, muzzleDistance, ~0, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < obstacles; i++)
                    {
                        if (sightHits[i].collider.GetComponentInParent<RacingVehiclePhysics>() != vehicle)
                            muzzleDistance = Mathf.Min(muzzleDistance, Mathf.Max(.25f, sightHits[i].distance - .35f));
                    }
                    var targetRacer = FindTarget(used, vehicle.LastCommands.ItemBackward);
                    Quaternion launchRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(fireDirection, Vector3.up), Vector3.up);
                    if (targetRacer != null)
                    {
                        var targetBody = targetRacer.GetComponentInParent<Rigidbody>();
                        var targetVehicle = targetRacer.GetComponentInParent<RacingVehiclePhysics>();
                        Vector3 targetPos = targetRacer.position + Vector3.up * 0.35f;
                        float distance = (targetPos - origin).magnitude;
                        var ai = GetComponent<AiVehicleController>();
                        float anticipation = ai != null ? ai.Profile.itemTiming : used == RaceItemType.KineticMissile ? 0 : 1;
                        float flightTime = distance / (used == RaceItemType.HomingRocket ? 54f : 64f) * anticipation; // 64 m/s projectile speed

                        Vector3 targetVelocity = targetBody != null ? targetBody.linearVelocity : (targetVehicle != null ? targetVehicle.transform.forward * targetVehicle.SpeedMps : Vector3.zero);
                        Vector3 predictedTargetPosition = targetPos + targetVelocity * flightTime;

                        // Expected curve / racing-line adjustment:
                        if (tracker != null && tracker.TrackGraph != null)
                        {
                            tracker.TrackGraph.ProjectPosition(targetPos, out _, out float targetTrackDist, out _);
                            var futureTrackNode = tracker.TrackGraph.SampleDistance(targetTrackDist + targetVelocity.magnitude * flightTime);
                            predictedTargetPosition = Vector3.Lerp(predictedTargetPosition, futureTrackNode.Position + Vector3.up * 0.35f, 0.40f);
                        }

                        // Drift slip adjustment:
                        if (targetVehicle != null && targetVehicle.IsDrifting)
                        {
                            Vector3 driftLateral = targetVehicle.DriftDirection > 0 ? targetVehicle.transform.right : -targetVehicle.transform.right;
                            predictedTargetPosition += driftLateral * (Mathf.Abs(targetVehicle.SlipAngle) * 0.08f);
                        }

                        Vector3 leadDir = Vector3.ProjectOnPlane(predictedTargetPosition - origin, Vector3.up).normalized;
                        if (Vector3.Dot(leadDir, fireDirection) > 0.35f)
                            launchRot = Quaternion.LookRotation(leadDir, Vector3.up);
                    }
                    var missile = pool.Fire(vehicle, origin + fireDirection * muzzleDistance, launchRot, used);
                    if (missile != null && used != RaceItemType.KineticMissile) missile.SetTarget(targetRacer);
                    break;
                case RaceItemType.ShockwavePulse:
                    Shockwave();
                    break;
            }
            if (playerInput != null) RacingFeedback.Play(RacingCue.ItemUse);
            bool thrown = used == RaceItemType.GoldenRock || used == RaceItemType.ElectroSpike || used == RaceItemType.TwinRock || used == RaceItemType.KineticMissile || used == RaceItemType.TrackingDart || used == RaceItemType.HomingRocket;
            Vector3 effectDirection = thrown ? (vehicle.LastCommands.ItemBackward ? -transform.forward : transform.forward) * 3f : Vector3.up;
            RacingVfxPool.Burst(transform.position + (used == RaceItemType.Coin10 ? Vector3.up * 1.35f : transform.forward * .8f), RaceItemUtility.ColorFor(used), used == RaceItemType.Coin10 ? 6 : 10, effectDirection);
            OnItemUsed?.Invoke(used);
            return true;
        }

        public void FlushCoinRewards()
        {
            if (!coinRewardsDirty) return;
            PlayerPrefs.Save();
            coinRewardsDirty = false;
        }
        private void OnApplicationPause(bool paused) { if (paused) FlushCoinRewards(); }
        private void OnApplicationQuit() { FlushCoinRewards(); }
        private void UseCommandItem() { UseItem(); }
        private void OnDestroy() { if(vehicle!=null)vehicle.OnItemRequested-=UseCommandItem;FlushCoinRewards(); }

        Transform FindTarget(RaceItemType used = RaceItemType.None, bool backward = false)
        {
            if (tracker == null) tracker = FindFirstObjectByType<TrackProgressTracker>();
            var targetingItem = used != RaceItemType.None ? used : heldItem;
            if(targetingItem==RaceItemType.GoldenRock)
            {
                if(tracker==null)return null;
                var source=tracker.FindRacer(vehicle);if(source==null)return null;
                int desired=source.RacePosition+(backward?1:-1);
                if(desired<1||desired>tracker.RacersCount)return null;
                var ordered=tracker.Leaderboard;
                for(int i=0;i<ordered.Count;i++) {
                    var racer=ordered[i];
                    if(racer.RacePosition==desired&&!racer.Finished&&racer.Vehicle!=null&&racer.Vehicle!=vehicle&&racer.Vehicle.RaceEnabled)
                        return racer.Vehicle.transform;
                }
                return null;
            }
            float nearest = targetingItem == RaceItemType.HomingRocket ? 80f : targetingItem == RaceItemType.TrackingDart ? 60f : 48f;
            Transform result = null;
            if (tracker == null) return null;
            var leaderboard = tracker.Leaderboard;
            for (int racerIndex = 0; racerIndex < leaderboard.Count; racerIndex++)
            {
                var racer = leaderboard[racerIndex];
                if (racer.Vehicle == vehicle || racer.Vehicle == null || racer.Finished) continue;
                Vector3 delta = racer.Vehicle.transform.position - transform.position;
                float distance = delta.magnitude;
                if (distance >= nearest || Vector3.Dot(delta.normalized, backward ? -transform.forward : transform.forward) < (targetingItem == RaceItemType.HomingRocket ? .35f : targetingItem == RaceItemType.TrackingDart ? .50f : .65f)) continue;
                Vector3 origin = transform.position + Vector3.up * .35f;
                Vector3 aim = racer.Vehicle.transform.position + Vector3.up * .35f - origin;
                int count = Physics.RaycastNonAlloc(origin, aim.normalized, sightHits, aim.magnitude, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int i = 0; i < count; i++)
                {
                    var hitVehicle = sightHits[i].collider.GetComponentInParent<RacingVehiclePhysics>();
                    if (hitVehicle != vehicle && hitVehicle != racer.Vehicle) { blocked = true; break; }
                }
                if (blocked) continue;
                nearest = distance;
                result = racer.Vehicle.transform;
            }
            return result;
        }

        void Shockwave()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, 8, hits, ~0, QueryTriggerInteraction.Ignore), visited = 0;
            for (int i = 0; i < count; i++)
            {
                var rival = hits[i].GetComponentInParent<RacingVehiclePhysics>();
                if (rival == null || rival == vehicle || !rival.RaceEnabled) continue;
                bool duplicate = false;
                for (int j = 0; j < visited; j++) if (affected[j] == rival) { duplicate = true; break; }
                if (duplicate) continue;
                if (visited < affected.Length) affected[visited++] = rival;
                var defense = rival.GetComponent<ItemInventory>();
                if (defense != null && defense.AbsorbHit()) continue;
                Vector3 direction = Vector3.ProjectOnPlane(rival.transform.position - transform.position, Vector3.up).normalized;
                rival.Body.AddForce(direction * 3.5f, ForceMode.VelocityChange);
                rival.Trigger360Spin(.85f, false);
            }
            Array.Clear(affected, 0, affected.Length);
        }

        public bool AbsorbHit()
        {
            if (!HasShield) return false;
            shieldActive = false;
            RacingVfxPool.Burst(transform.position, RacingDesign.Cyan, 10);
            return true;
        }
    }
}
