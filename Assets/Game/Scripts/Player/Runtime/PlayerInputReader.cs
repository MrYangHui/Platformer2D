using UnityEngine;
using UnityEngine.InputSystem;

namespace SnowbreakFan.Player
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference move;
        [SerializeField] private InputActionReference jump;

        private bool pressed;
        private bool released;

        public float MoveX => move.action.ReadValue<Vector2>().x;

        private void OnEnable()
        {
            move.action.Enable();
            jump.action.Enable();
            jump.action.performed += OnJumpPerformed;
            jump.action.canceled += OnJumpCanceled;
        }

        private void OnDisable()
        {
            jump.action.performed -= OnJumpPerformed;
            jump.action.canceled -= OnJumpCanceled;
            move.action.Disable();
            jump.action.Disable();
        }

        private void OnJumpPerformed(InputAction.CallbackContext _) => pressed = true;

        private void OnJumpCanceled(InputAction.CallbackContext _) => released = true;

        public bool ConsumePressed()
        {
            bool value = pressed;
            pressed = false;
            return value;
        }

        public bool ConsumeReleased()
        {
            bool value = released;
            released = false;
            return value;
        }
    }
}
