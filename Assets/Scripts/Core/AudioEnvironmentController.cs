using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 58: linecasts from the camera to a heart-mounted AudioSource, counting
    /// intervening tissue layers on the "Anatomy_Macro" layer, and muffles the
    /// sound proportionally via an AudioLowPassFilter.
    /// </summary>
    [RequireComponent(typeof(AudioLowPassFilter))]
    public class AudioEnvironmentController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera listenerCamera;
        [SerializeField] private Transform audioSourceTransform;
        [SerializeField] private LayerMask tissueLayerMask;
        [SerializeField] private float cutoffPerLayer = 1500f;
        [SerializeField] private float maxCutoffFrequency = 22000f;
        [SerializeField] private float minCutoffFrequency = 500f;

        private AudioLowPassFilter _lowPassFilter;
        private RaycastHit[] _hitBuffer = new RaycastHit[16];

        private void Awake()
        {
            _lowPassFilter = GetComponent<AudioLowPassFilter>();
        }

        private void Update()
        {
            if (listenerCamera == null || audioSourceTransform == null) return;

            int layerCount = CountTissueLayers(listenerCamera.transform.position, audioSourceTransform.position,
                tissueLayerMask, _hitBuffer);

            _lowPassFilter.cutoffFrequency = ComputeCutoff(layerCount, maxCutoffFrequency, cutoffPerLayer, minCutoffFrequency);
        }

        private static int CountTissueLayers(Vector3 from, Vector3 to, LayerMask mask, RaycastHit[] buffer)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;
            if (distance < 0.0001f) return 0;

            return Physics.RaycastNonAlloc(from, direction.normalized, buffer, distance, mask);
        }

        /// <summary>Exposed for tests: pure cutoff-frequency math.</summary>
        public static float ComputeCutoff(int layerCount, float maxCutoff, float perLayerReduction, float minCutoff)
        {
            return Mathf.Max(minCutoff, maxCutoff - layerCount * perLayerReduction);
        }
    }
}
