using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 19: when an organ is focused, ghosts every other renderer in the scene
    /// by lerping albedo alpha and emission to near-zero via a MaterialPropertyBlock
    /// (no material instancing/allocation), leaving the focused object at full
    /// opacity.
    /// </summary>
    public class FocusModeController : MonoBehaviour
    {
        [SerializeField] private float ghostAlpha = 0.15f;
        [SerializeField] private float transitionDuration = 0.5f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        private readonly Dictionary<Renderer, MaterialPropertyBlock> _blocks = new Dictionary<Renderer, MaterialPropertyBlock>();
        private Coroutine _activeTransition;

        public void FocusOnly(GameObject focused)
        {
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);

            if (_activeTransition != null) StopCoroutine(_activeTransition);
            _activeTransition = StartCoroutine(TransitionAll(allRenderers, focused));
        }

        public void ClearFocus()
        {
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            if (_activeTransition != null) StopCoroutine(_activeTransition);
            _activeTransition = StartCoroutine(TransitionAll(allRenderers, null, forceFullOpacity: true));
        }

        private IEnumerator TransitionAll(Renderer[] renderers, GameObject focused, bool forceFullOpacity = false)
        {
            var startAlphas = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                startAlphas[i] = GetCurrentAlpha(renderers[i]);
            }

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);

                for (int i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    if (renderer == null) continue;

                    bool isFocused = focused != null && renderer.transform.IsChildOf(focused.transform);
                    float targetAlpha = forceFullOpacity || isFocused || focused == null ? 1f : ghostAlpha;

                    ApplyAlpha(renderer, Mathf.Lerp(startAlphas[i], targetAlpha, t));
                }

                yield return null;
            }
        }

        private float GetCurrentAlpha(Renderer renderer)
        {
            if (!_blocks.TryGetValue(renderer, out var block))
            {
                block = new MaterialPropertyBlock();
                _blocks[renderer] = block;
            }

            renderer.GetPropertyBlock(block);
            return block.HasFloat(AlphaId) ? block.GetFloat(AlphaId) : 1f;
        }

        private void ApplyAlpha(Renderer renderer, float alpha)
        {
            if (!_blocks.TryGetValue(renderer, out var block))
            {
                block = new MaterialPropertyBlock();
                _blocks[renderer] = block;
            }

            renderer.GetPropertyBlock(block);
            block.SetFloat(AlphaId, alpha);

            Color baseColor = renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(BaseColorId)
                ? renderer.sharedMaterial.GetColor(BaseColorId)
                : Color.white;
            baseColor.a = alpha;
            block.SetColor(BaseColorId, baseColor);
            block.SetColor(EmissionColorId, Color.black * alpha);

            renderer.SetPropertyBlock(block);
        }
    }
}
