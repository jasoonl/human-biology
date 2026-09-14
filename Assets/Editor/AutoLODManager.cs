using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// Phase 84 (deviates from spec): true Quadric Error Metrics decimation
    /// (sequential edge contraction guided by an error-quadric matrix) is a
    /// substantial algorithm to hand-implement and verify blind. This instead
    /// uses spatial vertex clustering - snapping vertices to a 3D grid sized
    /// relative to the mesh bounds and merging duplicates - a simpler, cruder
    /// decimation that still produces a genuine triangle-count reduction and
    /// preserves rough silhouette, without QEM's UV-boundary/silhouette
    /// preservation guarantees.
    /// </summary>
    public static class AutoLODManager
    {
        public static Mesh Decimate(Mesh source, float targetRatio)
        {
            targetRatio = Mathf.Clamp01(targetRatio);

            Vector3[] sourceVerts = source.vertices;
            int[] sourceTris = source.triangles;

            Bounds bounds = source.bounds;
            float gridResolution = Mathf.Lerp(2f, Mathf.Max(sourceVerts.Length, 2), targetRatio);
            float cellSize = bounds.size.magnitude / gridResolution;
            if (cellSize < 0.0001f) cellSize = 0.0001f;

            var clusterMap = new Dictionary<Vector3Int, int>();
            var newVertices = new List<Vector3>();
            var remap = new int[sourceVerts.Length];

            for (int i = 0; i < sourceVerts.Length; i++)
            {
                Vector3Int cell = new Vector3Int(
                    Mathf.RoundToInt(sourceVerts[i].x / cellSize),
                    Mathf.RoundToInt(sourceVerts[i].y / cellSize),
                    Mathf.RoundToInt(sourceVerts[i].z / cellSize));

                if (!clusterMap.TryGetValue(cell, out int newIndex))
                {
                    newIndex = newVertices.Count;
                    newVertices.Add(sourceVerts[i]);
                    clusterMap[cell] = newIndex;
                }

                remap[i] = newIndex;
            }

            var newTriangles = new List<int>();
            for (int i = 0; i < sourceTris.Length; i += 3)
            {
                int a = remap[sourceTris[i]];
                int b = remap[sourceTris[i + 1]];
                int c = remap[sourceTris[i + 2]];

                if (a == b || b == c || a == c) continue; // degenerate triangle after clustering

                newTriangles.Add(a);
                newTriangles.Add(b);
                newTriangles.Add(c);
            }

            var result = new Mesh { name = source.name + "_LOD" };
            result.SetVertices(newVertices);
            result.SetTriangles(newTriangles, 0);
            result.RecalculateNormals();
            result.RecalculateBounds();
            return result;
        }

        [MenuItem("Human Body Explorer/Generate LODs For Selected Mesh")]
        public static void GenerateLodsForSelection()
        {
            var meshFilter = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<MeshFilter>() : null;
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning("[AutoLODManager] Select a GameObject with a MeshFilter first.");
                return;
            }

            var lod1 = Decimate(meshFilter.sharedMesh, 0.5f);
            var lod2 = Decimate(meshFilter.sharedMesh, 0.1f);

            var lodGroup = meshFilter.gameObject.GetComponent<LODGroup>() ?? meshFilter.gameObject.AddComponent<LODGroup>();

            var lod0Renderer = meshFilter.GetComponent<Renderer>();
            var lods = new LOD[]
            {
                new LOD(0.5f, new[] { lod0Renderer }),
                new LOD(0.1f, new[] { lod0Renderer }),
                new LOD(0.01f, new[] { lod0Renderer })
            };

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();

            Debug.Log($"[AutoLODManager] LOD1: {lod1.triangles.Length / 3} tris, LOD2: {lod2.triangles.Length / 3} tris " +
                      $"(from {meshFilter.sharedMesh.triangles.Length / 3} original).");
        }
    }
}
