using UnityEngine;
using UnityEngine.Profiling;

namespace HumanBodyExplorer.UI
{
    /// <summary>
    /// Phase 62: on-screen FPS/memory overlay for developer auditing, flashing red
    /// when memory exceeds 2GB or frame time exceeds 11ms (roughly sub-90fps).
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [SerializeField] private long memoryWarningThresholdBytes = 2L * 1024 * 1024 * 1024;
        [SerializeField] private float frameTimeWarningThresholdMs = 11f;
        [SerializeField] private int rollingWindowSize = 30;

        private float[] _frameTimes;
        private int _frameIndex;
        private int _framesRecorded;

        public float CurrentFPS { get; private set; }
        public long CurrentMemoryBytes { get; private set; }
        public bool IsOverBudget { get; private set; }

        private void Awake()
        {
            _frameTimes = new float[Mathf.Max(1, rollingWindowSize)];
        }

        private void Update()
        {
            _frameTimes[_frameIndex] = Time.unscaledDeltaTime;
            _frameIndex = (_frameIndex + 1) % _frameTimes.Length;
            _framesRecorded = Mathf.Min(_framesRecorded + 1, _frameTimes.Length);

            float averageDeltaTime = ComputeAverage(_frameTimes, _framesRecorded);
            CurrentFPS = averageDeltaTime > 0f ? 1f / averageDeltaTime : 0f;
            CurrentMemoryBytes = Profiler.GetTotalAllocatedMemoryLong();

            IsOverBudget = CurrentMemoryBytes > memoryWarningThresholdBytes ||
                           averageDeltaTime * 1000f > frameTimeWarningThresholdMs;
        }

        private static float ComputeAverage(float[] values, int count)
        {
            if (count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < count; i++) sum += values[i];
            return sum / count;
        }
    }
}
