using System;
using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// What lies inside the organs: the deep structures of the brain (ventricles, corpus callosum,
    /// thalamus, basal ganglia, hippocampus), the heart's valves, septum and conduction system, the
    /// kidney's pyramids and collecting system, the lens and retina of the eye, and the bile and
    /// pancreatic ducts. Each sits inside an opaque parent, so they are reached by selecting them (the
    /// rest of the body then turns into a see-through ghost) or by hiding the outer layers.
    /// </summary>
    public static partial class OrganBuilder
    {
        private static void BuildInternals()
        {
            BuildDeepBrain();
            BuildHeartInternals();
            BuildKidneyInternals();
            BuildEyeInternals();
            BuildBiliaryDucts();
        }

        // ---------------------------------------------------------------- helpers

        private static Mesh LoftMesh(string name, Func<float, float> radius, int sides, float flatten, Vector3? widthAxis, params Vector3[] path)
        {
            var o = Loft.Options.Default;
            o.Sides = sides;
            o.Flatten = flatten;
            o.WidthAxis = widthAxis;
            o.RingsPerMetre = 1800f;
            o.CapRings = 3;
            return Loft.Tube(name, path, radius, o);   // unsaved: callers save what becomes an object
        }

        private static Mesh Sculpt(string name, SdfFunc shape, float voxel, params Vector3[] around) => Sculpt(name, shape, voxel, 0.02f, around);

        /// <summary>As above with an explicit margin: millimetre-sized structures (the ear bones) need a tight box or the grid is enormous.</summary>
        private static Mesh Sculpt(string name, SdfFunc shape, float voxel, float margin, params Vector3[] around)
        {
            PartFactory.BoundsOf(out var min, out var max, margin, around);
            return PartFactory.Save(SurfaceNets.Build(shape, min, max, voxel, name, 4f), name);
        }

        /// <summary>A shape described around <paramref name="pivot"/> with its axis along +Y, then turned so
        /// that axis points along <paramref name="axis"/>.</summary>
        private static SdfFunc Aimed(SdfFunc shape, Vector3 pivot, Vector3 axis) =>
            Sdf.Rotated(shape, pivot, Quaternion.FromToRotation(Vector3.up, axis.normalized));

        // ---------------------------------------------------------------- deep brain

        private static void BuildDeepBrain()
        {
            var white = _tissue(new Color(0.95f, 0.93f, 0.88f), 0.30f);
            var grey = _tissue(new Color(0.78f, 0.66f, 0.64f), 0.30f);
            var csf = _tissue(new Color(0.50f, 0.70f, 0.90f), 0.55f);

            // Corpus callosum: the arch of white matter joining the two hemispheres. It carries some
            // 200 million axons; cutting it (callosotomy) separates the two halves of the brain.
            Mesh callosum = LoftMesh("CorpusCallosum", t => 0.0026f + 0.0016f * Mathf.Sin(t * Mathf.PI), 12, 2.8f, Vector3.right,
                V(0f, 1.698f, -0.048f), V(0f, 1.712f, -0.028f), V(0f, 1.718f, -0.004f), V(0f, 1.716f, 0.020f), V(0f, 1.704f, 0.044f), V(0f, 1.688f, 0.058f));
            Place("CorpusCallosum", "SYS_NERV_CORPUS_CALLOSUM", PartFactory.Save(callosum, "CorpusCallosum"), white);

            // Lateral ventricles: the C-shaped, CSF-filled cavities, one in each hemisphere, curving
            // from the frontal horn back over the thalamus and down into the temporal horn.
            Mesh ventricle = LoftMesh("LateralVentricle", t => 0.0042f - 0.0018f * t + 0.0012f * Mathf.Sin(t * Mathf.PI), 10, 1.0f, null,
                V(0.007f, 1.700f, -0.052f), V(0.010f, 1.706f, -0.030f), V(0.013f, 1.706f, -0.006f), V(0.019f, 1.698f, 0.022f),
                V(0.030f, 1.680f, 0.040f), V(0.040f, 1.658f, 0.034f), V(0.044f, 1.643f, 0.012f), V(0.043f, 1.638f, -0.004f));
            PartFactory.AddPair(_root, "LateralVentricle", "SYS_NERV_VENTRICLES", PartFactory.Save(ventricle, "LateralVentricle"), csf, _layer);

            // Third ventricle (a narrow slit between the two thalami), cerebral aqueduct, fourth ventricle.
            SdfFunc third = Sdf.Ellipsoid(V(0f, 1.674f, -0.012f), V(0.0026f, 0.013f, 0.013f));
            SdfFunc fourth = Sdf.Ellipsoid(V(0f, 1.606f, 0.034f), V(0.0075f, 0.0055f, 0.0070f));
            SdfFunc aqueduct = Sdf.Capsule(V(0f, 1.664f, -0.004f), V(0f, 1.610f, 0.030f), 0.0016f);
            Mesh midline = Sculpt("MidlineVentricles", Sdf.SmoothUnion(0.003f, third, fourth, aqueduct), 0.0006f,
                V(0f, 1.60f, -0.03f), V(0f, 1.69f, 0.045f));
            Place("MidlineVentricles", "SYS_NERV_VENTRICLES", midline, csf);

            // Thalamus: the relay station through which nearly all sensory information reaches the cortex.
            Mesh thalamus = Sculpt("Thalamus", Sdf.Ellipsoid(V(0.014f, 1.683f, 0.002f), V(0.013f, 0.011f, 0.017f)), 0.0008f, V(0.014f, 1.683f, 0.002f));
            PartFactory.AddPair(_root, "Thalamus", "SYS_NERV_THALAMUS", thalamus, grey, _layer);

            // Hypothalamus: below the thalamus, the control centre for temperature, hunger, thirst and the pituitary.
            Mesh hypothalamus = Sculpt("Hypothalamus", Sdf.SmoothUnion(0.004f,
                Sdf.Ellipsoid(V(0.006f, 1.653f, -0.018f), V(0.0075f, 0.0075f, 0.010f)),
                Sdf.MirrorX(Sdf.Ellipsoid(V(0.006f, 1.653f, -0.018f), V(0.0075f, 0.0075f, 0.010f)))), 0.0005f, V(0f, 1.653f, -0.018f));
            Place("Hypothalamus", "SYS_NERV_HYPOTHALAMUS", hypothalamus, _tissue(new Color(0.86f, 0.62f, 0.58f), 0.30f));

            // Basal ganglia: the caudate nucleus (a comma arching over the thalamus), and the putamen and
            // globus pallidus beside it, which together select and smooth voluntary movements.
            SdfFunc caudate = Sdf.Chain(new[]
            {
                V(0.020f, 1.704f, -0.046f), V(0.024f, 1.714f, -0.016f), V(0.030f, 1.708f, 0.020f), V(0.040f, 1.684f, 0.034f), V(0.045f, 1.654f, 0.020f),
            }, new[] { 0.0076f, 0.0062f, 0.0046f, 0.0032f, 0.0022f }, 0.002f);
            SdfFunc putamen = Sdf.Ellipsoid(V(0.036f, 1.690f, -0.018f), V(0.008f, 0.016f, 0.020f));
            SdfFunc pallidus = Sdf.Ellipsoid(V(0.027f, 1.684f, -0.014f), V(0.005f, 0.010f, 0.012f));
            Mesh ganglia = Sculpt("BasalGanglia", Sdf.Union(caudate, putamen, pallidus), 0.0007f, V(0.018f, 1.646f, -0.056f), V(0.050f, 1.722f, 0.040f));
            PartFactory.AddPair(_root, "BasalGanglia", "SYS_NERV_BASAL_GANGLIA", ganglia, _tissue(new Color(0.72f, 0.58f, 0.60f), 0.30f), _layer);

            // Hippocampus (the seahorse, essential for forming new memories) and the amygdala at its tip
            // (emotional learning, especially fear), tucked into the medial temporal lobe.
            SdfFunc hippocampus = Sdf.Chain(new[]
            {
                V(0.030f, 1.672f, 0.042f), V(0.034f, 1.656f, 0.030f), V(0.037f, 1.644f, 0.010f), V(0.036f, 1.639f, -0.008f),
            }, new[] { 0.0040f, 0.0048f, 0.0052f, 0.0048f }, 0.002f);
            Mesh hippo = Sculpt("Hippocampus", hippocampus, 0.0006f, V(0.028f, 1.630f, -0.014f), V(0.042f, 1.678f, 0.048f));
            PartFactory.AddPair(_root, "Hippocampus", "SYS_NERV_HIPPOCAMPUS", hippo, _tissue(new Color(0.86f, 0.76f, 0.66f), 0.30f), _layer);
            Mesh amygdala = Sculpt("Amygdala", Sdf.Ellipsoid(V(0.036f, 1.636f, -0.019f), V(0.0085f, 0.0075f, 0.0085f)), 0.0006f, V(0.036f, 1.636f, -0.019f));
            PartFactory.AddPair(_root, "Amygdala", "SYS_NERV_AMYGDALA", amygdala, _tissue(new Color(0.80f, 0.52f, 0.46f), 0.30f), _layer);

            // Pineal gland: a pine-cone of tissue behind the third ventricle that makes melatonin.
            Mesh pineal = Sculpt("Pineal", Sdf.RoundCone(V(0f, 1.684f, 0.029f), 0.0036f, V(0f, 1.678f, 0.039f), 0.0016f), 0.0005f, V(0f, 1.682f, 0.034f));
            Place("Pineal", "SYS_ENDO_PINEAL", pineal, _tissue(new Color(0.76f, 0.52f, 0.38f), 0.40f));
        }

        // ---------------------------------------------------------------- heart internals

        /// <summary>A heart valve: the fibrous ring and its leaflets, described flat around
        /// <paramref name="c"/> and then aimed along the direction blood flows through it.</summary>
        private static SdfFunc Valve(Vector3 c, Vector3 flow, float ring, int cusps, float cuspLength, float thickness)
        {
            var parts = new List<SdfFunc> { Sdf.Torus(c, Quaternion.identity, ring, thickness * 1.6f) };
            for (int i = 0; i < cusps; i++)
            {
                float a = (i + 0.5f) / cusps * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                float width = ring * (cusps == 3 ? 0.62f : 0.95f);
                // Each leaflet hangs from the ring and bows toward the middle, along the flow.
                parts.Add(Sdf.Ellipsoid(c + radial * ring * 0.62f + Vector3.up * cuspLength * 0.35f,
                    new Vector3(thickness, cuspLength * 0.5f, width * 0.5f), Quaternion.LookRotation(tangent, Vector3.up)));
            }
            return Aimed(Sdf.SmoothUnion(0.0008f, parts.ToArray()), c, flow);
        }

        private static void BuildHeartInternals()
        {
            var valveMat = _tissue(new Color(0.90f, 0.84f, 0.72f), 0.50f);

            // The four valves: two atrioventricular (tricuspid on the right, mitral - two leaflets - on the
            // left) and two semilunar (pulmonary and aortic), which together keep blood moving one way.
            (string name, string id, Vector3 c, Vector3 flow, float ring, int cusps, float length)[] valves =
            {
                ("AorticValve", "SYS_CV_VALVE_AORTIC", V(0.004f, 1.298f, -0.036f), V(-0.02f, 1f, -0.01f), 0.0105f, 3, 0.0085f),
                ("MitralValve", "SYS_CV_VALVE_MITRAL", V(0.028f, 1.290f, -0.022f), V(0.35f, -0.75f, -0.35f), 0.0140f, 2, 0.0140f),
                ("TricuspidValve", "SYS_CV_VALVE_TRICUSPID", V(-0.010f, 1.280f, -0.050f), V(0.55f, -0.70f, -0.15f), 0.0165f, 3, 0.0130f),
                ("PulmonaryValve", "SYS_CV_VALVE_PULMONARY", V(0.018f, 1.304f, -0.074f), V(0.30f, 1f, 0.35f), 0.0110f, 3, 0.0080f),
            };
            foreach (var v in valves)
            {
                Mesh mesh = Sculpt(v.name, Valve(v.c, v.flow, v.ring, v.cusps, v.length, 0.0012f), 0.0006f, v.c);
                Place(v.name, v.id, mesh, valveMat);
            }

            // Interventricular septum: the muscular wall between the ventricles, thicker on the left where
            // the pressure is six times higher. Drawn as the curved plate that separates the two chambers.
            SdfFunc septum = Sdf.Ellipsoid(V(0.020f, 1.250f, -0.048f), V(0.0055f, 0.052f, 0.030f), Quaternion.Euler(0f, 0f, 24f));
            Mesh septumMesh = Sculpt("Septum", septum, 0.0008f, V(0.0f, 1.190f, -0.082f), V(0.050f, 1.310f, -0.014f));
            Place("Septum", "SYS_CV_SEPTUM", septumMesh, _tissue(new Color(0.58f, 0.15f, 0.14f), 0.38f));

            // Papillary muscles and chordae tendineae: fingers of muscle in the ventricles anchoring the
            // cords that stop the mitral and tricuspid leaflets from flipping back into the atria.
            var cords = new List<Mesh>();
            void Papillary(string name, Vector3 root, Vector3 tip, Vector3 leaflet)
            {
                cords.Add(LoftMesh(name + "Muscle", t => Mathf.Lerp(0.0032f, 0.0016f, t), 8, 1f, null, root, Vector3.Lerp(root, tip, 0.6f), tip));
                for (int k = 0; k < 4; k++)
                {
                    Vector3 end = leaflet + new Vector3((k - 1.5f) * 0.0040f, 0.002f, (k % 2 == 0 ? 0.003f : -0.003f));
                    cords.Add(LoftMesh($"{name}Cord{k}", Loft.Constant(0.00035f), 5, 1f, null, tip, Vector3.Lerp(tip, end, 0.5f), end));
                }
            }
            Papillary("LVPapillaryA", V(0.052f, 1.226f, -0.046f), V(0.047f, 1.246f, -0.040f), V(0.032f, 1.276f, -0.030f));
            Papillary("LVPapillaryB", V(0.048f, 1.222f, -0.020f), V(0.046f, 1.244f, -0.026f), V(0.030f, 1.276f, -0.018f));
            Papillary("RVPapillary", V(-0.004f, 1.236f, -0.072f), V(-0.002f, 1.256f, -0.068f), V(-0.010f, 1.274f, -0.056f));
            Place("PapillaryMuscles", "SYS_CV_PAPILLARY", Combine("PapillaryMuscles", cords), _tissue(new Color(0.62f, 0.20f, 0.18f), 0.35f));

            // Conduction system: the heart's own wiring. The sinoatrial node (the pacemaker) sits at the top of
            // the right atrium; the signal crosses to the atrioventricular node, pauses there, and runs down
            // the bundle of His and its two branches along the septum to the Purkinje fibres of the ventricles.
            var gold = _tissue(new Color(0.98f, 0.82f, 0.28f), 0.45f);
            Place("SinoatrialNode", "SYS_CV_SA_NODE",
                Sculpt("SinoatrialNode", Sdf.Ellipsoid(V(-0.030f, 1.320f, -0.046f), V(0.0035f, 0.0075f, 0.0025f)), 0.0004f, V(-0.030f, 1.32f, -0.046f)), gold);
            Place("AtrioventricularNode", "SYS_CV_AV_NODE",
                Sculpt("AtrioventricularNode", Sdf.Ellipsoid(V(-0.002f, 1.286f, -0.034f), V(0.0055f, 0.0035f, 0.0030f)), 0.0004f, V(-0.002f, 1.286f, -0.034f)), gold);

            var wiring = new List<Mesh>
            {
                LoftMesh("BundleOfHis", Loft.Constant(0.0012f), 8, 1f, null, V(-0.002f, 1.286f, -0.034f), V(0.006f, 1.278f, -0.038f), V(0.014f, 1.270f, -0.044f)),
                LoftMesh("LeftBundleBranch", Loft.Taper(0.0010f, 0.0006f), 8, 1f, null, V(0.014f, 1.270f, -0.044f), V(0.026f, 1.246f, -0.040f), V(0.040f, 1.226f, -0.034f), V(0.052f, 1.208f, -0.032f)),
                LoftMesh("RightBundleBranch", Loft.Taper(0.0010f, 0.0006f), 8, 1f, null, V(0.014f, 1.270f, -0.044f), V(0.012f, 1.246f, -0.056f), V(0.010f, 1.226f, -0.066f), V(0.014f, 1.208f, -0.070f)),
            };
            var rng = new System.Random(23);
            for (int k = 0; k < 10; k++)   // Purkinje fibres spreading into the ventricular walls at the apex
            {
                bool left = k < 6;
                Vector3 start = left ? V(0.050f, 1.212f, -0.032f) : V(0.014f, 1.210f, -0.070f);
                Vector3 end = start + new Vector3((float)(rng.NextDouble() - 0.4) * 0.022f, -(float)rng.NextDouble() * 0.016f - 0.006f, (float)(rng.NextDouble() - 0.5) * 0.030f);
                wiring.Add(LoftMesh($"Purkinje{k}", Loft.Constant(0.00045f), 5, 1f, null, start, Vector3.Lerp(start, end, 0.5f) + V(0f, 0f, 0.003f), end));
            }
            Place("HisPurkinjeSystem", "SYS_CV_HIS_PURKINJE", Combine("HisPurkinjeSystem", wiring), gold);
        }

        /// <summary>Weld several meshes into one, so a branching structure is a single selectable object.</summary>
        private static Mesh Combine(string name, List<Mesh> meshes)
        {
            var combine = new CombineInstance[meshes.Count];
            for (int i = 0; i < meshes.Count; i++) combine[i] = new CombineInstance { mesh = meshes[i], transform = Matrix4x4.identity, subMeshIndex = 0 };
            var merged = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            merged.CombineMeshes(combine, true, false);
            merged.RecalculateBounds();
            foreach (var part in meshes) UnityEngine.Object.DestroyImmediate(part);
            return PartFactory.Save(merged, name);
        }

        // ---------------------------------------------------------------- kidney internals

        private static void BuildKidneyInternals()
        {
            var medulla = _tissue(new Color(0.42f, 0.14f, 0.13f), 0.40f);
            var collecting = _tissue(new Color(0.90f, 0.80f, 0.55f), 0.45f);

            for (int s = -1; s <= 1; s += 2)
            {
                string tag = s > 0 ? "L" : "R";
                float y = s > 0 ? 1.108f : 1.090f;
                Vector3 hilum = V(s * 0.046f, y - 0.004f, 0.048f);
                Vector3 centre = V(s * 0.070f, y, 0.052f);

                // Renal pelvis and its three major calyces: the funnel that gathers urine from the pyramids and
                // passes it to the ureter.
                var funnel = new List<Mesh>();
                Vector3[] calyxTips = { V(s * 0.064f, y + 0.030f, 0.050f), V(s * 0.078f, y + 0.002f, 0.054f), V(s * 0.064f, y - 0.030f, 0.050f) };
                funnel.Add(LoftMesh($"RenalPelvis_{tag}", t => Mathf.Lerp(0.0055f, 0.0030f, t), 10, 1.4f, Vector3.up,
                    V(s * 0.040f, y - 0.010f, 0.044f), hilum + V(s * 0.004f, 0f, 0f), V(s * 0.060f, y, 0.052f)));
                for (int k = 0; k < 3; k++)
                    funnel.Add(LoftMesh($"MajorCalyx_{tag}{k}", t => Mathf.Lerp(0.0032f, 0.0026f, t), 8, 1f, null,
                        V(s * 0.060f, y, 0.052f), Vector3.Lerp(V(s * 0.060f, y, 0.052f), calyxTips[k], 0.5f), calyxTips[k]));
                Place("RenalPelvis_" + tag, "SYS_REN_PELVIS", Combine("RenalPelvis_" + tag, funnel), collecting);

                // Renal pyramids: the striped medulla, eight or so cones with their bases against the cortex and
                // their tips (papillae) opening into the calyces.
                var pyramids = new List<Mesh>();
                for (int k = 0; k < 8; k++)
                {
                    float angle = (k / 8f) * Mathf.PI * 2f + 0.3f;
                    Vector3 outward = new Vector3(Mathf.Cos(angle) * 0.85f, Mathf.Sin(angle) * 1.5f, 0.10f);
                    Vector3 tip = centre + V(s * -0.004f, 0f, 0f) + new Vector3(outward.x * 0.011f * s, outward.y * 0.011f, 0f);
                    Vector3 rim = centre + new Vector3(outward.x * 0.022f * s, outward.y * 0.034f, outward.z * 0.010f);
                    pyramids.Add(LoftMesh($"Pyramid_{tag}{k}", t => Mathf.Lerp(0.0016f, 0.0060f, t), 8, 1f, null, tip, Vector3.Lerp(tip, rim, 0.5f), rim));
                }
                Place("RenalMedulla_" + tag, "SYS_REN_MEDULLA", Combine("RenalMedulla_" + tag, pyramids), medulla);
            }
        }

        // ---------------------------------------------------------------- eye internals

        private static void BuildEyeInternals()
        {
            var c = V(0.031f, 1.658f, -0.070f);

            // Lens: the clear biconvex disc behind the pupil that changes shape to focus, thickening for near
            // objects (accommodation) as the ciliary muscle relaxes its tension.
            Mesh lens = Sculpt("Lens", Sdf.Ellipsoid(c + V(0f, 0f, -0.0093f), V(0.0046f, 0.0046f, 0.0019f)), 0.0003f, c);
            PartFactory.AddPair(_root, "Lens", "SYS_SENS_LENS", lens, _tissue(new Color(0.92f, 0.94f, 0.86f), 0.85f), _layer);

            // Retina: the light-sensitive layer lining the back of the eye, with its rods and cones, and the
            // optic disc - the blind spot - where the optic nerve leaves.
            SdfFunc shell = Sdf.Subtract(Sdf.Sphere(c, 0.0108f), Sdf.Sphere(c, 0.0100f));
            shell = Sdf.Intersect(shell, Sdf.HalfSpace(c + V(0f, 0f, -0.0010f), Vector3.forward * -1f));
            SdfFunc retina = Sdf.Union(shell, Sdf.Ellipsoid(c + V(-0.0040f, -0.0010f, 0.0100f), V(0.0018f, 0.0018f, 0.0006f)));
            Mesh retinaMesh = Sculpt("Retina", retina, 0.0003f, c);
            PartFactory.AddPair(_root, "Retina", "SYS_SENS_RETINA", retinaMesh, _tissue(new Color(0.80f, 0.34f, 0.24f), 0.50f), _layer);
        }

        // ---------------------------------------------------------------- bile and pancreatic ducts

        private static void BuildBiliaryDucts()
        {
            var bileMat = _tissue(new Color(0.36f, 0.62f, 0.26f), 0.55f);
            var kit = new TubeKit(_root, bileMat, _layer);
            Vector3 ampulla = V(-0.050f, 1.084f, -0.010f);

            // Bile ducts: hepatic ducts leave the liver, join the cystic duct from the gallbladder, and the common
            // bile duct runs down behind the duodenum to open with the pancreatic duct at the ampulla.
            kit.Add("RightHepaticDuct", "SYS_DIG_BILE_DUCTS", 0.0022f, 0.0026f, false, V(-0.080f, 1.146f, -0.026f), V(-0.060f, 1.134f, -0.034f), V(-0.046f, 1.126f, -0.036f));
            kit.Add("LeftHepaticDuct", "SYS_DIG_BILE_DUCTS", 0.0020f, 0.0026f, false, V(-0.010f, 1.156f, -0.046f), V(-0.028f, 1.138f, -0.042f), V(-0.046f, 1.126f, -0.036f));
            kit.Add("CommonBileDuct", "SYS_DIG_BILE_DUCTS", 0.0030f, 0.0032f, false,
                V(-0.046f, 1.126f, -0.036f), V(-0.044f, 1.110f, -0.030f), V(-0.048f, 1.096f, -0.018f), ampulla);
            kit.Add("CysticDuct", "SYS_DIG_BILE_DUCTS", 0.0022f, 0.0026f, false, V(-0.038f, 1.086f, -0.052f), V(-0.041f, 1.100f, -0.043f), V(-0.045f, 1.114f, -0.032f));
            kit.Add("PancreaticDuct", "SYS_DIG_PANCREATIC_DUCT", 0.0016f, 0.0028f, false,
                V(0.066f, 1.142f, 0.014f), V(0.030f, 1.124f, -0.004f), V(-0.004f, 1.110f, -0.016f), V(-0.034f, 1.098f, -0.012f), ampulla);
        }
    }
}
