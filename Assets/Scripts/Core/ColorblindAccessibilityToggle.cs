using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 33: toggles the Daltonization full-screen renderer feature on/off at
    /// runtime. The pass itself is URP's own built-in FullScreenPassRendererFeature
    /// (configured with Colorblind.shader as its passMaterial via
    /// Assets/Editor/ColorblindFilterFeatureSetup.cs) rather than a hand-rolled
    /// ScriptableRenderPass - URP 17's RenderGraph-only pass API
    /// (RecordRenderGraph/TextureHandle/UniversalResourceData) is substantial
    /// boilerplate to reimplement correctly, and Unity already ships and
    /// maintains a fullscreen-blit feature that does exactly this.
    /// </summary>
    public class ColorblindAccessibilityToggle : MonoBehaviour
    {
        [SerializeField] private ScriptableRendererFeature colorblindFeature;

        public bool IsEnabled => colorblindFeature != null && colorblindFeature.isActive;

        public void SetEnabled(bool enabled)
        {
            if (colorblindFeature == null) return;
            colorblindFeature.SetActive(enabled);
        }
    }
}
