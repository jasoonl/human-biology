using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 95: drops the target frame rate to 30 after 5 seconds of no input,
    /// restoring it immediately on the next touch/head movement, to save battery
    /// on mobile/Quest.
    /// </summary>
    public class BatteryOptimizer : MonoBehaviour
    {
        [SerializeField] private float idleTimeoutSeconds = 5f;
        [SerializeField] private int idleFrameRate = 30;
        [SerializeField] private int activeFrameRate = 90;

        private float _idleTimer;
        private bool _isIdle;

        public void NotifyInputReceived()
        {
            _idleTimer = 0f;
            if (_isIdle)
            {
                _isIdle = false;
                Application.targetFrameRate = activeFrameRate;
            }
        }

        private void Update()
        {
            _idleTimer += Time.unscaledDeltaTime;

            if (!_isIdle && _idleTimer >= idleTimeoutSeconds)
            {
                _isIdle = true;
                Application.targetFrameRate = idleFrameRate;
            }
        }

        /// <summary>Exposed for tests: the idle-transition decision without needing Application.targetFrameRate side effects.</summary>
        public static bool ShouldGoIdle(float idleTimer, float idleTimeoutSeconds) => idleTimer >= idleTimeoutSeconds;
    }
}
