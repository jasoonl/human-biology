using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// Persists generated meshes as assets under Assets/Generated/Meshes and hands back
    /// the saved instance for a scene to reference.
    ///
    /// Meshes have to be assets rather than scene-embedded: embedding a few hundred of
    /// them bloats Bootstrap.unity and rewrites all of it on every rebuild. An existing
    /// asset is overwritten in place, never deleted and recreated, so its GUID - and
    /// therefore every scene reference to it - stays stable across rebuilds and the
    /// scene diff stays small. Meshes a rebuild no longer produces are removed at the end.
    /// </summary>
    public static class MeshAssets
    {
        public const string Folder = "Assets/Generated/Meshes";

        private static readonly HashSet<string> Produced = new HashSet<string>();
        // Where each mesh saved this build lives. A mesh created while asset editing is batched has no asset path
        // yet (GetAssetPath is empty until the batch ends), so its path is remembered here instead.
        private static readonly Dictionary<Mesh, string> PathOf = new Dictionary<Mesh, string>();
        private static bool _editing;

        public static void BeginBuild()
        {
            Produced.Clear();
            PathOf.Clear();
            EnsureFolder();
            AssetDatabase.StartAssetEditing();
            _editing = true;
        }

        public static void EndBuild()
        {
            if (_editing)
            {
                AssetDatabase.StopAssetEditing();
                _editing = false;
            }

            int removed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Produced.Contains(path)) continue;
                AssetDatabase.DeleteAsset(path);
                removed++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MeshAssets] {Produced.Count} meshes written, {removed} stale removed.");
        }

        /// <summary>Keep only the meshes the scene actually uses; anything else this build saved (a stand-in
        /// that something replaced) is cleaned up as stale.</summary>
        public static void KeepOnlyUsedBy(Transform root)
        {
            var used = new HashSet<string>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                string path = PathFor(filter.sharedMesh);
                if (!string.IsNullOrEmpty(path)) used.Add(path);
            }
            Produced.IntersectWith(used);
        }

        /// <summary>Drop a mesh from this build's output so it is cleaned up as stale, for when something
        /// replaces it after it was saved.</summary>
        public static void Forget(Mesh mesh)
        {
            string path = PathFor(mesh);
            if (!string.IsNullOrEmpty(path)) Produced.Remove(path);
        }

        private static string PathFor(Mesh mesh)
        {
            if (mesh == null) return null;
            if (PathOf.TryGetValue(mesh, out string path)) return path;
            return AssetDatabase.GetAssetPath(mesh);
        }

        /// <summary>Save (or overwrite) the mesh and return the persistent asset.</summary>
        public static Mesh Save(Mesh mesh, string assetName)
        {
            EnsureFolder();
            string path = $"{Folder}/{Sanitize(assetName)}.asset";
            Produced.Add(path);

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                PathOf[existing] = path;
                return existing;
            }

            mesh.name = Sanitize(assetName);
            AssetDatabase.CreateAsset(mesh, path);
            PathOf[mesh] = path;
            return mesh;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Generated", "Meshes");
        }

        private static string Sanitize(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }
    }
}
