using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 17: caches each child's original local position and outward direction
    /// from the parent, then lerps them outward by a 0-1 slider.
    /// </summary>
    public class ExplodedViewController : MonoBehaviour
    {
        [SerializeField] private float maxExplosionDistance = 1f;

        private struct ChildState
        {
            public Transform Transform;
            public Vector3 OriginalLocalPosition;
            public Vector3 OutwardDirection;
        }

        private readonly List<ChildState> _children = new List<ChildState>();

        private void Awake()
        {
            CacheChildren();
        }

        public void CacheChildren()
        {
            _children.Clear();
            foreach (Transform child in transform)
            {
                Vector3 outward = (child.position - transform.position);
                outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : Vector3.up;

                _children.Add(new ChildState
                {
                    Transform = child,
                    OriginalLocalPosition = child.localPosition,
                    OutwardDirection = outward
                });
            }
        }

        public void UpdateExplosion(float slider0To1)
        {
            slider0To1 = Mathf.Clamp01(slider0To1);

            for (int i = 0; i < _children.Count; i++)
            {
                var state = _children[i];
                if (state.Transform == null) continue;

                Vector3 exploded = state.OriginalLocalPosition + state.OutwardDirection * maxExplosionDistance;
                state.Transform.localPosition = Vector3.Lerp(state.OriginalLocalPosition, exploded, slider0To1);
            }
        }

        public int CachedChildCount => _children.Count;
    }
}
