using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 28 (stub only - see Planning.md M3 scope note). The spec calls for
    /// ML-Agents inference over a pre-trained ONNX tissue-tearing model driven by
    /// virtual-scalpel contact forces. That needs a trained model file and the
    /// com.unity.ml-agents package, neither of which exist in this environment, and
    /// there is no way to validate FEM deformation correctness without a real
    /// surgical dataset. This class defines the intended integration surface so a
    /// future session can wire in a real model without redesigning the call sites,
    /// but SimulateContact() intentionally does not perform any deformation yet.
    /// </summary>
    public class SoftBodyDeformer : MonoBehaviour
    {
        [SerializeField] private bool modelLoaded = false;

        public bool IsModelLoaded => modelLoaded;

        /// <summary>
        /// Intended signature: (contactPoint, contactForce) -> localized spring-mass
        /// mesh deformation via the ONNX model. Currently a no-op; logs a warning
        /// once so callers notice this path isn't implemented rather than silently
        /// doing nothing.
        /// </summary>
        public void SimulateContact(Vector3 contactPoint, Vector3 contactForce)
        {
            if (!modelLoaded)
            {
                Debug.LogWarning("[SoftBodyDeformer] No ONNX tissue model loaded - " +
                                  "FEM soft-body deformation (Phase 28) is unimplemented in this environment.");
            }
        }
    }
}
