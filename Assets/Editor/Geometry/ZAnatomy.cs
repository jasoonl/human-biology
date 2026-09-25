using System;
using System.Collections.Generic;
using System.IO;
using HumanBodyExplorer.Core;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// Real anatomy from the Z-Anatomy atlas (CC BY-SA 4.0, built on BodyParts3D, CC BY-SA 2.1 JP). The Blender
    /// scene is exported by Tools/ZAnatomy/export.py into ZAnatomyData/, and zanatomy_map.json says which of its
    /// objects make up each explorer entity id. <see cref="Apply"/> swaps the hand-sculpted stand-in for each
    /// mapped id with the real mesh, keeping the id, layer and material; anything unmapped keeps its sculpt.
    /// When the data folder is missing the explorer builds exactly as before.
    /// </summary>
    public static class ZAnatomy
    {
        // The atlas figure is 1.70 m to the top of the skull with its soles at y = 0.011 (Blender z). The explorer's
        // figure is a little taller, so the atlas is scaled to match and dropped onto y = 0.
        public const float Scale = 1.03f;
        public const float FootY = 0.011f;

        [Serializable] private class MapEntry { public string id; public string[] names; public string[] lines; public float yMin; public float yMax; }
        [Serializable] private class LinePath { public float[] p; public float[] r; }
        [Serializable] private class LineEntry { public string name; public string coll; public float bevel; public LinePath[] lines; }
        [Serializable] private class LineFile { public LineEntry[] items; }
        [Serializable] private class MapFile { public MapEntry[] entries; }
        [Serializable] private class IndexEntry { public string name; public string coll; public int v; public int t; public long off; }
        [Serializable] private class IndexFile { public IndexEntry[] items; }

        private static string DataDir => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ZAnatomyData");
        public static bool Available => File.Exists(Path.Combine(DataDir, "zana.bin"));

        private static Dictionary<string, IndexEntry> _index;
        private static byte[] _blob;
        private static Dictionary<string, LineEntry> _lines;

        public static Vector3 Fit(Vector3 p) => new Vector3(p.x * Scale, (p.y - FootY) * Scale, p.z * Scale);

        private static void Load()
        {
            if (_index != null) return;
            _blob = File.ReadAllBytes(Path.Combine(DataDir, "zana.bin"));
            string json = File.ReadAllText(Path.Combine(DataDir, "zana_index.json"));
            var file = JsonUtility.FromJson<IndexFile>("{\"items\":" + json + "}");
            _index = new Dictionary<string, IndexEntry>();
            foreach (var e in file.items) _index[e.name] = e;

            _lines = new Dictionary<string, LineEntry>();
            string linePath = Path.Combine(DataDir, "zana_lines.json");
            if (File.Exists(linePath))
                foreach (var e in JsonUtility.FromJson<LineFile>("{\"items\":" + File.ReadAllText(linePath) + "}").items) _lines[e.name] = e;
        }

        /// <summary>Tubes along the atlas' centre lines (vessels and nerves), with the flow-shader UVs the explorer's
        /// own tubes have. Radii come from the atlas' bevel depth and per-point radius.</summary>
        public static Mesh Tubes(IEnumerable<string> names, string meshName, float yMin = -100f, float yMax = 100f)
        {
            Load();
            var parts = new List<CombineInstance>();
            foreach (string name in names)
            {
                if (!_lines.TryGetValue(name, out var entry)) continue;
                foreach (var path in entry.lines)
                {
                    int count = path.p.Length / 3;
                    float meanY = 0f;
                    for (int i = 0; i < count; i++) meanY += path.p[i * 3 + 1];
                    meanY /= Mathf.Max(1, count);
                    if (meanY < yMin || meanY > yMax) continue;
                    var pts = new List<Vector3>();
                    var radii = new List<float>();
                    for (int i = 0; i < count; i++)
                    {
                        var q = Fit(new Vector3(path.p[i * 3], path.p[i * 3 + 1], path.p[i * 3 + 2]));
                        if (pts.Count > 0 && i < count - 1 && (q - pts[pts.Count - 1]).sqrMagnitude < 0.004f * 0.004f) continue;
                        pts.Add(q);
                        radii.Add(Mathf.Max(0.0006f, entry.bevel * path.r[i] * Scale));
                    }
                    if (pts.Count < 2) continue;

                    var radiusAt = new Func<float, float>(t =>
                    {
                        float f = Mathf.Clamp01(t) * (radii.Count - 1);
                        int a = Mathf.Min((int)f, radii.Count - 2);
                        return Mathf.Lerp(radii[a], radii[a + 1], f - a);
                    });

                    float rmax = 0f;
                    foreach (float r in radii) rmax = Mathf.Max(rmax, r);
                    var o = Loft.Options.Default;
                    o.Sides = rmax >= 0.006f ? 14 : rmax >= 0.003f ? 10 : 6;
                    o.RingsPerMetre = 200f;
                    o.CapRings = 2;
                    o.VTiling = 8f;
                    Mesh tube = Loft.Tube(entry.name, pts, radiusAt, o);
                    if (tube != null && tube.vertexCount > 0)
                        parts.Add(new CombineInstance { mesh = tube, transform = Matrix4x4.identity });
                }
            }

            var result = new Mesh { name = meshName, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            if (parts.Count > 0) result.CombineMeshes(parts.ToArray(), true, false);
            foreach (var part in parts) UnityEngine.Object.DestroyImmediate(part.mesh);
            result.RecalculateBounds();
            return result;
        }

        /// <summary>One mesh from the union of the named atlas objects, in figure space, welded and smooth-shaded.</summary>
        public static Mesh Combine(IEnumerable<string> names, string meshName)
        {
            Load();
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var weld = new Dictionary<Vector3Int, int>();

            foreach (string name in names)
            {
                if (!_index.TryGetValue(name, out var e)) continue;
                var remap = new int[e.v];
                for (int i = 0; i < e.v; i++)
                {
                    int o = (int)e.off + i * 12;
                    var p = Fit(new Vector3(BitConverter.ToSingle(_blob, o), BitConverter.ToSingle(_blob, o + 4), BitConverter.ToSingle(_blob, o + 8)));
                    var key = new Vector3Int(Mathf.RoundToInt(p.x * 20000f), Mathf.RoundToInt(p.y * 20000f), Mathf.RoundToInt(p.z * 20000f));
                    if (!weld.TryGetValue(key, out int index)) { index = verts.Count; verts.Add(p); weld[key] = index; }
                    remap[i] = index;
                }
                int triStart = (int)e.off + e.v * 12;
                for (int i = 0; i < e.t * 3; i += 3)
                {
                    int a = remap[BitConverter.ToInt32(_blob, triStart + i * 4)];
                    int b = remap[BitConverter.ToInt32(_blob, triStart + (i + 1) * 4)];
                    int c = remap[BitConverter.ToInt32(_blob, triStart + (i + 2) * 4)];
                    if (a == b || b == c || a == c) continue;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                }
            }

            var mesh = new Mesh { name = meshName, indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ------------------------------------------------------------------------ the skin

        /// <summary>The body surface fitted to the atlas: a smoothed shell around every bone, muscle and organ,
        /// computed by Tools/ZAnatomy/skinfield.py and sampled here as a signed distance field (metres). The head
        /// has its own finer grid so the lips, eyelids and nose survive; the two are blended across the neck.</summary>
        public static SdfFunc SkinField(bool female = false)
        {
            // The female exterior is fitted without the male genital organs.
            var body = Grid(female ? "skinfield_f.bin" : "skinfield.bin", 0.176f, out _, out _);
            var head = Grid("skinhead.bin", 0.176f, out var hMin, out var hMax);
            var hand = Grid("skinhand.bin", 0.249f, out var handMin, out var handMax);
            return p =>
            {
                float b = body(p);

                // Hands: their own fine grid (left hand, mirrored) so the fingers stay separate.
                float wristY = 0.90f * Scale;
                if (p.y < wristY)
                {
                    var hq = new Vector3(Mathf.Abs(p.x) / Scale, p.y / Scale + FootY, p.z / Scale);
                    if (hq.x > handMin.x + 0.004f && hq.x < handMax.x - 0.004f && hq.y > handMin.y + 0.004f && hq.z > handMin.z + 0.004f && hq.z < handMax.z - 0.004f)
                    {
                        float wh = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(wristY, 0.86f * Scale, p.y));
                        b = Mathf.Lerp(b, hand(new Vector3(Mathf.Abs(p.x), p.y, p.z)), wh);
                    }
                    return b;
                }
                if (p.y < 1.50f * Scale) return b;
                var q = new Vector3(p.x / Scale, p.y / Scale + FootY, p.z / Scale);
                if (q.x < hMin.x + 0.004f || q.x > hMax.x - 0.004f || q.y > hMax.y - 0.004f || q.z < hMin.z + 0.004f || q.z > hMax.z - 0.004f) return b;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.50f * Scale, 1.57f * Scale, p.y));
                return Mathf.Lerp(b, head(p), t);
            };
        }

        private static SdfFunc Grid(string file, float slope, out Vector3 worldMin, out Vector3 worldMax)
        {
            using (var r = new BinaryReader(File.OpenRead(Path.Combine(DataDir, file))))
            {
                int nx = r.ReadInt32(), ny = r.ReadInt32(), nz = r.ReadInt32();
                var min = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                float voxel = r.ReadSingle();
                var data = new float[nx * ny * nz];
                byte[] bytes = r.ReadBytes(data.Length * 4);
                Buffer.BlockCopy(bytes, 0, data, 0, bytes.Length);
                worldMin = min;
                worldMax = min + new Vector3(nx - 1, ny - 1, nz - 1) * voxel;

                // Grid values are (0.5 - blurred occupancy): +-0.5 across the smoothing width. Convert to metres.
                float toMetres = voxel / slope * Scale;
                return p =>
                {
                    // back into atlas space
                    float x = (p.x / Scale - min.x) / voxel;
                    float y = ((p.y / Scale + FootY) - min.y) / voxel;
                    float z = (p.z / Scale - min.z) / voxel;
                    int i = Mathf.FloorToInt(x), j = Mathf.FloorToInt(y), k = Mathf.FloorToInt(z);
                    if (i < 0 || j < 0 || k < 0 || i >= nx - 1 || j >= ny - 1 || k >= nz - 1) return 0.5f * toMetres;
                    float fx = x - i, fy = y - j, fz = z - k;
                    // C-order (x, y, z) array: index = (i * ny + j) * nz + k
                    float G(int a, int b, int c) => data[(a * ny + b) * nz + c];
                    float c00 = Mathf.Lerp(G(i, j, k), G(i + 1, j, k), fx);
                    float c10 = Mathf.Lerp(G(i, j + 1, k), G(i + 1, j + 1, k), fx);
                    float c01 = Mathf.Lerp(G(i, j, k + 1), G(i + 1, j, k + 1), fx);
                    float c11 = Mathf.Lerp(G(i, j + 1, k + 1), G(i + 1, j + 1, k + 1), fx);
                    return Mathf.Lerp(Mathf.Lerp(c00, c10, fy), Mathf.Lerp(c01, c11, fy), fz) * toMetres;
                };
            }
        }

        // -------------------------------------------------------------------- swapping the parts

        private static List<MapEntry> LoadMap()
        {
            string path = "Assets/Editor/Geometry/zanatomy_map.json";
            var file = JsonUtility.FromJson<MapFile>(File.ReadAllText(path));
            return new List<MapEntry>(file.entries);
        }

        /// <summary>Replace the sculpted stand-in of every mapped entity with the atlas mesh.</summary>
        public static int Apply(Transform root)
        {
            if (!Available) return 0;
            Load();

            var byId = new Dictionary<string, List<AnatomyNodeReference>>();
            foreach (var node in root.GetComponentsInChildren<AnatomyNodeReference>(true))
            {
                if (string.IsNullOrEmpty(node.EntityId)) continue;
                if (!byId.TryGetValue(node.EntityId, out var list)) byId[node.EntityId] = list = new List<AnatomyNodeReference>();
                list.Add(node);
            }

            int replaced = 0;
            foreach (var entry in LoadMap())
            {
                if (!byId.TryGetValue(entry.id, out var existing) || existing.Count == 0) continue;

                var first = existing[0].gameObject;
                var parent = first.transform.parent;
                int layer = first.layer;
                var materials = first.GetComponent<MeshRenderer>().sharedMaterials;

                foreach (var node in existing)
                {
                    var filter = node.GetComponent<MeshFilter>();
                    if (filter != null) MeshAssets.Forget(filter.sharedMesh);
                    UnityEngine.Object.DestroyImmediate(node.gameObject);
                }

                var left = new List<string>(); var right = new List<string>(); var middle = new List<string>();
                var leftLines = new List<string>(); var rightLines = new List<string>(); var middleLines = new List<string>();
                void Sort(string n, List<string> l, List<string> r, List<string> m)
                {
                    if (n.EndsWith(".l", StringComparison.Ordinal)) l.Add(n);
                    else if (n.EndsWith(".r", StringComparison.Ordinal)) r.Add(n);
                    else m.Add(n);
                }
                foreach (string n in entry.names) Sort(n, left, right, middle);
                if (entry.lines != null) foreach (string n in entry.lines) Sort(n, leftLines, rightLines, middleLines);

                string baseName = entry.id.Replace("SYS_", "");
                if (middle.Count + middleLines.Count > 0) Place(parent, baseName, entry.id, middle, middleLines, materials, layer, entry);
                if (left.Count + leftLines.Count > 0) Place(parent, baseName + "_L", entry.id, left, leftLines, materials, layer, entry);
                if (right.Count + rightLines.Count > 0) Place(parent, baseName + "_R", entry.id, right, rightLines, materials, layer, entry);
                replaced++;
            }

            // The atlas has no female organs, so those stay hand-sculpted; they were placed for the old pelvis, which sat
            // lower than the atlas' one.
            foreach (var node in root.GetComponentsInChildren<AnatomyNodeReference>(true))
                if (node.EntityId != null && node.EntityId.StartsWith("SYS_REP_F_", StringComparison.Ordinal))
                    node.transform.position += new Vector3(0f, 0.015f, 0.010f);   // up, and behind the bladder
            return replaced;
        }

        private static void Place(Transform parent, string name, string id, List<string> names, List<string> lines, Material[] materials, int layer, MapEntry range)
        {
            float lo = range.yMin == 0f && range.yMax == 0f ? -100f : range.yMin;
            float hi = range.yMin == 0f && range.yMax == 0f ? 100f : range.yMax;
            Mesh mesh;
            if (lines.Count == 0) mesh = Combine(names, name);
            else if (names.Count == 0) mesh = Tubes(lines, name, lo, hi);
            else
            {
                Mesh solid = Combine(names, name), tubes = Tubes(lines, name, lo, hi);
                mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(new[] { new CombineInstance { mesh = solid }, new CombineInstance { mesh = tubes } }, true, false);
                UnityEngine.Object.DestroyImmediate(solid);
                UnityEngine.Object.DestroyImmediate(tubes);
            }
            if (mesh.vertexCount == 0) { UnityEngine.Object.DestroyImmediate(mesh); return; }
            PartFactory.Add(parent, name, id, PartFactory.Save(mesh, "Z_" + name), materials, layer);
        }
    }
}
