using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed class AiItemAgent
    {
        private readonly AiRacerProfile profile;
        private readonly ItemInventory inventory;
        private readonly RacingVehiclePhysics vehicle;
        private readonly TrackSplineGraph trackGraph;
        private readonly AiDecisionLogger logger;

        private float itemAcquiredTime;
        private RaceItemType lastObservedItem = RaceItemType.None;

        public AiItemAgent(AiRacerProfile prof, ItemInventory inv, RacingVehiclePhysics vPhysics, TrackSplineGraph graph, AiDecisionLogger decLogger)
        {
            profile = prof;
            inventory = inv;
            vehicle = vPhysics;
            trackGraph = graph;
            logger = decLogger;
        }

        public bool ShouldUseItem(
            int nearestNodeIndex,
            float currentTurnSharpness,
            bool isApproachingShortcut,
            bool threatBehind,
            bool targetAheadInLineOfSight,
            float speedKmh)
        {
            if (inventory == null || !inventory.HasItem)
            {
                logger?.UpdateItemDecision("None", "NoItem");
                return false;
            }

            RaceItemType item = inventory.HeldItem;
            if (item != lastObservedItem)
            {
                lastObservedItem = item;
                itemAcquiredTime = Time.time;
            }

            float heldDuration = Time.time - itemAcquiredTime;

            // Low MMR bots act impulsively:
            // If itemTiming is low (< 0.35), use item quickly after receiving (0.5 to 2.0s delay)
            if (profile.itemTiming < 0.35f)
            {
                if (heldDuration > (2.5f - profile.itemTiming * 3.0f))
                {
                    logger?.UpdateItemDecision("Use", "ImpulsiveUseLowTiming");
                    return true;
                }
                logger?.UpdateItemDecision("Hold", "WaitingImpulseDelay");
                return false;
            }

            // Tactical decision model for mid & high MMR bots
            switch (item)
            {
                case RaceItemType.TurboPulse:
                case RaceItemType.HyperCharge:
                    // Boost reasoning:
                    // 1. If approaching a known shortcut that requires boost:
                    if (isApproachingShortcut && profile.shortcutKnowledge > 0.4f)
                    {
                        logger?.UpdateItemDecision("Use", "ShortcutExecutionBoost");
                        return true;
                    }
                    // 2. Do NOT use if facing a sharp turn / hairpin (would smash into wall or fly off)
                    if (currentTurnSharpness > 35f && profile.itemKnowledge > 0.4f)
                    {
                        logger?.UpdateItemDecision("Hold", "PreventCrashInSharpTurn");
                        return false;
                    }
                    // 3. Do NOT use in mid-air
                    if (!vehicle.IsGrounded)
                    {
                        logger?.UpdateItemDecision("Hold", "VehicleAirborne");
                        return false;
                    }
                    // 4. Use on straightaways or after corner exit
                    if (currentTurnSharpness < 15f)
                    {
                        logger?.UpdateItemDecision("Use", "StraightawaySpeedBoost");
                        return true;
                    }
                    logger?.UpdateItemDecision("Hold", "WaitingForStraightSection");
                    return false;

                case RaceItemType.KineticMissile:
                    // Missile reasoning:
                    // Fire only when clear line-of-sight to target ahead and not aiming at sharp wall
                    if (currentTurnSharpness > 40f && profile.itemKnowledge > 0.4f)
                    {
                        logger?.UpdateItemDecision("Hold", "CurveBlocksLineOfSight");
                        return false;
                    }
                    if (targetAheadInLineOfSight)
                    {
                        logger?.UpdateItemDecision("Use", "TargetAcquiredInLineOfSight");
                        return true;
                    }
                    // If held for a long time (> 8s) on straight section, fire anyway
                    if (heldDuration > 8.0f && currentTurnSharpness < 20f)
                    {
                        logger?.UpdateItemDecision("Use", "TimeoutFlushAhead");
                        return true;
                    }
                    logger?.UpdateItemDecision("Hold", "NoTargetInSight");
                    return false;

                case RaceItemType.ElectroSpike:
                    // Trap reasoning:
                    // If rival is closely tailgating behind, drop trap right in their face
                    if (threatBehind && profile.defensiveAwareness > 0.4f)
                    {
                        logger?.UpdateItemDecision("Use", "DropBehindRivalThreat");
                        return true;
                    }
                    // Or drop at corner apex to block racing line
                    if (currentTurnSharpness > 30f && heldDuration > 1.5f && profile.offensiveAwareness > 0.5f)
                    {
                        logger?.UpdateItemDecision("Use", "DropAtCornerApex");
                        return true;
                    }
                    // Hold as passive defense if heldDuration < 6s
                    if (heldDuration < 6.0f)
                    {
                        logger?.UpdateItemDecision("Hold", "PreserveRearDefense");
                        return false;
                    }
                    logger?.UpdateItemDecision("Use", "FlushTrap");
                    return true;

                case RaceItemType.MagneticShield:
                    // Shield reasoning:
                    // Use immediately if threat behind or heavy rival cluster
                    if (threatBehind || heldDuration > 2.0f)
                    {
                        logger?.UpdateItemDecision("Use", "ActivateShieldProtection");
                        return true;
                    }
                    logger?.UpdateItemDecision("Hold", "WaitingForThreat");
                    return false;

                case RaceItemType.ShockwavePulse:
                    // Use if in close proximity pack with other racers
                    if (threatBehind || targetAheadInLineOfSight)
                    {
                        logger?.UpdateItemDecision("Use", "DisruptNearbyPack");
                        return true;
                    }
                    if (heldDuration > 4.0f)
                    {
                        logger?.UpdateItemDecision("Use", "TimeoutPulse");
                        return true;
                    }
                    logger?.UpdateItemDecision("Hold", "NoRivalsInRadius");
                    return false;

                default:
                    return false;
            }
        }
    }
}
