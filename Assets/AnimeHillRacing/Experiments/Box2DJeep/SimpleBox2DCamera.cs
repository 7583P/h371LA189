using UnityEngine;

namespace AnimeHillRacing.Experiments.Box2DJeep
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class SimpleBox2DCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset = new Vector2(2f, 0.5f);
        [SerializeField] private float cameraDepth = -10f;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
            FollowNow();
        }

        private void LateUpdate()
        {
            FollowNow();
        }

        private void FollowNow()
        {
            if (target == null)
            {
                return;
            }

            Vector3 targetPosition = target.position;
            transform.position = new Vector3(targetPosition.x + offset.x, targetPosition.y + offset.y, cameraDepth);
        }
    }
}
