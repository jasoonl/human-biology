using UnityEngine;
using UnityEngine.UI;

namespace HumanBodyExplorer.UI
{
    /// <summary>
    /// Phase 20: a secondary orthographic camera renders a wireframe-proxy "Minimap"
    /// layer to a RenderTexture shown on a UI RawImage, with a cone icon indicating
    /// the main camera's position/heading relative to the body bounds.
    /// </summary>
    public class MinimapController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera minimapCamera;
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private RawImage minimapDisplay;
        [SerializeField] private RectTransform headingConeIcon;
        [SerializeField] private Bounds bodyBounds = new Bounds(Vector3.zero, Vector3.one * 2f);
        [SerializeField] private int renderTextureSize = 256;

        private RenderTexture _renderTexture;

        private void Awake()
        {
            if (minimapCamera == null) return;

            _renderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16);
            minimapCamera.targetTexture = _renderTexture;
            minimapCamera.orthographic = true;
            minimapCamera.cullingMask = LayerMask.GetMask("Minimap");

            if (minimapDisplay != null) minimapDisplay.texture = _renderTexture;
        }

        private void LateUpdate()
        {
            if (mainCamera == null || headingConeIcon == null) return;

            Vector3 relative = mainCamera.transform.position - bodyBounds.center;
            Vector2 normalized = new Vector2(
                Mathf.Clamp(relative.x / Mathf.Max(0.0001f, bodyBounds.extents.x), -1f, 1f),
                Mathf.Clamp(relative.z / Mathf.Max(0.0001f, bodyBounds.extents.z), -1f, 1f));

            headingConeIcon.anchoredPosition = normalized * (renderTextureSize * 0.5f);

            float heading = mainCamera.transform.eulerAngles.y;
            headingConeIcon.localRotation = Quaternion.Euler(0f, 0f, -heading);
        }

        private void OnDestroy()
        {
            if (_renderTexture != null) _renderTexture.Release();
        }
    }
}
