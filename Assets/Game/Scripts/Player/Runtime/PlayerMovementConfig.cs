using UnityEngine;

namespace SnowbreakFan.Player
{
    [CreateAssetMenu(menuName = "Game/Player Movement Config")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f)] public float MaxSpeed { get; private set; } = 6f;
        [field: SerializeField, Min(0f)] public float GroundAcceleration { get; private set; } = 55f;
        [field: SerializeField, Min(0f)] public float GroundDeceleration { get; private set; } = 70f;
        [field: SerializeField, Min(0f)] public float AirAcceleration { get; private set; } = 30f;
        [field: SerializeField, Min(0f)] public float AirDeceleration { get; private set; } = 20f;
        [field: SerializeField, Min(0f)] public float JumpSpeed { get; private set; } = 13f;
        [field: SerializeField, Min(0f)] public float GravityScale { get; private set; } = 4f;
        [field: SerializeField, Min(1f)] public float FallGravityMultiplier { get; private set; } = 1.5f;
        [field: SerializeField, Range(0f, 1f)] public float JumpCutMultiplier { get; private set; } = 0.5f;
        [field: SerializeField, Min(0f)] public float CoyoteTime { get; private set; } = 0.10f;
        [field: SerializeField, Min(0f)] public float JumpBufferTime { get; private set; } = 0.12f;
    }
}
