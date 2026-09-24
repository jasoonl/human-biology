using System;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// Shared plumbing for the branching networks - arteries, veins, nerves, lymphatics. Each one
    /// is a lofted tube through a handful of control points, added to the scene with its entity
    /// id; a structure that exists on both sides is modelled once on the anatomical left and
    /// mirrored. Also the two placement tricks these networks lean on: hugging the skin from
    /// inside (superficial veins, cutaneous nerves) and hugging an organ (coronary arteries).
    /// </summary>
    public sealed class TubeKit
    {
        private readonly Transform _root;
        private readonly Material _material;
        private readonly int _layer;
        public int Count { get; private set; }

        public TubeKit(Transform root, Material material, int layer)
        {
            _root = root;
            _material = material;
            _layer = layer;
        }

        public static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        /// <summary>A tube whose radius tapers from <paramref name="r0"/> to <paramref name="r1"/>.
        /// With <paramref name="pair"/> it is drawn on both sides of the body, the points describing
        /// the left; otherwise it is placed exactly as given.</summary>
        public void Add(string name, string id, float r0, float r1, bool pair, params Vector3[] points) =>
            Add(name, id, Loft.Taper(r0, r1), pair, points);

        public void Add(string name, string id, Func<float, float> radius, bool pair, params Vector3[] points)
        {
            var o = Loft.Options.Default;
            float r = Mathf.Max(radius(0f), radius(1f));
            o.Sides = r >= 0.006f ? 14 : r >= 0.003f ? 10 : 8;
            o.RingsPerMetre = 240f;
            o.CapRings = 2;
            o.VTiling = 8f;

            Mesh mesh = PartFactory.Save(Loft.Tube(name, points, radius, o), name);
            if (pair) PartFactory.AddPair(_root, name, id, mesh, _material, _layer);
            else PartFactory.Add(_root, name, id, mesh, _material, _layer);
            Count++;
        }

        /// <summary>The same path reflected to the other side of the body.</summary>
        public static Vector3[] Mirror(params Vector3[] points)
        {
            var result = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++) result[i] = new Vector3(-points[i].x, points[i].y, points[i].z);
            return result;
        }

        /// <summary>A path running alongside another, displaced sideways and front-to-back.
        /// Vessels and nerves travel together in bundles, so a vein is placed as its artery's
        /// neighbour rather than being guessed separately.</summary>
        public static Vector3[] Beside(Vector3[] path, float dx, float dz, float dy = 0f)
        {
            var result = new Vector3[path.Length];
            for (int i = 0; i < path.Length; i++) result[i] = path[i] + new Vector3(dx, dy, dz);
            return result;
        }

        /// <summary>Where a rib's bony arc starts (leaving the spine) and ends (turning to cartilage),
        /// in the angle used by <see cref="SkeletonBuilder.RibPoint"/>.</summary>
        public static float RibStart(int rib)
        {
            SkeletonBuilder.RibFrame(rib, out _, out _, out float width, out _, out _, out _, out _);
            return Mathf.Asin(Mathf.Clamp(0.040f / width, 0.05f, 0.9f)) + 0.04f;
        }

        public static float RibEnd(int rib) => (rib >= 10 ? 0.56f : 0.86f) * Mathf.PI - 0.04f;

        /// <summary>Points of <paramref name="path"/> that lie on the given ribs' inner faces:
        /// the running of an intercostal bundle, which follows the lower border of its rib.</summary>
        public static Vector3[] AlongRib(int rib, float fromTheta, float toTheta, int steps, float standoff, float drop)
        {
            var points = new Vector3[steps];
            for (int i = 0; i < steps; i++)
            {
                float theta = Mathf.Lerp(fromTheta, toTheta, i / (float)(steps - 1));
                points[i] = SkeletonBuilder.RibPoint(rib, theta, standoff) + Vector3.down * drop;
            }
            return points;
        }

        // ---------------------------------------------------------------- surfaces

        /// <summary>Move a point along the surface normal until it lies <paramref name="depth"/>
        /// metres inside the skin. Superficial veins and cutaneous nerves are sketched roughly and
        /// then settled onto the skin from underneath, so they follow every curve of the body.</summary>
        public static Vector3 UnderSkin(Vector3 p, float depth) => Settle(SkinBuilder.Field, p, -depth);

        /// <summary>Move a point onto a surface standing <paramref name="standoff"/> metres proud of
        /// the shape (negative = inside it).</summary>
        public static Vector3 Settle(SdfFunc field, Vector3 p, float standoff)
        {
            if (field == null) return p;
            for (int i = 0; i < 8; i++)
            {
                float d = field(p) - standoff;
                if (Mathf.Abs(d) < 0.0002f) break;
                p -= Gradient(field, p) * d;
            }
            return p;
        }

        public static Vector3[] SettleAll(SdfFunc field, float standoff, params Vector3[] points)
        {
            var result = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++) result[i] = Settle(field, points[i], standoff);
            return result;
        }

        public static Vector3[] UnderSkinAll(float depth, params Vector3[] points) => SettleAll(SkinBuilder.Field, -depth, points);

        private static Vector3 Gradient(SdfFunc f, Vector3 p)
        {
            const float e = 0.0015f;
            var g = new Vector3(
                f(p + new Vector3(e, 0, 0)) - f(p - new Vector3(e, 0, 0)),
                f(p + new Vector3(0, e, 0)) - f(p - new Vector3(0, e, 0)),
                f(p + new Vector3(0, 0, e)) - f(p - new Vector3(0, 0, e)));
            return g.sqrMagnitude < 1e-12f ? Vector3.up : g.normalized;
        }
    }
}
