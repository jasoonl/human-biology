using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 50: procedural heartbeat math driving a "lub-dub" scale pulse and
    /// pitch-shifting a heartbeat AudioSource to match the target BPM.
    /// </summary>
    public class CardioAnimationController : MonoBehaviour
    {
        [SerializeField] private Transform heartTransform;
        [SerializeField] private AudioSource heartbeatAudioSource;
        [SerializeField] private float targetBPM = 70f;
        [SerializeField] private float basePitchBPM = 70f;

        public float TargetBPM
        {
            get => targetBPM;
            set
            {
                targetBPM = value;
                if (heartbeatAudioSource != null)
                {
                    heartbeatAudioSource.pitch = targetBPM / basePitchBPM;
                }
            }
        }

        /// <summary>Pure function so the waveform shape is unit-testable.</summary>
        public static float ComputeLubDubScale(float time, float bpm)
        {
            float phase = (time * bpm / 60f) * Mathf.PI * 2f;
            return Mathf.Max(0f, Mathf.Sin(phase) * 0.1f + Mathf.Sin(phase * 2f) * 0.05f);
        }

        private Vector3 _baseScale;

        private void Awake()
        {
            if (heartTransform != null) _baseScale = heartTransform.localScale;
        }

        private void Update()
        {
            if (heartTransform == null) return;

            float pulse = ComputeLubDubScale(Time.time, targetBPM);
            heartTransform.localScale = _baseScale * (1f + pulse);
        }
    }
}
