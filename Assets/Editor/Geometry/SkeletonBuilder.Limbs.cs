using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// The pelvic and shoulder girdles and the long bones of the limbs, modelled as
    /// signed distance fields and meshed. Coordinates are the figure's: metres, faces
    /// -Z, anatomical left is +X, soles at y = 0. Every bone is described for the
    /// left side and mirrored for the right.
    ///
    /// Joint centres are shared with the vessel and nerve generators, so a bone and
    /// the neurovascular bundle that runs beside it stay where they should relative to
    /// one another when either changes.
    /// </summary>
    public static partial class SkeletonBuilder
    {
        // ---------------------------------------------------------------- landmarks
        // Left side; the right is the mirror through x = 0.

        public static readonly Vector3 HipJoint = new Vector3(0.076f, 0.866f, 0.008f);
        public static readonly Vector3 KneeJoint = new Vector3(0.088f, 0.470f, 0.020f);
        public static readonly Vector3 AnkleJoint = new Vector3(0.083f, 0.078f, 0.015f);
        public static readonly Vector3 ShoulderJoint = new Vector3(0.176f, 1.386f, 0.018f);
        public static readonly Vector3 ElbowJoint = new Vector3(0.190f, 1.093f, 0.012f);
        public static readonly Vector3 WristJoint = new Vector3(0.212f, 0.840f, 0.010f);

        private static void BuildGirdlesAndLimbs(Transform root, Material bone, int layer)
        {
            BuildHipBone(root, bone, layer);
            BuildSacrumAndCoccyx(root, bone, layer);
            BuildScapula(root, bone, layer);
            BuildClavicle(root, bone, layer);
            BuildHumerus(root, bone, layer);
            BuildForearm(root, bone, layer);
            BuildFemur(root, bone, layer);
            BuildPatella(root, bone, layer);
            BuildTibia(root, bone, layer);
            BuildFibula(root, bone, layer);
        }

        // ---------------------------------------------------------------- pelvis

        private static void BuildHipBone(Transform root, Material bone, int layer)
        {
            Vector3 j = HipJoint;
            // The acetabulum faces laterally, and a little forward and down.
            Vector3 n = new Vector3(0.80f, -0.25f, -0.55f).normalized;

            // Iliac wing: a fan of thin plates from the acetabular roof out to the
            // crest. Their corners are the palpable landmarks - the two posterior spines,
            // the crest's highest point, the two anterior spines.
            var roof = new Vector3(0.090f, 0.890f, 0.004f);
            var psis = new Vector3(0.046f, 0.972f, 0.072f);
            var crestBack = new Vector3(0.078f, 1.000f, 0.054f);
            var crestMid = new Vector3(0.118f, 1.008f, -0.012f);
            var asis = new Vector3(0.140f, 0.956f, -0.066f);
            var aiis = new Vector3(0.122f, 0.906f, -0.050f);
            var piis = new Vector3(0.050f, 0.926f, 0.066f);
            var notch = new Vector3(0.064f, 0.884f, 0.040f);
            const float t = 0.0036f;

            SdfFunc wing = Sdf.SmoothUnion(0.011f,
                Sdf.TrianglePlate(roof, psis, crestBack, t),
                Sdf.TrianglePlate(roof, crestBack, crestMid, t),
                Sdf.TrianglePlate(roof, crestMid, asis, t),
                Sdf.TrianglePlate(roof, asis, aiis, t),
                Sdf.TrianglePlate(roof, piis, psis, t),
                Sdf.TrianglePlate(roof, notch, piis, t));

            // The crest is a thickened rim, and the spines are its palpable knobs.
            SdfFunc crest = Sdf.Chain(new[] { psis, crestBack, crestMid, asis },
                new[] { 0.0055f, 0.0060f, 0.0060f, 0.0055f }, 0.004f);
            SdfFunc spines = Sdf.Union(
                Sdf.Sphere(psis, 0.0075f), Sdf.Sphere(asis, 0.0080f), Sdf.Sphere(aiis, 0.0070f), Sdf.Sphere(piis, 0.0065f));

            // Body around the hip socket, sliced flat on its outer side and cupped.
            SdfFunc acetabularBody = Sdf.Subtract(Sdf.Sphere(j - n * 0.006f, 0.031f),
                Sdf.HalfSpace(j + n * 0.010f, -n));
            SdfFunc rim = Sdf.Torus(j + n * 0.004f, Quaternion.FromToRotation(Vector3.up, n), 0.0275f, 0.0058f);

            // Ischium: down and back from the socket to the ischial tuberosity - the
            // sitting bone - then forward as the ischiopubic ramus.
            var tuberosity = new Vector3(0.079f, 0.797f, 0.044f);
            SdfFunc ischium = Sdf.Union(
                Sdf.RoundCone(j - n * 0.004f + new Vector3(0f, -0.006f, 0.008f), 0.015f, tuberosity, 0.014f),
                Sdf.Sphere(tuberosity, 0.0175f));
            SdfFunc ischiopubicRamus = Sdf.Chain(
                new[] { tuberosity, new Vector3(0.052f, 0.798f, 0.012f), new Vector3(0.030f, 0.808f, -0.028f), new Vector3(0.014f, 0.824f, -0.052f) },
                new[] { 0.0125f, 0.0090f, 0.0085f, 0.0090f }, 0.004f);

            // Pubis: the superior ramus runs from the front of the socket to the
            // symphysis. Together with the ischium these enclose the obturator foramen.
            SdfFunc superiorRamus = Sdf.Chain(
                new[] { new Vector3(0.086f, 0.872f, -0.022f), new Vector3(0.052f, 0.858f, -0.046f), new Vector3(0.022f, 0.846f, -0.058f) },
                new[] { 0.0105f, 0.0095f, 0.0100f }, 0.004f);
            SdfFunc pubicBody = Sdf.Sphere(new Vector3(0.010f, 0.838f, -0.058f), 0.0125f);

            // The iliac wing is not flat: its outer (gluteal) face bulges outward in the middle and the front and back
            // edges curve toward the midline, so the wing cups the abdominal contents like a shallow bowl. Bend the
            // whole wing - plates, crest and spines together - by sampling it with an x that shifts with z squared.
            SdfFunc flatWing = Sdf.SmoothUnion(0.006f, wing, crest, spines);
            SdfFunc bentWing = q => flatWing(new Vector3(q.x + 1.7f * (q.z - 0.004f) * (q.z - 0.004f), q.y, q.z));

            SdfFunc joined = Sdf.SmoothUnion(0.008f, bentWing, acetabularBody, ischium, ischiopubicRamus, superiorRamus, pubicBody);
            joined = Sdf.Union(joined, rim);

            // Cut the socket, and stop at the midline so the two hip bones meet at the
            // symphysis instead of fusing into one.
            SdfFunc hip = Sdf.Subtract(joined, Sdf.Sphere(j, 0.0252f));
            hip = Sdf.Intersect(hip, Sdf.HalfSpace(new Vector3(0.0015f, 0f, 0f), Vector3.left));

            PartFactory.BoundsOf(out var min, out var max, 0.025f,
                psis, asis, crestMid, crestBack, tuberosity,
                new Vector3(0.002f, 0.83f, -0.07f), new Vector3(0.002f, 0.78f, 0.10f), new Vector3(0.16f, 0.78f, -0.09f));
            Mesh mesh = PartFactory.Save(SurfaceNets.Build(hip, min, max, 0.0022f, "HipBone"), "HipBone");
            PartFactory.AddPair(root, "HipBone", "SYS_SK_PELVIS", mesh, bone, layer);
        }

        private static void BuildSacrumAndCoccyx(Transform root, Material bone, int layer)
        {
            // Five fused vertebrae forming a wedge that curves forward. Built from many
            // heavily overlapping slices along the curve rather than five separate
            // ellipsoids - with five, the waist between each pair shows as a groove and
            // the bone reads as a stack of coins instead of one triangular wedge.
            const int slices = 14;
            var centers = new Vector3[5];
            var parts = new System.Collections.Generic.List<SdfFunc>();
            for (int i = 0; i < slices; i++)
            {
                float u = i / (float)(slices - 1);
                float y = Mathf.Lerp(0.964f, 0.838f, u);
                float z = Mathf.Lerp(0.064f, 0.080f, Mathf.Sin(u * Mathf.PI * 0.5f));   // curves backward as it descends
                float halfWidth = Mathf.Lerp(0.047f, 0.014f, Mathf.Pow(u, 0.85f));
                float thickness = Mathf.Lerp(0.024f, 0.013f, u);
                parts.Add(Sdf.Ellipsoid(new Vector3(0f, y, z), new Vector3(halfWidth, 0.0115f, thickness)));
            }
            for (int i = 0; i < 5; i++)
            {
                float u = i / 4f;
                centers[i] = new Vector3(0f, Mathf.Lerp(0.962f, 0.842f, u), Mathf.Lerp(0.064f, 0.080f, Mathf.Sin(u * Mathf.PI * 0.5f)));
            }

            // Alae: the upper sacrum's lateral wings; the median sacral crest behind.
            parts.Add(Sdf.MirrorX(Sdf.Ellipsoid(new Vector3(0.042f, 0.958f, 0.064f), new Vector3(0.020f, 0.017f, 0.021f))));
            parts.Add(Sdf.Chain(
                new[] { new Vector3(0f, 0.962f, 0.086f), new Vector3(0f, 0.895f, 0.099f), new Vector3(0f, 0.846f, 0.094f) },
                new[] { 0.006f, 0.005f, 0.004f }, 0.004f));
            parts.Add(Sdf.Sphere(new Vector3(0f, 0.987f, 0.056f), 0.011f)); // sacral promontory

            SdfFunc sacrum = Sdf.SmoothUnion(0.012f, parts.ToArray());

            // Four pairs of sacral foramina, the exits of the sacral nerves.
            for (int i = 0; i < 4; i++)
            {
                Vector3 c = Vector3.Lerp(centers[i], centers[i + 1], 0.5f);
                sacrum = Sdf.Subtract(sacrum, Sdf.MirrorX(Sdf.Sphere(new Vector3(0.019f - i * 0.003f, c.y, c.z - 0.019f), 0.0046f)));
            }

            PartFactory.BoundsOf(out var min, out var max, 0.02f, new Vector3(-0.075f, 0.82f, 0.04f), new Vector3(0.075f, 1.000f, 0.108f));
            PartFactory.Add(root, "Sacrum", "SYS_SK_SACRUM",
                PartFactory.Save(SurfaceNets.Build(sacrum, min, max, 0.0024f, "Sacrum"), "Sacrum"), bone, layer);

            // Coccyx: three or four small fused vertebrae, the vestigial tail.
            SdfFunc coccyx = Sdf.Chain(
                new[] { new Vector3(0f, 0.826f, 0.078f), new Vector3(0f, 0.808f, 0.075f), new Vector3(0f, 0.794f, 0.068f), new Vector3(0f, 0.786f, 0.060f) },
                new[] { 0.0095f, 0.0080f, 0.0065f, 0.0045f }, 0.003f);
            PartFactory.BoundsOf(out min, out max, 0.012f, new Vector3(-0.012f, 0.775f, 0.05f), new Vector3(0.012f, 0.835f, 0.09f));
            PartFactory.Add(root, "Coccyx", "SYS_SK_COCCYX",
                PartFactory.Save(SurfaceNets.Build(coccyx, min, max, 0.0012f, "Coccyx"), "Coccyx"), bone, layer);
        }

        // ---------------------------------------------------------------- shoulder girdle

        private static void BuildScapula(Transform root, Material bone, int layer)
        {
            // The blade is a thin triangular sheet lying on the back of the ribcage,
            // angled about 30 degrees out of the coronal plane.
            var superiorAngle = new Vector3(0.058f, 1.430f, 0.079f);
            var inferiorAngle = new Vector3(0.070f, 1.283f, 0.089f);
            var axillaryMid = new Vector3(0.120f, 1.322f, 0.060f);     // the lateral border bows outward
            var glenoidNeck = new Vector3(0.150f, 1.372f, 0.0398f);    // sampled before the blade is curved below
            const float t = 0.0028f;

            // A triangle: wide at the top, narrowing to the inferior angle.
            SdfFunc blade = Sdf.SmoothUnion(0.004f,
                Sdf.TrianglePlate(superiorAngle, inferiorAngle, axillaryMid, t),
                Sdf.TrianglePlate(superiorAngle, axillaryMid, glenoidNeck, t));
            // Thickened borders: the medial and lateral edges are where the bone is stiff.
            SdfFunc borders = Sdf.Union(
                Sdf.Capsule(superiorAngle, inferiorAngle, 0.0034f),
                Sdf.Chain(new[] { inferiorAngle, axillaryMid, glenoidNeck }, new[] { 0.0042f, 0.0046f, 0.0052f }, 0.003f),
                Sdf.Sphere(inferiorAngle, 0.0058f));
            // The blade is dished to sit on the curve of the ribcage: its lateral edge
            // swings forward around the chest.
            SdfFunc flatBlade = Sdf.SmoothUnion(0.004f, blade, borders);
            SdfFunc curvedBlade = q => flatBlade(new Vector3(q.x, q.y, q.z + 1.6f * (q.x - 0.09f) * (q.x - 0.09f)));

            // Spine of the scapula: a fin of bone standing up from the back of the blade,
            // running out and up over the shoulder as the acromion. It is a plate on
            // edge, not a rod - about 2 cm tall at its lateral end - with a thickened,
            // rounded free edge.
            var finMedial = new Vector3(0.0605f, 1.394f, 0.0815f);
            var finLateral = new Vector3(0.146f, 1.418f, 0.048f);
            var crestLateral = new Vector3(0.150f, 1.424f, 0.066f);
            var crestMedial = new Vector3(0.0625f, 1.399f, 0.0885f);   // the spine grows out of the blade: low at the root
            var acromionTop = new Vector3(0.176f, 1.431f, 0.036f);
            var acromionBottom = new Vector3(0.172f, 1.417f, 0.028f);
            SdfFunc spine = Sdf.SmoothUnion(0.005f,
                Sdf.QuadPlate(finMedial, finLateral, crestLateral, crestMedial, 0.0024f),
                Sdf.Capsule(crestMedial, crestLateral, 0.0040f),
                Sdf.QuadPlate(finLateral, crestLateral, acromionTop, acromionBottom, 0.0030f),
                Sdf.Capsule(crestLateral, acromionTop, 0.0048f),
                Sdf.Sphere(new Vector3(0.174f, 1.424f, 0.030f), 0.0070f));

            // Coracoid process: the hook projecting forward under the clavicle.
            SdfFunc coracoid = Sdf.Chain(
                new[] { new Vector3(0.150f, 1.396f, 0.030f), new Vector3(0.162f, 1.407f, 0.006f), new Vector3(0.166f, 1.397f, -0.014f) },
                new[] { 0.0075f, 0.0060f, 0.0050f }, 0.003f);

            // Glenoid fossa: the shallow socket for the humeral head.
            var glenoid = new Vector3(0.161f, 1.383f, 0.024f);
            SdfFunc glenoidBody = Sdf.Ellipsoid(glenoid, new Vector3(0.0095f, 0.020f, 0.0145f));

            SdfFunc scapula = Sdf.SmoothUnion(0.006f, curvedBlade, spine, coracoid, glenoidBody);
            scapula = Sdf.Subtract(scapula, Sdf.Sphere(glenoid + new Vector3(0.0225f, 0f, 0f), 0.0225f));

            PartFactory.BoundsOf(out var min, out var max, 0.02f, superiorAngle, inferiorAngle, acromionTop, crestMedial,
                new Vector3(0.17f, 1.40f, -0.02f));
            PartFactory.AddPair(root, "Scapula", "SYS_SK_SCAPULA",
                PartFactory.Save(SurfaceNets.Build(scapula, min, max, 0.0020f, "Scapula"), "Scapula"), bone, layer);
        }

        private static void BuildClavicle(Transform root, Material bone, int layer)
        {
            // The collarbone's double curve: convex forward medially, convex backward
            // laterally. Flared where it meets the sternum, flattened at the acromion.
            var points = new[]
            {
                new Vector3(0.016f, 1.420f, -0.028f), new Vector3(0.050f, 1.428f, -0.052f),
                new Vector3(0.095f, 1.430f, -0.048f), new Vector3(0.135f, 1.424f, -0.020f),
                new Vector3(0.170f, 1.421f, 0.020f),
            };
            var options = Loft.Options.Default;
            options.Sides = 12;
            options.Flatten = 1.15f;
            options.RingsPerMetre = 160f;

            Mesh mesh = Loft.Tube("Clavicle", points, t =>
            {
                const float shaft = 0.0062f;
                float medialFlare = Mathf.Lerp(0.0100f, shaft, Mathf.Clamp01(t / 0.22f));
                float lateralFlare = Mathf.Lerp(0.0075f, shaft, Mathf.Clamp01((1f - t) / 0.18f));
                return Mathf.Max(medialFlare, lateralFlare);
            }, options);

            PartFactory.AddPair(root, "Clavicle", "SYS_SK_CLAVICLE", PartFactory.Save(mesh, "Clavicle"), bone, layer);
        }

        // ---------------------------------------------------------------- arm

        private static void BuildHumerus(Transform root, Material bone, int layer)
        {
            var head = ShoulderJoint;
            SdfFunc humerus = Sdf.SmoothUnion(0.010f,
                Sdf.Sphere(head, 0.0225f),                                        // head
                Sdf.Sphere(head + new Vector3(0.024f, 0.004f, -0.004f), 0.0150f), // greater tubercle
                Sdf.Sphere(head + new Vector3(0.010f, -0.002f, -0.014f), 0.0100f),// lesser tubercle
                Sdf.RoundCone(head + new Vector3(0.006f, -0.020f, 0f), 0.0170f,    // neck and shaft
                    new Vector3(0.190f, 1.135f, 0.012f), 0.0125f),
                Sdf.Ellipsoid(new Vector3(0.190f, 1.100f, 0.010f), new Vector3(0.024f, 0.015f, 0.013f)), // trochlea + capitulum
                Sdf.Sphere(new Vector3(0.168f, 1.100f, 0.012f), 0.0110f),         // medial epicondyle
                Sdf.Sphere(new Vector3(0.211f, 1.104f, 0.012f), 0.0090f));        // lateral epicondyle
            // Olecranon fossa: the dimple at the back of the elbow end.
            humerus = Sdf.Subtract(humerus, Sdf.Sphere(new Vector3(0.190f, 1.113f, 0.026f), 0.0085f));

            PartFactory.BoundsOf(out var min, out var max, 0.03f, head, ElbowJoint, new Vector3(0.215f, 1.09f, 0.03f), new Vector3(0.16f, 1.41f, -0.02f));
            PartFactory.AddPair(root, "Humerus", "SYS_SK_HUMERUS",
                PartFactory.Save(SurfaceNets.Build(humerus, min, max, 0.0024f, "Humerus"), "Humerus"), bone, layer);
        }

        private static void BuildForearm(Transform root, Material bone, int layer)
        {
            // Radius: lateral, thumb side, small at the elbow and broad at the wrist.
            SdfFunc radius = Sdf.SmoothUnion(0.006f,
                Sdf.Ellipsoid(new Vector3(0.205f, 1.083f, 0.012f), new Vector3(0.0105f, 0.0048f, 0.0105f)), // head
                Sdf.Chain(new[] { new Vector3(0.207f, 1.070f, 0.012f), new Vector3(0.214f, 0.930f, 0.004f), new Vector3(0.222f, 0.862f, 0.006f) },
                    new[] { 0.0078f, 0.0088f, 0.0115f }, 0.004f),
                Sdf.Ellipsoid(new Vector3(0.222f, 0.850f, 0.006f), new Vector3(0.0155f, 0.0110f, 0.0115f)),   // distal end
                Sdf.Sphere(new Vector3(0.231f, 0.836f, 0.004f), 0.0055f));                                    // styloid process

            // Ulna: medial, the long one at the elbow, with the olecranon behind.
            SdfFunc ulna = Sdf.SmoothUnion(0.006f,
                Sdf.Ellipsoid(new Vector3(0.187f, 1.090f, 0.024f), new Vector3(0.0110f, 0.0145f, 0.0100f)),  // olecranon
                Sdf.Sphere(new Vector3(0.190f, 1.082f, 0.012f), 0.0090f),                                    // coronoid
                Sdf.Chain(new[] { new Vector3(0.191f, 1.070f, 0.016f), new Vector3(0.195f, 0.950f, 0.012f), new Vector3(0.198f, 0.860f, 0.008f) },
                    new[] { 0.0098f, 0.0074f, 0.0068f }, 0.004f),
                Sdf.Sphere(new Vector3(0.198f, 0.852f, 0.008f), 0.0085f),                                    // head
                Sdf.Sphere(new Vector3(0.196f, 0.838f, 0.014f), 0.0042f));                                   // styloid

            PartFactory.BoundsOf(out var min, out var max, 0.02f, new Vector3(0.18f, 0.82f, -0.005f), new Vector3(0.24f, 1.11f, 0.04f));
            PartFactory.AddPair(root, "Radius", "SYS_SK_RADIUS",
                PartFactory.Save(SurfaceNets.Build(radius, min, max, 0.0018f, "Radius"), "Radius"), bone, layer);
            PartFactory.AddPair(root, "Ulna", "SYS_SK_ULNA",
                PartFactory.Save(SurfaceNets.Build(ulna, min, max, 0.0018f, "Ulna"), "Ulna"), bone, layer);
        }

        // ---------------------------------------------------------------- leg

        private static void BuildFemur(Transform root, Material bone, int layer)
        {
            Vector3 head = HipJoint;
            // Slight forward bow of the shaft: the femur is convex anteriorly.
            SdfFunc femur = Sdf.SmoothUnion(0.012f,
                Sdf.Sphere(head, 0.0235f),                                           // head
                Sdf.RoundCone(head + new Vector3(0.006f, -0.004f, 0f), 0.0165f,      // neck
                    new Vector3(0.100f, 0.842f, 0.008f), 0.0175f),
                Sdf.Sphere(new Vector3(0.112f, 0.846f, 0.012f), 0.0195f),            // greater trochanter
                Sdf.Sphere(new Vector3(0.086f, 0.804f, 0.002f), 0.0085f),            // lesser trochanter
                Sdf.Chain(new[] { new Vector3(0.099f, 0.830f, 0.010f), new Vector3(0.093f, 0.650f, 0.000f), new Vector3(0.089f, 0.510f, 0.014f) },
                    new[] { 0.0178f, 0.0148f, 0.0165f }, 0.010f),                    // shaft
                Sdf.Ellipsoid(new Vector3(0.066f, 0.470f, 0.020f), new Vector3(0.0225f, 0.0270f, 0.0310f)),  // medial condyle
                Sdf.Ellipsoid(new Vector3(0.110f, 0.470f, 0.020f), new Vector3(0.0245f, 0.0270f, 0.0310f)),  // lateral condyle
                Sdf.Sphere(new Vector3(0.088f, 0.486f, -0.010f), 0.0205f));          // patellar surface

            PartFactory.BoundsOf(out var min, out var max, 0.03f, head, new Vector3(0.14f, 0.44f, 0.05f), new Vector3(0.04f, 0.87f, -0.02f));
            PartFactory.AddPair(root, "Femur", "SYS_SK_FEMUR",
                PartFactory.Save(SurfaceNets.Build(femur, min, max, 0.0027f, "Femur"), "Femur"), bone, layer);
        }

        private static void BuildPatella(Transform root, Material bone, int layer)
        {
            SdfFunc patella = Sdf.Ellipsoid(new Vector3(0.088f, 0.474f, -0.040f), new Vector3(0.0225f, 0.0260f, 0.0115f),
                Quaternion.Euler(8f, 0f, 0f));
            PartFactory.BoundsOf(out var min, out var max, 0.01f, new Vector3(0.06f, 0.44f, -0.06f), new Vector3(0.115f, 0.51f, -0.02f));
            PartFactory.AddPair(root, "Patella", "SYS_SK_PATELLA",
                PartFactory.Save(SurfaceNets.Build(patella, min, max, 0.0012f, "Patella"), "Patella"), bone, layer);
        }

        private static void BuildTibia(Transform root, Material bone, int layer)
        {
            SdfFunc tibia = Sdf.SmoothUnion(0.008f,
                Sdf.Ellipsoid(new Vector3(0.086f, 0.458f, 0.020f), new Vector3(0.0370f, 0.0115f, 0.0300f)),   // tibial plateau
                Sdf.Sphere(new Vector3(0.084f, 0.430f, -0.008f), 0.0105f),                                   // tibial tuberosity
                Sdf.RoundCone(new Vector3(0.085f, 0.440f, 0.018f), 0.0180f, new Vector3(0.083f, 0.100f, 0.014f), 0.0128f), // shaft
                Sdf.Capsule(new Vector3(0.084f, 0.420f, -0.004f), new Vector3(0.083f, 0.110f, 0.000f), 0.0090f),          // anterior crest
                Sdf.Ellipsoid(new Vector3(0.084f, 0.082f, 0.012f), new Vector3(0.0225f, 0.0125f, 0.0200f)),  // distal end
                Sdf.RoundCone(new Vector3(0.073f, 0.092f, 0.010f), 0.0125f, new Vector3(0.070f, 0.060f, 0.010f), 0.0080f)); // medial malleolus

            PartFactory.BoundsOf(out var min, out var max, 0.03f, new Vector3(0.04f, 0.04f, -0.03f), new Vector3(0.13f, 0.49f, 0.06f));
            PartFactory.AddPair(root, "Tibia", "SYS_SK_TIBIA",
                PartFactory.Save(SurfaceNets.Build(tibia, min, max, 0.0027f, "Tibia"), "Tibia"), bone, layer);
        }

        private static void BuildFibula(Transform root, Material bone, int layer)
        {
            SdfFunc fibula = Sdf.SmoothUnion(0.006f,
                Sdf.Sphere(new Vector3(0.114f, 0.448f, 0.030f), 0.0105f),                                   // head
                Sdf.Chain(new[] { new Vector3(0.113f, 0.430f, 0.028f), new Vector3(0.108f, 0.250f, 0.022f), new Vector3(0.104f, 0.100f, 0.016f) },
                    new[] { 0.0075f, 0.0058f, 0.0066f }, 0.004f),
                Sdf.RoundCone(new Vector3(0.104f, 0.098f, 0.016f), 0.0075f, new Vector3(0.102f, 0.060f, 0.014f), 0.0095f)); // lateral malleolus

            PartFactory.BoundsOf(out var min, out var max, 0.02f, new Vector3(0.09f, 0.04f, 0.0f), new Vector3(0.13f, 0.47f, 0.05f));
            PartFactory.AddPair(root, "Fibula", "SYS_SK_FIBULA",
                PartFactory.Save(SurfaceNets.Build(fibula, min, max, 0.0020f, "Fibula"), "Fibula"), bone, layer);
        }
    }
}
