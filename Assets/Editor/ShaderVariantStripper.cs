using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// Phase 94: strips shader variants compiled for Forward Add/Point/Spot
    /// lights at build time, since this project uses a single Directional light
    /// + IBL only, cutting shader build time.
    /// </summary>
    public class ShaderVariantStripper : IPreprocessShaders
    {
        public int callbackOrder => 0;

        private static readonly ShaderKeyword AdditionalLightsKeyword = new ShaderKeyword("_ADDITIONAL_LIGHTS");
        private static readonly ShaderKeyword AdditionalLightShadowsKeyword = new ShaderKeyword("_ADDITIONAL_LIGHT_SHADOWS");

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            for (int i = data.Count - 1; i >= 0; i--)
            {
                if (ShouldStrip(data[i].shaderKeywordSet))
                {
                    data.RemoveAt(i);
                }
            }
        }

        /// <summary>Exposed for tests: the strip decision without needing a real build pass.</summary>
        public static bool ShouldStrip(ShaderKeywordSet keywordSet)
        {
            return keywordSet.IsEnabled(AdditionalLightsKeyword) || keywordSet.IsEnabled(AdditionalLightShadowsKeyword);
        }
    }
}
