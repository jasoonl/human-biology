using System.Collections.Generic;
using UnityEngine;
using static HumanBodyExplorer.EditorTools.Geometry.TubeKit;

namespace HumanBodyExplorer.EditorTools.Geometry
{
    /// <summary>
    /// The circulatory system as branching networks of lofted tubes: the whole arterial tree from
    /// the aortic root to the digital arteries of the hand and the plantar arch of the foot, the
    /// venous return that shadows it, the superficial veins that run just under the skin, the
    /// dural sinuses and the thoracic duct. Paths are described in the left half of the body
    /// (positive X) and mirrored where the anatomy is symmetric; the handful of asymmetric ones -
    /// where the great vessels leave the heart - are written out on each side.
    ///
    /// Vessels share landmarks with the skeleton, muscles and organs they travel between
    /// (<see cref="SkeletonBuilder"/> joints, <see cref="BodyShape"/> limbs, the heart field), so
    /// they sit against the structures they really touch. Skin-hugging veins are settled onto the
    /// skin from underneath rather than hand-placed.
    /// </summary>
    public static class VesselBuilder
    {
        private static TubeKit _a;   // arteries
        private static TubeKit _v;   // veins
        private static TubeKit _l;   // lymphatics

        public static int Build(Transform root, Material artery, Material vein, Material lymph, int layer)
        {
            _a = new TubeKit(root, artery, layer);
            _v = new TubeKit(root, vein, layer);
            _l = new TubeKit(root, lymph, layer);

            Aorta();
            HeadAndNeck();
            Thorax();
            Abdomen();
            Pelvis();
            ArmArteries();
            LegArteries();
            HeadNeckVeins();
            TrunkVeins();
            ArmVeins();
            LegVeins();
            ThoracicDuct();
            return _a.Count + _v.Count + _l.Count;
        }

        // ================================================================ aorta and its arch branches

        private static void Aorta()
        {
            // Ascending aorta from the left ventricle, the arch over the left bronchus, then the
            // descending aorta down the left front of the spine, through the diaphragm at T12.
            _a.Add("Aorta_Ascending", "SYS_CV_AORTA_ASC", 0.0135f, 0.0125f, false,
                V(0.006f, 1.296f, -0.036f), V(0.000f, 1.330f, -0.040f), V(-0.004f, 1.362f, -0.038f), V(-0.002f, 1.392f, -0.030f));
            _a.Add("Aorta_Arch", "SYS_CV_AORTIC_ARCH", 0.0125f, 0.0115f, false,
                V(-0.002f, 1.392f, -0.030f), V(0.012f, 1.412f, -0.016f), V(0.024f, 1.410f, 0.006f), V(0.030f, 1.392f, 0.026f), V(0.026f, 1.362f, 0.036f));
            _a.Add("Aorta_Descending", "SYS_CV_AORTA_DESC", 0.0115f, 0.0100f, false,
                V(0.026f, 1.362f, 0.036f), V(0.023f, 1.320f, 0.038f), V(0.019f, 1.250f, 0.038f), V(0.015f, 1.180f, 0.033f), V(0.010f, 1.150f, 0.028f));
            _a.Add("Aorta_Abdominal", "SYS_CV_AORTA_ABD", 0.0100f, 0.0072f, false,
                V(0.010f, 1.150f, 0.028f), V(0.005f, 1.100f, 0.018f), V(0.002f, 1.050f, 0.011f), V(0.000f, 1.008f, 0.012f));

            // The arch's three branches: brachiocephalic trunk (which soon divides into right
            // subclavian and right common carotid), left common carotid, left subclavian.
            _a.Add("BrachiocephalicTrunk", "SYS_CV_BRACHIOCEPHALIC", 0.0088f, 0.0074f, false,
                V(0.000f, 1.400f, -0.028f), V(-0.014f, 1.418f, -0.030f), V(-0.030f, 1.438f, -0.030f));
        }

        // ================================================================ head and neck

