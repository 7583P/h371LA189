using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [Serializable]
    public sealed class RacerProgress
    {
        public RacerIdentity Identity;
        public RacingVehiclePhysics Vehicle;
        public VehicleRecovery Recovery;

        public int CurrentLap = 1;
        public int LastCheckpointIndex = -1;
        public int CheckpointsPassedCount;
        public float DistanceAlongTrack;
        public float TotalDistance;
        public float NormalizedProgress;
        public int RacePosition = 12; // 1 to 12
        public bool Finished;
        public int FinishPosition;
        public float FinishTime;
        public float CurrentLapStartTime;
        public float BestLapTime = float.MaxValue;
        public int CheckpointVisits;
        public float LastCheckpointTime;
        public float LastLapDuration;
        public List<float> MeasuredLapTimes=new List<float>(3);
        public bool HasCrossedStart;

        // Safe recovery state to guarantee progress context is never lost or corrupted on off-track recovery
        public Vector3 SafePosition;
        public Quaternion SafeRotation = Quaternion.identity;
        public int SafeLap = 1;
        public int SafeCheckpointIndex = -1;
        public int SafeCheckpointsPassedCount;
        public float SafeDistanceAlongTrack;
        public float SafeTotalDistance;
        public bool SafeHasCrossedStart;
        public int SafeNearestNode;
        public bool HasSafeState;

        public void SnapshotSafeState(Vector3 pos, Quaternion rot, int node, float distAlongTrack, float totalDist)
        {
            SafePosition = pos;
            SafeRotation = rot;
            SafeLap = Mathf.Max(SafeLap, CurrentLap);
            SafeCheckpointIndex = LastCheckpointIndex;
            SafeCheckpointsPassedCount = Mathf.Max(SafeCheckpointsPassedCount, CheckpointsPassedCount);
            SafeDistanceAlongTrack = distAlongTrack;
            SafeTotalDistance = Mathf.Max(SafeTotalDistance, totalDist);
            SafeHasCrossedStart = HasCrossedStart;
            SafeNearestNode = node;
            HasSafeState = true;
        }

        public void RestoreSafeState()
        {
            if (!HasSafeState) return;
            CurrentLap = Mathf.Max(CurrentLap, SafeLap);
            LastCheckpointIndex = SafeCheckpointIndex;
            CheckpointsPassedCount = Mathf.Max(CheckpointsPassedCount, SafeCheckpointsPassedCount);
            DistanceAlongTrack = SafeDistanceAlongTrack;
            TotalDistance = Mathf.Max(TotalDistance, SafeTotalDistance);
            HasCrossedStart = SafeHasCrossedStart;
        }

        public float GetDisplayBestLap() => BestLapTime < float.MaxValue ? BestLapTime : 0f;
    }

    public sealed class TrackProgressTracker : MonoBehaviour
    {
        [SerializeField] private TrackSplineGraph trackGraph;
        [SerializeField] private int totalLaps = 3;
        [SerializeField] private int totalCheckpointsCount = 8;

        private readonly List<RacerProgress> racers = new List<RacerProgress>(12);
        private readonly List<RacerProgress> sortedRacers = new List<RacerProgress>(12);
        private int finishedCount;

        public int TotalLaps => totalLaps;
        public int TotalCheckpointsCount => totalCheckpointsCount;
        public int RacersCount => racers.Count;
        public IReadOnlyList<RacerProgress> Leaderboard => sortedRacers;
        public TrackSplineGraph TrackGraph => trackGraph;

        public event Action<RacerProgress, int> OnRacerLapCompleted; // racer, lapNumber
        public event Action<RacerProgress, int> OnRacerFinished;     // racer, finishPosition
        public event Action<RacerProgress,int,float> OnCheckpointReached;

        public void Initialize(TrackSplineGraph graph, int laps, int checkpoints = 8)
        {
            trackGraph = graph;
            totalLaps = Mathf.Max(1, laps);
            totalCheckpointsCount = Mathf.Max(1, checkpoints);
            racers.Clear();
            sortedRacers.Clear();
            finishedCount = 0;
        }

        public RacerProgress RegisterRacer(RacerIdentity identity, RacingVehiclePhysics vehicle, VehicleRecovery recovery)
        {
            var p = new RacerProgress
            {
                Identity = identity,
                Vehicle = vehicle,
                Recovery = recovery,
                CurrentLap = 1,
                LastCheckpointIndex = -1,
                CheckpointsPassedCount = 0,
                DistanceAlongTrack = 0f,
                TotalDistance = 0f,
                NormalizedProgress = 0f,
                RacePosition = racers.Count + 1,
                Finished = false,
                FinishPosition = 0,
                FinishTime = 0f,
                CurrentLapStartTime = 0f,
                BestLapTime = float.MaxValue
            };

            if (recovery != null)
            {
                recovery.OnVehicleRecovered += (spawnPos) =>
                {
                    p.RestoreSafeState();
                };
            }

            racers.Add(p);
            sortedRacers.Add(p);
            return p;
        }

        public void NotifyCheckpoint(RacingVehiclePhysics vehicle, int checkpointIndex, bool isFinishLine, float raceTime)
        {
            RacerProgress p = FindRacer(vehicle);
            if (p == null || p.Finished) return;

            if (checkpointIndex < 0 || checkpointIndex >= totalCheckpointsCount || !float.IsFinite(raceTime)) return;

            // Start line crossing (gate 0)
            if (!p.HasCrossedStart)
            {
                if (checkpointIndex != 0) return; // Must cross gate 0 first to start race
                p.HasCrossedStart = true;
                p.CurrentLapStartTime = raceTime;
                p.SafeHasCrossedStart = true;
                p.SafeLap = 1;
                p.SafeCheckpointIndex = 0;
                p.SafeCheckpointsPassedCount = 0;
                p.SafeDistanceAlongTrack = 0f;
                p.SafeTotalDistance = 0f;
                p.LastCheckpointIndex = 0;
                p.CheckpointVisits++;
                p.LastCheckpointTime = raceTime;
                OnCheckpointReached?.Invoke(p,checkpointIndex,raceTime);
                return;
            }

            // Duplicate trigger of current gate is ignored
            if (checkpointIndex == p.LastCheckpointIndex) return;

            int prevCheckpoint = p.LastCheckpointIndex;

            if (!isFinishLine)
            {
                int expectedNext = (p.LastCheckpointIndex + 1) % totalCheckpointsCount;
                // Both authored shortcuts rejoin before the next required gate.
                // Proximity/projection alone never grants unvisited checkpoints.
                if (checkpointIndex == expectedNext)
                {
                    p.LastCheckpointIndex = checkpointIndex;
                    p.CheckpointVisits++;
                    p.LastCheckpointTime = raceTime;
                    p.CheckpointsPassedCount++;

                    p.SafeCheckpointIndex = p.LastCheckpointIndex;
                    p.SafeCheckpointsPassedCount = p.CheckpointsPassedCount;
                    p.SafeLap = p.CurrentLap;
                    OnCheckpointReached?.Invoke(p,checkpointIndex,raceTime);
                }
                return;
            }

            // Finish line crossed (checkpoint 0 / isFinishLine == true)
            // Legitimate lap completion requires completing a substantial portion of the circuit
            bool validLapCompletion = p.CheckpointsPassedCount == totalCheckpointsCount - 1 &&
                                     prevCheckpoint == totalCheckpointsCount - 1;

            if (validLapCompletion)
            {
                p.LastCheckpointIndex = 0;
                p.CheckpointVisits++;
                p.LastCheckpointTime = raceTime;
                OnCheckpointReached?.Invoke(p,checkpointIndex,raceTime);

                float lapDuration = raceTime - p.CurrentLapStartTime;
                p.LastLapDuration = lapDuration;
                if (lapDuration > 5f)
                {
                    p.MeasuredLapTimes.Add(lapDuration);
                    if (lapDuration < p.BestLapTime)
                    {
                        p.BestLapTime = lapDuration;
                    }
                }

                p.CurrentLapStartTime = raceTime;
                p.CheckpointsPassedCount = 0;

                if (p.CurrentLap >= totalLaps)
                {
                    // Racer Finished!
                    p.Finished = true;
                    finishedCount++;
                    p.FinishPosition = finishedCount;
                    p.FinishTime = raceTime;
                    p.RacePosition = finishedCount;

                    OnRacerFinished?.Invoke(p, finishedCount);
                }
                else
                {
                    p.CurrentLap++;
                    p.SafeLap = p.CurrentLap;
                    p.SafeCheckpointIndex = 0;
                    p.SafeCheckpointsPassedCount = 0;
                    float trackLength = trackGraph != null ? Mathf.Max(10f, trackGraph.TotalLength) : 1000f;
                    p.SafeDistanceAlongTrack = 0f;
                    p.SafeTotalDistance = (p.CurrentLap - 1) * trackLength;
                    p.DistanceAlongTrack = 0f;
                    p.TotalDistance = Mathf.Max(p.TotalDistance, p.SafeTotalDistance);
                    OnRacerLapCompleted?.Invoke(p, p.CurrentLap);
                }
            }
        }

        public void UpdateProgress(float raceTime)
        {
            if (trackGraph == null || racers.Count == 0) return;

            float trackLen = Mathf.Max(10f, trackGraph.TotalLength);

            // 1. Calculate each racer's continuous distance along track
            for (int i = 0; i < racers.Count; i++)
            {
                RacerProgress r = racers[i];
                if (r.Vehicle == null) continue;

                if (!r.Finished)
                {
                    bool isRecoveringOrFalling = r.Recovery != null && (r.Recovery.IsRecovering || r.Recovery.IsFallingIntoVoid);
                    if (!isRecoveringOrFalling)
                    {
                        Vector3 carPos = r.Vehicle.transform.position;

                        // Check if car is progressing along an authored shortcut
                        bool onShortcut = false;
                        float distAlongTrack = 0f;
                        int nearestNode = 0;
                        float lateral = 0f;

                        if (trackGraph.Shortcuts != null)
                        {
                            for (int scIdx = 0; scIdx < trackGraph.Shortcuts.Length; scIdx++)
                            {
                                var sc = trackGraph.Shortcuts[scIdx];
                                if (sc == null || !sc.IsValid() || sc.pathPoints == null || sc.pathPoints.Length < 2) continue;

                                for (int pt = 1; pt < sc.pathPoints.Length; pt++)
                                {
                                    Vector3 a = sc.pathPoints[pt - 1], delta = sc.pathPoints[pt] - a;
                                    float sqrLen = delta.sqrMagnitude;
                                    if (sqrLen < 0.01f) continue;
                                    float fraction = Mathf.Clamp01(Vector3.Dot(carPos - a, delta) / sqrLen);
                                    Vector3 proj = a + delta * fraction;
                                    if ((carPos - proj).sqrMagnitude < sc.width * sc.width * 0.45f)
                                    {
                                        onShortcut = true;
                                        float entryDist = trackGraph.GetNode(sc.entryNodeIndex).CumulativeDistance;
                                        float exitDist = trackGraph.GetNode(sc.exitNodeIndex).CumulativeDistance;
                                        if (exitDist < entryDist) exitDist += trackLen;
                                        float pathT = (pt - 1 + fraction) / (sc.pathPoints.Length - 1);
                                        distAlongTrack = Mathf.Repeat(Mathf.Lerp(entryDist, exitDist, pathT), trackLen);
                                        nearestNode = pathT > 0.5f ? sc.exitNodeIndex : sc.entryNodeIndex;
                                        lateral = 0f;
                                        break;
                                    }
                                }
                                if (onShortcut) break;
                            }
                        }

                        if (!onShortcut)
                        {
                            trackGraph.ProjectPosition(
                                carPos,
                                out nearestNode,
                                out distAlongTrack,
                                out lateral
                            );
                        }

                        var road = trackGraph.SampleDistance(distAlongTrack);
                        bool isSafeOnRoad = r.Vehicle.IsGrounded && !r.Vehicle.IsOffroad &&
                            Mathf.Abs(lateral) < road.TrackWidth * .45f && Mathf.Abs(carPos.y - road.Position.y) < 3.0f;

                        float proposedTotalDist = (r.CurrentLap - 1) * trackLen + distAlongTrack;
                        if (!r.HasCrossedStart && distAlongTrack > trackLen * 0.75f) proposedTotalDist -= trackLen;

                        // Guard against erratic projection snaps (e.g. adjacent hairpins / tunnels)
                        if (r.HasSafeState && proposedTotalDist < r.SafeTotalDistance - 30f && !isRecoveringOrFalling)
                        {
                            proposedTotalDist = r.TotalDistance;
                        }

                        r.DistanceAlongTrack = distAlongTrack;
                        r.TotalDistance = Mathf.Max(r.TotalDistance, proposedTotalDist);
                        r.NormalizedProgress = totalLaps > 0 ? Mathf.Clamp01(r.TotalDistance / (totalLaps * trackLen)) : 0f;

                        if (isSafeOnRoad)
                        {
                            // Update safe recovery anchor regularly when verified securely on road
                            if (r.Recovery != null && !r.Recovery.IsRecovering)
                            {
                                trackGraph.GetSafeRecoveryAnchor(nearestNode, out Vector3 safePos, out Quaternion safeRot);
                                r.Recovery.SetLastSafeTransform(safePos, safeRot);
                                r.SnapshotSafeState(safePos, safeRot, nearestNode, distAlongTrack, r.TotalDistance);
                            }
                        }
                    }
                }
            }

            // 2. Sort leaderboard:
            // - Finished racers first, ordered by FinishPosition ascending.
            // - Lap priority: Racer on higher lap is ALWAYS ahead (Lap 3 > Lap 2 > Lap 1).
            // - Checkpoints passed count in current lap.
            // - Total distance along circuit.
            sortedRacers.Sort((a, b) =>
            {
                if (a.Finished && b.Finished)
                    return a.FinishPosition.CompareTo(b.FinishPosition);
                if (a.Finished) return -1;
                if (b.Finished) return 1;

                // 1. PRIMARY: Lap comparison (Lap 3 ALWAYS beats Lap 2)
                if (a.CurrentLap != b.CurrentLap)
                    return b.CurrentLap.CompareTo(a.CurrentLap);

                // 2. SECONDARY: Checkpoints in current lap
                if (a.CheckpointsPassedCount != b.CheckpointsPassedCount)
                    return b.CheckpointsPassedCount.CompareTo(a.CheckpointsPassedCount);

                // 3. TERTIARY: Continuous TotalDistance
                int distanceOrder = b.TotalDistance.CompareTo(a.TotalDistance);
                return distanceOrder != 0 ? distanceOrder : a.Identity.gridPosition.CompareTo(b.Identity.gridPosition);
            });

            // 3. Assign real-time race positions (1 to 12)
            for (int i = 0; i < sortedRacers.Count; i++)
            {
                sortedRacers[i].RacePosition = i + 1;
            }
        }

        public void NotifyVehicleRecovered(RacingVehiclePhysics vehicle)
        {
            RacerProgress p = FindRacer(vehicle);
            if (p != null)
            {
                p.RestoreSafeState();
            }
        }

        public RacerProgress FindRacer(RacingVehiclePhysics vehicle)
        {
            for (int i = 0; i < racers.Count; i++)
            {
                if (racers[i].Vehicle == vehicle) return racers[i];
            }
            return null;
        }

        public RacerProgress GetPlayerProgress()
        {
            for (int i = 0; i < racers.Count; i++)
            {
                if (racers[i].Identity.isHuman) return racers[i];
            }
            return null;
        }
    }
}
