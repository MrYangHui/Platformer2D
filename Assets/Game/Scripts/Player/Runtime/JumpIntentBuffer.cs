namespace SnowbreakFan.Player
{
    public sealed class JumpIntentBuffer
    {
        private float groundedAt = float.NegativeInfinity;
        private float pressedAt = float.NegativeInfinity;
        private bool consumed = true;

        public void MarkGrounded(float now) => groundedAt = now;

        public void PressJump(float now)
        {
            pressedAt = now;
            consumed = false;
        }

        public bool TryConsume(float now, float coyoteTime, float bufferTime)
        {
            if (consumed || now - groundedAt > coyoteTime || now - pressedAt > bufferTime)
                return false;

            consumed = true;
            return true;
        }

        public void Reset()
        {
            groundedAt = float.NegativeInfinity;
            pressedAt = float.NegativeInfinity;
            consumed = true;
        }
    }
}
