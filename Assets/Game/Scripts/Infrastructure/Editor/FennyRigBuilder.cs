using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace SnowbreakFan.Infrastructure.Editor
{
    public static class FennyRigBuilder
    {
        public const string PartsPath =
            "Assets/Game/Art/Characters/Player/FennyGolden_RigParts_v005.png";
        public const string RigPrefabPath =
            "Assets/Game/Prefabs/Player/FennyVisualRig.prefab";
        public const string IdleClipPath =
            "Assets/Game/Animations/Player/Fenny_Idle.anim";
        public const string RunClipPath =
            "Assets/Game/Animations/Player/Fenny_Run.anim";
        public const string AirborneClipPath =
            "Assets/Game/Animations/Player/Fenny_Airborne.anim";
        public const string ControllerPath =
            "Assets/Game/Animations/Player/Fenny_Rig.controller";

        public static readonly string[] RequiredPartNames =
        {
            "Head", "RearHair", "NearPonyUpper", "NearPonyLower",
            "FarPonyUpper", "FarPonyLower", "Torso", "Pelvis",
            "FrontSkirt", "RearSkirt", "Backpack", "NearUpperArm",
            "NearForearmHand", "FarUpperArm", "FarForearmHand",
            "RedThigh", "RedShin", "RedBoot", "BareThigh",
            "BareShin", "BareBoot"
        };

        private const int Columns = 7;
        private const int CellWidth = 256;
        private const int CellHeight = 384;
        private const int AtlasHeight = 1152;
        private const float PixelsPerUnit = 512f;

        [MenuItem("Snowbreak Fan/Build Fenny Cutout Rig")]
        public static void Build()
        {
            ConfigureAtlas();
            EnsureAnimationFolder();
            AnimationClip idle = CreateIdleClip();
            AnimationClip run = CreateRunClip();
            AnimationClip airborne = CreateAirborneClip();
            AnimatorController controller =
                CreateController(idle, run, airborne);
            BuildRigPrefab(controller);
            AssetDatabase.SaveAssets();
        }

        public static void RenderPreview(string outputPath)
        {
            RenderPreviewInternal(null, 0f, outputPath, false);
        }

        public static void RenderAnimationPreview(
            string clipPath,
            float time,
            string outputPath,
            bool faceLeft)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                throw new UnityException($"Fenny animation clip is missing: {clipPath}");
            RenderPreviewInternal(clip, time, outputPath, faceLeft);
        }

        [MenuItem("Snowbreak Fan/Render Fenny Rig Validation Previews")]
        public static void RenderValidationPreviews()
        {
            RenderAnimationPreview(
                IdleClipPath,
                0f,
                "TestResults/FennyRigPreviews/Idle.png",
                false);
            RenderAnimationPreview(
                RunClipPath,
                0f,
                "TestResults/FennyRigPreviews/Run_Contact.png",
                false);
            RenderAnimationPreview(
                RunClipPath,
                0.1375f,
                "TestResults/FennyRigPreviews/Run_Passing.png",
                false);
            RenderAnimationPreview(
                RunClipPath,
                0.275f,
                "TestResults/FennyRigPreviews/Run_OppositeContact.png",
                false);
            RenderAnimationPreview(
                RunClipPath,
                0.4125f,
                "TestResults/FennyRigPreviews/Run_OppositePassing.png",
                false);
            RenderAnimationPreview(
                RunClipPath,
                0.1375f,
                "TestResults/FennyRigPreviews/Run_Left.png",
                true);
            RenderAnimationPreview(
                AirborneClipPath,
                0f,
                "TestResults/FennyRigPreviews/Airborne.png",
                false);
        }

        private static void RenderPreviewInternal(
            AnimationClip clip,
            float time,
            string outputPath,
            bool faceLeft)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            if (prefab == null)
                throw new UnityException("Fenny visual rig prefab is missing.");

            PreviewRenderUtility preview = new(true, true);
            GameObject instance = null;
            Texture2D image = null;

            try
            {
                instance = preview.InstantiatePrefabInScene(prefab);
                if (clip != null)
                    clip.SampleAnimation(instance, Mathf.Clamp(time, 0f, clip.length));
                if (faceLeft)
                    instance.transform.localScale = new Vector3(-1f, 1f, 1f);

                SpriteRenderer[] renderers = instance
                    .GetComponentsInChildren<SpriteRenderer>(true);
                Bounds bounds = renderers[0].bounds;
                foreach (SpriteRenderer renderer in renderers.Skip(1))
                    bounds.Encapsulate(renderer.bounds);

                preview.camera.orthographic = true;
                preview.camera.orthographicSize =
                    Mathf.Max(bounds.extents.y, bounds.extents.x) + 0.08f;
                preview.camera.transform.position = new Vector3(
                    bounds.center.x,
                    bounds.center.y,
                    bounds.center.z - 10f);
                preview.camera.transform.rotation = Quaternion.identity;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(0.04f, 0.055f, 0.09f, 1f);

                Rect previewRect = new(0f, 0f, 512f, 512f);
                preview.BeginStaticPreview(previewRect);
                preview.camera.Render();
                image = preview.EndStaticPreview();

                string absolutePath = Path.GetFullPath(outputPath);
                string directory = Path.GetDirectoryName(absolutePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllBytes(absolutePath, image.EncodeToPNG());
            }
            finally
            {
                preview.Cleanup();
                if (instance != null)
                    Object.DestroyImmediate(instance);
                if (image != null)
                    Object.DestroyImmediate(image);
            }
        }

        private static void EnsureAnimationFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Game/Animations"))
                AssetDatabase.CreateFolder("Assets/Game", "Animations");
            if (!AssetDatabase.IsValidFolder("Assets/Game/Animations/Player"))
                AssetDatabase.CreateFolder("Assets/Game/Animations", "Player");
        }

        private static AnimationClip CreateIdleClip()
        {
            AnimationClip clip = LoadOrCreateClip(IdleClipPath, "Fenny_Idle", true);
            SetRotationCurve(clip, "Hip/Chest", 2f, -1f, 1f, -1f);
            SetRotationCurve(clip, "Hip/Chest/Neck", 2f, 1f, -1f, 1f);
            SetRotationCurve(
                clip,
                "Hip/Chest/Neck/NearPonyUpper",
                2f,
                -2f,
                2f,
                -2f);
            SetRotationCurve(
                clip,
                "Hip/Chest/Neck/NearPonyUpper/NearPonyLower",
                2f,
                2f,
                -3f,
                2f);
            SetRotationCurve(
                clip,
                "Hip/Chest/Neck/FarPonyUpper",
                2f,
                1f,
                -2f,
                1f);
            SetRotationCurve(
                clip,
                "Hip/Chest/Neck/FarPonyUpper/FarPonyLower",
                2f,
                -2f,
                3f,
                -2f);
            return clip;
        }

        private static AnimationClip CreateRunClip()
        {
            const float duration = 0.55f;
            AnimationClip clip = LoadOrCreateClip(RunClipPath, "Fenny_Run", true);

            SetPositionCurve(
                clip,
                "Hip",
                "m_LocalPosition.y",
                duration,
                -0.20f, -0.23f, -0.21f, -0.19f, -0.20f,
                -0.23f, -0.21f, -0.19f, -0.20f);
            SetRotationCurve(
                clip,
                "Hip/Chest",
                duration,
                -7f, -9f, -8f, -6f, -7f, -9f, -8f, -6f, -7f);

            SetRotationCurve(
                clip,
                "Hip/NearThigh",
                duration,
                22f, 12f, -8f, -22f, -18f, -8f, 10f, 22f, 22f);
            SetRotationCurve(
                clip,
                "Hip/NearThigh/NearShin",
                duration,
                4f, 18f, 42f, 30f, 8f, 14f, 38f, 28f, 4f);
            SetRotationCurve(
                clip,
                "Hip/NearThigh/NearShin/NearBoot",
                duration,
                -8f, -3f, 12f, 4f, -6f, -2f, 10f, 3f, -8f);

            SetRotationCurve(
                clip,
                "Hip/FarThigh",
                duration,
                -18f, -8f, 10f, 22f, 22f, 12f, -8f, -22f, -18f);
            SetRotationCurve(
                clip,
                "Hip/FarThigh/FarShin",
                duration,
                8f, 14f, 38f, 28f, 4f, 18f, 42f, 30f, 8f);
            SetRotationCurve(
                clip,
                "Hip/FarThigh/FarShin/FarBoot",
                duration,
                -6f, -2f, 10f, 3f, -8f, -3f, 12f, 4f, -6f);

            SetRotationCurve(
                clip,
                "Hip/Chest/NearUpperArm",
                duration,
                -18f, -10f, 5f, 18f, 18f, 10f, -5f, -18f, -18f);
            SetRotationCurve(
                clip,
                "Hip/Chest/NearUpperArm/NearForearm",
                duration,
                25f, 32f, 40f, 34f, 25f, 32f, 40f, 34f, 25f);
            SetRotationCurve(
                clip,
                "Hip/Chest/FarUpperArm",
                duration,
                18f, 10f, -5f, -18f, -18f, -10f, 5f, 18f, 18f);
            SetRotationCurve(
                clip,
                "Hip/Chest/FarUpperArm/FarForearm",
                duration,
                25f, 32f, 40f, 34f, 25f, 32f, 40f, 34f, 25f);

            SetRotationCurve(
                clip,
                "Hip/Chest/Neck/NearPonyUpper",
                duration,
                -6f, -2f, 6f, 10f, 6f, 2f, -6f, -8f, -6f);
            SetRotationCurve(
                clip,
                "Hip/Chest/Neck/NearPonyUpper/NearPonyLower",
                duration,
                -3f, 4f, 10f, 6f, 1f, -4f, -8f, -5f, -3f);
            SetRotationCurve(
                clip,
                "Hip/Chest/Neck/FarPonyUpper",
                duration,
                4f, 8f, 5f, -1f, -5f, -8f, -2f, 3f, 4f);
            SetRotationCurve(
                clip,
                "Hip/Chest/Neck/FarPonyUpper/FarPonyLower",
                duration,
                2f, 7f, 4f, -3f, -7f, -4f, 1f, 5f, 2f);

            SetLimbSortingCurves(clip, duration);
            return clip;
        }

        private static AnimationClip CreateAirborneClip()
        {
            AnimationClip clip =
                LoadOrCreateClip(AirborneClipPath, "Fenny_Airborne", false);
            SetRotationCurve(clip, "Hip/Chest", 1f, -8f, -8f);
            SetRotationCurve(clip, "Hip/NearThigh", 1f, 95f, 95f);
            SetRotationCurve(clip, "Hip/NearThigh/NearShin", 1f, -125f, -125f);
            SetRotationCurve(
                clip,
                "Hip/NearThigh/NearShin/NearBoot",
                1f,
                20f,
                20f);
            SetRotationCurve(clip, "Hip/FarThigh", 1f, -5f, -5f);
            SetRotationCurve(clip, "Hip/FarThigh/FarShin", 1f, 15f, 15f);
            SetRotationCurve(clip, "Hip/Chest/NearUpperArm", 1f, -28f, -28f);
            SetRotationCurve(
                clip,
                "Hip/Chest/NearUpperArm/NearForearm",
                1f,
                40f,
                40f);
            SetRotationCurve(clip, "Hip/Chest/FarUpperArm", 1f, 22f, 22f);
            SetRotationCurve(
                clip,
                "Hip/Chest/FarUpperArm/FarForearm",
                1f,
                38f,
                38f);
            SetRotationCurve(
                clip,
                "Hip/Chest/Neck/NearPonyUpper",
                1f,
                -14f,
                -14f);
            SetRotationCurve(
                clip,
                "Hip/Chest/Neck/NearPonyUpper/NearPonyLower",
                1f,
                -10f,
                -10f);
            SetRotationCurve(
                clip,
                "Hip/Chest/Neck/FarPonyUpper",
                1f,
                -10f,
                -10f);
            return clip;
        }

        private static AnimationClip LoadOrCreateClip(
            string path,
            string name,
            bool loop)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.ClearCurves();
            clip.name = name;
            clip.frameRate = 60f;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateController(
            AnimationClip idleClip,
            AnimationClip runClip,
            AnimationClip airborneClip)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.parameters = new[]
            {
                new AnimatorControllerParameter
                {
                    name = "IsRunning",
                    type = AnimatorControllerParameterType.Bool
                },
                new AnimatorControllerParameter
                {
                    name = "IsAirborne",
                    type = AnimatorControllerParameterType.Bool
                },
                new AnimatorControllerParameter
                {
                    name = "RunSpeed",
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = 1f
                }
            };

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states.ToArray())
                machine.RemoveState(child.state);
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions.ToArray())
                machine.RemoveAnyStateTransition(transition);

            AnimatorState idle = machine.AddState("Idle", new Vector3(200f, 120f));
            AnimatorState run = machine.AddState("Run", new Vector3(430f, 120f));
            AnimatorState airborne =
                machine.AddState("Airborne", new Vector3(315f, 300f));
            idle.motion = idleClip;
            run.motion = runClip;
            airborne.motion = airborneClip;
            idle.writeDefaultValues = true;
            run.writeDefaultValues = true;
            airborne.writeDefaultValues = true;
            run.speedParameterActive = true;
            run.speedParameter = "RunSpeed";
            machine.defaultState = idle;

            AddTransition(idle, airborne, 0.05f, "IsAirborne", true);
            AddTransition(idle, run, 0.08f, "IsRunning", true);
            AddTransition(run, airborne, 0.05f, "IsAirborne", true);
            AddTransition(run, idle, 0.08f, "IsRunning", false);

            AnimatorStateTransition airToIdle =
                AddTransition(airborne, idle, 0.06f, "IsAirborne", false);
            airToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");
            AnimatorStateTransition airToRun =
                AddTransition(airborne, run, 0.06f, "IsAirborne", false);
            airToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorStateTransition AddTransition(
            AnimatorState source,
            AnimatorState destination,
            float duration,
            string parameter,
            bool expected)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.AddCondition(
                expected ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                parameter);
            return transition;
        }

        private static void SetRotationCurve(
            AnimationClip clip,
            string path,
            float duration,
            params float[] values)
        {
            SetFloatCurve(
                clip,
                path,
                typeof(Transform),
                "localEulerAnglesRaw.z",
                CreateKeys(duration, values),
                false);
        }

        private static void SetPositionCurve(
            AnimationClip clip,
            string path,
            string property,
            float duration,
            params float[] values)
        {
            SetFloatCurve(
                clip,
                path,
                typeof(Transform),
                property,
                CreateKeys(duration, values),
                false);
        }

        private static Keyframe[] CreateKeys(float duration, IReadOnlyList<float> values)
        {
            Keyframe[] keys = new Keyframe[values.Count];
            for (int index = 0; index < values.Count; index++)
            {
                float time = duration * index / (values.Count - 1f);
                keys[index] = new Keyframe(time, values[index]);
            }
            return keys;
        }

        private static void SetFloatCurve(
            AnimationClip clip,
            string path,
            System.Type type,
            string property,
            Keyframe[] keys,
            bool constant)
        {
            AnimationCurve curve = new(keys);
            for (int index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve,
                    index,
                    constant ? AnimationUtility.TangentMode.Constant :
                        AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(
                    curve,
                    index,
                    constant ? AnimationUtility.TangentMode.Constant :
                        AnimationUtility.TangentMode.ClampedAuto);
            }

            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, type, property);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static void SetLimbSortingCurves(AnimationClip clip, float duration)
        {
            SetSortingCurve(
                clip,
                "Hip/NearThigh/RedThigh_Sprite",
                duration,
                20f,
                6f);
            SetSortingCurve(
                clip,
                "Hip/NearThigh/NearShin/RedShin_Sprite",
                duration,
                21f,
                7f);
            SetSortingCurve(
                clip,
                "Hip/NearThigh/NearShin/NearBoot/RedBoot_Sprite",
                duration,
                22f,
                8f);
            SetSortingCurve(
                clip,
                "Hip/FarThigh/BareThigh_Sprite",
                duration,
                6f,
                20f);
            SetSortingCurve(
                clip,
                "Hip/FarThigh/FarShin/BareShin_Sprite",
                duration,
                7f,
                21f);
            SetSortingCurve(
                clip,
                "Hip/FarThigh/FarShin/FarBoot/BareBoot_Sprite",
                duration,
                8f,
                22f);
        }

        private static void SetSortingCurve(
            AnimationClip clip,
            string path,
            float duration,
            float firstHalf,
            float secondHalf)
        {
            float midpoint = duration * 0.5f;
            Keyframe[] keys =
            {
                new(0f, firstHalf),
                new(midpoint - 0.001f, firstHalf),
                new(midpoint, secondHalf),
                new(duration - 0.001f, secondHalf),
                new(duration, firstHalf)
            };
            SetFloatCurve(
                clip,
                path,
                typeof(SpriteRenderer),
                "m_SortingOrder",
                keys,
                true);
        }

        private static void ConfigureAtlas()
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(PartsPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            SpriteDataProviderFactories factories = new();
            factories.Init();
            ISpriteEditorDataProvider provider =
                factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            Dictionary<string, GUID> existingIds = provider.GetSpriteRects()
                .Where(item => RequiredPartNames.Contains(item.name))
                .ToDictionary(item => item.name, item => item.spriteID);

            SpriteRect[] rects = new SpriteRect[RequiredPartNames.Length];
            for (int index = 0; index < rects.Length; index++)
            {
                int column = index % Columns;
                int row = index / Columns;
                string partName = RequiredPartNames[index];
                rects[index] = new SpriteRect
                {
                    name = partName,
                    rect = new Rect(
                        column * CellWidth,
                        AtlasHeight - ((row + 1) * CellHeight),
                        CellWidth,
                        CellHeight),
                    alignment = SpriteAlignment.Custom,
                    pivot = GetPivot(partName),
                    spriteID = existingIds.TryGetValue(partName, out GUID existingId)
                        ? existingId
                        : GUID.Generate()
                };
            }

            provider.SetSpriteRects(rects);
            ISpriteNameFileIdDataProvider nameProvider =
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameProvider.SetNameFileIdPairs(
                rects.Select(item => new SpriteNameFileIdPair(item.name, item.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static Vector2 GetPivot(string partName)
        {
            float pivotY = partName switch
            {
                "Head" => 82f / CellHeight,
                "RearHair" => 0.5f,
                "NearPonyUpper" => 308f / CellHeight,
                "NearPonyLower" => 312f / CellHeight,
                "FarPonyUpper" => 308f / CellHeight,
                "FarPonyLower" => 307f / CellHeight,
                "Torso" => 70f / CellHeight,
                "Pelvis" => 130f / CellHeight,
                "FrontSkirt" => 296f / CellHeight,
                "RearSkirt" => 288f / CellHeight,
                "Backpack" => 0.5f,
                "NearUpperArm" => 310f / CellHeight,
                "NearForearmHand" => 322f / CellHeight,
                "FarUpperArm" => 304f / CellHeight,
                "FarForearmHand" => 329f / CellHeight,
                "RedThigh" => 328f / CellHeight,
                "RedShin" => 324f / CellHeight,
                "RedBoot" => 294f / CellHeight,
                "BareThigh" => 323f / CellHeight,
                "BareShin" => 326f / CellHeight,
                "BareBoot" => 291f / CellHeight,
                _ => throw new UnityException($"Unknown Fenny rig part: {partName}")
            };
            return new Vector2(0.5f, pivotY);
        }

        private static void BuildRigPrefab(RuntimeAnimatorController controller)
        {
            Dictionary<string, Sprite> sprites = AssetDatabase
                .LoadAllAssetsAtPath(PartsPath)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name);
            if (sprites.Count != RequiredPartNames.Length)
                throw new UnityException("Fenny rig atlas does not contain all required parts.");

            GameObject root = new("FennyVisualRig");
            try
            {
                Animator animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                CreateNode(root.transform, "GroundContact", new Vector2(0f, -1f));
                Transform hip = CreateNode(root.transform, "Hip", new Vector2(0f, -0.2f));
                Transform chest = CreateNode(hip, "Chest", new Vector2(0f, 0.24f));
                Transform neck = CreateNode(chest, "Neck", new Vector2(0.02f, 0.237f));

                Transform nearThigh = CreateNode(hip, "NearThigh", new Vector2(0.07f, 0f));
                Transform nearShin = CreateNode(nearThigh, "NearShin", new Vector2(0f, -0.3f));
                Transform nearBoot = CreateNode(nearShin, "NearBoot", new Vector2(0f, -0.3f));
                Transform farThigh = CreateNode(hip, "FarThigh", new Vector2(-0.07f, 0f));
                Transform farShin = CreateNode(farThigh, "FarShin", new Vector2(0f, -0.3f));
                Transform farBoot = CreateNode(farShin, "FarBoot", new Vector2(0f, -0.3f));

                Transform nearUpperArm =
                    CreateNode(chest, "NearUpperArm", new Vector2(0.07f, 0.11f));
                Transform nearForearm =
                    CreateNode(nearUpperArm, "NearForearm", new Vector2(0f, -0.3f));
                Transform farUpperArm =
                    CreateNode(chest, "FarUpperArm", new Vector2(-0.06f, 0.1f));
                Transform farForearm =
                    CreateNode(farUpperArm, "FarForearm", new Vector2(0f, -0.3f));

                Transform nearPonyUpper =
                    CreateNode(neck, "NearPonyUpper", new Vector2(-0.1f, 0.35f));
                Transform nearPonyLower =
                    CreateNode(nearPonyUpper, "NearPonyLower", new Vector2(0f, -0.3f));
                Transform farPonyUpper =
                    CreateNode(neck, "FarPonyUpper", new Vector2(-0.14f, 0.32f));
                Transform farPonyLower =
                    CreateNode(farPonyUpper, "FarPonyLower", new Vector2(0f, -0.3f));

                AddPart(neck, sprites, "RearHair", 2, new Vector2(-0.1f, 0.18f));
                AddPart(farPonyUpper, sprites, "FarPonyUpper", 0);
                AddPart(farPonyLower, sprites, "FarPonyLower", 1);
                AddPart(chest, sprites, "Backpack", 3, new Vector2(-0.12f, 0.08f));
                AddPart(farForearm, sprites, "FarForearmHand", 4);
                AddPart(farUpperArm, sprites, "FarUpperArm", 5);
                AddPart(farThigh, sprites, "BareThigh", 6);
                AddPart(farShin, sprites, "BareShin", 7);
                AddPart(farBoot, sprites, "BareBoot", 8, new Vector2(0f, 0.19f));
                AddPart(hip, sprites, "RearSkirt", 9, new Vector2(-0.02f, 0.02f));
                AddPart(nearForearm, sprites, "NearForearmHand", 24);
                AddPart(nearUpperArm, sprites, "NearUpperArm", 25);
                AddPart(hip, sprites, "Torso", 26);
                AddPart(hip, sprites, "Pelvis", 27, new Vector2(0f, -0.18f));
                AddPart(nearThigh, sprites, "RedThigh", 20);
                AddPart(nearShin, sprites, "RedShin", 21);
                AddPart(nearBoot, sprites, "RedBoot", 22, new Vector2(0f, 0.2f));
                AddPart(hip, sprites, "FrontSkirt", 28, new Vector2(0.02f, 0.02f));
                AddPart(neck, sprites, "Head", 30);
                AddPart(nearPonyUpper, sprites, "NearPonyUpper", 31);
                AddPart(nearPonyLower, sprites, "NearPonyLower", 32);

                PrefabUtility.SaveAsPrefabAsset(root, RigPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Transform CreateNode(Transform parent, string name, Vector2 position)
        {
            GameObject node = new(name);
            node.transform.SetParent(parent, false);
            node.transform.localPosition = new Vector3(position.x, position.y, 0f);
            node.transform.localRotation = Quaternion.identity;
            node.transform.localScale = Vector3.one;
            return node.transform;
        }

        private static void AddPart(
            Transform parent,
            IReadOnlyDictionary<string, Sprite> sprites,
            string partName,
            int sortingOrder,
            Vector2 localPosition = default)
        {
            GameObject part = new($"{partName}_Sprite");
            part.transform.SetParent(parent, false);
            part.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = Vector3.one;

            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites[partName];
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = sortingOrder;
        }
    }
}