        private static void HeadAndNeck()
        {
            // Common carotids. The right rises from the brachiocephalic trunk, the left directly
            // from the arch, so the left is the longer.
            _a.Add("CommonCarotid_L", "SYS_CV_CAROTID", 0.0042f, 0.0040f, false,
                V(0.016f, 1.410f, -0.010f), V(0.028f, 1.434f, -0.016f), V(0.034f, 1.472f, -0.016f), V(0.036f, 1.535f, -0.010f));
            _a.Add("CommonCarotid_R", "SYS_CV_CAROTID", 0.0042f, 0.0040f, false,
                V(-0.030f, 1.438f, -0.030f), V(-0.033f, 1.470f, -0.018f), V(-0.036f, 1.535f, -0.010f));

            // External carotid: to the face, scalp and jaws. Internal carotid: to the brain, with no
            // branches at all in the neck.
            _a.Add("ExternalCarotid", "SYS_CV_CAROTID_EXT", 0.0034f, 0.0022f, true,
                V(0.036f, 1.535f, -0.010f), V(0.040f, 1.556f, -0.018f), V(0.048f, 1.580f, -0.016f), V(0.058f, 1.606f, -0.012f));
            _a.Add("InternalCarotid", "SYS_CV_CAROTID_INT", 0.0036f, 0.0026f, true,
                V(0.036f, 1.535f, -0.010f), V(0.036f, 1.568f, 0.000f), V(0.032f, 1.596f, -0.004f), V(0.024f, 1.608f, -0.012f), V(0.016f, 1.612f, -0.026f));

            _a.Add("SuperiorThyroidArtery", "SYS_CV_THYROID_ARTERY", 0.0016f, 0.0010f, true,
                V(0.038f, 1.544f, -0.020f), V(0.032f, 1.524f, -0.034f), V(0.024f, 1.500f, -0.036f), V(0.020f, 1.488f, -0.030f));

            // Superficial branches of the external carotid, settled onto the skin from beneath.
            _a.Add("FacialArtery", "SYS_CV_FACIAL_ARTERY", 0.0018f, 0.0010f, true,
                UnderSkinAll(0.007f, V(0.044f, 1.556f, -0.030f), V(0.056f, 1.538f, -0.066f), V(0.040f, 1.534f, -0.094f),
                    V(0.032f, 1.558f, -0.104f), V(0.024f, 1.598f, -0.104f), V(0.016f, 1.636f, -0.096f)));
            _a.Add("SuperficialTemporalArtery", "SYS_CV_TEMPORAL_ARTERY", 0.0018f, 0.0009f, true,
                UnderSkinAll(0.004f, V(0.058f, 1.606f, -0.012f), V(0.070f, 1.640f, -0.008f), V(0.068f, 1.690f, -0.010f), V(0.056f, 1.728f, -0.018f)));
            _a.Add("TemporalArtery_Parietal", "SYS_CV_TEMPORAL_ARTERY", 0.0012f, 0.0007f, true,
                UnderSkinAll(0.004f, V(0.070f, 1.690f, -0.010f), V(0.070f, 1.720f, 0.010f), V(0.056f, 1.748f, 0.034f)));

            // Vertebral artery: up through the transverse foramina of C6 to C1, round the atlas and
            // in through the foramen magnum to join its twin as the basilar artery.
            _a.Add("VertebralArtery", "SYS_CV_VERTEBRAL", 0.0028f, 0.0026f, true,
                V(0.046f, 1.424f, -0.024f), V(0.030f, 1.452f, 0.026f), V(0.026f, 1.500f, 0.032f), V(0.028f, 1.552f, 0.036f),
                V(0.020f, 1.566f, 0.048f), V(0.010f, 1.572f, 0.034f), V(0.004f, 1.580f, 0.006f));
            _a.Add("BasilarArtery", "SYS_CV_BASILAR", 0.0032f, 0.0028f, false,
                V(0f, 1.580f, 0.004f), V(0f, 1.592f, -0.010f), V(0f, 1.604f, -0.017f), V(0f, 1.611f, -0.013f));

            // Circle of Willis: the ring at the base of the brain that lets either carotid or the
            // basilar artery feed the whole brain, with its cerebral arteries leaving it.
            _a.Add("CommunicatingPosterior", "SYS_CV_CIRCLE_OF_WILLIS", 0.0010f, 0.0010f, true,
                V(0.016f, 1.612f, -0.026f), V(0.015f, 1.613f, -0.014f), V(0.011f, 1.613f, -0.004f));
            _a.Add("CommunicatingAnterior", "SYS_CV_CIRCLE_OF_WILLIS", 0.0011f, 0.0011f, false,
                V(-0.008f, 1.621f, -0.040f), V(0f, 1.623f, -0.042f), V(0.008f, 1.621f, -0.040f));
            _a.Add("AnteriorCerebralArtery", "SYS_CV_ACA", 0.0018f, 0.0010f, true,
                V(0.016f, 1.612f, -0.026f), V(0.008f, 1.621f, -0.040f), V(0.005f, 1.645f, -0.066f), V(0.004f, 1.685f, -0.046f),
                V(0.004f, 1.712f, -0.008f), V(0.004f, 1.712f, 0.036f));
            _a.Add("MiddleCerebralArtery", "SYS_CV_MCA", 0.0022f, 0.0010f, true,
                V(0.016f, 1.612f, -0.026f), V(0.034f, 1.620f, -0.030f), V(0.050f, 1.634f, -0.024f), V(0.060f, 1.654f, -0.010f), V(0.062f, 1.680f, 0.004f));
            _a.Add("PosteriorCerebralArtery", "SYS_CV_PCA", 0.0018f, 0.0010f, true,
                V(0.000f, 1.612f, -0.012f), V(0.014f, 1.613f, 0.000f), V(0.030f, 1.620f, 0.024f), V(0.034f, 1.634f, 0.056f), V(0.030f, 1.648f, 0.084f));
        }

        // ================================================================ thorax

