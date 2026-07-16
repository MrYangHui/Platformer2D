using NUnit.Framework;
using SnowbreakFan.Level;
using UnityEngine;

public sealed class RespawnPointStoreTests
{
    [Test]
    public void UsesDefaultUntilCheckpointIsSet()
    {
        var store = new RespawnPointStore(new Vector2(2f, 3f));
        Assert.That(store.Current, Is.EqualTo(new Vector2(2f, 3f)));
        store.Set(new Vector2(10f, 4f));
        Assert.That(store.Current, Is.EqualTo(new Vector2(10f, 4f)));
    }
}
