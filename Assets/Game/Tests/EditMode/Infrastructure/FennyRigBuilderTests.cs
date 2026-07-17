using System.IO;
using NUnit.Framework;
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
    }
}
