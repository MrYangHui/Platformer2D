using NUnit.Framework;
using SnowbreakFan.Player;

public sealed class PlayerMovementMathTests
{
    [Test]
    public void HorizontalVelocity_AcceleratesTowardTarget()
    {
        Assert.That(PlayerMovementMath.HorizontalVelocity(0f, 1f, 6f, 20f, 0.1f), Is.EqualTo(2f).Within(0.001f));
    }

    [Test]
    public void HorizontalVelocity_NeverExceedsMaxSpeed()
    {
        Assert.That(PlayerMovementMath.HorizontalVelocity(5.8f, 1f, 6f, 20f, 0.1f), Is.EqualTo(6f).Within(0.001f));
    }
}
