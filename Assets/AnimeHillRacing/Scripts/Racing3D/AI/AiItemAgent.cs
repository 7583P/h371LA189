using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public struct AiItemContext
    {
        public bool reserveBoost, shortcutEntryReady, targetAhead, racerBehind, projectileThreat, finalLap, overtakingOpportunity;
        public float turnDegrees, straightDistance;
        public int rivalsInPulseRadius, position;
    }

    public sealed class AiItemAgent
    {
        readonly AiRacerProfile profile;
        readonly ItemInventory inventory;
        readonly RacingVehiclePhysics vehicle;
        readonly AiDecisionLogger logger;

        readonly TrackSplineGraph trackGraph;

        public AiItemAgent(AiRacerProfile skill, ItemInventory inv, RacingVehiclePhysics car, TrackSplineGraph graph, AiDecisionLogger log)
        {
            profile = skill;
            inventory = inv;
            vehicle = car;
            trackGraph = graph;
            logger = log;
        }

        bool Decision(bool use, string reason)
        {
            logger?.UpdateItemDecision(use ? "Use" : "Hold", reason);
            return use;
        }

        public bool ShouldThrowBackward(AiItemContext context)
        {
            if (inventory == null || !inventory.HasItem) return false;
            if ((inventory.HeldItem == RaceItemType.ElectroSpike || inventory.HeldItem == RaceItemType.TwinRock)) // Rock
            {
                // Throw backward if someone is trailing closely behind
                if (context.racerBehind) return true;
                // Otherwise throw forward at rival ahead along racing line
                return false;
            }
            return false;
        }

        public bool ShouldUseItem(AiItemContext context)
        {
            if (inventory == null || !inventory.HasItem) return Decision(false, "NoItem");
            if (vehicle.IsLancerActive && RaceItemUtility.IsBoost(inventory.HeldItem)) return Decision(false, "PreserveBoostUntilLancerEnds");
            float held = Time.time - inventory.LastAcquiredTime;
            if (held < Mathf.Lerp(1.5f, .05f, profile.reactionSkill)) return Decision(false, "AcquisitionReaction");

            switch (inventory.HeldItem)
            {
                case RaceItemType.GoldenRock:
                    return Decision(context.position>1&&vehicle.IsGrounded&&!vehicle.IsSpunOut&&inventory.HasMissileTarget&&
                        (context.turnDegrees<35||held>4),"GoldenRockAgainstImmediatelyAheadRank");
                case RaceItemType.Lancer:
                    return Decision(vehicle.IsGrounded && !vehicle.IsSpunOut && !vehicle.IsDrifting &&
                        !context.reserveBoost && !context.shortcutEntryReady && context.turnDegrees < 12 &&
                        context.straightDistance >= 35f && Mathf.Abs(vehicle.SlipAngle) < 8,
                        "LancerCatchUpFromControlledStraight");
                case RaceItemType.Coin10:
                    return Decision(true, "ConsumeCoinRewardWithoutChangingPace");
                case RaceItemType.MiniTurbo:
                    return Decision(vehicle.IsGrounded && !vehicle.IsSpunOut && context.turnDegrees < 15 && vehicle.BoostRemainingSeconds < .25f &&
                        (context.straightDistance > 25 || context.reserveBoost || context.overtakingOpportunity), "ShortTurboOnControlledLine");
                case RaceItemType.TripleTurbo:
                case RaceItemType.TurboPulse:
                case RaceItemType.HyperCharge:
                    if(context.shortcutEntryReady && !vehicle.IsSpunOut && vehicle.AirborneTime<.35f) return Decision(true,"ExecutePreparedBoostThroughBriefDeckBounce");
                    if (!vehicle.IsGrounded || vehicle.IsSpunOut || context.turnDegrees > 25) return Decision(false, "WaitForControlledExit");
                    if (context.shortcutEntryReady) return Decision(true, "ExecuteReservedShortcutBoost");
                    if (context.reserveBoost) return Decision(false, "SaveBoostForKnownRoute");
                    if (vehicle.BoostRemainingSeconds > .5f) return Decision(false, "AvoidStackingBoostTimers");
                    // Corner exit onto straight or overtaking opportunity
                    bool cornerExit = context.turnDegrees < 12 && vehicle.SpeedKmh < vehicle.Stats.SpeedKmh * 0.90f;
                    bool overtaking = context.overtakingOpportunity && context.turnDegrees < 15;
                    bool straightRun = context.straightDistance > 50f && context.turnDegrees < 8;
                    return Decision(cornerExit || overtaking || straightRun || context.finalLap, "TacticalTurboAcceleration");

                case RaceItemType.TrackingDart:
                case RaceItemType.HomingRocket:
                case RaceItemType.KineticMissile: // Dart Item
                    // High-MMR Predictive Intercept targeting:
                    // Only release when target is within tracking corridor and not obstructed by blind corner walls
                    bool targetValid = context.targetAhead && inventory.HasMissileTarget;
                    if (!targetValid) return Decision(false, "NoPredictiveTargetLocked");

                    if (profile.itemKnowledge > .70f)
                    {
                        // 7.5k-10k elite bots wait for high-probability intercept windows:
                        // Avoid firing during sharp turn entry (> 22 deg) where projectile hits side barriers
                        bool sharpCornerBlind = context.turnDegrees > 22f && context.straightDistance < 18f;
                        if (sharpCornerBlind && !context.finalLap)
                            return Decision(false, "HoldDartForClearStraightOrApexExit");
                    }

                    bool goodDartWindow = context.overtakingOpportunity || context.straightDistance > 16f || context.finalLap || context.turnDegrees < 20f;
                    float stylePrecision = profile.style == AiRacingStyle.ItemTactical ? profile.NormalizedSkill : 0;
                    goodDartWindow&=inventory.HasPredictedFiringWindow(Mathf.Lerp(.05f,.7f,profile.itemTiming)+.05f*stylePrecision,
                        Mathf.Lerp(48,18,profile.itemTiming)-3f*stylePrecision);
                    return Decision(goodDartWindow, "PredictiveDartInterceptStrike");

                case RaceItemType.TwinRock:
                case RaceItemType.ElectroSpike: // Rock Item
                    // Ballistic Trajectory Calculation (Launch Speed: 36 m/s, Gravity: 22 m/s²):
                    // Backward defensive drop against close pursuer (< 15m)
                    if (context.racerBehind)
                        return Decision(true, "DefensiveRearRockTrap");

                    // Forward ballistic launch:
                    // Calculated roll/bounce trajectory requires target ahead in corridor on straight or mild sweeper
                    bool ballisticForwardOpportunity = context.targetAhead &&
                        context.turnDegrees < 14f &&
                        context.straightDistance >= 12f && context.straightDistance <= 38f;

                    if (ballisticForwardOpportunity)
                        return Decision(true, "PredictiveBallisticForwardRockTrap");

                    // Fallback on final lap or held too long before entering hairpin
                    if (held > 5.0f && context.turnDegrees > 12f)
                        return Decision(true, "ApexEntryRockDischarge");

                    return Decision(false, "HoldRockForBallisticWindow");

                case RaceItemType.PulseMine:
                    return Decision(vehicle.IsGrounded && (context.racerBehind || context.turnDegrees > 12 && held > 4), "PredictedPursuitMine");
                case RaceItemType.AerialRecovery:
                    return Decision(!vehicle.IsGrounded && (vehicle.AirborneTime > .25f || vehicle.IsSpunOut), "AerialStabilizationAndReturn");
                case RaceItemType.MagneticShield:
                    if (inventory.HasShield) return Decision(false, "PreserveExistingDefense");
                    return Decision(context.projectileThreat || context.rivalsInPulseRadius >= 2 || context.reserveBoost || (context.finalLap && context.racerBehind), "IncomingThreatOrFreeSlotForVisibleRouteSupply");

                case RaceItemType.ShockwavePulse:
                    return Decision(context.rivalsInPulseRadius > 0, "RivalInsideActualPulseRadius");

                default:
                    return Decision(false, "NoTacticalOpportunity");
            }
        }

        public bool ShouldUseItem(int node, float turn, bool shortcut, bool rear, bool front, float speed)
            => ShouldUseItem(new AiItemContext { turnDegrees = turn, reserveBoost = shortcut, racerBehind = rear, targetAhead = front });
    }
}
