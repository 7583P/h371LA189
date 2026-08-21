using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Experiments.Box2DJeep
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EdgeCollider2D))]
    public sealed class AbsoluteEdgeTerrain2D : MonoBehaviour
    {
        public const float StepX = 0.8f;
        public const int PointsBehind = 30;
        public const int PointsAhead = 180;
        public const int PointCount = PointsBehind + PointsAhead + 1;
        public const float Seed = 17f;

        [SerializeField] private Transform target;
        [SerializeField] private EdgeCollider2D terrainCollider;
        [SerializeField] private LineRenderer terrainLine;
        [SerializeField] private bool useRelativeProfile;
        [SerializeField] private float profileOriginX;

        private readonly List<Vector2> points = new List<Vector2>(PointCount);

        public EdgeCollider2D TerrainCollider => terrainCollider;

        private void Awake()
        {
            EnsureReferences();
            RebuildNow();
        }

        private void Update()
        {
            // Deliberately unconditional: this reproduces the historical workload.
            RebuildNow();
        }

        public void Configure(Transform followTarget, LineRenderer line = null)
        {
            target = followTarget;
            terrainLine = line;
            useRelativeProfile = false;
            profileOriginX = 0f;
            EnsureReferences();
            RebuildNow();
        }

        public void ConfigureRelativeProfile(Transform followTarget, float originX, LineRenderer line = null)
        {
            target = followTarget;
            if (line != null)
            {
                terrainLine = line;
            }
            useRelativeProfile = true;
            profileOriginX = originX;
            EnsureReferences();
            RebuildNow();
        }

        public void RebuildNow()
        {
            EnsureReferences();
            if (terrainCollider == null)
            {
                return;
            }

            float centerX = target != null ? target.position.x : 0f;
            if (useRelativeProfile)
            {
                centerX -= profileOriginX;
            }
            int centerIndex = Mathf.FloorToInt(centerX / StepX);
            int firstIndex = centerIndex - PointsBehind;

            points.Clear();
            for (int i = 0; i < PointCount; i++)
            {
                // The collider lives at world origin, so these are absolute coordinates.
                float profileX = (firstIndex + i) * StepX;
                float absoluteX = useRelativeProfile ? profileOriginX + profileX : profileX;
                points.Add(new Vector2(absoluteX, SampleHeight(profileX)));
            }

            terrainCollider.SetPoints(points);

            if (terrainLine != null)
            {
                terrainLine.positionCount = points.Count;
                for (int i = 0; i < points.Count; i++)
                {
                    Vector2 point = points[i];
                    terrainLine.SetPosition(i, new Vector3(point.x, point.y, 0f));
                }
            }
        }

        public static float SampleHeight(float x)
        {
            float broad = 1.4f * Mathf.Sin(0.05f * x);
            float detail = 0.35f * Mathf.Sin(0.12f * x + 1.7f);
            float noise = 0.12f * (Mathf.PerlinNoise(Seed + 0.05f * x, 0.25f * Seed) - 0.5f);
            return -1.5f + broad + detail + noise;
        }

        private void EnsureReferences()
        {
            if (terrainCollider == null)
            {
                terrainCollider = GetComponent<EdgeCollider2D>();
            }
        }
    }
}
