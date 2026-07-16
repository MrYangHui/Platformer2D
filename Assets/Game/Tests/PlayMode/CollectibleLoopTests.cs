using System.Collections;
using System.Linq;
using NUnit.Framework;
using SnowbreakFan.Collectibles;
using SnowbreakFan.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SnowbreakFan.Tests.PlayMode
{
    public sealed class CollectibleLoopTests
    {
        [UnityTest]
        public IEnumerator UniqueSamplesUpdateHudAndEndpointShowsCompletion()
        {
            yield return SceneManager.LoadSceneAsync("00_Bootstrap", LoadSceneMode.Single);

            float timeout = 10f;
            while (timeout > 0f &&
                   (!SceneManager.GetSceneByName("10_Level_Prototype").isLoaded ||
                    !SceneManager.GetSceneByName("20_UI_Gameplay").isLoaded))
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            LevelSessionController session = Object.FindFirstObjectByType<LevelSessionController>();
            Assert.That(session.TryCollect("sample-01"), Is.True);
            Assert.That(session.TryCollect("sample-01"), Is.False);
            Assert.That(session.TryCollect("sample-02"), Is.True);
            Assert.That(session.TryCollect("sample-03"), Is.True);
            yield return null;

            GameObject countObject = GameObject.Find("SampleCount");
            Assert.That(countObject, Is.Not.Null);
            Component textComponent = countObject.GetComponent("TextMeshProUGUI");
            string renderedText = (string)textComponent.GetType().GetProperty("text").GetValue(textComponent);
            Assert.That(renderedText, Is.EqualTo("样本 3/3"));

            PlayerRespawnTarget player = Object.FindFirstObjectByType<PlayerRespawnTarget>();
            GameObject endpoint = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(transform => transform.name == "LevelEnd").gameObject;
            player.transform.position = endpoint.transform.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            GameObject panel = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(transform => transform.name == "LevelCompletedPanel").gameObject;
            Assert.That(panel.activeSelf, Is.True);
        }
    }
}