        private static void Thorax()
        {
            // Pulmonary trunk from the right ventricle, splitting into the two pulmonary arteries;
            // the right one passes behind the aorta and the superior vena cava.
            _a.Add("PulmonaryTrunk", "SYS_CV_PULMONARY_ARTERY", 0.0125f, 0.0110f, false,
                V(0.018f, 1.298f, -0.078f), V(0.026f, 1.324f, -0.062f), V(0.032f, 1.350f, -0.040f), V(0.034f, 1.362f, -0.022f));
            _a.Add("PulmonaryArtery_L", "SYS_CV_PULMONARY_ARTERY", 0.0085f, 0.0060f, false,
                V(0.034f, 1.362f, -0.022f), V(0.052f, 1.366f, -0.008f), V(0.070f, 1.356f, 0.004f), V(0.082f, 1.338f, 0.008f));
            _a.Add("PulmonaryArtery_R", "SYS_CV_PULMONARY_ARTERY", 0.0085f, 0.0060f, false,
                V(0.034f, 1.362f, -0.022f), V(0.012f, 1.360f, -0.012f), V(-0.024f, 1.354f, -0.008f), V(-0.050f, 1.346f, 0.000f), V(-0.074f, 1.332f, 0.006f));

            // Pulmonary veins: two from each lung into the left atrium. Oxygenated blood in a vein -
            // the exception to "veins carry deoxygenated blood".
            _v.Add("PulmonaryVein_LS", "SYS_CV_PULMONARY_VEIN", 0.0060f, 0.0056f, false,
                V(0.078f, 1.336f, 0.002f), V(0.052f, 1.328f, -0.006f), V(0.030f, 1.324f, -0.008f));
            _v.Add("PulmonaryVein_LI", "SYS_CV_PULMONARY_VEIN", 0.0056f, 0.0052f, false,
                V(0.080f, 1.310f, 0.012f), V(0.052f, 1.314f, 0.004f), V(0.030f, 1.314f, -0.004f));
            _v.Add("PulmonaryVein_RS", "SYS_CV_PULMONARY_VEIN", 0.0060f, 0.0056f, false,
                V(-0.076f, 1.336f, 0.004f), V(-0.050f, 1.330f, -0.008f), V(-0.010f, 1.322f, -0.014f));
            _v.Add("PulmonaryVein_RI", "SYS_CV_PULMONARY_VEIN", 0.0056f, 0.0052f, false,
                V(-0.078f, 1.310f, 0.014f), V(-0.050f, 1.312f, 0.002f), V(-0.010f, 1.312f, -0.006f));

            // Coronary arteries and veins: pressed against the surface of the heart, so their paths
            // are sketched and then settled onto the heart field.
            SdfFunc heart = OrganBuilder.HeartField;
            _a.Add("Coronary_LeftAnteriorDescending", "SYS_CV_CORONARY_LAD", 0.0026f, 0.0012f, false,
                SettleAll(heart, 0.0016f, V(0.010f, 1.318f, -0.046f), V(0.020f, 1.300f, -0.066f), V(0.034f, 1.268f, -0.074f),
                    V(0.048f, 1.236f, -0.066f), V(0.056f, 1.208f, -0.052f)));
            _a.Add("Coronary_Circumflex", "SYS_CV_CORONARY_CX", 0.0022f, 0.0012f, false,
                SettleAll(heart, 0.0016f, V(0.010f, 1.318f, -0.046f), V(0.030f, 1.326f, -0.030f), V(0.050f, 1.310f, -0.010f),
                    V(0.066f, 1.278f, -0.004f), V(0.068f, 1.242f, -0.014f)));
            _a.Add("Coronary_Right", "SYS_CV_CORONARY_RCA", 0.0026f, 0.0014f, false,
                SettleAll(heart, 0.0016f, V(-0.004f, 1.322f, -0.048f), V(-0.026f, 1.316f, -0.068f), V(-0.046f, 1.290f, -0.066f),
                    V(-0.050f, 1.258f, -0.050f), V(-0.034f, 1.230f, -0.052f), V(-0.010f, 1.206f, -0.046f)));
            _v.Add("GreatCardiacVein", "SYS_CV_CARDIAC_VEIN", 0.0028f, 0.0036f, false,
                SettleAll(heart, 0.0016f, V(0.056f, 1.212f, -0.054f), V(0.040f, 1.246f, -0.076f), V(0.024f, 1.298f, -0.068f),
                    V(0.020f, 1.322f, -0.040f), V(0.040f, 1.320f, 0.006f), V(0.012f, 1.288f, 0.008f), V(-0.016f, 1.282f, -0.008f)));

            // Internal thoracic arteries down the back of the costal cartilages beside the sternum,
            // and the posterior intercostal arteries between the ribs.
            _a.Add("InternalThoracicArtery", "SYS_CV_INTERNAL_THORACIC", 0.0026f, 0.0016f, true,
                V(0.044f, 1.426f, -0.030f), V(0.026f, 1.408f, -0.044f), V(0.022f, 1.350f, -0.062f), V(0.020f, 1.290f, -0.076f), V(0.020f, 1.236f, -0.084f));
            for (int rib = 2; rib <= 10; rib++)
            {
                float y = SkeletonBuilder.ThoracicY(rib);
                Vector3[] along = AlongRib(rib, RibStart(rib), Mathf.Min(RibEnd(rib), 0.80f * Mathf.PI), 8, -0.006f, 0.007f);
                var path = new List<Vector3> { V(0.020f, y - 0.004f, 0.036f), V(0.028f, y - 0.006f, 0.042f) };
                path.AddRange(along);
                _a.Add($"PosteriorIntercostalArtery_{rib + 1}", "SYS_CV_INTERCOSTAL_ARTERY", 0.0014f, 0.0009f, true, path.ToArray());
            }
        }

        // ================================================================ abdomen

