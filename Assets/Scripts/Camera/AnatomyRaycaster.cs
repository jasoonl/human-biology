using System;
using HumanBodyExplorer.Core;
using UnityEngine;

namespace HumanBodyExplorer.CameraSystem
{
    /// <summary>
    /// Phase 15: non-allocating physics raycaster. Casts through a fixed-size result
    /// buffer, sorts hits by distance, skips renderers that are mostly transparent
    /// (ghosted by FocusModeController), and fires OnNodeSelected with the resolved
    /// AnatomyNode EntityID.
    /// </summary>
    public class AnatomyRaycaster : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera sourceCamera;
        [SerializeField] private LayerMask anatomyLayerMask = ~0;
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private int maxHits = 16;
        [SerializeField] private float alphaVisibilityThreshold = 0.2f;

        private RaycastHit[] _resultsBuffer;
        private static readonly int AlphaPropertyId = Shader.PropertyToID("_Alpha");

        public static event Action<string> OnNodeSelected;

        /// <summary>Explicit camera override, mainly for tests where relying on the
        /// Camera.main tag lookup is unreliable across fixtures sharing a Play session.</summary>
        public UnityEngine.Camera SourceCamera
        {
            get => sourceCamera;
            set => sourceCamera = value;
        }

        private void Awake()
        {
            _resultsBuffer = new RaycastHit[Mathf.Max(1, maxHits)];
            if (sourceCamera == null) sourceCamera = UnityEngine.Camera.main;
        }

        public void ExecuteClick(Vector2 screenPosition)
        {
            if (sourceCamera == null) return;

            Ray ray = sourceCamera.ScreenPointToRay(screenPosition);
            TryRaycast(ray);
        }

        /// <summary>Exposed for automated tests, which can't synthesize a real click.</summary>
        public bool TryRaycast(Ray ray)
        {
            int hitCount = Physics.RaycastNonAlloc(ray, _resultsBuffer, maxDistance, anatomyLayerMask);
            if (hitCount <= 0) return false;

            Array.Sort(_resultsBuffer, 0, hitCount, DistanceComparer.Instance);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _resultsBuffer[i];
                var renderer = hit.collider.GetComponentInParent<Renderer>();

                if (renderer != null && IsMostlyTransparent(renderer)) continue;

                var nodeRef = hit.collider.GetComponentInParent<AnatomyNodeReference>();
                if (nodeRef != null && !string.IsNullOrEmpty(nodeRef.EntityId))
                {
                    OnNodeSelected?.Invoke(nodeRef.EntityId);
                    return true;
                }
            }

            return false;
        }

        private bool IsMostlyTransparent(Renderer renderer)
        {
            if (!renderer.sharedMaterial.HasProperty(AlphaPropertyId)) return false;
            return renderer.sharedMaterial.GetFloat(AlphaPropertyId) < alphaVisibilityThreshold;
        }

        private sealed class DistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly DistanceComparer Instance = new DistanceComparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
