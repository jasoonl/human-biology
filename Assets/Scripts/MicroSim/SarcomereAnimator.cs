using UnityEngine;

namespace HumanBodyExplorer.MicroSim
{
    /// <summary>
    /// Phase 46: visually demonstrates the sliding-filament cross-bridge cycle -
    /// a calcium ion binds Troponin (shifting Tropomyosin), the myosin head
    /// attaches to actin, and an ATP molecule spawns to trigger the power stroke.
    /// Driven by a looping sine wave rather than real muscle physics.
    /// </summary>
    public class SarcomereAnimator : MonoBehaviour
    {
        [SerializeField] private Transform myosinHead;
        [SerializeField] private Transform calciumIon;
        [SerializeField] private Transform tropomyosin;
        [SerializeField] private GameObject atpMoleculePrefab;
        [SerializeField] private float cycleDuration = 1f;
        [SerializeField] private float powerStrokeAngle = 45f;
        [SerializeField] private float tropomyosinShiftDistance = 0.02f;

        private float _cycleTime;

        /// <summary>0-1 phase within the current contraction cycle, exposed for tests.</summary>
        public float ComputePhase(float time) => Mathf.Repeat(time / cycleDuration, 1f);

        private void Update()
        {
            _cycleTime += Time.deltaTime;
            float phase = ComputePhase(_cycleTime);

            if (calciumIon != null)
            {
                float bindProgress = Mathf.Clamp01(phase / 0.2f);
                calciumIon.localPosition = Vector3.Lerp(calciumIon.localPosition, Vector3.zero, bindProgress * Time.deltaTime * 5f);
            }

            if (tropomyosin != null)
            {
                float shiftT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((phase - 0.1f) / 0.2f));
                tropomyosin.localPosition = new Vector3(shiftT * tropomyosinShiftDistance, 0f, 0f);
            }

            if (myosinHead != null)
            {
                float strokeT = Mathf.Sin(phase * Mathf.PI * 2f) * 0.5f + 0.5f;
                myosinHead.localRotation = Quaternion.Euler(0f, 0f, strokeT * powerStrokeAngle);
            }

            if (phase < Time.deltaTime / cycleDuration && atpMoleculePrefab != null)
            {
                Instantiate(atpMoleculePrefab, transform.position, Quaternion.identity);
            }
        }
    }
}
