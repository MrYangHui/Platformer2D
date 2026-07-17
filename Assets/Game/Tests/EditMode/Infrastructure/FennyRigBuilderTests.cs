using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SnowbreakFan.Infrastructure.Editor;
using UnityEditor;
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
    }
}
