using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D.ML
{
    public static class DeterministicLegalityFilter
    {
        public const float MaxPhysicalSpeedMps = 75f; // Ordinary trajectory bound remains 270 km/h.
        public const float MaxLancerPhysicalSpeedMps = 90f; // 300 km/h motor plus bounded vertical/slope margin.
        // Expiry restores the ordinary motor without deleting physical momentum.
        public const float LancerCoastValidationSeconds = 2f;
        public const float MaxPhysicalAccelerationMps2 = 120f; // Max impulse threshold
        public const float MinFloorY = -25f; // Track kill/pit floor threshold

        public struct ValidationResult
        {
            public bool IsLegal;
            public string RejectionReason;
            public float ConfidenceScore;
        }

        /// <summary>
        /// Validates a sequence of telemetry samples against physical, geometric and game-rule invariants.
        /// </summary>
        public static ValidationResult ValidateTrajectory(IList<TelemetrySample> samples, IList<TelemetryEvent> events)
        {
            if (samples == null || samples.Count < 5)
            {
                return new ValidationResult { IsLegal = false, RejectionReason = "Insufficient sample count." };
            }

            int recoveryCount = 0;
            if (events != null)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    if (events[i].eventType == TelemetryEventType.Recovery)
                    {
                        recoveryCount++;
                    }
                }
            }

            for(int i=0;i<samples.Count;i++)
            {
                var s=samples[i];
                if(!float.IsFinite(s.timestamp)||!float.IsFinite(s.speedMps)||!float.IsFinite(s.trackProgress)||
                   !float.IsFinite(s.steering)||!float.IsFinite(s.throttle)||!float.IsFinite(s.brake)||
                   !Finite(s.worldPosition)||!Finite(s.velocity)||!Finite(s.angularVelocity)||
                   !float.IsFinite(s.pitch)||!float.IsFinite(s.roll)||!float.IsFinite(s.yaw)||
                   s.speedMps<0||s.speedMps>SpeedLimitAt(s.timestamp,events)||s.trackProgress<0||s.trackProgress>1.001f||
                   Mathf.Abs(s.steering)>1||s.throttle<0||s.throttle>1||s.brake<0||s.brake>1)
                    return new ValidationResult{IsLegal=false,RejectionReason="Invalid finite/control/progress/speed invariant at sample "+i};
            }
            if(events!=null)for(int i=0;i<events.Count;i++)
                if(!float.IsFinite(events[i].timestamp)||!float.IsFinite(events[i].param2)||!Finite(events[i].position)||
                   (i>0&&events[i].timestamp<events[i-1].timestamp))
                    return new ValidationResult{IsLegal=false,RejectionReason="Invalid event ordering or numeric value"};

            // Verify continuous physics and absence of teleportation or unphysical velocity spikes
            for (int i = 1; i < samples.Count; i++)
            {
                var prev = samples[i - 1];
                var curr = samples[i];
                float dt = curr.timestamp - prev.timestamp;

                if (dt <= 0.0001f)return new ValidationResult{IsLegal=false,RejectionReason="Non-increasing sample time"};
                bool recovered=false;
                if(events!=null)foreach(var e in events)recovered|=e.eventType==TelemetryEventType.Recovery&&e.timestamp>=prev.timestamp-.05f&&e.timestamp<=curr.timestamp+.05f;
                // Shared recovery is an observable game mechanic, not an illicit teleport.
                if(recovered)continue;

                float displacement = Vector3.Distance(curr.worldPosition, prev.worldPosition);
                float instantaneousSpeed = displacement / dt;

                float limit=Mathf.Max(SpeedLimitAt(prev.timestamp,events),SpeedLimitAt(curr.timestamp,events));
                if (instantaneousSpeed > limit)
                {
                    return new ValidationResult
                    {
                        IsLegal = false,
                        RejectionReason = $"Unphysical velocity spike detected ({instantaneousSpeed * 3.6f:F1} km/h > max allowable {limit * 3.6f:F1} km/h)."
                    };
                }

                float accel = Mathf.Abs(curr.speedMps - prev.speedMps) / dt;
                bool physicalImpulse=false;
                if(events!=null)foreach(var e in events)physicalImpulse|=(e.eventType==TelemetryEventType.Collision||e.eventType==TelemetryEventType.ItemHit||e.eventType==TelemetryEventType.Landing)&&e.timestamp>=prev.timestamp-.05f&&e.timestamp<=curr.timestamp+.05f;
                if (accel > MaxPhysicalAccelerationMps2&&!physicalImpulse)
                {
                    return new ValidationResult
                    {
                        IsLegal = false,
                        RejectionReason = $"Unphysical acceleration spike detected ({accel:F1} m/s²)."
                    };
                }

                // Check under-map violation
                if (curr.worldPosition.y < MinFloorY)
                {
                    return new ValidationResult
                    {
                        IsLegal = false,
                        RejectionReason = $"Under-map geometry breach detected at Y={curr.worldPosition.y:F1}."
                    };
                }
            }

            return new ValidationResult
            {
                IsLegal = true,
                RejectionReason = string.Empty,
                ConfidenceScore = 1.0f
            };
        }
        public static float SpeedLimitAt(float timestamp,IList<TelemetryEvent> events)
        {
            if(events!=null)for(int i=events.Count-1;i>=0;i--)
            {
                var e=events[i];
                if(e.timestamp<timestamp-RaceItemUtility.LancerDurationSeconds-LancerCoastValidationSeconds-.05f)break;
                if(e.timestamp>=0&&e.eventType==TelemetryEventType.ItemUsed&&e.param1==(int)RaceItemType.Lancer&&
                    e.timestamp<=timestamp+.05f&&timestamp-e.timestamp<=RaceItemUtility.LancerDurationSeconds+LancerCoastValidationSeconds+.05f)
                    return MaxLancerPhysicalSpeedMps;
            }
            return MaxPhysicalSpeedMps;
        }
        static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
    }
}
