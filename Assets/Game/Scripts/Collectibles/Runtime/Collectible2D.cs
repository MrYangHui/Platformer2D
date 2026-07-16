using UnityEngine;

namespace SnowbreakFan.Collectibles
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Collectible2D : MonoBehaviour
    {
        [SerializeField] private string collectibleId;
        [SerializeField] private LevelSessionController session;
        [SerializeField] private GameObject visual;
        private Collider2D triggerCollider;

        private void Awake() => triggerCollider = GetComponent<Collider2D>();

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
                return;
            if (session == null)
            {
                Debug.LogError("Collectible2D requires a LevelSessionController.", this);
                return;
            }
            if (!session.TryCollect(collectibleId))
                return;

            triggerCollider.enabled = false;
            if (visual != null)
                visual.SetActive(false);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(collectibleId))
                Debug.LogError("Collectible2D needs a unique id.", this);
        }
    }
}
