using HumanBodyExplorer.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// Creates the scene object for one anatomical structure: mesh, renderer, collider,
    /// and the EntityID tag that ties it to the dictionary, the quiz, the highlighter
    /// and the layer toggles.
    /// </summary>
    public static class PartFactory
    {
        public static GameObject Add(Transform parent, string name, string entityId, Mesh mesh,
            Material material, int layer, bool mirrorX = false) =>
            Add(parent, name, entityId, mesh, new[] { material }, layer, mirrorX);

        public static GameObject Add(Transform parent, string name, string entityId, Mesh mesh,
            Material[] materials, int layer, bool mirrorX = false)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);

            // Paired structures share one mesh. The right-hand one is the left mirrored
            // through x = 0, which needs the object at the origin and a negative X scale;
            // Unity flips the triangle winding for negative scale by itself.
            if (mirrorX) go.transform.localScale = new Vector3(-1f, 1f, 1f);

            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            // A material with no submesh to draw would repaint the last one; a muscle with
            // no tendon stretch has one submesh but is given a tendon material.
            if (mesh.subMeshCount < materials.Length)
                materials = Trim(materials, mesh.subMeshCount);

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;

            go.AddComponent<AnatomyNodeReference>().SetEntityId(entityId);
            return go;
        }

        private static Material[] Trim(Material[] all, int count)
        {
            var result = new Material[Mathf.Max(1, count)];
            for (int i = 0; i < result.Length; i++) result[i] = all[Mathf.Min(i, all.Length - 1)];
            return result;
        }

        /// <summary>Add a structure that exists on both sides, from a mesh modelled on
        /// the anatomical left (positive X). Objects are named with _L and _R.</summary>
        public static void AddPair(Transform parent, string baseName, string entityId, Mesh leftMesh,
            Material material, int layer)
        {
            Add(parent, baseName + "_L", entityId, leftMesh, material, layer, false);
            Add(parent, baseName + "_R", entityId, leftMesh, material, layer, true);
        }

        public static void AddPair(Transform parent, string baseName, string entityId, Mesh leftMesh,
            Material[] materials, int layer)
        {
            Add(parent, baseName + "_L", entityId, leftMesh, materials, layer, false);
            Add(parent, baseName + "_R", entityId, leftMesh, materials, layer, true);
        }

        /// <summary>Save a mesh built for one part and return the persistent asset.</summary>
        public static Mesh Save(Mesh mesh, string assetName) => MeshAssets.Save(mesh, assetName);

        /// <summary>Axis-aligned bounds around a set of key points, padded on every
        /// side, for sizing the sampling grid of an SDF mesh.</summary>
        public static void BoundsOf(out Vector3 min, out Vector3 max, float margin, params Vector3[] points)
        {
            min = points[0];
            max = points[0];
            for (int i = 1; i < points.Length; i++)
            {
                min = Vector3.Min(min, points[i]);
                max = Vector3.Max(max, points[i]);
            }
            min -= Vector3.one * margin;
            max += Vector3.one * margin;
        }
    }
}
