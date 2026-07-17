using System.Linq;
using SnowbreakFan.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SnowbreakFan.Infrastructure.Editor
{
    public static class ArtIntegrationConfigurator
    {
        private const string LevelScenePath = "Assets/Game/Scenes/10_Level_Prototype.unity";
        private readonly struct PlatformBinding
        {
            public PlatformBinding(string prefabPath, string spritePath)
            {
                PrefabPath = prefabPath;
                SpritePath = spritePath;
            }

            public string PrefabPath { get; }
            public string SpritePath { get; }
        }

        private static readonly PlatformBinding[] PlatformBindings =
        {
            new(
                "Assets/Game/Prefabs/Gameplay/Platform_Short.prefab",
                "Assets/Game/Art/Environments/Terrain/Platform_Short_v001.png"),
            new(
                "Assets/Game/Prefabs/Gameplay/Platform_Medium.prefab",
                "Assets/Game/Art/Environments/Terrain/Platform_Medium_v001.png"),
            new(
                "Assets/Game/Prefabs/Gameplay/Platform_Long.prefab",
                "Assets/Game/Art/Environments/Terrain/Platform_Long_v001.png")
        };

        private readonly struct BackgroundBinding
        {
            public BackgroundBinding(
                string name,
                string spritePath,
                float scale,
                float originY,
                float horizontalFollow,
                float verticalFollow,
                string sortingLayer,
                int sortingOrder,
                Color color)
            {
                Name = name;
                SpritePath = spritePath;
                Scale = scale;
                OriginY = originY;
                HorizontalFollow = horizontalFollow;
                VerticalFollow = verticalFollow;
                SortingLayer = sortingLayer;
                SortingOrder = sortingOrder;
                Color = color;
            }

            public string Name { get; }
            public string SpritePath { get; }
            public float Scale { get; }
            public float OriginY { get; }
            public float HorizontalFollow { get; }
            public float VerticalFollow { get; }
            public string SortingLayer { get; }
            public int SortingOrder { get; }
            public Color Color { get; }
        }

        private static readonly BackgroundBinding[] BackgroundBindings =
        {
            new(
                "Background_Sky",
                "Assets/Game/Art/Environments/Backgrounds/Background_Sky_v001.png",
                1.5f,
                2f,
                1f,
                1f,
                "BackgroundFar",
                -100,
                Color.white),
            new(
                "Background_Far",
                "Assets/Game/Art/Environments/Backgrounds/Background_Far_v001.png",
                2f,
                1f,
                0.88f,
                0.85f,
                "BackgroundFar",
                0,
                new Color(0.78f, 0.86f, 0.95f, 0.95f)),
            new(
                "Background_Mid",
                "Assets/Game/Art/Environments/Backgrounds/Background_Mid_v001.png",
                2f,
                0f,
                0.68f,
                0.65f,
                "BackgroundMid",
                0,
                new Color(0.72f, 0.8f, 0.88f, 0.88f)),
            new(
                "Background_Near",
                "Assets/Game/Art/Environments/Backgrounds/Background_Near_v001.png",
                1.8f,
                0f,
                0.45f,
                0.45f,
                "BackgroundNear",
                0,
                new Color(0.48f, 0.6f, 0.72f, 0.52f))
        };

        [MenuItem("Snowbreak Fan/Configure First Level Art")]
        public static void Configure()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            FennyRigBuilder.Build();

            foreach (PlatformBinding binding in PlatformBindings)
                ConfigurePlatform(binding);

            ConfigureBackgrounds();
            AssetDatabase.SaveAssets();
        }

        private static void ConfigurePlatform(PlatformBinding binding)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(binding.SpritePath)
                .OfType<Sprite>()
                .Single();
            GameObject root = PrefabUtility.LoadPrefabContents(binding.PrefabPath);

            try
            {
                BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
                SpriteRenderer renderer = root.GetComponentInChildren<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = Color.white;
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = collider.size;
                renderer.sortingLayerName = "Terrain";
                renderer.transform.localPosition = Vector3.zero;
                renderer.transform.localRotation = Quaternion.identity;
                renderer.transform.localScale = Vector3.one;
                PrefabUtility.SaveAsPrefabAsset(root, binding.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureBackgrounds()
        {
            Scene scene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
            GameObject existing = scene.GetRootGameObjects()
                .FirstOrDefault(item => item.name == "EnvironmentVisuals");

            Transform cameraTransform = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Camera>(true))
                .Single(item => item.CompareTag("MainCamera"))
                .transform;

            GameObject root = existing != null ? existing : new GameObject("EnvironmentVisuals");
            if (existing == null)
                SceneManager.MoveGameObjectToScene(root, scene);

            foreach (BackgroundBinding binding in BackgroundBindings)
                CreateBackgroundLayer(root.transform, cameraTransform, binding);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void CreateBackgroundLayer(
            Transform parent,
            Transform cameraTransform,
            BackgroundBinding binding)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(binding.SpritePath);
            Transform existingLayer = parent.Find(binding.Name);
            GameObject layerObject = existingLayer != null
                ? existingLayer.gameObject
                : new GameObject(binding.Name);
            if (existingLayer == null)
                layerObject.transform.SetParent(parent, false);
            layerObject.transform.position = new Vector3(0f, binding.OriginY, 0f);

            SpriteRenderer[] segments = new SpriteRenderer[3];
            float tileWidth = sprite.bounds.size.x * binding.Scale;
            for (int index = 0; index < segments.Length; index++)
            {
                Transform existingSegment = layerObject.transform.Find($"Segment_{index}");
                GameObject segmentObject = existingSegment != null
                    ? existingSegment.gameObject
                    : new GameObject($"Segment_{index}");
                if (existingSegment == null)
                    segmentObject.transform.SetParent(layerObject.transform, false);
                segmentObject.transform.localPosition = new Vector3((index - 1) * tileWidth, 0f, 0f);
                segmentObject.transform.localScale = Vector3.one * binding.Scale;

                SpriteRenderer renderer = segmentObject.GetComponent<SpriteRenderer>();
                if (renderer == null)
                    renderer = segmentObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = binding.Color;
                renderer.sortingLayerName = binding.SortingLayer;
                renderer.sortingOrder = binding.SortingOrder;
                segments[index] = renderer;
            }

            ParallaxLayer2D layer = layerObject.GetComponent<ParallaxLayer2D>();
            if (layer == null)
                layer = layerObject.AddComponent<ParallaxLayer2D>();
            SerializedObject serialized = new(layer);
            serialized.FindProperty("cameraTransform").objectReferenceValue = cameraTransform;
            SerializedProperty segmentProperty = serialized.FindProperty("segments");
            segmentProperty.arraySize = segments.Length;
            for (int index = 0; index < segments.Length; index++)
                segmentProperty.GetArrayElementAtIndex(index).objectReferenceValue = segments[index];
            serialized.FindProperty("horizontalFollow").floatValue = binding.HorizontalFollow;
            serialized.FindProperty("verticalFollow").floatValue = binding.VerticalFollow;
            serialized.FindProperty("tileWidth").floatValue = tileWidth;
            serialized.FindProperty("overlap").floatValue = 0.2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

    }
}
