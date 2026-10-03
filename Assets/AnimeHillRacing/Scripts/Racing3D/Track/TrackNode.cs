using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [Serializable]
    public struct TrackNode
    {
        public Vector3 Position;
        public Vector3 Forward;
        public Vector3 Right;
        public Vector3 Up;
        public float TrackWidth;
        public float OptimalLineOffset; // -1 (left curb) to +1 (right curb)
        public float SafeLineOffset;    // Center-biased safer path
        public float TargetSpeedKmh;    // Safe max cornering speed
        public bool IsDriftZone;        // Suggested drift apex
        public float CumulativeDistance;// Distance in meters from start line along track
    }

    [Serializable]
    public sealed class TrackShortcut
    {
        public string shortcutId = "shortcut_01";
        public string displayName = "DIRT CUT";
        public int entryNodeIndex;
        public int exitNodeIndex;
        public Vector3[] pathPoints = Array.Empty<Vector3>();
        public bool requiresBoost = true;
        public bool requiresJump;
        [Range(0f, 1f)] public float minSkillRequired = 0.4f;
        [Range(0f, 1f)] public float riskLevel = 0.5f;

        public bool IsValid() => entryNodeIndex >= 0 && exitNodeIndex > entryNodeIndex && pathPoints != null && pathPoints.Length > 0;
    }
}
