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

        [Header("Progression bands")]
        [Min(1f)] public float sparseEndDistance = 300f;
        [Min(1f)] public float growingEndDistance = 600f;
        [Min(1f)] public float variedEndDistance = 1200f;

        public float MaximumGroupsPer100Meters => 100f / Mathf.Max(1f, cellLength);
        public int MaximumCoinsPerGroup => 7;

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

            uint patternHash = Hash((uint)(cellIndex ^ routeSeed ^ unchecked((int)0x6D2B79F5)));
            CoinPatternKind pattern;
            int coinCount;
            if (tier == 0)
            {
                pattern = (patternHash & 1u) == 0u ? CoinPatternKind.Line : CoinPatternKind.SpecialCenter;
                coinCount = pattern == CoinPatternKind.Line ? 3 : 5;
            }
            else if (tier == 1)
            {
                pattern = (CoinPatternKind)(patternHash % 3u);
                coinCount = 5;
            }
            else if (tier == 2)
            {
                pattern = (CoinPatternKind)(1 + patternHash % 4u);
                coinCount = 5;
            }
            else
            {
                pattern = (CoinPatternKind)(patternHash % 6u);
                coinCount = pattern == CoinPatternKind.JumpArc ? 7 : 5;
            }
            group = new CoinRouteGroup
            {
                cellIndex = cellIndex,
                anchorDistance = anchor,
                pattern = pattern,
                coinCount = coinCount,
                densityTier = tier
            };
            return true;
        }

        public CoinRoutePoint GetPoint(CoinRouteGroup group, int pointIndex)
        {
            int count = Mathf.Max(1, group.coinCount);
            int index = Mathf.Clamp(pointIndex, 0, count - 1);
            float centered = index - (count - 1) * .5f;
            float distance = centered * 2.15f;
            float height = 0f;
            float lateral = 0f;
            int value = 1;
            switch (group.pattern)
            {
                case CoinPatternKind.SpecialCenter:
                    if (index == count / 2) value = 5;
                    break;
                case CoinPatternKind.Arc:
                {
                    float normalized = count <= 1 ? 0f : index / (count - 1f);
                    height = Mathf.Sin(normalized * Mathf.PI) * 1.35f;
                    lateral = Mathf.Sin((normalized - .5f) * Mathf.PI) * .28f;
                    if (index == count / 2) value = 5;
                    break;
                }
                case CoinPatternKind.Ascent:
                    height = index * .38f;
                    if (index == count - 1) value = 5;
                    break;
                case CoinPatternKind.Descent:
                    height = (count - 1 - index) * .38f;
                    if (index == count - 1) value = 5;
                    break;
                case CoinPatternKind.JumpArc:
                {
                    float normalized = count <= 1 ? 0f : index / (count - 1f);
                    height = .45f + Mathf.Sin(normalized * Mathf.PI) * 2.15f;
                    lateral = Mathf.Sin(normalized * Mathf.PI * 2f) * .22f;
                    if (index == count / 2) value = 5;
                    break;
                }
            }
            return new CoinRoutePoint
            {
                distanceOffset = distance,
                heightOffset = height,
                lateralOffset = lateral,
                value = value
            };
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
            sparseEndDistance = Mathf.Max(firstCoinDistance + cellLength, sparseEndDistance);
            growingEndDistance = Mathf.Max(sparseEndDistance + cellLength, growingEndDistance);
            variedEndDistance = Mathf.Max(growingEndDistance + cellLength, variedEndDistance);
        }
    }
}
