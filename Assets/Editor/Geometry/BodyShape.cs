using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// The limbs' skin geometry, defined once. The skin is built from these and muscles are
    /// placed relative to them, so a muscle described as "2 cm under the skin on the front
    /// of the thigh" stays 2 cm under the skin if the thigh is later made fatter. Left side;
    /// the right is the mirror. Angles run around the limb: 0 = front (-Z), 90 = outer
    /// (+X on the left), 180 = back, 270 = inner.
    /// </summary>
    public static class BodyShape
    {
        public struct Limb
        {
            public Vector3 A, B;      // centre-line ends (y is the height)
            public float RA, RB;      // radius at each end
            public float BulgeY, BulgeHalf, BulgeBack; // optional posterior bulge (the calf)
        }

        public static readonly Limb UpperArm = new Limb { A = new Vector3(0.19f, 1.39f, 0.0f), B = new Vector3(0.19f, 1.095f, 0.012f), RA = 0.058f, RB = 0.044f };
        public static readonly Limb Forearm = new Limb { A = new Vector3(0.19f, 1.095f, 0.012f), B = new Vector3(0.205f, 0.83f, 0.010f), RA = 0.044f, RB = 0.030f };
        public static readonly Limb Thigh = new Limb { A = new Vector3(0.09f, 0.87f, 0.005f), B = new Vector3(0.088f, 0.475f, 0.020f), RA = 0.085f, RB = 0.064f };
        public static readonly Limb Shank = new Limb
        {
            A = new Vector3(0.088f, 0.475f, 0.020f), B = new Vector3(0.083f, 0.078f, 0.015f), RA = 0.058f, RB = 0.036f,
            BulgeY = 0.32f, BulgeHalf = 0.11f, BulgeBack = 0.021f,
        };

        public static float RadiusAt(Limb l, float y)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(l.A.y, l.B.y, y));
            return Mathf.Lerp(l.RA, l.RB, t);
        }

        public static Vector3 CentreAt(Limb l, float y)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(l.A.y, l.B.y, y));
            return new Vector3(Mathf.Lerp(l.A.x, l.B.x, t), y, Mathf.Lerp(l.A.z, l.B.z, t));
        }

        /// <summary>A point on the plane through the limb at height y, at angle phi round it, and
        /// <paramref name="depth"/> metres inside the skin.</summary>
        public static Vector3 Under(Limb l, float y, float phiDegrees, float depth)
        {
            float phi = phiDegrees * Mathf.Deg2Rad;
            Vector3 c = CentreAt(l, y);
            float r = RadiusAt(l, y);
            if (l.BulgeBack > 0f)
            {
                float u = (y - l.BulgeY) / l.BulgeHalf;
                float bulge = l.BulgeBack * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u));
                r += bulge * Mathf.Max(0f, -Mathf.Cos(phi));   // only toward the back
            }
            return c + new Vector3(Mathf.Sin(phi), 0f, -Mathf.Cos(phi)) * (r - depth);
        }
    }
}
