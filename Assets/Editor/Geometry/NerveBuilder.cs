using System.Collections.Generic;
using UnityEngine;
using static HumanBodyExplorer.EditorTools.Geometry.TubeKit;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// The nervous system outside the brain: the spinal cord with its enlargements and the cauda
    /// equina, all 31 pairs of spinal nerves with their dorsal root ganglia, the four plexuses and the
    /// named nerves that leave them, the intercostal nerves under each rib, the twelve cranial nerves
    /// and the autonomic chain. Nerves run with the vessels they share a bundle with - sharing the
    /// same landmark paths - so the two never disagree about where a limb's structures are.
    /// </summary>
    public static class NerveBuilder
    {
        private static TubeKit _n;

        public static int Build(Transform root, Material nerve, int layer)
        {
            _n = new TubeKit(root, nerve, layer);
            SpinalCord();
            SpinalNerves();
            Plexuses();
            ArmNerves();
            TrunkNerves();
            LegNerves();
            CranialNerves();
            Autonomic();
            return _n.Count;
        }

        // ---------------------------------------------------------------- spinal cord

        /// <summary>Height of the spinal cord's own segment for each spinal level. The cord ends at
        /// about L1-L2 while the column goes on to the sacrum, so the lower segments lie well above
        /// their vertebrae and their roots run a long way down to their exits.</summary>
        private static float CordY(char region, int i)
        {
            switch (region)
            {
                case 'C': return Mathf.Lerp(1.555f, 1.430f, i / 7f);
                case 'T': return Mathf.Lerp(1.418f, 1.225f, i / 11f);
                case 'L': return Mathf.Lerp(1.205f, 1.150f, i / 4f);
                default: return Mathf.Lerp(1.135f, 1.100f, Mathf.Min(i, 4) / 4f);
            }
        }

        /// <summary>The vertebral canal's centre, continued through the sacrum.</summary>
        private static Vector3 Canal(float y)
        {
            if (y >= 0.962f) return SkeletonBuilder.SpineCanal(y);
            float u = Mathf.InverseLerp(0.962f, 0.842f, y);
            return V(0f, y, Mathf.Lerp(0.073f, 0.088f, Mathf.Sin(u * Mathf.PI * 0.5f)));
        }

        private static void SpinalCord()
        {
            const float top = 1.575f, conus = 1.105f, end = 1.082f;
            var pts = new List<Vector3>();
            for (float y = top; y >= end - 0.001f; y -= 0.02f) pts.Add(Canal(y));
            pts.Add(Canal(end));

            _n.Add("SpinalCord", "SYS_NERV_SPINALCORD", t =>
            {
                float y = Mathf.Lerp(top, end, t);
                float r = 0.0050f;
                r += 0.0020f * Bump(y, 1.470f, 0.045f);   // cervical enlargement: the arm's nerves
                r += 0.0016f * Bump(y, 1.170f, 0.035f);   // lumbar enlargement: the leg's nerves
                r *= Mathf.Lerp(1f, 0.25f, Mathf.InverseLerp(conus, end, y) * Mathf.InverseLerp(conus, end, y)); // conus medullaris
                return r;
            }, false, pts.ToArray());

            // Filum terminale and the cauda equina: below the cord's end the lumbar and sacral roots
            // stream down the canal like a horse's tail, each to its own exit.
            var filum = new List<Vector3>();
            for (float y = end; y >= 0.86f; y -= 0.03f) filum.Add(Canal(y));
            _n.Add("FilumTerminale", "SYS_NERV_CAUDA_EQUINA", 0.0006f, 0.0004f, false, filum.ToArray());

            for (int i = 0; i < 10; i++)
            {
                int level = i; // L1..L5, S1..S5
                float exitY = level < 5 ? SkeletonBuilder.LumbarY(level) - 0.012f : Mathf.Lerp(0.947f, 0.842f, (level - 5) / 4f);
                float dx = 0.0022f + level * 0.0009f;
                float zc = Canal(exitY).z;
                _n.Add($"CaudaEquina_{level + 1}", "SYS_NERV_CAUDA_EQUINA", 0.0009f, 0.0009f, true,
                    V(0.0018f, end + 0.002f, Canal(end).z), V(dx, exitY + 0.045f, Canal(exitY + 0.045f).z + 0.0005f * level),
                    V(dx + 0.002f, exitY + 0.010f, zc), V(0.018f, exitY, zc - 0.001f));
            }
        }

        private static float Bump(float y, float centre, float half) => Mathf.Exp(-Mathf.Pow((y - centre) / half, 2f));

        // ---------------------------------------------------------------- spinal nerves

        private static Vector3 Exit(char region, int i, out float exitY)
        {
            switch (region)
            {
                case 'C': exitY = i <= 6 ? SkeletonBuilder.CervicalY(i) - 0.004f : 1.443f; break;
                case 'T': exitY = SkeletonBuilder.ThoracicY(i) - 0.011f; break;
                case 'L': exitY = SkeletonBuilder.LumbarY(i) - 0.012f; break;
                default: exitY = Mathf.Lerp(0.947f, 0.847f, Mathf.Min(i, 4) / 4f); break;
            }
            float x = region == 'S' ? Mathf.Lerp(0.019f, 0.010f, Mathf.Min(i, 4) / 4f) : (region == 'C' ? 0.022f : 0.025f);
            return V(x, exitY, Canal(exitY).z);
        }

        private static float RootRadius(float t) =>
            0.0010f + 0.0017f * Mathf.Exp(-Mathf.Pow((t - 0.5f) / 0.09f, 2f)) + 0.0008f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, t));

        private static void SpinalNerves()
        {
            var groups = new (char region, int count, string id, string name)[]
            {
                ('C', 8, "SYS_NERV_SPINAL_CERVICAL", "C"), ('T', 12, "SYS_NERV_SPINAL_THORACIC", "T"),
                ('L', 5, "SYS_NERV_SPINAL_LUMBAR", "L"), ('S', 5, "SYS_NERV_SPINAL_SACRAL", "S"),
            };
            foreach (var g in groups)
            {
                for (int i = 0; i < g.count; i++)
                {
                    Vector3 exit = Exit(g.region, i, out float exitY);
                    float cordY = CordY(g.region, i);
                    Vector3 cord = Canal(cordY);
                    // Sacral nerves leave forwards through the sacral foramina; the rest sideways and a little forwards.
                    Vector3 outside = g.region == 'S'
                        ? V(exit.x + 0.006f, exitY - 0.002f, exit.z - 0.026f)
                        : V(exit.x + 0.012f, exitY - 0.006f, exit.z - 0.008f);
                    var path = new List<Vector3> { V(0.004f, cordY, cord.z) };
                    if (Mathf.Abs(exitY - cordY) > 0.03f) path.Add(V(0.006f, Mathf.Lerp(cordY, exitY, 0.55f), Canal(Mathf.Lerp(cordY, exitY, 0.55f)).z));
                    path.Add(V(exit.x - 0.004f, exitY + 0.002f, exit.z));
                    path.Add(exit);
                    path.Add(outside);
                    _n.Add($"SpinalNerve_{g.name}{i + 1}", g.id, RootRadius, true, path.ToArray());
                }
            }

            // Coccygeal nerve: the last, vestigial pair.
            _n.Add("SpinalNerve_Co1", "SYS_NERV_SPINAL_SACRAL", 0.0007f, 0.0006f, true,
                V(0.002f, 1.098f, Canal(1.098f).z), V(0.003f, 0.900f, Canal(0.900f).z), V(0.006f, 0.840f, 0.086f), V(0.016f, 0.826f, 0.080f));
        }

        // ---------------------------------------------------------------- plexuses

        private static void Plexuses()
        {
            // Cervical plexus (C1-C4): loops deep to the sternocleidomastoid, supplying the neck and,
            // through the phrenic nerve, the diaphragm.
            for (int i = 0; i < 4; i++)
            {
                float y = SkeletonBuilder.CervicalY(i) - 0.004f;
                _n.Add($"CervicalPlexus_C{i + 1}", "SYS_NERV_CERVICAL_PLEXUS", 0.0016f, 0.0013f, true,
                    V(0.034f, y - 0.006f, Canal(y).z - 0.008f), V(0.044f, y - 0.012f, Canal(y).z - 0.010f), V(0.048f, 1.512f - i * 0.006f, 0.006f));
            }
            _n.Add("AnsaCervicalis", "SYS_NERV_CERVICAL_PLEXUS", 0.0012f, 0.0010f, true,
                V(0.048f, 1.500f, 0.004f), V(0.042f, 1.500f, -0.010f), V(0.040f, 1.480f, -0.016f), V(0.034f, 1.458f, -0.030f), V(0.022f, 1.450f, -0.032f));
            _n.Add("SupraclavicularNerves", "SYS_NERV_CERVICAL_PLEXUS", 0.0012f, 0.0006f, true,
                UnderSkinAll(0.004f, V(0.052f, 1.500f, 0.002f), V(0.070f, 1.470f, -0.010f), V(0.098f, 1.448f, -0.034f), V(0.130f, 1.436f, -0.050f)));

            // Brachial plexus (C5-T1): five roots join into three trunks, split, and re-form as three
            // cords named for their relation to the axillary artery, from which the arm's nerves come.
            var rootPts = new[]
            {
                V(0.034f, 1.478f, 0.030f), V(0.034f, 1.466f, 0.034f), V(0.034f, 1.454f, 0.036f), V(0.034f, 1.445f, 0.038f), V(0.034f, 1.436f, 0.040f),
            };
            var trunkAt = new[] { V(0.054f, 1.462f, 0.026f), V(0.054f, 1.450f, 0.024f), V(0.054f, 1.438f, 0.024f) };
            int[] trunkOf = { 0, 0, 1, 2, 2 };
            for (int i = 0; i < 5; i++)
                _n.Add($"BrachialPlexus_Root{i + 5}", "SYS_NERV_BRACHIAL_PLEXUS", 0.0026f, 0.0022f, true,
                    rootPts[i], Vector3.Lerp(rootPts[i], trunkAt[trunkOf[i]], 0.5f) + V(0f, 0f, 0.004f), trunkAt[trunkOf[i]]);
            string[] trunkName = { "Upper", "Middle", "Lower" };
            var divisionAt = new[] { V(0.096f, 1.440f, 0.008f), V(0.096f, 1.428f, 0.008f), V(0.096f, 1.418f, 0.006f) };
            for (int i = 0; i < 3; i++)
                _n.Add($"BrachialPlexus_{trunkName[i]}Trunk", "SYS_NERV_BRACHIAL_PLEXUS", 0.0040f, 0.0034f, true,
                    trunkAt[i], Vector3.Lerp(trunkAt[i], divisionAt[i], 0.5f) + V(0f, 0.002f, 0.004f), divisionAt[i]);

            // Cords: lateral (in front and outside the artery), posterior (behind), medial (inside).
            var lateral = new[] { V(0.096f, 1.440f, 0.006f), V(0.126f, 1.418f, -0.014f), V(0.150f, 1.410f, -0.018f) };
            var posterior = new[] { V(0.096f, 1.428f, 0.008f), V(0.126f, 1.410f, 0.004f), V(0.152f, 1.402f, 0.004f) };
            var medial = new[] { V(0.096f, 1.418f, 0.006f), V(0.126f, 1.402f, -0.002f), V(0.150f, 1.394f, -0.006f) };
            _n.Add("BrachialPlexus_LateralCord", "SYS_NERV_BRACHIAL_PLEXUS", 0.0038f, 0.0034f, true, lateral);
            _n.Add("BrachialPlexus_PosteriorCord", "SYS_NERV_BRACHIAL_PLEXUS", 0.0044f, 0.0038f, true, posterior);
            _n.Add("BrachialPlexus_MedialCord", "SYS_NERV_BRACHIAL_PLEXUS", 0.0036f, 0.0032f, true, medial);

            // Lumbar plexus (L1-L4) inside the psoas, and sacral plexus (L4-S4) on the piriformis:
            // between them the nerves of the whole leg.
            for (int i = 0; i < 4; i++)
            {
                Vector3 exit = Exit('L', i, out float y);
                _n.Add($"LumbarPlexus_L{i + 1}", "SYS_NERV_LUMBAR_PLEXUS", 0.0024f, 0.0026f, true,
                    exit + V(0.010f, -0.006f, -0.008f), V(0.040f, y - 0.030f - i * 0.006f, 0.038f), V(0.050f, 1.000f - i * 0.002f, 0.030f));
            }
            _n.Add("LumbosacralTrunk", "SYS_NERV_SACRAL_PLEXUS", 0.0030f, 0.0030f, true,
                V(0.036f, 1.000f, 0.040f), V(0.040f, 0.960f, 0.048f), V(0.046f, 0.910f, 0.056f), V(0.050f, 0.880f, 0.062f));
            for (int i = 0; i < 4; i++)
            {
                Vector3 exit = Exit('S', i, out float y);
                _n.Add($"SacralPlexus_S{i + 1}", "SYS_NERV_SACRAL_PLEXUS", 0.0024f, 0.0030f, true,
                    exit + V(0.006f, -0.002f, -0.026f), V(0.030f, y - 0.012f, 0.052f), V(0.050f, 0.876f - i * 0.002f, 0.062f));
            }
        }

        // ---------------------------------------------------------------- arm

        private static void ArmNerves()
        {
            // Musculocutaneous: pierces coracobrachialis, runs between biceps and brachialis, and
            // ends as the lateral cutaneous nerve of the forearm.
            _n.Add("MusculocutaneousNerve", "SYS_NERV_MUSCULOCUTANEOUS", 0.0026f, 0.0016f, true,
                V(0.150f, 1.410f, -0.018f), V(0.166f, 1.384f, -0.014f), V(0.176f, 1.340f, -0.014f), V(0.182f, 1.240f, -0.020f), V(0.190f, 1.134f, -0.022f),
                UnderSkin(V(0.210f, 1.040f, -0.030f), 0.005f), UnderSkin(V(0.222f, 0.930f, -0.030f), 0.004f));

            // Axillary: round the surgical neck of the humerus to the deltoid and teres minor.
            _n.Add("AxillaryNerve", "SYS_NERV_AXILLARY", 0.0034f, 0.0018f, true,
                V(0.152f, 1.402f, 0.004f), V(0.166f, 1.392f, 0.028f), V(0.192f, 1.376f, 0.034f), V(0.216f, 1.376f, 0.014f), V(0.230f, 1.384f, -0.010f));

            // Radial: behind the humerus in the spiral groove, then to the front of the elbow and down
            // the thumb side of the forearm; its deep branch winds round the radius to the extensors.
            _n.Add("RadialNerve", "SYS_NERV_RADIAL", 0.0040f, 0.0026f, true,
                V(0.152f, 1.402f, 0.004f), V(0.172f, 1.352f, 0.032f), V(0.194f, 1.296f, 0.034f), V(0.208f, 1.230f, 0.024f),
                V(0.212f, 1.156f, -0.008f), V(0.210f, 1.110f, -0.020f), V(0.218f, 1.060f, -0.020f), V(0.228f, 0.980f, -0.014f),
                UnderSkin(V(0.236f, 0.900f, -0.004f), 0.004f), UnderSkin(V(0.240f, 0.830f, 0.020f), 0.003f));
            _n.Add("PosteriorInterosseousNerve", "SYS_NERV_RADIAL_DEEP", 0.0022f, 0.0012f, true,
                V(0.210f, 1.110f, -0.020f), V(0.216f, 1.090f, 0.010f), V(0.218f, 1.040f, 0.030f), V(0.222f, 0.980f, 0.030f), V(0.226f, 0.910f, 0.026f));

            // Median: with the brachial artery, crossing in front of it at mid-arm, through the carpal
            // tunnel to the thumb, index, middle and half the ring finger.
            _n.Add("MedianNerve", "SYS_NERV_MEDIAN", 0.0038f, 0.0028f, true,
                V(0.150f, 1.406f, -0.014f), V(0.166f, 1.384f, -0.010f), V(0.174f, 1.300f, -0.008f), V(0.176f, 1.200f, -0.006f),
                V(0.182f, 1.108f, -0.014f), V(0.194f, 1.040f, -0.006f), V(0.205f, 0.960f, -0.008f), V(0.212f, 0.890f, -0.010f),
                V(0.214f, 0.848f, -0.014f), V(0.220f, 0.816f, -0.022f));

            // Ulnar: medial to the artery, behind the medial epicondyle (the "funny bone") and along the
            // little-finger side of the forearm to the hand.
            _n.Add("UlnarNerve", "SYS_NERV_ULNAR", 0.0036f, 0.0024f, true,
                V(0.150f, 1.394f, -0.006f), V(0.166f, 1.362f, 0.006f), V(0.168f, 1.282f, 0.016f), V(0.170f, 1.180f, 0.022f),
                V(0.164f, 1.108f, 0.026f), V(0.174f, 1.056f, 0.016f), V(0.186f, 0.982f, 0.000f), V(0.194f, 0.912f, -0.006f),
                V(0.196f, 0.852f, -0.010f), V(0.192f, 0.820f, -0.018f));

            // Digital nerves: median to the thumb, index, middle and ring; ulnar to the ring and little.
            var thumb = SkeletonBuilder.ThumbPath();
            _n.Add("DigitalNerve_Thumb", "SYS_NERV_DIGITAL_HAND", 0.0014f, 0.0007f, true,
                V(0.220f, 0.816f, -0.022f), thumb[1] + V(-0.004f, 0f, -0.006f), thumb[2] + V(-0.004f, 0f, -0.006f), Vector3.Lerp(thumb[2], thumb[3], 0.85f) + V(-0.004f, 0f, -0.006f));
            for (int i = 0; i < 4; i++)
            {
                Vector3[] j = SkeletonBuilder.FingerPath(i);
                _n.Add($"DigitalNerve_{i + 1}", "SYS_NERV_DIGITAL_HAND", 0.0013f, 0.0006f, true,
                    V(j[0].x + (i < 2 ? 0.006f : -0.004f), 0.800f, -0.024f), j[1] + V(-0.002f, 0f, -0.0085f), j[2] + V(-0.002f, 0f, -0.0085f),
                    j[3] + V(-0.002f, 0f, -0.0085f), j[4] + V(-0.002f, 0f, -0.0085f));
            }
        }

        // ---------------------------------------------------------------- trunk

        private static void TrunkNerves()
        {
            // Intercostal nerves in the costal groove under each rib; the twelfth is the subcostal nerve.
            for (int rib = 1; rib <= 11; rib++)
            {
                Vector3 exit = Exit('T', rib, out _);
                var path = new List<Vector3> { V(exit.x + 0.012f, exit.y - 0.006f, exit.z - 0.008f) };
                path.AddRange(AlongRib(rib, RibStart(rib), Mathf.Min(RibEnd(rib), 0.82f * Mathf.PI), 8, -0.004f, 0.011f));
                _n.Add($"IntercostalNerve_{rib + 1}", "SYS_NERV_INTERCOSTAL", 0.0016f, 0.0010f, true, path.ToArray());
            }

            // Phrenic nerve: from C3-C5, down the front of the scalene muscle, between the lung and the
            // pericardium, to the diaphragm - the nerve that keeps you breathing.
            SdfFunc heart = OrganBuilder.HeartField;
            _n.Add("PhrenicNerve_L", "SYS_NERV_PHRENIC", 0.0022f, 0.0016f, false,
                V(0.044f, 1.505f, 0.016f), V(0.048f, 1.470f, 0.006f), V(0.056f, 1.430f, -0.022f), V(0.070f, 1.372f, -0.030f),
                Settle(heart, V(0.082f, 1.310f, -0.040f), 0.0075f), Settle(heart, V(0.084f, 1.262f, -0.030f), 0.0075f), V(0.086f, 1.200f, -0.010f), V(0.084f, 1.160f, 0.004f));
            _n.Add("PhrenicNerve_R", "SYS_NERV_PHRENIC", 0.0022f, 0.0016f, false,
                V(-0.044f, 1.505f, 0.016f), V(-0.048f, 1.470f, 0.006f), V(-0.052f, 1.430f, -0.020f), V(-0.050f, 1.372f, -0.034f),
                V(-0.058f, 1.322f, -0.040f), V(-0.064f, 1.276f, -0.036f), V(-0.070f, 1.220f, -0.020f), V(-0.072f, 1.170f, -0.004f));

            // Pudendal nerve: the perineum's nerve, from S2-S4 round the ischial spine.
            _n.Add("PudendalNerve", "SYS_NERV_PUDENDAL", 0.0026f, 0.0018f, true,
                V(0.050f, 0.876f, 0.062f), V(0.046f, 0.842f, 0.058f), V(0.040f, 0.818f, 0.044f), V(0.030f, 0.796f, 0.022f), V(0.018f, 0.782f, -0.004f));
        }

        // ---------------------------------------------------------------- leg

        private static void LegNerves()
        {
            // Femoral nerve: out of the lumbar plexus, under the inguinal ligament beside the femoral
            // artery, splitting into the branches to the quadriceps; its longest branch, the saphenous
            // nerve, runs down the inside of the leg to the foot.
            _n.Add("FemoralNerve", "SYS_NERV_FEMORAL", 0.0046f, 0.0026f, true,
                V(0.050f, 1.000f, 0.030f), V(0.066f, 0.950f, 0.010f), V(0.086f, 0.896f, -0.026f), V(0.090f, 0.850f, -0.042f), V(0.088f, 0.806f, -0.050f));
            _n.Add("FemoralNerve_QuadricepsBranches", "SYS_NERV_FEMORAL", 0.0020f, 0.0009f, true,
                V(0.090f, 0.850f, -0.042f), V(0.100f, 0.800f, -0.054f), V(0.104f, 0.720f, -0.062f), V(0.100f, 0.620f, -0.058f));
            _n.Add("SaphenousNerve", "SYS_NERV_SAPHENOUS", 0.0020f, 0.0012f, true,
                V(0.088f, 0.806f, -0.050f), V(0.078f, 0.736f, -0.040f), V(0.070f, 0.650f, -0.026f), V(0.062f, 0.590f, -0.006f),
                UnderSkin(BodyShape.Under(BodyShape.Thigh, 0.520f, 285f, 0.004f), 0.005f),
                UnderSkin(BodyShape.Under(BodyShape.Shank, 0.400f, 280f, 0.004f), 0.004f),
                UnderSkin(BodyShape.Under(BodyShape.Shank, 0.250f, 275f, 0.004f), 0.004f),
                UnderSkin(BodyShape.Under(BodyShape.Shank, 0.120f, 275f, 0.004f), 0.004f), V(0.064f, 0.058f, -0.036f));

            // Obturator nerve: through the obturator foramen to the adductor muscles of the medial thigh.
            _n.Add("ObturatorNerve", "SYS_NERV_OBTURATOR", 0.0030f, 0.0016f, true,
                V(0.044f, 1.000f, 0.036f), V(0.052f, 0.950f, 0.020f), V(0.060f, 0.892f, -0.006f), V(0.064f, 0.848f, -0.012f), V(0.064f, 0.780f, 0.000f), V(0.066f, 0.710f, 0.012f));
            _n.Add("LateralFemoralCutaneousNerve", "SYS_NERV_FEMORAL_CUTANEOUS", 0.0016f, 0.0008f, true,
                V(0.052f, 1.010f, 0.034f), V(0.090f, 0.966f, 0.020f), UnderSkin(V(0.112f, 0.936f, -0.030f), 0.005f),
                UnderSkin(V(0.128f, 0.860f, -0.024f), 0.005f), UnderSkin(V(0.134f, 0.760f, -0.020f), 0.005f), UnderSkin(V(0.130f, 0.660f, -0.014f), 0.005f));

            // Sciatic nerve: the body's thickest, out of the pelvis below the piriformis and down the
            // back of the thigh, dividing above the knee into the tibial and common fibular nerves.
            _n.Add("SciaticNerve", "SYS_NERV_SCIATIC", 0.0070f, 0.0060f, true,
                V(0.050f, 0.876f, 0.062f), V(0.064f, 0.850f, 0.070f), V(0.076f, 0.810f, 0.070f), V(0.084f, 0.740f, 0.070f),
                V(0.088f, 0.660f, 0.070f), V(0.090f, 0.590f, 0.068f), V(0.088f, 0.545f, 0.068f));
            _n.Add("TibialNerve", "SYS_NERV_TIBIAL", 0.0046f, 0.0030f, true,
                V(0.088f, 0.545f, 0.068f), V(0.088f, 0.500f, 0.070f), V(0.086f, 0.450f, 0.062f), V(0.082f, 0.380f, 0.054f), V(0.080f, 0.300f, 0.049f),
                V(0.076f, 0.200f, 0.043f), V(0.070f, 0.120f, 0.039f), V(0.064f, 0.085f, 0.034f), V(0.062f, 0.062f, 0.036f));
            _n.Add("MedialPlantarNerve", "SYS_NERV_PLANTAR", 0.0022f, 0.0010f, true,
                V(0.062f, 0.062f, 0.036f), V(0.066f, 0.030f, 0.010f), V(0.072f, 0.014f, -0.036f), V(0.070f, 0.010f, -0.096f));
            _n.Add("LateralPlantarNerve", "SYS_NERV_PLANTAR", 0.0020f, 0.0010f, true,
                V(0.062f, 0.062f, 0.036f), V(0.078f, 0.030f, 0.026f), V(0.090f, 0.014f, -0.004f), V(0.098f, 0.011f, -0.046f), V(0.100f, 0.010f, -0.096f));

            _n.Add("CommonFibularNerve", "SYS_NERV_FIBULAR_COMMON", 0.0040f, 0.0026f, true,
                V(0.088f, 0.545f, 0.068f), V(0.104f, 0.520f, 0.064f), V(0.118f, 0.488f, 0.050f), V(0.126f, 0.462f, 0.036f), V(0.126f, 0.440f, 0.020f));
            _n.Add("DeepFibularNerve", "SYS_NERV_FIBULAR_DEEP", 0.0026f, 0.0012f, true,
                V(0.126f, 0.440f, 0.020f), V(0.120f, 0.418f, 0.004f), V(0.110f, 0.380f, -0.008f), V(0.108f, 0.300f, -0.016f), V(0.102f, 0.200f, -0.019f),
                V(0.096f, 0.120f, -0.017f), V(0.094f, 0.076f, -0.018f), V(0.096f, 0.050f, -0.048f), V(0.094f, 0.040f, -0.092f));
            _n.Add("SuperficialFibularNerve", "SYS_NERV_FIBULAR_SUPERFICIAL", 0.0024f, 0.0010f, true,
                V(0.126f, 0.440f, 0.020f), V(0.130f, 0.400f, 0.010f),
                UnderSkin(V(0.128f, 0.320f, 0.004f), 0.008f), UnderSkin(V(0.122f, 0.220f, -0.006f), 0.008f),
                UnderSkin(V(0.112f, 0.130f, -0.024f), 0.008f), UnderSkin(V(0.108f, 0.070f, -0.060f), 0.008f), UnderSkin(V(0.100f, 0.050f, -0.100f), 0.008f));
            _n.Add("SuralNerve", "SYS_NERV_SURAL", 0.0018f, 0.0012f, true,
                V(0.088f, 0.520f, 0.072f), UnderSkin(V(0.088f, 0.420f, 0.098f), 0.004f), UnderSkin(V(0.090f, 0.300f, 0.092f), 0.004f),
                UnderSkin(V(0.104f, 0.180f, 0.062f), 0.004f), UnderSkin(V(0.114f, 0.088f, 0.030f), 0.004f), UnderSkin(V(0.118f, 0.050f, -0.030f), 0.003f));
        }

        // ---------------------------------------------------------------- cranial nerves

        private static void CranialNerves()
        {
            // I: olfactory - the sensory fibres from the nose through the cribriform plate to the bulb,
            // and the tract carrying smell back into the brain.
            _n.Add("OlfactoryBulbAndTract", "SYS_NERV_OLFACTORY", t => Mathf.Lerp(0.0034f, 0.0018f, t), true,
                V(0.010f, 1.626f, -0.074f), V(0.011f, 1.624f, -0.056f), V(0.013f, 1.622f, -0.040f), V(0.016f, 1.621f, -0.026f));
            _n.Add("OlfactoryNerves", "SYS_NERV_OLFACTORY", 0.0005f, 0.0005f, true,
                V(0.008f, 1.626f, -0.076f), V(0.008f, 1.618f, -0.082f), V(0.010f, 1.606f, -0.088f));

            // II: optic - the eye to the chiasm, where half the fibres cross, and on as the optic tracts.
            _n.Add("OpticNerve", "SYS_NERV_OPTIC", 0.0032f, 0.0030f, true,
                V(0.030f, 1.658f, -0.058f), V(0.026f, 1.656f, -0.042f), V(0.020f, 1.640f, -0.036f), V(0.008f, 1.624f, -0.033f));
            _n.Add("OpticTract", "SYS_NERV_OPTIC", 0.0032f, 0.0026f, true,
                V(0.008f, 1.624f, -0.033f), V(0.014f, 1.622f, -0.024f), V(0.022f, 1.624f, -0.010f), V(0.030f, 1.630f, 0.004f));

            // III oculomotor, IV trochlear, VI abducens: the three nerves that move the eye.
            _n.Add("OculomotorNerve", "SYS_NERV_OCULOMOTOR", 0.0014f, 0.0010f, true,
                V(0.006f, 1.616f, -0.014f), V(0.012f, 1.618f, -0.030f), V(0.020f, 1.630f, -0.046f), V(0.028f, 1.654f, -0.050f));
            _n.Add("TrochlearNerve", "SYS_NERV_TROCHLEAR", 0.0009f, 0.0007f, true,
                V(0.004f, 1.638f, 0.006f), V(0.018f, 1.630f, 0.002f), V(0.024f, 1.624f, -0.024f), V(0.024f, 1.640f, -0.050f), V(0.026f, 1.666f, -0.052f));
            _n.Add("AbducensNerve", "SYS_NERV_ABDUCENS", 0.0010f, 0.0008f, true,
                V(0.005f, 1.586f, -0.012f), V(0.012f, 1.598f, -0.030f), V(0.026f, 1.640f, -0.048f), V(0.036f, 1.654f, -0.056f));

            // V: trigeminal - the great sensory nerve of the face, with its ganglion and three divisions:
            // ophthalmic (forehead, eye), maxillary (cheek, upper jaw) and mandibular (lower jaw, chewing).
            _n.Add("TrigeminalRoot", "SYS_NERV_TRIGEMINAL", 0.0034f, 0.0034f, true,
                V(0.016f, 1.604f, 0.000f), V(0.026f, 1.606f, -0.010f), V(0.034f, 1.610f, -0.020f));
            _n.Add("TrigeminalGanglion", "SYS_NERV_TRIGEMINAL", t => 0.0026f + 0.0030f * Mathf.Sin(t * Mathf.PI), true,
                V(0.034f, 1.610f, -0.020f), V(0.038f, 1.610f, -0.026f), V(0.042f, 1.608f, -0.032f));
            _n.Add("OphthalmicNerve", "SYS_NERV_TRIGEMINAL", 0.0018f, 0.0008f, true,
                V(0.038f, 1.610f, -0.026f), V(0.034f, 1.628f, -0.046f), V(0.030f, 1.652f, -0.066f),
                UnderSkin(V(0.026f, 1.690f, -0.084f), 0.005f), UnderSkin(V(0.022f, 1.722f, -0.080f), 0.005f));
            _n.Add("MaxillaryNerve", "SYS_NERV_TRIGEMINAL", 0.0020f, 0.0008f, true,
                V(0.040f, 1.608f, -0.030f), V(0.046f, 1.598f, -0.052f), V(0.044f, 1.590f, -0.074f), UnderSkin(V(0.038f, 1.594f, -0.098f), 0.006f));
            _n.Add("MandibularNerve", "SYS_NERV_TRIGEMINAL", 0.0026f, 0.0010f, true,
                V(0.042f, 1.606f, -0.030f), V(0.052f, 1.586f, -0.026f), V(0.060f, 1.560f, -0.034f), UnderSkin(V(0.056f, 1.538f, -0.054f), 0.012f),
                UnderSkin(V(0.048f, 1.526f, -0.076f), 0.012f), UnderSkin(V(0.036f, 1.520f, -0.094f), 0.012f));

            // VII: facial - to the muscles of expression, through the parotid gland where it fans into
            // its five branches (temporal, zygomatic, buccal, marginal mandibular, cervical).
            _n.Add("FacialNerve", "SYS_NERV_FACIAL", 0.0024f, 0.0020f, true,
                V(0.016f, 1.596f, 0.006f), V(0.034f, 1.604f, 0.012f), V(0.050f, 1.594f, 0.018f), V(0.058f, 1.578f, 0.010f), V(0.062f, 1.568f, -0.008f));
            _n.Add("FacialNerve_Temporal", "SYS_NERV_FACIAL", 0.0011f, 0.0006f, true,
                V(0.062f, 1.568f, -0.008f), UnderSkin(V(0.070f, 1.610f, -0.020f), 0.006f), UnderSkin(V(0.070f, 1.650f, -0.036f), 0.005f), UnderSkin(V(0.060f, 1.694f, -0.056f), 0.005f));
            _n.Add("FacialNerve_Zygomatic", "SYS_NERV_FACIAL", 0.0011f, 0.0006f, true,
                V(0.062f, 1.568f, -0.008f), UnderSkin(V(0.070f, 1.596f, -0.040f), 0.006f), UnderSkin(V(0.064f, 1.626f, -0.076f), 0.005f), UnderSkin(V(0.050f, 1.640f, -0.092f), 0.005f));
            _n.Add("FacialNerve_Buccal", "SYS_NERV_FACIAL", 0.0012f, 0.0006f, true,
                V(0.062f, 1.568f, -0.008f), UnderSkin(V(0.068f, 1.580f, -0.046f), 0.006f), UnderSkin(V(0.056f, 1.586f, -0.086f), 0.005f), UnderSkin(V(0.040f, 1.580f, -0.104f), 0.005f));
            _n.Add("FacialNerve_MarginalMandibular", "SYS_NERV_FACIAL", 0.0010f, 0.0006f, true,
                V(0.062f, 1.568f, -0.008f), UnderSkin(V(0.064f, 1.552f, -0.044f), 0.006f), UnderSkin(V(0.052f, 1.534f, -0.080f), 0.005f), UnderSkin(V(0.040f, 1.530f, -0.098f), 0.005f));
            _n.Add("FacialNerve_Cervical", "SYS_NERV_FACIAL", 0.0010f, 0.0006f, true,
                V(0.062f, 1.568f, -0.008f), UnderSkin(V(0.062f, 1.540f, -0.020f), 0.006f), UnderSkin(V(0.056f, 1.508f, -0.030f), 0.005f));

            // VIII: vestibulocochlear - hearing and balance, from the inner ear to the brainstem.
            _n.Add("VestibulocochlearNerve", "SYS_NERV_VESTIBULOCOCHLEAR", 0.0022f, 0.0018f, true,
                V(0.016f, 1.598f, 0.008f), V(0.030f, 1.606f, 0.014f), V(0.042f, 1.612f, 0.018f), V(0.052f, 1.616f, 0.020f));

            // IX glossopharyngeal, X vagus, XI accessory: the three that leave the skull together
            // through the jugular foramen; XII hypoglossal moves the tongue.
            _n.Add("GlossopharyngealNerve", "SYS_NERV_GLOSSOPHARYNGEAL", 0.0016f, 0.0010f, true,
                V(0.010f, 1.582f, 0.014f), V(0.030f, 1.596f, 0.016f), V(0.040f, 1.580f, 0.006f), V(0.036f, 1.556f, -0.014f), V(0.028f, 1.540f, -0.040f), V(0.022f, 1.536f, -0.062f));
            _n.Add("AccessoryNerve", "SYS_NERV_ACCESSORY", 0.0014f, 0.0012f, true,
                V(0.006f, 1.500f, Canal(1.500f).z - 0.001f), V(0.014f, 1.530f, Canal(1.530f).z + 0.001f), V(0.024f, 1.566f, 0.038f), V(0.036f, 1.596f, 0.022f),
                V(0.044f, 1.572f, 0.004f), V(0.056f, 1.540f, 0.002f), UnderSkin(V(0.070f, 1.500f, 0.014f), 0.006f), UnderSkin(V(0.090f, 1.462f, 0.040f), 0.006f));
            _n.Add("HypoglossalNerve", "SYS_NERV_HYPOGLOSSAL", 0.0016f, 0.0012f, true,
                V(0.006f, 1.582f, 0.010f), V(0.022f, 1.582f, -0.004f), V(0.036f, 1.556f, -0.014f), V(0.040f, 1.542f, -0.036f), V(0.030f, 1.538f, -0.062f), V(0.016f, 1.540f, -0.080f));

            // Vagus: the wandering nerve, from the medulla through the neck in the carotid sheath, down
            // the chest to the gut. The left crosses in front of the aortic arch and the right behind
            // the right bronchus; the two form the oesophageal plexus and end as the anterior and
            // posterior vagal trunks at the stomach.
            _n.Add("VagusNerve_L", "SYS_NERV_VAGUS", 0.0024f, 0.0016f, false,
                V(0.010f, 1.580f, 0.012f), V(0.028f, 1.598f, 0.014f), V(0.038f, 1.570f, 0.004f), V(0.038f, 1.520f, -0.006f), V(0.040f, 1.450f, -0.008f),
                V(0.044f, 1.412f, 0.006f), V(0.046f, 1.372f, 0.022f), V(0.042f, 1.336f, 0.030f), V(0.030f, 1.290f, 0.034f),
                V(0.020f, 1.240f, 0.010f), V(0.024f, 1.200f, -0.008f), V(0.040f, 1.170f, -0.030f));
            _n.Add("VagusNerve_R", "SYS_NERV_VAGUS", 0.0024f, 0.0016f, false,
                V(-0.010f, 1.580f, 0.012f), V(-0.028f, 1.598f, 0.014f), V(-0.038f, 1.570f, 0.004f), V(-0.038f, 1.520f, -0.006f), V(-0.040f, 1.450f, -0.010f),
                V(-0.038f, 1.412f, 0.000f), V(-0.034f, 1.384f, 0.016f), V(-0.030f, 1.350f, 0.026f), V(-0.016f, 1.300f, 0.036f),
                V(-0.006f, 1.250f, 0.038f), V(0.002f, 1.204f, 0.024f), V(0.004f, 1.180f, 0.006f));

            // Recurrent laryngeal nerves: branches of the vagus that loop under the aortic arch (left)
            // and the right subclavian artery, then climb back up to the larynx to move the vocal cords.
            _n.Add("RecurrentLaryngealNerve_L", "SYS_NERV_RECURRENT_LARYNGEAL", 0.0014f, 0.0010f, false,
                V(0.044f, 1.404f, 0.004f), V(0.032f, 1.384f, 0.014f), V(0.016f, 1.396f, 0.016f), V(0.010f, 1.440f, 0.016f), V(0.012f, 1.490f, 0.010f), V(0.016f, 1.508f, -0.004f));
            _n.Add("RecurrentLaryngealNerve_R", "SYS_NERV_RECURRENT_LARYNGEAL", 0.0014f, 0.0010f, false,
                V(-0.040f, 1.436f, -0.002f), V(-0.040f, 1.420f, 0.006f), V(-0.024f, 1.424f, 0.014f), V(-0.010f, 1.450f, 0.016f), V(-0.012f, 1.490f, 0.010f), V(-0.016f, 1.508f, -0.004f));
        }

        // ---------------------------------------------------------------- autonomic

        private static void Autonomic()
        {
            // Sympathetic trunk: a chain of ganglia down each side of the spine, joined by short
            // connecting nerves, from the neck to the coccyx - beads on a string.
            var chain = new List<Vector3>();
            for (float y = 1.560f; y >= 0.900f; y -= 0.026f)
            {
                float zBody = SkeletonBuilder.SpineBodyZ(y);
                float x = y > 1.44f ? 0.030f : (y > 1.15f ? 0.030f : (y > 1.0f ? 0.032f : 0.020f));
                float z = zBody + (y > 1.44f ? -0.004f : y > 1.15f ? 0.004f : -0.008f);
                if (y < 1.0f) z = Canal(y).z - 0.030f;
                chain.Add(V(x, y, z));
            }
            _n.Add("SympatheticTrunk", "SYS_NERV_SYMPATHETIC_TRUNK", t => 0.0009f + 0.0012f * Mathf.Pow(Mathf.Abs(Mathf.Sin(t * Mathf.PI * 23f)), 3f), true, chain.ToArray());

            // Greater splanchnic nerve: from the mid-thoracic chain through the diaphragm to the celiac ganglion.
            _n.Add("GreaterSplanchnicNerve", "SYS_NERV_SPLANCHNIC", 0.0014f, 0.0014f, true,
                V(0.030f, 1.320f, 0.044f), V(0.026f, 1.260f, 0.044f), V(0.022f, 1.200f, 0.038f), V(0.020f, 1.160f, 0.030f), V(0.014f, 1.148f, 0.016f));
            _n.Add("CeliacGanglion", "SYS_NERV_SPLANCHNIC", t => 0.0026f + 0.0016f * Mathf.Sin(t * Mathf.PI), true,
                V(0.014f, 1.150f, 0.018f), V(0.010f, 1.148f, 0.010f), V(0.008f, 1.146f, 0.002f));
        }
    }
}
