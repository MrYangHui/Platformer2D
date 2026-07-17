using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SnowbreakFan.Presentation
{
    [CreateAssetMenu(menuName = "Snowbreak Fan/Character Presentation Profile")]
    public sealed class CharacterPresentationProfile : ScriptableObject
    {
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] runFrames;
        [SerializeField] private Sprite risingFrame;
        [SerializeField] private Sprite apexFrame;
        [SerializeField] private Sprite fallingFrame;
        [SerializeField] private Sprite fallbackFrame;
        [SerializeField] private float idleFramesPerSecond = 4f;
        [SerializeField] private float runFramesPerSecond = 16f;
        [SerializeField] private float movementThreshold = 0.1f;
        [SerializeField] private float referenceRunSpeed = 6f;
        [SerializeField] private float apexVelocityThreshold = 0.75f;
        [SerializeField] private Vector3 visualRootLocalPosition = new(0f, -1.1f, 0f);
        [SerializeField] private float visualScale = 1f;

        public IReadOnlyList<Sprite> IdleFrames => idleFrames;
        public IReadOnlyList<Sprite> RunFrames => runFrames;
        public Sprite RisingFrame => risingFrame;
        public Sprite ApexFrame => apexFrame;
        public Sprite FallingFrame => fallingFrame;
        public Sprite FallbackFrame => fallbackFrame;
        public float IdleFramesPerSecond => idleFramesPerSecond;
        public float RunFramesPerSecond => runFramesPerSecond;
        public float MovementThreshold => movementThreshold;
        public float ReferenceRunSpeed => referenceRunSpeed;
        public float ApexVelocityThreshold => apexVelocityThreshold;
        public Vector3 VisualRootLocalPosition => visualRootLocalPosition;
        public float VisualScale => visualScale;

        public bool TryValidate(out string error)
        {
            if (idleFrames == null || idleFrames.Length < 2 || idleFrames.Any(frame => frame == null))
            {
                error = "Idle requires at least two complete frames.";
                return false;
            }

            if (runFrames == null || runFrames.Length < 8 || runFrames.Any(frame => frame == null))
            {
                error = "Run requires at least eight complete frames.";
                return false;
            }

            if (risingFrame == null)
            {
                error = "Rising frame is required.";
                return false;
            }

            if (apexFrame == null)
            {
                error = "Apex frame is required.";
                return false;
            }

            if (fallingFrame == null)
            {
                error = "Falling frame is required.";
                return false;
            }

            if (fallbackFrame == null)
            {
                error = "Fallback frame is required.";
                return false;
            }

            if (!IsPositiveFinite(idleFramesPerSecond) ||
                !IsPositiveFinite(runFramesPerSecond) ||
                !IsPositiveFinite(referenceRunSpeed))
            {
                error = "Animation timing and reference run speed must be positive finite values.";
                return false;
            }

            if (!IsNonNegativeFinite(movementThreshold) ||
                !IsNonNegativeFinite(apexVelocityThreshold))
            {
                error = "Movement and apex thresholds must be non-negative finite values.";
                return false;
            }

            if (!IsPositiveFinite(visualScale))
            {
                error = "Visual scale must be a positive finite value.";
                return false;
            }

            if (!IsFinite(visualRootLocalPosition.x) ||
                !IsFinite(visualRootLocalPosition.y) ||
                !IsFinite(visualRootLocalPosition.z))
            {
                error = "Visual root position must contain finite values.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool IsPositiveFinite(float value) => value > 0f && IsFinite(value);

        private static bool IsNonNegativeFinite(float value) => value >= 0f && IsFinite(value);

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
