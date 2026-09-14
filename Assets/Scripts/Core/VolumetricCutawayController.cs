using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 22: pushes a draggable cutaway sphere's world position and radius to
    /// global shader properties. The paired HLSL fragment logic (any shader opting
    /// in) discards pixels within the sphere, revealing interior structures without
    /// a flat slicing plane.
    /// </summary>
    public class VolumetricCutawayController : MonoBehaviour
    {
        [SerializeField] private Transform cutawaySphere;
        [SerializeField] private float radius = 0.1f;

        private static readonly int CutawayCenterId = Shader.PropertyToID("_CutawaySphereCenter");
        private static readonly int CutawayRadiusId = Shader.PropertyToID("_CutawaySphereRadius");

        public bool IsActive { get; private set; }

        public void SetActive(bool active)
        {
            IsActive = active;
            Shader.SetGlobalFloat("_CutawayActive", active ? 1f : 0f);
        }

        public void SetRadius(float value)
        {
            radius = Mathf.Max(0f, value);
        }

        private void Update()
        {
            if (!IsActive || cutawaySphere == null) return;
            PushToShader();
        }

        public void PushToShader()
        {
            if (cutawaySphere == null) return;
            Shader.SetGlobalVector(CutawayCenterId, cutawaySphere.position);
            Shader.SetGlobalFloat(CutawayRadiusId, radius);
        }
    }
}
