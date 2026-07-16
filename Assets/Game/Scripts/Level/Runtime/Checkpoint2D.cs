using UnityEngine;

namespace SnowbreakFan.Level
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Checkpoint2D : MonoBehaviour
    {
        [SerializeField] private RespawnService respawnService;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private LayerMask playerMask;

        private bool activated;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activated || (playerMask.value & (1 << other.gameObject.layer)) == 0)
                return;
            if (respawnService == null || spawnPoint == null)
            {
                Debug.LogError("Checkpoint2D is not configured.", this);
                return;
            }

            activated = true;
            respawnService.SetCheckpoint(spawnPoint.position);
        }
    }
}
