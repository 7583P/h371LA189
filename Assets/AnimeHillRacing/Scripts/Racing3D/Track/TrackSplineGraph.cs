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
        [SerializeField] private int[] checkpointNodeIndices = Array.Empty<int>();
        public int MainJumpNodeIndex=-1;

        public int NodeCount => nodes != null ? nodes.Length : 0;
        public float TotalLength => totalLength;
        public TrackShortcut[] Shortcuts => shortcuts;
        public int[] CheckpointNodeIndices => checkpointNodeIndices;

        public void InitializeFromNodes(TrackNode[] authoredNodes, TrackShortcut[] authoredShortcuts = null, int[] checkpointNodes = null)
        {
            nodes = authoredNodes ?? Array.Empty<TrackNode>();
            shortcuts = authoredShortcuts ?? Array.Empty<TrackShortcut>();
            checkpointNodeIndices = checkpointNodes ?? Array.Empty<int>();
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
            for (int i = 0; i < nodes.Length; i++)
            {
                Vector3 delta = nodes[(i + 1) % nodes.Length].Position - nodes[i].Position;
                float lengthSquared = delta.sqrMagnitude;
                if (lengthSquared < 0.0001f) continue;
                float fraction = Mathf.Clamp01(Vector3.Dot(worldPos - nodes[i].Position, delta) / lengthSquared);
                Vector3 projected = nodes[i].Position + delta * fraction;
                float sqrD = (projected - worldPos).sqrMagnitude;
                if (sqrD < minSqrDist)
                {
                    minSqrDist = sqrD;
                    nearestNodeIndex = i;
                    distanceAlongTrack = nodes[i].CumulativeDistance + fraction * Mathf.Sqrt(lengthSquared);
                    lateralDistance = Vector3.Dot(worldPos - projected, nodes[i].Right);
                }
            }
        }

        public TrackNode SampleDistance(float distance)
        {
            if (NodeCount < 2 || totalLength < 0.01f) return GetNode(0);
            distance = Mathf.Repeat(distance, totalLength);
            for (int i = 0; i < NodeCount; i++)
            {
                TrackNode a = nodes[i], b = nodes[(i + 1) % NodeCount];
                float length = Vector3.Distance(a.Position, b.Position);
                if (distance > a.CumulativeDistance + length && i < NodeCount - 1) continue;
                float t = length > 0.001f ? Mathf.Clamp01((distance - a.CumulativeDistance) / length) : 0f;
                a.Position = Vector3.Lerp(a.Position, b.Position, t);
                a.Forward = Vector3.Slerp(a.Forward, b.Forward, t).normalized;
                a.Right = Vector3.Cross(Vector3.up, a.Forward).normalized;
                a.TrackWidth = Mathf.Lerp(a.TrackWidth, b.TrackWidth, t);
                a.OptimalLineOffset = Mathf.Lerp(a.OptimalLineOffset, b.OptimalLineOffset, t);
                a.TargetSpeedKmh = Mathf.Lerp(a.TargetSpeedKmh, b.TargetSpeedKmh, t);
                a.CumulativeDistance = distance;
                return a;
            }
            return nodes[0];
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
            if (nodes == null || nodes.Length == 0 || totalLength < 0.01f) return transform.position;

            // Step forward along nodes until lookAheadMeters is reached
            float distCovered = 0f;
            int searchIdx = currentNodeIndex;
            int steps = 0;
            while (distCovered < lookAheadMeters && steps++ < nodes.Length)
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
                if (sc != null && sc.IsValid())
                {
                    if (nodeIndex >= sc.entryNodeIndex - 3 && nodeIndex < sc.exitNodeIndex)
                        return sc;
                }
            }
            return null;
        }

        public TrackShortcut FindUpcomingShortcut(float distanceAlongTrack, float horizon, out float distanceToEntry)
        {
            distanceToEntry = float.MaxValue;
            TrackShortcut result = null;
            if (shortcuts == null || totalLength <= 0) return null;
            foreach (var route in shortcuts)
            {
                if (route == null || !route.IsValid() || route.exitNodeIndex >= NodeCount) continue;
                float ahead = Mathf.Repeat(GetNode(route.entryNodeIndex).CumulativeDistance - distanceAlongTrack, totalLength);
                if (ahead > horizon || ahead >= distanceToEntry) continue;
                result = route; distanceToEntry = ahead;
            }
            return result;
        }

        public void GetSafeRecoveryAnchor(int nearestNodeIndex, out Vector3 safePos, out Quaternion safeRot)
        {
            TrackNode node = GetNode(nearestNodeIndex);
            safePos = node.Position + Vector3.up * 0.5f;
            safeRot = Quaternion.LookRotation(node.Forward.sqrMagnitude > 0.01f ? node.Forward : Vector3.forward, Vector3.up);
        }
    }
}
