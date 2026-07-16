using UnityEditor;
using UnityEditor.SceneManagement;

namespace SnowbreakFan.Infrastructure.Editor
{
    [InitializeOnLoad]
    public static class ProjectEntryConfigurator
    {
        private const string BootstrapScenePath = "Assets/Game/Scenes/00_Bootstrap.unity";

        static ProjectEntryConfigurator()
        {
            if (UnityEngine.Application.isBatchMode)
                EditorSceneManager.playModeStartScene = null;
            else
                Configure();
        }

        [MenuItem("Snowbreak Fan/Configure Bootstrap Play Mode Start")]
        public static void Configure()
        {
            SceneAsset bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
            if (bootstrapScene == null)
            {
                UnityEngine.Debug.LogError($"Bootstrap scene is missing: {BootstrapScenePath}");
                return;
            }

            if (EditorSceneManager.playModeStartScene != bootstrapScene)
                EditorSceneManager.playModeStartScene = bootstrapScene;
        }
    }
}
