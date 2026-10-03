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
            TryCrossing(other);
        }
        private void OnTriggerStay(Collider other)
        {
            // A racer can enter the volume while braking or spun out, then cross
            // its plane on a later physics step without another Enter callback.
            TryCrossing(other);
        }
        private void TryCrossing(Collider other)
        {
            RacingVehiclePhysics racer = other.GetComponentInParent<RacingVehiclePhysics>();
            if (racer != null)
            {
                if (manager == null || (manager.State != RaceState.Racing && manager.State != RaceState.PlayerFinished)) return;
                if (Vector3.Dot(racer.Body.linearVelocity, transform.forward) <= 0.5f) return;
                if(Vector3.Dot(racer.Body.position-transform.position,transform.forward)<0)return;
                var progress=tracker?.FindRacer(racer);
                if(progress==null || progress.Finished || progress.LastCheckpointIndex==checkpointIndex)return;
                int before=progress.CheckpointVisits;
                tracker?.NotifyCheckpoint(racer, checkpointIndex, isFinishLine, manager.RaceElapsed);
                if(progress.CheckpointVisits>before)OnRacerEntered?.Invoke(racer, checkpointIndex, isFinishLine);
            }
        }
    }
}
