using SnowbreakFan.Core;
using UnityEngine;

namespace SnowbreakFan.Level
{
    public sealed class RespawnService : MonoBehaviour
    {
        [SerializeField] private Transform defaultSpawn;
        [SerializeField] private MonoBehaviour targetBehaviour;

        private RespawnPointStore store;

        private IRespawnTarget Target => targetBehaviour as IRespawnTarget;

        private void Awake()
        {
            if (defaultSpawn == null || Target == null)
            {
                Debug.LogError("RespawnService is not configured.", this);
                enabled = false;
                return;
            }

            store = new RespawnPointStore(defaultSpawn.position);
        }

        public void SetCheckpoint(Vector2 point) => store.Set(point);

        public void Respawn() => Target.RespawnAt(store.Current);
    }
}
