using System;
using System.Collections.Generic;

namespace AnimeHillRacing.TimeTrial
{
    [Serializable]
    public sealed class CheckpointGraph
    {
        private readonly Dictionary<int, int[]> next = new Dictionary<int, int[]>();

        public void Add(int nodeId, params int[] nextNodeIds) => next[nodeId] = nextNodeIds;

        public bool IsValidTransition(int currentNode, int candidate)
        {
            if (!next.TryGetValue(currentNode, out int[] values)) return false;
            for (int i = 0; i < values.Length; i++)
                if (values[i] == candidate) return true;
            return false;
        }
    }
}
