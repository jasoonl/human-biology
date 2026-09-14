using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace HumanBodyExplorer.Dicom
{
    /// <summary>
    /// Phase 77: packs a flattened Hounsfield-unit-style float array into a
    /// Texture3D for the raymarching shader (DicomRaymarch.shader, Module III
    /// Phase 30). This part doesn't need real patient data or a native parser -
    /// any correctly-shaped float[] works - so it's genuinely implemented and
    /// tested, unlike Phase 76's native DICOM parser (see DicomStubs.cs).
    /// </summary>
    public static class DicomVolumeLoader
    {
        public static Texture3D CreateVolumeTexture(float[] flattenedDensities, int width, int height, int depth)
        {
            if (flattenedDensities.Length != width * height * depth)
            {
                throw new System.ArgumentException(
                    $"Density array length {flattenedDensities.Length} does not match {width}x{height}x{depth}={width * height * depth}.");
            }

            var texture = new Texture3D(width, height, depth, GraphicsFormat.R32_SFloat, TextureCreationFlags.None);
            var colors = new Color[flattenedDensities.Length];

            for (int i = 0; i < flattenedDensities.Length; i++)
            {
                colors[i] = new Color(flattenedDensities[i], 0, 0, 0);
            }

            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        public static int FlattenedIndex(int x, int y, int z, int width, int height) => x + y * width + z * width * height;
    }
}
