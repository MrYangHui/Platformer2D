using System.Collections;
using NUnit.Framework;
using SnowbreakFan.Player;
using SnowbreakFan.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace SnowbreakFan.Tests.PlayMode
{
    public sealed class VerticalSliceSmokeTests
    {
        [UnityTest]
        public IEnumerator BootstrapLoadsLevelAndUiExactlyOnce()
        {
            yield return SceneManager.LoadSceneAsync("00_Bootstrap", LoadSceneMode.Single);

            float timeout = 10f;
            while (timeout > 0f &&
                   (!SceneManager.GetSceneByName("10_Level_Prototype").isLoaded ||
                    !SceneManager.GetSceneByName("20_UI_Gameplay").isLoaded))
            {
                timeout -= UnityEngine.Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(SceneManager.GetSceneByName("10_Level_Prototype").isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("20_UI_Gameplay").isLoaded, Is.True);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator PlayerUsesConfiguredFennyPresentationWithoutPhysicsChanges()
        {
            yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);

            PlayerMotor2D motor = Object.FindFirstObjectByType<PlayerMotor2D>();
            Rigidbody2D body = motor.GetComponent<Rigidbody2D>();
            CapsuleCollider2D collider = motor.GetComponent<CapsuleCollider2D>();
            SpriteRenderer renderer = motor.GetComponentInChildren<SpriteRenderer>();

            Assert.That(motor.GetComponent("PlayerSpriteAnimator2D"), Is.Not.Null);
            Assert.That(renderer.sprite.name, Does.StartWith("FennyGolden_IdlePoses"));
            Assert.That(renderer.sortingLayerName, Is.EqualTo("Player"));
            Assert.That(body.gravityScale, Is.EqualTo(4f));
            Assert.That(collider.size, Is.EqualTo(new Vector2(0.8f, 1.8f)));
        }

        [UnityTest]
        public IEnumerator FennyPresentationCyclesRunAndPreservesFacingAtIdle()
        {
            yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);

            PlayerSpriteAnimator2D animator = Object.FindFirstObjectByType<PlayerSpriteAnimator2D>();
            PlayerMotor2D motor = animator.GetComponent<PlayerMotor2D>();
            Rigidbody2D body = animator.GetComponent<Rigidbody2D>();
            SpriteRenderer renderer = animator.GetComponentInChildren<SpriteRenderer>();

            float timeout = 2f;
            while (motor.State != PlayerMotionState.Grounded && timeout > 0f)
            {
                timeout -= Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            Assert.That(motor.State, Is.EqualTo(PlayerMotionState.Grounded));

            motor.enabled = false;
            body.gravityScale = 0f;
            body.linearVelocity = new Vector2(4f, 0f);
            Sprite idleFrame = renderer.sprite;

            yield return new WaitForSeconds(0.2f);

            Assert.That(renderer.sprite.name, Does.StartWith("FennyGolden_RunPoses"));
            Assert.That(renderer.sprite, Is.Not.SameAs(idleFrame));
            Assert.That(renderer.flipX, Is.False);
            Assert.That(renderer.transform.localPosition.x, Is.LessThan(-0.1f));

            body.linearVelocity = new Vector2(-4f, 0f);
            yield return null;
            Assert.That(renderer.flipX, Is.True);
            Assert.That(renderer.transform.localPosition.x, Is.GreaterThan(0.1f));

            body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.3f);
            Assert.That(renderer.sprite.name, Does.StartWith("FennyGolden_IdlePoses"));
            Assert.That(renderer.flipX, Is.True);
            Assert.That(renderer.transform.localPosition,
                Is.EqualTo(new Vector3(0f, -0.95f, 0f))
                    .Using(Vector3ComparerWithEqualsOperator.Instance));

            typeof(PlayerMotor2D).GetProperty(nameof(PlayerMotor2D.State))
                .GetSetMethod(true)
                .Invoke(motor, new object[] { PlayerMotionState.Rising });
            body.linearVelocity = new Vector2(0f, 5f);
            yield return null;
            Assert.That(renderer.sprite.name,
                Does.StartWith("FennyGolden_Airborne_Candidate_v004"));
        }
    }
}
