using UnityEngine;

namespace HumanBodyExplorer.XR
{
    // Module VI (Phases 71-75) - STUB ONLY. See Planning.md M6.
    //
    // No XR headset, no com.unity.xr.interaction.toolkit package, and no OpenXR
    // runtime is available in this environment, so none of this can be exercised
    // even in the Editor (XR simulation still needs the XR Interaction Toolkit
    // package). These document the intended integration surface only.

    /// <summary>Phase 71: intended XRBaseInteractable-driven grab/throw via FixedJoint.</summary>
    public class HandTrackingGrabInteractable : MonoBehaviour
    {
        [SerializeField] private Rigidbody targetRigidbody;

        public void OnSelectEntered(Vector3 interactorVelocity, Vector3 interactorAngularVelocity)
        {
            Debug.LogWarning("[HandTrackingGrabInteractable] No XR Interaction Toolkit installed; grab is a no-op.");
        }

        public void OnSelectExited(Vector3 releaseVelocity, Vector3 releaseAngularVelocity)
        {
            if (targetRigidbody == null) return;
            targetRigidbody.linearVelocity = releaseVelocity;
            targetRigidbody.angularVelocity = releaseAngularVelocity;
        }
    }

    /// <summary>Phase 72: intended OpenXR spatial anchor persistence via a saved UUID.</summary>
    public class SpatialAnchorManager : MonoBehaviour
    {
        private const string AnchorUuidKey = "HBE_SpatialAnchorUuid";

        public bool HasSavedAnchor => Core.SecurePrefs.HasKey(AnchorUuidKey);

        public void SaveAnchor(string uuid)
        {
            Core.SecurePrefs.SetString(AnchorUuidKey, uuid);
        }

        public string LoadAnchorUuid() => Core.SecurePrefs.GetString(AnchorUuidKey, string.Empty);
    }

    /// <summary>Phase 73: intended XRBaseController haptic impulses.</summary>
    public class HapticFeedbackManager : MonoBehaviour
    {
        public void PlayHeartbeatHaptic(float amplitude, float duration)
        {
            Debug.LogWarning("[HapticFeedbackManager] No XR controller connected; haptic impulse is a no-op.");
        }

        public void PlayScalpelHaptic()
        {
            Debug.LogWarning("[HapticFeedbackManager] No XR controller connected; haptic impulse is a no-op.");
        }
    }

    /// <summary>Phase 74: intended Fixed Foveated Rendering level control via XR APIs.</summary>
    public class EyeTrackingOptimizer : MonoBehaviour
    {
        public void EnableHighFoveation()
        {
            Debug.LogWarning("[EyeTrackingOptimizer] No XR device connected; FFR level cannot be set.");
        }
    }

    /// <summary>Phase 75: intended headset depth-API occlusion mesh generation.</summary>
    public class AROcclusionManager : MonoBehaviour
    {
        public bool TryGenerateOcclusionMesh(out Mesh occlusionMesh)
        {
            occlusionMesh = null;
            Debug.LogWarning("[AROcclusionManager] No AR depth API available on this platform.");
            return false;
        }
    }
}
