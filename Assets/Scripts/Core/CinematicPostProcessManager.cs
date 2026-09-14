using HumanBodyExplorer.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 32: as the camera pushes into macro-photography distances, ramps up
    /// Bloom intensity and Depth of Field blur for a wetter, more organic close-up
    /// presentation.
    /// </summary>
    public class CinematicPostProcessManager : MonoBehaviour
    {
        [SerializeField] private Volume volume;
        [SerializeField] private AdvancedOrbitalCamera orbitalCamera;
        [SerializeField] private float macroDistance = 0.1f;
        [SerializeField] private float normalDistance = 3f;
        [SerializeField] private float maxBloomIntensity = 2f;
        [SerializeField] private float maxFocusDistanceBlur = 0.05f;

        private Bloom _bloom;
        private DepthOfField _depthOfField;

        private void Awake()
        {
            if (volume == null || volume.profile == null) return;
            volume.profile.TryGet(out _bloom);
            volume.profile.TryGet(out _depthOfField);
        }

        private void Update()
        {
            if (orbitalCamera == null) return;

            float t = Mathf.InverseLerp(normalDistance, macroDistance, orbitalCamera.ZoomDistance);
            t = Mathf.Clamp01(t);

            if (_bloom != null)
            {
                _bloom.intensity.value = Mathf.Lerp(0f, maxBloomIntensity, t);
            }

            if (_depthOfField != null)
            {
                _depthOfField.focusDistance.value = Mathf.Max(0.01f, orbitalCamera.ZoomDistance);
                _depthOfField.gaussianMaxRadius.value = Mathf.Lerp(0.5f, maxFocusDistanceBlur * 100f, t);
            }
        }
    }
}
