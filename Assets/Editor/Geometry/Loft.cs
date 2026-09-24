using System;
using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// Sweeps an elliptical cross-section along a smooth curve with a radius that
    /// varies along its length. This is the right tool for anything that is really a
    /// tube: a muscle belly tapering into its tendons, a rib, an artery, a nerve, a
    /// finger bone with flared ends. It is far cheaper than an SDF (a few hundred
    /// vertices rather than thousands) and keeps its cross-section exactly circular or
    /// exactly as flat as asked, which a voxel mesh cannot promise for thin features.
    ///
    /// UV.y runs along the length in metres, so the vessel shader's pulse travels along
    /// a vessel; UV.x runs once around, so a fibre texture stripes a muscle lengthwise.
    /// </summary>
    public static class Loft
    {
        public struct Options
        {
            /// <summary>Points around the cross-section.</summary>
            public int Sides;
            /// <summary>Width of the cross-section relative to its thickness (1 = round).</summary>
            public float Flatten;
            /// <summary>Direction the wide axis should follow; projected perpendicular to
            /// the path at every ring. Null leaves the section free to twist naturally.</summary>
            public Vector3? WidthAxis;
            /// <summary>Rings per metre along the path (before end caps).</summary>
            public float RingsPerMetre;
            /// <summary>Rings used to round each end into a dome; 0 leaves ends open.</summary>
            public int CapRings;
            /// <summary>Chooses which submesh a stretch of the tube belongs to, from its
            /// position along the length (0 to 1). Used to give tendons their own material.</summary>
            public Func<float, int> Submesh;
            public float VTiling;

            public static Options Default => new Options
            {
                Sides = 10, Flatten = 1f, WidthAxis = null, RingsPerMetre = 90f, CapRings = 3, Submesh = null, VTiling = 8f,
            };
        }

        private struct Ring
        {
            public Vector3 Center, Width, Thick;
            public float Radius, T;
            public bool Cap;
        }

        /// <summary>
        /// A tube through <paramref name="controlPoints"/> (Catmull-Rom smoothed).
        /// <paramref name="radiusAt"/> maps position along the length, 0 to 1, to a radius.
        /// </summary>
        public static Mesh Tube(string name, IList<Vector3> controlPoints, Func<float, float> radiusAt, Options o)
        {
            List<Vector3> path = SamplePath(controlPoints, o.RingsPerMetre);
            int n = path.Count;

            var tangents = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 a = path[Mathf.Max(0, i - 1)], b = path[Mathf.Min(n - 1, i + 1)];
                tangents[i] = (b - a).normalized;
            }

            var lengthAt = new float[n];
            for (int i = 1; i < n; i++) lengthAt[i] = lengthAt[i - 1] + (path[i] - path[i - 1]).magnitude;
            float total = Mathf.Max(lengthAt[n - 1], 1e-5f);

            // Frames: keep the wide axis pointing where asked, or carry the previous
            // frame along the curve without twisting it.
            var widths = new Vector3[n];
            Vector3 carry = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                Vector3 t = tangents[i];
                Vector3 w;
                if (o.WidthAxis.HasValue)
                {
                    w = o.WidthAxis.Value - t * Vector3.Dot(o.WidthAxis.Value, t);
                    if (w.sqrMagnitude < 1e-6f) w = i > 0 ? carry : Perpendicular(t);
                }
                else if (i == 0) w = Perpendicular(t);
                else w = carry - t * Vector3.Dot(carry, t);

                w = w.sqrMagnitude < 1e-8f ? Perpendicular(t) : w.normalized;
                widths[i] = w;
                carry = w;
            }

            var rings = new List<Ring>(n + 2 * o.CapRings);

            float r0 = Mathf.Max(radiusAt(0f), 1e-5f);
            for (int c = o.CapRings; c >= 1; c--)
            {
                float phi = c / (float)(o.CapRings + 1) * Mathf.PI * 0.5f;
                rings.Add(MakeRing(path[0] - tangents[0] * (r0 * Mathf.Sin(phi)), tangents[0], widths[0],
                    r0 * Mathf.Cos(phi), 0f, o.Flatten, true));
            }

            for (int i = 0; i < n; i++)
            {
                float t = lengthAt[i] / total;
                rings.Add(MakeRing(path[i], tangents[i], widths[i], Mathf.Max(radiusAt(t), 1e-5f), t, o.Flatten, false));
            }

            float r1 = Mathf.Max(radiusAt(1f), 1e-5f);
            for (int c = 1; c <= o.CapRings; c++)
            {
                float phi = c / (float)(o.CapRings + 1) * Mathf.PI * 0.5f;
                rings.Add(MakeRing(path[n - 1] + tangents[n - 1] * (r1 * Mathf.Sin(phi)), tangents[n - 1], widths[n - 1],
                    r1 * Mathf.Cos(phi), 1f, o.Flatten, true));
            }

            return BuildMesh(name, rings, o, total);
        }

        /// <summary>A muscle-belly radius profile: thin tendon, swelling belly, thin
        /// tendon. <paramref name="bellyStart"/> and <paramref name="bellyEnd"/> mark
        /// where the belly ends and the tendon begins, as fractions of the length.</summary>
        public static Func<float, float> BellyProfile(float tendonRadius, float bellyRadius,
            float bellyStart = 0.18f, float bellyEnd = 0.82f, float peak = 0.5f)
        {
            return t =>
            {
                // Rise from tendon to belly, then fall back, using smoothstep on each side.
                float rise = Smooth01(Mathf.InverseLerp(0f, bellyStart + (peak - bellyStart) * 0.6f, t));
                float fall = Smooth01(Mathf.InverseLerp(1f, bellyEnd - (bellyEnd - peak) * 0.6f, t));
                float bump = Mathf.Min(rise, fall);
                return Mathf.Lerp(tendonRadius, bellyRadius, Mathf.Pow(bump, 0.8f));
            };
        }

        /// <summary>Radius that swells at both ends, like a long bone's epiphyses,
        /// with a slimmer shaft between.</summary>
        public static Func<float, float> BoneProfile(float endRadius, float shaftRadius, float flare = 0.22f)
        {
            return t =>
            {
                float toEnd = Mathf.Min(t, 1f - t);
                float k = Smooth01(toEnd / flare);
                return Mathf.Lerp(endRadius, shaftRadius, k);
            };
        }

        public static Func<float, float> Constant(float r) => t => r;

        public static Func<float, float> Taper(float start, float end) => t => Mathf.Lerp(start, end, t);

        // ---------------------------------------------------------------- internals

        private static float Smooth01(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        private static Vector3 Perpendicular(Vector3 t)
        {
            Vector3 axis = Mathf.Abs(t.y) < 0.9f ? Vector3.up : Vector3.right;
            return Vector3.Cross(t, axis).normalized;
        }

        private static Ring MakeRing(Vector3 center, Vector3 tangent, Vector3 width, float radius, float t, float flatten, bool cap)
        {
            Vector3 thick = Vector3.Cross(tangent, width).normalized;
            return new Ring { Center = center, Width = width, Thick = thick, Radius = radius, T = t, Cap = cap };
        }

        private static List<Vector3> SamplePath(IList<Vector3> pts, float ringsPerMetre)
        {
            var result = new List<Vector3>();
            int count = pts.Count;
            if (count == 1) { result.Add(pts[0]); result.Add(pts[0] + Vector3.up * 1e-3f); return result; }

            for (int s = 0; s < count - 1; s++)
            {
                Vector3 p0 = s == 0 ? 2f * pts[0] - pts[1] : pts[s - 1];
                Vector3 p1 = pts[s], p2 = pts[s + 1];
                Vector3 p3 = s + 2 < count ? pts[s + 2] : 2f * pts[count - 1] - pts[count - 2];

                float len = (p2 - p1).magnitude;
                int steps = Mathf.Max(2, Mathf.CeilToInt(len * ringsPerMetre));
                for (int i = 0; i < steps; i++)
                    result.Add(CatmullRom(p0, p1, p2, p3, i / (float)steps));
            }
            result.Add(pts[count - 1]);
            return result;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private static Mesh BuildMesh(string name, List<Ring> rings, Options o, float totalLength)
        {
            int sides = Mathf.Max(3, o.Sides);
            int ringCount = rings.Count;
            int stride = sides + 1; // duplicated seam vertex so UVs wrap cleanly

            var verts = new List<Vector3>(ringCount * stride + 2);
            var norms = new List<Vector3>(ringCount * stride + 2);
            var uvs = new List<Vector2>(ringCount * stride + 2);

            float runLength = 0f;
            for (int r = 0; r < ringCount; r++)
            {
                Ring ring = rings[r];
                if (r > 0) runLength += (ring.Center - rings[r - 1].Center).magnitude;

                // Rate at which the radius changes along the axis gives the surface its
                // slope, which tilts the normal so tapers shade as cones, not cylinders.
                Ring prev = rings[Mathf.Max(0, r - 1)], next = rings[Mathf.Min(ringCount - 1, r + 1)];
                Vector3 axis = next.Center - prev.Center;
                float axisLen = Mathf.Max(axis.magnitude, 1e-6f);
                float slope = (next.Radius - prev.Radius) / axisLen;
                axis /= axisLen;

                for (int s = 0; s < stride; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    float wr = ring.Radius * o.Flatten, tr = ring.Radius;

                    verts.Add(ring.Center + ring.Width * (ca * wr) + ring.Thick * (sa * tr));

                    Vector3 radial = (ring.Width * (ca * tr) + ring.Thick * (sa * wr)).normalized;
                    norms.Add((radial - axis * slope).normalized);
                    uvs.Add(new Vector2(s / (float)sides, runLength * o.VTiling));
                }
            }

            int submeshCount = 1;
            var submeshTris = new List<List<int>> { new List<int>() };

            for (int r = 0; r < ringCount - 1; r++)
            {
                float t = rings[r].Cap ? (rings[r].T < 0.5f ? 0f : 1f) : 0.5f * (rings[r].T + rings[r + 1].T);
                int sub = o.Submesh != null ? Mathf.Max(0, o.Submesh(t)) : 0;
                while (submeshTris.Count <= sub) submeshTris.Add(new List<int>());
                submeshCount = Mathf.Max(submeshCount, sub + 1);

                for (int s = 0; s < sides; s++)
                {
                    int i00 = r * stride + s, i01 = r * stride + s + 1;
                    int i10 = (r + 1) * stride + s, i11 = (r + 1) * stride + s + 1;

                    // Unity's front face is clockwise. Rings advance along the path and
                    // the section runs Width -> Thick, so this order faces outward.
                    submeshTris[sub].Add(i00); submeshTris[sub].Add(i01); submeshTris[sub].Add(i10);
                    submeshTris[sub].Add(i01); submeshTris[sub].Add(i11); submeshTris[sub].Add(i10);
                }
            }

            // Close the ends: a single pole vertex fanned to the last ring at each end.
            if (o.CapRings > 0)
            {
                int startPole = verts.Count;
                Vector3 startDir = (rings[0].Center - rings[Mathf.Min(1, ringCount - 1)].Center).normalized;
                verts.Add(rings[0].Center + startDir * 1e-6f - startDir * rings[0].Radius * 0.02f);
                norms.Add(startDir);
                uvs.Add(new Vector2(0.5f, 0f));
                for (int s = 0; s < sides; s++)
                {
                    submeshTris[0].Add(startPole); submeshTris[0].Add(s + 1); submeshTris[0].Add(s);
                }

                int endPole = verts.Count;
                int last = (ringCount - 1) * stride;
                Vector3 endDir = (rings[ringCount - 1].Center - rings[Mathf.Max(0, ringCount - 2)].Center).normalized;
                verts.Add(rings[ringCount - 1].Center + endDir * rings[ringCount - 1].Radius * 0.02f);
                norms.Add(endDir);
                uvs.Add(new Vector2(0.5f, runLength * o.VTiling));
                int endSub = submeshTris.Count - 1;
                for (int s = 0; s < sides; s++)
                {
                    submeshTris[0].Add(endPole); submeshTris[0].Add(last + s); submeshTris[0].Add(last + s + 1);
                }
                _ = endSub;
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = submeshCount;
            for (int s = 0; s < submeshCount; s++)
                mesh.SetTriangles(s < submeshTris.Count ? submeshTris[s] : new List<int>(), s);
            mesh.RecalculateBounds();

            FixWinding(mesh);
            return mesh;
        }

        /// <summary>
        /// A tube's triangle order depends on which way the frame happens to be
        /// handed, which is easy to get backwards. Rather than trust the derivation,
        /// check the mesh: if its faces point inward against the normals just computed,
        /// flip every triangle.
        /// </summary>
        private static void FixWinding(Mesh mesh)
        {
            var verts = mesh.vertices;
            var norms = mesh.normals;
            float agree = 0f;

            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var tris = mesh.GetTriangles(s);
                for (int i = 0; i < tris.Length; i += 3)
                {
                    Vector3 face = Vector3.Cross(verts[tris[i + 1]] - verts[tris[i]], verts[tris[i + 2]] - verts[tris[i]]);
                    Vector3 nrm = norms[tris[i]] + norms[tris[i + 1]] + norms[tris[i + 2]];
                    agree += Vector3.Dot(face.normalized, nrm.normalized);
                }
            }

            if (agree >= 0f) return;

            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var tris = mesh.GetTriangles(s);
                for (int i = 0; i < tris.Length; i += 3)
                {
                    int tmp = tris[i + 1];
                    tris[i + 1] = tris[i + 2];
                    tris[i + 2] = tmp;
                }
                mesh.SetTriangles(tris, s);
            }
        }
    }
}
