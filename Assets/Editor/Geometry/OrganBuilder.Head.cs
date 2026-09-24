using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// The brain, the special senses (eye, ear), the tongue, the salivary glands and the lymph
    /// nodes. The brain is built like the skull: one folded surface divided along real
    /// boundaries into frontal, parietal, temporal and occipital lobes, so each can be
    /// selected and the lateral (Sylvian) fissure falls where the frontal lobe meets the temporal.
    /// </summary>
    public static partial class OrganBuilder
    {
        private static void BuildBrainAndHead()
        {
            BuildBrain();
            BuildEyes();
            BuildInnerEar();
            BuildMouthAndGlands();
            BuildLymphNodes();
        }

        // ---------------------------------------------------------------- brain

        private static SdfFunc CerebralHemisphere()
        {
            // One hemisphere (left, x > 0): an ellipsoid with a flattened base, its surface folded
            // into gyri and sulci. Folding uses the absolute value of noise, which makes creases
            // (sulci) between broad ridges (gyri) - a brain, not a bumpy potato.
            SdfFunc smooth = Sdf.Ellipsoid(V(0f, 1.672f, 0.006f), V(0.061f, 0.060f, 0.082f));
            smooth = Sdf.Intersect(smooth, Sdf.HalfSpace(V(0.0012f, 0f, 0f), Vector3.left), Sdf.HalfSpace(V(0f, 1.612f, 0f), Vector3.down));
            return p => smooth(p) + 0.0040f * (Mathf.Abs(Sdf.Noise3(p * 62f, 3)) - 0.42f)
                                  + 0.0009f * (Mathf.Abs(Sdf.Noise3(p * 170f, 8)) - 0.45f);
        }

        private static void BuildBrain()
        {
            SdfFunc hemisphere = CerebralHemisphere();
            var brainMat = _tissue(new Color(0.86f, 0.76f, 0.73f), 0.30f);

            // Lobe territories, in priority order. The frontal and temporal lobes are separated by
            // the lateral fissure, the frontal and parietal by the central sulcus.
            SdfFunc occipital = Behind(0.046f);
            SdfFunc temporal = Sdf.Intersect(p => p.y - (1.640f + (p.z + 0.045f) * 0.33f), Behind(-0.058f), InFrontOf(0.050f));
            SdfFunc frontal = InFrontOf(-0.004f);
            SdfFunc parietal = Sdf.Sphere(Vector3.zero, 50f);

            var lobes = new (string name, string id, SdfFunc mask)[]
            {
                ("OccipitalLobe", "SYS_NERV_OCCIPITAL_LOBE", occipital),
                ("TemporalLobe", "SYS_NERV_TEMPORAL_LOBE", temporal),
                ("FrontalLobe", "SYS_NERV_FRONTAL_LOBE", frontal),
                ("ParietalLobe", "SYS_NERV_PARIETAL_LOBE", parietal),
            };

            var min = V(-0.005f, 1.605f, -0.095f);
            var max = V(0.075f, 1.745f, 0.100f);
            for (int i = 0; i < lobes.Length; i++)
            {
                SdfFunc territory = lobes[i].mask;
                for (int j = 0; j < i; j++) territory = Sdf.Subtract(territory, Sdf.Offset(lobes[j].mask, 0.0016f));
                Mesh mesh = Make(lobes[i].name, Sdf.Intersect(hemisphere, territory), min, max, 0.0016f);
                PartFactory.AddPair(_root, lobes[i].name, lobes[i].id, mesh, brainMat, _layer);
            }

            // Cerebellum: two hemispheres and a midline vermis, finely ridged into folia.
            SdfFunc cerebellum = Sdf.SmoothUnion(0.012f,
                Sdf.Ellipsoid(V(0f, 1.616f, 0.066f), V(0.052f, 0.028f, 0.036f)),
                Sdf.Ellipsoid(V(0f, 1.622f, 0.050f), V(0.014f, 0.024f, 0.026f)));
            cerebellum = Sdf.Intersect(cerebellum, Sdf.HalfSpace(V(0f, 1.588f, 0f), Vector3.down));
            SdfFunc folia = p => cerebellum(p) + 0.0016f * Mathf.Sin(p.y * 1600f);
            Place("Cerebellum", "SYS_NERV_CEREBELLUM", Make("Cerebellum", folia, V(-0.065f, 1.58f, 0.02f), V(0.065f, 1.66f, 0.11f), 0.0009f),
                _tissue(new Color(0.78f, 0.64f, 0.60f), 0.30f));

            // Brainstem: midbrain, pons (the bulge) and medulla, tapering into the spinal cord
            // through the foramen magnum.
            var stemMat = _tissue(new Color(0.82f, 0.74f, 0.66f), 0.30f);
            Place("Midbrain", "SYS_NERV_MIDBRAIN", Make("Midbrain",
                Sdf.RoundCone(V(0f, 1.628f, -0.002f), 0.0135f, V(0f, 1.610f, 0.003f), 0.0130f), V(-0.03f, 1.59f, -0.03f), V(0.03f, 1.65f, 0.03f), 0.0007f), stemMat);
            Place("Pons", "SYS_NERV_PONS", Make("Pons",
                Sdf.Ellipsoid(V(0f, 1.598f, 0.003f), V(0.021f, 0.014f, 0.018f)), V(-0.04f, 1.57f, -0.03f), V(0.04f, 1.63f, 0.03f), 0.0007f), stemMat);
            Place("Medulla", "SYS_NERV_MEDULLA", Make("Medulla",
                Sdf.RoundCone(V(0f, 1.590f, 0.010f), 0.0125f, V(0f, 1.572f, 0.026f), 0.0085f), V(-0.03f, 1.55f, -0.02f), V(0.03f, 1.62f, 0.05f), 0.0007f), stemMat);

            // Pituitary gland, hanging below the hypothalamus in its saddle of the sphenoid bone.
            Place("Pituitary", "SYS_ENDO_PITUITARY", Make("Pituitary",
                Sdf.Ellipsoid(V(0f, 1.612f, -0.030f), V(0.0075f, 0.0055f, 0.0065f)), V(-0.02f, 1.59f, -0.05f), V(0.02f, 1.63f, -0.01f), 0.0005f),
                _tissue(new Color(0.78f, 0.60f, 0.42f), 0.40f));
        }

        // ---------------------------------------------------------------- eyes

        private static void BuildEyes()
        {
            var scleraMat = _tissue(new Color(0.94f, 0.92f, 0.90f), 0.55f);
            var irisMat = _tissue(new Color(0.34f, 0.24f, 0.16f), 0.60f);
            var pupilMat = _tissue(new Color(0.04f, 0.04f, 0.05f), 0.70f);

            // Eyeball: a sphere with the more sharply curved, transparent cornea bulging in front.
            var c = V(0.031f, 1.658f, -0.070f);
            SdfFunc eye = Sdf.SmoothUnion(0.004f, Sdf.Sphere(c, 0.0118f), Sdf.Sphere(c + V(0f, 0f, -0.0075f), 0.0078f));
            PartFactory.AddPair(_root, "Eyeball", "SYS_SENS_EYE",
                Make("Eyeball", eye, c - Vector3.one * 0.02f, c + Vector3.one * 0.02f, 0.0004f), scleraMat, _layer);

            // Iris and pupil: coloured ring and black centre, set just inside the cornea.
            PartFactory.AddPair(_root, "Iris", "SYS_SENS_EYE",
                Make("Iris", Sdf.Ellipsoid(c + V(0f, 0f, -0.0128f), V(0.0062f, 0.0062f, 0.0009f)), c - Vector3.one * 0.02f, c + Vector3.one * 0.02f, 0.0002f), irisMat, _layer);
            PartFactory.AddPair(_root, "Pupil", "SYS_SENS_EYE",
                Make("Pupil", Sdf.Ellipsoid(c + V(0f, 0f, -0.0134f), V(0.0028f, 0.0028f, 0.0006f)), c - Vector3.one * 0.02f, c + Vector3.one * 0.02f, 0.0002f), pupilMat, _layer);

            // The six muscles that move each eye. Four recti run straight back to a common tendon
            // at the apex of the orbit; the two obliques loop round - the superior through a
            // fibrous pulley (the trochlea) at the top inner corner.
            var apex = V(0.026f, 1.657f, -0.040f);
            var muscle = _tissue(MuscleTint, 0.30f);
            void Extra(string name, params Vector3[] path)
            {
                var o = Loft.Options.Default; o.Sides = 8; o.RingsPerMetre = 260f;
                Mesh m = PartFactory.Save(Loft.Tube(name, path, Loft.BellyProfile(0.0010f, 0.0028f, 0.15f, 0.8f), o), name);
                PartFactory.AddPair(_root, name, "SYS_MUSC_EXTRAOCULAR", m, muscle, _layer);
            }
            Extra("SuperiorRectus", apex, V(0.030f, 1.664f, -0.058f), c + V(0f, 0.0110f, -0.001f));
            Extra("InferiorRectus", apex, V(0.030f, 1.650f, -0.058f), c + V(0f, -0.0110f, -0.001f));
            Extra("MedialRectus", apex, V(0.026f, 1.657f, -0.058f), c + V(-0.0112f, 0f, -0.001f));
            Extra("LateralRectus", apex, V(0.034f, 1.657f, -0.058f), c + V(0.0112f, 0f, -0.001f));
            Extra("SuperiorOblique", apex + V(-0.002f, 0.004f, 0f), V(0.024f, 1.670f, -0.070f), V(0.022f, 1.673f, -0.086f), V(0.034f, 1.671f, -0.078f), c + V(0.006f, 0.010f, 0.004f));
            Extra("InferiorOblique", V(0.020f, 1.646f, -0.084f), V(0.030f, 1.645f, -0.078f), c + V(0.008f, -0.010f, 0.005f));
        }

        // ---------------------------------------------------------------- inner ear

        private static void BuildInnerEar()
        {
            var mat = _tissue(new Color(0.90f, 0.82f, 0.70f), 0.55f);
            var origin = V(0.049f, 1.614f, 0.002f);
            var rotation = Quaternion.Euler(0f, 30f, 35f);

            // Cochlea: a tube coiled two and a half turns like a snail shell, where sound becomes
            // nerve impulses.
            var spiral = new List<Vector3>();
            const int steps = 44;
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps;
                float angle = u * Mathf.PI * 2f * 2.5f;
                float radius = Mathf.Lerp(0.0048f, 0.0012f, u);
                spiral.Add(origin + rotation * new Vector3(Mathf.Cos(angle) * radius, u * 0.0060f, Mathf.Sin(angle) * radius));
            }
            var o = Loft.Options.Default; o.Sides = 8; o.RingsPerMetre = 5000f;
            PartFactory.AddPair(_root, "Cochlea", "SYS_SENS_COCHLEA",
                PartFactory.Save(Loft.Tube("Cochlea", spiral, Loft.Taper(0.0017f, 0.0007f), o), "Cochlea"), mat, _layer);

            // Semicircular canals: three loops at right angles to each other that sense rotation.
            var canalCentre = origin + rotation * V(0.004f, 0.009f, -0.006f);
            for (int k = 0; k < 3; k++)
            {
                var loop = new List<Vector3>();
                for (int i = 0; i <= 20; i++)
                {
                    float a = i / 20f * Mathf.PI * 2f;
                    Vector3 local = k == 0 ? new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f)
                                  : k == 1 ? new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a))
                                  : new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a));
                    loop.Add(canalCentre + rotation * (local * 0.0052f));
                }
                var co = Loft.Options.Default; co.Sides = 6; co.RingsPerMetre = 5000f;
                PartFactory.AddPair(_root, "SemicircularCanal" + (k + 1), "SYS_SENS_SEMICIRCULAR",
                    PartFactory.Save(Loft.Tube("SemicircularCanal" + (k + 1), loop, Loft.Constant(0.0008f), co), "SemicircularCanal" + (k + 1)), mat, _layer);
            }
        }

        // ---------------------------------------------------------------- mouth and glands

        private static void BuildMouthAndGlands()
        {
            // Tongue: a muscular organ lying between the teeth, thick at its root and tapering forward.
            SdfFunc tongue = Sdf.SmoothUnion(0.012f,
                Sdf.Ellipsoid(V(0f, 1.553f, -0.046f), V(0.022f, 0.009f, 0.034f)),
                Sdf.Ellipsoid(V(0f, 1.540f, -0.020f), V(0.020f, 0.016f, 0.020f)));
            tongue = Sdf.Subtract(tongue, Sdf.Capsule(V(0f, 1.562f, -0.076f), V(0f, 1.560f, -0.030f), 0.0016f)); // median groove
            tongue = Sdf.Displace(tongue, 0.0006f, 500f, 4);                                                      // papillae
            Place("Tongue", "SYS_DIG_TONGUE", Make("Tongue", tongue, V(-0.04f, 1.52f, -0.09f), V(0.04f, 1.58f, 0.005f), 0.0008f),
                _tissue(new Color(0.84f, 0.46f, 0.46f), 0.55f));

            var gland = _tissue(new Color(0.88f, 0.72f, 0.56f), 0.35f);
            PartFactory.AddPair(_root, "ParotidGland", "SYS_DIG_PAROTID",
                Make("ParotidGland", Sdf.Ellipsoid(V(0.066f, 1.588f, 0.012f), V(0.012f, 0.026f, 0.014f)), V(0.03f, 1.54f, -0.03f), V(0.10f, 1.64f, 0.05f), 0.0009f), gland, _layer);
            PartFactory.AddPair(_root, "SubmandibularGland", "SYS_DIG_SUBMANDIBULAR",
                Make("SubmandibularGland", Sdf.Ellipsoid(V(0.030f, 1.530f, -0.030f), V(0.013f, 0.009f, 0.016f)), V(0.0f, 1.50f, -0.06f), V(0.06f, 1.56f, 0.0f), 0.0008f), gland, _layer);
        }

        // ---------------------------------------------------------------- lymph nodes

        private static void BuildLymphNodes()
        {
            // Bean-shaped nodes in the chains where lymph is filtered: down the side of the neck,
            // in the armpit, and in the groin. One shared bean mesh, placed and turned per node.
            SdfFunc bean = Sdf.SmoothSubtract(0.0012f, Sdf.Ellipsoid(Vector3.zero, V(0.0050f, 0.0035f, 0.0035f)),
                Sdf.Sphere(V(0f, 0.0034f, 0f), 0.0020f));
            Mesh mesh = PartFactory.Save(SurfaceNets.Build(bean, V(-0.008f, -0.007f, -0.007f), V(0.008f, 0.007f, 0.007f), 0.0004f, "LymphNode", 4f), "LymphNode");
            var mat = _tissue(new Color(0.72f, 0.66f, 0.58f), 0.45f);

            var rng = new System.Random(11);
            void Node(string name, Vector3 at)
            {
                var go = PartFactory.Add(_root, name, "SYS_LYMPH_NODES", mesh, mat, _layer);
                go.transform.localPosition = at;
                go.transform.localRotation = Quaternion.Euler((float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 360f);
                go.transform.localScale = Vector3.one * (0.85f + 0.5f * (float)rng.NextDouble());
            }

            for (int s = -1; s <= 1; s += 2)
            {
                string tag = s > 0 ? "L" : "R";
                for (int i = 0; i < 6; i++)   // cervical chain along the sternocleidomastoid
                    Node($"CervicalNode_{tag}{i}", V(s * (0.046f + 0.003f * (i % 2)), 1.505f + 0.010f * i, -0.002f + 0.003f * (i % 3)));
                for (int i = 0; i < 7; i++)   // axillary group in the armpit
                    Node($"AxillaryNode_{tag}{i}", V(s * (0.146f + 0.008f * (i % 3)), 1.318f + 0.010f * (i % 4), -0.012f + 0.012f * (i % 3)));
                for (int i = 0; i < 5; i++)   // inguinal group in the groin
                    Node($"InguinalNode_{tag}{i}", V(s * (0.062f + 0.006f * (i % 3)), 0.862f - 0.008f * (i % 3), -0.040f + 0.005f * i));
            }
        }
    }
}
