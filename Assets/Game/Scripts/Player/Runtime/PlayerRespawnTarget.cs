using SnowbreakFan.Core;
using UnityEngine;

namespace SnowbreakFan.Player
{
    public sealed class PlayerRespawnTarget : MonoBehaviour, IRespawnTarget
    {
        [SerializeField] private PlayerMotor2D motor;

        public void RespawnAt(Vector2 position)
        {
            transform.position = position;
            motor.ResetMotion();
        }
    }
}
