using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MovementPlaneConstraint : MonoBehaviour
    {
        [SerializeField] private float planeZ;
        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints |= RigidbodyConstraints.FreezePositionZ |
                                RigidbodyConstraints.FreezeRotationX |
                                RigidbodyConstraints.FreezeRotationY;
            planeZ = transform.position.z;
        }

        private void FixedUpdate()
        {
            Vector3 position = body.position;
            if (!Mathf.Approximately(position.z, planeZ))
            {
                position.z = planeZ;
                body.position = position;
            }
        }
    }
}
