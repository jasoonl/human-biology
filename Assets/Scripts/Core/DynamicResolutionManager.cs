using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 93: monitors a rolling average FPS window and drops URP's render
    /// scale on underpowered hardware, prioritizing frame rate over resolution.
    /// </summary>
    public class DynamicResolutionManager : MonoBehaviour
    {
        [SerializeField] private float targetFPS = 45f;
        [SerializeField] private float reducedRenderScale = 0.7f;
        [SerializeField] private float normalRenderScale = 1f;
        [SerializeField] private int windowSize = 60;

        private float[] _frameTimes;
        private int _index;
        private int _recorded;

        private void Awake()
        {
            _frameTimes = new float[Mathf.Max(1, windowSize)];
        }

        private void Update()
        {
            _frameTimes[_index] = Time.unscaledDeltaTime;
            _index = (_index + 1) % _frameTimes.Length;
            _recorded = Mathf.Min(_recorded + 1, _frameTimes.Length);

            float averageFPS = ComputeAverageFPS(_frameTimes, _recorded);
            ApplyRenderScale(averageFPS);
        }

        private void ApplyRenderScale(float averageFPS)
        {
            var urpAsset = UniversalRenderPipeline.asset;
            if (urpAsset == null) return;

            urpAsset.renderScale = averageFPS < targetFPS ? reducedRenderScale : normalRenderScale;
        }

        /// <summary>Exposed for tests: the averaging math without needing a live URP asset.</summary>
        public static float ComputeAverageFPS(float[] frameTimes, int count)
        {
            if (count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < count; i++) sum += frameTimes[i];
            float avgDeltaTime = sum / count;
            return avgDeltaTime > 0f ? 1f / avgDeltaTime : 0f;
        }
    }
}
