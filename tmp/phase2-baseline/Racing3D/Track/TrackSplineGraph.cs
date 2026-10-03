using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed class TrackSplineGraph : MonoBehaviour
    {
        [SerializeField] private TrackNode[] nodes = Array.Empty<TrackNode>();
        [SerializeField] private TrackShortcut[] shortcuts = Array.Empty<TrackShortcut>();
        [SerializeField] private float totalLength;

        public int NodeCount => nodes != null ? nodes.Length : 0;
        public float TotalLength => totalLength;
        public TrackShortcut[] Shortcuts => shortcuts;

        public void InitializeFromNodes(TrackNode[] authoredNodes, TrackShortcut[] authoredShortcuts = null)
        {
            nodes = authoredNodes ?? Array.Empty<TrackNode>();
            shortcuts = authoredShortcuts ?? Array.Empty<TrackShortcut>();
            RecomputeDistances();
        }

        public void RecomputeDistances()
        {
            if (nodes == null || nodes.Length < 2)
            {
                totalLength = 0f;
                return;
            }

            float runningDist = 0f;
            nodes[0].CumulativeDistance = 0f;

            for (int i = 0; i < nodes.Length; i++)
            {
                int nextIdx = (i + 1) % nodes.Length;
                Vector3 delta = nodes[nextIdx].Position - nodes[i].Position;
                float segLen = delta.magnitude;

                if (i + 1 < nodes.Length)
                {
                    runningDist += segLen;
                    nodes[i + 1].CumulativeDistance = runningDist;
                }
                else
                {
                    runningDist += segLen;
                }

                // Compute orientation vectors if unassigned
                if (nodes[i].Forward.sqrMagnitude < 0.01f && segLen > 0.01f)
                {
                    nodes[i].Forward = delta.normalized;
                    nodes[i].Up = Vector3.up;
                    nodes[i].Right = Vector3.Cross(nodes[i].Up, nodes[i].Forward).normalized;
                }
            }

            totalLength = runningDist;
        }

        public TrackNode GetNode(int index)
        {
            if (nodes == null || nodes.Length == 0) return default;
            int wrapped = (index % nodes.Length + nodes.Length) % nodes.Length;
            return nodes[wrapped];
        }

        /// <summary>
        /// Projects a world position onto the track spline.
        /// Returns nearest node index, exact continuous track distance (meters from start), and lateral offset.
        /// </summary>
        public void ProjectPosition(
            Vector3 worldPos,
            out int nearestNodeIndex,
            out float distanceAlongTrack,
            out float lateralDistance)
        {
            nearestNodeIndex = 0;
            distanceAlongTrack = 0f;
            lateralDistance = 0f;

            if (nodes == null || nodes.Length < 2) return;

            float minSqrDist = float.MaxValue;
            int bestIdx = 0;

            for (int i = 0; i < nodes.Length; i++)
            {
                float sqrD = (nodes[i].Position - worldPos).sqrMagnitude;
                if (sqrD < minSqrDist)
                {
                    minSqrDist = sqrD;
                    bestIdx = i;
                }
            }

            nearestNodeIndex = bestIdx;

            // Project onto segment (bestIdx -> bestIdx + 1) or (bestIdx - 1 -> bestIdx)
            int nextIdx = (bestIdx + 1) % nodes.Length;
            int prevIdx = (bestIdx - 1 + nodes.Length) % nodes.Length;

            Vector3 segA = nodes[bestIdx].Position;
            Vector3 segB = nodes[nextIdx].Position;
            Vector3 segDir = segB - segA;
            float segLen = segDir.magnitude;

            if (segLen > 0.01f)
            {
                float t = Vector3.Dot(worldPos - segA, segDir) / (segLen * segLen);
                t = Mathf.Clamp01(t);
                distanceAlongTrack = nodes[bestIdx].CumulativeDistance + (t * segLen);

                Vector3 projPoint = segA + segDir * t;
                Vector3 toPos = worldPos - projPoint;
                lateralDistance = Vector3.Dot(toPos, nodes[bestIdx].Right);
            }
            else
            {
                distanceAlongTrack = nodes[bestIdx].CumulativeDistance;
                lateralDistance = 0f;
            }
        }

        /// <summary>
        /// Calculates target navigation point for AI racers.
        /// Blends between SafeLineOffset and OptimalLineOffset using racingLineSkill,
        /// and applies lane deviation / natural error jitter.
        /// </summary>
        public Vector3 EvaluateTargetPoint(
            int currentNodeIndex,
            float lookAheadMeters,
            float racingLineSkill,
            float lateralOffset)
        {
            if (nodes == null || nodes.Length == 0) return transform.position;

            // Step forward along nodes until lookAheadMeters is reached
            float distCovered = 0f;
            int searchIdx = currentNodeIndex;
            while (distCovered < lookAheadMeters)
            {
                int next = (searchIdx + 1) % nodes.Length;
                float segLen = Vector3.Distance(nodes[searchIdx].Position, nodes[next].Position);
                distCovered += segLen;
                searchIdx = next;
            }

            TrackNode targetNode = nodes[searchIdx];

            // Blend safe line vs optimal racing line
            float blendedOffset = Mathf.Lerp(targetNode.SafeLineOffset, targetNode.OptimalLineOffset, Mathf.Clamp01(racingLineSkill));
            // Add lateral offset (overtaking or natural personality lane variation)
            blendedOffset = Mathf.Clamp(blendedOffset + lateralOffset, -1f, 1f);

            float halfWidth = Mathf.Max(3f, targetNode.TrackWidth * 0.5f);
            Vector3 targetPos = targetNode.Position + targetNode.Right * (blendedOffset * halfWidth * 0.85f);
            return targetPos;
        }

        public TrackShortcut FindShortcutNear(int nodeIndex)
        {
            if (shortcuts == null) return null;
            for (int i = 0; i < shortcuts.Length; i++)
            {
                TrackShortcut sc = shortcuts[i];
                if (sc != null && sc.IsValid() && Mathf.Abs(sc.entryNodeIndex - nodeIndex) <= 2)
                    return sc;
            }
            return null;
        }

        public void GetSafeRecoveryAnchor(int nearestNodeIndex, out Vector3 safePos, out Quaternion safeRot)
        {
            TrackNode node = GetNode(nearestNodeIndex);
            safePos = node.Position + Vector3.up * 0.5f;
            safeRot = Quaternion.LookRotation(node.Forward.sqrMagnitude > 0.01f ? node.Forward : Vector3.forward, Vector3.up);
        }
    }
}
