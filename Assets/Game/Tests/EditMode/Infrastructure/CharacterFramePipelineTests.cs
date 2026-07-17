using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SnowbreakFan.Presentation;
using UnityEditor;
using UnityEngine;

namespace SnowbreakFan.Infrastructure.Tests
{
    public sealed class CharacterFramePipelineTests
    {
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
    }
}
