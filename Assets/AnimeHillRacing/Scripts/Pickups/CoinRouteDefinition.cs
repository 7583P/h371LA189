using UnityEngine;

namespace AnimeHillRacing.Pickups
{
    public enum CoinPatternKind
    {
        Line,
        SpecialCenter,
        Arc,
        Ascent,
        Descent,
        JumpArc
    }

    public struct CoinRouteGroup
    {
        public int cellIndex;
        public float anchorDistance;
        public CoinPatternKind pattern;
        public int coinCount;
        public int lowerRowCoinCount;
        public int upperRowCoinCount;
        public int densityTier;
    }

    public struct CoinRoutePoint
    {
        public float distanceOffset;
        public float heightOffset;
        public float lateralOffset;
        public int value;
    }

    [CreateAssetMenu(menuName = "Anime Hill Racing/Coin Route Definition", fileName = "CoinRouteDefinition")]
    public sealed class CoinRouteDefinition : ScriptableObject
    {
        [Header("Permanent route")]
        public int routeSeed = 9137;
        [Min(24f)] public float cellLength = 48f;
        [Min(0f)] public float firstCoinDistance = 20f;
        [Range(0f, 12f)] public float anchorJitter = 5f;
        [Min(.2f)] public float surfaceOffset = 1.05f;
        [Min(.2f)] public float rowVerticalSpacing = .9f;
        [Min(.5f)] public float coinHorizontalSpacing = 1.75f;

        [Header("Progression bands")]
        [Min(1f)] public float sparseEndDistance = 300f;
        [Min(1f)] public float growingEndDistance = 600f;
        [Min(1f)] public float variedEndDistance = 1200f;
        [Min(1f)] public float allFiveStartDistance = 10000f;

        public float MaximumGroupsPer100Meters => 100f / Mathf.Max(1f, cellLength);
        public int MaximumCoinsPerGroup => 12;

        public bool TryGetGroup(int cellIndex, out CoinRouteGroup group)
        {
            group = default;
            if (cellIndex < 0) return false;
            float unit = Hash01(cellIndex, 17);
            float anchor = cellIndex * cellLength + cellLength * .5f +
                           Mathf.Lerp(-anchorJitter, anchorJitter, unit);
            if (anchor < firstCoinDistance) return false;
            int tier = DensityTier(anchor);
            int phase = PositiveModulo(routeSeed, 6);
            bool enabled = tier switch
            {
                0 => cellIndex == 0 || PositiveModulo(cellIndex + phase, 3) == 0,
                1 => PositiveModulo(cellIndex + phase, 2) == 0,
                2 => PositiveModulo(cellIndex + phase, 3) != 0,
                _ => true
            };
            if (!enabled) return false;

            // Coin groups always hug the sampled surface in two clean rows. The lower row grows first;
            // the upper row starts at the growing band and then adds one coin at a time until the
            // 10,000 m peak. This avoids the old abrupt jump to two full rows near the start.
            int lowerRowCount = tier switch
            {
                0 => 3,
                1 => 4,
                2 => 5,
                _ => 6
            };
            int upperRowCount = 0;
            if (anchor >= sparseEndDistance)
            {
                float upperProgress = Mathf.InverseLerp(sparseEndDistance,
                    Mathf.Max(sparseEndDistance + cellLength, allFiveStartDistance), anchor);
                upperRowCount = Mathf.Clamp(1 + Mathf.FloorToInt(upperProgress * 5f), 1, 6);
            }
            group = new CoinRouteGroup
            {
                cellIndex = cellIndex,
                anchorDistance = anchor,
                pattern = CoinPatternKind.Line,
                coinCount = lowerRowCount + upperRowCount,
                lowerRowCoinCount = lowerRowCount,
                upperRowCoinCount = upperRowCount,
                densityTier = tier
            };
            return true;
        }

        public CoinRoutePoint GetPoint(CoinRouteGroup group, int pointIndex)
        {
            int count = Mathf.Max(1, group.coinCount);
            int index = Mathf.Clamp(pointIndex, 0, count - 1);
            int lowerCount = Mathf.Clamp(group.lowerRowCoinCount, 1, count);
            bool upperRow = index >= lowerCount;
            int rowIndex = upperRow ? index - lowerCount : index;
            int rowCount = upperRow ? Mathf.Max(1, group.upperRowCoinCount) : lowerCount;
            float centered = rowIndex - (rowCount - 1) * .5f;
            float distance = centered * Mathf.Max(.5f, coinHorizontalSpacing);
            float height = upperRow ? Mathf.Max(.2f, rowVerticalSpacing) : 0f;
            float lateral = 0f;
            int fiveCoinCount = GetFiveCoinCount(group.anchorDistance, count);
            int value = index >= count - fiveCoinCount ? 5 : 1;
            return new CoinRoutePoint
            {
                distanceOffset = distance,
                heightOffset = height,
                lateralOffset = lateral,
                value = value
            };
        }

        public int GetFiveCoinCount(float distance, int coinCount)
        {
            int safeCount = Mathf.Max(1, coinCount);
            if (distance < sparseEndDistance) return 0;
            if (distance >= allFiveStartDistance) return safeCount;

            float progress = Mathf.InverseLerp(sparseEndDistance,
                Mathf.Max(sparseEndDistance + cellLength, allFiveStartDistance), distance);
            return Mathf.Clamp(1 + Mathf.FloorToInt(progress * (safeCount - 1)), 1, safeCount);
        }

        public long GetStableCoinId(CoinRouteGroup group, int pointIndex)
        {
            unchecked
            {
                ulong route = (uint)routeSeed;
                ulong cell = (uint)group.cellIndex;
                ulong point = (uint)(pointIndex + 1);
                ulong value = (route << 32) ^ (cell << 8) ^ point;
                return value == 0UL ? 1L : (long)value;
            }
        }

        public int FirstCellForDistance(float distance) =>
            Mathf.Max(0, Mathf.FloorToInt((distance - cellLength) / Mathf.Max(1f, cellLength)));

        public int LastCellForDistance(float distance) =>
            Mathf.Max(0, Mathf.CeilToInt((distance + cellLength) / Mathf.Max(1f, cellLength)));

        private int DensityTier(float distance)
        {
            if (distance < sparseEndDistance) return 0;
            if (distance < growingEndDistance) return 1;
            if (distance < variedEndDistance) return 2;
            return 3;
        }

        private float Hash01(int cellIndex, int salt)
        {
            uint hash = Hash((uint)(cellIndex ^ routeSeed ^ salt * 486187739));
            return (hash & 0x00FFFFFFu) / 16777215f;
        }

        private static uint Hash(uint value)
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }

        private static int PositiveModulo(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private void OnValidate()
        {
            cellLength = Mathf.Max(24f, cellLength);
            anchorJitter = Mathf.Min(anchorJitter, cellLength * .2f);
            rowVerticalSpacing = Mathf.Max(.2f, rowVerticalSpacing);
            coinHorizontalSpacing = Mathf.Max(.5f, coinHorizontalSpacing);
            sparseEndDistance = Mathf.Max(firstCoinDistance + cellLength, sparseEndDistance);
            growingEndDistance = Mathf.Max(sparseEndDistance + cellLength, growingEndDistance);
            variedEndDistance = Mathf.Max(growingEndDistance + cellLength, variedEndDistance);
            allFiveStartDistance = Mathf.Max(variedEndDistance + cellLength, allFiveStartDistance);
        }
    }
}
