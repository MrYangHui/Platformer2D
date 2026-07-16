using System;
using NUnit.Framework;
using SnowbreakFan.Core;

public sealed class SceneLoadPlanTests
{
    [Test]
    public void Create_ReturnsLevelThenUi()
    {
        CollectionAssert.AreEqual(
            new[] { "10_Level_Prototype", "20_UI_Gameplay" },
            SceneLoadPlan.Create("10_Level_Prototype", "20_UI_Gameplay"));
    }

    [TestCase("", "20_UI_Gameplay")]
    [TestCase("10_Level_Prototype", "")]
    [TestCase("Same", "Same")]
    public void Create_RejectsInvalidNames(string level, string ui)
    {
        Assert.Throws<ArgumentException>(() => SceneLoadPlan.Create(level, ui));
    }
}
