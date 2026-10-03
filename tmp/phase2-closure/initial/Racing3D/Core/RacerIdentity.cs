using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [Serializable]
    public sealed class RacerIdentity
    {
        public string racerId;
        public string displayName;
        public string vehicleId;
        public string wheelId;
        public int mmr;
        public bool isHuman;
        public int personalitySeed;
        public int gridPosition; // 1 to 12

        public RacerIdentity(string id, string name, string vehicle, string wheel, int rating, bool human, int seed, int grid)
        {
            racerId = id;
            displayName = name;
            vehicleId = vehicle;
            wheelId = wheel;
            mmr = Mathf.Max(0, rating);
            isHuman = human;
            personalitySeed = seed;
            gridPosition = grid;
        }

        public override string ToString() =>
            $"[{gridPosition}] {displayName} (MMR: {mmr}) {(isHuman ? "[HUMAN]" : "[AI]")}";
    }
}
