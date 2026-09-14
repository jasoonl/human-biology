using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 18: drives a draggable slicing plane and pushes its position/normal to
    /// global shader properties so any material's shader can clip against it
    /// (see Module III's Boolean Clipping HLSL node, Phase 24).
    /// </summary>
    public class RuntimeSlicer : MonoBehaviour
    {
        [SerializeField] private Transform slicePlane;

        private static readonly int SlicePlanePositionId = Shader.PropertyToID("_SlicePlanePosition");
        private static readonly int SlicePlaneNormalId = Shader.PropertyToID("_SlicePlaneNormal");

        public bool IsActive { get; private set; }

        public void SetActive(bool active)
        {
            IsActive = active;
            Shader.SetGlobalFloat("_SliceActive", active ? 1f : 0f);
        }

        private void Update()
        {
            if (!IsActive || slicePlane == null) return;
            PushToShader();
        }

        public void PushToShader()
        {
            if (slicePlane == null) return;
            Shader.SetGlobalVector(SlicePlanePositionId, slicePlane.position);
            Shader.SetGlobalVector(SlicePlaneNormalId, slicePlane.up);
        }

        public void SetSlicePlane(Transform plane)
        {
            slicePlane = plane;
        }
    }
}
