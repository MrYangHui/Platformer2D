using System;
using UnityEngine;

namespace SnowbreakFan.Level
{
    [CreateAssetMenu(menuName = "Game/Channels/Level Completed")]
    public sealed class LevelCompletedChannel : ScriptableObject
    {
        public event Action Raised;
        public void Raise() => Raised?.Invoke();
    }
}
