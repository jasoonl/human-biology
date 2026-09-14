using System.Collections;
using HumanBodyExplorer.Core;
using UnityEngine;

namespace HumanBodyExplorer.MicroSim
{
    /// <summary>
    /// Phase 39: eases the transition from macro to microscopic scale - fades to
    /// white, relocates the camera into a dedicated "micro scene" of upscaled
    /// models, tightens the near clip plane, and unloads the macro Addressables
    /// group to reclaim memory.
    /// </summary>
    public class ScaleDiveController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private Transform microSceneOrigin;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float fadeDuration = 1.5f;
        [SerializeField] private float microNearClipPlane = 0.01f;

        private float _macroNearClipPlane;

        public IEnumerator DiveToMicro(string macroAddressableKey, IAssetStreamingManager assetStreamingManager)
        {
            if (mainCamera != null) _macroNearClipPlane = mainCamera.nearClipPlane;

            yield return Fade(0f, 1f);

            if (mainCamera != null && microSceneOrigin != null)
            {
                mainCamera.transform.SetPositionAndRotation(microSceneOrigin.position, microSceneOrigin.rotation);
                mainCamera.nearClipPlane = microNearClipPlane;
            }

            if (!string.IsNullOrEmpty(macroAddressableKey))
            {
                assetStreamingManager?.UnloadAnatomyGroup(macroAddressableKey);
            }

            yield return Fade(1f, 0f);
        }

        public IEnumerator ReturnToMacro()
        {
            yield return Fade(0f, 1f);

            if (mainCamera != null) mainCamera.nearClipPlane = _macroNearClipPlane;

            yield return Fade(1f, 0f);
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadeOverlay == null) yield break;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeOverlay.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }
            fadeOverlay.alpha = to;
        }
    }
}
