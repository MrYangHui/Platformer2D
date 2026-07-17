using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SnowbreakFan.Infrastructure.Editor;
using SnowbreakFan.Player;
using SnowbreakFan.Presentation;
using UnityEditor;
using UnityEngine;

namespace SnowbreakFan.Infrastructure.Tests
{
    public sealed class CharacterFramePipelineTests
    {
        private const string AtlasPath =
            "Assets/Game/Art/Characters/Player/FennyGolden_Frames_v006.png";
        private const string ProfilePath =
            "Assets/Game/Config/Characters/FennyGoldenPresentation.asset";
        private const string PlayerPrefabPath =
            "Assets/Game/Prefabs/Player/Player.prefab";

        private readonly List<Object> cleanup = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in cleanup.Where(item => item != null))
                Object.DestroyImmediate(item);
            cleanup.Clear();
        }

        [Test]
        public void ProfileRequiresEveryCoreStateAndPositiveTiming()
        {
            CharacterPresentationProfile profile = CreateValidProfile();
            Assert.That(profile.TryValidate(out string error), Is.True, error);

            SerializedObject serialized = new(profile);
            serialized.FindProperty("fallingFrame").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(profile.TryValidate(out error), Is.False);
            Assert.That(error, Does.Contain("Falling"));
        }

        [Test]
        public void ProfileRequiresCompleteFrameArrays()
        {
            CharacterPresentationProfile profile = CreateValidProfile();
            SerializedObject serialized = new(profile);
            serialized.FindProperty("runFrames").arraySize = 7;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(profile.TryValidate(out string error), Is.False);
            Assert.That(error, Does.Contain("Run"));
        }

        [Test]
        public void ProfileRejectsInvalidTimingScaleAndRootCoordinates()
        {
            CharacterPresentationProfile profile = CreateValidProfile();
            SerializedObject serialized = new(profile);
            serialized.FindProperty("runFramesPerSecond").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(profile.TryValidate(out string error), Is.False);
            Assert.That(error, Does.Contain("timing"));

            serialized.FindProperty("runFramesPerSecond").floatValue = 16f;
            serialized.FindProperty("visualScale").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(profile.TryValidate(out error), Is.False);
            Assert.That(error, Does.Contain("scale"));

            serialized.FindProperty("visualScale").floatValue = 1f;
            serialized.FindProperty("visualRootLocalPosition").vector3Value =
                new Vector3(float.NaN, -1.1f, 0f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(profile.TryValidate(out error), Is.False);
            Assert.That(error, Does.Contain("position"));
        }

        [Test]
        public void ProfileExposesReadOnlyRuntimeValues()
        {
            CharacterPresentationProfile profile = CreateValidProfile();

            Assert.That(profile.IdleFrames.Count, Is.EqualTo(2));
            Assert.That(profile.RunFrames.Count, Is.EqualTo(8));
            Assert.That(profile.IdleFramesPerSecond, Is.EqualTo(4f));
            Assert.That(profile.RunFramesPerSecond, Is.EqualTo(16f));
            Assert.That(profile.MovementThreshold, Is.EqualTo(0.1f));
            Assert.That(profile.ReferenceRunSpeed, Is.EqualTo(6f));
            Assert.That(profile.ApexVelocityThreshold, Is.EqualTo(0.75f));
            Assert.That(profile.VisualRootLocalPosition, Is.EqualTo(new Vector3(0f, -1.1f, 0f)));
            Assert.That(profile.VisualScale, Is.EqualTo(1f));
        }

        [Test]
        public void ProfileHasNoPerFrameTransformCorrectionFields()
        {
            string[] forbidden = { "runFrameOffsets", "frameScales", "airborneOffset" };
            Assert.That(forbidden.Any(name =>
                typeof(CharacterPresentationProfile).GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.NonPublic) != null), Is.False);
        }

        [Test]
        public void AdapterExposesOnlyFixedPresentationDependencies()
        {
            string[] required = { "profile", "targetRenderer", "visualRoot", "body", "motor" };
            string[] forbidden = { "runFrameOffsets", "frameScales", "airborneOffset" };
            foreach (string name in required)
            {
                Assert.That(typeof(PlayerFramePresentation2D).GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null, name);
            }
            Assert.That(forbidden.Any(name => typeof(PlayerFramePresentation2D).GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic) != null), Is.False);
        }

        [Test]
        public void AdapterSelectsRunAndApexFramesWithoutPerFrameTransformWrites()
        {
            GameObject root = new("Player");
            cleanup.Add(root);
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            PlayerMotor2D motor = root.AddComponent<PlayerMotor2D>();
            motor.enabled = false;
            GameObject visualObject = new("Visual");
            visualObject.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            PlayerFramePresentation2D adapter = root.AddComponent<PlayerFramePresentation2D>();
            CharacterPresentationProfile profile = CreateValidProfile();
            SerializedObject serialized = new(adapter);
            serialized.FindProperty("profile").objectReferenceValue = profile;
            serialized.FindProperty("targetRenderer").objectReferenceValue = renderer;
            serialized.FindProperty("visualRoot").objectReferenceValue = visualObject.transform;
            serialized.FindProperty("body").objectReferenceValue = body;
            serialized.FindProperty("motor").objectReferenceValue = motor;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            InvokePrivate(adapter, "Awake");
            Vector3 fixedPosition = visualObject.transform.localPosition;
            Vector3 fixedScale = visualObject.transform.localScale;
            SetMotorState(motor, PlayerMotionState.Grounded);
            body.linearVelocity = new Vector2(4f, 0f);
            InvokePrivate(adapter, "Update");
            Assert.That(renderer.sprite.name, Does.StartWith("Run_"));
            Assert.That(renderer.flipX, Is.False);

            SetMotorState(motor, PlayerMotionState.Rising);
            body.linearVelocity = new Vector2(-0.2f, 0.2f);
            InvokePrivate(adapter, "Update");
            Assert.That(renderer.sprite.name, Is.EqualTo("Apex"));
            Assert.That(renderer.flipX, Is.True);
            Assert.That(visualObject.transform.localPosition, Is.EqualTo(fixedPosition));
            Assert.That(visualObject.transform.localScale, Is.EqualTo(fixedScale));
        }

        [Test]
        public void ConfiguratorCreatesSemanticAtlasProfileAndStablePlayerPresentation()
        {
            FennyFrameConfigurator.Configure();

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();
            Assert.That(sprites, Has.Length.EqualTo(15));
            Assert.That(sprites.All(sprite => sprite.rect.size == new Vector2(768f, 1024f)),
                Is.True);
            Assert.That(sprites.All(sprite => sprite.pixelsPerUnit == 480f), Is.True);
            Assert.That(sprites.All(sprite => sprite.pivot == new Vector2(384f, 0f)), Is.True);

            CharacterPresentationProfile profile =
                AssetDatabase.LoadAssetAtPath<CharacterPresentationProfile>(ProfilePath);
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.TryValidate(out string error), Is.True, error);
            Assert.That(profile.IdleFrames.Count, Is.EqualTo(4));
            Assert.That(profile.RunFrames.Count, Is.EqualTo(8));
            Assert.That(profile.RisingFrame.name, Is.EqualTo("Fenny_Rising"));
            Assert.That(profile.ApexFrame.name, Is.EqualTo("Fenny_Apex"));
            Assert.That(profile.FallingFrame.name, Is.EqualTo("Fenny_Falling"));
            Assert.That(profile.IdleFramesPerSecond, Is.EqualTo(4f));
            Assert.That(profile.RunFramesPerSecond, Is.EqualTo(16f));
            Assert.That(profile.ReferenceRunSpeed, Is.EqualTo(6f));
            Assert.That(profile.VisualRootLocalPosition.y, Is.EqualTo(-1.1f));

            GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform visual = player.transform.Find("Visual");
                Assert.That(player.transform.Find("FennyVisualRig"), Is.Null);
                Assert.That(player.GetComponent<PlayerRigPresentation2D>(), Is.Null);
                Assert.That(player.GetComponent<PlayerFramePresentation2D>(), Is.Not.Null);
                Assert.That(visual, Is.Not.Null);
                Assert.That(visual.GetComponentsInChildren<SpriteRenderer>(true),
                    Has.Length.EqualTo(1));
                Assert.That(visual.localPosition, Is.EqualTo(new Vector3(0f, -1.1f, 0f)));
                Assert.That(visual.localScale, Is.EqualTo(Vector3.one));
                Assert.That(player.GetComponent<CapsuleCollider2D>().size,
                    Is.EqualTo(new Vector2(0.8f, 1.8f)));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        [Test]
        public void ConfiguratorIsIdempotentForAtlasProfileAndPlayer()
        {
            FennyFrameConfigurator.Configure();
            Hash128[] first =
            {
                AssetDatabase.GetAssetDependencyHash(AtlasPath),
                AssetDatabase.GetAssetDependencyHash(ProfilePath),
                AssetDatabase.GetAssetDependencyHash(PlayerPrefabPath)
            };

            FennyFrameConfigurator.Configure();
            Hash128[] second =
            {
                AssetDatabase.GetAssetDependencyHash(AtlasPath),
                AssetDatabase.GetAssetDependencyHash(ProfilePath),
                AssetDatabase.GetAssetDependencyHash(PlayerPrefabPath)
            };

            Assert.That(second, Is.EqualTo(first));
        }

        private CharacterPresentationProfile CreateValidProfile()
        {
            CharacterPresentationProfile profile =
                ScriptableObject.CreateInstance<CharacterPresentationProfile>();
            cleanup.Add(profile);
            SerializedObject serialized = new(profile);
            AssignSprites(serialized.FindProperty("idleFrames"), 2, "Idle");
            AssignSprites(serialized.FindProperty("runFrames"), 8, "Run");
            serialized.FindProperty("risingFrame").objectReferenceValue = CreateSprite("Rising");
            serialized.FindProperty("apexFrame").objectReferenceValue = CreateSprite("Apex");
            serialized.FindProperty("fallingFrame").objectReferenceValue = CreateSprite("Falling");
            serialized.FindProperty("fallbackFrame").objectReferenceValue = CreateSprite("Fallback");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private void AssignSprites(SerializedProperty array, int count, string prefix)
        {
            array.arraySize = count;
            for (int index = 0; index < count; index++)
                array.GetArrayElementAtIndex(index).objectReferenceValue =
                    CreateSprite($"{prefix}_{index:00}");
        }

        private Sprite CreateSprite(string name)
        {
            Texture2D texture = new(2, 2);
            texture.SetPixels(Enumerable.Repeat(Color.white, 4).ToArray());
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.zero);
            sprite.name = name;
            cleanup.Add(sprite);
            cleanup.Add(texture);
            return sprite;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private static void SetMotorState(PlayerMotor2D motor, PlayerMotionState state)
        {
            typeof(PlayerMotor2D).GetProperty(nameof(PlayerMotor2D.State))
                .GetSetMethod(true)
                .Invoke(motor, new object[] { state });
        }
    }
}
