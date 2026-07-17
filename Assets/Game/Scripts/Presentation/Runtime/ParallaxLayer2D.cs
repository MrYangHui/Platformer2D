using UnityEngine;

namespace SnowbreakFan.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ParallaxLayer2D : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private SpriteRenderer[] segments;
        [SerializeField, Range(0f, 1f)] private float horizontalFollow = 0.8f;
        [SerializeField, Range(0f, 1f)] private float verticalFollow = 0.8f;
        [SerializeField, Min(0.01f)] private float tileWidth = 30f;
        [SerializeField, Min(0f)] private float overlap = 0.2f;

        private Vector3 origin;

        private void Awake()
        {
            if (cameraTransform != null &&
                segments is { Length: 3 } &&
                segments[0] != null &&
                segments[1] != null &&
                segments[2] != null &&
                tileWidth > 0f)
            {
                origin = transform.position;
                return;
            }

            Debug.LogError($"Parallax layer is not configured: {name}", this);
            enabled = false;
        }

        private void LateUpdate()
        {
            Vector3 cameraPosition = cameraTransform.position;
            transform.position = new Vector3(
                origin.x + (cameraPosition.x - origin.x) * horizontalFollow,
                origin.y + (cameraPosition.y - origin.y) * verticalFollow,
                origin.z);

            float stride = Mathf.Max(0.01f, tileWidth - overlap);
            float cameraLocalX = transform.InverseTransformPoint(cameraPosition).x;
            float center = Mathf.Round(cameraLocalX / stride) * stride;

            for (int index = 0; index < segments.Length; index++)
            {
                Vector3 localPosition = segments[index].transform.localPosition;
                localPosition.x = center + (index - 1) * stride;
                segments[index].transform.localPosition = localPosition;
            }
        }
    }
}
