using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// The skull, built as one solid and then divided into its named bones. Dividing a
    /// single shape by region masks - each mask a cutting surface where a real suture
    /// runs, with a hair's gap between neighbours - gives individually selectable
    /// frontal, parietal, temporal, occipital, sphenoid, zygomatic, maxillary and
    /// nasal bones whose outlines fall exactly where the sutures do.
    ///
    /// Head landmarks (metres, figure space): vertex 1.74, eyes 1.66, ear canal 1.63,
    /// skull base 1.585, upper teeth 1.57, chin 1.525. The face is at -Z.
    /// </summary>
    public static partial class SkeletonBuilder
    {
        private const float SutureGap = 0.0017f;

        private static SdfFunc SkullSolid()
        {
            // The braincase: an ellipsoid, flattened underneath into the skull base.
            SdfFunc cranium = Sdf.Ellipsoid(new Vector3(0f, 1.662f, 0.008f), new Vector3(0.069f, 0.078f, 0.092f));
            SdfFunc occiput = Sdf.Ellipsoid(new Vector3(0f, 1.634f, 0.082f), new Vector3(0.052f, 0.040f, 0.022f));
            // A real skull is broadest low down, at the temporal bones. A plain ellipsoid
            // narrows too fast toward the base and leaves the mastoid hanging off it.
            SdfFunc baseMass = Sdf.Ellipsoid(new Vector3(0f, 1.614f, 0.010f), new Vector3(0.066f, 0.046f, 0.088f));
            cranium = Sdf.SmoothUnion(0.018f, cranium, occiput, baseMass);
            cranium = Sdf.Intersect(cranium, Sdf.HalfSpace(new Vector3(0f, 1.588f, 0f), Vector3.down));

            // Face: a mass under the brow, the cheekbones and their arches, the brow
            // ridges, and the tooth-bearing alveolar arch.
            SdfFunc faceMass = Sdf.Ellipsoid(new Vector3(0f, 1.612f, -0.046f), new Vector3(0.047f, 0.050f, 0.042f));
            // The cheekbone is a plate facing forward and outward, not a ball: a thin
            // ellipsoid turned about 40 degrees off the front.
            SdfFunc cheekbones = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.047f, 1.634f, -0.064f),
                new Vector3(0.008f, 0.014f, 0.018f), Quaternion.Euler(0f, 41f, 0f)));
            SdfFunc zygomaticArch = Sdf.MirrorX(Sdf.Capsule(new Vector3(0.056f, 1.628f, -0.056f), new Vector3(0.071f, 1.627f, -0.006f), 0.0050f));
            SdfFunc browRidges = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.030f, 1.680f, -0.078f), new Vector3(0.026f, 0.006f, 0.009f)));
            SdfFunc glabella = Sdf.Sphere(new Vector3(0f, 1.679f, -0.082f), 0.0080f);
            SdfFunc alveolar = Sdf.Ellipsoid(new Vector3(0f, 1.583f, -0.052f), new Vector3(0.033f, 0.014f, 0.031f));
            SdfFunc mastoids = Sdf.MirrorX(Sdf.Sphere(new Vector3(0.055f, 1.601f, 0.024f), 0.0080f));
            SdfFunc condyles = Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.0125f, 1.586f, 0.030f), new Vector3(0.0055f, 0.006f, 0.0115f)));
            SdfFunc styloids = Sdf.MirrorX(Sdf.RoundCone(new Vector3(0.046f, 1.592f, 0.022f), 0.0025f, new Vector3(0.040f, 1.568f, 0.030f), 0.0016f));

            SdfFunc skull = Sdf.SmoothUnion(0.011f, cranium, faceMass, cheekbones, zygomaticArch, browRidges, glabella, alveolar, mastoids, condyles, styloids);

            // Openings. The orbits (socket, tapering into the depth of the skull),
            // the pear-shaped nasal aperture, the foramen magnum for the spinal cord,
            // and the ear canals.
            SdfFunc orbits = Sdf.MirrorX(Sdf.SmoothUnion(0.006f,
                Sdf.Sphere(new Vector3(0.031f, 1.658f, -0.080f), 0.0215f),
                Sdf.RoundCone(new Vector3(0.031f, 1.658f, -0.075f), 0.020f, new Vector3(0.024f, 1.655f, -0.042f), 0.008f)));
            SdfFunc nasalAperture = Sdf.SmoothUnion(0.005f,
                Sdf.Ellipsoid(new Vector3(0f, 1.628f, -0.092f), new Vector3(0.0115f, 0.020f, 0.020f)),
                Sdf.Ellipsoid(new Vector3(0f, 1.612f, -0.092f), new Vector3(0.0145f, 0.012f, 0.018f)));
            SdfFunc foramenMagnum = Sdf.Capsule(new Vector3(0f, 1.570f, 0.028f), new Vector3(0f, 1.610f, 0.028f), 0.0145f);
            SdfFunc earCanals = Sdf.MirrorX(Sdf.Capsule(new Vector3(0.078f, 1.628f, 0.010f), new Vector3(0.055f, 1.630f, 0.010f), 0.0038f));

            return Sdf.Subtract(skull, orbits, nasalAperture, foramenMagnum, earCanals);
        }

        /// <summary>Everything at or beyond this distance from a region mask's
        /// surface belongs to it. Built from primitives so the cut is a clean
        /// plane or blob, and adjacent masks are pulled apart by the suture gap.</summary>
        private struct CranialBone
        {
            public string Name, EntityId;
            public SdfFunc Mask;
            public bool Paired;
        }

        private static SdfFunc Left(SdfFunc f) => Sdf.Intersect(f, Sdf.HalfSpace(new Vector3(0.0008f, 0f, 0f), Vector3.left));
        private static SdfFunc Below(float y) => Sdf.HalfSpace(new Vector3(0f, y, 0f), Vector3.up);       // negative for p.y < y
        private static SdfFunc Above(float y) => Sdf.HalfSpace(new Vector3(0f, y, 0f), Vector3.down);     // negative for p.y > y
        private static SdfFunc InFrontOf(float z) => Sdf.HalfSpace(new Vector3(0f, 0f, z), Vector3.forward); // negative for p.z < z
        private static SdfFunc Behind(float z) => Sdf.HalfSpace(new Vector3(0f, 0f, z), Vector3.back);       // negative for p.z > z

        private static List<CranialBone> CranialBones()
        {
            // Listed in priority order: earlier bones keep their territory, later ones
            // are carved away from it.
            SdfFunc nasal = Sdf.RoundCone(new Vector3(0f, 1.672f, -0.084f), 0.0085f, new Vector3(0f, 1.640f, -0.100f), 0.0085f);

            SdfFunc zygomatic = Left(Sdf.Union(
                Sdf.Sphere(new Vector3(0.049f, 1.633f, -0.062f), 0.026f),
                Sdf.Capsule(new Vector3(0.056f, 1.628f, -0.056f), new Vector3(0.068f, 1.627f, -0.030f), 0.010f)));

            SdfFunc maxilla = Left(Sdf.Union(
                Sdf.Intersect(Below(1.640f), InFrontOf(-0.028f), Sdf.HalfSpace(new Vector3(0.052f, 0f, 0f), Vector3.right)),
                Sdf.Intersect(Below(1.665f), InFrontOf(-0.040f), Sdf.HalfSpace(new Vector3(0.026f, 0f, 0f), Vector3.right))));

            SdfFunc sphenoid = Left(Sdf.Ellipsoid(new Vector3(0.063f, 1.643f, -0.030f), new Vector3(0.013f, 0.021f, 0.024f)));

            SdfFunc temporal = Left(Sdf.Union(
                Sdf.Ellipsoid(new Vector3(0.060f, 1.626f, 0.016f), new Vector3(0.030f, 0.056f, 0.046f)),
                Sdf.Intersect(Below(1.600f), Behind(-0.010f), Sdf.HalfSpace(new Vector3(0.030f, 0f, 0f), Vector3.left))));

            // The coronal suture arches over the top from ear to ear, so the frontal
            // bone's back edge is a tilted plane, not a vertical one.
            var coronalNormal = new Vector3(0f, 0.28f, 0.96f).normalized;
            SdfFunc frontal = Sdf.Intersect(Above(1.666f), Sdf.HalfSpace(new Vector3(0f, 1.742f, -0.012f), coronalNormal));

            SdfFunc occipital = Sdf.Union(
                Sdf.Ellipsoid(new Vector3(0f, 1.615f, 0.075f), new Vector3(0.078f, 0.070f, 0.060f)),
                Sdf.Intersect(Below(1.600f), Behind(-0.020f), Sdf.HalfSpace(new Vector3(0.030f, 0f, 0f), Vector3.right),
                    Sdf.HalfSpace(new Vector3(-0.030f, 0f, 0f), Vector3.left)));

            SdfFunc parietal = Left(Above(1.610f));

            return new List<CranialBone>
            {
                new CranialBone { Name = "NasalBones", EntityId = "SYS_SK_NASAL", Mask = nasal, Paired = false },
                new CranialBone { Name = "Zygomatic", EntityId = "SYS_SK_ZYGOMATIC", Mask = zygomatic, Paired = true },
                new CranialBone { Name = "Maxilla", EntityId = "SYS_SK_MAXILLA", Mask = maxilla, Paired = true },
                new CranialBone { Name = "Sphenoid", EntityId = "SYS_SK_SPHENOID", Mask = sphenoid, Paired = true },
                new CranialBone { Name = "Temporal", EntityId = "SYS_SK_TEMPORAL", Mask = temporal, Paired = true },
                new CranialBone { Name = "Frontal", EntityId = "SYS_SK_FRONTAL", Mask = frontal, Paired = false },
                new CranialBone { Name = "Occipital", EntityId = "SYS_SK_OCCIPITAL", Mask = occipital, Paired = false },
                new CranialBone { Name = "Parietal", EntityId = "SYS_SK_PARIETAL", Mask = parietal, Paired = true },
                // Whatever the masks above leave uncovered - chiefly the central skull
                // base - belongs to the sphenoid, which is the bone that forms it. Without
                // this catch-all those patches would be holes in the skull.
                new CranialBone { Name = "SphenoidBase", EntityId = "SYS_SK_SPHENOID", Mask = Left(Sdf.Sphere(Vector3.zero, 50f)), Paired = true },
            };
        }

        private static void BuildSkull(Transform root, Material bone, Material cartilage, int layer)
        {
            SdfFunc skull = SkullSolid();
            var bones = CranialBones();
            var min = new Vector3(-0.088f, 1.560f, -0.118f);
            var max = new Vector3(0.088f, 1.752f, 0.116f);

            for (int i = 0; i < bones.Count; i++)
            {
                SdfFunc territory = bones[i].Mask;
                for (int j = 0; j < i; j++) territory = Sdf.Subtract(territory, Sdf.Offset(bones[j].Mask, SutureGap));

                SdfFunc region = Sdf.Intersect(skull, territory);
                Mesh mesh = PartFactory.Save(SurfaceNets.Build(region, min, max, 0.0016f, bones[i].Name), "Skull_" + bones[i].Name);

                if (bones[i].Paired) PartFactory.AddPair(root, bones[i].Name, bones[i].EntityId, mesh, bone, layer);
                else PartFactory.Add(root, bones[i].Name, bones[i].EntityId, mesh, bone, layer);
            }

            BuildMandible(root, bone, layer);
            BuildTeeth(root, bone, layer);
            BuildHyoid(root, bone, layer);
        }

        private static void BuildMandible(Transform root, Material bone, int layer)
        {
            // Body: the U of the jaw from chin to angle, thickest at the chin.
            SdfFunc body = Sdf.Chain(new[]
            {
                new Vector3(0.000f, 1.536f, -0.082f), new Vector3(0.020f, 1.538f, -0.079f), new Vector3(0.038f, 1.546f, -0.066f),
                new Vector3(0.050f, 1.556f, -0.044f), new Vector3(0.056f, 1.564f, -0.016f), new Vector3(0.057f, 1.566f, 0.006f),
            }, new[] { 0.0115f, 0.0105f, 0.0098f, 0.0095f, 0.0092f, 0.0100f }, 0.004f);

            // Ramus: the broad plate rising to the jaw joint, with the coronoid
            // process in front and the condyle behind the mandibular notch.
            SdfFunc ramus = Sdf.Ellipsoid(new Vector3(0.056f, 1.592f, 0.000f), new Vector3(0.0045f, 0.030f, 0.017f));
            SdfFunc coronoid = Sdf.RoundCone(new Vector3(0.053f, 1.590f, -0.020f), 0.0055f, new Vector3(0.051f, 1.633f, -0.027f), 0.0040f);
            SdfFunc condyle = Sdf.Ellipsoid(new Vector3(0.056f, 1.628f, 0.010f), new Vector3(0.010f, 0.005f, 0.006f));
            SdfFunc neck = Sdf.RoundCone(new Vector3(0.056f, 1.600f, 0.008f), 0.0050f, new Vector3(0.056f, 1.626f, 0.010f), 0.0040f);
            SdfFunc chin = Sdf.Ellipsoid(new Vector3(0f, 1.543f, -0.083f), new Vector3(0.016f, 0.010f, 0.007f));

            SdfFunc mandible = Sdf.SmoothUnion(0.008f, body, Sdf.MirrorX(Sdf.SmoothUnion(0.006f, ramus, coronoid, condyle, neck)), chin);
            // Mirror the whole body so the two halves meet at the symphysis.
            mandible = Sdf.SmoothUnion(0.006f, mandible, Sdf.MirrorX(body));

            PartFactory.Add(root, "Mandible", "SYS_SK_MANDIBLE",
                PartFactory.Save(SurfaceNets.Build(mandible,
                    new Vector3(-0.075f, 1.520f, -0.100f), new Vector3(0.075f, 1.650f, 0.030f), 0.0014f, "Mandible"), "Mandible"), bone, layer);
        }

        /// <summary>Sixteen teeth per arch: 4 incisors, 2 canines, 4 premolars, 6 molars.
        /// Each is an ellipsoid sized for its kind and set round a parabolic arch.</summary>
        private static void BuildTeeth(Transform root, Material bone, int layer)
        {
            // Width and height by distance from the midline: central incisor first.
            float[] widths = { 0.0085f, 0.0068f, 0.0075f, 0.0066f, 0.0066f, 0.0094f, 0.0100f, 0.0094f };
            float[] heights = { 0.0105f, 0.0090f, 0.0112f, 0.0085f, 0.0085f, 0.0075f, 0.0075f, 0.0070f };

            SdfFunc Arch(float y, float zCentre, float a, float b, float heightSign, float scale)
            {
                var teeth = new List<SdfFunc>();
                for (int side = -1; side <= 1; side += 2)
                    for (int k = 0; k < 8; k++)
                    {
                        float theta = (0.10f + k * 0.175f) * side;
                        Vector3 c = new Vector3(a * Mathf.Sin(theta), y + heightSign * heights[k] * 0.4f, zCentre - b * Mathf.Cos(theta));
                        Quaternion facing = Quaternion.Euler(0f, theta * Mathf.Rad2Deg, 0f);
                        teeth.Add(Sdf.Ellipsoid(c, new Vector3(widths[k] * 0.5f * scale, heights[k] * 0.5f * scale, widths[k] * 0.42f * scale), facing));
                    }
                return Sdf.Union(teeth.ToArray());
            }

            SdfFunc upper = Arch(1.5675f, -0.038f, 0.031f, 0.046f, -1f, 1f);
            SdfFunc lower = Arch(1.5490f, -0.040f, 0.029f, 0.043f, +1f, 0.96f);

            PartFactory.Add(root, "TeethUpper", "SYS_SK_TEETH",
                PartFactory.Save(SurfaceNets.Build(upper, new Vector3(-0.045f, 1.550f, -0.095f), new Vector3(0.045f, 1.585f, -0.010f), 0.0009f, "TeethUpper"), "TeethUpper"), bone, layer);
            PartFactory.Add(root, "TeethLower", "SYS_SK_TEETH",
                PartFactory.Save(SurfaceNets.Build(lower, new Vector3(-0.045f, 1.532f, -0.095f), new Vector3(0.045f, 1.567f, -0.010f), 0.0009f, "TeethLower"), "TeethLower"), bone, layer);
        }

        private static void BuildHyoid(Transform root, Material bone, int layer)
        {
            // The floating U-shaped bone under the tongue: a body with greater and
            // lesser horns. It is anchored to nothing by bone - only muscle and ligament.
            SdfFunc hyoid = Sdf.SmoothUnion(0.004f,
                Sdf.Ellipsoid(new Vector3(0f, 1.502f, -0.046f), new Vector3(0.014f, 0.0055f, 0.0050f)),
                Sdf.MirrorX(Sdf.Chain(new[] { new Vector3(0.012f, 1.502f, -0.045f), new Vector3(0.024f, 1.502f, -0.032f), new Vector3(0.033f, 1.503f, -0.012f) },
                    new[] { 0.0042f, 0.0036f, 0.0040f }, 0.003f)),
                Sdf.MirrorX(Sdf.Sphere(new Vector3(0.011f, 1.510f, -0.044f), 0.0028f)));
            PartFactory.Add(root, "Hyoid", "SYS_SK_HYOID",
                PartFactory.Save(SurfaceNets.Build(hyoid, new Vector3(-0.045f, 1.490f, -0.060f), new Vector3(0.045f, 1.520f, 0.000f), 0.0010f, "Hyoid"), "Hyoid"), bone, layer);
        }
    }
}
