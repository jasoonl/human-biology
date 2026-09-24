using System.Collections.Generic;
using HumanBodyExplorer.CameraSystem;
using HumanBodyExplorer.EditorTools.Geometry;
using HumanBodyExplorer.Core;
using HumanBodyExplorer.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// Builds the classroom-facing explorer: a full primitive humanoid figure
    /// with tagged bones/organs/muscles positioned anatomically under a
    /// skin-covered head/torso silhouette, plus an info panel and quiz HUD with
    /// live timer and on-figure flash feedback. No licensed anatomical meshes
    /// exist in this project, so parts are primitives (spheres/capsules) - not
    /// photorealistic, but correctly named, positioned relative to each other,
    /// and wired to real educational descriptions and gameplay.
    /// </summary>
    public static partial class HumanBodyExplorerSetup
    {
        private struct PartDef
        {
            public string Name;
            public string EntityId;
            public PrimitiveType Shape;
            public Vector3 LocalPosition;
            public Vector3 LocalScale;

            /// <summary>Euler angles; defaults to zero. Needed for structures that do
            /// not run along a cardinal axis - the clavicles, the transverse colon and
            /// the pancreas all lie roughly horizontally, and the heart is tilted with
            /// its apex pointing down and to the anatomical left.</summary>
            public Vector3 LocalEuler;

            public Color Color;
            public float Smoothness;

            /// <summary>Squishy anatomy (organs/muscles/brain) gets a noise-displaced,
            /// non-perfect-sphere mesh so it doesn't read as a plastic ball; bone and
            /// skin stay smooth/hard-surfaced. Only applies to Sphere-shaped parts.</summary>
            public bool Organic;
        }

        // Tissue colours chosen to match how these structures actually appear in
        // dissection and surgical photography, rather than arbitrary bright hues -
        // fresh bone is ivory rather than white, lungs are grey-pink rather than
        // candy pink, and the gallbladder really is green from the bile it holds.
        /// <summary>Backdrop behind the figure. Kept next to the tissue colours
        /// deliberately: it has to be checked against them, and when it was not, it
        /// ended up within 0.05 of BoneColor and the skeleton vanished into it.
        /// A near-black navy fixed that (dE >= 62 from every tissue) but read as too
        /// dark on screen - closer to a void than a studio backdrop. This mid slate
        /// keeps hue separated from muscle/heart/vessels while lifting lightness to
        /// L* ~30 (navy was L* ~12), which is what actually reads as "backdrop" rather
        /// than "no background at all". Still dE >= 43 from every tissue.</summary>
        private static readonly Color BackdropColor = new Color(0.24f, 0.28f, 0.35f);

        private static readonly Color BoneColor = new Color(0.93f, 0.90f, 0.83f);
        internal static readonly Color MuscleColor = new Color(0.70f, 0.23f, 0.19f);
        private static readonly Color HeartColor = new Color(0.62f, 0.14f, 0.13f);
        private static readonly Color LungColor = new Color(0.80f, 0.57f, 0.56f);
        private static readonly Color CartilageColor = new Color(0.85f, 0.86f, 0.83f);
        private static readonly Color NerveColor = new Color(0.90f, 0.87f, 0.80f);
        private static readonly Color BrainColor = new Color(0.85f, 0.76f, 0.73f);
        private static readonly Color RenalColor = new Color(0.52f, 0.21f, 0.19f);
        private static readonly Color EndoColor = new Color(0.72f, 0.55f, 0.40f);
        private static readonly Color ThyroidColor = new Color(0.64f, 0.30f, 0.26f);
        private static readonly Color LymphColor = new Color(0.42f, 0.15f, 0.22f);
        private static readonly Color ThymusColor = new Color(0.84f, 0.72f, 0.68f);
        private static readonly Color LiverColor = new Color(0.44f, 0.18f, 0.15f);
        private static readonly Color StomachColor = new Color(0.82f, 0.60f, 0.52f);
        private static readonly Color GutColor = new Color(0.80f, 0.62f, 0.50f);
        private static readonly Color ColonColor = new Color(0.76f, 0.58f, 0.45f);
        private static readonly Color PancreasColor = new Color(0.83f, 0.72f, 0.52f);
        private static readonly Color BileColor = new Color(0.32f, 0.45f, 0.24f);
        private static readonly Color BladderColor = new Color(0.80f, 0.76f, 0.60f);
        private static readonly Color SkinColor = new Color(0.87f, 0.70f, 0.56f);
        private static readonly Color ArteryColor = new Color(0.75f, 0.08f, 0.08f);
        private static readonly Color VeinColor = new Color(0.16f, 0.26f, 0.52f);

        private const float OrganGloss = 0.42f;
        private const float MuscleGloss = 0.3f;
        private const float BoneGloss = 0.12f;
        private const float SkinGloss = 0.18f;

        private static readonly PartDef[] Parts =
        {
            // ================= INTEGUMENTARY: translucent skin shell =================
            // Drawn alpha-blended, and the raycaster treats see-through surfaces as
            // click-through, so the shell can genuinely enclose the body instead of
            // sitting behind it as a flat backdrop.
            // The deltoid cap is the widest point of the body (bideltoid breadth
            // ~0.50 m), so the shoulder needs its own skin segment rather than being
            // squeezed under the chest ellipsoid.

            // ================= SKELETAL =================
            // Near-horizontal struts from sternum to acromion.

            // Radius (lateral, thumb side) and ulna (medial) as separate bones.
            // Scapulae: flat blades riding on the back of the rib cage.

            // --- Deeper muscle layer ---

            // --- Peripheral nerves, so the Nerves layer shows an actual network ---
            new PartDef { Name = "SciaticNerve_L", EntityId = "SYS_NERV_SCIATIC", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.082f, 0.70f, 0.040f), LocalScale = new Vector3(0.014f, 0.18f, 0.014f), Color = NerveColor, Smoothness = OrganGloss },
            new PartDef { Name = "SciaticNerve_R", EntityId = "SYS_NERV_SCIATIC", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.082f, 0.70f, 0.040f), LocalScale = new Vector3(0.014f, 0.18f, 0.014f), Color = NerveColor, Smoothness = OrganGloss },
            new PartDef { Name = "BrachialPlexus_L", EntityId = "SYS_NERV_BRACHIAL_PLEXUS", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.115f, 1.395f, 0.012f), LocalScale = new Vector3(0.016f, 0.045f, 0.016f),
                LocalEuler = new Vector3(0f, 0f, 52f), Color = NerveColor, Smoothness = OrganGloss },
            new PartDef { Name = "BrachialPlexus_R", EntityId = "SYS_NERV_BRACHIAL_PLEXUS", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.115f, 1.395f, 0.012f), LocalScale = new Vector3(0.016f, 0.045f, 0.016f),
                LocalEuler = new Vector3(0f, 0f, -52f), Color = NerveColor, Smoothness = OrganGloss },
            new PartDef { Name = "VagusNerve_L", EntityId = "SYS_NERV_VAGUS", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.028f, 1.37f, 0.020f), LocalScale = new Vector3(0.009f, 0.13f, 0.009f), Color = NerveColor, Smoothness = OrganGloss },
            new PartDef { Name = "VagusNerve_R", EntityId = "SYS_NERV_VAGUS", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.028f, 1.37f, 0.020f), LocalScale = new Vector3(0.009f, 0.13f, 0.009f), Color = NerveColor, Smoothness = OrganGloss },

            // --- Great vessels of the neck ---
            new PartDef { Name = "CarotidArtery_L", EntityId = "SYS_CV_CAROTID", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.026f, 1.452f, -0.014f), LocalScale = new Vector3(0.014f, 0.048f, 0.014f), Color = ArteryColor, Smoothness = OrganGloss },
            new PartDef { Name = "CarotidArtery_R", EntityId = "SYS_CV_CAROTID", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.026f, 1.452f, -0.014f), LocalScale = new Vector3(0.014f, 0.048f, 0.014f), Color = ArteryColor, Smoothness = OrganGloss },
            new PartDef { Name = "JugularVein_L", EntityId = "SYS_CV_JUGULAR", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.042f, 1.452f, -0.020f), LocalScale = new Vector3(0.016f, 0.048f, 0.016f), Color = VeinColor, Smoothness = OrganGloss },
            new PartDef { Name = "JugularVein_R", EntityId = "SYS_CV_JUGULAR", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.042f, 1.452f, -0.020f), LocalScale = new Vector3(0.016f, 0.048f, 0.016f), Color = VeinColor, Smoothness = OrganGloss },

            // ================= MUSCULAR =================
            // The calf sits behind the leg (+z is posterior here).
            // Domed sheet at the thoracic/abdominal boundary, just under the lung bases.
            new PartDef { Name = "Diaphragm", EntityId = "SYS_RESP_DIAPHRAGM", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.205f, -0.005f), LocalScale = new Vector3(0.26f, 0.085f, 0.175f), Color = MuscleColor, Smoothness = MuscleGloss, Organic = true },


            // --- Major muscle groups filling out the figure (back, flank, posterior limb) ---

            // ================= NERVOUS =================
            new PartDef { Name = "Cerebrum", EntityId = "SYS_NERV_BRAIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.665f, 0f), LocalScale = new Vector3(0.125f, 0.115f, 0.15f), Color = BrainColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Cerebellum", EntityId = "SYS_NERV_CEREBELLUM", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.585f, 0.055f), LocalScale = new Vector3(0.085f, 0.05f, 0.06f), Color = BrainColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Brainstem", EntityId = "SYS_NERV_BRAINSTEM", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0f, 1.565f, 0.015f), LocalScale = new Vector3(0.028f, 0.035f, 0.028f), Color = NerveColor, Smoothness = OrganGloss },
            // Ends at L1-L2 (~0.92), well above the end of the vertebral column.
            new PartDef { Name = "SpinalCord", EntityId = "SYS_NERV_SPINALCORD", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0f, 1.21f, 0.06f), LocalScale = new Vector3(0.018f, 0.29f, 0.018f), Color = NerveColor, Smoothness = OrganGloss },

            // ================= CARDIOVASCULAR =================
            // Two thirds of the heart lies left of midline, tilted with the apex down
            // and to the anatomical left (+x here, since the figure faces the camera).
            new PartDef { Name = "Heart", EntityId = "SYS_CV_HEART", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.025f, 1.275f, -0.045f), LocalScale = new Vector3(0.115f, 0.135f, 0.10f),
                LocalEuler = new Vector3(0f, 0f, 20f), Color = HeartColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Aorta_Ascending", EntityId = "SYS_CV_AORTA", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.005f, 1.345f, -0.025f), LocalScale = new Vector3(0.032f, 0.045f, 0.032f), Color = ArteryColor, Smoothness = OrganGloss },
            new PartDef { Name = "Aorta_Descending", EntityId = "SYS_CV_AORTA", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.01f, 1.14f, 0.045f), LocalScale = new Vector3(0.028f, 0.15f, 0.028f), Color = ArteryColor, Smoothness = OrganGloss },
            new PartDef { Name = "VenaCava", EntityId = "SYS_CV_VEIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.035f, 1.17f, 0.03f), LocalScale = new Vector3(0.026f, 0.155f, 0.026f), Color = VeinColor, Smoothness = OrganGloss },
            // The rest of the vascular tree - subclavian through radial/ulnar in each
            // arm, common iliac through anterior/posterior tibial in each leg, the
            // aortic arch, coronaries, pulmonary vessels, and the gut's portal system -
            // is real named anatomy rather than one undifferentiated tube per limb, so
            // it is generated by BuildVascularSystem/BuildTrunkVasculature below instead
            // of listed here.

            // ================= RESPIRATORY =================
            // Right lung is the larger of the two (3 lobes); the left is smaller and
            // notched to make room for the heart.
            new PartDef { Name = "Lung_R", EntityId = "SYS_RESP_LUNG_R", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.105f, 1.315f, -0.005f), LocalScale = new Vector3(0.135f, 0.255f, 0.155f), Color = LungColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Lung_L", EntityId = "SYS_RESP_LUNG_L", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.105f, 1.315f, -0.005f), LocalScale = new Vector3(0.125f, 0.255f, 0.15f), Color = LungColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Trachea", EntityId = "SYS_RESP_TRACHEA", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0f, 1.44f, -0.025f), LocalScale = new Vector3(0.026f, 0.055f, 0.026f), Color = CartilageColor, Smoothness = OrganGloss },

            // ================= DIGESTIVE =================
            // Behind the trachea, in front of the vertebral column.
            new PartDef { Name = "Esophagus", EntityId = "SYS_DIG_ESOPHAGUS", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0f, 1.335f, 0.04f), LocalScale = new Vector3(0.022f, 0.115f, 0.022f), Color = GutColor, Smoothness = OrganGloss },
            new PartDef { Name = "Stomach", EntityId = "SYS_DIG_STOMACH", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.065f, 1.16f, -0.045f), LocalScale = new Vector3(0.135f, 0.125f, 0.085f), Color = StomachColor, Smoothness = OrganGloss, Organic = true },
            // Right upper quadrant, the largest abdominal organ.
            new PartDef { Name = "Liver", EntityId = "SYS_DIG_LIVER", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.0693f, 1.18f, -0.0376f), LocalScale = new Vector3(0.1647f, 0.1035f, 0.1223f), Color = LiverColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Gallbladder", EntityId = "SYS_DIG_GALLBLADDER", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.065f, 1.125f, -0.075f), LocalScale = new Vector3(0.038f, 0.055f, 0.035f), Color = BileColor, Smoothness = OrganGloss, Organic = true },
            // Lies transversely across L1-L2, hence the rotation.
            new PartDef { Name = "Pancreas", EntityId = "SYS_DIG_PANCREAS", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.015f, 1.115f, 0.025f), LocalScale = new Vector3(0.03f, 0.055f, 0.03f),
                LocalEuler = new Vector3(0f, 0f, 80f), Color = PancreasColor, Smoothness = OrganGloss },
            new PartDef { Name = "SmallIntestine", EntityId = "SYS_DIG_SMALL_INTESTINE", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.01f, -0.05f), LocalScale = new Vector3(0.20f, 0.15f, 0.115f), Color = GutColor, Smoothness = OrganGloss, Organic = true },
            // The colon frames the small intestine: up the right side, across, down the left.
            new PartDef { Name = "Colon_Ascending", EntityId = "SYS_DIG_LARGE_INTESTINE", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.115f, 1.015f, -0.02f), LocalScale = new Vector3(0.05f, 0.065f, 0.05f), Color = ColonColor, Smoothness = OrganGloss },
            new PartDef { Name = "Colon_Transverse", EntityId = "SYS_DIG_LARGE_INTESTINE", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0f, 1.10f, -0.045f), LocalScale = new Vector3(0.05f, 0.11f, 0.05f),
                LocalEuler = new Vector3(0f, 0f, 90f), Color = ColonColor, Smoothness = OrganGloss },
            new PartDef { Name = "Colon_Descending", EntityId = "SYS_DIG_LARGE_INTESTINE", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.115f, 1.015f, -0.02f), LocalScale = new Vector3(0.05f, 0.065f, 0.05f), Color = ColonColor, Smoothness = OrganGloss },

            // ================= RENAL =================
            // Retroperitoneal, against the posterior wall; the right kidney sits lower
            // because the liver occupies the space above it.
            new PartDef { Name = "Kidney_L", EntityId = "SYS_REN_KIDNEY_L", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.07f, 1.125f, 0.055f), LocalScale = new Vector3(0.055f, 0.11f, 0.045f), Color = RenalColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Kidney_R", EntityId = "SYS_REN_KIDNEY_R", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.07f, 1.10f, 0.055f), LocalScale = new Vector3(0.055f, 0.11f, 0.045f), Color = RenalColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Ureter_L", EntityId = "SYS_REN_URETER", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.055f, 1.0f, 0.04f), LocalScale = new Vector3(0.011f, 0.055f, 0.011f), Color = GutColor, Smoothness = OrganGloss },
            new PartDef { Name = "Ureter_R", EntityId = "SYS_REN_URETER", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.055f, 0.99f, 0.04f), LocalScale = new Vector3(0.011f, 0.055f, 0.011f), Color = GutColor, Smoothness = OrganGloss },
            new PartDef { Name = "Bladder", EntityId = "SYS_REN_BLADDER", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 0.90f, -0.03f), LocalScale = new Vector3(0.075f, 0.065f, 0.065f), Color = BladderColor, Smoothness = OrganGloss, Organic = true },

            // ================= ENDOCRINE =================
            new PartDef { Name = "Thyroid", EntityId = "SYS_ENDO_THYROID", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.475f, -0.04f), LocalScale = new Vector3(0.055f, 0.03f, 0.03f), Color = ThyroidColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Adrenal_L", EntityId = "SYS_ENDO_ADRENAL", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.07f, 1.185f, 0.055f), LocalScale = new Vector3(0.035f, 0.022f, 0.03f), Color = EndoColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Adrenal_R", EntityId = "SYS_ENDO_ADRENAL", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.07f, 1.16f, 0.055f), LocalScale = new Vector3(0.035f, 0.022f, 0.03f), Color = EndoColor, Smoothness = OrganGloss, Organic = true },
            // Pea-sized, in the sella turcica beneath the cerebrum.
            new PartDef { Name = "Pituitary", EntityId = "SYS_ENDO_PITUITARY", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.60f, 0.015f), LocalScale = new Vector3(0.016f, 0.016f, 0.016f), Color = EndoColor, Smoothness = OrganGloss, Organic = true },

            // ================= LYMPHATIC =================
            new PartDef { Name = "Spleen", EntityId = "SYS_LYMPH_SPLEEN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.115f, 1.21f, 0.03f), LocalScale = new Vector3(0.055f, 0.095f, 0.045f), Color = LymphColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Thymus", EntityId = "SYS_LYMPH_THYMUS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.365f, -0.085f), LocalScale = new Vector3(0.055f, 0.075f, 0.03f), Color = ThymusColor, Smoothness = OrganGloss, Organic = true },
        };

        /// <summary>Greys the menu item out during Play mode - see BuildExplorer for why.</summary>
        [MenuItem("Human Body Explorer/Build Full Explorer (Figure + UI)", true)]
        private static bool ValidateBuildExplorer() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("Human Body Explorer/Build Full Explorer (Figure + UI)")]
        public static void BuildExplorer()
        {
            // Unity throws away every scene edit made during Play mode: on exit it
            // reloads the backup it took on entry. Building from Play mode therefore
            // appears to work - the figure is right there in the Scene view - and then
            // silently vanishes the moment you press Stop, so the next Play session
            // shows the old figure and the rebuild looks like it did nothing at all.
            // Refuse outright rather than let that happen.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[HumanBodyExplorerSetup] Exit Play mode before building. " +
                               "Unity discards all scene changes made during Play mode when you press Stop, " +
                               "so the rebuilt figure would be thrown away and you'd see the old one.");
                return;
            }

            var mainCameraGO = GameObject.FindWithTag("MainCamera");
            if (mainCameraGO == null)
            {
                Debug.LogError("[HumanBodyExplorerSetup] No Main Camera found. Open Bootstrap.unity first.");
                return;
            }

            RemoveIfExists("DemoHeart_LeftVentricle"); // superseded by the full figure
            RemoveIfExists("HumanBodyRoot");

            CreateGroundAndLight();
            GameObject root = BuildHumanFigure();
            ReportSkinContainment(root);
            WireUpCamera(mainCameraGO, root);
            SetupPostProcessing(mainCameraGO);
            AnatomyOutlineFeatureSetup.AddFeatureToActiveRenderer();
            BuildUI(mainCameraGO);

            // Save, don't just dirty. An unsaved rebuild is lost to any later scene
            // reload, which is the same "nothing changed" failure by a slower route.
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            if (!string.IsNullOrEmpty(activeScene.path)) EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[HumanBodyExplorerSetup] Built the full explorer: " + Parts.Length +
                      " tagged body parts, info panel, and quiz mode with live timer + flash feedback. " +
                      "Scene saved. Press Play - the camera auto-frames the figure. Orbit with drag, " +
                      "zoom with scroll, click any part to learn about it, and press Start Quiz to test yourself.");
        }

        /// <summary>
        /// Headless-safe entry point for CI/automation: opens Bootstrap.unity (a
        /// plain -executeMethod call otherwise starts from an empty untitled scene
        /// with no tagged Main Camera, so BuildExplorer() bails out immediately),
        /// runs the normal build, and saves the result back to disk.
        /// </summary>
        [MenuItem("Human Body Explorer/Build Full Explorer (Headless, opens+saves Bootstrap)")]
        public static void BuildExplorerHeadless()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity");
            BuildExplorer();
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Nothing inside the body should poke through the skin. Every structure's vertices
        /// are tested against the skin's own surface function, and the worst offenders
        /// named - a misplaced organ is otherwise only found by a person spotting it.
        /// </summary>
        private static void ReportSkinContainment(GameObject root)
        {
            var offenders = SkinBuilder.ContainmentReport(root.transform);
            if (offenders.Count == 0)
            {
                Debug.Log("[HumanBodyExplorerSetup] Containment: every structure lies inside the skin.");
                return;
            }

            var lines = new System.Text.StringBuilder();
            int shown = 0;
            foreach (var (name, outside) in offenders)
            {
                if (shown++ >= 25) break;
                lines.AppendLine($"    {name}: {outside * 100f:F1} cm outside");
            }
            Debug.LogWarning($"[HumanBodyExplorerSetup] Containment: {offenders.Count} structures poke through the skin:\n{lines}");
        }

        private static void RemoveIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }

        private static void CreateGroundAndLight()
        {
            // The figure is built to real scale (1.75 m, soles at y = 0), so the floor
            // belongs at y = 0. Set it every rebuild rather than only on creation, so
            // an existing ground plane from an older layout gets corrected.
            // No ground plane: the figure is presented on plain background like an
            // anatomical plate, and a receding floor only added perspective clutter.
            RemoveIfExists("DemoGround");

            // Lit like an anatomical plate rather than a film set: a soft key from
            // the camera side, a fill from the opposite side, and strong ambient, so
            // every structure is legible instead of half of them falling into shadow.
            RemoveIfExists("Directional Light");

            if (GameObject.Find("KeyLight") == null)
            {
                var keyGO = new GameObject("KeyLight");
                var key = keyGO.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.05f;
                key.color = new Color(1f, 0.99f, 0.97f);
                key.shadows = LightShadows.None;
                keyGO.transform.rotation = Quaternion.Euler(28f, 18f, 0f);
            }

            if (GameObject.Find("FillLight") == null)
            {
                var fillGO = new GameObject("FillLight");
                var fill = fillGO.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.75f;
                fill.color = new Color(0.94f, 0.96f, 1f);
                fill.shadows = LightShadows.None;
                fillGO.transform.rotation = Quaternion.Euler(18f, 200f, 0f);
            }

            if (GameObject.Find("RimLight") == null)
            {
                var rimGO = new GameObject("RimLight");
                var rim = rimGO.AddComponent<Light>();
                rim.type = LightType.Directional;
                rim.intensity = 0.5f;
                rim.color = new Color(1f, 0.97f, 0.92f);
                rim.shadows = LightShadows.None;
                rimGO.transform.rotation = Quaternion.Euler(-32f, 110f, 0f);
            }

            // Flat, bright ambient is what keeps a printed plate readable. Cooled
            // very slightly so warm ivory bone separates from the cool backdrop
            // rather than both drifting the same direction.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.60f, 0.60f, 0.63f);

            // Against a dark backdrop the skybox would still light the figure with
            // whatever gradient it holds; clear it so ambient is the only fill.
            RenderSettings.skybox = null;

            WarnOnLowBackdropContrast();
        }

        /// <summary>
        /// The backdrop was once set to a colour under 0.05 from BoneColor on every
        /// channel, which made the skeleton disappear into it and was only caught by
        /// the user looking at the screen. This runs on every build so the next
        /// palette change cannot repeat that silently.
        ///
        /// Measured as CIE Lab dE, not a luminance gap: a first attempt at this check
        /// used luminance alone and flagged dark red muscle on a near-black backdrop
        /// as unreadable, when in fact the two differ hugely in hue and separate
        /// perfectly well. Anything under ~25 is where structures genuinely start to
        /// merge into the background.
        /// </summary>
        private static void WarnOnLowBackdropContrast()
        {
            var tissues = new (string Name, Color Value)[]
            {
                ("BoneColor", BoneColor), ("MuscleColor", MuscleColor), ("HeartColor", HeartColor),
                ("LungColor", LungColor), ("CartilageColor", CartilageColor), ("NerveColor", NerveColor),
            };

            foreach (var (name, value) in tissues)
            {
                float deltaE = PerceptualDistance(BackdropColor, value);
                if (deltaE < 25f)
                {
                    Debug.LogWarning($"[HumanBodyExplorerSetup] {name} is only dE {deltaE:F1} from the " +
                                     "backdrop; structures using it will blend into the background. " +
                                     "Adjust BackdropColor or that tissue colour.");
                }
            }
        }

        /// <summary>CIE76 dE between two sRGB colours.</summary>
        private static float PerceptualDistance(Color a, Color b)
        {
            Vector3 la = ToLab(a), lb = ToLab(b);
            return Vector3.Distance(la, lb);
        }

        private static Vector3 ToLab(Color c)
        {
            float Linear(float v) => v <= 0.04045f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            float r = Linear(c.r), g = Linear(c.g), b = Linear(c.b);

            // sRGB -> CIE XYZ (D65), then XYZ -> Lab against the D65 white point.
            float x = (r * 0.4124f + g * 0.3576f + b * 0.1805f) / 0.95047f;
            float y = (r * 0.2126f + g * 0.7152f + b * 0.0722f);
            float z = (r * 0.0193f + g * 0.1192f + b * 0.9505f) / 1.08883f;

            float F(float t) => t > 0.008856f ? Mathf.Pow(t, 1f / 3f) : 7.787f * t + 16f / 116f;
            float fx = F(x), fy = F(y), fz = F(z);

            return new Vector3(116f * fy - 16f, 500f * (fx - fy), 200f * (fy - fz));
        }

        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int CullId = Shader.PropertyToID("_Cull");
        private static readonly int TransmissionColorId = Shader.PropertyToID("_TransmissionColor");
        private static readonly int ThicknessMultiplierId = Shader.PropertyToID("_ThicknessMultiplier");
        private static readonly int TransmissionIntensityId = Shader.PropertyToID("_TransmissionIntensity");
        private static readonly int FlowColorId = Shader.PropertyToID("_FlowColor");
        private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
        private static readonly int PulseIntensityId = Shader.PropertyToID("_PulseIntensity");
        private const string AnatomyLayerName = "Anatomy";

        private static GameObject BuildHumanFigure()
        {
            var root = new GameObject("HumanBodyRoot");
            int anatomyLayer = EnsureLayer(AnatomyLayerName);

            var shaderLit = Shader.Find("Universal Render Pipeline/Lit");
            var shaderTissue = Shader.Find("HumanBodyExplorer/TissueSSS");
            var shaderVessel = Shader.Find("HumanBodyExplorer/BloodFlow");

            // Shared, generated (not imported) noise texture modulates base color
            // as a brightness multiplier so flat skin/bone color reads as mottled
            // tissue instead of flat plastic.
            var skinNoiseTex = CreateNoiseTexture(64, 0.94f, 1f, 6f, 11);

            foreach (var part in Parts)
            {
                var go = GameObject.CreatePrimitive(part.Shape);
                go.name = part.Name;
                go.layer = anatomyLayer;
                go.transform.SetParent(root.transform, worldPositionStays: false);
                go.transform.localPosition = part.LocalPosition;
                go.transform.localRotation = Quaternion.Euler(part.LocalEuler);
                go.transform.localScale = part.LocalScale;

                go.AddComponent<AnatomyNodeReference>().SetEntityId(part.EntityId);

                bool isVessel = part.EntityId == "SYS_CV_ARTERY" || part.EntityId == "SYS_CV_VEIN";
                bool isSkin = part.EntityId == "SYS_INTEG_SKIN";
                Material material;

                if (isVessel)
                {
                    // Real vessel-specific shader (Phase 36): scrolls a pulse
                    // highlight along the capsule's length to read as flowing blood
                    // instead of a static red/blue tube.
                    bool isArtery = part.EntityId == "SYS_CV_ARTERY";
                    material = new Material(shaderVessel);
                    material.SetColor(BaseColorId, part.Color);
                    material.SetColor(FlowColorId, isArtery ? new Color(1f, 0.3f, 0.25f) : new Color(0.4f, 0.6f, 0.95f));
                    material.SetFloat(FlowSpeedId, isArtery ? 1.4f : 0.8f);
                    material.SetFloat(PulseIntensityId, 0.6f);
                }
                else if (part.Organic)
                {
                    // Fake-subsurface-scattering shader (Phase 23) gives organs/
                    // muscle a wet, backlit-translucent look instead of flat lit
                    // plastic.
                    material = new Material(shaderTissue);
                    material.SetColor(BaseColorId, part.Color);
                    material.SetColor(TransmissionColorId, Color.Lerp(part.Color, Color.white, 0.3f));
                    material.SetFloat(ThicknessMultiplierId, 1f);
                    material.SetFloat(TransmissionIntensityId, 0.6f);
                }
                else
                {
                    material = new Material(shaderLit) { color = part.Color };
                    material.SetFloat(SmoothnessId, part.Smoothness);
                    material.SetTexture(BaseMapId, skinNoiseTex);
                    material.SetTextureScale(BaseMapId, new Vector2(2f, 2f));

                    if (isSkin)
                    {
                        // Skin becomes genuinely see-through (not just a ghosting
                        // heuristic) so the opaque organs/vessels/bone underneath
                        // show through it, matching a classic cutaway anatomy chart.
                        MakeTransparent(material, alpha: 0.22f);
                    }
                }

                if (part.Organic && part.Shape == PrimitiveType.Sphere)
                {
                    int seed = Mathf.Abs(part.Name.GetHashCode() % 1000);
                    go.GetComponent<MeshFilter>().sharedMesh = CreateNoisySphereMesh(0.1f, 3f, seed);

                    // The custom mesh's triangle winding isn't guaranteed to match
                    // Unity's back-face convention the way a built-in primitive's
                    // does, and normals are set explicitly from the sphere direction
                    // (not recalculated from winding), so render both faces rather
                    // than risk the organ being back-face-culled into invisibility.
                    material.SetFloat(CullId, (float)UnityEngine.Rendering.CullMode.Off);
                }

                go.GetComponent<Renderer>().sharedMaterial = material;
            }

            // Structures made of many repeating bones - the vertebral column, the rib
            // cage, and the bones of the hands and feet - are generated rather than
            // written out as hundreds of literal PartDefs. A single sphere cannot read
            // as a rib cage; twelve curved pairs can.
            var boneMat = CreateSimpleMaterial(shaderLit, BoneColor, BoneGloss, skinNoiseTex);
            var cartilageMat = CreateSimpleMaterial(shaderLit, CartilageColor, OrganGloss, skinNoiseTex);

            // Muscle is striped along its fibres, and its tendons are pale, glossy connective tissue.
            var muscleMat = CreateSimpleMaterial(shaderLit, MuscleColor, MuscleGloss, CreateFibreTexture());
            var tendonMat = CreateSimpleMaterial(shaderLit, new Color(0.90f, 0.86f, 0.76f), 0.35f, skinNoiseTex);
            var recessMat = CreateSimpleMaterial(shaderLit, new Color(0.13f, 0.11f, 0.10f), 0.05f, skinNoiseTex);
            var nerveMat = CreateSimpleMaterial(shaderLit, NerveColor, OrganGloss, skinNoiseTex);

            // The animated flowing-blood shader, built once and shared by every
            // generated vessel (as opposed to the literal Carotid/Jugular/Aorta/
            // VenaCava PartDefs above, which predate this and still use a plain tinted
            // material) so the whole vascular tree pulses, not just the four limb
            // trunks that used to carry the generic SYS_CV_ARTERY/SYS_CV_VEIN ids.
            var arteryFlowMat = new Material(shaderVessel);
            arteryFlowMat.SetColor(BaseColorId, ArteryColor);
            arteryFlowMat.SetColor(FlowColorId, new Color(1f, 0.3f, 0.25f));
            arteryFlowMat.SetFloat(FlowSpeedId, 1.4f);
            arteryFlowMat.SetFloat(PulseIntensityId, 0.6f);

            var veinFlowMat = new Material(shaderVessel);
            veinFlowMat.SetColor(BaseColorId, VeinColor);
            veinFlowMat.SetColor(FlowColorId, new Color(0.4f, 0.6f, 0.95f));
            veinFlowMat.SetFloat(FlowSpeedId, 0.8f);
            veinFlowMat.SetFloat(PulseIntensityId, 0.6f);

            // Skeleton and skin are sculpted meshes, saved as assets so the scene
            // references them instead of embedding them. See Geometry/.
            MeshAssets.BeginBuild();
            SkeletonBuilder.Build(root.transform, boneMat, cartilageMat, anatomyLayer);

            var skinMat = new Material(Shader.Find("HumanBodyExplorer/SkinShell"));
            skinMat.SetColor(BaseColorId, new Color(SkinColor.r, SkinColor.g, SkinColor.b, 0.35f));
            SkinBuilder.Build(root.transform, skinMat, anatomyLayer);

            MuscleBuilder.Build(root.transform, muscleMat, tendonMat, anatomyLayer);
            BuildVascularSystem(root.transform, arteryFlowMat, veinFlowMat, anatomyLayer, 1f);
            BuildVascularSystem(root.transform, arteryFlowMat, veinFlowMat, anatomyLayer, -1f);
            BuildLimbNerves(root.transform, nerveMat, anatomyLayer, 1f);
            BuildLimbNerves(root.transform, nerveMat, anatomyLayer, -1f);
            BuildTrunkVasculatureAndNerves(root.transform, arteryFlowMat, veinFlowMat, nerveMat, anatomyLayer);
            MeshAssets.EndBuild();

            return root;
        }

        /// <summary>Fine stripes across the texture's U axis. The loft maps U once around a muscle,
        /// so the stripes run along its length - the fibre grain of real muscle.</summary>
        internal static Texture2D CreateFibreTexture()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Repeat, name = "MuscleFibres" };
            var rng = new System.Random(7);
            var stripe = new float[size];
            for (int x = 0; x < size; x++) stripe[x] = 0.80f + 0.20f * (float)rng.NextDouble();
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Neighbouring fibres are similar, with slow variation down the length.
                    float wobble = 0.94f + 0.06f * Mathf.PerlinNoise(x * 0.35f, y * 0.045f);
                    float v = Mathf.Clamp01(Mathf.Lerp(stripe[x], stripe[(x + 1) % size], 0.5f) * wobble);
                    tex.SetPixel(x, y, new Color(v, v, v));
                }
            tex.Apply();
            return tex;
        }

        private static Material CreateSimpleMaterial(Shader shader, Color color, float smoothness, Texture2D noiseTex)
        {
            var material = new Material(shader) { color = color };
            material.SetFloat(SmoothnessId, smoothness);
            material.SetTexture(BaseMapId, noiseTex);
            material.SetTextureScale(BaseMapId, new Vector2(2f, 2f));
            return material;
        }

        /// <summary>Capsule spanning two points - the building block for every generated bone.</summary>
        private static void CreateSegment(Transform parent, string name, string entityId,
            Vector3 from, Vector3 to, float radius, Material material, int layer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.layer = layer;
            go.transform.SetParent(parent, false);

            Vector3 delta = to - from;
            float length = delta.magnitude;
            go.transform.localPosition = (from + to) * 0.5f;
            go.transform.localRotation = length > 1e-5f
                ? Quaternion.FromToRotation(Vector3.up, delta / length)
                : Quaternion.identity;
            go.transform.localScale = new Vector3(radius * 2f, Mathf.Max(radius, length * 0.5f), radius * 2f);

            go.GetComponent<Renderer>().sharedMaterial = material;
            go.AddComponent<AnatomyNodeReference>().SetEntityId(entityId);
        }

        private static void CreateBlob(Transform parent, string name, string entityId,
            Vector3 position, Vector3 scale, Material material, int layer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.layer = layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.AddComponent<AnatomyNodeReference>().SetEntityId(entityId);
        }




        /// <summary>
        /// Named arterial and venous trunks down one arm and one leg, replacing what
        /// used to be a single undifferentiated "Artery_Arm"/"Vein_Arm" tube per limb.
        /// Landmark points are independently-tuned literals matching the bone PartDefs
        /// and the wrist/ankle constants in BuildHandSkeleton/BuildFootSkeleton - the
        /// same convention those two already use - rather than reading the Parts array,
        /// since that is a static initializer this runs alongside, not after.
        /// Veins run a few millimetres lateral and posterior of their artery, which is
        /// real (the two travel together in a neurovascular bundle) and also keeps two
        /// overlapping capsules from z-fighting.
        /// </summary>
        private static void BuildVascularSystem(Transform root, Material arteryMat, Material veinMat, int layer, float side)
        {
            string tag = side > 0 ? "L" : "R";
            Vector3 VeinOffset(Vector3 p, float dx, float dz) => p + new Vector3(side * dx, 0f, dz);

            // Arm: base of neck -> shoulder -> mid-humerus -> elbow, then forearm
            // splits into radial (lateral/thumb side) and ulnar (medial) arteries.
            var neckRoot = new Vector3(side * 0.075f, 1.400f, -0.035f);
            var shoulder = new Vector3(side * 0.150f, 1.395f, -0.005f);
            var midHumerus = new Vector3(side * 0.190f, 1.260f, 0.010f); // matches Humerus_L/R centre
            var elbow = new Vector3(side * 0.190f, 1.095f, 0.012f);      // matches Humerus bottom
            var wrist = new Vector3(side * 0.212f, 0.840f, 0.010f);      // just proximal of the carpals

            CreateSegment(root, $"SubclavianArtery_{tag}", "SYS_CV_SUBCLAVIAN", neckRoot, shoulder, 0.010f, arteryMat, layer);
            CreateSegment(root, $"AxillaryArtery_{tag}", "SYS_CV_AXILLARY", shoulder, midHumerus, 0.009f, arteryMat, layer);
            CreateSegment(root, $"BrachialArtery_{tag}", "SYS_CV_BRACHIAL", midHumerus, elbow, 0.008f, arteryMat, layer);
            CreateSegment(root, $"RadialArtery_{tag}", "SYS_CV_RADIAL",
                elbow + new Vector3(side * 0.006f, 0f, -0.004f), wrist + new Vector3(side * 0.010f, 0f, -0.004f), 0.005f, arteryMat, layer);
            CreateSegment(root, $"UlnarArtery_{tag}", "SYS_CV_ULNAR",
                elbow + new Vector3(-side * 0.006f, 0f, 0.004f), wrist + new Vector3(-side * 0.006f, 0f, 0.004f), 0.005f, arteryMat, layer);

            CreateSegment(root, $"SubclavianVein_{tag}", "SYS_CV_SUBCLAVIAN_VEIN",
                VeinOffset(neckRoot, 0.020f, -0.015f), VeinOffset(shoulder, 0.020f, -0.015f), 0.010f, veinMat, layer);
            CreateSegment(root, $"AxillaryVein_{tag}", "SYS_CV_AXILLARY_VEIN",
                VeinOffset(shoulder, 0.020f, -0.015f), VeinOffset(midHumerus, 0.020f, -0.015f), 0.009f, veinMat, layer);
            CreateSegment(root, $"BrachialVein_{tag}", "SYS_CV_BRACHIAL_VEIN",
                VeinOffset(midHumerus, 0.020f, -0.015f), VeinOffset(wrist, 0.018f, -0.012f), 0.008f, veinMat, layer);

            // Leg: groin -> hip -> knee (femoral, then popliteal behind the knee),
            // then below the knee splits into anterior tibial (front of shin, next to
            // TibialisAnterior) and posterior tibial (behind the tibia, toward the sole).
            var groin = new Vector3(side * 0.055f, 0.895f, 0.020f);
            var hip = new Vector3(side * 0.090f, 0.830f, 0.020f);   // upper Femur, medial (femoral triangle)
            var knee = new Vector3(side * 0.088f, 0.475f, 0.025f);  // matches Femur/Tibia junction
            var belowKnee = new Vector3(side * 0.082f, 0.440f, 0.020f);
            var ankle = new Vector3(side * 0.083f, 0.078f, 0.015f); // matches Tibia bottom / Talus

            CreateSegment(root, $"CommonIliacArtery_{tag}", "SYS_CV_ILIAC", groin, hip, 0.014f, arteryMat, layer);
            CreateSegment(root, $"FemoralArtery_{tag}", "SYS_CV_FEMORAL", hip, knee, 0.011f, arteryMat, layer);
            CreateSegment(root, $"PoplitealArtery_{tag}", "SYS_CV_POPLITEAL", knee, belowKnee, 0.009f, arteryMat, layer);
            CreateSegment(root, $"AnteriorTibialArtery_{tag}", "SYS_CV_ANT_TIBIAL",
                belowKnee + new Vector3(0f, 0f, -0.020f), ankle + new Vector3(0f, 0.010f, -0.015f), 0.006f, arteryMat, layer);
            CreateSegment(root, $"PosteriorTibialArtery_{tag}", "SYS_CV_POST_TIBIAL",
                belowKnee + new Vector3(0f, 0f, 0.018f), ankle + new Vector3(0f, 0.010f, 0.020f), 0.006f, arteryMat, layer);

            CreateSegment(root, $"CommonIliacVein_{tag}", "SYS_CV_ILIAC_VEIN",
                VeinOffset(groin, 0.015f, -0.006f), VeinOffset(hip, 0.015f, -0.006f), 0.014f, veinMat, layer);
            CreateSegment(root, $"FemoralVein_{tag}", "SYS_CV_FEMORAL_VEIN",
                VeinOffset(hip, 0.015f, -0.006f), VeinOffset(knee, 0.015f, -0.006f), 0.011f, veinMat, layer);
            CreateSegment(root, $"PoplitealVein_{tag}", "SYS_CV_POPLITEAL_VEIN",
                VeinOffset(knee, 0.015f, -0.006f), VeinOffset(belowKnee, 0.015f, -0.006f), 0.009f, veinMat, layer);

            // Great saphenous vein: the longest vein in the body, superficial along the
            // whole medial leg (harvested for coronary bypass grafts) - a single run
            // from the medial ankle to the groin, deliberately offset from the deep
            // vessels above since it travels just under the skin, not alongside the
            // femoral/tibial bundle.
            CreateSegment(root, $"GreatSaphenousVein_{tag}", "SYS_CV_SAPHENOUS",
                new Vector3(side * 0.060f, 0.085f, -0.010f), new Vector3(side * 0.045f, 0.870f, -0.005f),
                0.005f, veinMat, layer);
        }

        /// <summary>
        /// Peripheral nerves down one arm and one leg, roughly paralleling the vessels
        /// BuildVascularSystem lays down along the same limb - real neurovascular
        /// bundles travel together.
        /// </summary>
        private static void BuildLimbNerves(Transform root, Material nerveMat, int layer, float side)
        {
            string tag = side > 0 ? "L" : "R";
            var elbow = new Vector3(side * 0.190f, 1.095f, 0.012f);
            var wrist = new Vector3(side * 0.212f, 0.840f, 0.010f);
            var knee = new Vector3(side * 0.088f, 0.475f, 0.025f);
            var belowKnee = new Vector3(side * 0.082f, 0.440f, 0.020f);
            var ankle = new Vector3(side * 0.083f, 0.078f, 0.015f);
            var hip = new Vector3(side * 0.090f, 0.830f, 0.020f);

            // Radial, median and ulnar: the three great nerves of the forearm and
            // hand, spaced across it the way the radial/ulnar arteries are.
            CreateSegment(root, $"RadialNerve_{tag}", "SYS_NERV_RADIAL",
                elbow + new Vector3(side * 0.014f, 0f, -0.006f), wrist + new Vector3(side * 0.018f, 0f, -0.006f), 0.004f, nerveMat, layer);
            CreateSegment(root, $"MedianNerve_{tag}", "SYS_NERV_MEDIAN",
                elbow + new Vector3(0f, 0f, 0.002f), wrist + new Vector3(0f, 0f, 0.002f), 0.0042f, nerveMat, layer);
            CreateSegment(root, $"UlnarNerve_{tag}", "SYS_NERV_ULNAR",
                elbow + new Vector3(-side * 0.014f, 0f, 0.008f), wrist + new Vector3(-side * 0.012f, 0f, 0.008f), 0.004f, nerveMat, layer);

            // Femoral: runs down the anterior thigh, lateral to the femoral vessels
            // in the femoral triangle.
            CreateSegment(root, $"FemoralNerve_{tag}", "SYS_NERV_FEMORAL",
                hip + new Vector3(side * 0.018f, 0f, -0.010f), knee + new Vector3(side * 0.014f, 0f, -0.012f),
                0.005f, nerveMat, layer);

            // Tibial: continues straight down the posterior compartment behind the knee.
            CreateSegment(root, $"TibialNerve_{tag}", "SYS_NERV_TIBIAL",
                belowKnee + new Vector3(0f, 0f, 0.024f), ankle + new Vector3(0f, 0.012f, 0.024f), 0.0045f, nerveMat, layer);

            // Common fibular (peroneal): wraps laterally around the fibular head, then
            // partway down the outside of the shin before it branches - notorious as
            // the nerve injured by a cast or a kneeling position, causing foot drop.
            CreateSegment(root, $"CommonFibularNerve_{tag}", "SYS_NERV_FIBULAR_COMMON",
                knee + new Vector3(side * 0.026f, 0f, 0f), belowKnee + new Vector3(side * 0.030f, -0.10f, -0.010f),
                0.0038f, nerveMat, layer);
        }

        /// <summary>
        /// Vessels and nerves that don't repeat per-limb: the aortic arch (closing the
        /// visible gap between the ascending and descending aorta), the coronary
        /// arteries, pulmonary vessels, the gut's arterial supply and portal venous
        /// drainage, the renal vessels, and the cranial/trunk nerves (optic, facial,
        /// trigeminal, phrenic, intercostal, pudendal).
        /// </summary>
        private static void BuildTrunkVasculatureAndNerves(Transform root, Material arteryMat, Material veinMat, Material nerveMat, int layer)
        {
            // Aortic arch: two segments up-and-over from the ascending aorta's top to
            // the descending aorta's top, via a posterior-superior apex point. A
            // straight line between the two would cut through the trachea.
            var ascendingTop = new Vector3(0.005f, 1.390f, -0.025f);
            var archApex = new Vector3(-0.015f, 1.410f, 0.010f);
            var descendingTop = new Vector3(-0.010f, 1.290f, 0.045f);
            CreateSegment(root, "AorticArch_1", "SYS_CV_AORTIC_ARCH", ascendingTop, archApex, 0.030f, arteryMat, layer);
            CreateSegment(root, "AorticArch_2", "SYS_CV_AORTIC_ARCH", archApex, descendingTop, 0.030f, arteryMat, layer);

            // Coronary arteries: the heart's own blood supply, running across its own
            // surface from the aortic root. The left anterior descending branch is the
            // one clinicians call the "widowmaker".
            CreateSegment(root, "CoronaryArtery_L", "SYS_CV_CORONARY",
                new Vector3(0.010f, 1.378f, -0.032f), new Vector3(0.055f, 1.280f, -0.058f), 0.006f, arteryMat, layer);
            CreateSegment(root, "CoronaryArtery_R", "SYS_CV_CORONARY",
                new Vector3(0.000f, 1.378f, -0.018f), new Vector3(-0.035f, 1.275f, -0.028f), 0.006f, arteryMat, layer);

            // Pulmonary vessels: the one artery in the body that carries deoxygenated
            // blood, and the one vein that carries oxygenated blood - the exception AP
            // Biology always tests.
            var pulmonaryOut = new Vector3(0.015f, 1.318f, -0.062f);
            CreateSegment(root, "PulmonaryArtery_L", "SYS_CV_PULMONARY_ARTERY", pulmonaryOut, new Vector3(0.078f, 1.302f, -0.020f), 0.013f, arteryMat, layer);
            CreateSegment(root, "PulmonaryArtery_R", "SYS_CV_PULMONARY_ARTERY", pulmonaryOut, new Vector3(-0.078f, 1.302f, -0.020f), 0.013f, arteryMat, layer);
            var pulmonaryIn = new Vector3(0.032f, 1.308f, -0.048f);
            CreateSegment(root, "PulmonaryVein_L", "SYS_CV_PULMONARY_VEIN", new Vector3(0.078f, 1.302f, -0.015f), pulmonaryIn, 0.011f, veinMat, layer);
            CreateSegment(root, "PulmonaryVein_R", "SYS_CV_PULMONARY_VEIN", new Vector3(-0.078f, 1.302f, -0.015f), pulmonaryIn, 0.011f, veinMat, layer);

            // Gut circulation: the celiac trunk (foregut - stomach/liver/spleen), the
            // superior mesenteric artery (mid/hindgut - small intestine), and the
            // hepatic portal vein, which is the whole reason "portal system" is an AP
            // Biology term at all - it carries blood from gut capillaries to a second
            // capillary bed in the liver instead of straight back to the heart.
            CreateSegment(root, "CeliacTrunk", "SYS_CV_CELIAC", new Vector3(-0.010f, 1.170f, 0.045f), new Vector3(0.030f, 1.160f, -0.015f), 0.010f, arteryMat, layer);
            CreateSegment(root, "SuperiorMesentericArtery", "SYS_CV_SMA", new Vector3(-0.010f, 1.110f, 0.045f), new Vector3(0.000f, 1.050f, -0.020f), 0.009f, arteryMat, layer);
            CreateSegment(root, "HepaticPortalVein", "SYS_CV_PORTAL_VEIN", new Vector3(0.000f, 1.030f, -0.055f), new Vector3(-0.069f, 1.150f, -0.050f), 0.011f, veinMat, layer);

            // Renal vessels: aorta/vena cava direct to each kidney.
            CreateSegment(root, "RenalArtery_L", "SYS_CV_RENAL_ARTERY", new Vector3(-0.010f, 1.108f, 0.045f), new Vector3(0.070f, 1.125f, 0.055f), 0.007f, arteryMat, layer);
            CreateSegment(root, "RenalArtery_R", "SYS_CV_RENAL_ARTERY", new Vector3(-0.010f, 1.100f, 0.045f), new Vector3(-0.070f, 1.100f, 0.055f), 0.007f, arteryMat, layer);
            CreateSegment(root, "RenalVein_L", "SYS_CV_RENAL_VEIN", new Vector3(-0.035f, 1.112f, 0.030f), new Vector3(0.070f, 1.125f, 0.050f), 0.008f, veinMat, layer);
            CreateSegment(root, "RenalVein_R", "SYS_CV_RENAL_VEIN", new Vector3(-0.035f, 1.104f, 0.030f), new Vector3(-0.070f, 1.100f, 0.050f), 0.008f, veinMat, layer);

            for (int s = -1; s <= 1; s += 2)
            {
                string tag = s > 0 ? "L" : "R";

                // Phrenic: the diaphragm's only motor supply, running the full length
                // of the neck and thorax - if it is cut, that side of the diaphragm
                // stops moving.
                CreateSegment(root, $"PhrenicNerve_{tag}", "SYS_NERV_PHRENIC",
                    new Vector3(s * 0.030f, 1.400f, -0.010f), new Vector3(s * 0.022f, 1.230f, -0.015f), 0.003f, nerveMat, layer);

                // Cranial nerves: optic (vision, from the back of the orbit to the
                // brain), trigeminal (facial sensation) and facial (facial movement).
                CreateSegment(root, $"OpticNerve_{tag}", "SYS_NERV_OPTIC",
                    new Vector3(s * 0.030f, 1.655f, -0.060f), new Vector3(s * 0.010f, 1.630f, -0.020f), 0.0035f, nerveMat, layer);
                CreateSegment(root, $"TrigeminalNerve_{tag}", "SYS_NERV_TRIGEMINAL",
                    new Vector3(0f, 1.565f, 0.015f), new Vector3(s * 0.048f, 1.605f, -0.035f), 0.0035f, nerveMat, layer);
                CreateSegment(root, $"FacialNerve_{tag}", "SYS_NERV_FACIAL",
                    new Vector3(0f, 1.565f, 0.015f), new Vector3(s * 0.058f, 1.598f, -0.020f), 0.003f, nerveMat, layer);

                // Pudendal: the pelvic floor's nerve - short, easy to miss, but the one
                // every anatomy course names.
                CreateSegment(root, $"PudendalNerve_{tag}", "SYS_NERV_PUDENDAL",
                    new Vector3(s * 0.040f, 0.850f, 0.050f), new Vector3(s * 0.020f, 0.800f, 0.020f), 0.003f, nerveMat, layer);

                // Intercostal nerves at four rib levels, hugging the chest wall - not
                // an exhaustive twelve pairs, but enough to read as a real nerve
                // distribution rather than an empty ribcage once Nervous is the only
                // visible layer.
                float[] ribLevels = { 1.320f, 1.270f, 1.220f, 1.170f };
                for (int i = 0; i < ribLevels.Length; i++)
                {
                    CreateSegment(root, $"IntercostalNerve_{tag}_{i + 1}", "SYS_NERV_INTERCOSTAL",
                        new Vector3(s * 0.120f, ribLevels[i], -0.020f), new Vector3(s * 0.160f, ribLevels[i], -0.075f),
                        0.0028f, nerveMat, layer);
                }
            }
        }

        /// <summary>
        /// Standard scripted recipe for switching a URP Lit material to alpha-blended
        /// transparency (Unity's own material inspector does the same property/keyword
        /// set when you change Surface Type to Transparent).
        /// </summary>
        private static void MakeTransparent(Material material, float alpha)
        {
            var c = material.color;
            material.color = new Color(c.r, c.g, c.b, alpha);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        /// <summary>
        /// Builds a UV sphere (radius 0.5 at scale 1, matching Unity's primitive
        /// sphere convention) with each vertex pushed in/out along its own normal
        /// by a blended-2D-Perlin pseudo-3D noise, so it reads as an organic blob
        /// instead of a perfect sphere. Normals are set directly from the
        /// undisplaced sphere direction rather than recalculated from triangle
        /// winding, so the shape is guaranteed to shade as convex/outward-facing
        /// even if the winding direction below doesn't match Unity's own
        /// primitive-sphere convention exactly.
        /// </summary>
        private static Mesh CreateNoisySphereMesh(float noiseStrength, float noiseScale, int seed)
        {
            const int lonSegments = 24;
            const int latSegments = 16;
            int vertsPerRow = lonSegments + 1;

            var vertices = new List<Vector3>((latSegments + 1) * vertsPerRow);
            var normals = new List<Vector3>((latSegments + 1) * vertsPerRow);
            var uvs = new List<Vector2>((latSegments + 1) * vertsPerRow);

            for (int lat = 0; lat <= latSegments; lat++)
            {
                float v = (float)lat / latSegments;
                float theta = v * Mathf.PI;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                for (int lon = 0; lon <= lonSegments; lon++)
                {
                    float u = (float)lon / lonSegments;
                    float phi = u * Mathf.PI * 2f;
                    var dir = new Vector3(sinTheta * Mathf.Cos(phi), cosTheta, sinTheta * Mathf.Sin(phi));

                    float n = Noise3D(dir.x * noiseScale + seed, dir.y * noiseScale + seed, dir.z * noiseScale + seed);
                    float radius = 0.5f * (1f + (n * 2f - 1f) * noiseStrength);

                    vertices.Add(dir * radius);
                    normals.Add(dir);
                    uvs.Add(new Vector2(u, v));
                }
            }

            var triangles = new List<int>(latSegments * lonSegments * 6);
            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int current = lat * vertsPerRow + lon;
                    int next = current + vertsPerRow;

                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(current + 1);

                    triangles.Add(current + 1);
                    triangles.Add(next);
                    triangles.Add(next + 1);
                }
            }

            var mesh = new Mesh { name = "NoisyOrganSphere" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float Noise3D(float x, float y, float z)
        {
            float xy = Mathf.PerlinNoise(x, y);
            float yz = Mathf.PerlinNoise(y, z);
            float zx = Mathf.PerlinNoise(z, x);
            return (xy + yz + zx) / 3f;
        }

        private static Texture2D CreateNoiseTexture(int size, float minValue, float maxValue, float noiseScale, int seed)
        {
            var tex = new Texture2D(size, size) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = Mathf.PerlinNoise((x / (float)size) * noiseScale + seed, (y / (float)size) * noiseScale + seed);
                    float value = Mathf.Lerp(minValue, maxValue, n);
                    pixels[y * size + x] = new Color(value, value, value, 1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Finds or creates a user layer by name in ProjectSettings/TagManager.asset.
        /// The orbit camera's own anti-clip collision check needs the body parts on
        /// a layer it can exclude - otherwise the target sits inside the figure's
        /// own colliders and the "don't clip through walls" raycast treats the
        /// figure itself as a wall, yanking the camera in until it's inside the body.
        /// </summary>
        private static int EnsureLayer(string layerName)
        {
            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var tagManager = new SerializedObject(tagManagerAsset);
            var layersProp = tagManager.FindProperty("layers");

            for (int i = 8; i < layersProp.arraySize; i++)
            {
                if (layersProp.GetArrayElementAtIndex(i).stringValue == layerName) return i;
            }

            for (int i = 8; i < layersProp.arraySize; i++)
            {
                var layerSP = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerSP.stringValue))
                {
                    layerSP.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }

            Debug.LogWarning("[HumanBodyExplorerSetup] No free layer slot for \"" + layerName +
                              "\"; camera collision avoidance will still treat the figure as an obstacle.");
            return 0;
        }

        private static void WireUpCamera(GameObject mainCameraGO, GameObject root)
        {
            var focusPoint = GameObject.Find("BodyFocusPoint");
            if (focusPoint == null)
            {
                focusPoint = new GameObject("BodyFocusPoint");
            }
            focusPoint.transform.SetParent(root.transform, worldPositionStays: false);
            focusPoint.transform.localPosition = new Vector3(0, 1.2f, 0);

            // Pin the camera to a known-good orientation before Play starts.
            // AdvancedOrbitalCamera captures whatever rotation it finds at Awake()
            // as its starting orbit angle, so if the camera was manually rotated in
            // the Scene view at any point, Play would orbit around that stale
            // angle instead of actually facing the figure.
            mainCameraGO.transform.rotation = Quaternion.identity;

            // Dark, slightly cool backdrop. An earlier parchment colour (0.94, 0.92,
            // 0.88) was a near-exact match for BoneColor (0.93, 0.90, 0.83) - under
            // 0.05 apart on every channel - so the skeleton dissolved into the
            // background. Ivory bone and dark red muscle both separate hard against
            // this, which is also why every 3D anatomy atlas uses a dark ground.
            var cam = mainCameraGO.GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = BackdropColor;
                cam.nearClipPlane = 0.03f;
            }

            var orbitalCamera = mainCameraGO.GetComponent<AdvancedOrbitalCamera>();
            if (orbitalCamera == null) orbitalCamera = mainCameraGO.AddComponent<AdvancedOrbitalCamera>();
            orbitalCamera.Target = focusPoint.transform;
            orbitalCamera.SetZoomDistanceImmediate(2.4f); // fallback only - CameraFocusTargeter overrides this correctly at Play

            // Exclude the figure's own layer from the anti-clip collision check -
            // the orbit target sits inside the body, so without this the camera's
            // "don't clip through walls" raycast hits the figure's own colliders
            // and drags the camera inside the body it's supposed to be framing.
            int anatomyLayer = EnsureLayer(AnatomyLayerName);
            var serializedOrbitalCamera = new SerializedObject(orbitalCamera);
            serializedOrbitalCamera.FindProperty("collisionMask").intValue = ~(1 << anatomyLayer);
            serializedOrbitalCamera.ApplyModifiedPropertiesWithoutUndo();

            var raycaster = mainCameraGO.GetComponent<AnatomyRaycaster>();
            if (raycaster == null) raycaster = mainCameraGO.AddComponent<AnatomyRaycaster>();

            // A click through the torso crosses the skin shell, several ribs, a muscle,
            // a lung, the heart, the diaphragm, a vertebra and more. RaycastNonAlloc
            // truncates arbitrarily (not by distance) once the buffer is full, so an
            // undersized buffer can silently drop the nearest hits.
            var serializedRaycaster = new SerializedObject(raycaster);
            serializedRaycaster.FindProperty("maxHits").intValue = 256;
            serializedRaycaster.ApplyModifiedPropertiesWithoutUndo();

            // Camera feel: a 1.75 m figure needs a much tighter distance range than the
            // 0.1-20 default, and zoom is now proportional to distance (see
            // AdvancedOrbitalCamera.Zoom), so sensitivity is a fraction, not a multiple.
            var serializedOrbit = new SerializedObject(orbitalCamera);
            serializedOrbit.FindProperty("minDistance").floatValue = 0.22f;
            serializedOrbit.FindProperty("maxDistance").floatValue = 6f;
            serializedOrbit.FindProperty("zoomSensitivity").floatValue = 0.18f;
            serializedOrbit.FindProperty("orbitSensitivity").floatValue = 0.32f;
            serializedOrbit.FindProperty("smoothTime").floatValue = 0.09f;
            serializedOrbit.ApplyModifiedPropertiesWithoutUndo();

            var focusTargeter = mainCameraGO.GetComponent<CameraFocusTargeter>();
            if (focusTargeter == null) focusTargeter = mainCameraGO.AddComponent<CameraFocusTargeter>();
            var serializedFocusTargeter = new SerializedObject(focusTargeter);
            serializedFocusTargeter.FindProperty("orbitalCamera").objectReferenceValue = orbitalCamera;
            serializedFocusTargeter.FindProperty("targetCamera").objectReferenceValue = mainCameraGO.GetComponent<Camera>();
            serializedFocusTargeter.ApplyModifiedPropertiesWithoutUndo();

            var bridgeGO = GameObject.Find("DemoInputBridge") ?? new GameObject("DemoInputBridge");
            var bridge = bridgeGO.GetComponent<DemoInputBridge>() ?? bridgeGO.AddComponent<DemoInputBridge>();

            var serializedBridge = new SerializedObject(bridge);
            serializedBridge.FindProperty("orbitalCamera").objectReferenceValue = orbitalCamera;
            serializedBridge.FindProperty("raycaster").objectReferenceValue = raycaster;
            serializedBridge.FindProperty("focusTargeter").objectReferenceValue = focusTargeter;
            serializedBridge.FindProperty("bodyRoot").objectReferenceValue = root;
            serializedBridge.ApplyModifiedPropertiesWithoutUndo();
        }

        private const string PostProcessProfilePath = "Assets/Generated/ExplorerPostProcessProfile.asset";

        /// <summary>
        /// Subtle bloom/contrast/vignette so the figure doesn't look like flat-lit
        /// primitives in a void. Uses only core URP Volume components (Bloom,
        /// ColorAdjustments, Vignette), which work off the camera's own
        /// post-processing flag and don't require adding a Renderer Feature to the
        /// project's URP Renderer asset.
        /// </summary>
        private static void SetupPostProcessing(GameObject mainCameraGO)
        {
            var camData = mainCameraGO.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = mainCameraGO.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            if (!AssetDatabase.IsValidFolder("Assets/Generated"))
            {
                AssetDatabase.CreateFolder("Assets", "Generated");
            }

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProcessProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, PostProcessProfilePath);
            }

            if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.4f);
            bloom.intensity.Override(0.04f);
            bloom.scatter.Override(0.6f);

            if (!profile.TryGet(out ColorAdjustments colorAdjustments)) colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.Override(0.05f);
            colorAdjustments.contrast.Override(14f);
            colorAdjustments.saturation.Override(20f);

            if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0f);
            vignette.smoothness.Override(0.6f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            RemoveIfExists("ExplorerPostProcessVolume");
            var volumeGO = new GameObject("ExplorerPostProcessVolume");
            var volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.weight = 1f;
            volume.profile = profile;
        }

        /// <summary>
        /// Unity UI dispatches pointer events through an EventSystem; without one in
        /// the scene, no Button anywhere will ever fire - which is exactly why every
        /// control in this explorer appeared dead. The project uses the new Input
        /// System, so it needs InputSystemUIInputModule rather than the legacy
        /// StandaloneInputModule (which throws under the new backend).
        /// </summary>
        private static void EnsureEventSystem()
        {
            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null)
            {
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                {
                    foreach (var legacy in existing.GetComponents<BaseInputModule>())
                    {
                        Object.DestroyImmediate(legacy);
                    }
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                return;
            }

            var eventSystemGO = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemGO.transform.SetSiblingIndex(0);
            Debug.Log("[HumanBodyExplorerSetup] Created the missing EventSystem - UI buttons can now receive clicks.");
        }

        private static void BuildUI(GameObject mainCameraGO)
        {
            RemoveIfExists("ExplorerCanvas");
            RemoveIfExists("ExplorerUIController");
            RemoveIfExists("AnatomyPartFeedback");

            EnsureEventSystem();

            var canvasGO = new GameObject("ExplorerCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Info panel (bottom-left)
            // Taller than before: at AP Biology / Clinical detail this panel carries
            // several paragraphs of facts rather than a single sentence.
            var infoPanel = CreatePanel(canvasGO.transform, "InfoPanel",
                anchorMin: new Vector2(1, 0), anchorMax: new Vector2(1, 0),
                pivot: new Vector2(1, 0), anchoredPos: new Vector2(-30, 30), size: new Vector2(780, 430));
            var infoText = CreateText(infoPanel.transform, "InfoText", "Click on a body part to learn what it does.", 20);
            StretchToParent(infoText.rectTransform, padding: 20);
            infoText.alignment = TextAlignmentOptions.TopLeft;

            // Quiz HUD (top-right), hidden until quiz starts
            var quizPanel = CreatePanel(canvasGO.transform, "QuizPanel",
                anchorMin: new Vector2(1, 1), anchorMax: new Vector2(1, 1),
                pivot: new Vector2(1, 1), anchoredPos: new Vector2(-30, -30), size: new Vector2(480, 240));
            var quizPrompt = CreateText(quizPanel.transform, "QuizPrompt", "", 26);
            quizPrompt.rectTransform.anchorMin = new Vector2(0, 0.68f);
            quizPrompt.rectTransform.anchorMax = new Vector2(1, 1);
            StretchToParent(quizPrompt.rectTransform, padding: 15, keepAnchors: true);

            var quizTimer = CreateText(quizPanel.transform, "QuizTimer", "", 22);
            quizTimer.color = new Color(1f, 0.85f, 0.3f);
            quizTimer.rectTransform.anchorMin = new Vector2(0, 0.48f);
            quizTimer.rectTransform.anchorMax = new Vector2(1, 0.68f);
            StretchToParent(quizTimer.rectTransform, padding: 15, keepAnchors: true);

            var quizFeedback = CreateText(quizPanel.transform, "QuizFeedback", "", 20);
            quizFeedback.rectTransform.anchorMin = new Vector2(0, 0.24f);
            quizFeedback.rectTransform.anchorMax = new Vector2(1, 0.48f);
            StretchToParent(quizFeedback.rectTransform, padding: 15, keepAnchors: true);

            var quizScore = CreateText(quizPanel.transform, "QuizScore", "Score: 0", 20);
            quizScore.rectTransform.anchorMin = new Vector2(0, 0f);
            quizScore.rectTransform.anchorMax = new Vector2(1, 0.24f);
            StretchToParent(quizScore.rectTransform, padding: 15, keepAnchors: true);

            quizPanel.SetActive(false);

            // Start Quiz button (top-left)
            var buttonGO = new GameObject("StartQuizButton", typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(canvasGO.transform, false);
            var buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 1);
            buttonRect.anchorMax = new Vector2(0, 1);
            buttonRect.pivot = new Vector2(0, 1);
            buttonRect.anchoredPosition = new Vector2(30, -26);
            buttonRect.sizeDelta = new Vector2(210, 54);
            buttonGO.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.3f);

            var buttonText = CreateText(buttonGO.transform, "Text", "Start Quiz", 24);
            StretchToParent(buttonText.rectTransform, padding: 0);
            buttonText.alignment = TextAlignmentOptions.Center;

            // System filter button (below Start Quiz) - cycles "All" / one body
            // system at a time, so a teacher can quiz just Skeletal, just
            // Cardiovascular, etc.
            var filterButtonGO = new GameObject("SystemFilterButton", typeof(Image), typeof(Button));
            filterButtonGO.transform.SetParent(canvasGO.transform, false);
            var filterButtonRect = filterButtonGO.GetComponent<RectTransform>();
            filterButtonRect.anchorMin = new Vector2(0, 1);
            filterButtonRect.anchorMax = new Vector2(0, 1);
            filterButtonRect.pivot = new Vector2(0, 1);
            filterButtonRect.anchoredPosition = new Vector2(30, -88);
            filterButtonRect.sizeDelta = new Vector2(210, 46);
            filterButtonGO.GetComponent<Image>().color = new Color(0.25f, 0.3f, 0.45f);

            var filterText = CreateText(filterButtonGO.transform, "Text", "Study: All", 20);
            StretchToParent(filterText.rectTransform, padding: 0);
            filterText.alignment = TextAlignmentOptions.Center;

            // Colorblind accessibility toggle (below the system filter) - off by
            // default, boosts red/blue contrast between arteries and veins.
            var colorblindButtonGO = new GameObject("ColorblindToggleButton", typeof(Image), typeof(Button));
            colorblindButtonGO.transform.SetParent(canvasGO.transform, false);
            var colorblindButtonRect = colorblindButtonGO.GetComponent<RectTransform>();
            colorblindButtonRect.anchorMin = new Vector2(0, 1);
            colorblindButtonRect.anchorMax = new Vector2(0, 1);
            colorblindButtonRect.pivot = new Vector2(0, 1);
            colorblindButtonRect.anchoredPosition = new Vector2(30, -140);
            colorblindButtonRect.sizeDelta = new Vector2(210, 46);
            colorblindButtonGO.GetComponent<Image>().color = new Color(0.35f, 0.25f, 0.15f);

            var colorblindText = CreateText(colorblindButtonGO.transform, "Text", "Colorblind Mode: Off", 18);
            StretchToParent(colorblindText.rectTransform, padding: 0);
            colorblindText.alignment = TextAlignmentOptions.Center;

            // Detail level: plain English for a first pass, AP Biology for curriculum
            // facts, Clinical for the medical correlations.
            var detailButtonGO = new GameObject("DetailLevelButton", typeof(Image), typeof(Button));
            detailButtonGO.transform.SetParent(canvasGO.transform, false);
            var detailButtonRect = detailButtonGO.GetComponent<RectTransform>();
            detailButtonRect.anchorMin = new Vector2(0, 1);
            detailButtonRect.anchorMax = new Vector2(0, 1);
            detailButtonRect.pivot = new Vector2(0, 1);
            detailButtonRect.anchoredPosition = new Vector2(30, -192);
            detailButtonRect.sizeDelta = new Vector2(210, 46);
            detailButtonGO.GetComponent<Image>().color = new Color(0.22f, 0.36f, 0.34f);

            var detailText = CreateText(detailButtonGO.transform, "Text", "Detail: Plain English", 18);
            StretchToParent(detailText.rectTransform, padding: 0);
            detailText.alignment = TextAlignmentOptions.Center;

            // Layer toggles: peel the body back one system at a time.
            var layersHeader = CreateText(canvasGO.transform, "LayersHeader", "LAYERS", 16);
            var layersHeaderRect = layersHeader.rectTransform;
            layersHeaderRect.anchorMin = new Vector2(0, 1);
            layersHeaderRect.anchorMax = new Vector2(0, 1);
            layersHeaderRect.pivot = new Vector2(0, 1);
            layersHeaderRect.anchoredPosition = new Vector2(32, -250);
            layersHeaderRect.sizeDelta = new Vector2(200, 24);
            layersHeader.alignment = TextAlignmentOptions.Left;
            layersHeader.color = new Color(0.72f, 0.76f, 0.8f);

            var layerGroups = AnatomyLayerVisibility.AllGroups;
            var layerButtons = new Button[layerGroups.Length];
            var layerLabels = new TMP_Text[layerGroups.Length];

            for (int i = 0; i < layerGroups.Length; i++)
            {
                var layerButtonGO = new GameObject($"LayerButton_{layerGroups[i]}", typeof(Image), typeof(Button));
                layerButtonGO.transform.SetParent(canvasGO.transform, false);
                var rect = layerButtonGO.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(30, -276 - i * 46);
                rect.sizeDelta = new Vector2(210, 40);
                layerButtonGO.GetComponent<Image>().color = new Color(0.16f, 0.18f, 0.22f, 0.92f);

                var label = CreateText(layerButtonGO.transform, "Text",
                    $"●  {AnatomyLayerVisibility.DisplayName(layerGroups[i])}", 18);
                StretchToParent(label.rectTransform, padding: 0);
                label.alignment = TextAlignmentOptions.Center;

                layerButtons[i] = layerButtonGO.GetComponent<Button>();
                layerLabels[i] = label;
            }

            // Wire the controller
            var controllerGO = new GameObject("ExplorerUIController", typeof(ExplorerUIController));
            var controller = controllerGO.GetComponent<ExplorerUIController>();
            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("infoPanelText").objectReferenceValue = infoText;
            serializedController.FindProperty("startQuizButton").objectReferenceValue = buttonGO.GetComponent<Button>();
            serializedController.FindProperty("quizPanelRoot").objectReferenceValue = quizPanel;
            serializedController.FindProperty("quizPromptText").objectReferenceValue = quizPrompt;
            serializedController.FindProperty("quizScoreText").objectReferenceValue = quizScore;
            serializedController.FindProperty("quizFeedbackText").objectReferenceValue = quizFeedback;
            serializedController.FindProperty("quizTimerText").objectReferenceValue = quizTimer;
            serializedController.FindProperty("systemFilterText").objectReferenceValue = filterText;
            serializedController.FindProperty("systemFilterButton").objectReferenceValue = filterButtonGO.GetComponent<Button>();
            serializedController.FindProperty("colorblindToggleText").objectReferenceValue = colorblindText;
            serializedController.FindProperty("colorblindToggleButton").objectReferenceValue = colorblindButtonGO.GetComponent<Button>();
            serializedController.FindProperty("colorblindToggle").objectReferenceValue = WireColorblindToggle(controllerGO);
            serializedController.FindProperty("detailLevelText").objectReferenceValue = detailText;
            serializedController.FindProperty("detailLevelButton").objectReferenceValue = detailButtonGO.GetComponent<Button>();

            var layerVisibility = controllerGO.GetComponent<AnatomyLayerVisibility>();
            if (layerVisibility == null) layerVisibility = controllerGO.AddComponent<AnatomyLayerVisibility>();
            serializedController.FindProperty("layerVisibility").objectReferenceValue = layerVisibility;

            var buttonsProp = serializedController.FindProperty("layerButtons");
            var labelsProp = serializedController.FindProperty("layerButtonLabels");
            buttonsProp.arraySize = layerButtons.Length;
            labelsProp.arraySize = layerLabels.Length;
            for (int i = 0; i < layerButtons.Length; i++)
            {
                buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = layerButtons[i];
                labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = layerLabels[i];
            }
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            var feedbackGO = new GameObject("AnatomyPartFeedback", typeof(AnatomyPartFeedback));
            var feedback = feedbackGO.GetComponent<AnatomyPartFeedback>();
            var serializedFeedback = new SerializedObject(feedback);
            serializedFeedback.FindProperty("explorerUI").objectReferenceValue = controller;
            serializedFeedback.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Adds URP's Colorblind Daltonization renderer feature to the active
        /// pipeline's renderer (Phase 33 - built and shader-complete, but never
        /// actually attached to a toggle anyone could reach in play) and wires a
        /// ColorblindAccessibilityToggle component to it, defaulting to off.
        /// Returns null (leaving the UI button inert) if the renderer feature
        /// can't be located, e.g. if the project isn't on URP.
        /// </summary>
        private static ColorblindAccessibilityToggle WireColorblindToggle(GameObject controllerGO)
        {
            ColorblindFilterFeatureSetup.AddFeatureToActiveRenderer();

            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null) return null;

            var rendererDataField = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rendererDataList = rendererDataField?.GetValue(urpAsset) as ScriptableRendererData[];
            if (rendererDataList == null || rendererDataList.Length == 0) return null;

            ScriptableRendererFeature feature = null;
            foreach (var candidate in rendererDataList[0].rendererFeatures)
            {
                if (candidate is FullScreenPassRendererFeature fs && fs.name == "Colorblind Daltonization")
                {
                    feature = fs;
                    break;
                }
            }
            if (feature == null) return null;

            var toggle = controllerGO.GetComponent<ColorblindAccessibilityToggle>();
            if (toggle == null) toggle = controllerGO.AddComponent<ColorblindAccessibilityToggle>();

            var serializedToggle = new SerializedObject(toggle);
            serializedToggle.FindProperty("colorblindFeature").objectReferenceValue = feature;
            serializedToggle.ApplyModifiedPropertiesWithoutUndo();

            return toggle;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            go.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, string content, float fontSize)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static void StretchToParent(RectTransform rect, float padding, bool keepAnchors = false)
        {
            if (!keepAnchors)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
            }
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
