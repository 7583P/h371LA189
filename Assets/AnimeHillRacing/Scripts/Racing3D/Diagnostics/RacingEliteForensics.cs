#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    // Opt-in measurement. No driving commands, random draws or race-time disk I/O.
    [DefaultExecutionOrder(1500)]
    public sealed class RacingEliteForensics : MonoBehaviour
    {
        public static bool Requested => Array.IndexOf(Environment.GetCommandLineArgs(), "--elite-forensics") >= 0;
        [Serializable] public sealed class Racer
        {
            public string name, style;
            public int grid, mmr, contacts, walls, hits, uses, routeEntries, routeCompletions, routeAborts, passAttempts, positionChanges, recoveries;
            public float finishTime, seconds, nearSafeSeconds, below80SafeSeconds, brakeBelowSafeSeconds, blockedSeconds, offLineSeconds, boostSeconds;
            [NonSerialized] public RacerProgress progress;
            [NonSerialized] public AiVehicleController ai;
            [NonSerialized] public ItemInventory inventory;
            [NonSerialized] public float contactAt = -100, wallAt = -100, hitAt = -100, previousSpeed;
            [NonSerialized] public int previousHits, previousUses, previousEntries, previousCompletions, previousAborts, previousPosition;
            [NonSerialized] public string previousAbort;
        }
        struct Sample
        {
            public int racer, lap, position, route, contacts, walls, hits, uses, heldItem, entries, completions, recoveries, passAttempts, flags;
            public float time, distance, speed, safe, target, throttle, brake, lane, optimalLane, edge, boost, gap, ahead, side, relativeSpeed;
        }
        struct Contact
        {
            public int racer, other, lap, node, route;
            public float time, distance, relativeKmh, speedBefore, speedAtContact, otherKmh, normalForward, normalSide, headingAngle, impulse;
        }
        [Serializable] sealed class Report
        {
            public string classification = "BUFFERED_REAL_UNITY_PHYSICS_OBSERVATION_NO_CAUSAL_CLAIM";
            public Racer[] racers;
            public int sampleCount, contactCount, droppedSamples, droppedContacts;
            public float trackLength;
        }
        RaceManager manager;
        Racer[] racers;
        Sample[] samples = new Sample[80000];
        Contact[] contacts = new Contact[6000];
        int sampleCount, contactCount, droppedSamples, droppedContacts;
        float nextSample;
        public void Bind(RaceManager race)
        {
            manager = race;
            var board = race.ProgressTracker.Leaderboard;
            racers = new Racer[board.Count];
            for (int i = 0; i < racers.Length; i++)
            {
                var p = board[i];
                var ai = p.Vehicle.GetComponent<AiVehicleController>();
                racers[i] = new Racer { name = p.Identity.displayName, grid = p.Identity.gridPosition,
                    style = ai != null ? ai.Profile.style.ToString() : "Human", mmr = ai != null ? ai.Profile.mmr : 0,
                    progress = p, ai = ai, inventory = p.Vehicle.GetComponent<ItemInventory>() };
                p.Vehicle.gameObject.AddComponent<RacingEliteContactObserver>().Bind(this, i);
            }
        }
        void FixedUpdate()
        {
            if (manager == null || manager.State == RaceState.Countdown || manager.State == RaceState.Results) return;
            float now = manager.RaceElapsed, dt = Time.fixedDeltaTime;
            bool sample = now >= nextSample;
            if (sample) nextSample = now + .1f;
            var board = manager.ProgressTracker.Leaderboard;
            for (int i = 0; i < racers.Length; i++)
            {
                var r = racers[i]; var p = r.progress; var car = p.Vehicle; var ai = r.ai;
                if (p.Finished || !car.RaceEnabled || ai == null) continue;
                if (car.ItemHits > r.previousHits) r.hitAt = now;
                r.previousHits = car.ItemHits;
                r.previousSpeed = car.SpeedKmh;
                if (r.previousAbort != ai.LastRouteAbortReason && !string.IsNullOrEmpty(ai.LastRouteAbortReason)) r.routeAborts++;
                r.previousAbort = ai.LastRouteAbortReason;
                if (r.previousPosition > 0 && p.RacePosition < r.previousPosition) r.positionChanges += r.previousPosition - p.RacePosition;
                r.previousPosition = p.RacePosition;
                float safe = ai.CommandSpeedKmh;
                bool eligible = !p.Recovery.IsRecovering && !car.IsSpunOut && safe > 40;
                r.seconds += dt;
                if (eligible && car.SpeedKmh >= safe * .9f && car.SpeedKmh <= safe * 1.1f) r.nearSafeSeconds += dt;
                if (eligible && car.SpeedKmh < safe * .8f) r.below80SafeSeconds += dt;
                if (eligible && car.LastCommands.Brake > .2f && car.SpeedKmh < safe - 8) r.brakeBelowSafeSeconds += dt;
                if (ai.TrafficBraking) r.blockedSeconds += dt;
                if (!ai.ExecutingShortcut && Mathf.Abs(ai.LateralOffset - ai.RacingLineOffset) > 2f) r.offLineSeconds += dt;
                if (car.IsBoosting) r.boostSeconds += dt;
                if (!sample) continue;
                float ahead = 999, side = 999, relativeSpeed = 0;
                for (int j = 0; j < racers.Length; j++)
                {
                    if (j == i || racers[j].progress.Finished) continue;
                    var other = racers[j].progress.Vehicle;
                    Vector3 delta = other.transform.position - car.transform.position;
                    float z = Vector3.Dot(delta, car.transform.forward), x = Mathf.Abs(Vector3.Dot(delta, car.transform.right));
                    if (z > 0 && z < ahead && x < 3f) { ahead = z; side = x; relativeSpeed = car.SpeedKmh - other.SpeedKmh; }
                }
                int flags = (p.Recovery.IsRecovering ? 1 : 0) | (car.IsSpunOut ? 2 : 0) | (car.IsDrifting ? 4 : 0) |
                    (!car.IsGrounded ? 8 : 0) | (car.IsBoosting ? 16 : 0) | (car.IsLancerActive ? 32 : 0) |
                    (ai.TrafficBraking ? 64 : 0) | (now - r.contactAt < 1f ? 128 : 0) |
                    (now - r.wallAt < 1f ? 256 : 0) | (now - r.hitAt < 3f ? 512 : 0) |
                    (ai.CurrentIntent == "CommittedPass" ? 1024 : 0);
                var s = new Sample { racer = i, lap = p.CurrentLap, position = p.RacePosition, route = car.ActiveShortcut != null ? ai.ActiveRouteIndex + 1 : 0,
                    contacts = car.RacerCollisions, walls = car.WallCollisions, hits = car.ItemHits, uses = r.inventory != null ? r.inventory.ItemsUsed : 0,
                    heldItem = r.inventory != null ? (int)r.inventory.HeldItem : 0, entries = ai.ShortcutEntries, completions = ai.ShortcutCompletions, passAttempts = ai.PassingDecisions,
                    recoveries = p.Recovery.RecoveryCount, flags = flags, time = now, distance = p.TotalDistance, speed = car.SpeedKmh,
                    safe = safe, target = ai.TargetSpeedKmh, throttle = car.LastCommands.Throttle, brake = car.LastCommands.Brake,
                    lane = ai.LateralOffset, optimalLane = ai.RacingLineOffset, edge = ai.EdgeUrgency, boost = car.BoostRemainingSeconds,
                    gap = board.Count > 0 ? Mathf.Max(0, board[0].TotalDistance - p.TotalDistance) : 0, ahead = ahead, side = side, relativeSpeed = relativeSpeed };
                if (sampleCount < samples.Length) samples[sampleCount++] = s; else droppedSamples++;
            }
        }
        public void ObserveContact(int index, Collision collision)
        {
            if (manager == null || manager.State == RaceState.Countdown || manager.State == RaceState.Results) return;
            var r = racers[index]; var p = r.progress; if (p.Finished || !p.Vehicle.RaceEnabled) return;
            var other = collision.collider.GetComponentInParent<RacingVehiclePhysics>();
            if (collision.contactCount == 0) return;
            Vector3 normal = collision.GetContact(0).normal;
            if (other == null && Mathf.Abs(normal.y) >= .65f) return;
            if (collision.relativeVelocity.magnitude < 1.44f) return;
            int otherIndex = -1;
            for (int j = 0; j < racers.Length; j++) if (racers[j].progress.Vehicle == other) { otherIndex = j; break; }
            if (other != null) r.contactAt = manager.RaceElapsed; else r.wallAt = manager.RaceElapsed;
            var c = new Contact { racer = index, other = otherIndex, lap = p.CurrentLap, node = r.ai != null ? r.ai.CurrentNodeIndex : -1,
                route = p.Vehicle.ActiveShortcut != null && r.ai != null ? r.ai.ActiveRouteIndex + 1 : 0,
                time = manager.RaceElapsed, distance = p.TotalDistance, relativeKmh = collision.relativeVelocity.magnitude * 3.6f,
                speedBefore = r.previousSpeed, speedAtContact = p.Vehicle.SpeedKmh, otherKmh = other != null ? other.SpeedKmh : 0,
                normalForward = Vector3.Dot(normal, p.Vehicle.transform.forward), normalSide = Vector3.Dot(normal, p.Vehicle.transform.right),
                headingAngle = other != null ? Vector3.Angle(p.Vehicle.transform.forward, other.transform.forward) : 0, impulse = collision.impulse.magnitude };
            if (contactCount < contacts.Length) contacts[contactCount++] = c; else droppedContacts++;
        }
        public void Export(string output)
        {
            foreach (var r in racers)
            {
                var car = r.progress.Vehicle; var ai = r.ai;
                r.contacts = car.RacerCollisions; r.walls = car.WallCollisions; r.hits = car.ItemHits;
                r.uses = r.inventory != null ? r.inventory.ItemsUsed : 0;
                r.finishTime = r.progress.FinishTime; r.recoveries = r.progress.Recovery.RecoveryCount;
                if (ai != null) { r.routeEntries = ai.ShortcutEntries; r.routeCompletions = ai.ShortcutCompletions; r.passAttempts = ai.PassingDecisions; }
            }
            File.WriteAllText(Path.Combine(output, "elite-forensics.json"), JsonUtility.ToJson(new Report { racers = racers, sampleCount = sampleCount,
                contactCount = contactCount, droppedSamples = droppedSamples, droppedContacts = droppedContacts, trackLength = manager.ProgressTracker.TrackGraph.TotalLength }, true));
            using (var file = new StreamWriter(Path.Combine(output, "elite-samples.csv")))
            {
                file.WriteLine("racer,time,lap,position,distance,speed,safe,target,throttle,brake,lane,optimal_lane,edge,route,boost,gap,ahead,side,relative_speed,contacts,walls,hits,uses,held_item,entries,completions,recoveries,flags,pass_attempts");
                for (int i = 0; i < sampleCount; i++) { var s = samples[i]; file.WriteLine(FormattableString.Invariant($"{s.racer},{s.time},{s.lap},{s.position},{s.distance},{s.speed},{s.safe},{s.target},{s.throttle},{s.brake},{s.lane},{s.optimalLane},{s.edge},{s.route},{s.boost},{s.gap},{s.ahead},{s.side},{s.relativeSpeed},{s.contacts},{s.walls},{s.hits},{s.uses},{s.heldItem},{s.entries},{s.completions},{s.recoveries},{s.flags},{s.passAttempts}")); }
            }
            using (var file = new StreamWriter(Path.Combine(output, "elite-contacts.csv")))
            {
                file.WriteLine("racer,other,time,lap,node,distance,route,relative_kmh,speed_before,speed_at_contact,other_kmh,normal_forward,normal_side,heading_angle,impulse");
                for (int i = 0; i < contactCount; i++) { var c = contacts[i]; file.WriteLine(FormattableString.Invariant($"{c.racer},{c.other},{c.time},{c.lap},{c.node},{c.distance},{c.route},{c.relativeKmh},{c.speedBefore},{c.speedAtContact},{c.otherKmh},{c.normalForward},{c.normalSide},{c.headingAngle},{c.impulse}")); }
            }
        }
    }
    public sealed class RacingEliteContactObserver : MonoBehaviour
    {
        RacingEliteForensics probe; int index;
        public void Bind(RacingEliteForensics recorder, int racerIndex) { probe = recorder; index = racerIndex; }
        void OnCollisionEnter(Collision collision) { if (probe != null) probe.ObserveContact(index, collision); }
    }
}
#endif
