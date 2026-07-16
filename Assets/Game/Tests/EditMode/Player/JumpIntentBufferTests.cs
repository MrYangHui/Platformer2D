using NUnit.Framework;
using SnowbreakFan.Player;

public sealed class JumpIntentBufferTests
{
    [Test]
    public void ConsumesBufferedPressInsideCoyoteWindow()
    {
        var buffer = new JumpIntentBuffer();
        buffer.MarkGrounded(1.00f);
        buffer.PressJump(1.05f);
        Assert.That(buffer.TryConsume(1.08f, 0.10f, 0.12f), Is.True);
        Assert.That(buffer.TryConsume(1.08f, 0.10f, 0.12f), Is.False);
    }

    [Test]
    public void RejectsExpiredPress()
    {
        var buffer = new JumpIntentBuffer();
        buffer.MarkGrounded(1.00f);
        buffer.PressJump(1.01f);
        Assert.That(buffer.TryConsume(1.20f, 0.10f, 0.12f), Is.False);
    }
}