        private static void Abdomen()
        {
            // Celiac trunk and its three branches - the foregut's supply: liver, stomach, spleen.
            _a.Add("CeliacTrunk", "SYS_CV_CELIAC", 0.0042f, 0.0038f, false,
                V(0.010f, 1.148f, 0.026f), V(0.010f, 1.148f, 0.004f), V(0.010f, 1.150f, -0.018f));
            _a.Add("CommonHepaticArtery", "SYS_CV_HEPATIC_ARTERY", 0.0032f, 0.0026f, false,
                V(0.010f, 1.150f, -0.018f), V(-0.010f, 1.152f, -0.030f), V(-0.034f, 1.146f, -0.036f), V(-0.046f, 1.142f, -0.038f));
            _a.Add("HepaticArtery_RightBranch", "SYS_CV_HEPATIC_ARTERY", 0.0022f, 0.0008f, false,
                V(-0.046f, 1.142f, -0.038f), V(-0.066f, 1.150f, -0.034f), V(-0.092f, 1.158f, -0.026f), V(-0.112f, 1.170f, -0.014f));
            _a.Add("HepaticArtery_LeftBranch", "SYS_CV_HEPATIC_ARTERY", 0.0020f, 0.0008f, false,
                V(-0.046f, 1.142f, -0.038f), V(-0.030f, 1.160f, -0.048f), V(-0.010f, 1.176f, -0.054f), V(0.016f, 1.188f, -0.056f));
            _a.Add("LeftGastricArtery", "SYS_CV_GASTRIC_ARTERY", 0.0022f, 0.0010f, false,
                V(0.010f, 1.150f, -0.018f), V(0.014f, 1.176f, -0.024f), V(0.026f, 1.166f, -0.040f), V(0.008f, 1.134f, -0.052f), V(-0.024f, 1.114f, -0.052f));
            _a.Add("SplenicArtery", "SYS_CV_SPLENIC_ARTERY", 0.0036f, 0.0022f, false,
                V(0.010f, 1.150f, -0.018f), V(0.032f, 1.164f, -0.006f), V(0.048f, 1.152f, 0.006f), V(0.066f, 1.166f, 0.014f), V(0.082f, 1.156f, 0.024f), V(0.092f, 1.170f, 0.032f));

            // Superior mesenteric artery: down through the mesentery, fanning into arcades that feed
            // the jejunum and ileum, with ileocolic, right and middle colic branches to the colon.
            var sma = new[] { V(0.004f, 1.116f, 0.016f), V(0.004f, 1.100f, 0.000f), V(0.006f, 1.070f, -0.014f), V(0.004f, 1.030f, -0.022f), V(-0.010f, 0.992f, -0.030f), V(-0.040f, 0.962f, -0.038f) };
            _a.Add("SuperiorMesentericArtery", "SYS_CV_SMA", 0.0046f, 0.0018f, false, sma);
            // Eight intestinal branches fan out of it: the upper ones curve left to the jejunum, the
            // lower ones right and down to the ileum, each sagging in an arc through the mesentery.
            for (int i = 0; i < 8; i++)
            {
                float u = i / 7f;
                Vector3 from = Vector3.Lerp(sma[1], sma[4], Mathf.Lerp(0.20f, 0.90f, u));
                bool left = i < 4;
                float k = left ? i / 3f : (i - 4) / 3f;
                Vector3 target = left ? V(Mathf.Lerp(0.030f, 0.088f, k), Mathf.Lerp(1.040f, 0.990f, k), -0.044f)
                                      : V(Mathf.Lerp(-0.010f, -0.070f, k), Mathf.Lerp(0.968f, 0.918f, k), -0.048f);
                Vector3 near = from + V(left ? 0.010f : -0.010f, -0.006f, -0.010f);
                Vector3 mid = Vector3.Lerp(near, target, 0.55f) + V(0f, -0.012f, -0.006f);
                _a.Add($"IntestinalArtery_{i + 1}", "SYS_CV_MESENTERIC_ARTERIES", 0.0016f, 0.0008f, false, from, near, mid, target);
            }
            _a.Add("MiddleColicArtery", "SYS_CV_MESENTERIC_ARTERIES", 0.0016f, 0.0008f, false,
                sma[1], V(0.000f, 1.100f, -0.030f), V(-0.010f, 1.094f, -0.056f), V(-0.020f, 1.086f, -0.070f));
            _a.Add("InferiorMesentericArtery", "SYS_CV_IMA", 0.0026f, 0.0012f, false,
                V(0.002f, 1.036f, 0.010f), V(0.014f, 1.014f, -0.002f), V(0.026f, 0.992f, -0.006f), V(0.034f, 0.962f, 0.000f), V(0.012f, 0.928f, 0.026f), V(0.006f, 0.898f, 0.036f));

            // Renal arteries: a short, wide branch of the aorta to each kidney - a quarter of the
            // heart's output goes through them. The right passes behind the vena cava.
            _a.Add("RenalArtery_L", "SYS_CV_RENAL_ARTERY", 0.0038f, 0.0030f, false,
                V(0.004f, 1.102f, 0.018f), V(0.026f, 1.104f, 0.034f), V(0.050f, 1.104f, 0.046f));
            _a.Add("RenalArtery_R", "SYS_CV_RENAL_ARTERY", 0.0038f, 0.0030f, false,
                V(0.002f, 1.098f, 0.018f), V(-0.020f, 1.096f, 0.036f), V(-0.050f, 1.086f, 0.046f));
        }

        // ================================================================ pelvis and leg arteries

        private static void Pelvis()
        {
            _a.Add("CommonIliacArtery", "SYS_CV_ILIAC", 0.0062f, 0.0056f, true,
                V(0.000f, 1.008f, 0.012f), V(0.020f, 0.986f, 0.014f), V(0.040f, 0.958f, 0.010f));
            _a.Add("ExternalIliacArtery", "SYS_CV_ILIAC_EXT", 0.0056f, 0.0052f, true,
                V(0.040f, 0.958f, 0.010f), V(0.058f, 0.926f, -0.010f), V(0.070f, 0.890f, -0.034f));
            _a.Add("InternalIliacArtery", "SYS_CV_ILIAC_INT", 0.0046f, 0.0028f, true,
                V(0.040f, 0.958f, 0.010f), V(0.044f, 0.930f, 0.030f), V(0.040f, 0.894f, 0.048f), V(0.030f, 0.862f, 0.054f));
        }

        private static readonly Vector3[] Femoral =
        {
            V(0.070f, 0.890f, -0.034f), V(0.074f, 0.820f, -0.044f), V(0.076f, 0.740f, -0.038f), V(0.072f, 0.650f, -0.018f),
            V(0.066f, 0.575f, 0.020f), V(0.072f, 0.520f, 0.048f),
        };

        private static readonly Vector3[] Popliteal =
        {
            V(0.072f, 0.520f, 0.048f), V(0.080f, 0.490f, 0.058f), V(0.085f, 0.455f, 0.058f), V(0.085f, 0.430f, 0.050f),
        };

        private static readonly Vector3[] AnteriorTibial =
        {
            V(0.085f, 0.430f, 0.050f), V(0.094f, 0.410f, 0.030f), V(0.104f, 0.380f, 0.006f), V(0.106f, 0.300f, -0.010f),
            V(0.100f, 0.200f, -0.014f), V(0.094f, 0.120f, -0.012f), V(0.092f, 0.078f, -0.014f),
        };

        private static readonly Vector3[] PosteriorTibial =
        {
            V(0.085f, 0.430f, 0.050f), V(0.082f, 0.380f, 0.046f), V(0.080f, 0.300f, 0.040f), V(0.076f, 0.200f, 0.034f),
            V(0.070f, 0.120f, 0.030f), V(0.066f, 0.085f, 0.026f), V(0.062f, 0.060f, 0.030f),
        };

