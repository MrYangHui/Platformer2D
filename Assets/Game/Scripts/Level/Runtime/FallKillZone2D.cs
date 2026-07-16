using UnityEngine;

namespace SnowbreakFan.Level
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FallKillZone2D : MonoBehaviour
    {
        [SerializeField] private RespawnService respawnService;
        [SerializeField] private LayerMask playerMask;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((playerMask.value & (1 << other.gameObject.layer)) == 0)
                return;
            if (respawnService == null)
            {
                Debug.LogError("FallKillZone2D is not configured.", this);
                return;
            }

            respawnService.Respawn();
        }
    }
}
