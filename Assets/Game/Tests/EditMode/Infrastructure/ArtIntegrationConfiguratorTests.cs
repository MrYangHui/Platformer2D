using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Utils;

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
        public void FennyFrameSheetsUseBottomCenterPivotsAndComparableWorldHeight()
        {
            Sprite[] idle = LoadSprites(
                "Assets/Game/Art/Characters/Player/FennyGolden_IdlePoses_Candidate_v003.png");
            Sprite[] run = LoadSprites(
                "Assets/Game/Art/Characters/Player/FennyGolden_RunPoses_Candidate_v003.png");

            Assert.That(idle, Has.Length.EqualTo(4));
            Assert.That(run, Has.Length.EqualTo(8));
            Assert.That(idle.All(sprite => sprite.pivot.y <= 0.01f), Is.True);
            Assert.That(run.All(sprite => sprite.pivot.y <= 0.01f), Is.True);
            Assert.That(idle.Average(sprite => sprite.bounds.size.y),
                Is.EqualTo(1.91f).Within(0.08f));
            Assert.That(run.Average(sprite => sprite.bounds.size.y),
                Is.EqualTo(1.91f).Within(0.08f));
        }

        [Test]
        public void FennyAirborneAndPrefabUseStablePresentationCalibration()
        {
            const string airbornePath =
                "Assets/Game/Art/Characters/Player/FennyGolden_Airborne_Candidate_v004.png";
            const string playerPath = "Assets/Game/Prefabs/Player/Player.prefab";

            Sprite airborne = AssetDatabase.LoadAssetAtPath<Sprite>(airbornePath);
            Assert.That(airborne, Is.Not.Null);
            Assert.That(airborne.pivot.y, Is.LessThanOrEqualTo(0.01f));
            Assert.That(airborne.bounds.size.y, Is.EqualTo(1.91f).Within(0.03f));

            GameObject root = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                Component animator = root.GetComponent("PlayerSpriteAnimator2D");
                Assert.That(animator, Is.Not.Null);
                SerializedObject serialized = new(animator);
                SerializedProperty offsets = serialized.FindProperty("runFrameOffsets");

                Assert.That(visual.localPosition, Is.EqualTo(new Vector3(0f, -0.95f, 0f)));
                Assert.That(serialized.FindProperty("runFramesPerSecond").floatValue, Is.EqualTo(16f));
                Assert.That(serialized.FindProperty("airborneFrame").objectReferenceValue, Is.SameAs(airborne));
                Assert.That(offsets.arraySize, Is.EqualTo(8));

                Vector2[] expected =
                {
                    new(0.009259f, 0f),
                    new(-0.046296f, 0f),
                    new(-0.159722f, 0f),
                    new(-0.328704f, 0f),
                    new(-0.041667f, 0f),
                    new(-0.106481f, 0f),
                    new(-0.215278f, 0f),
                    new(-0.349537f, 0f)
                };
                for (int index = 0; index < expected.Length; index++)
                {
                    Assert.That(offsets.GetArrayElementAtIndex(index).vector2Value,
                        Is.EqualTo(expected[index])
                            .Using(Vector2ComparerWithEqualsOperator.Instance));
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Sprite[] LoadSprites(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();
    }
}
