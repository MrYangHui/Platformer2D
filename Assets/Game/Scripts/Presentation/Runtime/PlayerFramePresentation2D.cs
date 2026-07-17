using SnowbreakFan.Player;
using UnityEngine;

namespace SnowbreakFan.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PlayerFramePresentation2D : MonoBehaviour
    {
        private enum PresentationState
        {
            Idle,
            Run,
            Rising,
            Apex,
            Falling
        }

        [SerializeField] private CharacterPresentationProfile profile;
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private PlayerMotor2D motor;

        private PresentationState currentState;
        private bool hasCurrentState;
        private bool facingRight = true;
        private readonly FramePlaybackClock playbackClock = new();

        private void Awake()
        {
            string error = string.Empty;
            if (profile == null || targetRenderer == null || visualRoot == null ||
                body == null || motor == null || !profile.TryValidate(out error))
            {
                Debug.LogError(
                    $"PlayerFramePresentation2D is missing valid presentation references. {error}",
                    this);
                enabled = false;
                return;
            }

            visualRoot.localPosition = profile.VisualRootLocalPosition;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one * profile.VisualScale;
            targetRenderer.sprite = profile.FallbackFrame;
            targetRenderer.flipX = false;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void Tick(float deltaTime)
        {
            float horizontalSpeed = body.linearVelocity.x;
            float absoluteHorizontalSpeed = Mathf.Abs(horizontalSpeed);
            if (absoluteHorizontalSpeed > profile.MovementThreshold)
                facingRight = horizontalSpeed > 0f;
            targetRenderer.flipX = !facingRight;

            PresentationState state = ResolveState(absoluteHorizontalSpeed);
            if (!hasCurrentState || state != currentState)
            {
                playbackClock.Reset();
                currentState = state;
                hasCurrentState = true;
            }
            else
            {
                AdvancePlayback(deltaTime, state, absoluteHorizontalSpeed);
            }

            targetRenderer.sprite = ResolveFrame(state) ??
                                    profile.FallbackFrame;
        }

        private void AdvancePlayback(
            float deltaTime,
            PresentationState state,
            float absoluteHorizontalSpeed)
        {
            switch (state)
            {
                case PresentationState.Idle:
                    playbackClock.Advance(deltaTime, profile.IdleFramesPerSecond);
                    break;
                case PresentationState.Run:
                    float speedMultiplier = Mathf.Clamp(
                        absoluteHorizontalSpeed / profile.ReferenceRunSpeed,
                        0.75f,
                        1.35f);
                    playbackClock.Advance(
                        deltaTime,
                        profile.RunFramesPerSecond * speedMultiplier);
                    break;
            }
        }

        private PresentationState ResolveState(float absoluteHorizontalSpeed)
        {
            if (motor.State == PlayerMotionState.Grounded)
            {
                return absoluteHorizontalSpeed > profile.MovementThreshold
                    ? PresentationState.Run
                    : PresentationState.Idle;
            }

            if (Mathf.Abs(body.linearVelocity.y) <= profile.ApexVelocityThreshold)
                return PresentationState.Apex;
            return motor.State == PlayerMotionState.Rising
                ? PresentationState.Rising
                : PresentationState.Falling;
        }

        private Sprite ResolveFrame(PresentationState state)
        {
            switch (state)
            {
                case PresentationState.Idle:
                    return ResolveLoopFrame(profile.IdleFrames);
                case PresentationState.Run:
                    return ResolveLoopFrame(profile.RunFrames);
                case PresentationState.Rising:
                    return profile.RisingFrame;
                case PresentationState.Apex:
                    return profile.ApexFrame;
                case PresentationState.Falling:
                    return profile.FallingFrame;
                default:
                    return profile.FallbackFrame;
            }
        }

        private Sprite ResolveLoopFrame(System.Collections.Generic.IReadOnlyList<Sprite> frames)
        {
            if (frames == null || frames.Count == 0)
                return null;
            int index = playbackClock.CurrentIndex(frames.Count);
            return frames[index];
        }
    }
}
