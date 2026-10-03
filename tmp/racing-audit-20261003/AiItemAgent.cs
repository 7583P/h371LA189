using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    public struct AiItemContext
    {
        public bool reserveBoost, shortcutEntryReady, targetAhead, racerBehind, projectileThreat, finalLap;
        public float turnDegrees;
        public int rivalsInPulseRadius, position;
    }
    public sealed class AiItemAgent
    {
        readonly AiRacerProfile profile; readonly ItemInventory inventory; readonly RacingVehiclePhysics vehicle;
        readonly AiDecisionLogger logger;
        public AiItemAgent(AiRacerProfile skill, ItemInventory inv, RacingVehiclePhysics car, TrackSplineGraph graph, AiDecisionLogger log)
        { profile = skill; inventory = inv; vehicle = car; logger = log; }
        bool Decision(bool use, string reason) { logger?.UpdateItemDecision(use ? "Use" : "Hold", reason); return use; }
        public bool ShouldUseItem(AiItemContext context)
        {
            if (inventory == null || !inventory.HasItem) return Decision(false, "NoItem");
            float held = Time.time - inventory.LastAcquiredTime;
            if (held < Mathf.Lerp(1.8f, .12f, profile.reactionSkill)) return Decision(false, "AcquisitionReaction");
            if (profile.itemTiming < .25f) return Decision(held > 2 - profile.itemTiming * 3, "ImpulsiveTiming");
            switch (inventory.HeldItem)
            {
                case RaceItemType.TurboPulse:
                case RaceItemType.HyperCharge:
                    if (!vehicle.IsGrounded || vehicle.IsSpunOut || context.turnDegrees > 28) return Decision(false, "WaitForControlledExit");
                    if (context.shortcutEntryReady) return Decision(true, "ExecuteReservedShortcutBoost");
                    if (context.reserveBoost) return Decision(false, "SaveBoostForKnownRoute");
                    if (vehicle.BoostRemainingSeconds > .65f) return Decision(false, "AvoidStackingBoostTimers");
                    return Decision(context.turnDegrees < 12 && (!vehicle.IsDrifting || context.finalLap), "ExitOrStraightBoost");
                case RaceItemType.KineticMissile:
                    return Decision(context.targetAhead && context.turnDegrees < 32 && inventory.HasMissileTarget, "RequireVisibleUnblockedTarget");
                case RaceItemType.ElectroSpike:
                    return Decision(context.racerBehind || (held > 6 && context.turnDegrees > 12 && vehicle.IsGrounded), "RearThreatOrApexTrap");
                case RaceItemType.MagneticShield:
                    if (inventory.HasShield) return Decision(false, "PreserveExistingDefense");
                    return Decision(context.projectileThreat || context.rivalsInPulseRadius >= 2 || (context.finalLap && context.racerBehind), "IncomingThreatOrDensePack");
                case RaceItemType.ShockwavePulse:
                    return Decision(context.rivalsInPulseRadius > 0, "RivalInsideActualPulseRadius");
                default: return Decision(false, "NoTacticalOpportunity");
            }
        }
        public bool ShouldUseItem(int node, float turn, bool shortcut, bool rear, bool front, float speed)
            => ShouldUseItem(new AiItemContext { turnDegrees = turn, reserveBoost = shortcut, racerBehind = rear, targetAhead = front });
    }
}