        private static void LegArteries()
        {
            _a.Add("FemoralArtery", "SYS_CV_FEMORAL", 0.0056f, 0.0048f, true, Femoral);
            _a.Add("DeepFemoralArtery", "SYS_CV_PROFUNDA_FEMORIS", 0.0040f, 0.0018f, true,
                V(0.072f, 0.850f, -0.044f), V(0.084f, 0.806f, -0.030f), V(0.100f, 0.744f, 0.030f), V(0.096f, 0.664f, 0.040f), V(0.088f, 0.590f, 0.044f));
            _a.Add("PoplitealArtery", "SYS_CV_POPLITEAL", 0.0048f, 0.0042f, true, Popliteal);
            _a.Add("AnteriorTibialArtery", "SYS_CV_ANT_TIBIAL", 0.0036f, 0.0026f, true, AnteriorTibial);
            _a.Add("DorsalisPedisArtery", "SYS_CV_DORSALIS_PEDIS", 0.0026f, 0.0016f, true,
                V(0.092f, 0.078f, -0.014f), V(0.094f, 0.058f, -0.044f), V(0.094f, 0.044f, -0.076f), V(0.090f, 0.036f, -0.104f));
            _a.Add("PosteriorTibialArtery", "SYS_CV_POST_TIBIAL", 0.0036f, 0.0028f, true, PosteriorTibial);
            _a.Add("PeronealArtery", "SYS_CV_PERONEAL", 0.0028f, 0.0016f, true,
                V(0.080f, 0.380f, 0.046f), V(0.100f, 0.340f, 0.040f), V(0.106f, 0.250f, 0.036f), V(0.106f, 0.150f, 0.030f), V(0.104f, 0.090f, 0.024f));

            // Plantar arteries: the two branches of the posterior tibial into the sole, which meet
            // in the plantar arch.
            _a.Add("MedialPlantarArtery", "SYS_CV_PLANTAR_ARTERY", 0.0022f, 0.0012f, true,
                V(0.062f, 0.060f, 0.030f), V(0.068f, 0.030f, 0.004f), V(0.074f, 0.014f, -0.040f), V(0.070f, 0.010f, -0.100f));
            _a.Add("LateralPlantarArtery", "SYS_CV_PLANTAR_ARTERY", 0.0024f, 0.0014f, true,
                V(0.062f, 0.060f, 0.030f), V(0.080f, 0.030f, 0.022f), V(0.092f, 0.014f, -0.008f), V(0.098f, 0.012f, -0.050f), V(0.086f, 0.011f, -0.086f), V(0.070f, 0.011f, -0.090f));
        }

        // ================================================================ arm arteries

        private static readonly Vector3[] Axillary =
        {
            V(0.100f, 1.428f, -0.014f), V(0.140f, 1.408f, -0.010f), V(0.172f, 1.380f, -0.006f),
        };

        private static readonly Vector3[] Brachial =
        {
            V(0.172f, 1.380f, -0.006f), V(0.170f, 1.300f, 0.000f), V(0.170f, 1.200f, 0.004f), V(0.176f, 1.124f, -0.004f), V(0.180f, 1.106f, -0.018f),
        };

        private static readonly Vector3[] Radial =
        {
            V(0.180f, 1.106f, -0.018f), V(0.198f, 1.060f, -0.016f), V(0.212f, 0.960f, -0.010f), V(0.224f, 0.880f, -0.010f), V(0.230f, 0.846f, -0.010f),
        };

        private static readonly Vector3[] Ulnar =
        {
            V(0.180f, 1.106f, -0.018f), V(0.184f, 1.070f, -0.016f), V(0.192f, 1.010f, -0.016f), V(0.198f, 0.940f, -0.012f), V(0.200f, 0.880f, -0.012f), V(0.198f, 0.846f, -0.012f),
        };

        private static void ArmArteries()
        {
            // Left subclavian leaves the arch; the right comes off the brachiocephalic trunk.
            _a.Add("SubclavianArtery_L", "SYS_CV_SUBCLAVIAN", 0.0068f, 0.0060f, false,
                V(0.024f, 1.408f, 0.004f), V(0.040f, 1.428f, 0.000f), V(0.066f, 1.436f, -0.012f), V(0.100f, 1.428f, -0.014f));
            _a.Add("SubclavianArtery_R", "SYS_CV_SUBCLAVIAN", 0.0068f, 0.0060f, false,
                V(-0.030f, 1.438f, -0.030f), V(-0.052f, 1.438f, -0.022f), V(-0.078f, 1.436f, -0.012f), V(-0.100f, 1.428f, -0.014f));
            _a.Add("AxillaryArtery", "SYS_CV_AXILLARY", 0.0058f, 0.0052f, true, Axillary);
            _a.Add("BrachialArtery", "SYS_CV_BRACHIAL", 0.0050f, 0.0042f, true, Brachial);
            _a.Add("DeepBrachialArtery", "SYS_CV_PROFUNDA_BRACHII", 0.0028f, 0.0012f, true,
                V(0.172f, 1.350f, 0.000f), V(0.168f, 1.316f, 0.020f), V(0.180f, 1.270f, 0.030f), V(0.196f, 1.222f, 0.024f));
            _a.Add("RadialArtery", "SYS_CV_RADIAL", 0.0032f, 0.0024f, true, Radial);
            _a.Add("UlnarArtery", "SYS_CV_ULNAR", 0.0034f, 0.0026f, true, Ulnar);

            // Palmar arches: the ulnar artery completes a superficial arch across the palm, the
            // radial a deep one; each gives off the arteries of the fingers.
            _a.Add("SuperficialPalmarArch", "SYS_CV_PALMAR_ARCH", 0.0024f, 0.0022f, true,
                V(0.198f, 0.846f, -0.012f), V(0.196f, 0.816f, -0.016f), V(0.206f, 0.796f, -0.020f), V(0.220f, 0.796f, -0.020f), V(0.234f, 0.806f, -0.018f));
            _a.Add("DeepPalmarArch", "SYS_CV_PALMAR_ARCH", 0.0022f, 0.0020f, true,
                V(0.230f, 0.846f, -0.010f), V(0.232f, 0.824f, -0.006f), V(0.220f, 0.812f, 0.000f), V(0.204f, 0.812f, 0.000f));
            for (int i = 0; i < 4; i++)
            {
                Vector3[] joints = SkeletonBuilder.FingerPath(i);
                var digital = new[] { joints[1], joints[2], joints[3], joints[4] };
                var path = new List<Vector3> { V(joints[0].x, 0.796f, -0.020f) };
                foreach (Vector3 j in digital) path.Add(j + V(0.002f, 0f, -0.0075f));
                _a.Add($"DigitalArtery_{i + 1}", "SYS_CV_DIGITAL_ARTERY", 0.0015f, 0.0007f, true, path.ToArray());
            }
            Vector3[] thumb = SkeletonBuilder.ThumbPath();
            _a.Add("DigitalArtery_Thumb", "SYS_CV_DIGITAL_ARTERY", 0.0018f, 0.0007f, true,
                V(0.234f, 0.806f, -0.018f), thumb[1] + V(0.002f, 0f, -0.006f), thumb[2] + V(0.002f, 0f, -0.006f), Vector3.Lerp(thumb[2], thumb[3], 0.85f) + V(0.002f, 0f, -0.006f));
        }

