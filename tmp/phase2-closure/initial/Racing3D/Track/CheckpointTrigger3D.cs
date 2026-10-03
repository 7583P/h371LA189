using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(Collider))]
    public sealed class CheckpointTrigger3D : MonoBehaviour
    {
        [SerializeField] private int checkpointIndex;
        [SerializeField] private bool isFinishLine;

        public int CheckpointIndex => checkpointIndex;
        public bool IsFinishLine => isFinishLine;

        public event Action<RacingVehiclePhysics, int, bool> OnRacerEntered;
        private TrackProgressTracker tracker;
        private RaceManager manager;

        public void Configure(int index, bool finish)
        {
            checkpointIndex = index;
            isFinishLine = finish;
        }

        public void Bind(TrackProgressTracker progress, RaceManager race)
        {
            tracker = progress;
            manager = race;
        }

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            RacingVehiclePhysics racer = other.GetComponentInParent<RacingVehiclePhysics>();
            if (racer != null)
            {
                if (manager == null || (manager.State != RaceState.Racing && manager.State != RaceState.PlayerFinished)) return;
                if (Vector3.Dot(racer.Body.linearVelocity, transform.forward) <= 0.5f) return;
                tracker?.NotifyCheckpoint(racer, checkpointIndex, isFinishLine, manager.RaceElapsed);
                OnRacerEntered?.Invoke(racer, checkpointIndex, isFinishLine);
            }
        }
    }
}
