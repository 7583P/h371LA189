using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.TimeTrial
{
    public sealed class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private int nodeId;
        [SerializeField] private TimeTrialManager manager;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<VehicleRuntimeBase>() != null)
                manager?.NotifyCheckpoint(nodeId, transform);
        }

        public void Assign(int id, TimeTrialManager targetManager)
        {
            nodeId = id;
            manager = targetManager;
        }
    }
}
