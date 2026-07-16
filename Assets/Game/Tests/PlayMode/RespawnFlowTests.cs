using System.Collections;
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
            Checkpoint2D checkpoint = Object.FindFirstObjectByType<Checkpoint2D>();
            Rigidbody2D body = target.GetComponent<Rigidbody2D>();

            target.transform.position = checkpoint.transform.position;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Vector2 expectedCheckpoint = new Vector2(5f, 1.7f);
            body.linearVelocity = new Vector2(4f, -8f);
            service.Respawn();
            Assert.That(Vector2.Distance(target.transform.position, expectedCheckpoint), Is.LessThan(0.001f));
            Assert.That(body.linearVelocity.sqrMagnitude, Is.LessThan(0.000001f));

            target.transform.position = new Vector3(0f, -5f, 0f);
            body.linearVelocity = new Vector2(0f, -5f);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(target.transform.position.x, Is.EqualTo(expectedCheckpoint.x).Within(0.01f));
            Assert.That(target.transform.position.y, Is.GreaterThan(1.5f));
        }
    }
}
