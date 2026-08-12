using UnityEngine;

namespace AnimeHillRacing.Core
{
    /// <summary>
    /// Passive, per-run rotation accumulator. In the 2.5D vehicle frame, negative Z rotation
    /// lowers the nose (Flip) and positive Z rotation raises it (Backflip).
    /// </summary>
    public sealed class StuntRotationTracker
    {
        public const int BonusPerRotation = 5;
        private const float CompleteRotationDegrees = 360f;
        private const float MaximumCredibleDeltaPerSample = 90f;

        private float previousAngle;
        private float accumulatedRotation;
        private bool angleInitialized;

        public int Flips { get; private set; }
        public int Backflips { get; private set; }
        public int StuntBonus => (Flips + Backflips) * BonusPerRotation;
        public float AccumulatedRotation => accumulatedRotation;

        public void Reset(float initialAngleDegrees = 0f)
        {
            Flips = 0;
            Backflips = 0;
            accumulatedRotation = 0f;
            previousAngle = initialAngleDegrees;
            angleInitialized = true;
        }

        public void SampleAngle(float currentAngleDegrees, bool airborne, bool runActive)
        {
            if (!angleInitialized)
            {
                previousAngle = currentAngleDegrees;
                angleInitialized = true;
                return;
            }
            float delta = Mathf.DeltaAngle(previousAngle, currentAngleDegrees);
            previousAngle = currentAngleDegrees;
            SampleDelta(delta, airborne, runActive);
        }

        public void SampleDelta(float signedDeltaDegrees, bool airborne, bool runActive)
        {
            if (!runActive || !airborne)
            {
                // Partial rotations cannot be stitched across a landing or game-over state.
                accumulatedRotation = 0f;
                return;
            }
            if (!float.IsFinite(signedDeltaDegrees) ||
                Mathf.Abs(signedDeltaDegrees) > MaximumCredibleDeltaPerSample)
            {
                accumulatedRotation = 0f;
                return;
            }
            if (Mathf.Abs(signedDeltaDegrees) < .001f) return;
            accumulatedRotation += signedDeltaDegrees;
            while (accumulatedRotation <= -CompleteRotationDegrees)
            {
                Flips++;
                accumulatedRotation += CompleteRotationDegrees;
            }
            while (accumulatedRotation >= CompleteRotationDegrees)
            {
                Backflips++;
                accumulatedRotation -= CompleteRotationDegrees;
            }
        }
    }
}
