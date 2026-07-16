using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SnowbreakFan.Level;
using SnowbreakFan.Player;
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
            float topVerticalPlatformSurface = verticalPlatforms.Cast<Transform>()
                .Max(item => item.GetComponent<BoxCollider2D>().bounds.max.y);
            Assert.That(
                checkpoints.Max(checkpoint => checkpoint.transform.position.y),
                Is.EqualTo(topVerticalPlatformSurface + 1.5f).Within(0.05f));

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

        [UnityTest]
        public IEnumerator Chunk02OneWayChainFitsConfiguredJumpEnvelope()
        {
            yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);
            Physics2D.SyncTransforms();

            LevelChunk2D chunk = Object.FindObjectsByType<LevelChunk2D>(FindObjectsSortMode.None)
                .Single(item => item.name == "Chunk_02_Gaps");
            PlayerMovementConfig config = Resources.FindObjectsOfTypeAll<PlayerMovementConfig>()
                .Single(item => item.name == "PlayerMovementConfig");

            BoxCollider2D[] platforms = chunk.transform.Find("Platforms")
                .Cast<Transform>()
                .Select(item => item.GetComponent<BoxCollider2D>())
                .Where(item => item != null)
                .ToArray();

            int oneWayLayer = LayerMask.NameToLayer("OneWayPlatform");
            int groundLayer = LayerMask.NameToLayer("Ground");
            BoxCollider2D[] oneWayPlatforms = platforms
                .Where(item => item.gameObject.layer == oneWayLayer)
                .OrderBy(item => item.bounds.center.x)
                .ToArray();
            BoxCollider2D[] groundPlatforms = platforms
                .Where(item => item.gameObject.layer == groundLayer)
                .ToArray();

            Assert.That(oneWayPlatforms, Has.Length.EqualTo(3));

            BoxCollider2D sourceGround = groundPlatforms
                .Where(item => item.bounds.max.x <= oneWayPlatforms[0].bounds.min.x)
                .OrderByDescending(item => item.bounds.max.x)
                .First();
            BoxCollider2D destinationGround = groundPlatforms
                .Where(item => item.bounds.min.x >= oneWayPlatforms[^1].bounds.max.x)
                .OrderBy(item => item.bounds.min.x)
                .First();

            var route = new List<BoxCollider2D> { sourceGround };
            route.AddRange(oneWayPlatforms);
            route.Add(destinationGround);

            float risingGravity = Mathf.Abs(Physics2D.gravity.y) * config.GravityScale;
            float fallingGravity = risingGravity * config.FallGravityMultiplier;
            float maximumRise = config.JumpSpeed * config.JumpSpeed / (2f * risingGravity);
            float timeToApex = config.JumpSpeed / risingGravity;
            const float horizontalSafetyFactor = 0.85f;

            for (int index = 0; index < route.Count - 1; index++)
            {
                BoxCollider2D source = route[index];
                BoxCollider2D destination = route[index + 1];
                float heightDelta = destination.bounds.max.y - source.bounds.max.y;

                Assert.That(heightDelta, Is.LessThanOrEqualTo(maximumRise),
                    $"Transition at x={source.bounds.center.x:0.##} -> {destination.bounds.center.x:0.##} " +
                    $"rises {heightDelta:0.##}, above the configured maximum {maximumRise:0.##}.");

                float fallingTime = Mathf.Sqrt(2f * (maximumRise - heightDelta) / fallingGravity);
                float safeHorizontalRange = config.MaxSpeed * (timeToApex + fallingTime) * horizontalSafetyFactor;
                float requiredHorizontalRange = Mathf.Max(0f, destination.bounds.min.x - source.bounds.max.x);

                Assert.That(requiredHorizontalRange, Is.LessThanOrEqualTo(safeHorizontalRange),
                    $"Transition at x={source.bounds.center.x:0.##} -> {destination.bounds.center.x:0.##} " +
                    $"needs {requiredHorizontalRange:0.##} horizontal units, above the safe configured range " +
                    $"{safeHorizontalRange:0.##}.");
            }
        }

        [UnityTest]
        public IEnumerator Chunk03VerticalRouteFitsConfiguredJumpEnvelope()
        {
            yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);
            Physics2D.SyncTransforms();

            LevelChunk2D chunk = Object.FindObjectsByType<LevelChunk2D>(FindObjectsSortMode.None)
                .Single(item => item.name == "Chunk_03_Vertical");
            PlayerMovementConfig config = Resources.FindObjectsOfTypeAll<PlayerMovementConfig>()
                .Single(item => item.name == "PlayerMovementConfig");
            BoxCollider2D[] route = chunk.transform.Find("Platforms")
                .Cast<Transform>()
                .Select(item => item.GetComponent<BoxCollider2D>())
                .Where(item => item != null)
                .OrderBy(item => item.bounds.center.x)
                .ToArray();

            Assert.That(route, Has.Length.EqualTo(7));

            float risingGravity = Mathf.Abs(Physics2D.gravity.y) * config.GravityScale;
            float fallingGravity = risingGravity * config.FallGravityMultiplier;
            float maximumRise = config.JumpSpeed * config.JumpSpeed / (2f * risingGravity);
            float timeToApex = config.JumpSpeed / risingGravity;
            const float horizontalSafetyFactor = 0.85f;

            for (int index = 0; index < route.Length - 1; index++)
            {
                BoxCollider2D source = route[index];
                BoxCollider2D destination = route[index + 1];
                float heightDelta = destination.bounds.max.y - source.bounds.max.y;

                Assert.That(heightDelta, Is.LessThanOrEqualTo(maximumRise),
                    $"Vertical transition at x={source.bounds.center.x:0.##} -> {destination.bounds.center.x:0.##} " +
                    $"rises {heightDelta:0.##}, above the configured maximum {maximumRise:0.##}.");

                float fallingTime = Mathf.Sqrt(2f * (maximumRise - heightDelta) / fallingGravity);
                float safeHorizontalRange = config.MaxSpeed * (timeToApex + fallingTime) * horizontalSafetyFactor;
                float requiredHorizontalRange = Mathf.Max(0f, destination.bounds.min.x - source.bounds.max.x);

                Assert.That(requiredHorizontalRange, Is.LessThanOrEqualTo(safeHorizontalRange),
                    $"Vertical transition at x={source.bounds.center.x:0.##} -> {destination.bounds.center.x:0.##} " +
                    $"needs {requiredHorizontalRange:0.##} horizontal units, above the safe configured range " +
                    $"{safeHorizontalRange:0.##}.");
            }
        }

        [UnityTest]
        public IEnumerator Chunk04MainAndRecoveryRoutesFitConfiguredJumpEnvelope()
        {
            yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);
            Physics2D.SyncTransforms();

            LevelChunk2D chunk = Object.FindObjectsByType<LevelChunk2D>(FindObjectsSortMode.None)
                .Single(item => item.name == "Chunk_04_Recovery");
            PlayerMovementConfig config = Resources.FindObjectsOfTypeAll<PlayerMovementConfig>()
                .Single(item => item.name == "PlayerMovementConfig");
            BoxCollider2D[] platforms = chunk.transform.Find("Platforms")
                .Cast<Transform>()
                .Select(item => item.GetComponent<BoxCollider2D>())
                .Where(item => item != null)
                .ToArray();
            BoxCollider2D[] mainRoute = platforms
                .Where(item => item.bounds.center.y > -2.5f)
                .OrderBy(item => item.bounds.center.x)
                .ToArray();
            BoxCollider2D[] recoveryRoute = platforms
                .Where(item => item.bounds.center.y <= -2.5f)
                .OrderBy(item => item.bounds.center.x)
                .ToArray();

            Assert.That(mainRoute, Has.Length.EqualTo(6));
            Assert.That(recoveryRoute, Has.Length.EqualTo(2));
            AssertRouteFitsConfiguredJumpEnvelope(mainRoute, config, "Chunk 04 main route");
            AssertRouteFitsConfiguredJumpEnvelope(recoveryRoute, config, "Chunk 04 recovery floor");
            AssertTransitionFitsConfiguredJumpEnvelope(
                recoveryRoute[^1],
                mainRoute[^2],
                config,
                "Chunk 04 recovery rejoin");
        }

        [UnityTest]
        public IEnumerator Chunk05FinalRouteFitsConfiguredJumpEnvelope()
        {
            yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);
            Physics2D.SyncTransforms();

            LevelChunk2D chunk = Object.FindObjectsByType<LevelChunk2D>(FindObjectsSortMode.None)
                .Single(item => item.name == "Chunk_05_Final");
            PlayerMovementConfig config = Resources.FindObjectsOfTypeAll<PlayerMovementConfig>()
                .Single(item => item.name == "PlayerMovementConfig");
            BoxCollider2D[] route = chunk.transform.Find("Platforms")
                .Cast<Transform>()
                .Select(item => item.GetComponent<BoxCollider2D>())
                .Where(item => item != null)
                .OrderBy(item => item.bounds.center.x)
                .ToArray();

            Assert.That(route, Has.Length.EqualTo(6));
            AssertRouteFitsConfiguredJumpEnvelope(route, config, "Chunk 05 final route");
        }

        private static void AssertRouteFitsConfiguredJumpEnvelope(
            IReadOnlyList<BoxCollider2D> route,
            PlayerMovementConfig config,
            string routeName)
        {
            for (int index = 0; index < route.Count - 1; index++)
            {
                AssertTransitionFitsConfiguredJumpEnvelope(route[index], route[index + 1], config, routeName);
            }
        }

        private static void AssertTransitionFitsConfiguredJumpEnvelope(
            BoxCollider2D source,
            BoxCollider2D destination,
            PlayerMovementConfig config,
            string routeName)
        {
            float risingGravity = Mathf.Abs(Physics2D.gravity.y) * config.GravityScale;
            float fallingGravity = risingGravity * config.FallGravityMultiplier;
            float maximumRise = config.JumpSpeed * config.JumpSpeed / (2f * risingGravity);
            float heightDelta = destination.bounds.max.y - source.bounds.max.y;

            Assert.That(heightDelta, Is.LessThanOrEqualTo(maximumRise),
                $"{routeName} transition at x={source.bounds.center.x:0.##} -> {destination.bounds.center.x:0.##} " +
                $"rises {heightDelta:0.##}, above the configured maximum {maximumRise:0.##}.");

            float timeToApex = config.JumpSpeed / risingGravity;
            float fallingTime = Mathf.Sqrt(2f * (maximumRise - heightDelta) / fallingGravity);
            float safeHorizontalRange = config.MaxSpeed * (timeToApex + fallingTime) * 0.85f;
            float requiredHorizontalRange = Mathf.Max(0f, destination.bounds.min.x - source.bounds.max.x);

            Assert.That(requiredHorizontalRange, Is.LessThanOrEqualTo(safeHorizontalRange),
                $"{routeName} transition at x={source.bounds.center.x:0.##} -> {destination.bounds.center.x:0.##} " +
                $"needs {requiredHorizontalRange:0.##} horizontal units, above the safe configured range " +
                $"{safeHorizontalRange:0.##}.");
        }
    }
}
