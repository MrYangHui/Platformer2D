using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SnowbreakFan.Infrastructure.Editor
{
    public static class ArtIntegrationConfigurator
    {
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

        [MenuItem("Snowbreak Fan/Configure First Level Art")]
        public static void Configure()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (PlatformBinding binding in PlatformBindings)
                ConfigurePlatform(binding);

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
    }
}
