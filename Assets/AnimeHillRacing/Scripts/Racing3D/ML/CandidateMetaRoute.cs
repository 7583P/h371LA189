using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D.ML
{
    [Serializable]
    public sealed class CandidateMetaRoute
    {
        public string routeId;
        public string trackId;
        public int entryNodeIndex;
        public int exitNodeIndex;
        public Vector3 entryPosition;
        public Vector3 exitPosition;

        // Skill conditioning observation
        public int minObservedMmr;
        public int maxObservedMmr;
        public int attempts;
        public int successes;

        // Performance metrics
        public float medianTimeSaved;
        public float bestTimeSaved;
        public float averageSpeedMps;
        public float entryPitch;
        public float entryRoll;
        public float aerialBoostDuration;
        public float landingQuality; // 1.0 = perfect alignment with track normal
        
        // Safety & Verification
        public bool isLegallyValidated;
        public string legalityNote;
        public float confidence;

        public float SuccessRate => attempts > 0 ? (float)successes / attempts : 0f;
    }

    [Serializable]
    public sealed class MovementTechChain
    {
        public string techId; // e.g. "TechChain_044"
        public int startNode;
        public int endNode;
        public List<string> actionSequence = new List<string>();
        public List<float> actionDurations = new List<float>();

        public float averageTimeSaved;
        public float successRate;
        public float averageSpeedGainMps;
        public float qualityScore;
        public int occurrences;
        public int flights = 1;
        public int successes;
        public float entrySpeedMps, exitSpeedMps, meanDuration, landingPitch, landingRoll, risk;
        public float bestTimeSaved;
        public bool hasMatchedBaseline;
        public string measurementNote;
    }
}
