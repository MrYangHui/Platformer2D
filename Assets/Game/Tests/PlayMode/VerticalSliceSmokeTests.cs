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
            PlayerFramePresentation2D presentation = motor.GetComponent<PlayerFramePresentation2D>();
            Transform visual = motor.transform.Find("Visual");
            SpriteRenderer[] renderers = visual != null
                ? visual.GetComponentsInChildren<SpriteRenderer>()
                : new SpriteRenderer[0];

            Assert.That(motor.GetComponent<PlayerSpriteAnimator2D>(), Is.Null);
            Assert.That(presentation, Is.Not.Null);
            Assert.That(motor.GetComponent<PlayerRigPresentation2D>(), Is.Null);
            Assert.That(motor.transform.Find("FennyVisualRig"), Is.Null);
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.GetComponent<Animator>(), Is.Null);
            Assert.That(renderers, Has.Length.EqualTo(1));
            Assert.That(body.gravityScale, Is.EqualTo(4f));
            Assert.That(collider.size, Is.EqualTo(new Vector2(0.8f, 1.8f)));
            Assert.That(visual.localPosition, Is.EqualTo(new Vector3(0f, -1.1f, 0f))
                .Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(visual.localScale, Is.EqualTo(Vector3.one)
                .Using(Vector3ComparerWithEqualsOperator.Instance));
        }

        [UnityTest]
        public IEnumerator FennyFramesDriveRunAirborneAndPreserveFixedRoot()
        {
            yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);

            PlayerFramePresentation2D presentation =
                Object.FindFirstObjectByType<PlayerFramePresentation2D>();
            PlayerMotor2D motor = presentation.GetComponent<PlayerMotor2D>();
            Rigidbody2D body = presentation.GetComponent<Rigidbody2D>();
            Transform visual = presentation.transform.Find("Visual");
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();

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

            Assert.That(renderer.sprite.name, Does.StartWith("Fenny_Run_"));
            Assert.That(renderer.flipX, Is.False);
            Assert.That(visual.localPosition, Is.EqualTo(new Vector3(0f, -1.1f, 0f))
                .Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(visual.localScale, Is.EqualTo(Vector3.one)
                .Using(Vector3ComparerWithEqualsOperator.Instance));

            body.linearVelocity = new Vector2(-4f, 0f);
            yield return null;
            Assert.That(renderer.flipX, Is.True);

            body.linearVelocity = Vector2.zero;
            yield return null;
            Assert.That(renderer.sprite.name, Does.StartWith("Fenny_Idle_"));
            Assert.That(renderer.flipX, Is.True);

            SetMotorState(motor, PlayerMotionState.Rising);
            body.linearVelocity = new Vector2(0f, 0.2f);
            yield return null;
            Assert.That(renderer.sprite.name, Is.EqualTo("Fenny_Apex"));
            Assert.That(visual.localPosition, Is.EqualTo(new Vector3(0f, -1.1f, 0f))
                .Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(visual.localScale, Is.EqualTo(Vector3.one)
                .Using(Vector3ComparerWithEqualsOperator.Instance));
        }

        private static void SetMotorState(PlayerMotor2D motor, PlayerMotionState state)
        {
            typeof(PlayerMotor2D).GetProperty(nameof(PlayerMotor2D.State))
                .GetSetMethod(true)
                .Invoke(motor, new object[] { state });
        }
    }
}
