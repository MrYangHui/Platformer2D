using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
        private const float PixelsPerUnit = 600f;

        [MenuItem("Snowbreak Fan/Build Fenny Cutout Rig")]
        public static void Build()
        {
            ConfigureAtlas();
            BuildRigPrefab();
            AssetDatabase.SaveAssets();
        }

        public static void RenderPreview(string outputPath)
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
            if (partName is "RearHair" or "Backpack")
                return new Vector2(0.5f, 0.5f);

            if (partName is "Head" or "Torso" or "Pelvis")
                return new Vector2(0.5f, 0.08f);

            return new Vector2(0.5f, 0.92f);
        }

        private static void BuildRigPrefab()
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
                CreateNode(root.transform, "GroundContact", new Vector2(0f, -1f));
                Transform hip = CreateNode(root.transform, "Hip", new Vector2(0f, -0.15f));
                Transform chest = CreateNode(hip, "Chest", new Vector2(0f, 0.28f));
                Transform neck = CreateNode(chest, "Neck", new Vector2(0.02f, 0.26f));

                Transform nearThigh = CreateNode(hip, "NearThigh", new Vector2(0.07f, 0f));
                Transform nearShin = CreateNode(nearThigh, "NearShin", new Vector2(0f, -0.34f));
                Transform nearBoot = CreateNode(nearShin, "NearBoot", new Vector2(0f, -0.32f));
                Transform farThigh = CreateNode(hip, "FarThigh", new Vector2(-0.07f, 0f));
                Transform farShin = CreateNode(farThigh, "FarShin", new Vector2(0f, -0.34f));
                Transform farBoot = CreateNode(farShin, "FarBoot", new Vector2(0f, -0.32f));

                Transform nearUpperArm =
                    CreateNode(chest, "NearUpperArm", new Vector2(0.07f, 0.2f));
                Transform nearForearm =
                    CreateNode(nearUpperArm, "NearForearm", new Vector2(0f, -0.3f));
                Transform farUpperArm =
                    CreateNode(chest, "FarUpperArm", new Vector2(-0.06f, 0.19f));
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
                AddPart(farBoot, sprites, "BareBoot", 8, new Vector2(0f, 0.24f));
                AddPart(hip, sprites, "RearSkirt", 9, new Vector2(-0.02f, 0.02f));
                AddPart(nearForearm, sprites, "NearForearmHand", 24);
                AddPart(nearUpperArm, sprites, "NearUpperArm", 25);
                AddPart(hip, sprites, "Torso", 26);
                AddPart(hip, sprites, "Pelvis", 27, new Vector2(0f, -0.18f));
                AddPart(nearThigh, sprites, "RedThigh", 20);
                AddPart(nearShin, sprites, "RedShin", 21);
                AddPart(nearBoot, sprites, "RedBoot", 22, new Vector2(0f, 0.24f));
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
