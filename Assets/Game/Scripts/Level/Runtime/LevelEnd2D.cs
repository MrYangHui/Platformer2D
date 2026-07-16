using UnityEngine;

namespace SnowbreakFan.Level
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class LevelEnd2D : MonoBehaviour
    {
        [SerializeField] private LevelCompletedChannel channel;
        [SerializeField] private LayerMask playerMask;
        private bool completed;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (completed || (playerMask.value & (1 << other.gameObject.layer)) == 0)
                return;
            if (channel == null)
            {
                Debug.LogError("LevelEnd2D requires a completion channel.", this);
                return;
            }

            completed = true;
            channel.Raise();
        }
    }
}
