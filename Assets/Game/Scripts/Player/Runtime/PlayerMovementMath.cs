using UnityEngine;

namespace SnowbreakFan.Player
{
    public static class PlayerMovementMath
    {
        public static float HorizontalVelocity(
            float current,
            float input,
            float maxSpeed,
            float acceleration,
            float deltaTime)
        {
            float target = Mathf.Clamp(input, -1f, 1f) * maxSpeed;
            return Mathf.MoveTowards(current, target, acceleration * deltaTime);
        }
    }
}
