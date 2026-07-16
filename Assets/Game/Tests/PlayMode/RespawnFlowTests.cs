using System.Collections;
using System.Linq;
using NUnit.Framework;
using SnowbreakFan.Level;
using SnowbreakFan.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SnowbreakFan.Tests.PlayMode
{
    public sealed class RespawnFlowTests
    {
        [UnityTest]
        public IEnumerator CheckpointAndKillZoneReturnPlayerToSafePoint()
        {
            yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();

            PlayerRespawnTarget target = Object.FindFirstObjectByType<PlayerRespawnTarget>();
            RespawnService service = Object.FindFirstObjectByType<RespawnService>();
            Checkpoint2D checkpoint = Object.FindObjectsByType<Checkpoint2D>(FindObjectsSortMode.None)
                .First(item => item.name == "RespawnCheckpoint");
            FallKillZone2D killZone = Object.FindFirstObjectByType<FallKillZone2D>();
            Rigidbody2D body = target.GetComponent<Rigidbody2D>();

            target.transform.position = checkpoint.transform.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Vector2 expectedCheckpoint = checkpoint.transform.Find("SpawnPoint").position;
            body.linearVelocity = new Vector2(4f, -8f);
            service.Respawn();
            Assert.That(Vector2.Distance(target.transform.position, expectedCheckpoint), Is.LessThan(0.001f));
            Assert.That(body.linearVelocity.sqrMagnitude, Is.LessThan(0.000001f));

            target.transform.position = killZone.transform.position;
            body.linearVelocity = new Vector2(0f, -5f);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(Vector2.Distance(target.transform.position, expectedCheckpoint), Is.LessThan(0.01f));
        }
    }
}
