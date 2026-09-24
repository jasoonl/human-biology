using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// The vertebral column, ribs, costal cartilage and sternum.
    ///
    /// Everything hangs off one spine curve, so the ribs actually meet the vertebrae
    /// they articulate with and the sternum sits where the cartilages arrive. An earlier
    /// version placed vertebrae, ribs and sternum independently and they did not connect:
    /// the atlas floated 4 cm below the skull and the ribs started at the midline.
    /// </summary>
    public static partial class SkeletonBuilder
    {
        // ------------------------------------------------------------ spine landmarks

        /// <summary>Height of each vertebral body's centre, top of the column downward.
        /// Cervical spine sits directly under the skull base; the lumbar spine ends on
        /// the sacrum's first segment.</summary>
        public static float CervicalY(int i) => Mathf.Lerp(1.555f, 1.452f, i / 6f);   // C1..C7
        public static float ThoracicY(int i) => Mathf.Lerp(1.435f, 1.148f, i / 11f);  // T1..T12
        public static float LumbarY(int i) => Mathf.Lerp(1.118f, 0.993f, i / 4f);     // L1..L5

        private static readonly Vector2[] SpineCurve =
        {
            new Vector2(1.575f, 0.036f), new Vector2(1.510f, 0.031f), new Vector2(1.470f, 0.034f),
            new Vector2(1.440f, 0.041f), new Vector2(1.400f, 0.052f), new Vector2(1.330f, 0.063f),
            new Vector2(1.270f, 0.066f), new Vector2(1.200f, 0.061f), new Vector2(1.148f, 0.054f),
            new Vector2(1.118f, 0.049f), new Vector2(1.055f, 0.041f), new Vector2(0.993f, 0.046f),
            new Vector2(0.960f, 0.054f),
        };

        /// <summary>Z of the vertebral bodies at a given height: the three curves of the
        /// spine - cervical lordosis, thoracic kyphosis, lumbar lordosis. The bodies lie
        /// forward of the vertebral canal, which the spinal cord occupies.</summary>
        public static float SpineBodyZ(float y)
        {
            for (int i = 0; i < SpineCurve.Length - 1; i++)
            {
                Vector2 a = SpineCurve[i], b = SpineCurve[i + 1];
                if (y <= a.x && y >= b.x)
                    return Mathf.Lerp(a.y, b.y, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a.x, b.x, y)));
            }
            return y > SpineCurve[0].x ? SpineCurve[0].y : SpineCurve[SpineCurve.Length - 1].y;
        }

        /// <summary>Centre of the vertebral canal, where the spinal cord runs.</summary>
        public static Vector3 SpineCanal(float y) => new Vector3(0f, y, SpineBodyZ(y) + 0.019f);

        private static Quaternion SpineTilt(float y)
        {
            float dz = SpineBodyZ(y - 0.01f) - SpineBodyZ(y + 0.01f); // dz per 2 cm of height
            return Quaternion.FromToRotation(Vector3.up, new Vector3(0f, 0.02f, -dz).normalized);
        }

        // ------------------------------------------------------------ vertebra shapes

        private static SdfFunc ThoracicVertebra()
        {
            SdfFunc body = Sdf.Ellipsoid(Vector3.zero, new Vector3(0.0135f, 0.0095f, 0.0125f));
            SdfFunc pedicles = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0105f, 0.001f, 0.008f), 0.0038f, new Vector3(0.0108f, 0.002f, 0.0215f), 0.0035f));
            SdfFunc laminae = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0108f, 0.002f, 0.0215f), 0.0034f, new Vector3(0.0025f, 0.004f, 0.0295f), 0.0036f));
            // Thoracic spinous processes slope steeply downward and overlap like roof tiles.
            SdfFunc spinous = Sdf.RoundCone(new Vector3(0f, 0.004f, 0.0295f), 0.0058f, new Vector3(0f, -0.014f, 0.0620f), 0.0044f);
            SdfFunc transverse = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0108f, 0.003f, 0.0220f), 0.0045f, new Vector3(0.0300f, 0.004f, 0.0300f), 0.0052f));
            SdfFunc superiorFacets = Sdf.MirrorX(Sdf.Sphere(new Vector3(0.0088f, 0.0108f, 0.0225f), 0.0038f));
            SdfFunc inferiorFacets = Sdf.MirrorX(Sdf.Sphere(new Vector3(0.0078f, -0.0112f, 0.0300f), 0.0035f));
            return Sdf.SmoothUnion(0.004f, body, pedicles, laminae, spinous, transverse, superiorFacets, inferiorFacets);
        }

        private static SdfFunc CervicalVertebra()
        {
            SdfFunc body = Sdf.Ellipsoid(Vector3.zero, new Vector3(0.0105f, 0.0068f, 0.0085f));
            SdfFunc pedicles = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0078f, 0.001f, 0.005f), 0.0030f, new Vector3(0.0095f, 0.001f, 0.0150f), 0.0030f));
            SdfFunc laminae = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0095f, 0.001f, 0.0150f), 0.0028f, new Vector3(0.0020f, 0.002f, 0.0215f), 0.0030f));
            SdfFunc spinous = Sdf.RoundCone(new Vector3(0f, 0.002f, 0.0215f), 0.0042f, new Vector3(0f, -0.002f, 0.0400f), 0.0032f);
            // Short transverse processes, each pierced by the foramen for the vertebral artery.
            SdfFunc transverse = Sdf.Subtract(
                Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0092f, 0.001f, 0.0100f), 0.0036f, new Vector3(0.0230f, 0.001f, 0.0090f), 0.0040f)),
                Sdf.MirrorX(Sdf.Sphere(new Vector3(0.0125f, 0f, 0.0095f), 0.0020f)));
            SdfFunc pillars = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.0128f, 0f, 0.0165f), new Vector3(0.0040f, 0.0078f, 0.0048f)));
            return Sdf.SmoothUnion(0.003f, body, pedicles, laminae, spinous, transverse, pillars);
        }

        private static SdfFunc LumbarVertebra()
        {
            SdfFunc body = Sdf.Ellipsoid(Vector3.zero, new Vector3(0.0195f, 0.0125f, 0.0145f));
            SdfFunc pedicles = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0125f, 0.001f, 0.010f), 0.0055f, new Vector3(0.0130f, 0.002f, 0.0245f), 0.0050f));
            SdfFunc laminae = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0130f, 0.002f, 0.0245f), 0.0048f, new Vector3(0.0035f, 0.003f, 0.0335f), 0.0050f));
            // Lumbar spinous processes are thick and blunt, projecting straight back.
            SdfFunc spinous = Sdf.RoundCone(new Vector3(0f, 0.003f, 0.0335f), 0.0072f, new Vector3(0f, 0.002f, 0.0620f), 0.0064f);
            SdfFunc transverse = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0130f, 0.002f, 0.0240f), 0.0050f, new Vector3(0.0400f, 0.001f, 0.0250f), 0.0042f));
            SdfFunc superiorFacets = Sdf.MirrorX(Sdf.Sphere(new Vector3(0.0100f, 0.0138f, 0.0270f), 0.0050f));
            SdfFunc inferiorFacets = Sdf.MirrorX(Sdf.Sphere(new Vector3(0.0100f, -0.0138f, 0.0335f), 0.0045f));
            return Sdf.SmoothUnion(0.005f, body, pedicles, laminae, spinous, transverse, superiorFacets, inferiorFacets);
        }

        /// <summary>The atlas (C1): no body, just a ring of bone that carries the skull -
        /// two lateral masses joined by a slim front and back arch.</summary>
        private static SdfFunc Atlas()
        {
            SdfFunc masses = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.0175f, 0f, 0.0035f), new Vector3(0.0088f, 0.0075f, 0.0115f)));
            SdfFunc anteriorArch = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0175f, 0f, 0.0005f), 0.0045f, new Vector3(0f, 0f, -0.0105f), 0.0045f));
            SdfFunc posteriorArch = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0175f, 0f, 0.0070f), 0.0038f, new Vector3(0f, 0.001f, 0.0250f), 0.0040f));
            SdfFunc transverse = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0175f, 0f, 0.0060f), 0.0042f, new Vector3(0.0345f, 0f, 0.0060f), 0.0042f));
            return Sdf.SmoothUnion(0.003f, masses, anteriorArch, posteriorArch, transverse);
        }

        /// <summary>The axis (C2): a body with the dens, the peg the atlas pivots on.</summary>
        private static SdfFunc Axis()
        {
            SdfFunc body = Sdf.Ellipsoid(Vector3.zero, new Vector3(0.0105f, 0.0075f, 0.0090f));
            SdfFunc dens = Sdf.RoundCone(new Vector3(0f, 0.0075f, -0.0010f), 0.0058f, new Vector3(0f, 0.0235f, -0.0005f), 0.0046f);
            SdfFunc facets = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.0150f, 0.0078f, 0.0035f), new Vector3(0.0078f, 0.0035f, 0.0095f)));
            SdfFunc pedicles = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0085f, 0.001f, 0.005f), 0.0034f, new Vector3(0.0100f, 0.001f, 0.0165f), 0.0032f));
            SdfFunc laminae = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0100f, 0.001f, 0.0165f), 0.0032f, new Vector3(0.0022f, 0.002f, 0.0225f), 0.0034f));
            SdfFunc spinous = Sdf.RoundCone(new Vector3(0f, 0.002f, 0.0225f), 0.0055f, new Vector3(0f, -0.001f, 0.0440f), 0.0040f);
            SdfFunc transverse = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.0095f, 0f, 0.0100f), 0.0034f, new Vector3(0.0210f, -0.001f, 0.0090f), 0.0036f));
            return Sdf.SmoothUnion(0.003f, body, dens, facets, pedicles, laminae, spinous, transverse);
        }

        private static Mesh Template(string name, SdfFunc shape, float voxel)
        {
            var min = new Vector3(-0.045f, -0.030f, -0.022f);
            var max = new Vector3(0.045f, 0.032f, 0.072f);
            return PartFactory.Save(SurfaceNets.Build(shape, min, max, voxel, name), name);
        }

        private static void BuildVertebralColumn(Transform root, Material bone, Material cartilage, int layer)
        {
            Mesh atlasMesh = Template("Vertebra_Atlas", Atlas(), 0.0012f);
            Mesh axisMesh = Template("Vertebra_Axis", Axis(), 0.0012f);
            Mesh cervicalMesh = Template("Vertebra_Cervical", CervicalVertebra(), 0.0012f);
            Mesh thoracicMesh = Template("Vertebra_Thoracic", ThoracicVertebra(), 0.0013f);
            Mesh lumbarMesh = Template("Vertebra_Lumbar", LumbarVertebra(), 0.0015f);

            void Place(string name, string id, Mesh mesh, float y, float scale)
            {
                var go = PartFactory.Add(root, name, id, mesh, bone, layer);
                go.transform.localPosition = new Vector3(0f, y, SpineBodyZ(y));
                go.transform.localRotation = SpineTilt(y);
                go.transform.localScale = Vector3.one * scale;
            }

            Place("Vertebra_C1_Atlas", "SYS_SK_ATLAS", atlasMesh, CervicalY(0), 1f);
            Place("Vertebra_C2_Axis", "SYS_SK_AXIS", axisMesh, CervicalY(1), 1f);
            for (int i = 2; i < 7; i++)
                Place($"Vertebra_C{i + 1}", "SYS_SK_VERT_CERVICAL", cervicalMesh, CervicalY(i), Mathf.Lerp(0.94f, 1.14f, (i - 2) / 4f));
            for (int i = 0; i < 12; i++)
                Place($"Vertebra_T{i + 1}", "SYS_SK_VERT_THORACIC", thoracicMesh, ThoracicY(i), Mathf.Lerp(0.86f, 1.22f, i / 11f));
            for (int i = 0; i < 5; i++)
                Place($"Vertebra_L{i + 1}", "SYS_SK_VERT_LUMBAR", lumbarMesh, LumbarY(i), Mathf.Lerp(0.95f, 1.07f, i / 4f));

            // Intervertebral discs: fibrocartilage pads that give the column its
            // spring and take up about a quarter of its length.
            Mesh discCervical = Template("Disc_Cervical", Sdf.Ellipsoid(Vector3.zero, new Vector3(0.0112f, 0.0034f, 0.0092f)), 0.0010f);
            Mesh discThoracic = Template("Disc_Thoracic", Sdf.Ellipsoid(Vector3.zero, new Vector3(0.0145f, 0.0038f, 0.0135f)), 0.0011f);
            Mesh discLumbar = Template("Disc_Lumbar", Sdf.Ellipsoid(Vector3.zero, new Vector3(0.0210f, 0.0044f, 0.0158f)), 0.0013f);

            void Disc(string name, Mesh mesh, float yUpper, float yLower, float scale)
            {
                float y = (yUpper + yLower) * 0.5f;
                var go = PartFactory.Add(root, name, "SYS_SK_DISC", mesh, cartilage, layer);
                go.transform.localPosition = new Vector3(0f, y, SpineBodyZ(y));
                go.transform.localRotation = SpineTilt(y);
                go.transform.localScale = new Vector3(scale, 1f, scale);
            }

            for (int i = 1; i < 6; i++)
                Disc($"Disc_C{i + 1}_C{i + 2}", discCervical, CervicalY(i), CervicalY(i + 1), Mathf.Lerp(0.98f, 1.14f, (i - 1) / 4f));
            Disc("Disc_C7_T1", discThoracic, CervicalY(6), ThoracicY(0), 0.86f);
            for (int i = 0; i < 11; i++)
                Disc($"Disc_T{i + 1}_T{i + 2}", discThoracic, ThoracicY(i), ThoracicY(i + 1), Mathf.Lerp(0.86f, 1.22f, (i + 0.5f) / 11f));
            Disc("Disc_T12_L1", discLumbar, ThoracicY(11), LumbarY(0), 0.86f);
            for (int i = 0; i < 4; i++)
                Disc($"Disc_L{i + 1}_L{i + 2}", discLumbar, LumbarY(i), LumbarY(i + 1), Mathf.Lerp(0.95f, 1.07f, (i + 0.5f) / 4f));
            Disc("Disc_L5_S1", discLumbar, LumbarY(4), 0.976f, 1.07f);
        }

        // ------------------------------------------------------------ thorax

        // Where each rib's cartilage meets the sternum: height, and the sternum's depth there.
        private static readonly Vector2[] SternalAttach =
        {
            new Vector2(1.412f, -0.062f), new Vector2(1.378f, -0.071f), new Vector2(1.352f, -0.078f),
            new Vector2(1.331f, -0.083f), new Vector2(1.311f, -0.088f), new Vector2(1.290f, -0.094f),
            new Vector2(1.268f, -0.099f),
        };

        /// <summary>The plan-view geometry of rib <paramref name="rib"/> (0 = first): its vertebral
        /// level, half-width, front-to-back depth, and how far it slopes down toward the front.</summary>
        public static void RibFrame(int rib, out float yT, out float zB, out float width, out float depth,
            out float drop, out float zc, out float b)
        {
            float t = rib / 11f;
            yT = ThoracicY(rib);
            zB = SpineBodyZ(yT);
            width = rib <= 7 ? Mathf.Lerp(0.072f, 0.150f, rib / 7f) : Mathf.Lerp(0.150f, 0.108f, (rib - 7) / 4f);
            depth = RibDepth[rib];
            drop = Mathf.Lerp(0.040f, 0.115f, Mathf.Pow(t, 0.9f));
            float zPole = zB + 0.034f;
            zc = (zPole - depth) * 0.5f;
            b = (zPole + depth) * 0.5f;
        }

        /// <summary>A point on rib <paramref name="rib"/> at angle <paramref name="thetaRadians"/>
        /// round the chest (0 = at the spine, pi/2 = the side, pi = the front), pushed
        /// <paramref name="standoff"/> metres outward. Chest muscles are built from these so they
        /// lie on the ribs instead of near them.</summary>
        public static Vector3 RibPoint(int rib, float thetaRadians, float standoff = 0f)
        {
            RibFrame(rib, out float yT, out _, out float width, out _, out float drop, out float zc, out float b);
            float sweep = (1f - Mathf.Cos(thetaRadians)) * 0.5f;
            var p = new Vector3(width * Mathf.Sin(thetaRadians), yT + 0.004f - drop * sweep, zc + b * Mathf.Cos(thetaRadians));
            var n = new Vector2(b * Mathf.Sin(thetaRadians), width * Mathf.Cos(thetaRadians)).normalized;
            return p + new Vector3(n.x, 0f, n.y) * standoff;
        }

        private static readonly float[] RibDepth = { 0.062f, 0.071f, 0.079f, 0.085f, 0.091f, 0.096f, 0.100f, 0.098f, 0.090f, 0.078f, 0.040f, 0.020f };

        private static void BuildThorax(Transform root, Material bone, Material cartilage, int layer)
        {
            BuildSternum(root, bone, layer);

            for (int rib = 0; rib < 12; rib++)
            {
                float t = rib / 11f;
                float yT = ThoracicY(rib);
                float zB = SpineBodyZ(yT);
                float width = rib <= 7 ? Mathf.Lerp(0.072f, 0.150f, rib / 7f) : Mathf.Lerp(0.150f, 0.108f, (rib - 7) / 4f);
                float depth = RibDepth[rib];
                float drop = Mathf.Lerp(0.040f, 0.115f, Mathf.Pow(t, 0.9f));
                bool floating = rib >= 10;
                float thetaEnd = floating ? 0.56f * Mathf.PI : 0.86f * Mathf.PI;

                // In plan the rib is a half-ellipse: widest at its side, its posterior
                // pole lying BEHIND the vertebral body (the ribs wrap round the spine, which
                // bulges into the chest between them).
                float zPole = zB + 0.034f;
                float zc = (zPole - depth) * 0.5f;
                float b = (zPole + depth) * 0.5f;

                Vector3 Arc(float theta)
                {
                    float sweep = (1f - Mathf.Cos(theta)) * 0.5f;
                    return new Vector3(width * Mathf.Sin(theta), yT + 0.004f - drop * sweep, zc + b * Mathf.Cos(theta));
                }

                var pts = new List<Vector3>
                {
                    new Vector3(0.010f, yT + 0.004f, zB + 0.003f),   // head, on the vertebral body
                    new Vector3(0.022f, yT + 0.007f, zB + 0.017f),   // neck
                };
                float theta0 = Mathf.Asin(Mathf.Clamp(0.040f / width, 0.05f, 0.9f));
                int steps = Mathf.Max(6, Mathf.CeilToInt((thetaEnd - theta0) / 0.26f));
                for (int s = 0; s <= steps; s++) pts.Add(Arc(Mathf.Lerp(theta0, thetaEnd, s / (float)steps)));

                // Flat strap of bone, broad in the vertical, thicker toward the angle.
                float thick = Mathf.Lerp(0.0033f, 0.0028f, t);
                var options = Loft.Options.Default;
                options.Sides = 8;
                options.Flatten = 1.75f;
                options.WidthAxis = Vector3.up;
                options.RingsPerMetre = 70f;
                Mesh mesh = Loft.Tube($"Rib{rib + 1}", pts, u => thick * (1f + 0.35f * Mathf.Exp(-Mathf.Pow((u - 0.18f) / 0.10f, 2f))), options);

                string id = rib < 7 ? "SYS_SK_RIB_TRUE" : (rib < 10 ? "SYS_SK_RIB_FALSE" : "SYS_SK_RIB_FLOATING");
                PartFactory.AddPair(root, $"Rib{rib + 1}", id, PartFactory.Save(mesh, $"Rib{rib + 1}"), bone, layer);

                if (floating) continue;

                // Costal cartilage: from the end of the bony rib to the sternum. The
                // false ribs (8-10) do not reach it - each ascends to the cartilage above.
                Vector3 end = Arc(thetaEnd);
                Vector3 target;
                if (rib < 7) target = new Vector3(0.019f, SternalAttach[rib].x, SternalAttach[rib].y);
                else target = new Vector3(0.022f + (rib - 7) * 0.012f, 1.262f - (rib - 7) * 0.004f, -0.098f + (rib - 7) * 0.006f);

                var cartPts = new List<Vector3> { end, Vector3.Lerp(end, target, 0.5f) + new Vector3(0f, rib < 7 ? 0.006f : 0.012f, -0.004f), target };
                var cartOptions = Loft.Options.Default;
                cartOptions.Sides = 8;
                cartOptions.Flatten = 1.4f;
                cartOptions.WidthAxis = Vector3.up;
                cartOptions.RingsPerMetre = 90f;
                Mesh cart = Loft.Tube($"CostalCartilage{rib + 1}", cartPts, Loft.Constant(thick * 0.95f), cartOptions);
                PartFactory.AddPair(root, $"CostalCartilage{rib + 1}", "SYS_SK_COSTAL_CARTILAGE",
                    PartFactory.Save(cart, $"CostalCartilage{rib + 1}"), cartilage, layer);
            }
        }

        private static void BuildSternum(Transform root, Material bone, int layer)
        {
            const float t = 0.0032f;

            // Manubrium, body and xiphoid process: three flat plates along a line that
            // slopes forward as it descends, so the sternum leans out from the chest.
            SdfFunc manubrium = Sdf.QuadPlate(
                new Vector3(0.031f, 1.428f, -0.058f), new Vector3(-0.031f, 1.428f, -0.058f),
                new Vector3(-0.025f, 1.376f, -0.072f), new Vector3(0.025f, 1.376f, -0.072f), t);
            SdfFunc body = Sdf.QuadPlate(
                new Vector3(0.019f, 1.376f, -0.072f), new Vector3(-0.019f, 1.376f, -0.072f),
                new Vector3(-0.014f, 1.246f, -0.103f), new Vector3(0.014f, 1.246f, -0.103f), t);
            SdfFunc xiphoid = Sdf.TrianglePlate(new Vector3(0.010f, 1.246f, -0.103f), new Vector3(-0.010f, 1.246f, -0.103f),
                new Vector3(0f, 1.212f, -0.108f), 0.0026f);

            SdfFunc sternum = Sdf.SmoothUnion(0.005f, manubrium, body, xiphoid,
                Sdf.Capsule(new Vector3(0.031f, 1.428f, -0.058f), new Vector3(-0.031f, 1.428f, -0.058f), 0.0038f));
            // Jugular notch: the dip at the top you can feel between the collarbones.
            sternum = Sdf.Subtract(sternum, Sdf.Sphere(new Vector3(0f, 1.440f, -0.052f), 0.0115f));

            // Costal notches: the scalloped edges where the seven true ribs' cartilages meet the sternum, and the
            // clavicular notches at the top corners of the manubrium.
            for (int i = 0; i < 7; i++)
                sternum = Sdf.SmoothSubtract(0.002f, sternum,
                    Sdf.MirrorX(Sdf.Sphere(new Vector3(0.0185f, SternalAttach[i].x, SternalAttach[i].y), 0.0042f)));
            sternum = Sdf.SmoothSubtract(0.002f, sternum, Sdf.MirrorX(Sdf.Sphere(new Vector3(0.032f, 1.426f, -0.060f), 0.0050f)));
            sternum = Sdf.Displace(sternum, 0.0004f, 220f, 3);   // the pitted surface of bone

            PartFactory.BoundsOf(out var min, out var max, 0.012f, new Vector3(-0.04f, 1.205f, -0.12f), new Vector3(0.04f, 1.445f, -0.045f));
            PartFactory.Add(root, "Sternum", "SYS_SK_STERNUM",
                PartFactory.Save(SurfaceNets.Build(sternum, min, max, 0.0016f, "Sternum"), "Sternum"), bone, layer);
        }
    }
}
