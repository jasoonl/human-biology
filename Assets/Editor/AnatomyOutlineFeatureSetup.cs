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
            EnsureAmbientOcclusion(rendererData);

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

        /// <summary>
        /// Screen-space ambient occlusion, so cavities read as cavities. With flat, bright
        /// ambient light and no shadows, an eye socket, a nasal aperture or the gap
        /// between two ribs is lit exactly like the bone around it and looks like a
        /// shallow dimple. Occlusion darkens anything with nearer geometry close beside it.
        /// The radius is tiny because the anatomy is centimetres across.
        /// </summary>
        private static void EnsureAmbientOcclusion(ScriptableRendererData rendererData)
        {
            ScreenSpaceAmbientOcclusion ssao = null;
            foreach (var existing in rendererData.rendererFeatures)
                if (existing is ScreenSpaceAmbientOcclusion found) ssao = found;

            if (ssao == null)
            {
                ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = "Anatomy Ambient Occlusion";
                AssetDatabase.AddObjectToAsset(ssao, rendererData);
                rendererData.rendererFeatures.Add(ssao);
            }

            var serialized = new SerializedObject(ssao);
            SerializedProperty settings = serialized.FindProperty("m_Settings");
            void Set(string field, float value) { var p = settings.FindPropertyRelative(field); if (p != null) p.floatValue = value; }
            void SetInt(string field, int value) { var p = settings.FindPropertyRelative(field); if (p != null) p.intValue = value; }
            void SetBool(string field, bool value) { var p = settings.FindPropertyRelative(field); if (p != null) p.boolValue = value; }

            SetBool("AfterOpaque", true);      // multiply the finished image, so custom shaders are darkened too
            SetInt("Source", 1);               // depth + normals
            SetInt("Samples", 0);              // high quality
            SetInt("BlurQuality", 0);
            Set("Intensity", 1.2f);
            Set("Radius", 0.05f);
            Set("Falloff", 60f);
            Set("DirectLightingStrength", 0.25f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ssao.SetActive(true);
            EditorUtility.SetDirty(rendererData);
        }

        /// <summary>A near-black ink rather than pure black, and a line about a pixel
        /// wide, which is what a printed plate looks like; pure black at 3px reads as
        /// a cartoon cel-shade instead.</summary>
        private static void ApplyLookSettings(Material material)
        {
            material.SetColor("_OutlineColor", new Color(0.03f, 0.03f, 0.05f));
            material.SetFloat("_Thickness", 1.0f);
            material.SetFloat("_DepthThreshold", 0.006f);
            material.SetFloat("_NormalThreshold", 0.55f);
            material.SetFloat("_Strength", 0.9f);
        }
    }
}
