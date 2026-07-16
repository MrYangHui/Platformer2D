using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SnowbreakFan.Infrastructure
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private BootstrapSettings settings;

        private static bool activeInstance;
        private bool ownsActiveFlag;

        private void Awake()
        {
            if (activeInstance)
            {
                Destroy(gameObject);
                return;
            }

            activeInstance = true;
            ownsActiveFlag = true;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            if (settings == null)
            {
                Debug.LogError("GameBootstrap requires BootstrapSettings.", this);
                yield break;
            }

            foreach (string sceneName in settings.BuildLoadPlan())
            {
                string scenePath = $"Assets/Game/Scenes/{sceneName}.unity";
                if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
                {
                    Debug.LogError($"Scene is not enabled in Build Profiles: {sceneName}", this);
                    yield break;
                }

                if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                    yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
        }

        private void OnDestroy()
        {
            if (ownsActiveFlag)
                activeInstance = false;
        }
    }
}
