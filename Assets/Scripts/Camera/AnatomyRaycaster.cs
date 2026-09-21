using System;
using System.Collections.Generic;
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

        /// <summary>A surface you can see through should not intercept a click meant
        /// for the anatomy behind it. Anything less opaque than this counts as
        /// see-through (the translucent skin shell sits at ~0.42).</summary>
        [SerializeField] private float clickThroughAlphaThreshold = 0.9f;

        private RaycastHit[] _resultsBuffer;
        private readonly List<string> _idsUnderCursor = new List<string>();
        private static readonly int AlphaPropertyId = Shader.PropertyToID("_Alpha");
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

        public static event Action<string> OnNodeSelected;

        /// <summary>Every distinct anatomy id along the ray, nearest first. The quiz
        /// scores against this rather than the single frontmost hit, so a structure
        /// that sits behind skin, muscle or bone is still answerable by clicking over
        /// it - otherwise deep questions would be impossible to get right.</summary>
        public static event Action<IReadOnlyList<string>> OnNodesUnderCursor;

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

            _idsUnderCursor.Clear();
            string seeThroughFallbackId = null;
            string selectedId = null;

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _resultsBuffer[i];
                var nodeRef = hit.collider.GetComponentInParent<AnatomyNodeReference>();
                if (nodeRef == null || string.IsNullOrEmpty(nodeRef.EntityId)) continue;

                if (!_idsUnderCursor.Contains(nodeRef.EntityId)) _idsUnderCursor.Add(nodeRef.EntityId);

                // Keep walking the ray after a selection is found, so the full list of
                // structures under the cursor is still reported.
                if (selectedId != null) continue;

                var renderer = hit.collider.GetComponentInParent<Renderer>();
                if (renderer != null && IsSeeThrough(renderer))
                {
                    // The skin shell encloses the whole body, so it is the first thing
                    // every ray meets. Since you can see the organs through it, a click
                    // should reach them - but keep the first see-through hit so that
                    // clicking bare skin with nothing underneath still selects skin.
                    seeThroughFallbackId ??= nodeRef.EntityId;
                    continue;
                }

                selectedId = nodeRef.EntityId;
            }

            selectedId ??= seeThroughFallbackId;
            OnNodesUnderCursor?.Invoke(_idsUnderCursor);

            if (selectedId == null) return false;

            OnNodeSelected?.Invoke(selectedId);
            return true;
        }

        private bool IsSeeThrough(Renderer renderer)
        {
            var material = renderer.sharedMaterial;
            if (material == null) return false;

            // Module III's ghosting shaders expose a dedicated _Alpha slider...
            if (material.HasProperty(AlphaPropertyId) &&
                material.GetFloat(AlphaPropertyId) < alphaVisibilityThreshold)
            {
                return true;
            }

            // ...whereas a URP Lit material switched to Transparent carries its
            // opacity in the alpha channel of _BaseColor.
            return material.HasProperty(BaseColorPropertyId) &&
                   material.GetColor(BaseColorPropertyId).a < clickThroughAlphaThreshold;
        }

        private sealed class DistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly DistanceComparer Instance = new DistanceComparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
