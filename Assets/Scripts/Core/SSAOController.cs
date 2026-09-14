using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 96: maps camera zoom distance to SSAO radius/intensity - softer,
    /// larger-radius shadows up close (e.g. brain sulci), sharper/tighter from
    /// far away. URP's ScreenSpaceAmbientOcclusion renderer feature doesn't
    /// expose its Radius/Intensity settings as a public runtime API, so this
    /// reaches them via reflection on the feature's serialized settings field -
    /// documented here as a fragile integration point that may break across URP
    /// versions, rather than hidden behind a clean-looking public API.
    /// </summary>
    public class SSAOController : MonoBehaviour
    {
        [SerializeField] private ScriptableRendererFeature ssaoFeature;
        [SerializeField] private float closeDistance = 0.1f;
        [SerializeField] private float farDistance = 5f;
        [SerializeField] private float closeRadius = 0.8f;
        [SerializeField] private float farRadius = 0.2f;
        [SerializeField] private float closeIntensity = 0.5f;
        [SerializeField] private float farIntensity = 1f;

        private FieldInfo _settingsField;
        private FieldInfo _radiusField;
        private FieldInfo _intensityField;

        private void Awake()
        {
            if (ssaoFeature == null) return;

            var featureType = ssaoFeature.GetType();
            _settingsField = featureType.GetField("m_Settings", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_settingsField == null) return;

            var settingsType = _settingsField.FieldType;
            _radiusField = settingsType.GetField("Radius", BindingFlags.Public | BindingFlags.Instance);
            _intensityField = settingsType.GetField("Intensity", BindingFlags.Public | BindingFlags.Instance);
        }

        public void UpdateForZoomDistance(float zoomDistance)
        {
            float t = ComputeCloseness(zoomDistance, closeDistance, farDistance);
            float radius = Mathf.Lerp(farRadius, closeRadius, t);
            float intensity = Mathf.Lerp(farIntensity, closeIntensity, t);

            ApplySettings(radius, intensity);
        }

        /// <summary>Exposed for tests: the distance-to-closeness mapping without needing a live renderer feature.</summary>
        public static float ComputeCloseness(float distance, float closeDistance, float farDistance)
        {
            return 1f - Mathf.Clamp01(Mathf.InverseLerp(closeDistance, farDistance, distance));
        }

        private void ApplySettings(float radius, float intensity)
        {
            if (ssaoFeature == null || _settingsField == null) return;

            object settings = _settingsField.GetValue(ssaoFeature);
            if (settings == null) return;

            _radiusField?.SetValue(settings, radius);
            _intensityField?.SetValue(settings, intensity);
            _settingsField.SetValue(ssaoFeature, settings);
        }
    }
}
