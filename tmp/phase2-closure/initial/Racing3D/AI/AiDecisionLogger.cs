using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed class AiDecisionLogger
    {
        public string BotName { get; set; } = "AI";
        public string CurrentIntent { get; private set; } = "FollowOptimalLine";
        public string Reason { get; private set; } = "ClearTrack";
        public string HeldItemDecision { get; private set; } = "None";
        public string ItemDecisionReason { get; private set; } = "NoItem";
        public Vector3 TargetPosition { get; private set; }
        public float TargetSteering { get; private set; }
        public float TargetThrottle { get; private set; }
        public bool IsDrifting { get; private set; }

        private float nextLogTime;
        private const float LogIntervalSeconds = 3.0f;

        public void UpdateSteeringDecision(string intent, string reason, Vector3 targetPos, float steer, float throttle, bool drift)
        {
            CurrentIntent = intent;
            Reason = reason;
            TargetPosition = targetPos;
            TargetSteering = steer;
            TargetThrottle = throttle;
            IsDrifting = drift;
        }

        public void UpdateItemDecision(string decision, string itemReason)
        {
            HeldItemDecision = decision;
            ItemDecisionReason = itemReason;
        }

        public void LogThrottled(bool enabled)
        {
            if (!enabled || Time.time < nextLogTime) return;
            nextLogTime = Time.time + LogIntervalSeconds;
            Debug.Log($"[AI:{BotName}] Intent: {CurrentIntent} | Reason: {Reason} | Item: {HeldItemDecision} ({ItemDecisionReason})");
        }

        public string GetDebugSummary() =>
            $"Intent: {CurrentIntent}\nReason: {Reason}\nItem: {HeldItemDecision} ({ItemDecisionReason})\nSteer: {TargetSteering:F2} | Throttle: {TargetThrottle:F2} | Drift: {IsDrifting}";
    }
}
