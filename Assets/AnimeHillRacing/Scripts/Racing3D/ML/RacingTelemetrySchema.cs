using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D.ML
{
    [Serializable]
    public sealed class RaceMetadata
    {
        public string schemaVersion = "3.0.0";
        public int ratingScaleMaximum = 10000;
        public string ratingNormalizationVersion = "mmr-0-10k-1";
        public string source = "UNVERIFIED";
        public string featureVersion = "racing-context-2";
        public string actionLabelVersion = "command-intent-1";
        public string itemCatalogVersion = "night-city-items-16-v1";
        public string itemFeatureEncoding = "legacy-mechanical-family-1";
        public string trackRevision = "night-city-192-v3";
        public bool isLegallyValidated;
        public string legalityNote;
        public float referenceLapTime;
        public string sessionId;
        public string gameVersion;
        public string trackId;
        public int trackSeed;
        public int playerMmr;
        public float normalizedSkill; // clamp(mmr / 10000); prestige is bounded at one.
        public string vehicleName;
        public string wheelSetup;
        public int racerCount;
        public int finalPosition;
        public float totalRaceTime;
        public float bestLapTime;
        public float[] lapTimes;
        public float[] sectorTimes;
        public float trainingQualityScore;
        public bool isHumanPlayer;
    }

    [Serializable]
    public struct TelemetrySample
    {
        public float timestamp;
        public float trackProgress; // normalized progress along the track [0, 1]
        public float trackDistance; // meters along track
        public Vector3 worldPosition;
        public Vector3 velocity;
        public float speedMps;
        public float speedKmh;

        // Controls
        public float steering;
        public float throttle;
        public float brake;
        public bool isDrifting;
        public bool isJumping;
        public bool jumpInput, driftInput, aerialBoostInput;
        public float pitchInput;
        public Vector3 localVelocity, angularVelocity;
        public float headingError, curvature15, curvature35, curvature70, slope, bankAngle;
        public float driftCharge, boostRemaining, nextJumpDistance, checkpointProgress;
        public int racePosition, lap;
        public float gapAhead, gapBehind, relativeSpeedAhead;

        // Vehicle Attitude & Airborne State
        public bool isAirborne;
        public float pitch;
        public float roll;
        public float yaw;
        public float airRollInput;
        public bool isAerialBoosting;
        public bool hasLandingBoost;

        // Context
        public int activeItemType;
        public int nearbyRacerCount;
        public float distanceToLeadRacer;
        public float lateralOffset; // offset from track centerline
        public int activeShortcutId; // 0 if main track
    }

    public enum TelemetryEventType
    {
        None = 0,
        JumpStart = 1,
        Landing = 2,
        LandingBoost = 3,
        DriftStart = 4,
        DriftRelease = 5,
        ShortcutEnter = 6,
        ShortcutExit = 7,
        AerialRoute = 8,
        Recovery = 9,
        ItemUsed = 10,
        ItemHit = 11,
        Collision = 12,
        LapCompleted = 13,
        Finish = 14
    }

    [Serializable]
    public struct TelemetryEvent
    {
        public float timestamp;
        public TelemetryEventType eventType;
        public int param1;
        public float param2;
        public Vector3 position;
        public string tag;
    }

    [Serializable]
    public sealed class RaceTelemetrySession
    {
        public RaceMetadata metadata = new RaceMetadata();
        public List<TelemetrySample> samples = new List<TelemetrySample>();
        public List<TelemetryEvent> events = new List<TelemetryEvent>();

        public void ComputeQualityScore()
        {
            // Quality weighting = SkillWeight * RaceQuality * TimeSavingValue * Consistency * Legality
            if (!metadata.isLegallyValidated) { metadata.trainingQualityScore = 0; return; }
            
            // Measure sector consistency across laps
            float consistency = 1f;
            if (metadata.lapTimes != null && metadata.lapTimes.Length > 1 && metadata.bestLapTime > 0.1f)
            {
                float varianceSum = 0f;
                for (int i = 0; i < metadata.lapTimes.Length; i++)
                {
                    varianceSum += Mathf.Abs(metadata.lapTimes[i] - metadata.bestLapTime);
                }
                float avgDelta = varianceSum / metadata.lapTimes.Length;
                consistency = Mathf.Clamp01(1f - (avgDelta / 15f));
            }

            int collisions=0,recoveries=0;
            foreach (var e in events) { if(e.eventType==TelemetryEventType.Collision)collisions++;if(e.eventType==TelemetryEventType.Recovery)recoveries++; }
            float pace = metadata.referenceLapTime>0 && metadata.bestLapTime>0 ? Mathf.Clamp01(metadata.referenceLapTime/metadata.bestLapTime) : .7f;
            metadata.trainingQualityScore = metadata.bestLapTime>0 ? (.25f+.75f*pace)*(.5f+.5f*consistency)/(1+.025f*collisions+.25f*recoveries) : .05f;
        }
    }
}