        // ================================================================ veins

        private static void HeadNeckVeins()
        {
            // Internal jugular: from the skull base beside the carotid down to meet the subclavian.
            _v.Add("InternalJugularVein", "SYS_CV_JUGULAR", 0.0058f, 0.0072f, true,
                V(0.040f, 1.602f, 0.018f), V(0.046f, 1.560f, 0.002f), V(0.046f, 1.500f, -0.016f), V(0.048f, 1.452f, -0.024f), V(0.050f, 1.430f, -0.028f));
            _v.Add("ExternalJugularVein", "SYS_CV_JUGULAR_EXT", 0.0030f, 0.0036f, true,
                UnderSkinAll(0.004f, V(0.064f, 1.566f, -0.008f), V(0.060f, 1.520f, -0.014f), V(0.062f, 1.480f, -0.020f), V(0.064f, 1.446f, -0.026f), V(0.062f, 1.432f, -0.030f)));

            // Dural venous sinuses: blood channels between the layers of the dura mater that drain
            // the brain into the jugular veins.
            _v.Add("SuperiorSagittalSinus", "SYS_CV_DURAL_SINUS", 0.0032f, 0.0040f, false,
                V(0f, 1.706f, -0.070f), V(0f, 1.734f, -0.030f), V(0f, 1.740f, 0.030f), V(0f, 1.708f, 0.086f), V(0f, 1.676f, 0.094f));
            _v.Add("TransverseSigmoidSinus", "SYS_CV_DURAL_SINUS", 0.0036f, 0.0046f, true,
                V(0.000f, 1.676f, 0.094f), V(0.030f, 1.660f, 0.094f), V(0.058f, 1.640f, 0.072f), V(0.066f, 1.624f, 0.048f), V(0.052f, 1.608f, 0.030f), V(0.040f, 1.602f, 0.018f));
        }

