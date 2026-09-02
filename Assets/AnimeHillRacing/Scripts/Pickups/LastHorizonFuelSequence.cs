using System.Collections.Generic;
using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.Pickups
{
    public readonly struct LastHorizonFuelPoint
    {
        public readonly float Position;
        public readonly bool IsScarcityFuel;
        public readonly long StableId;

        public LastHorizonFuelPoint(float position, bool isScarcityFuel, long stableId)
        {
            Position = position;
            IsScarcityFuel = isScarcityFuel;
            StableId = stableId;
        }
    }

    /// <summary>
    /// Stateless-input, cached-output fuel route for Last Horizon. The cache is only an acceleration:
    /// every position is derived from map seed and sequence index, so streaming order and backtracking
    /// cannot change a can that was already seen. Normal gaps resume immediately after the single
    /// 500-1000 m reset gap scheduled at each absolute 20 km boundary.
    /// </summary>
    public sealed class LastHorizonFuelSequence
    {
        private readonly List<LastHorizonFuelPoint> points = new List<LastHorizonFuelPoint>(384);
        private MapProfile profile;
        private uint seed;
        private float lastFuelPosition;
        private float nextScarcityStart;
        private int scarcityIndex;

        public float CurrentSpacing { get; private set; }
        public float NextPosition { get; private set; }
        public int SequenceIndex => points.Count;
        public float NextScarcityStart => nextScarcityStart;

        public void Reset(MapProfile mapProfile)
        {
            profile = mapProfile;
            seed = CombinedSeed(mapProfile);
            points.Clear();
            lastFuelPosition = 0f;
            CurrentSpacing = 0f;
            scarcityIndex = 0;
            nextScarcityStart = LastHorizonSettings.RequiredSystemLength;
            NextPosition = RandomRange(200f, 500f, 7103);
        }

        public void GetPoints(float start, float end, List<LastHorizonFuelPoint> results)
        {
            results.Clear();
            if (profile == null || profile.lastHorizon == null || end <= 0f) return;
            EnsureThrough(end);
            for (int i = 0; i < points.Count; i++)
            {
                LastHorizonFuelPoint point = points[i];
                if (point.Position < start) continue;
                if (point.Position >= end) break;
                results.Add(point);
            }
        }

        private void EnsureThrough(float end)
        {
            int safety = 0;
            while (NextPosition < end && safety++ < 100000)
                AppendNext();
        }

        private void AppendNext()
        {
            LastHorizonSettings settings = profile.lastHorizon;
            int sequence = points.Count;
            Vector2 normalRange = Ordered(settings.normalFuelGapRange, 200f, 500f);
            Vector2 scarcityRange = Ordered(settings.scarcityFuelGapRange, 500f, 1000f);
            scarcityRange.y = Mathf.Min(1000f, scarcityRange.y);

            float normalGap = RandomRange(normalRange.x, normalRange.y, sequence * 7919 + 7103);
            bool scarcityFuel = lastFuelPosition + normalGap >= nextScarcityStart;
            float minimumGap = scarcityFuel
                ? Mathf.Max(scarcityRange.x, nextScarcityStart - lastFuelPosition)
                : normalRange.x;
            float maximumGap = scarcityFuel ? scarcityRange.y : normalRange.y;
            maximumGap = Mathf.Max(minimumGap, maximumGap);
            float requestedGap = scarcityFuel
                ? RandomRange(minimumGap, maximumGap, scarcityIndex * 8191 + 7121)
                : normalGap;
            float target = lastFuelPosition + requestedGap;
            float safePosition = FindSafePosition(target, lastFuelPosition + minimumGap,
                lastFuelPosition + maximumGap);
            CurrentSpacing = safePosition - lastFuelPosition;
            lastFuelPosition = safePosition;
            long stableId = ((long)seed << 32) ^ (uint)(sequence + 1) ^ 0x4c485f4655454cL;
            if (stableId == 0L) stableId = sequence + 1L;
            points.Add(new LastHorizonFuelPoint(safePosition, scarcityFuel, stableId));

            if (scarcityFuel)
            {
                scarcityIndex++;
                float interval = Mathf.Max(1000f, settings.cycleResetFuelInterval);
                nextScarcityStart = (scarcityIndex + 1) * interval;
            }

            float nextNormal = RandomRange(normalRange.x, normalRange.y,
                (sequence + 1) * 7919 + 7103);
            bool nextIsScarcity = safePosition + nextNormal >= nextScarcityStart;
            float nextMinimumGap = nextIsScarcity
                ? Mathf.Max(scarcityRange.x, nextScarcityStart - safePosition)
                : normalRange.x;
            float nextMaximumGap = nextIsScarcity
                ? Mathf.Max(nextMinimumGap, scarcityRange.y)
                : normalRange.y;
            float nextGap = nextIsScarcity
                ? RandomRange(nextMinimumGap, nextMaximumGap, scarcityIndex * 8191 + 7121)
                : nextNormal;
            NextPosition = safePosition + nextGap;
        }

        private float FindSafePosition(float target, float minimum, float maximum)
        {
            LastHorizonSettings settings = profile.lastHorizon;
            float min = Mathf.Min(minimum, maximum);
            float max = Mathf.Max(minimum, maximum);
            float step = Mathf.Max(.5f, settings.fuelSafetySearchStep);
            float radius = Mathf.Max(step, settings.fuelSafetySearchRadius);
            int steps = Mathf.CeilToInt(radius / step);
            float best = Mathf.Clamp(target, min, max);
            float bestSlope = float.MaxValue;

            for (int i = 0; i <= steps; i++)
            {
                int sideCount = i == 0 ? 1 : 2;
                for (int side = 0; side < sideCount; side++)
                {
                    float offset = i == 0 ? 0f : i * step * (side == 0 ? -1f : 1f);
                    float candidate = Mathf.Clamp(target + offset, min, max);
                    if (TerrainGenerator.IsLastHorizonObstacleReserved(profile, candidate) ||
                        IsCoinPickupReserved(candidate)) continue;
                    float slope = SurfaceSlope(candidate);
                    if (slope < bestSlope)
                    {
                        bestSlope = slope;
                        best = candidate;
                    }
                    if (slope <= profile.fuelMaximumPlacementSlope) return candidate;
                }
            }

            // A complete range scan is the deterministic fallback for an unusually steep target area.
            // The terrain grammar has no holes, so selecting its shallowest point always remains reachable.
            for (float candidate = min; candidate <= max; candidate += step)
            {
                if (TerrainGenerator.IsLastHorizonObstacleReserved(profile, candidate) ||
                    IsCoinPickupReserved(candidate)) continue;
                float slope = SurfaceSlope(candidate);
                if (slope >= bestSlope) continue;
                bestSlope = slope;
                best = candidate;
            }
            return Mathf.Clamp(best, min, max);
        }

        private float SurfaceSlope(float x)
        {
            const float halfSample = 1f;
            float a = TerrainGenerator.SampleLastHorizonHeight(profile, Mathf.Max(0f, x - halfSample));
            float b = TerrainGenerator.SampleLastHorizonHeight(profile, x + halfSample);
            return Mathf.Abs(Mathf.Atan2(b - a, halfSample * 2f) * Mathf.Rad2Deg);
        }

        private bool IsCoinPickupReserved(float position)
        {
            CoinRouteDefinition route = profile != null ? profile.coinRoute : null;
            if (route == null) return false;
            int firstCell = route.FirstCellForDistance(position - 3f);
            int lastCell = route.LastCellForDistance(position + 3f);
            for (int cell = firstCell; cell <= lastCell; cell++)
            {
                if (!route.TryGetGroup(cell, out CoinRouteGroup group)) continue;
                for (int pointIndex = 0; pointIndex < group.coinCount; pointIndex++)
                {
                    CoinRoutePoint point = route.GetPoint(group, pointIndex);
                    float coinPosition = group.anchorDistance + point.distanceOffset;
                    if (Mathf.Abs(position - coinPosition) < 2.25f) return true;
                }
            }
            return false;
        }

        private float RandomRange(float minimum, float maximum, int salt) =>
            Mathf.Lerp(minimum, maximum, Hash01(seed, salt));

        private static Vector2 Ordered(Vector2 range, float fallbackMinimum, float fallbackMaximum)
        {
            float minimum = range.x > 0f ? range.x : fallbackMinimum;
            float maximum = range.y > 0f ? range.y : fallbackMaximum;
            return new Vector2(Mathf.Min(minimum, maximum), Mathf.Max(minimum, maximum));
        }

        private static uint CombinedSeed(MapProfile mapProfile)
        {
            uint hash = 2166136261u;
            string id = mapProfile != null ? mapProfile.mapId ?? string.Empty : string.Empty;
            for (int i = 0; i < id.Length; i++) hash = (hash ^ id[i]) * 16777619u;
            return hash ^ (uint)(mapProfile != null ? mapProfile.seed : 0) * 2246822519u;
        }

        private static float Hash01(uint routeSeed, int value)
        {
            uint x = routeSeed ^ (uint)value * 374761393u;
            x = (x ^ (x >> 13)) * 1274126177u;
            x ^= x >> 16;
            return (x & 0x00ffffffu) / 16777215f;
        }
    }
}
