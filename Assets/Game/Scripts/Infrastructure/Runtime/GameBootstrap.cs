using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SnowbreakFan.Infrastructure
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private BootstrapSettings settings;

        private static GameBootstrap instance;
        private bool isLoadingScenes;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                instance.EnsureScenesLoaded();
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => EnsureScenesLoaded();

        private void EnsureScenesLoaded()
        {
            if (!isLoadingScenes)
                StartCoroutine(LoadConfiguredScenes());
        }

        private IEnumerator LoadConfiguredScenes()
        {
            isLoadingScenes = true;
            if (settings == null)
            {
                Debug.LogError("GameBootstrap requires BootstrapSettings.", this);
                isLoadingScenes = false;
                yield break;
            }

            foreach (string sceneName in settings.BuildLoadPlan())
            {
                string scenePath = $"Assets/Game/Scenes/{sceneName}.unity";
                if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
                {
                    Debug.LogError($"Scene is not enabled in Build Profiles: {sceneName}", this);
                    isLoadingScenes = false;
                    yield break;
                }

                if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                    yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }

            isLoadingScenes = false;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
