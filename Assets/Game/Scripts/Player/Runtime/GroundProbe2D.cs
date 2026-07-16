using UnityEngine;

namespace SnowbreakFan.Player
{
    public sealed class GroundProbe2D : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new(0.55f, 0.12f);
        [SerializeField] private LayerMask groundMask;

        public bool IsGrounded => Physics2D.OverlapBox(transform.position, size, 0f, groundMask) != null;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
