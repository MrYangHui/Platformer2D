using System.Collections;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SnowbreakFan.Tests.PlayMode
{
    public sealed class CameraConfigurationTests
    {
        [UnityTest]
        public IEnumerator LongLevelCameraUsesApprovedTwoDimensionalSettings()
        {
            yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);
            yield return null;

            Camera outputCamera = Camera.main;
            Assert.That(outputCamera, Is.Not.Null);
            Assert.That(outputCamera.orthographic, Is.True);
            Assert.That(outputCamera.GetComponent<CinemachineBrain>(), Is.Not.Null);

            CinemachineCamera virtualCamera = Object.FindFirstObjectByType<CinemachineCamera>();
            Assert.That(virtualCamera, Is.Not.Null);
            Assert.That(virtualCamera.Follow.name, Is.EqualTo("CameraTarget"));
            Assert.That(virtualCamera.Lens.ModeOverride, Is.EqualTo(LensSettings.OverrideModes.Orthographic));
            Assert.That(virtualCamera.Lens.OrthographicSize, Is.EqualTo(6.5f).Within(0.001f));
            Assert.That(virtualCamera.transform.rotation, Is.EqualTo(Quaternion.identity));

            CinemachinePositionComposer composer = virtualCamera.GetComponent<CinemachinePositionComposer>();
            Assert.That(composer, Is.Not.Null);
            Assert.That(composer.Damping.x, Is.EqualTo(0.20f).Within(0.001f));
            Assert.That(composer.Damping.y, Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(composer.Composition.DeadZone.Enabled, Is.True);
            Assert.That(composer.Composition.DeadZone.Size, Is.EqualTo(new Vector2(0.20f, 0.15f)));
            Assert.That(composer.Composition.DeadZoneRect.center, Is.EqualTo(new Vector2(0.50f, 0.55f)));
            Assert.That(composer.Lookahead.Enabled, Is.True);
            Assert.That(composer.Lookahead.Time, Is.EqualTo(0.15f).Within(0.001f));
            Assert.That(composer.Lookahead.Smoothing, Is.EqualTo(0.10f).Within(0.001f));

            CinemachineConfiner2D confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
            Assert.That(confiner, Is.Not.Null);
            Assert.That(confiner.BoundingShape2D, Is.TypeOf<CompositeCollider2D>());
            Assert.That(confiner.BoundingShape2D.bounds.min.x, Is.LessThanOrEqualTo(0f));
            Assert.That(confiner.BoundingShape2D.bounds.max.x, Is.EqualTo(280f).Within(0.01f));
        }
    }
}
