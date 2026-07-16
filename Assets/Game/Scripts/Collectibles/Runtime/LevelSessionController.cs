using UnityEngine;

namespace SnowbreakFan.Collectibles
{
    public sealed class LevelSessionController : MonoBehaviour
    {
        [SerializeField, Min(1)] private int totalSamples = 3;
        [SerializeField] private CollectibleCountChannel channel;
        private LevelSessionState state;

        private void Awake()
        {
            if (channel == null)
            {
                Debug.LogError("LevelSessionController requires a count channel.", this);
                enabled = false;
                return;
            }

            state = new LevelSessionState(totalSamples);
            channel.Publish(0, totalSamples);
        }

        public bool TryCollect(string id)
        {
            if (state == null || !state.TryCollect(id))
                return false;
            channel.Publish(state.Collected, state.Total);
            return true;
        }
    }
}
