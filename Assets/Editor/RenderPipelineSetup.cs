using System.IO;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// Creates a URP Pipeline Asset + Universal Renderer and assigns them as the
    /// project's active render pipeline (Graphics Settings + all Quality levels).
    /// The URP package was installed from the start, but nothing had ever created
    /// and assigned an actual pipeline asset, so the project was silently still
    /// running on the deprecated Built-in Render Pipeline.
    /// </summary>
    public static class RenderPipelineSetup
    {
        private const string SettingsFolder = "Assets/Settings";
        private const string RendererPath = SettingsFolder + "/HBE_UniversalRenderer.asset";
        private const string PipelineAssetPath = SettingsFolder + "/HBE_URPAsset.asset";

        [MenuItem("Human Body Explorer/Create And Assign URP Asset")]
        public static void CreateAndAssignUrpAsset()
        {
            Directory.CreateDirectory(SettingsFolder);

            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);

            var pipelineAsset = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            int levelCount = QualitySettings.names.Length;
            for (int i = 0; i < levelCount; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipelineAsset;
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[RenderPipelineSetup] Assigned '{PipelineAssetPath}' as the default render pipeline " +
                      $"and across {levelCount} quality level(s).");
        }
    }
}
