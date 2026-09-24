using System;
using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// Skeletal muscle as lofted meshes: each muscle runs from its real origin to its real
    /// insertion along a smooth path, swells into a belly, and narrows into pale tendons
    /// (a second submesh, so tendons get their own material). Broad muscles - the pectoral,
    /// the trapezius, the latissimus - are drawn as several fascicle strips fanning from
    /// origin to insertion, which is how they look on a dissection and why their fibres
    /// visibly converge. UV runs so that a striped fibre texture follows the length.
    ///
    /// Everything is modelled on the left and mirrored. Limb muscles are positioned relative
    /// to the skin's own limb geometry (<see cref="BodyShape"/>) so they sit a fixed depth
    /// under it; chest muscles are built from <see cref="SkeletonBuilder.RibPoint"/> so they
    /// lie on the ribs.
    /// </summary>
    public static class MuscleBuilder
    {
        private static Transform _root;
        private static Material _muscle, _tendon;
        private static int _layer;

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        private static Vector3[] P(params Vector3[] pts) => pts;

        private static readonly Func<Vector3, Vector3> Back = p => Vector3.forward;
        private static readonly Func<Vector3, Vector3> Front = p => Vector3.back;
        private static readonly Func<Vector3, Vector3> Outward = p => Vector3.right;

        private static Func<Vector3, Vector3> Around(BodyShape.Limb l) => p =>
        {
            Vector3 c = BodyShape.CentreAt(l, p.y);
            return new Vector3(p.x - c.x, 0f, p.z - c.z);
        };

        /// <summary>Chest wall: straight out from the ribcage's own axis.</summary>
        private static readonly Func<Vector3, Vector3> Chest = p => new Vector3(p.x, 0f, p.z - 0.005f);

        private static Vector3 A(BodyShape.Limb l, float y, float phi, float depth) => BodyShape.Under(l, y, phi, depth);
        private static Vector3 Rib(int rib, float thetaDeg, float standoff) => SkeletonBuilder.RibPoint(rib, thetaDeg * Mathf.Deg2Rad, standoff);

        // ---------------------------------------------------------------- plumbing

        private static void Add(string id, string name, float belly, float tendon, float flatten,
            Func<Vector3, Vector3> thick, float b0, float b1, params Vector3[][] paths) =>
            // Tendon is much slimmer than its belly: at the same width it read as a chunky
            // white sleeve on the end of every broad muscle rather than a cord or aponeurosis.
            AddShaped(id, name, Loft.BellyProfile(Mathf.Min(tendon, belly * 0.34f), belly, b0, b1, (b0 + b1) * 0.5f), flatten, thick, b0, b1, true, paths);

        private static void AddShaped(string id, string name, Func<float, float> profile, float flatten,
            Func<Vector3, Vector3> thick, float b0, float b1, bool paired, params Vector3[][] paths)
        {
            for (int k = 0; k < paths.Length; k++)
            {
                var o = Loft.Options.Default;
                o.Sides = 10;
                o.Flatten = flatten;
                o.ThickAt = thick;
                o.RingsPerMetre = 110f;
                o.VTiling = 6f;
                o.Submesh = t => (t < b0 || t > b1) ? 1 : 0;

                string mesh = paths.Length > 1 ? $"Muscle_{name}_{k + 1}" : $"Muscle_{name}";
                Mesh m = PartFactory.Save(Loft.Tube(mesh, paths[k], profile, o), mesh);
                var mats = new[] { _muscle, _tendon };
                if (paired) PartFactory.AddPair(_root, paths.Length > 1 ? $"{name}_{k + 1}" : name, id, m, mats, _layer);
                else PartFactory.Add(_root, paths.Length > 1 ? $"{name}_{k + 1}" : name, id, m, mats, _layer);
            }
        }

        /// <summary>A pure tendon or fascial band: pale, no belly.</summary>
        private static void AddTendon(string id, string name, float radius, float flatten,
            Func<Vector3, Vector3> thick, params Vector3[][] paths)
        {
            for (int k = 0; k < paths.Length; k++)
            {
                var o = Loft.Options.Default;
                o.Sides = 8; o.Flatten = flatten; o.ThickAt = thick; o.RingsPerMetre = 110f; o.VTiling = 6f;
                string mesh = paths.Length > 1 ? $"Tendon_{name}_{k + 1}" : $"Tendon_{name}";
                Mesh m = PartFactory.Save(Loft.Tube(mesh, paths[k], Loft.Constant(radius), o), mesh);
                PartFactory.AddPair(_root, paths.Length > 1 ? $"{name}_{k + 1}" : name, id, m, _tendon, _layer);
            }
        }

        private static List<Vector3> Loop(Vector3 centre, float rx, float ry, int n, float z)
        {
            var pts = new List<Vector3>();
            for (int i = 0; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                pts.Add(new Vector3(centre.x + Mathf.Cos(a) * rx, centre.y + Mathf.Sin(a) * ry, z));
            }
            return pts;
        }

        // ---------------------------------------------------------------- the muscles

        public static void Build(Transform root, Material muscle, Material tendon, int layer)
        {
            _root = root; _muscle = muscle; _tendon = tendon; _layer = layer;

            BuildShoulderAndArm();
            BuildForearmAndHand();
            BuildTorso();
            BuildNeckAndFace();
            BuildHipAndThigh();
            BuildLowerLeg();
        }

        private static void BuildShoulderAndArm()
        {
            var Arm = BodyShape.UpperArm;

            // Deltoid: three parts - clavicular (front), acromial (middle), spinal (back) -
            // converging on the deltoid tuberosity halfway down the humerus.
            Add("SYS_MUSC_DELTOID", "Deltoid", 0.0165f, 0.006f, 1.25f, Around(Arm), 0.05f, 0.88f,
                P(V(0.136f, 1.424f, -0.030f), V(0.182f, 1.412f, -0.046f), V(0.204f, 1.345f, -0.030f), V(0.202f, 1.300f, -0.008f)),
                P(V(0.172f, 1.436f, 0.014f), V(0.214f, 1.408f, 0.006f), V(0.213f, 1.350f, 0.010f), V(0.204f, 1.300f, 0.008f)),
                P(V(0.112f, 1.413f, 0.074f), V(0.176f, 1.410f, 0.062f), V(0.208f, 1.350f, 0.036f), V(0.202f, 1.305f, 0.016f)));

            // Pectoralis major: clavicular and sternal fibres fan out to the crest of the humerus.
            var pecIns = V(0.194f, 1.338f, -0.034f);
            var origins = new[]
            {
                V(0.046f, 1.421f, -0.042f), V(0.086f, 1.425f, -0.040f), V(0.021f, 1.384f, -0.078f),
                V(0.021f, 1.352f, -0.086f), V(0.021f, 1.318f, -0.093f), V(0.023f, 1.284f, -0.100f),
            };
            var pec = new List<Vector3[]>();
            foreach (var o in origins)
                pec.Add(P(o, Vector3.Lerp(o, pecIns, 0.5f) + V(0.004f, 0f, -0.010f), Vector3.Lerp(o, pecIns, 0.85f) + V(0f, 0f, -0.008f), pecIns));
            Add("SYS_MUSC_PECTORALIS", "Pectoralis", 0.0070f, 0.005f, 2.6f, Front, 0.04f, 0.80f, pec.ToArray());

            Add("SYS_MUSC_PECTORALIS_MINOR", "PectoralisMinor", 0.0055f, 0.003f, 2.0f, Front, 0.10f, 0.85f,
                P(Rib(2, 128f, 0.006f), V(0.116f, 1.376f, -0.062f), V(0.166f, 1.397f, -0.016f)));

            // Serratus anterior: the fingerlike digitations from the ribs round to the
            // medial border of the scapula.
            var serratus = new List<Vector3[]>();
            for (int i = 0; i < 4; i++)
            {
                int r = 2 + i;
                serratus.Add(P(Rib(r, 125f, 0.005f), Rib(r, 90f, 0.005f), Rib(r, 58f, 0.005f), V(0.068f, 1.36f - 0.03f * i, 0.084f)));
            }
            Add("SYS_MUSC_SERRATUS", "Serratus", 0.0035f, 0.003f, 4.0f, Chest, 0.05f, 0.9f, serratus.ToArray());

            // Trapezius: the diamond over the back of the neck and shoulders - upper fibres
            // from the skull, middle and lower from the spine, all to the scapula and clavicle.
            Add("SYS_MUSC_TRAPEZIUS", "Trapezius", 0.0045f, 0.003f, 5.0f, Back, 0.04f, 0.92f,
                P(V(0.006f, 1.598f, 0.092f), V(0.040f, 1.520f, 0.068f), V(0.095f, 1.455f, 0.054f), V(0.148f, 1.427f, 0.020f)),
                P(V(0.004f, 1.410f, 0.121f), V(0.060f, 1.408f, 0.108f), V(0.120f, 1.412f, 0.082f), V(0.172f, 1.424f, 0.048f)),
                P(V(0.004f, 1.320f, 0.128f), V(0.050f, 1.360f, 0.114f), V(0.100f, 1.400f, 0.094f), V(0.150f, 1.418f, 0.062f)),
                P(V(0.004f, 1.230f, 0.131f), V(0.030f, 1.300f, 0.121f), V(0.055f, 1.360f, 0.110f), V(0.064f, 1.398f, 0.096f)));

            Add("SYS_MUSC_LATISSIMUS", "Latissimus", 0.0055f, 0.004f, 4.2f, Back, 0.04f, 0.86f,
                P(V(0.010f, 1.180f, 0.130f), V(0.070f, 1.215f, 0.112f), V(0.134f, 1.262f, 0.056f), V(0.182f, 1.322f, 0.006f)),
                P(V(0.012f, 1.110f, 0.130f), V(0.078f, 1.170f, 0.112f), V(0.138f, 1.235f, 0.052f), V(0.183f, 1.318f, 0.004f)),
                P(V(0.030f, 1.030f, 0.124f), V(0.100f, 1.110f, 0.104f), V(0.146f, 1.200f, 0.040f), V(0.185f, 1.314f, 0.002f)));

            Add("SYS_MUSC_RHOMBOIDS", "Rhomboids", 0.0045f, 0.003f, 4.0f, Back, 0.06f, 0.94f,
                P(V(0.006f, 1.425f, 0.118f), V(0.036f, 1.410f, 0.104f), V(0.062f, 1.400f, 0.090f)),
                P(V(0.006f, 1.360f, 0.128f), V(0.038f, 1.350f, 0.108f), V(0.066f, 1.330f, 0.092f)));

            Add("SYS_MUSC_LEVATOR_SCAPULAE", "LevatorScapulae", 0.006f, 0.003f, 1.6f, Back, 0.10f, 0.85f,
                P(V(0.028f, 1.560f, 0.034f), V(0.044f, 1.500f, 0.056f), V(0.058f, 1.436f, 0.080f)));

            // Rotator cuff and teres major: the small muscles that hold the humeral head in
            // the shallow glenoid and rotate the arm.
            Add("SYS_MUSC_SUPRASPINATUS", "Supraspinatus", 0.0075f, 0.004f, 1.7f, Back, 0.05f, 0.80f,
                P(V(0.078f, 1.418f, 0.090f), V(0.130f, 1.421f, 0.060f), V(0.198f, 1.408f, 0.026f)));
            Add("SYS_MUSC_INFRASPINATUS", "Infraspinatus", 0.006f, 0.004f, 3.0f, Back, 0.05f, 0.82f,
                P(V(0.084f, 1.352f, 0.088f), V(0.132f, 1.374f, 0.070f), V(0.198f, 1.398f, 0.034f)));
            Add("SYS_MUSC_TERES_MINOR", "TeresMinor", 0.0055f, 0.003f, 1.8f, Back, 0.05f, 0.80f,
                P(V(0.132f, 1.322f, 0.060f), V(0.170f, 1.354f, 0.048f), V(0.199f, 1.388f, 0.030f)));
            Add("SYS_MUSC_TERES_MAJOR", "TeresMajor", 0.0075f, 0.004f, 1.5f, Back, 0.05f, 0.82f,
                P(V(0.078f, 1.296f, 0.094f), V(0.140f, 1.330f, 0.058f), V(0.184f, 1.345f, 0.014f)));
            Add("SYS_MUSC_SUBSCAPULARIS", "Subscapularis", 0.006f, 0.004f, 3.0f, Chest, 0.05f, 0.82f,
                P(V(0.092f, 1.352f, 0.064f), V(0.140f, 1.376f, 0.038f), V(0.182f, 1.392f, -0.004f)));

            // Biceps: long and short heads meeting in one belly, inserting on the radius.
            Add("SYS_MUSC_BICEPS", "Biceps", 0.0135f, 0.004f, 1.0f, Around(Arm), 0.10f, 0.78f,
                P(V(0.162f, 1.396f, 0.020f), V(0.184f, 1.345f, -0.010f), A(Arm, 1.24f, 8f, 0.018f), A(Arm, 1.14f, 6f, 0.016f), V(0.204f, 1.072f, 0.004f)),
                P(V(0.166f, 1.397f, -0.014f), V(0.188f, 1.335f, -0.020f), A(Arm, 1.24f, 350f, 0.018f), A(Arm, 1.14f, 6f, 0.016f), V(0.204f, 1.072f, 0.004f)));
            Add("SYS_MUSC_BRACHIALIS", "Brachialis", 0.012f, 0.004f, 1.6f, Around(Arm), 0.10f, 0.80f,
                P(V(0.192f, 1.300f, -0.006f), A(Arm, 1.20f, 20f, 0.030f), A(Arm, 1.12f, 20f, 0.020f), V(0.192f, 1.088f, -0.004f)));
            Add("SYS_MUSC_CORACOBRACHIALIS", "Coracobrachialis", 0.006f, 0.003f, 1.2f, Around(Arm), 0.10f, 0.85f,
                P(V(0.166f, 1.397f, -0.014f), V(0.183f, 1.360f, -0.006f), V(0.192f, 1.290f, 0.000f)));
            Add("SYS_MUSC_TRICEPS", "Triceps", 0.0165f, 0.005f, 1.15f, Around(Arm), 0.10f, 0.78f,
                P(V(0.152f, 1.360f, 0.038f), A(Arm, 1.30f, 190f, 0.020f), A(Arm, 1.20f, 180f, 0.020f), A(Arm, 1.12f, 180f, 0.016f), V(0.187f, 1.090f, 0.030f)),
                P(V(0.200f, 1.340f, 0.026f), A(Arm, 1.26f, 140f, 0.020f), A(Arm, 1.16f, 160f, 0.018f), V(0.188f, 1.096f, 0.030f)));
        }

        private static void BuildForearmAndHand()
        {
            var F = BodyShape.Forearm;
            var R = Around(F);

            Add("SYS_MUSC_BRACHIORADIALIS", "Brachioradialis", 0.0085f, 0.004f, 1.2f, R, 0.05f, 0.42f,
                P(V(0.206f, 1.140f, 0.006f), A(F, 1.05f, 45f, 0.014f), A(F, 0.95f, 60f, 0.010f), V(0.230f, 0.840f, 0.004f)));
            Add("SYS_MUSC_PRONATOR_TERES", "PronatorTeres", 0.0075f, 0.004f, 1.3f, R, 0.05f, 0.70f,
                P(V(0.172f, 1.094f, 0.004f), A(F, 1.03f, 10f, 0.012f), V(0.214f, 0.985f, -0.006f)));
            Add("SYS_MUSC_FLEXOR_CARPI_RADIALIS", "FlexorCarpiRadialis", 0.0070f, 0.003f, 1.2f, R, 0.05f, 0.46f,
                P(V(0.174f, 1.090f, 0.000f), A(F, 1.03f, 355f, 0.012f), A(F, 0.92f, 15f, 0.008f), V(0.216f, 0.845f, -0.004f)));
            Add("SYS_MUSC_PALMARIS_LONGUS", "PalmarisLongus", 0.0050f, 0.002f, 1.2f, R, 0.05f, 0.38f,
                P(V(0.172f, 1.088f, 0.006f), A(F, 1.03f, 340f, 0.012f), A(F, 0.92f, 350f, 0.008f), V(0.208f, 0.842f, -0.010f)));
            Add("SYS_MUSC_FLEXOR_DIGITORUM_SUPERFICIALIS", "FlexorDigitorumSuperficialis", 0.0095f, 0.004f, 1.2f, R, 0.05f, 0.52f,
                P(V(0.176f, 1.086f, 0.008f), A(F, 1.02f, 0f, 0.020f), A(F, 0.92f, 355f, 0.018f), V(0.210f, 0.842f, -0.002f)));
            Add("SYS_MUSC_FLEXOR_CARPI_ULNARIS", "FlexorCarpiUlnaris", 0.0080f, 0.003f, 1.2f, R, 0.05f, 0.55f,
                P(V(0.170f, 1.088f, 0.018f), A(F, 1.02f, 300f, 0.012f), A(F, 0.92f, 290f, 0.008f), V(0.196f, 0.850f, 0.004f)));
            Add("SYS_MUSC_EXTENSOR_CARPI_RADIALIS", "ExtensorCarpiRadialis", 0.0080f, 0.003f, 1.2f, R, 0.05f, 0.50f,
                P(V(0.208f, 1.116f, 0.008f), A(F, 1.04f, 80f, 0.012f), A(F, 0.94f, 85f, 0.008f), V(0.226f, 0.848f, 0.010f)));
            Add("SYS_MUSC_EXTENSOR_DIGITORUM", "ExtensorDigitorum", 0.0090f, 0.003f, 1.2f, R, 0.05f, 0.55f,
                P(V(0.202f, 1.092f, 0.020f), A(F, 1.03f, 140f, 0.012f), A(F, 0.93f, 160f, 0.008f), V(0.212f, 0.842f, 0.018f)));
            Add("SYS_MUSC_EXTENSOR_CARPI_ULNARIS", "ExtensorCarpiUlnaris", 0.0070f, 0.003f, 1.2f, R, 0.05f, 0.52f,
                P(V(0.198f, 1.086f, 0.024f), A(F, 1.02f, 190f, 0.012f), A(F, 0.92f, 215f, 0.008f), V(0.196f, 0.845f, 0.020f)));

            // The pads of the palm at the base of the thumb and the little finger.
            Add("SYS_MUSC_THENAR", "Thenar", 0.0105f, 0.008f, 1.3f, Front, 0.05f, 0.95f,
                P(V(0.228f, 0.815f, -0.010f), V(0.238f, 0.788f, -0.014f)));
            Add("SYS_MUSC_HYPOTHENAR", "Hypothenar", 0.0075f, 0.006f, 1.3f, Front, 0.05f, 0.95f,
                P(V(0.196f, 0.812f, -0.006f), V(0.190f, 0.780f, -0.006f)));
        }

        private static void BuildTorso()
        {
            // Rectus abdominis: the paired straps either side of the midline, divided by
            // tendinous intersections into the segments of a "six-pack".
            Func<float, float> segmented = t =>
            {
                float belly = Loft.BellyProfile(0.004f, 0.0095f, 0.03f, 0.92f)(t);
                float dips = Mathf.Exp(-Mathf.Pow((t - 0.30f) / 0.018f, 2f)) + Mathf.Exp(-Mathf.Pow((t - 0.52f) / 0.018f, 2f))
                           + Mathf.Exp(-Mathf.Pow((t - 0.74f) / 0.018f, 2f));
                return belly * (1f - 0.32f * dips);
            };
            AddShaped("SYS_MUSC_RECTUS_ABDOMINIS", "RectusAbdominis", segmented, 3.4f, Front, 0.02f, 0.98f, true,
                P(V(0.020f, 0.848f, -0.066f), V(0.022f, 0.930f, -0.108f), V(0.024f, 1.060f, -0.113f), V(0.026f, 1.200f, -0.112f), V(0.028f, 1.284f, -0.104f)));

            // External oblique: the outermost flank muscle, fibres running down and forward
            // "as if into the pockets", from the lower ribs to the iliac crest and the
            // aponeurosis beside the rectus.
            var oblique = new List<Vector3[]>();
            for (int i = 0; i < 5; i++)
            {
                Vector3 o = Rib(4 + i, 104f + i * 3f, 0.008f);
                oblique.Add(P(o, V(0.106f, o.y - 0.045f, -0.062f + i * 0.004f), V(0.048f, o.y - 0.090f - 0.012f * i, -0.112f)));
            }
            Add("SYS_MUSC_OBLIQUE", "ExternalOblique", 0.0055f, 0.004f, 3.2f, Chest, 0.05f, 0.75f, oblique.ToArray());

            Add("SYS_MUSC_INTERNAL_OBLIQUE", "InternalOblique", 0.0045f, 0.003f, 3.2f, Chest, 0.05f, 0.75f,
                P(V(0.126f, 0.990f, -0.028f), V(0.092f, 1.040f, -0.096f), V(0.050f, 1.092f, -0.110f)),
                P(V(0.134f, 0.986f, -0.052f), V(0.100f, 1.080f, -0.090f), V(0.052f, 1.150f, -0.108f)),
                P(V(0.120f, 0.988f, -0.010f), Rib(9, 112f, 0.004f), V(0.056f, 1.200f, -0.104f)));

            var transversus = new List<Vector3[]>();
            foreach (float y in new[] { 1.00f, 1.07f, 1.14f })
                transversus.Add(P(V(0.046f, y, -0.107f), V(0.090f, y, -0.096f), V(0.122f, y, -0.056f)));
            Add("SYS_MUSC_TRANSVERSUS_ABDOMINIS", "TransversusAbdominis", 0.0035f, 0.003f, 4.0f, Chest, 0.05f, 0.95f, transversus.ToArray());

            Add("SYS_MUSC_ERECTOR_SPINAE", "ErectorSpinae", 0.011f, 0.005f, 2.6f, Back, 0.08f, 0.88f,
                P(V(0.032f, 0.985f, 0.110f), V(0.034f, 1.100f, 0.120f), V(0.036f, 1.230f, 0.124f), V(0.034f, 1.360f, 0.114f), V(0.030f, 1.470f, 0.072f), V(0.024f, 1.560f, 0.050f)));

            Add("SYS_MUSC_QUADRATUS_LUMBORUM", "QuadratusLumborum", 0.008f, 0.004f, 2.5f, Back, 0.05f, 0.90f,
                P(V(0.070f, 1.150f, 0.084f), V(0.084f, 1.075f, 0.076f), V(0.098f, 0.992f, 0.060f)));

            Add("SYS_MUSC_PSOAS", "PsoasMajor", 0.014f, 0.005f, 1.1f, Outward, 0.12f, 0.80f,
                P(V(0.036f, 1.140f, 0.050f), V(0.040f, 1.040f, 0.040f), V(0.062f, 0.960f, 0.026f), V(0.078f, 0.860f, 0.004f), V(0.088f, 0.806f, 0.002f)));
            Add("SYS_MUSC_ILIACUS", "Iliacus", 0.011f, 0.004f, 2.4f, p => V(-0.5f, 0f, -0.85f), 0.10f, 0.82f,
                P(V(0.104f, 0.982f, -0.004f), V(0.100f, 0.930f, 0.000f), V(0.086f, 0.850f, 0.000f), V(0.086f, 0.806f, 0.002f)));
        }

        private static void BuildNeckAndFace()
        {
            // Sternocleidomastoid: the strap from the sternum and collarbone up to the mastoid.
            Add("SYS_MUSC_STERNOCLEIDOMASTOID", "Sternocleidomastoid", 0.0085f, 0.004f, 1.5f, Outward, 0.06f, 0.82f,
                P(V(0.014f, 1.426f, -0.054f), V(0.030f, 1.490f, -0.012f), V(0.046f, 1.560f, 0.004f), V(0.056f, 1.598f, 0.020f)),
                P(V(0.040f, 1.420f, -0.036f), V(0.044f, 1.492f, -0.006f), V(0.056f, 1.598f, 0.020f)));
            Add("SYS_MUSC_SCALENES", "Scalenes", 0.006f, 0.003f, 1.4f, Outward, 0.10f, 0.85f,
                P(V(0.020f, 1.505f, 0.016f), V(0.034f, 1.470f, -0.002f), V(0.046f, 1.440f, -0.030f)));

            // Digastric: two bellies joined by a tendon slung to the hyoid.
            Add("SYS_MUSC_DIGASTRIC", "Digastric", 0.0055f, 0.003f, 1.3f, Outward, 0.10f, 0.85f,
                P(V(0.055f, 1.597f, 0.020f), V(0.038f, 1.540f, -0.006f), V(0.024f, 1.506f, -0.034f)),
                P(V(0.020f, 1.506f, -0.040f), V(0.014f, 1.520f, -0.062f), V(0.010f, 1.535f, -0.080f)));

            // Face. Mastication first: temporalis fans over the temple, masseter hangs from the cheekbone.
            Add("SYS_MUSC_TEMPORALIS", "Temporalis", 0.0042f, 0.003f, 3.2f, Outward, 0.05f, 0.85f,
                P(V(0.060f, 1.700f, -0.030f), V(0.064f, 1.668f, -0.028f), V(0.056f, 1.640f, -0.030f)),
                P(V(0.068f, 1.690f, 0.000f), V(0.070f, 1.655f, -0.006f), V(0.060f, 1.632f, -0.026f)),
                P(V(0.062f, 1.695f, 0.030f), V(0.070f, 1.660f, 0.006f), V(0.060f, 1.632f, -0.024f)));
            Add("SYS_MUSC_MASSETER", "Masseter", 0.0065f, 0.003f, 2.5f, Outward, 0.08f, 0.88f,
                P(V(0.066f, 1.626f, -0.030f), V(0.067f, 1.596f, -0.012f), V(0.064f, 1.568f, 0.002f)));

            // Expression: broad sheets over the brow, rings round the eye and mouth, and slim
            // straps radiating to the lips.
            Add("SYS_MUSC_FRONTALIS", "Frontalis", 0.0035f, 0.002f, 4.0f, p => V(0f, 0.3f, -1f), 0.05f, 0.95f,
                P(V(0.028f, 1.732f, -0.026f), V(0.030f, 1.712f, -0.066f), V(0.030f, 1.688f, -0.090f)));
            AddShaped("SYS_MUSC_ORBICULARIS_OCULI", "OrbicularisOculi", Loft.Constant(0.0024f), 2.4f, Front, 0f, 1f, true,
                Loop(V(0.031f, 1.658f, 0f), 0.022f, 0.017f, 16, -0.087f).ToArray());
            AddShaped("SYS_MUSC_ORBICULARIS_ORIS", "OrbicularisOris", Loft.Constant(0.0030f), 2.2f, Front, 0f, 1f, false,
                Loop(V(0f, 1.556f, 0f), 0.021f, 0.008f, 16, -0.097f).ToArray());
            Add("SYS_MUSC_ZYGOMATICUS", "Zygomaticus", 0.003f, 0.002f, 1.5f, Front, 0.10f, 0.85f,
                P(V(0.046f, 1.632f, -0.074f), V(0.038f, 1.598f, -0.090f), V(0.026f, 1.562f, -0.094f)));
            Add("SYS_MUSC_BUCCINATOR", "Buccinator", 0.006f, 0.003f, 2.5f, p => V(0.3f, 0f, -1f), 0.08f, 0.9f,
                P(V(0.058f, 1.580f, -0.012f), V(0.050f, 1.570f, -0.060f), V(0.030f, 1.560f, -0.094f)));
            Add("SYS_MUSC_CORRUGATOR_SUPERCILII", "CorrugatorSupercilii", 0.002f, 0.0015f, 1.5f, Front, 0.1f, 0.9f,
                P(V(0.010f, 1.676f, -0.092f), V(0.030f, 1.688f, -0.088f)));
            AddShaped("SYS_MUSC_PROCERUS", "Procerus", Loft.Constant(0.003f), 1.4f, Front, 0f, 1f, false,
                P(V(0f, 1.681f, -0.090f), V(0f, 1.660f, -0.099f)));
            Add("SYS_MUSC_NASALIS", "Nasalis", 0.0025f, 0.0015f, 1.5f, Front, 0.1f, 0.9f,
                P(V(0.012f, 1.640f, -0.098f), V(0.016f, 1.618f, -0.106f)));
            Add("SYS_MUSC_LEVATOR_LABII_SUPERIORIS", "LevatorLabiiSuperioris", 0.003f, 0.002f, 1.6f, Front, 0.1f, 0.9f,
                P(V(0.026f, 1.642f, -0.092f), V(0.016f, 1.576f, -0.100f)));
            Add("SYS_MUSC_DEPRESSOR_ANGULI_ORIS", "DepressorAnguliOris", 0.003f, 0.002f, 1.8f, Front, 0.1f, 0.9f,
                P(V(0.040f, 1.545f, -0.080f), V(0.026f, 1.560f, -0.098f)));
            AddShaped("SYS_MUSC_MENTALIS", "Mentalis", Loft.Constant(0.0035f), 1.4f, Front, 0f, 1f, true,
                P(V(0.008f, 1.545f, -0.082f), V(0.004f, 1.534f, -0.088f)));
        }

        private static void BuildHipAndThigh()
        {
            var T = BodyShape.Thigh;
            var R = Around(T);

            // Gluteus maximus: the largest muscle in the body - five fascicles fanning from the
            // sacrum and back of the ilium to the femur and the iliotibial tract.
            var gmax = new List<Vector3[]>();
            foreach (var o in new[] { V(0.014f, 0.965f, 0.092f), V(0.034f, 0.940f, 0.094f), V(0.052f, 0.985f, 0.078f), V(0.024f, 0.900f, 0.090f) })
                gmax.Add(P(o, V(0.070f, o.y - 0.020f, 0.116f), V(0.100f, o.y - 0.060f, 0.100f), V(0.108f, 0.790f, 0.026f)));
            Add("SYS_MUSC_GLUTEUS_MAXIMUS", "GluteusMaximus", 0.0135f, 0.006f, 2.4f, Back, 0.05f, 0.85f, gmax.ToArray());

            Add("SYS_MUSC_GLUTEUS_MEDIUS", "GluteusMedius", 0.011f, 0.005f, 2.4f, Outward, 0.05f, 0.85f,
                P(V(0.100f, 0.995f, 0.040f), V(0.120f, 0.940f, 0.030f), V(0.118f, 0.856f, 0.012f)),
                P(V(0.120f, 0.990f, 0.000f), V(0.130f, 0.930f, 0.012f), V(0.118f, 0.856f, 0.012f)),
                P(V(0.126f, 0.968f, -0.030f), V(0.130f, 0.920f, -0.004f), V(0.118f, 0.856f, 0.012f)));
            Add("SYS_MUSC_PIRIFORMIS", "Piriformis", 0.007f, 0.004f, 1.3f, Back, 0.10f, 0.85f,
                P(V(0.022f, 0.935f, 0.072f), V(0.062f, 0.885f, 0.052f), V(0.106f, 0.852f, 0.014f)));
            Add("SYS_MUSC_TFL", "TensorFasciaeLatae", 0.010f, 0.004f, 1.6f, Outward, 0.10f, 0.82f,
                P(V(0.138f, 0.972f, -0.048f), V(0.130f, 0.930f, -0.040f), V(0.126f, 0.880f, -0.020f)));
            AddTendon("SYS_MUSC_ILIOTIBIAL_TRACT", "IliotibialTract", 0.0035f, 7.0f, R,
                P(V(0.126f, 0.880f, -0.020f), A(T, 0.70f, 82f, 0.006f), A(T, 0.55f, 88f, 0.005f), V(0.116f, 0.446f, 0.000f)));

            // Quadriceps: rectus femoris (the only head that crosses the hip), the two
            // vasti on each side, and the deep vastus intermedius, all converging on the patella.
            Add("SYS_MUSC_RECTUS_FEMORIS", "RectusFemoris", 0.016f, 0.005f, 1.1f, R, 0.12f, 0.78f,
                P(V(0.122f, 0.906f, -0.050f), A(T, 0.78f, 0f, 0.021f), A(T, 0.64f, 0f, 0.021f), A(T, 0.54f, 0f, 0.020f), V(0.088f, 0.494f, -0.042f)));
            Add("SYS_MUSC_VASTUS_LATERALIS", "VastusLateralis", 0.019f, 0.005f, 1.3f, R, 0.10f, 0.80f,
                P(V(0.112f, 0.820f, 0.000f), A(T, 0.72f, 62f, 0.025f), A(T, 0.60f, 62f, 0.025f), A(T, 0.52f, 55f, 0.022f), V(0.100f, 0.490f, -0.030f)));
            Add("SYS_MUSC_VASTUS_MEDIALIS", "VastusMedialis", 0.018f, 0.005f, 1.3f, R, 0.10f, 0.82f,
                P(V(0.080f, 0.800f, -0.004f), A(T, 0.68f, 312f, 0.024f), A(T, 0.58f, 312f, 0.022f), A(T, 0.51f, 320f, 0.018f), V(0.074f, 0.486f, -0.038f)));
            Add("SYS_MUSC_VASTUS_INTERMEDIUS", "VastusIntermedius", 0.013f, 0.005f, 1.2f, R, 0.10f, 0.82f,
                P(V(0.100f, 0.800f, -0.006f), V(0.093f, 0.650f, -0.028f), V(0.090f, 0.540f, -0.034f), V(0.088f, 0.496f, -0.036f)));
            AddTendon("SYS_MUSC_PATELLAR_TENDON", "PatellarTendon", 0.0055f, 3.0f, Front,
                P(V(0.088f, 0.494f, -0.046f), V(0.087f, 0.470f, -0.044f), V(0.085f, 0.436f, -0.014f)));

            Add("SYS_MUSC_SARTORIUS", "Sartorius", 0.0065f, 0.003f, 2.4f, R, 0.05f, 0.90f,
                P(V(0.140f, 0.950f, -0.066f), A(T, 0.82f, 10f, 0.010f), A(T, 0.68f, 340f, 0.010f), A(T, 0.55f, 310f, 0.010f), V(0.076f, 0.440f, 0.004f)));

            // Adductors and gracilis: the inner thigh, arising from the pubis and ischium.
            Add("SYS_MUSC_GRACILIS", "Gracilis", 0.005f, 0.003f, 3.0f, R, 0.05f, 0.85f,
                P(V(0.020f, 0.830f, -0.052f), A(T, 0.65f, 270f, 0.008f), A(T, 0.52f, 262f, 0.010f), V(0.078f, 0.440f, 0.004f)));
            Add("SYS_MUSC_ADDUCTOR_LONGUS", "AdductorLongus", 0.012f, 0.004f, 2.0f, R, 0.08f, 0.85f,
                P(V(0.024f, 0.836f, -0.056f), A(T, 0.72f, 300f, 0.018f), V(0.086f, 0.640f, 0.006f)));
            Add("SYS_MUSC_ADDUCTOR_MAGNUS", "AdductorMagnus", 0.017f, 0.005f, 2.2f, R, 0.08f, 0.85f,
                P(V(0.044f, 0.808f, 0.018f), A(T, 0.72f, 260f, 0.022f), A(T, 0.60f, 255f, 0.022f), V(0.070f, 0.500f, 0.016f)));

            // Hamstrings: all three arise from the ischial tuberosity and cross both hip and knee.
            Add("SYS_MUSC_BICEPS_FEMORIS", "BicepsFemoris", 0.016f, 0.005f, 1.2f, R, 0.10f, 0.80f,
                P(V(0.079f, 0.800f, 0.044f), A(T, 0.70f, 175f, 0.021f), A(T, 0.58f, 150f, 0.021f), V(0.114f, 0.448f, 0.030f)));
            Add("SYS_MUSC_SEMITENDINOSUS", "Semitendinosus", 0.013f, 0.004f, 1.2f, R, 0.10f, 0.62f,
                P(V(0.078f, 0.798f, 0.042f), A(T, 0.70f, 200f, 0.020f), A(T, 0.56f, 230f, 0.018f), V(0.080f, 0.442f, 0.016f)));
            Add("SYS_MUSC_SEMIMEMBRANOSUS", "Semimembranosus", 0.015f, 0.005f, 1.3f, R, 0.10f, 0.72f,
                P(V(0.078f, 0.796f, 0.040f), A(T, 0.68f, 205f, 0.030f), A(T, 0.55f, 235f, 0.026f), V(0.076f, 0.458f, 0.018f)));
        }

        private static void BuildLowerLeg()
        {
            var S = BodyShape.Shank;
            var R = Around(S);

            // Calf: the two heads of gastrocnemius over the deeper soleus, all narrowing
            // into the Achilles tendon - the strongest tendon in the body - on the heel bone.
            Add("SYS_MUSC_GASTROCNEMIUS", "Gastrocnemius", 0.018f, 0.006f, 1.25f, Back, 0.06f, 0.84f,
                P(V(0.070f, 0.482f, 0.034f), A(S, 0.40f, 200f, 0.022f), A(S, 0.30f, 195f, 0.024f), A(S, 0.20f, 185f, 0.014f), V(0.086f, 0.150f, 0.044f)),
                P(V(0.106f, 0.482f, 0.034f), A(S, 0.40f, 165f, 0.022f), A(S, 0.30f, 170f, 0.024f), A(S, 0.20f, 178f, 0.014f), V(0.086f, 0.150f, 0.044f)));
            Add("SYS_MUSC_SOLEUS", "Soleus", 0.017f, 0.006f, 1.7f, Back, 0.08f, 0.86f,
                P(V(0.098f, 0.440f, 0.030f), A(S, 0.34f, 180f, 0.030f), A(S, 0.24f, 180f, 0.020f), V(0.086f, 0.150f, 0.042f)));
            AddTendon("SYS_MUSC_ACHILLES", "AchillesTendon", 0.0075f, 1.8f, Back,
                P(V(0.086f, 0.180f, 0.040f), V(0.087f, 0.110f, 0.048f), V(0.087f, 0.048f, 0.062f)));

            Add("SYS_MUSC_TIBIALIS", "TibialisAnterior", 0.011f, 0.004f, 1.4f, R, 0.05f, 0.52f,
                P(V(0.100f, 0.442f, 0.002f), A(S, 0.34f, 22f, 0.015f), A(S, 0.22f, 15f, 0.012f), V(0.084f, 0.100f, -0.024f), V(0.070f, 0.042f, -0.054f)));
            Add("SYS_MUSC_EXTENSOR_DIGITORUM_LONGUS", "ExtensorDigitorumLongus", 0.008f, 0.003f, 1.3f, R, 0.05f, 0.50f,
                P(V(0.108f, 0.436f, 0.006f), A(S, 0.32f, 40f, 0.018f), A(S, 0.20f, 30f, 0.010f), V(0.098f, 0.090f, -0.022f), V(0.090f, 0.038f, -0.080f)));
            Add("SYS_MUSC_PERONEUS_LONGUS", "PeroneusLongus", 0.0075f, 0.003f, 1.3f, R, 0.05f, 0.50f,
                P(V(0.114f, 0.440f, 0.030f), A(S, 0.32f, 100f, 0.014f), A(S, 0.18f, 110f, 0.008f), V(0.104f, 0.072f, 0.026f), V(0.090f, 0.030f, -0.030f)));
            Add("SYS_MUSC_TIBIALIS_POSTERIOR", "TibialisPosterior", 0.009f, 0.003f, 1.3f, R, 0.05f, 0.55f,
                P(V(0.096f, 0.436f, 0.026f), A(S, 0.32f, 200f, 0.034f), A(S, 0.20f, 250f, 0.012f), V(0.068f, 0.070f, 0.018f), V(0.070f, 0.034f, -0.030f)));
            Add("SYS_MUSC_FLEXOR_DIGITORUM_LONGUS", "FlexorDigitorumLongus", 0.008f, 0.003f, 1.3f, R, 0.05f, 0.52f,
                P(V(0.090f, 0.430f, 0.030f), A(S, 0.30f, 225f, 0.030f), A(S, 0.18f, 262f, 0.010f), V(0.066f, 0.064f, 0.020f), V(0.078f, 0.030f, -0.060f)));
        }
    }
}
