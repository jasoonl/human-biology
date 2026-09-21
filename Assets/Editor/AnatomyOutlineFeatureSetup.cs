using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// Attaches AnatomyOutline.shader as an always-on URP full-screen pass, so every
    /// structure is bounded by an ink contour the way a printed anatomical plate is.
    /// Mirrors ColorblindFilterFeatureSetup, with one difference that matters: the
    /// feature declares Depth|Normal requirements, because the shader reads the
    /// depth-normals prepass and URP only generates it when something asks.
    /// </summary>
    public static class AnatomyOutlineFeatureSetup
    {
        private const string MaterialPath = "Assets/Settings/HBE_AnatomyOutlineMaterial.mat";
        private const string FeatureName = "Anatomy Outline";
        private const string ShaderName = "HumanBodyExplorer/AnatomyOutline";

        [MenuItem("Human Body Explorer/Add Anatomy Outline Renderer Feature")]
        public static void AddFeatureToActiveRenderer()
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                Debug.LogError("[AnatomyOutlineFeatureSetup] No active UniversalRenderPipelineAsset found.");
                return;
            }

            var rendererDataField = typeof(UniversalRenderPipelineAsset)
                .GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rendererDataList = rendererDataField?.GetValue(urpAsset) as ScriptableRendererData[];

            if (rendererDataList == null || rendererDataList.Length == 0)
            {
                Debug.LogError("[AnatomyOutlineFeatureSetup] No ScriptableRendererData found on the URP asset.");
                return;
            }

            var rendererData = rendererDataList[0];

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[AnatomyOutlineFeatureSetup] Could not find shader '{ShaderName}'.");
                return;
            }

            // Re-running the build must be able to retune the look, so an existing
            // feature gets its material refreshed rather than being left alone.
            foreach (var existing in rendererData.rendererFeatures)
            {
                if (existing is FullScreenPassRendererFeature present && present.name == FeatureName)
                {
                    if (present.passMaterial != null) ApplyLookSettings(present.passMaterial);
                    present.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
                    present.SetActive(true);
                    EditorUtility.SetDirty(rendererData);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[AnatomyOutlineFeatureSetup] Outline feature already present; refreshed its settings.");
                    return;
                }
            }

            Directory.CreateDirectory("Assets/Settings");
            var material = new Material(shader);
            ApplyLookSettings(material);
            AssetDatabase.CreateAsset(material, MaterialPath);

            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = FeatureName;
            feature.passMaterial = material;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            feature.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            feature.SetActive(true);

            AssetDatabase.AddObjectToAsset(feature, rendererData);
            rendererData.rendererFeatures.Add(feature);

            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AnatomyOutlineFeatureSetup] Added the Anatomy Outline renderer feature.");
        }

        /// <summary>A near-black ink rather than pure black, and a line about a pixel
        /// wide, which is what a printed plate looks like; pure black at 3px reads as
        /// a cartoon cel-shade instead.</summary>
        private static void ApplyLookSettings(Material material)
        {
            material.SetColor("_OutlineColor", new Color(0.05f, 0.05f, 0.07f));
            material.SetFloat("_Thickness", 1.2f);
            material.SetFloat("_DepthSensitivity", 4.0f);
            material.SetFloat("_NormalSensitivity", 2.5f);
            material.SetFloat("_Strength", 0.85f);
        }
    }
}
