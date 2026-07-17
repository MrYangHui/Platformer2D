using System.Collections.Generic;
using System.IO;
using System.Linq;
using SnowbreakFan.Player;
using SnowbreakFan.Presentation;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace SnowbreakFan.Infrastructure.Editor
{
    public static class FennyFrameConfigurator
    {
        public const string AtlasPath =
            "Assets/Game/Art/Characters/Player/FennyGolden_Frames_v006.png";
        public const string ProfilePath =
            "Assets/Game/Config/Characters/FennyGoldenPresentation.asset";
        public const string PlayerPrefabPath =
            "Assets/Game/Prefabs/Player/Player.prefab";

        private const int CellWidth = 768;
        private const int CellHeight = 1024;
        private const int AtlasHeight = 4096;
        private const int Columns = 4;
        private const float PixelsPerUnit = 480f;

        private static readonly string[] FrameNames =
        {
            "Fenny_Idle_00", "Fenny_Idle_01", "Fenny_Idle_02", "Fenny_Idle_03",
            "Fenny_Run_00", "Fenny_Run_01", "Fenny_Run_02", "Fenny_Run_03",
            "Fenny_Run_04", "Fenny_Run_05", "Fenny_Run_06", "Fenny_Run_07",
            "Fenny_Rising", "Fenny_Apex", "Fenny_Falling"
        };

        [MenuItem("Snowbreak Fan/Configure Fenny Whole Frames")]
        public static void Configure()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureAtlas();
            CharacterPresentationProfile profile = CreateOrUpdateProfile();
            InstallOnPlayer(profile);
            AssetDatabase.SaveAssets();
        }

        public static void ConfigureAtlas()
        {
            TextureImporter importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            if (importer == null)
                throw new UnityException($"Fenny frame atlas is missing: {AtlasPath}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            SpriteDataProviderFactories factories = new();
            factories.Init();
            ISpriteEditorDataProvider provider =
                factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            Dictionary<string, GUID> existingIds = provider.GetSpriteRects()
                .Where(item => FrameNames.Contains(item.name))
                .ToDictionary(item => item.name, item => item.spriteID);
            SpriteRect[] rects = new SpriteRect[FrameNames.Length];
            for (int index = 0; index < rects.Length; index++)
            {
                int column = index % Columns;
                int row = index / Columns;
                string frameName = FrameNames[index];
                rects[index] = new SpriteRect
                {
                    name = frameName,
                    rect = new Rect(
                        column * CellWidth,
                        AtlasHeight - ((row + 1) * CellHeight),
                        CellWidth,
                        CellHeight),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f),
                    spriteID = existingIds.TryGetValue(frameName, out GUID existingId)
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

        public static CharacterPresentationProfile CreateOrUpdateProfile()
        {
            EnsureFolder("Assets/Game/Config");
            EnsureFolder("Assets/Game/Config/Characters");
            CharacterPresentationProfile profile =
                AssetDatabase.LoadAssetAtPath<CharacterPresentationProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CharacterPresentationProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name);
            if (sprites.Count != FrameNames.Length || FrameNames.Any(name => !sprites.ContainsKey(name)))
                throw new UnityException("Fenny frame atlas does not contain all semantic frames.");

            SerializedObject serialized = new(profile);
            AssignFrames(serialized.FindProperty("idleFrames"), FrameNames.Take(4), sprites);
            AssignFrames(serialized.FindProperty("runFrames"), FrameNames.Skip(4).Take(8), sprites);
            serialized.FindProperty("risingFrame").objectReferenceValue = sprites["Fenny_Rising"];
            serialized.FindProperty("apexFrame").objectReferenceValue = sprites["Fenny_Apex"];
            serialized.FindProperty("fallingFrame").objectReferenceValue = sprites["Fenny_Falling"];
            serialized.FindProperty("fallbackFrame").objectReferenceValue = sprites["Fenny_Idle_00"];
            serialized.FindProperty("idleFramesPerSecond").floatValue = 4f;
            serialized.FindProperty("runFramesPerSecond").floatValue = 16f;
            serialized.FindProperty("movementThreshold").floatValue = 0.1f;
            serialized.FindProperty("referenceRunSpeed").floatValue = 6f;
            serialized.FindProperty("apexVelocityThreshold").floatValue = 0.75f;
            serialized.FindProperty("visualRootLocalPosition").vector3Value =
                new Vector3(0f, -1.1f, 0f);
            serialized.FindProperty("visualScale").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        public static void InstallOnPlayer(CharacterPresentationProfile profile)
        {
            string error = string.Empty;
            if (profile == null || !profile.TryValidate(out error))
                throw new UnityException($"Cannot install invalid Fenny profile. {error}");

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                DestroyComponent<PlayerRigPresentation2D>(root);
                DestroyComponent<PlayerSpriteAnimator2D>(root);
                Transform oldRig = root.transform.Find("FennyVisualRig");
                if (oldRig != null)
                    Object.DestroyImmediate(oldRig.gameObject);

                Transform visual = root.transform.Find("Visual");
                if (visual == null)
                {
                    GameObject visualObject = new("Visual");
                    visualObject.transform.SetParent(root.transform, false);
                    visual = visualObject.transform;
                }
                visual.gameObject.layer = root.layer;
                visual.localPosition = profile.VisualRootLocalPosition;
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one * profile.VisualScale;

                for (int index = visual.childCount - 1; index >= 0; index--)
                    Object.DestroyImmediate(visual.GetChild(index).gameObject);
                Animator animator = visual.GetComponent<Animator>();
                if (animator != null)
                    Object.DestroyImmediate(animator);
                SpriteRenderer[] renderers = visual.GetComponents<SpriteRenderer>();
                SpriteRenderer renderer = renderers.FirstOrDefault();
                if (renderer == null)
                    renderer = visual.gameObject.AddComponent<SpriteRenderer>();
                foreach (SpriteRenderer extra in renderers.Skip(1))
                    Object.DestroyImmediate(extra);
                renderer.sprite = profile.IdleFrames[0];
                renderer.color = Color.white;
                renderer.flipX = false;
                renderer.drawMode = SpriteDrawMode.Simple;
                renderer.sortingLayerName = "Player";
                renderer.sortingOrder = 0;

                PlayerFramePresentation2D driver =
                    root.GetComponent<PlayerFramePresentation2D>();
                if (driver == null)
                    driver = root.AddComponent<PlayerFramePresentation2D>();
                SerializedObject serialized = new(driver);
                serialized.FindProperty("profile").objectReferenceValue = profile;
                serialized.FindProperty("targetRenderer").objectReferenceValue = renderer;
                serialized.FindProperty("visualRoot").objectReferenceValue = visual;
                serialized.FindProperty("body").objectReferenceValue = root.GetComponent<Rigidbody2D>();
                serialized.FindProperty("motor").objectReferenceValue = root.GetComponent<PlayerMotor2D>();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void RenderContactPreview(string outputPath)
        {
            string fullSourcePath = Path.GetFullPath(AtlasPath);
            string fullOutputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));
            File.Copy(fullSourcePath, fullOutputPath, true);
        }

        private static void AssignFrames(
            SerializedProperty property,
            IEnumerable<string> names,
            IReadOnlyDictionary<string, Sprite> sprites)
        {
            string[] frameNames = names.ToArray();
            property.arraySize = frameNames.Length;
            for (int index = 0; index < frameNames.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = sprites[frameNames[index]];
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || !AssetDatabase.IsValidFolder(parent))
                throw new UnityException($"Cannot create asset folder: {path}");
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void DestroyComponent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component != null)
                Object.DestroyImmediate(component);
        }
    }
}
