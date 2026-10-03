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
        public int RacePosition = 12; // 1 to 12
        public bool Finished;
        public int FinishPosition;
        public float FinishTime;
        public float CurrentLapStartTime;
        public float BestLapTime = float.MaxValue;
        public int CheckpointVisits;
        public float LastCheckpointTime;
        public float LastLapDuration;
        public bool HasCrossedStart;

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
        public int RacersCount => racers.Count;
        public IReadOnlyList<RacerProgress> Leaderboard => sortedRacers;
        public TrackSplineGraph TrackGraph => trackGraph;

        public event Action<RacerProgress, int> OnRacerLapCompleted; // racer, lapNumber
        public event Action<RacerProgress, int> OnRacerFinished;     // racer, finishPosition

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
                RacePosition = racers.Count + 1,
                Finished = false,
                FinishPosition = 0,
                FinishTime = 0f,
                CurrentLapStartTime = 0f,
                BestLapTime = float.MaxValue
            };

            racers.Add(p);
            sortedRacers.Add(p);
            return p;
        }

        public void NotifyCheckpoint(RacingVehiclePhysics vehicle, int checkpointIndex, bool isFinishLine, float raceTime)
        {
            RacerProgress p = FindRacer(vehicle);
            if (p == null || p.Finished) return;

            if (checkpointIndex < 0 || checkpointIndex >= totalCheckpointsCount || !float.IsFinite(raceTime)) return;
            int expectedNext = (p.LastCheckpointIndex + 1) % totalCheckpointsCount;
            if (checkpointIndex != expectedNext) return;
            p.LastCheckpointIndex = checkpointIndex;
            p.CheckpointVisits++;
            p.LastCheckpointTime = raceTime;
            if (!p.HasCrossedStart)
            {
                p.HasCrossedStart = true;
                p.CurrentLapStartTime = raceTime;
                return;
            }
            if (!isFinishLine) p.CheckpointsPassedCount++;

            // Finish line crossed
            if (isFinishLine)
            {
                if (p.CheckpointsPassedCount == totalCheckpointsCount - 1)
                {
                    float lapDuration = raceTime - p.CurrentLapStartTime;
                    p.LastLapDuration = lapDuration;
                    if (lapDuration > 5f)
                    {
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
                        OnRacerLapCompleted?.Invoke(p, p.CurrentLap);
                    }
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
                    trackGraph.ProjectPosition(
                        r.Vehicle.transform.position,
                        out int nearestNode,
                        out float distAlongTrack,
                        out _
                    );

                    r.DistanceAlongTrack = distAlongTrack;
                    r.TotalDistance = (r.CurrentLap - 1) * trackLen + distAlongTrack;
                    if (!r.HasCrossedStart && distAlongTrack > trackLen * 0.75f) r.TotalDistance -= trackLen;

                    // Update safe recovery anchor regularly
                    if (r.Recovery != null && !r.Recovery.IsRecovering && r.Vehicle.IsGrounded && !r.Vehicle.IsOffroad)
                    {
                        trackGraph.GetSafeRecoveryAnchor(nearestNode, out Vector3 safePos, out Quaternion safeRot);
                        r.Recovery.SetLastSafeTransform(safePos, safeRot);
                    }
                }
            }

            // 2. Sort leaderboard
            // Comparison: Finished racers first (ordered by FinishPosition ascending),
            // then active racers ordered by TotalDistance descending.
            sortedRacers.Sort((a, b) =>
            {
                if (a.Finished && b.Finished)
                    return a.FinishPosition.CompareTo(b.FinishPosition);
                if (a.Finished) return -1;
                if (b.Finished) return 1;

                int distanceOrder = b.TotalDistance.CompareTo(a.TotalDistance);
                return distanceOrder != 0 ? distanceOrder : a.Identity.gridPosition.CompareTo(b.Identity.gridPosition);
            });

            // 3. Assign real-time race positions (1 to 12)
            for (int i = 0; i < sortedRacers.Count; i++)
            {
                sortedRacers[i].RacePosition = i + 1;
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
