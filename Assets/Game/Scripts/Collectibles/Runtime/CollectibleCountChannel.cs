using System;
using UnityEngine;

namespace SnowbreakFan.Collectibles
{
    [CreateAssetMenu(menuName = "Game/Channels/Collectible Count")]
    public sealed class CollectibleCountChannel : ScriptableObject
    {
        public event Action<int, int> Changed;
        public int Current { get; private set; }
        public int Total { get; private set; }

        public void Publish(int current, int total)
        {
            Current = current;
            Total = total;
            Changed?.Invoke(current, total);
        }
    }
}
