using System;

namespace SnowbreakFan.Presentation
{
    public sealed class FramePlaybackClock
    {
        private double phase;

        public void Reset()
        {
            phase = 0d;
        }

        public void Advance(float deltaTime, float framesPerSecond)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (float.IsNaN(framesPerSecond) || float.IsInfinity(framesPerSecond) ||
                framesPerSecond <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(framesPerSecond));
            }

            phase += (double)deltaTime * framesPerSecond;
        }

        public int CurrentIndex(int frameCount)
        {
            if (frameCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(frameCount));
            return (int)Math.Floor(phase) % frameCount;
        }
    }
}
