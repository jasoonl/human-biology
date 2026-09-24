using System;
using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// The branching airway inside each lung (with the pulmonary arteries and veins that follow it) and the
    /// upper airway and throat: pharynx, nasal cavity and turbinates, paranasal sinuses, tonsils, epiglottis
    /// and the soft palate with its uvula.
    /// </summary>
    public static partial class OrganBuilder
    {
        private static void BuildAirwayDetail()
        {
            BuildBronchialTrees();
            BuildThroatAndNose();
        }

        // ---------------------------------------------------------------- bronchial tree

        private struct Twig
        {
            public Vector3 From, To;
            public float R0, R1;
        }

        /// <summary>
        /// Grows a tree that fills a region: the branch runs part of the way toward the centre of the
        /// points it must reach, then splits them into two groups along their longest direction and sends
        /// a branch to each, all the way down to single points. Diameters follow the number of points
        /// each branch serves, so they taper the way real airways do, and the tree spreads through the whole
        /// lung instead of hugging one edge.
        /// </summary>
        private static void GrowToward(List<Twig> twigs, Vector3 from, List<Vector3> points, int total, float rootRadius)
        {
            if (points.Count == 0) return;
            float radius = Mathf.Max(0.00042f, rootRadius * Mathf.Pow(points.Count / (float)total, 0.42f));

            Vector3 centre = Vector3.zero;
            foreach (var p in points) centre += p;
            centre /= points.Count;

            if (points.Count <= 2)
            {
                foreach (var p in points) twigs.Add(new Twig { From = from, To = p, R0 = radius, R1 = radius * 0.8f });
                return;
            }

            // Principal direction of the points, by a few rounds of power iteration on their covariance.
            Vector3 axis = Vector3.up;
            for (int iteration = 0; iteration < 8; iteration++)
            {
                Vector3 next = Vector3.zero;
                foreach (var p in points) { Vector3 d = p - centre; next += d * Vector3.Dot(d, axis); }
                if (next.sqrMagnitude < 1e-12f) break;
                axis = next.normalized;
            }

            var above = new List<Vector3>();
            var below = new List<Vector3>();
            foreach (var p in points) (Vector3.Dot(p - centre, axis) >= 0f ? above : below).Add(p);
            if (above.Count == 0 || below.Count == 0) { foreach (var p in points) twigs.Add(new Twig { From = from, To = p, R0 = radius, R1 = radius * 0.8f }); return; }

            Vector3 fork = Vector3.Lerp(from, centre, 0.58f);
            twigs.Add(new Twig { From = from, To = fork, R0 = radius, R1 = radius * 0.9f });
            GrowToward(twigs, fork, above, total, rootRadius);
            GrowToward(twigs, fork, below, total, rootRadius);
        }

        private static void BuildBronchialTrees()
        {
            var airMat = _tissue(new Color(0.88f, 0.86f, 0.80f), 0.40f);
            var arteryMat = _tissue(new Color(0.72f, 0.10f, 0.10f), 0.45f);
            var veinMat = _tissue(new Color(0.22f, 0.32f, 0.62f), 0.45f);

            for (int s = -1; s <= 1; s += 2)
            {
                string tag = s > 0 ? "L" : "R";
                SdfFunc lung = Lung(s);
                var rng = new System.Random(s > 0 ? 71 : 37);

                // Sample points throughout the lung for the tree to reach.
                var points = new List<Vector3>();
                for (int attempt = 0; attempt < 60000 && points.Count < 300; attempt++)
                {
                    var p = V(s * Mathf.Lerp(0.034f, 0.150f, (float)rng.NextDouble()), Mathf.Lerp(1.16f, 1.47f, (float)rng.NextDouble()),
                              Mathf.Lerp(-0.11f, 0.11f, (float)rng.NextDouble()));
                    if (lung(p) < -0.0045f) points.Add(p);
                }

                // Divide them among the lobes: three on the right, two on the left.
                Vector3 hilum = s > 0 ? V(0.050f, 1.334f, 0.014f) : V(-0.040f, 1.336f, 0.010f);
                var lobes = new List<List<Vector3>> { new List<Vector3>(), new List<Vector3>(), new List<Vector3>() };
                foreach (var p in points)
                {
                    int lobe;
                    if (s > 0) lobe = p.y > 1.31f - 0.35f * (p.z + 0.02f) ? 0 : 2;                 // upper (with lingula) or lower
                    else lobe = p.y > 1.34f ? 0 : (p.y > 1.27f && p.z < -0.010f ? 1 : 2);          // upper, middle, lower
                    lobes[lobe].Add(p);
                }

                var twigs = new List<Twig>();
                foreach (var lobe in lobes)
                {
                    if (lobe.Count == 0) continue;
                    // Each lobar bronchus is drawn from the end of the main bronchus.
                    GrowToward(twigs, hilum, lobe, points.Count, 0.0058f);
                }

                var airway = new List<Mesh>();
                var arteries = new List<Mesh>();
                var veins = new List<Mesh>();
                int n = 0;
                foreach (var t in twigs)
                {
                    Vector3 axis = (t.To - t.From).normalized;
                    Vector3 side1 = Vector3.Cross(axis, Vector3.up);
                    if (side1.sqrMagnitude < 1e-4f) side1 = Vector3.right;
                    side1.Normalize();
                    Vector3 mid = (t.From + t.To) * 0.5f;
                    airway.Add(LoftMesh($"Br{tag}{n}", Loft.Taper(t.R0, t.R1), 7, 1f, null, t.From, mid, t.To));

                    // Each larger airway is accompanied by a pulmonary artery on one side and a vein on the
                    // other; the finest branches are too small to draw alongside.
                    if (t.R0 > 0.0011f)
                    {
                        Vector3 offset = side1 * (t.R0 * 1.9f);
                        arteries.Add(LoftMesh($"Pa{tag}{n}", Loft.Taper(t.R0 * 0.75f, t.R1 * 0.75f), 7, 1f, null, t.From + offset, mid + offset, t.To + offset));
                        veins.Add(LoftMesh($"Pv{tag}{n}", Loft.Taper(t.R0 * 0.75f, t.R1 * 0.75f), 7, 1f, null, t.From - offset, mid - offset, t.To - offset));
                    }
                    n++;
                }

                Place("BronchialTree_" + tag, "SYS_RESP_BRONCHI", Combine("BronchialTree_" + tag, airway), airMat);
                Place("PulmonaryArteryBranches_" + tag, "SYS_CV_PULMONARY_ARTERY", Combine("PulmonaryArteryBranches_" + tag, arteries), arteryMat);
                Place("PulmonaryVeinBranches_" + tag, "SYS_CV_PULMONARY_VEIN", Combine("PulmonaryVeinBranches_" + tag, veins), veinMat);
            }
        }

        // ---------------------------------------------------------------- throat, nose and sinuses

        private static void BuildThroatAndNose()
        {
            var mucosa = _tissue(new Color(0.82f, 0.48f, 0.46f), 0.50f);
            var air = _tissue(new Color(0.70f, 0.82f, 0.92f), 0.60f);
            var lymphoid = _tissue(new Color(0.88f, 0.60f, 0.58f), 0.45f);
            var cartilage = _tissue(new Color(0.88f, 0.86f, 0.80f), 0.40f);

            // Pharynx: the muscular funnel from the back of the nose to the top of the oesophagus, crossed by
            // the paths of air and food - the reason we can choke.
            Mesh pharynx = LoftMesh("Pharynx", t => Mathf.Lerp(0.0078f, 0.0052f, t), 14, 2.4f, Vector3.right,
                V(0f, 1.588f, 0.004f), V(0f, 1.560f, 0.008f), V(0f, 1.530f, 0.010f), V(0f, 1.505f, 0.012f), V(0f, 1.492f, 0.014f));
            Place("Pharynx", "SYS_DIG_PHARYNX", PartFactory.Save(pharynx, "Pharynx"), mucosa);

            // Nasal cavity: two tall, narrow passages from the nostrils back to the pharynx, where inhaled air
            // is warmed, moistened and filtered. The three turbinates on each outer wall stir the air and
            // multiply the surface area.
            Mesh passage = LoftMesh("NasalPassage", t => 0.0042f, 10, 2.6f, Vector3.up,
                V(0.010f, 1.622f, -0.098f), V(0.010f, 1.618f, -0.074f), V(0.011f, 1.607f, -0.044f), V(0.011f, 1.594f, -0.020f));
            PartFactory.AddPair(_root, "NasalPassage", "SYS_RESP_NASAL_CAVITY", PartFactory.Save(passage, "NasalPassage"), mucosa, _layer);

            SdfFunc turbinates = Sdf.Union(
                Sdf.Ellipsoid(V(0.0155f, 1.636f, -0.050f), V(0.0032f, 0.0042f, 0.017f)),
                Sdf.Ellipsoid(V(0.0165f, 1.625f, -0.052f), V(0.0036f, 0.0048f, 0.021f)),
                Sdf.Ellipsoid(V(0.0175f, 1.613f, -0.056f), V(0.0040f, 0.0052f, 0.024f)));
            Mesh turbinateMesh = Sculpt("Turbinates", turbinates, 0.0005f, V(0.012f, 1.606f, -0.082f), V(0.024f, 1.644f, -0.030f));
            PartFactory.AddPair(_root, "Turbinates", "SYS_RESP_TURBINATES", turbinateMesh, mucosa, _layer);

            // Paranasal sinuses: air-filled hollows in the skull that lighten it and resonate the voice.
            Mesh frontal = Sculpt("FrontalSinus", Sdf.Ellipsoid(V(0.019f, 1.690f, -0.070f), V(0.014f, 0.011f, 0.007f)), 0.0006f, V(0.019f, 1.690f, -0.070f));
            PartFactory.AddPair(_root, "FrontalSinus", "SYS_RESP_SINUS_FRONTAL", frontal, air, _layer);
            Mesh maxillary = Sculpt("MaxillarySinus", Sdf.Ellipsoid(V(0.031f, 1.606f, -0.062f), V(0.013f, 0.013f, 0.013f)), 0.0006f, V(0.031f, 1.606f, -0.062f));
            PartFactory.AddPair(_root, "MaxillarySinus", "SYS_RESP_SINUS_MAXILLARY", maxillary, air, _layer);
            SdfFunc ethmoid = Sdf.Union(
                Sdf.Sphere(V(0.012f, 1.654f, -0.064f), 0.0040f), Sdf.Sphere(V(0.014f, 1.646f, -0.056f), 0.0038f),
                Sdf.Sphere(V(0.011f, 1.660f, -0.054f), 0.0036f), Sdf.Sphere(V(0.015f, 1.652f, -0.048f), 0.0036f),
                Sdf.Sphere(V(0.010f, 1.644f, -0.066f), 0.0034f));
            Mesh ethmoidMesh = Sculpt("EthmoidSinuses", ethmoid, 0.0004f, V(0.012f, 1.652f, -0.058f));
            PartFactory.AddPair(_root, "EthmoidSinuses", "SYS_RESP_SINUS_ETHMOID", ethmoidMesh, air, _layer);
            Mesh sphenoid = Sculpt("SphenoidSinus", Sdf.Ellipsoid(V(0f, 1.600f, -0.030f), V(0.010f, 0.006f, 0.010f)), 0.0005f, V(0f, 1.600f, -0.030f));
            Place("SphenoidSinus", "SYS_RESP_SINUS_SPHENOID", sphenoid, air);

            // Tonsils: lymphoid tissue guarding the entrance of the throat - the palatine tonsils at the sides,
            // the pharyngeal tonsil (adenoids) on the roof and the lingual tonsil at the base of the tongue.
            var tonsils = new List<Mesh>
            {
                Sculpt("PalatineTonsil_L", Sdf.Ellipsoid(V(0.024f, 1.548f, -0.006f), V(0.0060f, 0.0110f, 0.0070f)), 0.0004f, V(0.024f, 1.548f, -0.006f)),
                Sculpt("PalatineTonsil_R", Sdf.Ellipsoid(V(-0.024f, 1.548f, -0.006f), V(0.0060f, 0.0110f, 0.0070f)), 0.0004f, V(-0.024f, 1.548f, -0.006f)),
                Sculpt("PharyngealTonsil", Sdf.Ellipsoid(V(0f, 1.583f, 0.004f), V(0.0080f, 0.0055f, 0.0040f)), 0.0004f, V(0f, 1.583f, 0.004f)),
                Sculpt("LingualTonsil", Sdf.Ellipsoid(V(0f, 1.535f, -0.014f), V(0.0100f, 0.0040f, 0.0060f)), 0.0004f, V(0f, 1.535f, -0.014f)),
            };
            Place("Tonsils", "SYS_LYMPH_TONSILS", Combine("Tonsils", tonsils), lymphoid);

            // Epiglottis: the leaf of cartilage that folds over the larynx when you swallow, so food goes down
            // the oesophagus rather than the windpipe.
            Mesh epiglottis = Sculpt("Epiglottis", Sdf.Ellipsoid(V(0f, 1.533f, -0.030f), V(0.0100f, 0.0130f, 0.0016f), Quaternion.Euler(-18f, 0f, 0f)),
                0.0004f, V(0f, 1.533f, -0.030f));
            Place("Epiglottis", "SYS_RESP_EPIGLOTTIS", epiglottis, cartilage);

            // Soft palate and uvula: the muscular curtain behind the bony palate that lifts to seal the nose
            // during swallowing, ending in the uvula.
            SdfFunc palate = Sdf.SmoothUnion(0.003f,
                Sdf.Ellipsoid(V(0f, 1.568f, -0.018f), V(0.020f, 0.0026f, 0.017f)),
                Sdf.RoundCone(V(0f, 1.567f, -0.004f), 0.0028f, V(0f, 1.548f, -0.003f), 0.0016f));
            Mesh palateMesh = Sculpt("SoftPalate", palate, 0.0004f, V(0f, 1.545f, -0.036f), V(0f, 1.572f, 0.004f));
            Place("SoftPalate", "SYS_DIG_PALATE", palateMesh, mucosa);
        }
    }
}
