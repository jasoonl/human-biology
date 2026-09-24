using System;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>A signed distance field: negative inside the shape, positive outside.</summary>
    public delegate float SdfFunc(Vector3 p);

    /// <summary>
    /// Signed-distance-field modelling. Bones, organs and the skin shell are described
    /// as maths - "a round cone from here to there, smoothly joined to a sphere, with
    /// an eye socket cut out" - and turned into a mesh by <see cref="SurfaceNets"/>.
    /// That is what lets a pelvis have an obturator foramen and a skull have orbits,
    /// which no assembly of Unity's built-in sphere/capsule primitives can do.
    ///
    /// Everything is in metres, in the figure's own space: faces -Z, anatomical left is
    /// +X, soles at y = 0. Distances are approximate for some shapes (ellipsoids,
    /// smooth unions), which is fine for meshing but means offsets should stay small.
    /// </summary>
    public static class Sdf
    {
        // ------------------------------------------------------------------ primitives

        public static SdfFunc Sphere(Vector3 center, float radius) =>
            p => (p - center).magnitude - radius;

        /// <summary>Inigo Quilez's ellipsoid bound: exact on the surface, a good
        /// approximation close to it, which is all a mesher samples.</summary>
        public static SdfFunc Ellipsoid(Vector3 center, Vector3 radii)
        {
            var inv = new Vector3(1f / radii.x, 1f / radii.y, 1f / radii.z);
            var inv2 = new Vector3(inv.x * inv.x, inv.y * inv.y, inv.z * inv.z);
            float minRadius = Mathf.Min(radii.x, Mathf.Min(radii.y, radii.z));
            return p =>
            {
                Vector3 q = p - center;
                float k0 = Vector3.Scale(q, inv).magnitude;
                float k1 = Vector3.Scale(q, inv2).magnitude;
                return k1 < 1e-9f ? -minRadius : k0 * (k0 - 1f) / k1;
            };
        }

        /// <summary>An ellipsoid whose axes are rotated by <paramref name="rotation"/>
        /// about its own centre.</summary>
        public static SdfFunc Ellipsoid(Vector3 center, Vector3 radii, Quaternion rotation) =>
            Rotated(Ellipsoid(center, radii), center, rotation);

        /// <summary>A capsule with different radii at each end - the workhorse for long
        /// bones, limb segments, ribs and any tapering rod.</summary>
        public static SdfFunc RoundCone(Vector3 a, float ra, Vector3 b, float rb)
        {
            Vector3 ba = b - a;
            float l2 = Mathf.Max(Vector3.Dot(ba, ba), 1e-10f);
            float rr = ra - rb;
            float a2 = l2 - rr * rr;
            float il2 = 1f / l2;
            return p =>
            {
                Vector3 pa = p - a;
                float y = Vector3.Dot(pa, ba);
                float z = y - l2;
                Vector3 xv = pa * l2 - ba * y;
                float x2 = Vector3.Dot(xv, xv);
                float y2 = y * y * l2;
                float z2 = z * z * l2;
                float k = Mathf.Sign(rr) * rr * rr * x2;
                if (Mathf.Sign(z) * a2 * z2 > k) return Mathf.Sqrt(x2 + z2) * il2 - rb;
                if (Mathf.Sign(y) * a2 * y2 < k) return Mathf.Sqrt(x2 + y2) * il2 - ra;
                return (Mathf.Sqrt(x2 * a2 * il2) + y * rr) * il2 - ra;
            };
        }

        public static SdfFunc Capsule(Vector3 a, Vector3 b, float radius) => RoundCone(a, radius, b, radius);

        /// <summary>A chain of round cones through a polyline, radius interpolated
        /// per point. Smooth-unioned when <paramref name="blend"/> is above zero, so
        /// joints between segments do not show a crease.</summary>
        public static SdfFunc Chain(Vector3[] points, float[] radii, float blend = 0f)
        {
            var segments = new SdfFunc[points.Length - 1];
            for (int i = 0; i < segments.Length; i++)
                segments[i] = RoundCone(points[i], radii[i], points[i + 1], radii[i + 1]);
            return blend > 0f ? SmoothUnion(blend, segments) : Union(segments);
        }

        public static SdfFunc RoundBox(Vector3 center, Vector3 halfExtents, float rounding, Quaternion rotation)
        {
            Vector3 inner = halfExtents - Vector3.one * rounding;
            inner = Vector3.Max(inner, Vector3.zero);
            var box = new SdfFunc(p =>
            {
                Vector3 q = p - center;
                var d = new Vector3(Mathf.Abs(q.x) - inner.x, Mathf.Abs(q.y) - inner.y, Mathf.Abs(q.z) - inner.z);
                Vector3 outside = Vector3.Max(d, Vector3.zero);
                return outside.magnitude + Mathf.Min(Mathf.Max(d.x, Mathf.Max(d.y, d.z)), 0f) - rounding;
            });
            return Rotated(box, center, rotation);
        }

        /// <summary>A torus lying around the local Y axis, then rotated.</summary>
        public static SdfFunc Torus(Vector3 center, Quaternion rotation, float majorRadius, float minorRadius)
        {
            var torus = new SdfFunc(p =>
            {
                Vector3 q = p - center;
                float ring = Mathf.Sqrt(q.x * q.x + q.z * q.z) - majorRadius;
                return Mathf.Sqrt(ring * ring + q.y * q.y) - minorRadius;
            });
            return Rotated(torus, center, rotation);
        }

        /// <summary>A thin triangular plate - the shoulder blade and the iliac wing are
        /// each a curved sheet of bone only a few millimetres thick.</summary>
        public static SdfFunc TrianglePlate(Vector3 a, Vector3 b, Vector3 c, float halfThickness)
        {
            Vector3 ba = b - a, cb = c - b, ac = a - c;
            Vector3 nor = Vector3.Cross(ba, ac);
            float norLen2 = Mathf.Max(Vector3.Dot(nor, nor), 1e-12f);
            float ba2 = Mathf.Max(Vector3.Dot(ba, ba), 1e-12f);
            float cb2 = Mathf.Max(Vector3.Dot(cb, cb), 1e-12f);
            float ac2 = Mathf.Max(Vector3.Dot(ac, ac), 1e-12f);
            return p =>
            {
                Vector3 pa = p - a, pb = p - b, pc = p - c;
                float s0 = Mathf.Sign(Vector3.Dot(Vector3.Cross(ba, nor), pa));
                float s1 = Mathf.Sign(Vector3.Dot(Vector3.Cross(cb, nor), pb));
                float s2 = Mathf.Sign(Vector3.Dot(Vector3.Cross(ac, nor), pc));
                float d2;
                if (s0 + s1 + s2 < 2f)
                {
                    Vector3 e0 = ba * Mathf.Clamp01(Vector3.Dot(ba, pa) / ba2) - pa;
                    Vector3 e1 = cb * Mathf.Clamp01(Vector3.Dot(cb, pb) / cb2) - pb;
                    Vector3 e2 = ac * Mathf.Clamp01(Vector3.Dot(ac, pc) / ac2) - pc;
                    d2 = Mathf.Min(Vector3.Dot(e0, e0), Mathf.Min(Vector3.Dot(e1, e1), Vector3.Dot(e2, e2)));
                }
                else
                {
                    float h = Vector3.Dot(nor, pa);
                    d2 = h * h / norLen2;
                }
                return Mathf.Sqrt(d2) - halfThickness;
            };
        }

        /// <summary>A flat quadrilateral plate, made of two triangles that share a
        /// diagonal, for sheets that are not triangular.</summary>
        public static SdfFunc QuadPlate(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float halfThickness) =>
            Union(TrianglePlate(a, b, c, halfThickness), TrianglePlate(a, c, d, halfThickness));

        /// <summary>Everything on the positive side of the plane through
        /// <paramref name="point"/> with the given normal.</summary>
        public static SdfFunc HalfSpace(Vector3 point, Vector3 normal)
        {
            Vector3 n = normal.normalized;
            return p => Vector3.Dot(p - point, n);
        }

        // ------------------------------------------------------------------ transforms

        public static SdfFunc Translated(SdfFunc f, Vector3 offset) => p => f(p - offset);

        public static SdfFunc Rotated(SdfFunc f, Vector3 pivot, Quaternion rotation)
        {
            Quaternion inverse = Quaternion.Inverse(rotation);
            return p => f(pivot + inverse * (p - pivot));
        }

        /// <summary>Reflect across x = 0: describe the anatomical left once, get both.</summary>
        public static SdfFunc MirrorX(SdfFunc f) => p => f(new Vector3(Mathf.Abs(p.x), p.y, p.z));

        // ------------------------------------------------------------------ booleans

        public static SdfFunc Union(params SdfFunc[] shapes) => p =>
        {
            float d = float.MaxValue;
            for (int i = 0; i < shapes.Length; i++) d = Mathf.Min(d, shapes[i](p));
            return d;
        };

        /// <summary>Union that blends the shapes together over a distance of about
        /// <paramref name="k"/>, the way a bone's shaft flares into its head.</summary>
        public static SdfFunc SmoothUnion(float k, params SdfFunc[] shapes) => p =>
        {
            float d = shapes[0](p);
            for (int i = 1; i < shapes.Length; i++) d = SMin(d, shapes[i](p), k);
            return d;
        };

        public static SdfFunc Subtract(SdfFunc from, params SdfFunc[] cutters) => p =>
        {
            float d = from(p);
            for (int i = 0; i < cutters.Length; i++) d = Mathf.Max(d, -cutters[i](p));
            return d;
        };

        public static SdfFunc SmoothSubtract(float k, SdfFunc from, SdfFunc cutter) =>
            p => -SMin(-from(p), cutter(p), k);

        public static SdfFunc Intersect(params SdfFunc[] shapes) => p =>
        {
            float d = float.MinValue;
            for (int i = 0; i < shapes.Length; i++) d = Mathf.Max(d, shapes[i](p));
            return d;
        };

        /// <summary>Grow (positive) or shrink (negative) a shape by a fixed distance.</summary>
        public static SdfFunc Offset(SdfFunc f, float distance) => p => f(p) - distance;

        /// <summary>Roughen a surface with 3D noise, for the folded look of a brain or
        /// the mottled surface of a liver.</summary>
        public static SdfFunc Displace(SdfFunc f, float amplitude, float frequency, int seed) =>
            p => f(p) + amplitude * Noise3(p * frequency, seed);

        public static float SMin(float a, float b, float k)
        {
            float h = Mathf.Max(k - Mathf.Abs(a - b), 0f) / k;
            return Mathf.Min(a, b) - h * h * k * 0.25f;
        }

        // ------------------------------------------------------------------ noise

        /// <summary>Smooth value noise in roughly [-1, 1]. Deterministic, so the same
        /// seed always builds the same organ.</summary>
        public static float Noise3(Vector3 p, int seed)
        {
            int x0 = Mathf.FloorToInt(p.x), y0 = Mathf.FloorToInt(p.y), z0 = Mathf.FloorToInt(p.z);
            float fx = p.x - x0, fy = p.y - y0, fz = p.z - z0;
            float ux = fx * fx * (3f - 2f * fx), uy = fy * fy * (3f - 2f * fy), uz = fz * fz * (3f - 2f * fz);

            float c000 = Hash(x0, y0, z0, seed), c100 = Hash(x0 + 1, y0, z0, seed);
            float c010 = Hash(x0, y0 + 1, z0, seed), c110 = Hash(x0 + 1, y0 + 1, z0, seed);
            float c001 = Hash(x0, y0, z0 + 1, seed), c101 = Hash(x0 + 1, y0, z0 + 1, seed);
            float c011 = Hash(x0, y0 + 1, z0 + 1, seed), c111 = Hash(x0 + 1, y0 + 1, z0 + 1, seed);

            float x00 = Mathf.Lerp(c000, c100, ux), x10 = Mathf.Lerp(c010, c110, ux);
            float x01 = Mathf.Lerp(c001, c101, ux), x11 = Mathf.Lerp(c011, c111, ux);
            float y0v = Mathf.Lerp(x00, x10, uy), y1v = Mathf.Lerp(x01, x11, uy);
            return Mathf.Lerp(y0v, y1v, uz) * 2f - 1f;
        }

        private static float Hash(int x, int y, int z, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + z * 1274126177 + seed * 362437;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)0x7fffffff;
            }
        }
    }
}