        private static void TrunkVeins()
        {
            // Brachiocephalic veins join to form the superior vena cava, which enters the right atrium.
            _v.Add("BrachiocephalicVein_R", "SYS_CV_BRACHIOCEPHALIC_VEIN", 0.0060f, 0.0070f, false,
                V(-0.050f, 1.428f, -0.028f), V(-0.040f, 1.420f, -0.032f), V(-0.028f, 1.410f, -0.032f));
            _v.Add("BrachiocephalicVein_L", "SYS_CV_BRACHIOCEPHALIC_VEIN", 0.0060f, 0.0070f, false,
                V(0.050f, 1.428f, -0.028f), V(0.030f, 1.422f, -0.044f), V(0.004f, 1.416f, -0.048f), V(-0.018f, 1.412f, -0.042f), V(-0.028f, 1.410f, -0.032f));
            _v.Add("SuperiorVenaCava", "SYS_CV_SVC", 0.0086f, 0.0092f, false,
                V(-0.028f, 1.410f, -0.032f), V(-0.029f, 1.372f, -0.034f), V(-0.029f, 1.338f, -0.040f), V(-0.027f, 1.312f, -0.046f));

            // Inferior vena cava: up the right of the aorta, grooving the back of the liver, through
            // the diaphragm at T8 to the right atrium.
            _v.Add("InferiorVenaCava", "SYS_CV_IVC", 0.0090f, 0.0118f, false,
                V(-0.018f, 1.004f, 0.020f), V(-0.026f, 1.050f, 0.022f), V(-0.030f, 1.100f, 0.026f), V(-0.032f, 1.140f, 0.020f),
                V(-0.031f, 1.184f, 0.008f), V(-0.030f, 1.218f, -0.010f), V(-0.028f, 1.250f, -0.030f), V(-0.027f, 1.272f, -0.040f));

            _v.Add("RenalVein_L", "SYS_CV_RENAL_VEIN", 0.0050f, 0.0060f, false,
                V(0.050f, 1.112f, 0.046f), V(0.026f, 1.114f, 0.022f), V(0.004f, 1.114f, -0.004f), V(-0.014f, 1.112f, 0.006f), V(-0.028f, 1.110f, 0.020f));
            _v.Add("RenalVein_R", "SYS_CV_RENAL_VEIN", 0.0050f, 0.0060f, false,
                V(-0.050f, 1.094f, 0.046f), V(-0.040f, 1.096f, 0.036f), V(-0.030f, 1.098f, 0.028f));
            _v.Add("Azygos", "SYS_CV_AZYGOS", 0.0030f, 0.0040f, false,
                V(-0.010f, 1.100f, 0.028f), V(-0.013f, 1.160f, 0.034f), V(-0.015f, 1.240f, 0.040f), V(-0.016f, 1.320f, 0.040f),
                V(-0.020f, 1.372f, 0.034f), V(-0.026f, 1.394f, 0.014f), V(-0.029f, 1.394f, -0.012f), V(-0.029f, 1.384f, -0.030f));
            _v.Add("IntercostalVeins", "SYS_CV_AZYGOS", 0.0010f, 0.0010f, false,
                V(-0.024f, 1.322f, 0.040f), V(-0.040f, 1.324f, 0.044f), V(-0.062f, 1.328f, 0.040f));

            // Hepatic veins: the liver's three outflow channels into the vena cava; and the portal
            // system - splenic and mesenteric veins joining behind the pancreas to make the portal
            // vein, which carries the gut's blood to the liver before it returns to the heart.
            _v.Add("HepaticVein_Right", "SYS_CV_HEPATIC_VEIN", 0.0050f, 0.0060f, false,
                V(-0.092f, 1.150f, 0.010f), V(-0.062f, 1.176f, 0.008f), V(-0.034f, 1.204f, 0.000f), V(-0.030f, 1.214f, -0.008f));
            _v.Add("HepaticVein_Middle", "SYS_CV_HEPATIC_VEIN", 0.0040f, 0.0052f, false,
                V(-0.050f, 1.136f, -0.046f), V(-0.044f, 1.168f, -0.032f), V(-0.034f, 1.198f, -0.014f), V(-0.030f, 1.214f, -0.008f));
            _v.Add("HepaticVein_Left", "SYS_CV_HEPATIC_VEIN", 0.0036f, 0.0048f, false,
                V(0.004f, 1.176f, -0.052f), V(-0.010f, 1.196f, -0.038f), V(-0.026f, 1.212f, -0.016f), V(-0.030f, 1.216f, -0.008f));
            _v.Add("SplenicVein", "SYS_CV_SPLENIC_VEIN", 0.0040f, 0.0056f, false,
                V(0.092f, 1.150f, 0.030f), V(0.070f, 1.142f, 0.018f), V(0.040f, 1.128f, -0.002f), V(0.014f, 1.116f, -0.020f), V(-0.008f, 1.104f, -0.030f));
            _v.Add("SuperiorMesentericVein", "SYS_CV_SMV", 0.0044f, 0.0058f, false,
                V(-0.040f, 0.964f, -0.040f), V(-0.010f, 0.994f, -0.032f), V(0.000f, 1.040f, -0.024f), V(-0.004f, 1.078f, -0.026f), V(-0.008f, 1.104f, -0.030f));
            _v.Add("InferiorMesentericVein", "SYS_CV_IMV", 0.0026f, 0.0034f, false,
                V(0.034f, 0.980f, -0.002f), V(0.032f, 1.030f, 0.000f), V(0.030f, 1.078f, -0.008f), V(0.020f, 1.108f, -0.020f));
            _v.Add("PortalVein", "SYS_CV_PORTAL_VEIN", 0.0064f, 0.0060f, false,
                V(-0.008f, 1.104f, -0.030f), V(-0.022f, 1.116f, -0.034f), V(-0.040f, 1.132f, -0.040f), V(-0.052f, 1.144f, -0.044f));
            _v.Add("PortalVein_RightBranch", "SYS_CV_PORTAL_VEIN", 0.0044f, 0.0014f, false,
                V(-0.052f, 1.144f, -0.044f), V(-0.076f, 1.152f, -0.030f), V(-0.100f, 1.156f, -0.014f), V(-0.116f, 1.164f, -0.004f));
            _v.Add("PortalVein_LeftBranch", "SYS_CV_PORTAL_VEIN", 0.0040f, 0.0014f, false,
                V(-0.052f, 1.144f, -0.044f), V(-0.034f, 1.164f, -0.056f), V(-0.010f, 1.180f, -0.060f), V(0.014f, 1.190f, -0.062f));
        }

        private static void ArmVeins()
        {
            _v.Add("SubclavianVein", "SYS_CV_SUBCLAVIAN_VEIN", 0.0064f, 0.0070f, true,
                V(0.100f, 1.428f, -0.026f), V(0.070f, 1.428f, -0.030f), V(0.050f, 1.428f, -0.028f));
            _v.Add("AxillaryVein", "SYS_CV_AXILLARY_VEIN", 0.0060f, 0.0064f, true,
                V(0.172f, 1.378f, -0.018f), V(0.140f, 1.410f, -0.024f), V(0.100f, 1.428f, -0.026f));
            _v.Add("BrachialVein", "SYS_CV_BRACHIAL_VEIN", 0.0046f, 0.0060f, true,
                Beside(new[] { V(0.230f, 0.846f, -0.010f), V(0.212f, 0.960f, -0.010f), V(0.196f, 1.060f, -0.016f), V(0.180f, 1.106f, -0.018f),
                    V(0.176f, 1.124f, -0.004f), V(0.170f, 1.200f, 0.004f), V(0.170f, 1.300f, 0.000f), V(0.172f, 1.380f, -0.006f) }, -0.010f, -0.010f));

            // Superficial veins, seen through the skin of the arm: the cephalic up the thumb side
            // (into the axillary vein by the deltopectoral groove), the basilic up the little-finger
            // side, and the median cubital vein linking them across the elbow - where blood is drawn.
            var cephalic = new List<Vector3>
            {
                V(0.240f, 0.808f, 0.024f), V(0.236f, 0.870f, 0.020f),
                BodyShape.Under(BodyShape.Forearm, 0.960f, 62f, 0.004f), BodyShape.Under(BodyShape.Forearm, 1.050f, 58f, 0.004f),
                BodyShape.Under(BodyShape.UpperArm, 1.120f, 40f, 0.004f), BodyShape.Under(BodyShape.UpperArm, 1.220f, 44f, 0.004f),
                BodyShape.Under(BodyShape.UpperArm, 1.320f, 30f, 0.004f), V(0.150f, 1.420f, -0.040f), V(0.128f, 1.424f, -0.032f),
            };
            _v.Add("CephalicVein", "SYS_CV_CEPHALIC", 0.0034f, 0.0046f, true, UnderSkinAll(0.004f, cephalic.ToArray()));
            var basilic = new List<Vector3>
            {
                V(0.190f, 0.808f, 0.028f), V(0.192f, 0.870f, 0.024f),
                BodyShape.Under(BodyShape.Forearm, 0.960f, 280f, 0.004f), BodyShape.Under(BodyShape.Forearm, 1.050f, 292f, 0.004f),
                BodyShape.Under(BodyShape.UpperArm, 1.130f, 290f, 0.004f), BodyShape.Under(BodyShape.UpperArm, 1.220f, 284f, 0.005f),
                BodyShape.Under(BodyShape.UpperArm, 1.290f, 278f, 0.012f), V(0.166f, 1.360f, -0.012f),
            };
            _v.Add("BasilicVein", "SYS_CV_BASILIC", 0.0034f, 0.0046f, true, UnderSkinAll(0.004f, basilic.ToArray()));
            _v.Add("MedianCubitalVein", "SYS_CV_MEDIAN_CUBITAL", 0.0028f, 0.0028f, true,
                UnderSkinAll(0.004f, BodyShape.Under(BodyShape.UpperArm, 1.120f, 40f, 0.004f), V(0.186f, 1.104f, -0.040f), BodyShape.Under(BodyShape.UpperArm, 1.100f, 300f, 0.004f)));
        }

