using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SnowbreakFan.Infrastructure.Tests
{
    public sealed class ArtIntegrationConfiguratorTests
    {
        [TestCase("Platform_Short", "Platform_Short_v001.png", 3f)]
        [TestCase("Platform_Medium", "Platform_Medium_v001.png", 6f)]
        [TestCase("Platform_Long", "Platform_Long_v001.png", 12f)]
        public void PlatformPrefabUsesCandidateSpriteAndPreservesColliderSize(
            string prefabName,
            string textureName,
            float expectedWidth)
        {
            string prefabPath = $"Assets/Game/Prefabs/Gameplay/{prefabName}.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
                SpriteRenderer renderer = root.GetComponentInChildren<SpriteRenderer>();

                Assert.That(collider, Is.Not.Null);
                Assert.That(renderer, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(renderer.sprite), Does.EndWith(textureName));
                Assert.That(renderer.size, Is.EqualTo(new Vector2(expectedWidth, 1f)));
                Assert.That(renderer.color, Is.EqualTo(Color.white));
                Assert.That(renderer.sortingLayerName, Is.EqualTo("Terrain"));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void PlayerPrefabUsesWholeFramesWithGroundOverlapAndStablePhysics()
        {
            const string playerPath = "Assets/Game/Prefabs/Player/Player.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                Component driver = root.GetComponent("PlayerFramePresentation2D");
                SpriteRenderer renderer = visual != null
                    ? visual.GetComponent<SpriteRenderer>()
                    : null;
                CapsuleCollider2D collider = root.GetComponent<CapsuleCollider2D>();

                Assert.That(root.transform.Find("FennyVisualRig"), Is.Null);
                Assert.That(root.GetComponent("PlayerSpriteAnimator2D"), Is.Null);
                Assert.That(root.GetComponent("PlayerRigPresentation2D"), Is.Null);
                Assert.That(driver, Is.Not.Null);
                Assert.That(visual, Is.Not.Null);
                Assert.That(visual.localPosition, Is.EqualTo(new Vector3(0f, -1.1f, 0f)));
                Assert.That(visual.localScale, Is.EqualTo(Vector3.one));
                Assert.That(visual.GetComponent<Animator>(), Is.Null);
                Assert.That(renderer, Is.Not.Null);
                Assert.That(renderer.sprite.name, Is.EqualTo("Fenny_Idle_00"));
                Assert.That(renderer.sortingLayerName, Is.EqualTo("Player"));
                Assert.That(collider.size, Is.EqualTo(new Vector2(0.8f, 1.8f)));

                float colliderBottom = collider.offset.y - collider.size.y * 0.5f;
                Assert.That(visual.localPosition.y,
                    Is.EqualTo(colliderBottom - 0.2f).Within(0.001f));

                SerializedObject serialized = new(driver);
                Assert.That(serialized.FindProperty("profile").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(serialized.FindProperty("targetRenderer").objectReferenceValue,
                    Is.SameAs(renderer));
                Assert.That(serialized.FindProperty("visualRoot").objectReferenceValue,
                    Is.SameAs(visual));
                Assert.That(serialized.FindProperty("body").objectReferenceValue,
                    Is.SameAs(root.GetComponent<Rigidbody2D>()));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
