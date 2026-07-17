using System.Collections;
using System.Linq;
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
            PlayerRigPresentation2D presentation = motor.GetComponent<PlayerRigPresentation2D>();
            Transform visual = motor.transform.Find("FennyVisualRig");
            Animator animator = visual != null ? visual.GetComponent<Animator>() : null;

            Assert.That(motor.GetComponent<PlayerSpriteAnimator2D>(), Is.Null);
            Assert.That(presentation, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(visual.GetComponentsInChildren<SpriteRenderer>(), Has.Length.EqualTo(21));
            Assert.That(body.gravityScale, Is.EqualTo(4f));
            Assert.That(collider.size, Is.EqualTo(new Vector2(0.8f, 1.8f)));
            Assert.That(visual.Find("GroundContact").localPosition.y,
                Is.EqualTo(-1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator FennyRigDrivesRunAirborneAndPreservesFacingAtIdle()
        {
            yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);

            PlayerRigPresentation2D presentation =
                Object.FindFirstObjectByType<PlayerRigPresentation2D>();
            PlayerMotor2D motor = presentation.GetComponent<PlayerMotor2D>();
            Rigidbody2D body = presentation.GetComponent<Rigidbody2D>();
            Transform visual = presentation.transform.Find("FennyVisualRig");
            Animator animator = visual.GetComponent<Animator>();

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
            yield return null;

            Assert.That(animator.GetBool("IsRunning"), Is.True);
            Assert.That(animator.GetBool("IsAirborne"), Is.False);
            Assert.That(animator.GetFloat("RunSpeed"), Is.EqualTo(4f / 6f).Within(0.01f));
            Assert.That(visual.localScale, Is.EqualTo(Vector3.one)
                .Using(Vector3ComparerWithEqualsOperator.Instance));

            body.linearVelocity = new Vector2(-4f, 0f);
            yield return null;
            Assert.That(visual.localScale.x, Is.EqualTo(-1f));
            Assert.That(visual.localScale.y, Is.EqualTo(1f));

            body.linearVelocity = Vector2.zero;
            yield return null;
            Assert.That(animator.GetBool("IsRunning"), Is.False);
            Assert.That(visual.localScale.x, Is.EqualTo(-1f));
            Assert.That(visual.GetComponentsInChildren<Transform>()
                    .Where(item => item != visual)
                    .All(item => item.localScale == Vector3.one),
                Is.True);

            typeof(PlayerMotor2D).GetProperty(nameof(PlayerMotor2D.State))
                .GetSetMethod(true)
                .Invoke(motor, new object[] { PlayerMotionState.Rising });
            body.linearVelocity = new Vector2(0f, 5f);
            yield return null;
            Assert.That(animator.GetBool("IsAirborne"), Is.True);
            Assert.That(animator.GetBool("IsRunning"), Is.False);
        }
    }
}