        private static void LegVeins()
        {
            _v.Add("ExternalIliacVein", "SYS_CV_ILIAC_VEIN_EXT", 0.0070f, 0.0075f, true,
                Beside(new[] { V(0.040f, 0.958f, 0.010f), V(0.058f, 0.926f, -0.010f), V(0.070f, 0.890f, -0.034f) }, -0.012f, 0.004f));
            // The two common iliac veins join at L5 to make the inferior vena cava; the left, longer one
            // crosses behind the right common iliac artery to reach it.
            _v.Add("CommonIliacVein_L", "SYS_CV_ILIAC_VEIN", 0.0070f, 0.0085f, false,
                V(0.036f, 0.962f, 0.018f), V(0.012f, 0.988f, 0.024f), V(-0.014f, 1.004f, 0.022f));
            _v.Add("CommonIliacVein_R", "SYS_CV_ILIAC_VEIN", 0.0070f, 0.0085f, false,
                V(-0.036f, 0.962f, 0.018f), V(-0.028f, 0.988f, 0.022f), V(-0.018f, 1.004f, 0.022f));
            _v.Add("FemoralVein", "SYS_CV_FEMORAL_VEIN", 0.0072f, 0.0056f, true, Beside(Femoral, -0.010f, 0.004f));
            _v.Add("PoplitealVein", "SYS_CV_POPLITEAL_VEIN", 0.0056f, 0.0050f, true, Beside(Popliteal, 0.000f, 0.008f));
            _v.Add("PosteriorTibialVeins", "SYS_CV_TIBIAL_VEIN", 0.0040f, 0.0028f, true, Beside(PosteriorTibial, -0.008f, 0.006f));
            _v.Add("AnteriorTibialVeins", "SYS_CV_TIBIAL_VEIN", 0.0036f, 0.0026f, true, Beside(AnteriorTibial, -0.008f, -0.004f));

            // Great saphenous vein: the body's longest vein, from the medial foot up the inside of
            // the leg and thigh to the groin, just under the skin. Small saphenous: up the back of
            // the calf to the popliteal vein.
            var great = new List<Vector3> { V(0.066f, 0.044f, -0.048f), V(0.060f, 0.078f, -0.018f) };
            foreach (float y in new[] { 0.150f, 0.260f, 0.370f }) great.Add(BodyShape.Under(BodyShape.Shank, y, 285f, 0.004f));
            great.Add(BodyShape.Under(BodyShape.Thigh, 0.470f, 300f, 0.004f));
            foreach (float y in new[] { 0.560f, 0.680f, 0.800f }) great.Add(BodyShape.Under(BodyShape.Thigh, y, 320f, 0.004f));
            great.Add(V(0.062f, 0.876f, -0.056f));
            great.Reverse();
            _v.Add("GreatSaphenousVein", "SYS_CV_SAPHENOUS", 0.0058f, 0.0032f, true, UnderSkinAll(0.009f, great.ToArray()));

            var small = new List<Vector3> { V(0.110f, 0.066f, 0.014f) };
            foreach (float y in new[] { 0.130f, 0.220f, 0.330f, 0.420f }) small.Add(BodyShape.Under(BodyShape.Shank, y, 180f, 0.004f));
            small.Add(V(0.086f, 0.500f, 0.066f));
            _v.Add("SmallSaphenousVein", "SYS_CV_SAPHENOUS_SMALL", 0.0026f, 0.0040f, true, UnderSkinAll(0.008f, small.ToArray()));

            _v.Add("DorsalVenousArch", "SYS_CV_DORSAL_VENOUS_ARCH", 0.0020f, 0.0020f, true,
                V(0.066f, 0.044f, -0.048f), V(0.076f, 0.052f, -0.086f), V(0.092f, 0.052f, -0.090f), V(0.106f, 0.046f, -0.070f), V(0.110f, 0.040f, -0.040f));
        }

        // ================================================================ lymphatic

        private static void ThoracicDuct()
        {
            // The thoracic duct: from the cisterna chyli at L1-L2 up the front of the spine between
            // the aorta and the azygos vein, crossing to the left at T5 and emptying into the junction
            // of the left internal jugular and subclavian veins. It returns lymph from most of the body.
            _l.Add("ThoracicDuct", "SYS_LYMPH_THORACIC_DUCT", Loft.Taper(0.0030f, 0.0020f), false,
                V(-0.002f, 1.112f, 0.028f), V(0.000f, 1.170f, 0.036f), V(0.001f, 1.250f, 0.041f), V(0.008f, 1.340f, 0.043f),
                V(0.016f, 1.400f, 0.038f), V(0.030f, 1.438f, 0.024f), V(0.042f, 1.446f, 0.000f), V(0.050f, 1.436f, -0.024f));
        }
    }
}
