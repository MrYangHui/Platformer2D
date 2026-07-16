using System;
using UnityEngine;

namespace SnowbreakFan.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private GroundProbe2D groundProbe;

        private readonly JumpIntentBuffer jumpBuffer = new();
        private Rigidbody2D body;
        private bool cutJump;

        public PlayerMotionState State { get; private set; }
        public event Action<PlayerMotionState> StateChanged;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (config == null || input == null || groundProbe == null)
            {
                Debug.LogError("PlayerMotor2D is missing required references.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (input.ConsumePressed())
                jumpBuffer.PressJump(Time.time);
            if (input.ConsumeReleased())
                cutJump = true;
        }

        private void FixedUpdate()
        {
            bool grounded = groundProbe.IsGrounded;
            if (grounded)
                jumpBuffer.MarkGrounded(Time.time);

            Vector2 velocity = body.linearVelocity;
            bool hasInput = Mathf.Abs(input.MoveX) > 0.01f;
            float acceleration = grounded
                ? (hasInput ? config.GroundAcceleration : config.GroundDeceleration)
                : (hasInput ? config.AirAcceleration : config.AirDeceleration);
            velocity.x = PlayerMovementMath.HorizontalVelocity(
                velocity.x,
                input.MoveX,
                config.MaxSpeed,
                acceleration,
                Time.fixedDeltaTime);

            if (jumpBuffer.TryConsume(Time.time, config.CoyoteTime, config.JumpBufferTime))
                velocity.y = config.JumpSpeed;
            if (cutJump && velocity.y > 0f)
                velocity.y *= config.JumpCutMultiplier;
            cutJump = false;

            body.gravityScale = config.GravityScale * (velocity.y < 0f ? config.FallGravityMultiplier : 1f);
            body.linearVelocity = velocity;
            SetState(grounded && velocity.y <= 0.01f
                ? PlayerMotionState.Grounded
                : velocity.y > 0f
                    ? PlayerMotionState.Rising
                    : PlayerMotionState.Falling);
        }

        public void ResetMotion()
        {
            jumpBuffer.Reset();
            body.linearVelocity = Vector2.zero;
            body.gravityScale = config.GravityScale;
        }

        private void SetState(PlayerMotionState next)
        {
            if (State == next)
                return;

            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
