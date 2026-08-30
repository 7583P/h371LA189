using System;

namespace AnimeHillRacing.Core
{
    /// <summary>
    /// Distance reward sequence. A run starts after the saved personal best, so only new distance
    /// milestones can be rewarded. Passing a marker awards normally; its visible marker may also
    /// request the same idempotent claim when touched by the vehicle.
    /// </summary>
    public sealed class DistanceBonusTracker
    {
        public const float IntervalMeters = 500f;
        public const int StartingCoins = 100;
        public const int MaximumCoins = 5000;
        public const int IncrementCoins = 100;
        public const int FirstTierMaximumCoins = 500;
        public const int SecondTierIncrementCoins = 500;

        private int claimedMilestones;

        public int ClaimedMilestones => claimedMilestones;
        public float NextDistance => (claimedMilestones + 1) * IntervalMeters;
        public int NextAward => GetAwardForMilestone(claimedMilestones + 1);

        public void Reset(float personalBestDistance = 0f)
        {
            double safeBestDistance = Math.Max(0d, personalBestDistance);
            claimedMilestones = Math.Max(0,
                (int)Math.Floor((safeBestDistance + .001d) / IntervalMeters));
        }

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
            int firstTierMilestones = FirstTierMaximumCoins / IncrementCoins;
            if (safeMilestone <= firstTierMilestones)
                return safeMilestone * IncrementCoins;
            int secondTierAward = FirstTierMaximumCoins +
                                  (safeMilestone - firstTierMilestones) * SecondTierIncrementCoins;
            return Math.Min(MaximumCoins, secondTierAward);
        }

        public static int GetAwardAtDistance(float distance)
        {
            int milestone = Math.Max(1,
                (int)Math.Round(distance / IntervalMeters, MidpointRounding.AwayFromZero));
            return GetAwardForMilestone(milestone);
        }
    }
}
