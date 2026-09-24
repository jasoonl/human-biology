using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// The organs, sculpted. Modelled as signed distance fields so they get the shapes real
    /// organs have - a lung's tapering apex, broad base moulded over the diaphragm, its
    /// fissures and the notch the heart makes in the left one - instead of being ellipsoids.
    /// This part covers the chest and neck: lungs, heart (four separate chambers), airway,
    /// oesophagus, larynx, thyroid, thymus and diaphragm.
    ///
    /// Coordinates are the figure's (metres, faces -Z, anatomical left is +X). The ribcage's
    /// inner width is about 0.10 at the fourth rib and 0.13 at the eighth, which bounds the lungs.
    /// </summary>
    public static partial class OrganBuilder
    {
        private static Transform _root;
        private static int _layer;
        private static readonly Dictionary<(Color, float), Material> Cache = new Dictionary<(Color, float), Material>();

        /// <summary>Wet, slightly glossy tissue in the given colour. Materials are shared by colour.</summary>
        private static Material _tissue(Color colour, float gloss)
        {
            if (Cache.TryGetValue((colour, gloss), out var m)) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = colour, name = "Tissue" };
            m.SetFloat("_Smoothness", gloss);
            Cache[(colour, gloss)] = m;
            return m;
        }

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        private static Mesh Make(string name, SdfFunc shape, Vector3 min, Vector3 max, float voxel) =>
            PartFactory.Save(SurfaceNets.Build(shape, min, max, voxel, name, 4f), name);

        private static void Place(string name, string id, Mesh mesh, Material material) =>
            PartFactory.Add(_root, name, id, mesh, material, _layer);

        public static void Build(Transform root, int layer)
        {
            _root = root; _layer = layer;
            Cache.Clear();

            BuildDiaphragm();
            BuildHeart();
            BuildLungs();
            BuildAirway();
            BuildNeckOrgans();
            BuildAbdomen();
            BuildBrainAndHead();
        }

        // ---------------------------------------------------------------- diaphragm

        /// <summary>The two domes of the diaphragm as a thin muscular sheet, the right higher than
        /// the left because the liver pushes it up. Lung bases are moulded over the same shape.</summary>
        private static SdfFunc DiaphragmDomes()
        {
            SdfFunc right = Sdf.Ellipsoid(V(-0.062f, 1.130f, -0.004f), V(0.078f, 0.118f, 0.088f));
            SdfFunc left = Sdf.Ellipsoid(V(0.062f, 1.118f, -0.004f), V(0.078f, 0.104f, 0.088f));
            return Sdf.SmoothUnion(0.022f, right, left);
        }

        private static void BuildDiaphragm()
        {
            SdfFunc domes = DiaphragmDomes();
            SdfFunc shell = Sdf.Subtract(domes, Sdf.Offset(domes, -0.010f));
            // Only the dome above the diaphragm's line of attachment, which is not level: it clings to
            // the xiphoid and lower costal margin at the front and drops to the twelfth rib and the
            // lumbar spine behind. Cutting it level would leave the shell standing over the liver
            // like a bowl and hide the liver's front.
            shell = Sdf.Intersect(shell, p => (1.120f - 0.60f * p.z) - p.y);

            Mesh mesh = Make("Diaphragm", shell, V(-0.16f, 1.04f, -0.12f), V(0.16f, 1.26f, 0.12f), 0.0014f);
            Place("Diaphragm", "SYS_RESP_DIAPHRAGM", mesh, _tissue(MuscleTint, 0.30f));
        }

        private static readonly Color MuscleTint = new Color(0.66f, 0.26f, 0.22f);

        // ---------------------------------------------------------------- lungs

        private static SdfFunc Lung(float side)
        {
            // Narrow at the apex under the collarbone, broad at the base where it rests on the
            // diaphragm: two blended masses rather than one ellipsoid, which would narrow toward
            // the base and leave the lung inside the ribs' width at the bottom.
            SdfFunc upper = Sdf.Ellipsoid(V(side * 0.048f, 1.385f, 0.000f), V(0.034f, 0.074f, 0.060f));
            SdfFunc lower = Sdf.Ellipsoid(V(side * 0.066f, 1.272f, 0.004f), V(0.056f, 0.084f, 0.082f));
            SdfFunc lung = Sdf.SmoothUnion(0.030f, upper, lower);

            // Flat medial face, where the mediastinum (heart, great vessels, airway) lies.
            lung = Sdf.Intersect(lung, Sdf.HalfSpace(V(side * 0.032f, 0f, 0f), V(-side, 0f, 0f)));
            // The heart presses a cardiac impression into each lung - deeply into the left, whose
            // notch this replaces - so the two never share the same space.
            lung = Sdf.SmoothSubtract(0.008f, lung, Sdf.Offset(HeartField, 0.006f));

            // The base is concave, moulded over the dome of the diaphragm.
            lung = Sdf.Subtract(lung, Sdf.Offset(DiaphragmDomes(), 0.002f));

            // Hilum: the dimple on the medial face where the bronchus and vessels enter.
            lung = Sdf.SmoothSubtract(0.008f, lung, Sdf.Sphere(V(side * 0.034f, 1.340f, 0.014f), 0.014f));

            if (side > 0f)
            {
                // Left lung: the cardiac notch the heart presses into its lower front.
                lung = Sdf.SmoothSubtract(0.012f, lung, Sdf.Ellipsoid(V(0.034f, 1.262f, -0.052f), V(0.042f, 0.052f, 0.046f)));
            }

            // Fissures: shallow grooves dividing the lobes. Oblique fissure on both, running
            // down and forward from about T4 behind to the sixth rib in front; the right lung
            // also has a horizontal fissure at the fourth rib.
            var obliqueNormal = V(0f, 0.586f, -0.811f);
            SdfFunc oblique = Sdf.Intersect(
                p => Mathf.Abs(Vector3.Dot(p - V(0f, 1.32f, 0.02f), obliqueNormal)) - 0.0012f,
                Sdf.HalfSpace(V(side * 0.036f, 0f, 0f), V(side, 0f, 0f)));
            lung = Sdf.Subtract(lung, oblique);

            if (side < 0f)
            {
                SdfFunc horizontal = Sdf.Intersect(
                    p => Mathf.Abs(p.y - 1.320f) - 0.0012f,
                    Sdf.HalfSpace(V(-0.040f, 0f, 0f), V(1f, 0f, 0f)), InFrontOf(0.0f));
                lung = Sdf.Subtract(lung, horizontal);
            }

            return lung;
        }

        private static void BuildLungs()
        {
            var mat = _tissue(new Color(0.84f, 0.62f, 0.60f), 0.35f);
            Place("Lung_R", "SYS_RESP_LUNG_R", Make("Lung_R", Lung(-1f), V(-0.14f, 1.16f, -0.11f), V(0.005f, 1.48f, 0.11f), 0.0022f), mat);
            Place("Lung_L", "SYS_RESP_LUNG_L", Make("Lung_L", Lung(1f), V(-0.005f, 1.16f, -0.11f), V(0.14f, 1.48f, 0.11f), 0.0022f), mat);
        }

        // ---------------------------------------------------------------- heart

        /// <summary>The four heart chambers as one field. Lungs are moulded around it, and the
        /// coronary vessels are snapped onto it.</summary>
        public static SdfFunc HeartField { get; private set; }

        private static void BuildHeart()
        {
            var muscle = _tissue(new Color(0.66f, 0.17f, 0.15f), 0.38f);
            var atria = _tissue(new Color(0.60f, 0.20f, 0.18f), 0.38f);

            // Four chambers, each its own mesh so each can be selected. Two-thirds of the heart
            // lies left of the midline, tilted with its apex pointing down, forward and to the left.
            SdfFunc lv = Sdf.Ellipsoid(V(0.036f, 1.252f, -0.038f), V(0.037f, 0.060f, 0.036f), Quaternion.Euler(0f, 0f, 30f));
            SdfFunc rv = Sdf.Ellipsoid(V(0.004f, 1.262f, -0.060f), V(0.040f, 0.052f, 0.030f), Quaternion.Euler(0f, 0f, 22f));
            SdfFunc ra = Sdf.SmoothUnion(0.006f,
                Sdf.Ellipsoid(V(-0.030f, 1.290f, -0.040f), V(0.026f, 0.036f, 0.026f)),
                Sdf.Ellipsoid(V(-0.014f, 1.322f, -0.056f), V(0.014f, 0.012f, 0.012f)));       // right auricle
            SdfFunc la = Sdf.SmoothUnion(0.006f,
                Sdf.Ellipsoid(V(0.018f, 1.318f, -0.010f), V(0.032f, 0.020f, 0.024f)),
                Sdf.Ellipsoid(V(0.044f, 1.322f, -0.040f), V(0.012f, 0.012f, 0.012f)));        // left auricle

            // Each chamber gives way a little to its neighbours so they sit against each other
            // with a groove between them, rather than interpenetrating.
            HeartField = Sdf.SmoothUnion(0.010f, lv, rv, ra, la);
            const float groove = 0.0016f;
            SdfFunc lvOnly = Sdf.Subtract(lv, Sdf.Offset(rv, groove));
            SdfFunc rvOnly = rv;
            SdfFunc laOnly = Sdf.Subtract(la, Sdf.Offset(lv, groove), Sdf.Offset(rv, groove));
            SdfFunc raOnly = Sdf.Subtract(ra, Sdf.Offset(rv, groove), Sdf.Offset(la, groove));

            var min = V(-0.075f, 1.17f, -0.105f);
            var max = V(0.095f, 1.35f, 0.030f);

            // The pericardium: the fibrous sac the heart lies in, drawn as a translucent envelope.
            // It carries the whole-heart id, so "Heart" remains something you can point at and be
            // quizzed on, while the chambers inside it are individually selectable through it.
            SdfFunc sac = Sdf.Offset(Sdf.SmoothUnion(0.010f, lv, rv, ra, la), 0.0045f);
            var sacMat = new Material(Shader.Find("HumanBodyExplorer/SkinShell")) { name = "Pericardium" };
            sacMat.SetColor("_BaseColor", new Color(0.86f, 0.40f, 0.36f, 0.30f));
            sacMat.SetFloat("_CenterAlpha", 0.07f);
            sacMat.SetFloat("_EdgeAlpha", 0.55f);
            Place("Heart", "SYS_CV_HEART", Make("HeartSac", sac, min, max, 0.0016f), sacMat);
            Place("Heart_LeftVentricle", "SYS_CV_HEART_LV", Make("Heart_LV", lvOnly, min, max, 0.0013f), muscle);
            Place("Heart_RightVentricle", "SYS_CV_HEART_RV", Make("Heart_RV", rvOnly, min, max, 0.0013f), muscle);
            Place("Heart_LeftAtrium", "SYS_CV_HEART_LA", Make("Heart_LA", laOnly, min, max, 0.0013f), atria);
            Place("Heart_RightAtrium", "SYS_CV_HEART_RA", Make("Heart_RA", raOnly, min, max, 0.0013f), atria);
        }

        // ---------------------------------------------------------------- airway and gullet

        private static void BuildAirway()
        {
            var cartilageMat = _tissue(new Color(0.86f, 0.84f, 0.78f), 0.40f);
            var wall = _tissue(new Color(0.80f, 0.60f, 0.56f), 0.35f);

            var opts = Loft.Options.Default;
            opts.Sides = 14;
            opts.RingsPerMetre = 400f;   // enough rings to show the cartilage as ridges

            // Trachea: about 12 cm long, held open by C-shaped rings of cartilage - which show
            // as a corrugated surface.
            var path = new[] { V(0f, 1.498f, -0.020f), V(0f, 1.450f, -0.014f), V(0f, 1.400f, -0.002f), V(0f, 1.372f, 0.010f) };
            Mesh trachea = PartFactory.Save(Loft.Tube("Trachea", path, t => 0.0095f * (1f + 0.07f * Mathf.Cos(t * Mathf.PI * 2f * 16f)), opts), "Trachea");
            Place("Trachea", "SYS_RESP_TRACHEA", trachea, cartilageMat);

            // Main bronchi: the right is wider, shorter and steeper, which is why inhaled objects
            // more often end up in the right lung.
            var bronchOpts = opts; bronchOpts.Sides = 12;
            Mesh right = PartFactory.Save(Loft.Tube("Bronchus_R", new[] { V(0f, 1.374f, 0.010f), V(-0.022f, 1.354f, 0.012f), V(-0.040f, 1.336f, 0.010f) },
                t => 0.0078f * (1f + 0.07f * Mathf.Cos(t * Mathf.PI * 2f * 5f)), bronchOpts), "Bronchus_R");
            Mesh left = PartFactory.Save(Loft.Tube("Bronchus_L", new[] { V(0f, 1.374f, 0.010f), V(0.026f, 1.352f, 0.014f), V(0.050f, 1.334f, 0.014f) },
                t => 0.0068f * (1f + 0.07f * Mathf.Cos(t * Mathf.PI * 2f * 7f)), bronchOpts), "Bronchus_L");
            Place("Bronchus_R", "SYS_RESP_BRONCHI", right, cartilageMat);
            Place("Bronchus_L", "SYS_RESP_BRONCHI", left, cartilageMat);

            // Oesophagus: behind the trachea, down the front of the spine, through the
            // diaphragm to the stomach. A muscular tube, flattened front to back.
            var gullet = new[]
            {
                V(0f, 1.500f, 0.014f), V(0f, 1.440f, 0.024f), V(-0.002f, 1.360f, 0.032f), V(-0.002f, 1.300f, 0.034f),
                V(0.004f, 1.240f, 0.028f), V(0.014f, 1.200f, 0.010f), V(0.030f, 1.184f, -0.012f),
            };
            var gOpts = Loft.Options.Default;
            gOpts.Sides = 12; gOpts.Flatten = 1.35f; gOpts.ThickAt = p => Vector3.forward; gOpts.RingsPerMetre = 120f;
            Mesh esophagus = PartFactory.Save(Loft.Tube("Esophagus", gullet, Loft.Constant(0.0088f), gOpts), "Esophagus");
            Place("Esophagus", "SYS_DIG_ESOPHAGUS", esophagus, wall);
        }

        private static void BuildNeckOrgans()
        {
            var cartilage = _tissue(new Color(0.88f, 0.86f, 0.80f), 0.40f);

            // Larynx: the shield-shaped thyroid cartilage (the Adam's apple) over the signet-shaped
            // cricoid ring, under the hyoid bone.
            SdfFunc shield = Sdf.MirrorX(Sdf.SmoothUnion(0.003f,
                Sdf.QuadPlate(V(0.001f, 1.542f, -0.041f), V(0.026f, 1.540f, -0.018f), V(0.026f, 1.510f, -0.018f), V(0.001f, 1.508f, -0.043f), 0.0018f),
                Sdf.Capsule(V(0.026f, 1.540f, -0.018f), V(0.024f, 1.556f, -0.012f), 0.0022f),   // superior horn
                Sdf.Capsule(V(0.026f, 1.510f, -0.018f), V(0.020f, 1.500f, -0.010f), 0.0022f))); // inferior horn
            SdfFunc cricoid = Sdf.SmoothUnion(0.003f,
                Sdf.Torus(V(0f, 1.498f, -0.014f), Quaternion.identity, 0.0105f, 0.0035f),
                Sdf.Ellipsoid(V(0f, 1.499f, -0.005f), V(0.008f, 0.011f, 0.004f)));
            SdfFunc larynx = Sdf.SmoothUnion(0.003f, shield, cricoid);
            Place("Larynx", "SYS_RESP_LARYNX", Make("Larynx", larynx, V(-0.04f, 1.485f, -0.06f), V(0.04f, 1.565f, 0.01f), 0.0007f), cartilage);

            // Thyroid: two lobes either side of the trachea joined by an isthmus in front of it.
            SdfFunc thyroid = Sdf.SmoothUnion(0.004f,
                Sdf.MirrorX(Sdf.Ellipsoid(V(0.021f, 1.482f, -0.028f), V(0.010f, 0.023f, 0.010f))),
                Sdf.Ellipsoid(V(0f, 1.478f, -0.033f), V(0.014f, 0.004f, 0.004f)));
            Place("Thyroid", "SYS_ENDO_THYROID", Make("Thyroid", thyroid, V(-0.04f, 1.44f, -0.06f), V(0.04f, 1.53f, 0.0f), 0.0009f),
                _tissue(new Color(0.66f, 0.32f, 0.28f), 0.40f));

            // Thymus: two lobes behind the upper sternum - large in children, fatty and shrunken in adults.
            SdfFunc thymus = Sdf.MirrorX(Sdf.Ellipsoid(V(0.018f, 1.372f, -0.052f), V(0.016f, 0.034f, 0.009f)));
            Place("Thymus", "SYS_LYMPH_THYMUS", Make("Thymus", thymus, V(-0.05f, 1.32f, -0.08f), V(0.05f, 1.43f, -0.02f), 0.0012f),
                _tissue(new Color(0.86f, 0.74f, 0.68f), 0.40f));
        }

        // helpers reused by later partial files
        private static SdfFunc InFrontOf(float z) => Sdf.HalfSpace(V(0f, 0f, z), Vector3.forward);
        private static SdfFunc Behind(float z) => Sdf.HalfSpace(V(0f, 0f, z), Vector3.back);
    }
}
