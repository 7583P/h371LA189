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
                OnRacerEntered?.Invoke(racer, checkpointIndex, isFinishLine);
            }
        }
    }
}
