using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SnowbreakFan.Infrastructure.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SnowbreakFan.Infrastructure.Tests
{
    public sealed class FennyRigBuilderTests
    {
        private const string PartsPath =
            "Assets/Game/Art/Characters/Player/FennyGolden_RigParts_v005.png";

        [Test]
        public void RigPartsMasterIsNormalizedTransparentAtlas()
        {
            Assert.That(File.Exists(PartsPath), Is.True);

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PartsPath);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(PartsPath);

            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(1792));
            Assert.That(texture.height, Is.EqualTo(1152));
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.alphaIsTransparency, Is.True);
        }

        [Test]
        public void RigPartsAreSemanticAndStaticRigUsesStableHierarchy()
        {
            string[] expectedParts =
            {
                "Head", "RearHair", "NearPonyUpper", "NearPonyLower",
                "FarPonyUpper", "FarPonyLower", "Torso", "Pelvis",
                "FrontSkirt", "RearSkirt", "Backpack", "NearUpperArm",
                "NearForearmHand", "FarUpperArm", "FarForearmHand",
                "RedThigh", "RedShin", "RedBoot", "BareThigh",
                "BareShin", "BareBoot"
            };

            string[] actualParts = AssetDatabase.LoadAllAssetsAtPath(PartsPath)
                .OfType<Sprite>()
                .Select(sprite => sprite.name)
                .OrderBy(name => name)
                .ToArray();
            Assert.That(actualParts, Is.EqualTo(expectedParts.OrderBy(name => name)));

            const string rigPath = "Assets/Game/Prefabs/Player/FennyVisualRig.prefab";
            GameObject rig = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.name, Is.EqualTo("FennyVisualRig"));
            Assert.That(rig.transform.localPosition, Is.EqualTo(Vector3.zero));

            string[] jointPaths =
            {
                "Hip/Chest/Neck",
                "Hip/NearThigh/NearShin/NearBoot",
                "Hip/FarThigh/FarShin/FarBoot",
                "Hip/Chest/NearUpperArm/NearForearm",
                "Hip/Chest/FarUpperArm/FarForearm",
                "Hip/Chest/Neck/NearPonyUpper/NearPonyLower",
                "Hip/Chest/Neck/FarPonyUpper/FarPonyLower"
            };
            Assert.That(jointPaths.All(path => rig.transform.Find(path) != null), Is.True);

            Transform contact = rig.transform.Find("GroundContact");
            Assert.That(contact, Is.Not.Null);
            Assert.That(contact.localPosition.y, Is.EqualTo(-1f).Within(0.001f));

            Transform[] transforms = rig.GetComponentsInChildren<Transform>(true);
            Assert.That(transforms.All(item => item.localScale == Vector3.one), Is.True);

            SpriteRenderer[] renderers = rig.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(renderers, Has.Length.EqualTo(21));
            Assert.That(renderers.All(renderer => renderer.sortingLayerName == "Player"), Is.True);
            Assert.That(renderers.Select(renderer => renderer.sprite.name),
                Is.EquivalentTo(expectedParts));
        }

        [Test]
        public void RigBuilderCanRenderStaticPreview()
        {
            const string previewPath = "Temp/FennyRigStaticPreview.png";
            MethodInfo renderPreview = typeof(FennyRigBuilder).GetMethod(
                "RenderPreview",
                BindingFlags.Public | BindingFlags.Static);

            Assert.That(renderPreview, Is.Not.Null);
            renderPreview.Invoke(null, new object[] { previewPath });
            Assert.That(File.Exists(previewPath), Is.True);

            Texture2D preview = new(2, 2);
            try
            {
                Assert.That(preview.LoadImage(File.ReadAllBytes(previewPath)), Is.True);
                Assert.That(preview.width, Is.EqualTo(512));
                Assert.That(preview.height, Is.EqualTo(512));
                int visiblePixels = preview.GetPixels32().Count(pixel =>
                    pixel.r > 35 || pixel.g > 35 || pixel.b > 45);
                Assert.That(visiblePixels, Is.GreaterThan(5000));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        [Test]
        public void RigLayeringConcealsJointSocketsAndSeparatesNearFarLimbs()
        {
            const string rigPath = "Assets/Game/Prefabs/Player/FennyVisualRig.prefab";
            GameObject rig = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            Dictionary<string, SpriteRenderer> renderers = rig
                .GetComponentsInChildren<SpriteRenderer>(true)
                .ToDictionary(renderer => renderer.sprite.name);

            Assert.That(renderers["Torso"].sortingOrder,
                Is.GreaterThan(renderers["NearUpperArm"].sortingOrder));
            Assert.That(renderers["NearUpperArm"].sortingOrder,
                Is.GreaterThan(renderers["NearForearmHand"].sortingOrder));
            Assert.That(renderers["Torso"].sortingOrder,
                Is.GreaterThan(renderers["FarUpperArm"].sortingOrder));
            Assert.That(renderers["FarUpperArm"].sortingOrder,
                Is.GreaterThan(renderers["FarForearmHand"].sortingOrder));
            Assert.That(renderers["FrontSkirt"].sortingOrder,
                Is.GreaterThan(renderers["RedThigh"].sortingOrder));
            Assert.That(renderers["RearSkirt"].sortingOrder,
                Is.GreaterThan(renderers["BareThigh"].sortingOrder));

            Transform nearThigh = rig.transform.Find("Hip/NearThigh");
            Transform farThigh = rig.transform.Find("Hip/FarThigh");
            Transform nearUpperArm = rig.transform.Find("Hip/Chest/NearUpperArm");
            Transform farUpperArm = rig.transform.Find("Hip/Chest/FarUpperArm");
            Assert.That(nearThigh.localPosition.x - farThigh.localPosition.x,
                Is.GreaterThanOrEqualTo(0.12f));
            Assert.That(nearUpperArm.localPosition.x - farUpperArm.localPosition.x,
                Is.GreaterThanOrEqualTo(0.12f));
        }

        [Test]
        public void RigPresentationDriverExposesOnlyStateAndFacingDependencies()
        {
            Type driver = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(
                    "SnowbreakFan.Presentation.PlayerRigPresentation2D"))
                .SingleOrDefault(type => type != null);

            Assert.That(driver, Is.Not.Null);
            string[] serializedFields =
            {
                "animator", "visualRoot", "body", "motor",
                "movementThreshold", "referenceRunSpeed"
            };
            foreach (string fieldName in serializedFields)
            {
                Assert.That(driver.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            }
        }

        [Test]
        public void RigAnimationAssetsUseContinuousTransformCurvesWithoutScalePulse()
        {
            const string folder = "Assets/Game/Animations/Player";
            AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                $"{folder}/Fenny_Idle.anim");
            AnimationClip run = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                $"{folder}/Fenny_Run.anim");
            AnimationClip airborne = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                $"{folder}/Fenny_Airborne.anim");
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                $"{folder}/Fenny_Rig.controller");

            Assert.That(idle, Is.Not.Null);
            Assert.That(run, Is.Not.Null);
            Assert.That(airborne, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(AnimationUtility.GetAnimationClipSettings(idle).loopTime, Is.True);
            Assert.That(AnimationUtility.GetAnimationClipSettings(run).loopTime, Is.True);
            Assert.That(run.length, Is.EqualTo(0.55f).Within(0.01f));

            AnimatorControllerParameter[] parameters = controller.parameters;
            Assert.That(parameters.Single(item => item.name == "IsRunning").type,
                Is.EqualTo(AnimatorControllerParameterType.Bool));
            Assert.That(parameters.Single(item => item.name == "IsAirborne").type,
                Is.EqualTo(AnimatorControllerParameterType.Bool));
            Assert.That(parameters.Single(item => item.name == "RunSpeed").type,
                Is.EqualTo(AnimatorControllerParameterType.Float));
            Assert.That(controller.layers[0].stateMachine.states
                    .Select(item => item.state.name),
                Is.EquivalentTo(new[] { "Idle", "Run", "Airborne" }));

            AnimationClip[] clips = { idle, run, airborne };
            Assert.That(clips.SelectMany(AnimationUtility.GetCurveBindings)
                    .Any(binding => binding.propertyName.Contains("m_LocalScale")),
                Is.False);

            string[] requiredRunPaths =
            {
                "Hip", "Hip/Chest",
                "Hip/NearThigh", "Hip/NearThigh/NearShin",
                "Hip/FarThigh", "Hip/FarThigh/FarShin",
                "Hip/Chest/NearUpperArm", "Hip/Chest/FarUpperArm",
                "Hip/Chest/Neck/NearPonyUpper",
                "Hip/Chest/Neck/FarPonyUpper"
            };
            string[] runPaths = AnimationUtility.GetCurveBindings(run)
                .Select(binding => binding.path)
                .Distinct()
                .ToArray();
            Assert.That(requiredRunPaths.All(path => runPaths.Contains(path)), Is.True);
        }

        [Test]
        public void RigBuilderCanRenderSampledAnimationPhase()
        {
            MethodInfo renderAnimationPreview = typeof(FennyRigBuilder).GetMethod(
                "RenderAnimationPreview",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(renderAnimationPreview, Is.Not.Null);

            const string outputPath =
                "TestResults/FennyRigPreviews/RunQuarter.png";
            renderAnimationPreview.Invoke(
                null,
                new object[]
                {
                    "Assets/Game/Animations/Player/Fenny_Run.anim",
                    0.1375f,
                    outputPath,
                    false
                });
            Assert.That(File.Exists(outputPath), Is.True);

            Texture2D preview = new(2, 2);
            try
            {
                Assert.That(preview.LoadImage(File.ReadAllBytes(outputPath)), Is.True);
                int visiblePixels = preview.GetPixels32().Count(pixel =>
                    pixel.r > 35 || pixel.g > 35 || pixel.b > 45);
                Assert.That(visiblePixels, Is.GreaterThan(5000));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        [Test]
        public void RigPartPivotsMatchPaintedJointEdges()
        {
            Dictionary<string, float> expectedPivotY = new()
            {
                ["Head"] = 82f / 384f,
                ["RearHair"] = 0.5f,
                ["NearPonyUpper"] = 308f / 384f,
                ["NearPonyLower"] = 312f / 384f,
                ["FarPonyUpper"] = 308f / 384f,
                ["FarPonyLower"] = 307f / 384f,
                ["Torso"] = 70f / 384f,
                ["Pelvis"] = 130f / 384f,
                ["FrontSkirt"] = 296f / 384f,
                ["RearSkirt"] = 288f / 384f,
                ["Backpack"] = 0.5f,
                ["NearUpperArm"] = 310f / 384f,
                ["NearForearmHand"] = 322f / 384f,
                ["FarUpperArm"] = 304f / 384f,
                ["FarForearmHand"] = 329f / 384f,
                ["RedThigh"] = 328f / 384f,
                ["RedShin"] = 324f / 384f,
                ["RedBoot"] = 294f / 384f,
                ["BareThigh"] = 323f / 384f,
                ["BareShin"] = 326f / 384f,
                ["BareBoot"] = 291f / 384f
            };

            Dictionary<string, Sprite> sprites = AssetDatabase
                .LoadAllAssetsAtPath(PartsPath)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name);
            foreach ((string partName, float expected) in expectedPivotY)
            {
                float normalizedPivot =
                    sprites[partName].pivot.y / sprites[partName].rect.height;
                Assert.That(normalizedPivot,
                    Is.EqualTo(expected).Within(0.005f),
                    partName);
            }
            Assert.That(sprites.Values.All(sprite => sprite.pixelsPerUnit == 512f),
                Is.True);
        }

        [Test]
        public void RunGaitUsesShortStrideAndOverlappingLegJoints()
        {
            AnimationClip run = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/Game/Animations/Player/Fenny_Run.anim");
            Assert.That(run, Is.Not.Null);

            string[] thighPaths = { "Hip/NearThigh", "Hip/FarThigh" };
            string[] shinPaths =
            {
                "Hip/NearThigh/NearShin",
                "Hip/FarThigh/FarShin"
            };
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(run))
            {
                if (binding.propertyName != "localEulerAnglesRaw.z")
                    continue;

                float maxAngle = AnimationUtility.GetEditorCurve(run, binding)
                    .keys
                    .Max(key => Mathf.Abs(key.value));
                if (thighPaths.Contains(binding.path))
                    Assert.That(maxAngle, Is.LessThanOrEqualTo(25f), binding.path);
                if (shinPaths.Contains(binding.path))
                    Assert.That(maxAngle, Is.LessThanOrEqualTo(45f), binding.path);
            }

            GameObject rig = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Prefabs/Player/FennyVisualRig.prefab");
            Transform hip = rig.transform.Find("Hip");
            Assert.That(hip.localPosition.y, Is.LessThanOrEqualTo(-0.18f));
            string[] jointPaths =
            {
                "Hip/NearThigh/NearShin",
                "Hip/NearThigh/NearShin/NearBoot",
                "Hip/FarThigh/FarShin",
                "Hip/FarThigh/FarShin/FarBoot"
            };
            foreach (string path in jointPaths)
                Assert.That(Mathf.Abs(rig.transform.Find(path).localPosition.y),
                    Is.LessThanOrEqualTo(0.31f), path);
        }

        [Test]
        public void RepeatedBuildPreservesAnimatorStateObjects()
        {
            FennyRigBuilder.Build();
            AnimatorController first = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                FennyRigBuilder.ControllerPath);
            Dictionary<string, int> stateIds = first.layers[0].stateMachine.states
                .ToDictionary(item => item.state.name, item => item.state.GetInstanceID());
            Hash128 playerHash = AssetDatabase.GetAssetDependencyHash(
                FennyRigBuilder.PlayerPrefabPath);

            FennyRigBuilder.Build();
            AnimatorController second = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                FennyRigBuilder.ControllerPath);
            Dictionary<string, int> rebuiltIds = second.layers[0].stateMachine.states
                .ToDictionary(item => item.state.name, item => item.state.GetInstanceID());

            Assert.That(rebuiltIds, Is.EqualTo(stateIds));
            Assert.That(AssetDatabase.GetAssetDependencyHash(
                    FennyRigBuilder.PlayerPrefabPath),
                Is.EqualTo(playerHash));
        }
    }
}
