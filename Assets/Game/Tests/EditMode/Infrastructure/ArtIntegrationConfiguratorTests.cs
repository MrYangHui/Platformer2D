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

        private static Sprite[] LoadSprites(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();
    }
}
