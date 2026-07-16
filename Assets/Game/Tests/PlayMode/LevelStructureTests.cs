using System.Collections;
using System.Linq;
using NUnit.Framework;
using SnowbreakFan.Level;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SnowbreakFan.Tests.PlayMode
{
    public sealed class LevelStructureTests
    {
        [UnityTest]
        public IEnumerator PrototypeContainsFiveConfiguredChunksAcross280Units()
        {
            yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);

            LevelChunk2D[] chunks = Object.FindObjectsByType<LevelChunk2D>(FindObjectsSortMode.None)
                .OrderBy(chunk => chunk.transform.position.x)
                .ToArray();

            Assert.That(chunks.Select(chunk => chunk.name), Is.EqualTo(new[]
            {
                "Chunk_01_Tutorial",
                "Chunk_02_Gaps",
                "Chunk_03_Vertical",
                "Chunk_04_Recovery",
                "Chunk_05_Final"
            }));

            foreach (LevelChunk2D chunk in chunks)
            {
                Assert.That(chunk.ChunkId, Is.Not.Empty);
                Assert.That(chunk.GameplayTilemap, Is.Not.Null);
                Assert.That(chunk.TerrainArtTilemap, Is.Not.Null);
                Assert.That(chunk.CameraBoundary, Is.Not.Null);
                Assert.That(chunk.DefaultSpawn, Is.Not.Null);
                Assert.That(chunk.GameplayTilemap.GetComponent<TilemapCollider2D>(), Is.Not.Null);
                Assert.That(chunk.GameplayTilemap.GetComponent<CompositeCollider2D>(), Is.Not.Null);
                Assert.That(chunk.GameplayTilemap.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Static));
                Assert.That(chunk.TerrainArtTilemap.GetComponent<Collider2D>(), Is.Null);
                Assert.That(chunk.transform.Find("Platforms"), Is.Not.Null);
                Assert.That(chunk.transform.Find("Decorations"), Is.Not.Null);
                Assert.That(chunk.transform.Find("ParallaxAnchors"), Is.Not.Null);
            }

            float routeStart = chunks.Min(chunk => chunk.CameraBoundary.bounds.min.x);
            float routeEnd = chunks.Max(chunk => chunk.CameraBoundary.bounds.max.x);
            Assert.That(routeEnd - routeStart, Is.GreaterThanOrEqualTo(280f));

            Transform verticalPlatforms = chunks[2].transform.Find("Platforms");
            Assert.That(verticalPlatforms.childCount, Is.GreaterThanOrEqualTo(6));
            float verticalGain = verticalPlatforms.Cast<Transform>().Max(item => item.position.y) -
                                 verticalPlatforms.Cast<Transform>().Min(item => item.position.y);
            Assert.That(verticalGain, Is.InRange(10f, 14f));

            Checkpoint2D[] checkpoints = Object.FindObjectsByType<Checkpoint2D>(FindObjectsSortMode.None);
            Assert.That(checkpoints, Has.Length.GreaterThanOrEqualTo(2));
            Assert.That(checkpoints.Max(checkpoint => checkpoint.transform.position.y), Is.GreaterThanOrEqualTo(13f));

            int playerLayer = LayerMask.NameToLayer("Player");
            int groundLayer = LayerMask.NameToLayer("Ground");
            int oneWayLayer = LayerMask.NameToLayer("OneWayPlatform");
            Assert.That(LayerMask.NameToLayer("GameplayTrigger"), Is.GreaterThanOrEqualTo(0));
            Assert.That(Physics2D.GetIgnoreLayerCollision(playerLayer, groundLayer), Is.False);
            Assert.That(Physics2D.GetIgnoreLayerCollision(playerLayer, oneWayLayer), Is.False);

            string[] expectedSortingLayers =
            {
                "BackgroundFar", "BackgroundMid", "BackgroundNear", "Terrain",
                "Gameplay", "Player", "Foreground", "UIWorld"
            };
            Assert.That(SortingLayer.layers.Select(layer => layer.name).Skip(1), Is.EqualTo(expectedSortingLayers));
        }

        [UnityTest]
        public IEnumerator EveryPlatformVisualMatchesItsCollisionBounds()
        {
            yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);

            LevelChunk2D[] chunks = Object.FindObjectsByType<LevelChunk2D>(FindObjectsSortMode.None);
            foreach (LevelChunk2D chunk in chunks)
            {
                foreach (Transform platform in chunk.transform.Find("Platforms"))
                {
                    BoxCollider2D collider = platform.GetComponent<BoxCollider2D>();
                    SpriteRenderer renderer = platform.GetComponentInChildren<SpriteRenderer>();
                    Assert.That(collider, Is.Not.Null, $"{platform.name} is missing its collider.");
                    Assert.That(renderer, Is.Not.Null, $"{platform.name} is missing its visual.");
                    Assert.That(renderer.bounds.size.x, Is.EqualTo(collider.bounds.size.x).Within(0.01f),
                        $"{platform.name} visual width must match its collision width.");
                    Assert.That(renderer.bounds.size.y, Is.EqualTo(collider.bounds.size.y).Within(0.01f),
                        $"{platform.name} visual height must match its collision height.");
                }
            }
        }
    }
}
