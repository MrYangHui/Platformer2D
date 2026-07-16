using NUnit.Framework;
using SnowbreakFan.Collectibles;

public sealed class LevelSessionStateTests
{
    [Test]
    public void UniqueIdsCountOnce()
    {
        var state = new LevelSessionState(3);
        Assert.That(state.TryCollect("sample-a"), Is.True);
        Assert.That(state.TryCollect("sample-a"), Is.False);
        Assert.That(state.Collected, Is.EqualTo(1));
        Assert.That(state.IsComplete, Is.False);
    }

    [Test]
    public void AllSamplesCompleteCollectionGoal()
    {
        var state = new LevelSessionState(2);
        state.TryCollect("a");
        state.TryCollect("b");
        Assert.That(state.IsComplete, Is.True);
    }
}
