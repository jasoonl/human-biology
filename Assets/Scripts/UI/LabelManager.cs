using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.UI
{
    /// <summary>
    /// Phase 49: keeps 3D-world-pinned UI labels from overlapping by nudging
    /// overlapping pairs apart in screen space each frame.
    /// </summary>
    public class LabelManager : MonoBehaviour
    {
        [SerializeField] private float repulsionForce = 50f;
        private readonly List<RectTransform> _activeLabels = new List<RectTransform>();

        public void RegisterLabel(RectTransform label)
        {
            if (!_activeLabels.Contains(label)) _activeLabels.Add(label);
        }

        public void UnregisterLabel(RectTransform label)
        {
            _activeLabels.Remove(label);
        }

        private void LateUpdate()
        {
            ResolveOverlaps(_activeLabels, repulsionForce, Time.deltaTime);
        }

        /// <summary>Exposed as a static pure function so overlap resolution is unit-testable.</summary>
        public static void ResolveOverlaps(IReadOnlyList<RectTransform> labels, float repulsionForce, float deltaTime)
        {
            for (int i = 0; i < labels.Count; i++)
            {
                for (int j = i + 1; j < labels.Count; j++)
                {
                    var a = labels[i];
                    var b = labels[j];
                    if (a == null || b == null) continue;

                    if (RectTransformUtility.RectangleContainsScreenPoint(a, b.position) ||
                        Overlaps(a, b))
                    {
                        Vector2 direction = ((Vector2)(a.position - b.position));
                        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.up;
                        direction.Normalize();

                        Vector3 push = (Vector3)(direction * repulsionForce * deltaTime);
                        a.position += push;
                        b.position -= push;
                    }
                }
            }
        }

        private static bool Overlaps(RectTransform a, RectTransform b)
        {
            Rect rectA = GetScreenRect(a);
            Rect rectB = GetScreenRect(b);
            return rectA.Overlaps(rectB);
        }

        private static Rect GetScreenRect(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }
    }
}
