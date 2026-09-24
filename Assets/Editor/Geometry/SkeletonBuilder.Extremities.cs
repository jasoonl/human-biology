using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// Hands and feet: 27 bones per hand (8 carpals, 5 metacarpals, 14 phalanges) and 26
    /// per foot (7 tarsals, 5 metatarsals, 14 phalanges), each its own selectable mesh.
    /// Lengths follow real proportions - about 19 cm from wrist crease to fingertip and 26 cm
    /// from heel to hallux - which the earlier primitive hands and feet were well short of.
    /// Modelled on the left; the right is the mirror.
    /// </summary>
    public static partial class SkeletonBuilder
    {
        private static Mesh Rod(string name, IList<Vector3> path, float endRadius, float shaftRadius, int sides = 8)
        {
            var options = Loft.Options.Default;
            options.Sides = sides;
            options.RingsPerMetre = 260f;
            options.CapRings = 3;
            return PartFactory.Save(Loft.Tube(name, path, Loft.BoneProfile(endRadius, shaftRadius, 0.28f), options), name);
        }

        private static Mesh Blob(string name, Vector3 centre, Vector3 radii, Quaternion? rotation = null)
        {
            SdfFunc shape = Sdf.Ellipsoid(centre, radii, rotation ?? Quaternion.identity);
            float pad = Mathf.Max(radii.x, Mathf.Max(radii.y, radii.z)) + 0.004f;
            PartFactory.BoundsOf(out var min, out var max, pad, centre);
            return PartFactory.Save(SurfaceNets.Build(shape, min, max, 0.0007f, name), name);
        }

        // ---------------------------------------------------------------- hand

        private static void BuildHand(Transform root, Material bone, int layer)
        {
            // Carpals, two rows of four. Proximal row, thumb side to little-finger side:
            // scaphoid, lunate, triquetrum, pisiform. Distal row: trapezium, trapezoid,
            // capitate, hamate.
            var carpals = new (string name, Vector3 c, Vector3 r)[]
            {
                ("Scaphoid", new Vector3(0.228f, 0.826f, 0.011f), new Vector3(0.0085f, 0.0110f, 0.0065f)),
                ("Lunate", new Vector3(0.214f, 0.828f, 0.011f), new Vector3(0.0075f, 0.0080f, 0.0070f)),
                ("Triquetrum", new Vector3(0.202f, 0.826f, 0.012f), new Vector3(0.0070f, 0.0075f, 0.0065f)),
                ("Pisiform", new Vector3(0.196f, 0.821f, 0.003f), new Vector3(0.0050f, 0.0055f, 0.0050f)),
                ("Trapezium", new Vector3(0.231f, 0.813f, 0.003f), new Vector3(0.0085f, 0.0075f, 0.0075f)),
                ("Trapezoid", new Vector3(0.221f, 0.811f, 0.009f), new Vector3(0.0060f, 0.0065f, 0.0065f)),
                ("Capitate", new Vector3(0.211f, 0.811f, 0.011f), new Vector3(0.0075f, 0.0090f, 0.0075f)),
                ("Hamate", new Vector3(0.199f, 0.811f, 0.009f), new Vector3(0.0075f, 0.0080f, 0.0070f)),
            };
            foreach (var (name, c, r) in carpals)
                PartFactory.AddPair(root, "Carpal_" + name, "SYS_SK_CARPALS", Blob("Carpal_" + name, c, r), bone, layer);

            // Index, middle, ring and little fingers: where the metacarpal starts and its
            // knuckle, the metacarpal's length, then proximal / middle / distal phalanx.
            var fingers = new (string name, float baseX, float headX, float mc, float[] ph)[]
            {
                ("Index", 0.224f, 0.233f, 0.066f, new[] { 0.040f, 0.023f, 0.019f }),
                ("Middle", 0.213f, 0.216f, 0.064f, new[] { 0.044f, 0.027f, 0.020f }),
                ("Ring", 0.203f, 0.199f, 0.057f, new[] { 0.041f, 0.026f, 0.019f }),
                ("Little", 0.194f, 0.183f, 0.051f, new[] { 0.033f, 0.018f, 0.017f }),
            };

            foreach (var f in fingers)
            {
                var baseP = new Vector3(f.baseX, 0.806f, 0.010f);
                var head = new Vector3(f.headX, 0.806f - f.mc, 0.008f);
                PartFactory.AddPair(root, $"Metacarpal_{f.name}", "SYS_SK_METACARPALS",
                    Rod($"Metacarpal_{f.name}", new[] { baseP, Vector3.Lerp(baseP, head, 0.5f), head }, 0.0072f, 0.0050f), bone, layer);

                // Fingers rest in a gentle curl toward the palm: each joint bends a little more.
                Vector3 axis = (head - baseP).normalized;
                Vector3 cursor = head;
                float[] curl = { 6f, 20f, 34f };
                float[] radius = { 0.0062f, 0.0050f, 0.0042f };
                string[] label = { "Proximal", "Middle", "Distal" };
                for (int p = 0; p < 3; p++)
                {
                    Vector3 dir = Quaternion.AngleAxis(curl[p], Vector3.right) * axis;
                    Vector3 next = cursor + dir * f.ph[p];
                    PartFactory.AddPair(root, $"Phalanx_{f.name}_{label[p]}", "SYS_SK_PHALANGES_HAND",
                        Rod($"Phalanx_{f.name}_{label[p]}", new[] { cursor + dir * 0.002f, next }, radius[p] * 1.25f, radius[p]), bone, layer);
                    cursor = next;
                }
            }

            // Thumb: set forward and out from the wrist, with only two phalanges.
            var thumbBase = new Vector3(0.230f, 0.812f, 0.002f);
            var thumbHead = new Vector3(0.245f, 0.770f, -0.014f);
            PartFactory.AddPair(root, "Metacarpal_Thumb", "SYS_SK_METACARPALS",
                Rod("Metacarpal_Thumb", new[] { thumbBase, Vector3.Lerp(thumbBase, thumbHead, 0.5f), thumbHead }, 0.0085f, 0.0060f), bone, layer);
            Vector3 thumbAxis = (thumbHead - thumbBase).normalized;
            Vector3 p1 = thumbHead + Quaternion.AngleAxis(12f, Vector3.right) * thumbAxis * 0.032f;
            Vector3 p2 = p1 + Quaternion.AngleAxis(30f, Vector3.right) * thumbAxis * 0.026f;
            PartFactory.AddPair(root, "Phalanx_Thumb_Proximal", "SYS_SK_PHALANGES_HAND",
                Rod("Phalanx_Thumb_Proximal", new[] { thumbHead, p1 }, 0.0085f, 0.0068f), bone, layer);
            PartFactory.AddPair(root, "Phalanx_Thumb_Distal", "SYS_SK_PHALANGES_HAND",
                Rod("Phalanx_Thumb_Distal", new[] { p1, p2 }, 0.0072f, 0.0055f), bone, layer);
        }

        // ---------------------------------------------------------------- foot

        private static void BuildFoot(Transform root, Material bone, int layer)
        {
            // Ankle and hindfoot. The talus takes the weight from the tibia and passes it
            // to the calcaneus below and behind, which projects backward as the heel.
            Mesh talus = PartFactory.Save(SurfaceNets.Build(Sdf.SmoothUnion(0.006f,
                    Sdf.Ellipsoid(new Vector3(0.083f, 0.056f, 0.008f), new Vector3(0.0200f, 0.0140f, 0.0240f)),
                    Sdf.Sphere(new Vector3(0.079f, 0.048f, -0.020f), 0.0120f)),
                new Vector3(0.05f, 0.03f, -0.05f), new Vector3(0.12f, 0.085f, 0.04f), 0.0009f, "Talus"), "Talus");
            PartFactory.AddPair(root, "Talus", "SYS_SK_TALUS", talus, bone, layer);

            Mesh calcaneus = PartFactory.Save(SurfaceNets.Build(Sdf.SmoothUnion(0.006f,
                    Sdf.Ellipsoid(new Vector3(0.086f, 0.026f, 0.036f), new Vector3(0.0135f, 0.0200f, 0.0360f)),
                    Sdf.Ellipsoid(new Vector3(0.078f, 0.040f, 0.016f), new Vector3(0.0100f, 0.0070f, 0.0120f)),
                    Sdf.Sphere(new Vector3(0.088f, 0.028f, -0.008f), 0.0120f)),
                new Vector3(0.05f, 0.0f, -0.03f), new Vector3(0.12f, 0.06f, 0.09f), 0.0009f, "Calcaneus"), "Calcaneus");
            PartFactory.AddPair(root, "Calcaneus", "SYS_SK_CALCANEUS", calcaneus, bone, layer);

            var tarsals = new (string name, Vector3 c, Vector3 r)[]
            {
                ("Navicular", new Vector3(0.075f, 0.040f, -0.030f), new Vector3(0.0105f, 0.0140f, 0.0075f)),
                ("Cuboid", new Vector3(0.097f, 0.028f, -0.034f), new Vector3(0.0125f, 0.0125f, 0.0145f)),
                ("MedialCuneiform", new Vector3(0.070f, 0.032f, -0.054f), new Vector3(0.0090f, 0.0130f, 0.0100f)),
                ("IntermediateCuneiform", new Vector3(0.081f, 0.033f, -0.054f), new Vector3(0.0075f, 0.0110f, 0.0090f)),
                ("LateralCuneiform", new Vector3(0.091f, 0.030f, -0.052f), new Vector3(0.0085f, 0.0115f, 0.0095f)),
            };
            foreach (var (name, c, r) in tarsals)
                PartFactory.AddPair(root, "Tarsal_" + name, "SYS_SK_TARSALS", Blob("Tarsal_" + name, c, r), bone, layer);

            // Metatarsals: base at the tarsometatarsal joint, head at the ball of the foot.
            // The first is the stoutest, bearing the push-off.
            var metatarsals = new (Vector3 basePoint, Vector3 head, float radius)[]
            {
                (new Vector3(0.068f, 0.030f, -0.062f), new Vector3(0.064f, 0.020f, -0.128f), 0.0090f),
                (new Vector3(0.078f, 0.030f, -0.066f), new Vector3(0.075f, 0.018f, -0.135f), 0.0060f),
                (new Vector3(0.088f, 0.029f, -0.064f), new Vector3(0.086f, 0.018f, -0.130f), 0.0058f),
                (new Vector3(0.098f, 0.028f, -0.058f), new Vector3(0.097f, 0.018f, -0.124f), 0.0056f),
                (new Vector3(0.108f, 0.027f, -0.046f), new Vector3(0.107f, 0.019f, -0.114f), 0.0054f),
            };
            // Proximal / middle / distal phalanx lengths; the hallux has only two.
            var toes = new float[][]
            {
                new[] { 0.030f, 0.023f }, new[] { 0.022f, 0.011f, 0.010f }, new[] { 0.020f, 0.010f, 0.009f },
                new[] { 0.018f, 0.009f, 0.008f }, new[] { 0.015f, 0.007f, 0.007f },
            };

            for (int i = 0; i < 5; i++)
            {
                var m = metatarsals[i];
                string n = i == 0 ? "Hallux" : $"Toe{i + 1}";
                PartFactory.AddPair(root, $"Metatarsal_{i + 1}", "SYS_SK_METATARSALS",
                    Rod($"Metatarsal_{i + 1}", new[] { m.basePoint, Vector3.Lerp(m.basePoint, m.head, 0.5f), m.head }, m.radius * 1.3f, m.radius), bone, layer);

                Vector3 axis = (m.head - m.basePoint).normalized;
                Vector3 cursor = m.head;
                string[] label = { "Proximal", "Middle", "Distal" };
                for (int p = 0; p < toes[i].Length; p++)
                {
                    bool distal = p == toes[i].Length - 1;
                    string part = toes[i].Length == 2 ? (p == 0 ? "Proximal" : "Distal") : label[p];
                    Vector3 next = cursor + axis * toes[i][p];
                    float r = (i == 0 ? 0.0075f : 0.0048f) * (distal ? 0.85f : 1f);
                    PartFactory.AddPair(root, $"Phalanx_{n}_{part}", "SYS_SK_PHALANGES_FOOT",
                        Rod($"Phalanx_{n}_{part}", new[] { cursor + axis * 0.0015f, next }, r * 1.2f, r), bone, layer);
                    cursor = next;
                }
            }
        }
    }
}
