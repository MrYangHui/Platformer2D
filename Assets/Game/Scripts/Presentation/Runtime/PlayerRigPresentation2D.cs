using SnowbreakFan.Player;
using UnityEngine;

namespace SnowbreakFan.Presentation
{
    public sealed class PlayerRigPresentation2D : MonoBehaviour
    {
        private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
        private static readonly int IsAirborneHash = Animator.StringToHash("IsAirborne");
        private static readonly int RunSpeedHash = Animator.StringToHash("RunSpeed");

        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private float movementThreshold = 0.1f;
        [SerializeField] private float referenceRunSpeed = 6f;

        private bool facingRight = true;

        private void Awake()
        {
            if (animator == null || visualRoot == null || body == null || motor == null ||
                movementThreshold < 0f || referenceRunSpeed <= 0f)
            {
                Debug.LogError(
                    "PlayerRigPresentation2D is missing its required presentation references.",
                    this);
                enabled = false;
            }
        }

        private void Update()
        {
            float speed = Mathf.Abs(body.linearVelocity.x);
            if (speed > movementThreshold)
                facingRight = body.linearVelocity.x > 0f;

            bool airborne = motor.State != PlayerMotionState.Grounded;
            animator.SetBool(IsAirborneHash, airborne);
            animator.SetBool(IsRunningHash, !airborne && speed > movementThreshold);
            animator.SetFloat(
                RunSpeedHash,
                Mathf.Max(0.5f, speed / referenceRunSpeed));
            visualRoot.localScale = new Vector3(facingRight ? 1f : -1f, 1f, 1f);
        }
    }
}
