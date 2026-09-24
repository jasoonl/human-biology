using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// Turns a signed distance field into a triangle mesh with naive surface nets: one
    /// vertex per grid cell the surface passes through, placed at the average of where
    /// it crosses that cell's edges, then snapped onto the true surface.
    ///
    /// Chosen over marching cubes because it needs no 256-entry lookup tables, produces
    /// a single shared-vertex mesh with no seams, and gives smooth results from the
    /// blended shapes <see cref="Sdf"/> makes. Normals come from the field's gradient
    /// rather than from the triangles, so the surface shades smooth even where the
    /// mesh is coarse. Triangle winding is decided per face against those normals, so
    /// it is right by construction instead of by getting index order conventions right.
    /// </summary>
    public static class SurfaceNets
    {
        // Corner offsets of a cell, and the 12 edges joining them.
        private static readonly Vector3Int[] Corners =
        {
            new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0),
            new Vector3Int(0, 0, 1), new Vector3Int(1, 0, 1), new Vector3Int(0, 1, 1), new Vector3Int(1, 1, 1),
        };

        private static readonly int[,] Edges =
        {
            { 0, 1 }, { 2, 3 }, { 4, 5 }, { 6, 7 },
            { 0, 2 }, { 1, 3 }, { 4, 6 }, { 5, 7 },
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 },
        };

        /// <param name="field">The shape; negative inside.</param>
        /// <param name="min">Lower corner of the box to sample. The shape must not
        /// touch the box faces or it will be left open there.</param>
        /// <param name="max">Upper corner of the box.</param>
        /// <param name="voxel">Cell size in metres. Smaller is smoother and heavier;
        /// mesh detail thinner than about 1.5 voxels will not survive.</param>
        /// <param name="uvScale">Planar UV tiling, for the tissue texture.</param>
        public static Mesh Build(SdfFunc field, Vector3 min, Vector3 max, float voxel, string name, float uvScale = 6f)
        {
            int nx = Mathf.Max(3, Mathf.CeilToInt((max.x - min.x) / voxel) + 1);
            int ny = Mathf.Max(3, Mathf.CeilToInt((max.y - min.y) / voxel) + 1);
            int nz = Mathf.Max(3, Mathf.CeilToInt((max.z - min.z) / voxel) + 1);

            // Sample the field on the grid. Field functions are pure, so slabs of the
            // grid are evaluated in parallel.
            var values = new float[nx * ny * nz];
            Parallel.For(0, nz, k =>
            {
                for (int j = 0; j < ny; j++)
                    for (int i = 0; i < nx; i++)
                        values[i + nx * (j + ny * k)] = field(min + new Vector3(i, j, k) * voxel);
            });

            int cx = nx - 1, cy = ny - 1, cz = nz - 1;
            var cellVertex = new int[cx * cy * cz];
            for (int i = 0; i < cellVertex.Length; i++) cellVertex[i] = -1;

            var positions = new List<Vector3>();
            var cornerValues = new float[8];

            for (int k = 0; k < cz; k++)
            for (int j = 0; j < cy; j++)
            for (int i = 0; i < cx; i++)
            {
                int inside = 0;
                for (int c = 0; c < 8; c++)
                {
                    Vector3Int o = Corners[c];
                    float v = values[(i + o.x) + nx * ((j + o.y) + ny * (k + o.z))];
                    cornerValues[c] = v;
                    if (v < 0f) inside++;
                }
                if (inside == 0 || inside == 8) continue;

                Vector3 sum = Vector3.zero;
                int crossings = 0;
                for (int e = 0; e < 12; e++)
                {
                    int a = Edges[e, 0], b = Edges[e, 1];
                    float va = cornerValues[a], vb = cornerValues[b];
                    if ((va < 0f) == (vb < 0f)) continue;

                    float t = va / (va - vb);
                    sum += (Vector3)Corners[a] + t * (Vector3)(Corners[b] - Corners[a]);
                    crossings++;
                }

                Vector3 local = sum / crossings;
                cellVertex[i + cx * (j + cy * k)] = positions.Count;
                positions.Add(min + (new Vector3(i, j, k) + local) * voxel);
            }

            // Snap each vertex onto the real surface with one step along the gradient,
            // then take its normal from the field there.
            int vertexCount = positions.Count;
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            float eps = voxel * 0.5f;
            float maxMove = voxel * 0.75f;

            Parallel.For(0, vertexCount, n =>
            {
                Vector3 p = positions[n];
                Vector3 g = Gradient(field, p, eps);
                float d = field(p);
                if (g.sqrMagnitude > 1e-12f)
                {
                    g.Normalize();
                    p -= g * Mathf.Clamp(d, -maxMove, maxMove);
                    g = Gradient(field, p, eps);
                    g = g.sqrMagnitude > 1e-12f ? g.normalized : Vector3.up;
                }
                else g = Vector3.up;

                vertices[n] = p;
                normals[n] = g;
            });

            var triangles = new List<int>(vertexCount * 6);

            void Quad(int c0, int c1, int c2, int c3)
            {
                if (c0 < 0 || c1 < 0 || c2 < 0 || c3 < 0) return;
                AddTriangle(triangles, vertices, normals, c0, c1, c2);
                AddTriangle(triangles, vertices, normals, c0, c2, c3);
            }

            int Cell(int i, int j, int k) => cellVertex[i + cx * (j + cy * k)];

            // One quad for every grid edge the surface crosses, joining the four cells
            // around that edge.
            for (int k = 0; k < nz; k++)
            for (int j = 0; j < ny; j++)
            for (int i = 0; i < nx; i++)
            {
                float v0 = values[i + nx * (j + ny * k)];
                bool in0 = v0 < 0f;

                if (i < nx - 1 && j > 0 && j < ny - 1 && k > 0 && k < nz - 1)
                {
                    bool in1 = values[(i + 1) + nx * (j + ny * k)] < 0f;
                    if (in0 != in1)
                        Quad(Cell(i, j - 1, k - 1), Cell(i, j, k - 1), Cell(i, j, k), Cell(i, j - 1, k));
                }
                if (j < ny - 1 && i > 0 && i < nx - 1 && k > 0 && k < nz - 1)
                {
                    bool in1 = values[i + nx * ((j + 1) + ny * k)] < 0f;
                    if (in0 != in1)
                        Quad(Cell(i - 1, j, k - 1), Cell(i, j, k - 1), Cell(i, j, k), Cell(i - 1, j, k));
                }
                if (k < nz - 1 && i > 0 && i < nx - 1 && j > 0 && j < ny - 1)
                {
                    bool in1 = values[i + nx * (j + ny * (k + 1))] < 0f;
                    if (in0 != in1)
                        Quad(Cell(i - 1, j - 1, k), Cell(i, j - 1, k), Cell(i, j, k), Cell(i - 1, j, k));
                }
            }

            var uvs = new Vector2[vertexCount];
            for (int n = 0; n < vertexCount; n++)
                uvs[n] = new Vector2((vertices[n].x + vertices[n].z * 0.6f) * uvScale, vertices[n].y * uvScale);

            var mesh = new Mesh { name = name };
            if (vertexCount > 60000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 Gradient(SdfFunc f, Vector3 p, float e) => new Vector3(
            f(new Vector3(p.x + e, p.y, p.z)) - f(new Vector3(p.x - e, p.y, p.z)),
            f(new Vector3(p.x, p.y + e, p.z)) - f(new Vector3(p.x, p.y - e, p.z)),
            f(new Vector3(p.x, p.y, p.z + e)) - f(new Vector3(p.x, p.y, p.z - e)));

        /// <summary>Emit a triangle wound so its face normal agrees with the field's
        /// outward normals. Unity treats clockwise as front-facing, and that face
        /// normal is Cross(b - a, c - a).</summary>
        private static void AddTriangle(List<int> tris, Vector3[] v, Vector3[] n, int a, int b, int c)
        {
            Vector3 face = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (face.sqrMagnitude < 1e-16f) return; // degenerate sliver

            Vector3 outward = n[a] + n[b] + n[c];
            if (Vector3.Dot(face, outward) >= 0f) { tris.Add(a); tris.Add(b); tris.Add(c); }
            else { tris.Add(a); tris.Add(c); tris.Add(b); }
        }
    }
}
