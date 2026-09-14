using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 27: monitors a joint's hinge angle and drives a "_BulgeAmount"
    /// material property, paired with MuscleBulge.shader's vertex-color-masked
    /// normal displacement (fake soft-body bicep bulge).
    /// </summary>
    public class MuscleBulgeController : MonoBehaviour
    {
        [SerializeField] private Transform upperSegment;
        [SerializeField] private Transform lowerSegment;
        [SerializeField] private Transform jointPivot;
        [SerializeField] private Renderer muscleRenderer;
        [SerializeField] private float straightAngle = 180f;
        [SerializeField] private float fullyFlexedAngle = 30f;

        private static readonly int BulgeAmountId = Shader.PropertyToID("_BulgeAmount");
        private MaterialPropertyBlock _block;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (upperSegment == null || lowerSegment == null || jointPivot == null) return;

            float bulge = ComputeBulge();
            ApplyBulge(bulge);
        }

        public float ComputeBulge()
        {
            Vector3 toUpper = upperSegment.position - jointPivot.position;
            Vector3 toLower = lowerSegment.position - jointPivot.position;
            float angle = Vector3.Angle(toUpper, toLower);

            return ComputeBulgeFromAngle(angle);
        }

        /// <summary>Exposed separately so tests can verify the mapping without a live rig.</summary>
        public float ComputeBulgeFromAngle(float angleDegrees)
        {
            float t = Mathf.InverseLerp(straightAngle, fullyFlexedAngle, angleDegrees);
            return Mathf.Clamp01(t);
        }

        private void ApplyBulge(float bulge)
        {
            if (muscleRenderer == null) return;

            muscleRenderer.GetPropertyBlock(_block);
            _block.SetFloat(BulgeAmountId, bulge);
            muscleRenderer.SetPropertyBlock(_block);
        }
    }
}
