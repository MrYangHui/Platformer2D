using System;
using System.Collections.Generic;

namespace SnowbreakFan.Collectibles
{
    public sealed class LevelSessionState
    {
        private readonly HashSet<string> ids = new();

        public LevelSessionState(int total)
        {
            if (total < 1)
                throw new ArgumentOutOfRangeException(nameof(total));
            Total = total;
        }

        public int Total { get; }
        public int Collected => ids.Count;
        public bool IsComplete => Collected >= Total;

        public bool TryCollect(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Collectible id is required.", nameof(id));
            return ids.Add(id);
        }
    }
}
