using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Pickups
{
    public readonly struct GemRoutePoint
    {
        public readonly int SequenceIndex;
        public readonly float Position;

        public GemRoutePoint(int sequenceIndex, float position)
        {
            SequenceIndex = sequenceIndex;
            Position = position;
        }

        public long StableId => ((long)0x47454d << 32) | (uint)(SequenceIndex + 1);
    }

    /// <summary>
    /// Generates one deterministic route for the current run. Only distance records are retained;
    /// actual Gem GameObjects continue to use the existing chunk/pickup pools.
    /// </summary>
    public sealed class RandomGemRouteSequence
    {
        public const int MinimumSpacing = 700;
        public const int MaximumSpacing = 1000;

        private readonly List<GemRoutePoint> points = new List<GemRoutePoint>(32);
        private Random random;
        private float startPosition;
        private float lastPosition;

        public int Count => points.Count;
        public float NextPosition => points.Count > 0 ? points[points.Count - 1].Position : 0f;

        public void Reset(float startPosition, int seed)
        {
            points.Clear();
            random = new Random(seed);
            this.startPosition = startPosition;
            lastPosition = startPosition;
            AppendNext();
        }

        public void GetPoints(float startInclusive, float endExclusive, List<GemRoutePoint> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();
            if (endExclusive <= startInclusive) return;
            if (random == null) Reset(0f, 1);
            EnsureThrough(endExclusive);
            int firstIndex = FindFirstPointAtOrAfter(startInclusive);
            for (int i = firstIndex; i < points.Count; i++)
            {
                GemRoutePoint point = points[i];
                if (point.Position >= endExclusive) break;
                output.Add(point);
            }
        }

        public bool HasValidSpacing()
        {
            float previous = startPosition;
            for (int i = 0; i < points.Count; i++)
            {
                float spacing = points[i].Position - previous;
                if (spacing < MinimumSpacing || spacing > MaximumSpacing) return false;
                previous = points[i].Position;
            }
            return true;
        }

        private int FindFirstPointAtOrAfter(float position)
        {
            int low = 0;
            int high = points.Count;
            while (low < high)
            {
                int middle = low + ((high - low) >> 1);
                if (points[middle].Position < position)
                    low = middle + 1;
                else
                    high = middle;
            }
            return low;
        }

        private void EnsureThrough(float endExclusive)
        {
            int safety = 0;
            while (points[points.Count - 1].Position < endExclusive && safety++ < 1000000)
                AppendNext();
        }

        private void AppendNext()
        {
            lastPosition += random.Next(MinimumSpacing, MaximumSpacing + 1);
            points.Add(new GemRoutePoint(points.Count, lastPosition));
        }
    }
}
