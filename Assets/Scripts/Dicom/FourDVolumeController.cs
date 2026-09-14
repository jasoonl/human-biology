using UnityEngine;

namespace HumanBodyExplorer.Dicom
{
    /// <summary>
    /// Phase 82: swaps the active Texture3D fed to DicomRaymarch.shader based on
    /// elapsed time, producing a beating volumetric echocardiogram loop from a
    /// time-series of volumes.
    /// </summary>
    public class FourDVolumeController : MonoBehaviour
    {
        [SerializeField] private Texture3D[] frames;
        [SerializeField] private Material raymarchMaterial;
        [SerializeField] private float framesPerSecond = 15f;

        private static readonly int VolumeTexId = Shader.PropertyToID("_VolumeTex");

        /// <summary>Exposed for tests: frame index math without needing a live material.</summary>
        public static int ComputeFrameIndex(float time, float fps, int frameCount)
        {
            if (frameCount <= 0) return -1;
            int index = Mathf.FloorToInt(time * fps) % frameCount;
            return index < 0 ? index + frameCount : index;
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0 || raymarchMaterial == null) return;

            int index = ComputeFrameIndex(Time.time, framesPerSecond, frames.Length);
            raymarchMaterial.SetTexture(VolumeTexId, frames[index]);
        }
    }
}
