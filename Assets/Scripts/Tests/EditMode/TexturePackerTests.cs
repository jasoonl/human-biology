using HumanBodyExplorer.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    public class TexturePackerTests
    {
        private static Texture2D SolidTexture(float value)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];
            for (int i = 0; i < 4; i++) pixels[i] = new Color(value, value, value, 1f);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        [Test]
        public void PackChannels_CombinesFourSourcesIntoRGBA()
        {
            var metallic = SolidTexture(0.2f);
            var ao = SolidTexture(0.4f);
            var detail = SolidTexture(0.6f);
            var smoothness = SolidTexture(0.8f);

            var packed = TexturePacker.PackChannels(metallic, ao, detail, smoothness);

            Color pixel = packed.GetPixel(0, 0);
            Assert.AreEqual(0.2f, pixel.r, 0.01f);
            Assert.AreEqual(0.4f, pixel.g, 0.01f);
            Assert.AreEqual(0.6f, pixel.b, 0.01f);
            Assert.AreEqual(0.8f, pixel.a, 0.01f);

            Object.DestroyImmediate(metallic);
            Object.DestroyImmediate(ao);
            Object.DestroyImmediate(detail);
            Object.DestroyImmediate(smoothness);
            Object.DestroyImmediate(packed);
        }

        [Test]
        public void PackChannels_MissingSource_DefaultsToWhite()
        {
            var metallic = SolidTexture(0.5f);

            var packed = TexturePacker.PackChannels(metallic, null, null, null);
            Color pixel = packed.GetPixel(0, 0);

            Assert.AreEqual(0.5f, pixel.r, 0.01f);
            Assert.AreEqual(1f, pixel.g, 0.01f);
            Assert.AreEqual(1f, pixel.b, 0.01f);
            Assert.AreEqual(1f, pixel.a, 0.01f);

            Object.DestroyImmediate(metallic);
            Object.DestroyImmediate(packed);
        }

        [Test]
        public void PackChannels_MismatchedSize_Throws()
        {
            var small = SolidTexture(0.1f);
            var big = new Texture2D(4, 4);

            Assert.Throws<System.ArgumentException>(() => TexturePacker.PackChannels(small, big, null, null));

            Object.DestroyImmediate(small);
            Object.DestroyImmediate(big);
        }
    }
}
