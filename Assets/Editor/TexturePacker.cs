using UnityEditor;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// Phase 38: packs 4 grayscale source textures (Metallic, Ambient Occlusion,
    /// Detail Mask, Smoothness) into the R/G/B/A channels of a single Texture2D,
    /// reducing texture memory overhead by ~75% for mobile targets.
    /// </summary>
    public static class TexturePacker
    {
        /// <summary>
        /// Reads the red channel of each source texture (readable, same
        /// dimensions expected) into the corresponding output channel. Any null
        /// source defaults that channel to 1.0 (full white).
        /// </summary>
        public static Texture2D PackChannels(Texture2D metallic, Texture2D ao, Texture2D detailMask, Texture2D smoothness)
        {
            Texture2D reference = metallic ?? ao ?? detailMask ?? smoothness;
            if (reference == null)
            {
                throw new System.ArgumentException("At least one source texture must be provided.");
            }

            int width = reference.width;
            int height = reference.height;

            var packed = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            Color[] metallicPixels = ReadOrDefault(metallic, width, height);
            Color[] aoPixels = ReadOrDefault(ao, width, height);
            Color[] detailPixels = ReadOrDefault(detailMask, width, height);
            Color[] smoothnessPixels = ReadOrDefault(smoothness, width, height);

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(
                    metallicPixels[i].r,
                    aoPixels[i].r,
                    detailPixels[i].r,
                    smoothnessPixels[i].r);
            }

            packed.SetPixels(pixels);
            packed.Apply();
            return packed;
        }

        private static Color[] ReadOrDefault(Texture2D source, int width, int height)
        {
            if (source == null)
            {
                var fill = new Color[width * height];
                for (int i = 0; i < fill.Length; i++) fill[i] = Color.white;
                return fill;
            }

            if (source.width != width || source.height != height)
            {
                throw new System.ArgumentException(
                    $"Texture '{source.name}' ({source.width}x{source.height}) does not match reference size ({width}x{height}).");
            }

            return source.GetPixels();
        }

        [MenuItem("Human Body Explorer/Pack Selected Textures To Asset")]
        public static void PackSelectedAndSave()
        {
            var selected = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
            if (selected.Length == 0)
            {
                Debug.LogWarning("[TexturePacker] Select 1-4 textures in the Project window first.");
                return;
            }

            Texture2D metallic = selected.Length > 0 ? selected[0] : null;
            Texture2D ao = selected.Length > 1 ? selected[1] : null;
            Texture2D detail = selected.Length > 2 ? selected[2] : null;
            Texture2D smoothness = selected.Length > 3 ? selected[3] : null;

            var packed = PackChannels(metallic, ao, detail, smoothness);
            byte[] pngBytes = packed.EncodeToPNG();

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Packed Texture", "PackedTexture", "png", "Choose save location");

            if (string.IsNullOrEmpty(path)) return;

            System.IO.File.WriteAllBytes(path, pngBytes);
            AssetDatabase.Refresh();
            Debug.Log($"[TexturePacker] Saved packed texture to {path}");
        }
    }
}
