using System;

namespace AnimeHillRacing.Core
{
    /// <summary>
    /// Run-local distance reward sequence. It deliberately depends only on the maximum forward
    /// distance, so passing a marker above or below it awards the bonus without any collision.
    /// </summary>
    public sealed class DistanceBonusTracker
    {
        public const float IntervalMeters = 500f;
        public const int StartingCoins = 100;
        public const int MaximumCoins = 3000;
        public const int IncrementCoins = 100;

        private int claimedMilestones;

        public int ClaimedMilestones => claimedMilestones;
        public float NextDistance => (claimedMilestones + 1) * IntervalMeters;
        public int NextAward => GetAwardForMilestone(claimedMilestones + 1);

        public void Reset() => claimedMilestones = 0;

        public bool TryClaimThrough(float maximumRunDistance, out int award, out float milestoneDistance)
        {
            award = 0;
            milestoneDistance = NextDistance;
            if (maximumRunDistance + .001f < milestoneDistance)
                return false;

            claimedMilestones++;
            award = GetAwardForMilestone(claimedMilestones);
            return true;
        }

        public static int GetAwardForMilestone(int oneBasedMilestone)
        {
            int safeMilestone = Math.Max(1, oneBasedMilestone);
            return Math.Min(MaximumCoins, StartingCoins + (safeMilestone - 1) * IncrementCoins);
        }

        public static int GetAwardAtDistance(float distance)
        {
            int milestone = Math.Max(1,
                (int)Math.Round(distance / IntervalMeters, MidpointRounding.AwayFromZero));
            return GetAwardForMilestone(milestone);
        }
    }
}
