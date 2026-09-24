using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// The abdominal and pelvic organs. Solid organs (liver, spleen, kidneys) are sculpted fields;
    /// the hollow ones (oesophagus to rectum) are lofted tubes, because a gut really is a tube,
    /// and a tube can carry the sacculations of the colon and the fold of the small intestine
    /// that a blob cannot. The small intestine is a long tortuous coil, not a lump.
    /// </summary>
    public static partial class OrganBuilder
    {
        private static Mesh Gut(string name, IList<Vector3> path, System.Func<float, float> radius, int sides = 12, float flatten = 1f, float ringsPerMetre = 220f)
        {
            var o = Loft.Options.Default;
            o.Sides = sides; o.Flatten = flatten; o.RingsPerMetre = ringsPerMetre; o.CapRings = 3;
            return PartFactory.Save(Loft.Tube(name, path, radius, o), name);
        }

        /// <summary>Radius with regular sacculations (haustra), as on the colon.</summary>
        private static System.Func<float, float> Haustra(float radius, int count, float depth = 0.16f) =>
            t => radius * (1f + depth * Mathf.Cos(t * Mathf.PI * 2f * count));

        private static void BuildAbdomen()
        {
            BuildLiverAndBiliary();
            BuildStomachAndSpleen();
            BuildPancreasAndDuodenum();
            BuildSmallIntestine();
            BuildLargeIntestine();
            BuildUrinary();
        }

        private static void BuildLiverAndBiliary()
        {
            // The liver: a large right lobe and a thin left lobe drawn across the midline, its top
            // moulded to the underside of the diaphragm, its underside carrying the gallbladder fossa.
            SdfFunc right = Sdf.Ellipsoid(V(-0.070f, 1.168f, -0.018f), V(0.062f, 0.058f, 0.074f), Quaternion.Euler(0f, 0f, -8f));
            SdfFunc left = Sdf.Ellipsoid(V(0.018f, 1.182f, -0.060f), V(0.054f, 0.038f, 0.036f), Quaternion.Euler(0f, 0f, 8f));
            SdfFunc liver = Sdf.SmoothUnion(0.030f, right, left);

            liver = Sdf.Intersect(liver, Sdf.Offset(DiaphragmDomes(), -0.003f));

            // Gallbladder fossa, the notch for the ligamentum teres, and the groove of the inferior vena cava.
            liver = Sdf.SmoothSubtract(0.008f, liver, Sdf.Ellipsoid(V(-0.048f, 1.118f, -0.070f), V(0.018f, 0.030f, 0.022f)));
            SdfFunc falciform = Sdf.Intersect(p => Mathf.Abs(p.x + 0.008f) - 0.0016f, InFrontOf(-0.030f));
            liver = Sdf.Subtract(liver, falciform);
            liver = Sdf.SmoothSubtract(0.006f, liver, Sdf.Capsule(V(-0.030f, 1.100f, 0.030f), V(-0.030f, 1.230f, 0.030f), 0.012f));

            Place("Liver", "SYS_DIG_LIVER", Make("Liver", liver, V(-0.15f, 1.09f, -0.10f), V(0.09f, 1.25f, 0.06f), 0.0026f),
                _tissue(new Color(0.46f, 0.20f, 0.16f), 0.42f));

            // Gallbladder: the pear-shaped bile store under the right lobe, green from its contents.
            SdfFunc gall = Sdf.SmoothUnion(0.006f,
                Sdf.Ellipsoid(V(-0.050f, 1.116f, -0.072f), V(0.013f, 0.028f, 0.015f), Quaternion.Euler(0f, 0f, -12f)),
                Sdf.RoundCone(V(-0.044f, 1.094f, -0.064f), 0.006f, V(-0.036f, 1.082f, -0.050f), 0.003f));
            Place("Gallbladder", "SYS_DIG_GALLBLADDER", Make("Gallbladder", gall, V(-0.075f, 1.065f, -0.10f), V(-0.02f, 1.16f, -0.03f), 0.0011f),
                _tissue(new Color(0.34f, 0.46f, 0.24f), 0.50f));
        }

        private static void BuildStomachAndSpleen()
        {
            // Stomach: a J-shaped bag - fundus under the left dome, body sweeping down and across,
            // antrum narrowing to the pylorus. Built as a chain of round cones with the greater
            // and lesser curvature emerging from how the radii change along it.
            SdfFunc stomach = Sdf.Chain(new[]
            {
                V(0.066f, 1.190f, -0.020f), V(0.062f, 1.168f, -0.032f), V(0.048f, 1.136f, -0.052f),
                V(0.020f, 1.106f, -0.064f), V(-0.014f, 1.098f, -0.062f), V(-0.038f, 1.102f, -0.056f),
            }, new[] { 0.034f, 0.036f, 0.031f, 0.023f, 0.017f, 0.012f }, 0.02f);
            stomach = Sdf.Displace(stomach, 0.0010f, 220f, 5);   // rugal folds on the surface
            Place("Stomach", "SYS_DIG_STOMACH", Make("Stomach", stomach, V(-0.07f, 1.07f, -0.11f), V(0.12f, 1.24f, 0.03f), 0.0015f),
                _tissue(new Color(0.84f, 0.60f, 0.52f), 0.42f));

            // Spleen: a fist-sized organ tucked under the left ribs, with a notched border.
            SdfFunc spleen = Sdf.Ellipsoid(V(0.104f, 1.176f, 0.036f), V(0.020f, 0.050f, 0.030f), Quaternion.Euler(0f, 18f, 8f));
            spleen = Sdf.SmoothSubtract(0.006f, spleen, Sdf.Sphere(V(0.088f, 1.176f, 0.024f), 0.010f));
            Place("Spleen", "SYS_LYMPH_SPLEEN", Make("Spleen", spleen, V(0.06f, 1.10f, -0.01f), V(0.14f, 1.25f, 0.09f), 0.0014f),
                _tissue(new Color(0.46f, 0.18f, 0.24f), 0.40f));
        }

        private static void BuildPancreasAndDuodenum()
        {
            // Pancreas: head tucked into the duodenal curve, body crossing in front of the spine,
            // tail reaching toward the spleen.
            SdfFunc pancreas = Sdf.Chain(new[]
            {
                V(-0.030f, 1.098f, -0.012f), V(-0.004f, 1.110f, -0.018f), V(0.030f, 1.124f, -0.004f), V(0.070f, 1.142f, 0.016f),
            }, new[] { 0.021f, 0.014f, 0.012f, 0.010f }, 0.012f);
            Place("Pancreas", "SYS_DIG_PANCREAS", Make("Pancreas", pancreas, V(-0.06f, 1.06f, -0.06f), V(0.10f, 1.18f, 0.04f), 0.0014f),
                _tissue(new Color(0.85f, 0.73f, 0.52f), 0.35f));

            // Duodenum: the C-shaped first part of the small intestine wrapping the pancreatic head.
            var duodenum = new[]
            {
                V(-0.038f, 1.096f, -0.056f), V(-0.046f, 1.098f, -0.032f), V(-0.050f, 1.086f, -0.008f), V(-0.046f, 1.062f, 0.004f),
                V(-0.032f, 1.040f, 0.006f), V(-0.006f, 1.034f, 0.010f), V(0.020f, 1.050f, 0.008f),
            };
            Place("Duodenum", "SYS_DIG_DUODENUM", Gut("Duodenum", duodenum, Loft.Constant(0.0125f), 12, 1f, 260f),
                _tissue(new Color(0.82f, 0.62f, 0.50f), 0.40f));
        }

        private static void BuildSmallIntestine()
        {
            // Six metres of tube folded into the middle of the abdomen. Rather than a lump, it is a
            // serpentine coil: rows sweeping side to side, each joined to the next by a U-turn.
            // The upper rows are the jejunum (deeper red, thicker walled), the lower the ileum.
            var pts = new List<Vector3>();
            const int rows = 9;
            for (int r = 0; r < rows; r++)
            {
                float y = Mathf.Lerp(1.030f, 0.918f, r / (float)(rows - 1));
                bool leftToRight = r % 2 == 0;
                for (int k = 0; k < 7; k++)
                {
                    float u = k / 6f;
                    float x = Mathf.Lerp(-0.082f, 0.082f, leftToRight ? u : 1f - u);
                    float z = -0.044f + 0.024f * Mathf.Sin(k * 1.9f + r * 1.3f);
                    pts.Add(new Vector3(x, y + 0.006f * Mathf.Sin(k * 2.3f + r), z));
                }
            }

            int split = pts.Count * 4 / 9;
            var jejunum = pts.GetRange(0, split + 1);
            var ileum = pts.GetRange(split, pts.Count - split);

            Place("Jejunum", "SYS_DIG_JEJUNUM", Gut("Jejunum", jejunum, Loft.Constant(0.0105f), 12, 1f, 260f),
                _tissue(new Color(0.86f, 0.56f, 0.48f), 0.42f));
            Place("Ileum", "SYS_DIG_ILEUM", Gut("Ileum", ileum, Loft.Constant(0.0092f), 12, 1f, 260f),
                _tissue(new Color(0.80f, 0.66f, 0.56f), 0.42f));
        }

        private static void BuildLargeIntestine()
        {
            var colon = _tissue(new Color(0.78f, 0.60f, 0.46f), 0.40f);
            const float r = 0.0170f;

            // Caecum, a blind pouch below the ileocaecal valve, and the worm-like appendix hanging from it.
            SdfFunc caecum = Sdf.SmoothUnion(0.008f,
                Sdf.Ellipsoid(V(-0.092f, 0.938f, -0.044f), V(0.026f, 0.026f, 0.024f)),
                Sdf.Ellipsoid(V(-0.092f, 0.962f, -0.040f), V(0.020f, 0.020f, 0.020f)));
            caecum = Sdf.Displace(caecum, 0.0016f, 130f, 9);
            Place("Caecum", "SYS_DIG_CECUM", Make("Caecum", caecum, V(-0.13f, 0.89f, -0.09f), V(-0.05f, 0.99f, 0.0f), 0.0012f), colon);

            Place("Appendix", "SYS_DIG_APPENDIX", Gut("Appendix", new[] { V(-0.086f, 0.918f, -0.048f), V(-0.082f, 0.896f, -0.040f), V(-0.088f, 0.878f, -0.024f), V(-0.098f, 0.870f, -0.014f) },
                Loft.Taper(0.0045f, 0.0028f), 8, 1f, 300f), _tissue(new Color(0.84f, 0.66f, 0.56f), 0.40f));

            Place("Colon_Ascending", "SYS_DIG_COLON_ASC", Gut("Colon_Ascending", new[] { V(-0.094f, 0.958f, -0.038f), V(-0.106f, 1.006f, -0.034f), V(-0.110f, 1.056f, -0.030f), V(-0.106f, 1.082f, -0.032f) },
                Haustra(r, 4), 14, 1f, 260f), colon);

            // Transverse colon: the longest and most mobile part, sagging below the stomach.
            Place("Colon_Transverse", "SYS_DIG_COLON_TRANS", Gut("Colon_Transverse", new[]
                {
                    V(-0.106f, 1.082f, -0.032f), V(-0.062f, 1.088f, -0.076f), V(-0.012f, 1.066f, -0.086f), V(0.040f, 1.066f, -0.082f),
                    V(0.084f, 1.090f, -0.058f), V(0.108f, 1.112f, -0.022f),
                }, Haustra(r, 9), 14, 1f, 220f), colon);

            Place("Colon_Descending", "SYS_DIG_COLON_DESC", Gut("Colon_Descending", new[] { V(0.108f, 1.112f, -0.022f), V(0.112f, 1.060f, -0.012f), V(0.110f, 1.000f, -0.010f), V(0.104f, 0.958f, 0.000f) },
                Haustra(r * 0.9f, 5), 14, 1f, 260f), colon);

            // Sigmoid colon: the S-shaped loop in the pelvis, then the rectum following the curve of the sacrum.
            Place("Colon_Sigmoid", "SYS_DIG_SIGMOID", Gut("Colon_Sigmoid", new[]
                {
                    V(0.104f, 0.958f, 0.000f), V(0.082f, 0.930f, -0.020f), V(0.046f, 0.910f, -0.032f), V(0.014f, 0.922f, -0.014f), V(0.020f, 0.908f, 0.018f), V(0.006f, 0.898f, 0.034f),
                }, Haustra(r * 0.85f, 6, 0.10f), 14, 1f, 260f), colon);

            Place("Rectum", "SYS_DIG_RECTUM", Gut("Rectum", new[] { V(0.006f, 0.898f, 0.034f), V(0f, 0.866f, 0.046f), V(0f, 0.830f, 0.048f), V(0f, 0.800f, 0.038f) },
                Loft.Taper(0.0175f, 0.0110f), 14, 1f, 260f), _tissue(new Color(0.80f, 0.58f, 0.46f), 0.40f));
        }

        private static void BuildUrinary()
        {
            var renal = _tissue(new Color(0.56f, 0.24f, 0.21f), 0.42f);

            // Kidneys: bean-shaped, the right a little lower (the liver sits above it), each with a hilum
            // facing inward and forward where the renal artery, vein and ureter attach.
            for (int s = -1; s <= 1; s += 2)
            {
                float y = s > 0 ? 1.108f : 1.090f;
                SdfFunc kidney = Sdf.Ellipsoid(V(s * 0.070f, y, 0.052f), V(0.027f, 0.056f, 0.023f), Quaternion.Euler(0f, 0f, s * 12f));
                kidney = Sdf.SmoothSubtract(0.008f, kidney, Sdf.Ellipsoid(V(s * 0.045f, y - 0.004f, 0.046f), V(0.014f, 0.022f, 0.016f)));
                string tag = s > 0 ? "L" : "R";
                Place("Kidney_" + tag, s > 0 ? "SYS_REN_KIDNEY_L" : "SYS_REN_KIDNEY_R",
                    Make("Kidney_" + tag, kidney, V(s > 0 ? 0.03f : -0.11f, y - 0.07f, 0.02f), V(s > 0 ? 0.11f : -0.03f, y + 0.07f, 0.08f), 0.0014f), renal);

                // Adrenal gland: a small cap sitting on top of each kidney.
                SdfFunc adrenal = Sdf.Ellipsoid(V(s * 0.062f, y + 0.060f, 0.054f), V(0.017f, 0.010f, 0.019f), Quaternion.Euler(0f, 0f, s * 25f));
                Place("Adrenal_" + tag, "SYS_ENDO_ADRENAL",
                    Make("Adrenal_" + tag, adrenal, V(s > 0 ? 0.03f : -0.10f, y + 0.03f, 0.02f), V(s > 0 ? 0.10f : -0.03f, y + 0.09f, 0.09f), 0.0008f),
                    _tissue(new Color(0.80f, 0.64f, 0.34f), 0.40f));
            }

            // Ureters: muscular tubes carrying urine down the psoas to the bladder, entering it behind.
            var ureter = new[] { V(0.048f, 1.086f, 0.050f), V(0.052f, 1.020f, 0.044f), V(0.060f, 0.962f, 0.034f), V(0.052f, 0.922f, 0.008f), V(0.030f, 0.894f, -0.030f) };
            Mesh u = Gut("Ureter", ureter, Loft.Constant(0.0028f), 8, 1f, 300f);
            PartFactory.AddPair(_root, "Ureter", "SYS_REN_URETER", u, _tissue(new Color(0.82f, 0.74f, 0.58f), 0.40f), _layer);

            // Bladder: a muscular bag behind the pubic symphysis, here at moderate fill.
            SdfFunc bladder = Sdf.SmoothUnion(0.008f,
                Sdf.Ellipsoid(V(0f, 0.880f, -0.040f), V(0.038f, 0.030f, 0.034f)),
                Sdf.Ellipsoid(V(0f, 0.902f, -0.046f), V(0.020f, 0.014f, 0.018f)));
            Place("Bladder", "SYS_REN_BLADDER", Make("Bladder", bladder, V(-0.06f, 0.83f, -0.09f), V(0.06f, 0.94f, 0.01f), 0.0014f),
                _tissue(new Color(0.82f, 0.76f, 0.62f), 0.45f));
        }
    }
}
