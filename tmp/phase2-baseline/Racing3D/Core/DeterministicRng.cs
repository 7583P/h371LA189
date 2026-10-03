using System;

namespace AnimeHillRacing.Racing3D
{
    /// <summary>
    /// Fast, deterministic pseudo-random generator based on XorShift128.
    /// Provides reproducible results across platforms for a given seed.
    /// </summary>
    public sealed class DeterministicRng
    {
        private uint x;
        private uint y;
        private uint z;
        private uint w;

        public int InitialSeed { get; }

        public DeterministicRng(int seed)
        {
            InitialSeed = seed;
            Reseed(seed);
        }

        public void Reseed(int seed)
        {
            uint s = (uint)(seed == 0 ? 123456789 : Math.Abs(seed));
            // MurmurHash3 32-bit avalanche mixer
            s ^= s >> 16;
            s *= 0x85ebca6b;
            s ^= s >> 13;
            s *= 0xc2b2ae35;
            s ^= s >> 16;
            if (s == 0) s = 123456789;

            x = s;
            y = 362436069 ^ s;
            z = 521288629 ^ (s << 3);
            w = 88675123 ^ (s >> 5);

            for (int i = 0; i < 8; i++) NextUInt();
        }

        public uint NextUInt()
        {
            uint t = x ^ (x << 11);
            x = y;
            y = z;
            z = w;
            w = w ^ (w >> 19) ^ (t ^ (t >> 8));
            return w;
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive) return minInclusive;
            uint range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        public int Next(int maxExclusive) => Next(0, maxExclusive);

        public float NextFloat()
        {
            // [0.0, 1.0)
            return (NextUInt() & 0x00FFFFFF) / 16777216.0f;
        }

        public float Range(float minInclusive, float maxInclusive)
        {
            if (minInclusive >= maxInclusive) return minInclusive;
            return minInclusive + NextFloat() * (maxInclusive - minInclusive);
        }

        /// <summary>
        /// Generates an approximation of Gaussian / Normal distribution with given mean and standard deviation.
        /// Box-Muller transform using deterministic floats.
        /// </summary>
        public float NextGaussian(float mean, float stdDev)
        {
            float u1 = Math.Max(1e-7f, NextFloat());
            float u2 = NextFloat();
            float randStdNormal = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
            return mean + stdDev * randStdNormal;
        }
    }
}
