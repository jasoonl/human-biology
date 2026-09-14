using System.Collections;
using UnityEngine;

namespace HumanBodyExplorer.CameraSystem
{
    /// <summary>
    /// Phase 14: computes a composite bounds for a GameObject's renderers and eases
    /// the orbital camera's zoom distance to frame it, using the standard
    /// bounds-extents / sin(halfFOV) framing formula.
    /// </summary>
    public class CameraFocusTargeter : MonoBehaviour
    {
        [SerializeField] private AdvancedOrbitalCamera orbitalCamera;
        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private float focusDuration = 1.5f;

        private Coroutine _activeFocus;

        public void FocusOn(GameObject go)
        {
            if (go == null || orbitalCamera == null || targetCamera == null) return;

            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float fovRadians = targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float distance = bounds.extents.magnitude / Mathf.Max(0.0001f, Mathf.Sin(fovRadians));

            orbitalCamera.Target = bounds.center == Vector3.zero ? go.transform : orbitalCamera.Target;

            if (_activeFocus != null) StopCoroutine(_activeFocus);
            _activeFocus = StartCoroutine(EaseZoomTo(distance));
        }

        private IEnumerator EaseZoomTo(float targetDistance)
        {
            float startDistance = orbitalCamera.ZoomDistance;
            float elapsed = 0f;

            while (elapsed < focusDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / focusDuration);
                float eased = EaseInOutQuad(t);
                orbitalCamera.SetZoomDistanceImmediate(Mathf.Lerp(startDistance, targetDistance, eased));
                yield return null;
            }

            orbitalCamera.SetZoomDistanceImmediate(targetDistance);
        }

        private static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }
    }
}
