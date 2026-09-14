using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 51: deforms a skinned mesh's blend shape and lerps its base color
    /// from healthy to diseased as pathology severity (0-100) increases.
    /// </summary>
    public class DiseaseProgressionManager : MonoBehaviour
    {
        [SerializeField] private Color healthyColor = new Color(0.85f, 0.55f, 0.5f);
        [SerializeField] private Color diseasedColor = new Color(0.55f, 0.45f, 0.15f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly Dictionary<string, (SkinnedMeshRenderer skinned, int blendShapeIndex, Renderer colorRenderer)> _registry
            = new Dictionary<string, (SkinnedMeshRenderer, int, Renderer)>();

        public void RegisterNode(string nodeId, SkinnedMeshRenderer skinnedMesh, int blendShapeIndex, Renderer colorRenderer)
        {
            _registry[nodeId] = (skinnedMesh, blendShapeIndex, colorRenderer);
        }

        public void SetPathologySeverity(string nodeId, float severity0To100)
        {
            severity0To100 = Mathf.Clamp(severity0To100, 0f, 100f);
            if (!_registry.TryGetValue(nodeId, out var entry)) return;

            if (entry.skinned != null && entry.blendShapeIndex >= 0)
            {
                entry.skinned.SetBlendShapeWeight(entry.blendShapeIndex, severity0To100);
            }

            if (entry.colorRenderer != null)
            {
                var block = new MaterialPropertyBlock();
                entry.colorRenderer.GetPropertyBlock(block);
                Color lerped = Color.Lerp(healthyColor, diseasedColor, severity0To100 / 100f);
                block.SetColor(BaseColorId, lerped);
                entry.colorRenderer.SetPropertyBlock(block);
            }
        }

        /// <summary>Exposed for tests: the color math without needing live renderers.</summary>
        public Color ComputeColorForSeverity(float severity0To100)
        {
            return Color.Lerp(healthyColor, diseasedColor, Mathf.Clamp01(severity0To100 / 100f));
        }
    }
}
