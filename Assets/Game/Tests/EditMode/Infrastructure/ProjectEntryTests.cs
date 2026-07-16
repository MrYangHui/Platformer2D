using NUnit.Framework;
using SnowbreakFan.Infrastructure.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SnowbreakFan.Infrastructure.Tests
{
    public sealed class ProjectEntryTests
    {
        [TearDown]
        public void ClearPlayModeStartScene() => EditorSceneManager.playModeStartScene = null;

        [Test]
        public void TemplateSampleSceneIsNotPartOfTheProject()
        {
            SceneAsset sampleScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene.unity");
            Assert.That(sampleScene, Is.Null);
        }

        [Test]
        public void EditorPlayModeStartsFromBootstrap()
        {
            ProjectEntryConfigurator.Configure();
            SceneAsset startScene = EditorSceneManager.playModeStartScene;
            Assert.That(startScene, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(startScene),
                Is.EqualTo("Assets/Game/Scenes/00_Bootstrap.unity"));
        }
    }
}
