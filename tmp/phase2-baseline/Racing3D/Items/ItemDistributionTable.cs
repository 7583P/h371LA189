using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public static class ItemDistributionTable
    {
        [Serializable]
        public struct ItemWeight
        {
            public RaceItemType Type;
            public int Weight;
        }

        // Default authored tables by placement bracket
        private static readonly ItemWeight[] BracketP1 = new[]
        {
            new ItemWeight { Type = RaceItemType.ElectroSpike, Weight = 50 },
            new ItemWeight { Type = RaceItemType.MagneticShield, Weight = 35 },
            new ItemWeight { Type = RaceItemType.TurboPulse, Weight = 15 }
        };

        private static readonly ItemWeight[] BracketP2_P4 = new[]
        {
            new ItemWeight { Type = RaceItemType.KineticMissile, Weight = 35 },
            new ItemWeight { Type = RaceItemType.TurboPulse, Weight = 30 },
            new ItemWeight { Type = RaceItemType.ElectroSpike, Weight = 20 },
            new ItemWeight { Type = RaceItemType.MagneticShield, Weight = 15 }
        };

        private static readonly ItemWeight[] BracketP5_P8 = new[]
        {
            new ItemWeight { Type = RaceItemType.KineticMissile, Weight = 35 },
            new ItemWeight { Type = RaceItemType.TurboPulse, Weight = 35 },
            new ItemWeight { Type = RaceItemType.ShockwavePulse, Weight = 20 },
            new ItemWeight { Type = RaceItemType.MagneticShield, Weight = 10 }
        };

        private static readonly ItemWeight[] BracketP9_P11 = new[]
        {
            new ItemWeight { Type = RaceItemType.TurboPulse, Weight = 35 },
            new ItemWeight { Type = RaceItemType.ShockwavePulse, Weight = 25 },
            new ItemWeight { Type = RaceItemType.HyperCharge, Weight = 25 },
            new ItemWeight { Type = RaceItemType.KineticMissile, Weight = 15 }
        };

        private static readonly ItemWeight[] BracketP12 = new[]
        {
            new ItemWeight { Type = RaceItemType.HyperCharge, Weight = 55 },
            new ItemWeight { Type = RaceItemType.TurboPulse, Weight = 35 },
            new ItemWeight { Type = RaceItemType.ShockwavePulse, Weight = 10 }
        };

        public static RaceItemType RollItem(int racePosition, DeterministicRng rng = null)
        {
            ItemWeight[] bracket;
            if (racePosition <= 1) bracket = BracketP1;
            else if (racePosition <= 4) bracket = BracketP2_P4;
            else if (racePosition <= 8) bracket = BracketP5_P8;
            else if (racePosition <= 11) bracket = BracketP9_P11;
            else bracket = BracketP12;

            int totalWeight = 0;
            for (int i = 0; i < bracket.Length; i++) totalWeight += bracket[i].Weight;

            int roll = rng != null ? rng.Next(totalWeight) : UnityEngine.Random.Range(0, totalWeight);

            int accumulator = 0;
            for (int i = 0; i < bracket.Length; i++)
            {
                accumulator += bracket[i].Weight;
                if (roll < accumulator)
                {
                    return bracket[i].Type;
                }
            }

            return bracket[0].Type;
        }
    }
}
