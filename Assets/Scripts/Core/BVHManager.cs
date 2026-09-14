using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 16: lightweight bounding-volume hierarchy built from anatomy parts'
    /// world-space AABBs. Intended to prefilter a large candidate set (down from
    /// tens of thousands of parts) before a precise Physics.Raycast runs on the
    /// much smaller surviving subset. A simple median-split binary tree is enough
    /// at the object-count this project actually reaches with placeholder assets;
    /// it is not a substitute for engine-level physics broadphase, just a coarse
    /// candidate-reduction pass tailored to "anatomy system" groupings.
    /// </summary>
    public class BVHManager
    {
        private class Node
        {
            public Bounds Bounds;
            public Node Left;
            public Node Right;
            public GameObject Leaf;
        }

        private Node _root;

        public void Build(IReadOnlyList<GameObject> anatomyParts)
        {
            var entries = new List<(Bounds bounds, GameObject go)>(anatomyParts.Count);
            foreach (var go in anatomyParts)
            {
                if (go == null) continue;
                var renderer = go.GetComponentInChildren<Renderer>();
                if (renderer == null) continue;
                entries.Add((renderer.bounds, go));
            }

            _root = BuildRecursive(entries);
        }

        private Node BuildRecursive(List<(Bounds bounds, GameObject go)> entries)
        {
            if (entries.Count == 0) return null;

            if (entries.Count == 1)
            {
                return new Node { Bounds = entries[0].bounds, Leaf = entries[0].go };
            }

            Bounds combined = entries[0].bounds;
            for (int i = 1; i < entries.Count; i++) combined.Encapsulate(entries[i].bounds);

            Vector3 size = combined.size;
            int axis = size.x >= size.y && size.x >= size.z ? 0 : (size.y >= size.z ? 1 : 2);

            entries.Sort((a, b) => a.bounds.center[axis].CompareTo(b.bounds.center[axis]));

            int mid = entries.Count / 2;
            var leftEntries = entries.GetRange(0, mid);
            var rightEntries = entries.GetRange(mid, entries.Count - mid);

            var node = new Node { Bounds = combined };
            node.Left = BuildRecursive(leftEntries);
            node.Right = BuildRecursive(rightEntries);
            return node;
        }

        /// <summary>Returns candidate GameObjects whose bounds the ray intersects.</summary>
        public List<GameObject> Query(Ray ray, float maxDistance)
        {
            var results = new List<GameObject>();
            QueryRecursive(_root, ray, maxDistance, results);
            return results;
        }

        private void QueryRecursive(Node node, Ray ray, float maxDistance, List<GameObject> results)
        {
            if (node == null) return;
            if (!node.Bounds.IntersectRay(ray, out float hitDistance) || hitDistance > maxDistance) return;

            if (node.Leaf != null)
            {
                results.Add(node.Leaf);
                return;
            }

            QueryRecursive(node.Left, ray, maxDistance, results);
            QueryRecursive(node.Right, ray, maxDistance, results);
        }

        public int LeafCount() => CountRecursive(_root);

        private static int CountRecursive(Node node)
        {
            if (node == null) return 0;
            if (node.Leaf != null) return 1;
            return CountRecursive(node.Left) + CountRecursive(node.Right);
        }
    }
}
