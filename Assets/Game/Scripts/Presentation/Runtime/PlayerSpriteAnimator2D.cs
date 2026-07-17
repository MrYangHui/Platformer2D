using SnowbreakFan.Player;
using UnityEngine;

namespace SnowbreakFan.Presentation
{
    public sealed class PlayerSpriteAnimator2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] runFrames;
        [SerializeField] private Sprite airborneFrame;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector3 baseVisualLocalPosition = new(0f, -0.95f, 0f);
        [SerializeField] private Vector2[] runFrameOffsets;
        [SerializeField] private Vector2 airborneOffset;
        [SerializeField] private float idleFramesPerSecond = 4f;
        [SerializeField] private float runFramesPerSecond = 12f;
        [SerializeField] private float movementThreshold = 0.1f;

        private float elapsed;
        private bool wasRunning;
        private bool wasAirborne;
        private bool facingRight = true;

        private void Awake()
        {
            if (targetRenderer == null || body == null || motor == null ||
                visualRoot == null ||
                idleFrames == null || idleFrames.Length != 4 ||
                runFrames == null || runFrames.Length != 8 || airborneFrame == null ||
                runFrameOffsets == null || runFrameOffsets.Length != runFrames.Length ||
                idleFramesPerSecond <= 0f || runFramesPerSecond <= 0f)
            {
                Debug.LogError("PlayerSpriteAnimator2D is missing its required presentation references.", this);
                enabled = false;
                return;
            }

            ApplyFrame(idleFrames[0], Vector2.zero);
        }

        private void Update()
        {
            float horizontalSpeed = body.linearVelocity.x;
            if (Mathf.Abs(horizontalSpeed) > movementThreshold)
                facingRight = horizontalSpeed > 0f;
            targetRenderer.flipX = !facingRight;

            bool airborne = motor.State != PlayerMotionState.Grounded;
            if (airborne)
            {
                if (!wasAirborne)
                    elapsed = 0f;
                ApplyFrame(airborneFrame, airborneOffset);
                wasAirborne = true;
                wasRunning = false;
                return;
            }

            if (wasAirborne)
            {
                elapsed = 0f;
                wasAirborne = false;
            }

            bool running = Mathf.Abs(horizontalSpeed) > movementThreshold;
            if (running != wasRunning)
                elapsed = 0f;
            wasRunning = running;

            Sprite[] frames = running ? runFrames : idleFrames;
            float framesPerSecond = running ? runFramesPerSecond : idleFramesPerSecond;
            elapsed += Time.deltaTime;
            int frameIndex = Mathf.FloorToInt(elapsed * framesPerSecond) % frames.Length;
            ApplyFrame(
                frames[frameIndex],
                running ? runFrameOffsets[frameIndex] : Vector2.zero);
        }

        private void ApplyFrame(Sprite sprite, Vector2 offset)
        {
            targetRenderer.sprite = sprite;
            float offsetX = facingRight ? offset.x : -offset.x;
            visualRoot.localPosition = baseVisualLocalPosition +
                new Vector3(offsetX, offset.y, 0f);
        }
    }
}
