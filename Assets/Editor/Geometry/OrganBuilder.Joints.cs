using System;
using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// The connective tissue that holds the skeleton together and lets it move: the ligaments and
    /// menisci of the knee, articular cartilage on the joint surfaces, the nuchal ligament, the pubic symphysis and the plantar fascia - and, in the
    /// middle ear, the eardrum and the three smallest bones in the body.
    /// </summary>
    public static partial class OrganBuilder
    {
        private static void BuildJointsAndEar()
        {
            BuildKneeJoint();
            BuildOtherConnectiveTissue();
            BuildMiddleEar();
        }

        private static Mesh Strap(string name, float thickness, float flatten, Vector3 thickDirection, params Vector3[] path)
        {
            var o = Loft.Options.Default;
            o.Sides = 10;
            o.Flatten = flatten;
            o.ThickAt = _ => thickDirection;
            o.RingsPerMetre = 600f;
            o.CapRings = 2;
            return Loft.Tube(name, path, Loft.Constant(thickness), o);
        }

        // ---------------------------------------------------------------- knee

        private static void BuildKneeJoint()
        {
            var ligament = _tissue(new Color(0.94f, 0.91f, 0.80f), 0.45f);
            var fibrocartilage = _tissue(new Color(0.86f, 0.82f, 0.68f), 0.40f);
            var cartilage = _tissue(new Color(0.72f, 0.86f, 0.92f), 0.65f);

            // Cruciate ligaments: two ropes crossing inside the joint, the anterior stopping the shin sliding
            // forward, the posterior stopping it sliding back. The ACL is the most often torn.
            Mesh acl = PartFactory.Save(Strap("ACL", 0.0042f, 1.4f, Vector3.right, V(0.098f, 0.488f, 0.030f), V(0.094f, 0.478f, 0.018f), V(0.090f, 0.469f, 0.006f)), "ACL");
            PartFactory.AddPair(_root, "AnteriorCruciateLigament", "SYS_SK_ACL", acl, ligament, _layer);
            Mesh pcl = PartFactory.Save(Strap("PCL", 0.0050f, 1.4f, Vector3.right, V(0.080f, 0.488f, 0.008f), V(0.083f, 0.476f, 0.024f), V(0.086f, 0.463f, 0.040f)), "PCL");
            PartFactory.AddPair(_root, "PosteriorCruciateLigament", "SYS_SK_PCL", pcl, ligament, _layer);

            // Collateral ligaments: the medial one a broad strap, the lateral a cord, stopping the knee bending sideways.
            Mesh mcl = PartFactory.Save(Strap("MCL", 0.0018f, 3.2f, Vector3.right, V(0.040f, 0.474f, 0.020f), V(0.048f, 0.446f, 0.016f), V(0.060f, 0.418f, 0.010f)), "MCL");
            PartFactory.AddPair(_root, "MedialCollateralLigament", "SYS_SK_MCL", mcl, ligament, _layer);
            Mesh lcl = PartFactory.Save(Strap("LCL", 0.0022f, 1.1f, Vector3.right, V(0.137f, 0.474f, 0.020f), V(0.130f, 0.452f, 0.024f), V(0.123f, 0.436f, 0.028f)), "LCL");
            PartFactory.AddPair(_root, "LateralCollateralLigament", "SYS_SK_LCL", lcl, ligament, _layer);

            // Menisci: two C-shaped wedges of fibrocartilage on the tibial plateau that deepen the socket for the femoral
            // condyles and absorb shock. The medial is C-shaped and firmly fixed - hence torn far more often - the lateral almost an O.
            Vector3[] Arc(Vector3 centre, float a, float b, float fromDeg, float toDeg)
            {
                var pts = new List<Vector3>();
                for (int i = 0; i <= 14; i++)
                {
                    float rad = Mathf.Lerp(fromDeg, toDeg, i / 14f) * Mathf.Deg2Rad;
                    pts.Add(centre + new Vector3(Mathf.Cos(rad) * a, 0f, Mathf.Sin(rad) * b));
                }
                return pts.ToArray();
            }
            var menisci = new List<Mesh>
            {
                Strap("MedialMeniscus", 0.0030f, 1.9f, Vector3.up, Arc(V(0.064f, 0.4725f, 0.020f), 0.0150f, 0.0250f, 55f, 305f)),
                Strap("LateralMeniscus", 0.0030f, 1.9f, Vector3.up, Arc(V(0.108f, 0.4725f, 0.020f), 0.0150f, 0.0220f, -125f, 125f)),
            };
            PartFactory.AddPair(_root, "Menisci", "SYS_SK_MENISCUS", Combine("Menisci", menisci), fibrocartilage, _layer);

            // Articular cartilage: the smooth, slippery, blood-free layer over the ends of the bones, thickest at the
            // knee - the tissue that wears away in osteoarthritis.
            SdfFunc medial = Sdf.Ellipsoid(V(0.066f, 0.470f, 0.020f), V(0.0225f, 0.0270f, 0.0310f));
            SdfFunc lateral = Sdf.Ellipsoid(V(0.110f, 0.470f, 0.020f), V(0.0245f, 0.0270f, 0.0310f));
            SdfFunc plateau = Sdf.Ellipsoid(V(0.086f, 0.458f, 0.020f), V(0.0370f, 0.0115f, 0.0300f));
            SdfFunc condyleCap = Sdf.Intersect(Sdf.Subtract(Sdf.Union(Sdf.Offset(medial, 0.0022f), Sdf.Offset(lateral, 0.0022f)), Sdf.Union(medial, lateral)),
                Sdf.HalfSpace(V(0f, 0.481f, 0f), Vector3.up));
            SdfFunc plateauCap = Sdf.Intersect(Sdf.Subtract(Sdf.Offset(plateau, 0.0018f), plateau), Sdf.HalfSpace(V(0f, 0.4685f, 0f), Vector3.down));
            Mesh knee = Sculpt("KneeCartilage", Sdf.Union(condyleCap, plateauCap), 0.0009f, V(0.040f, 0.430f, -0.020f), V(0.135f, 0.500f, 0.056f));
            PartFactory.AddPair(_root, "KneeCartilage", "SYS_SK_ARTICULAR_CARTILAGE", knee, cartilage, _layer);

            Mesh hip = Sculpt("HipCartilage", Sdf.Subtract(Sdf.Sphere(SkeletonBuilder.HipJoint, 0.0257f), Sdf.Sphere(SkeletonBuilder.HipJoint, 0.0235f)), 0.0008f, SkeletonBuilder.HipJoint);
            PartFactory.AddPair(_root, "HipCartilage", "SYS_SK_ARTICULAR_CARTILAGE", hip, cartilage, _layer);
            Mesh shoulder = Sculpt("ShoulderCartilage", Sdf.Subtract(Sdf.Sphere(SkeletonBuilder.ShoulderJoint, 0.0245f), Sdf.Sphere(SkeletonBuilder.ShoulderJoint, 0.0225f)), 0.0008f, SkeletonBuilder.ShoulderJoint);
            PartFactory.AddPair(_root, "ShoulderCartilage", "SYS_SK_ARTICULAR_CARTILAGE", shoulder, cartilage, _layer);
        }

        // ---------------------------------------------------------------- other connective tissue

        private static void BuildOtherConnectiveTissue()
        {
            var ligament = _tissue(new Color(0.94f, 0.91f, 0.80f), 0.45f);
            var fibrocartilage = _tissue(new Color(0.86f, 0.82f, 0.68f), 0.40f);

            // Nuchal ligament: a sheet in the midline of the back of the neck from the skull to the seventh cervical
            // vertebra, a "tether" that holds the head up - hugely developed in grazing animals.
            Mesh nuchal = PartFactory.Save(Strap("NuchalLigament", 0.0012f, 6f, Vector3.right,
                V(0f, 1.598f, 0.090f), V(0f, 1.552f, 0.084f), V(0f, 1.504f, 0.086f), V(0f, 1.462f, 0.092f)), "NuchalLigament");
            Place("NuchalLigament", "SYS_SK_NUCHAL_LIGAMENT", nuchal, ligament);

            // Pubic symphysis: the disc of fibrocartilage joining the two pubic bones at the front of the pelvis. It
            // softens during pregnancy so the pelvis can widen for birth.
            Mesh symphysis = Sculpt("PubicSymphysis", Sdf.Ellipsoid(V(0f, 0.834f, -0.057f), V(0.0032f, 0.014f, 0.010f)), 0.0007f, V(0f, 0.834f, -0.057f));
            Place("PubicSymphysis", "SYS_SK_PUBIC_SYMPHYSIS", symphysis, fibrocartilage);

            // Plantar fascia: the tough fan of fibrous tissue under the sole from the heel bone to the toes that holds
            // up the arch of the foot and acts as a spring when you push off.
            var strips = new List<Mesh>();
            float[] headX = { 0.064f, 0.075f, 0.086f, 0.097f, 0.107f };
            for (int i = 0; i < 5; i++)
                strips.Add(Strap($"PlantarStrip{i}", 0.0012f, 3.6f, Vector3.up,
                    V(0.084f, 0.017f, 0.054f), V(Mathf.Lerp(0.084f, headX[i], 0.5f), 0.011f, -0.030f), V(headX[i], 0.012f, -0.118f)));
            PartFactory.AddPair(_root, "PlantarFascia", "SYS_SK_PLANTAR_FASCIA", Combine("PlantarFascia", strips), ligament, _layer);
        }

        // ---------------------------------------------------------------- middle ear

        private static void BuildMiddleEar()
        {
            var bone = _tissue(new Color(0.94f, 0.90f, 0.82f), 0.55f);
            var drum = _tissue(new Color(0.84f, 0.74f, 0.68f), 0.60f);

            // Tympanic membrane (eardrum): the taut disc at the end of the ear canal that vibrates with sound.
            Mesh eardrum = Sculpt("Eardrum", Sdf.Ellipsoid(V(0.0600f, 1.6285f, 0.0100f), V(0.0005f, 0.0046f, 0.0040f), Quaternion.Euler(0f, 0f, 12f)), 0.0002f, 0.004f, V(0.060f, 1.6285f, 0.010f));
            PartFactory.AddPair(_root, "Eardrum", "SYS_SENS_EARDRUM", eardrum, drum, _layer);

            // The ossicles: malleus (hammer, fixed to the eardrum), incus (anvil) and stapes (stirrup, the smallest bone in the
            // body, about 3 mm), a lever chain that carries the eardrum's vibration to the inner ear and amplifies it about
            // twenty-fold.
            SdfFunc malleus = Sdf.SmoothUnion(0.0006f,
                Sdf.Sphere(V(0.0545f, 1.6350f, 0.0110f), 0.0017f),                                               // head
                Sdf.RoundCone(V(0.0560f, 1.6330f, 0.0105f), 0.0009f, V(0.0585f, 1.6235f, 0.0100f), 0.0004f),     // neck and handle
                Sdf.Capsule(V(0.0565f, 1.6320f, 0.0100f), V(0.0575f, 1.6335f, 0.0125f), 0.0006f));               // lateral process
            Mesh malleusMesh = Sculpt("Malleus", malleus, 0.00022f, 0.004f, V(0.0545f, 1.6280f, 0.0105f));
            PartFactory.AddPair(_root, "Malleus", "SYS_SENS_MALLEUS", malleusMesh, bone, _layer);

            SdfFunc incus = Sdf.SmoothUnion(0.0006f,
                Sdf.Ellipsoid(V(0.0520f, 1.6350f, 0.0130f), V(0.0019f, 0.0022f, 0.0017f)),                        // body
                Sdf.RoundCone(V(0.0520f, 1.6335f, 0.0135f), 0.0008f, V(0.0510f, 1.6255f, 0.0135f), 0.0005f),      // long process
                Sdf.Capsule(V(0.0520f, 1.6355f, 0.0140f), V(0.0505f, 1.6360f, 0.0160f), 0.0006f));                // short process
            Mesh incusMesh = Sculpt("Incus", incus, 0.00022f, 0.004f, V(0.0515f, 1.6300f, 0.0140f));
            PartFactory.AddPair(_root, "Incus", "SYS_SENS_INCUS", incusMesh, bone, _layer);

            SdfFunc stapes = Sdf.SmoothUnion(0.0003f,
                Sdf.Torus(V(0.0490f, 1.6245f, 0.0135f), Quaternion.Euler(0f, 0f, 90f), 0.0016f, 0.00035f),         // the stirrup's arch
                Sdf.Ellipsoid(V(0.0475f, 1.6245f, 0.0135f), V(0.0003f, 0.0010f, 0.0016f)));                        // footplate in the oval window
            Mesh stapesMesh = Sculpt("Stapes", stapes, 0.00015f, 0.004f, V(0.0490f, 1.6245f, 0.0135f));
            PartFactory.AddPair(_root, "Stapes", "SYS_SENS_STAPES", stapesMesh, bone, _layer);
        }
    }
}
