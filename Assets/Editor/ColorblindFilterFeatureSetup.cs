using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// Phase 33: creates a material from Colorblind.shader and attaches it to a
    /// URP FullScreenPassRendererFeature instance, added to the active
    /// UniversalRendererData's Renderer Features list.
    /// </summary>
    public static class ColorblindFilterFeatureSetup
    {
        private const string MaterialPath = "Assets/Settings/HBE_ColorblindMaterial.mat";

        [MenuItem("Human Body Explorer/Add Colorblind Renderer Feature")]
        public static void AddFeatureToActiveRenderer()
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                Debug.LogError("[ColorblindFilterFeatureSetup] No active UniversalRenderPipelineAsset found.");
                return;
            }

            var rendererDataField = typeof(UniversalRenderPipelineAsset)
                .GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rendererDataList = rendererDataField?.GetValue(urpAsset) as ScriptableRendererData[];

            if (rendererDataList == null || rendererDataList.Length == 0)
            {
                Debug.LogError("[ColorblindFilterFeatureSetup] No ScriptableRendererData found on the URP asset.");
                return;
            }

            var rendererData = rendererDataList[0];

            foreach (var existing in rendererData.rendererFeatures)
            {
                if (existing is FullScreenPassRendererFeature fs && fs.name == "Colorblind Daltonization")
                {
                    Debug.Log("[ColorblindFilterFeatureSetup] Feature already present, skipping.");
                    return;
                }
            }

            var shader = Shader.Find("HumanBodyExplorer/Colorblind");
            if (shader == null)
            {
                Debug.LogError("[ColorblindFilterFeatureSetup] Could not find shader 'HumanBodyExplorer/Colorblind'.");
                return;
            }

            Directory.CreateDirectory("Assets/Settings");
            var material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);

            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = "Colorblind Daltonization";
            feature.passMaterial = material;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            feature.SetActive(false); // opt-in accessibility toggle, off by default

            AssetDatabase.AddObjectToAsset(feature, rendererData);
            rendererData.rendererFeatures.Add(feature);

            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ColorblindFilterFeatureSetup] Added Colorblind Daltonization renderer feature (inactive by default).");
        }
    }
}
