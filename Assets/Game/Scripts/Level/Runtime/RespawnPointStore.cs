using UnityEngine;

namespace SnowbreakFan.Level
{
    public sealed class RespawnPointStore
    {
        public RespawnPointStore(Vector2 defaultPoint) => Current = defaultPoint;

        public Vector2 Current { get; private set; }

        public void Set(Vector2 point) => Current = point;
    }
}
