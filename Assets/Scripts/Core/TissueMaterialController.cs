using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 23: drives a fake-subsurface-scattering approximation via
    /// MaterialPropertyBlock, allowing a UI slider to transition a tissue's look
    /// from opaque bone to fleshy/translucent. True HDRP SSS profiles aren't
    /// available under URP (no HDRP in this project, see Planning.md) - this
    /// pairs with the custom TissueSSS.shader approximation instead.
    /// </summary>
    public class TissueMaterialController : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;

        private static readonly int ThicknessMultiplierId = Shader.PropertyToID("_ThicknessMultiplier");
        private static readonly int TransmissionIntensityId = Shader.PropertyToID("_TransmissionIntensity");

        private MaterialPropertyBlock _block;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
        }

        public void SetTissueTranslucency(float thicknessMultiplier, float transmissionIntensity)
        {
            if (targetRenderer == null) return;

            targetRenderer.GetPropertyBlock(_block);
            _block.SetFloat(ThicknessMultiplierId, thicknessMultiplier);
            _block.SetFloat(TransmissionIntensityId, transmissionIntensity);
            targetRenderer.SetPropertyBlock(_block);
        }

        /// <summary>0 = opaque bone, 1 = fully fleshy/translucent.</summary>
        public void SetBoneToFleshSlider(float slider0To1)
        {
            slider0To1 = Mathf.Clamp01(slider0To1);
            SetTissueTranslucency(Mathf.Lerp(0f, 2f, slider0To1), Mathf.Lerp(0f, 1f, slider0To1));
        }
    }
}
