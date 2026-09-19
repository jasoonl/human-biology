using System.Collections.Generic;
using HumanBodyExplorer.CameraSystem;
using HumanBodyExplorer.Core;
using HumanBodyExplorer.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
    public static class HumanBodyExplorerSetup
    {
        private struct PartDef
        {
            public string Name;
            public string EntityId;
            public PrimitiveType Shape;
            public Vector3 LocalPosition;
            public Vector3 LocalScale;
            public Color Color;
            public float Smoothness;

            /// <summary>Squishy anatomy (organs/muscles/brain) gets a noise-displaced,
            /// non-perfect-sphere mesh so it doesn't read as a plastic ball; bone and
            /// skin stay smooth/hard-surfaced. Only applies to Sphere-shaped parts.</summary>
            public bool Organic;
        }

        private static readonly Color BoneColor = new Color(0.88f, 0.85f, 0.78f);
        private static readonly Color MuscleColor = new Color(0.62f, 0.2f, 0.15f);
        private static readonly Color HeartColor = new Color(0.55f, 0.05f, 0.05f);
        private static readonly Color RespColor = new Color(0.85f, 0.55f, 0.6f);
        private static readonly Color NerveColor = new Color(0.85f, 0.72f, 0.75f);
        private static readonly Color RenalColor = new Color(0.5f, 0.18f, 0.22f);
        private static readonly Color EndoColor = new Color(0.55f, 0.35f, 0.65f);
        private static readonly Color LymphColor = new Color(0.4f, 0.12f, 0.38f);
        private static readonly Color SkinColor = new Color(0.87f, 0.68f, 0.52f);
        private static readonly Color ArteryColor = new Color(0.75f, 0.08f, 0.08f);
        private static readonly Color VeinColor = new Color(0.15f, 0.25f, 0.55f);

        private const float OrganGloss = 0.35f;
        private const float BoneGloss = 0.1f;
        private const float SkinGloss = 0.15f;

        private static readonly PartDef[] Parts =
        {
            // --- Integumentary backdrop (built first, furthest back / outermost) ---
            // Sized to reach past the shoulders (humerus sits at x=+-0.32) and sit
            // behind the ribcage (front face must stay further from camera than
            // RibCage's, i.e. a larger center_z - radius_z).
            new PartDef { Name = "TorsoSkin", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0, 1.15f, 0.18f), LocalScale = new Vector3(0.8f, 0.75f, 0.3f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "SkinHead", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0, 1.65f, 0.05f), LocalScale = new Vector3(0.3f, 0.3f, 0.3f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Neck", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0, 1.58f, 0.03f), LocalScale = new Vector3(0.1f, 0.06f, 0.1f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Forearm_L", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.32f, 1.05f, 0.05f), LocalScale = new Vector3(0.1f, 0.18f, 0.1f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Forearm_R", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.32f, 1.05f, 0.05f), LocalScale = new Vector3(0.1f, 0.18f, 0.1f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "LowerLeg_L", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.1f, 0.15f, 0.05f), LocalScale = new Vector3(0.12f, 0.3f, 0.12f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "LowerLeg_R", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.1f, 0.15f, 0.05f), LocalScale = new Vector3(0.12f, 0.3f, 0.12f), Color = SkinColor, Smoothness = SkinGloss },
            // Upper arm / thigh skin sit behind the Biceps/Quadriceps bulges (larger
            // z = further from camera) so the muscle stays the visible/clickable
            // surface, but fill in the sides/back that the muscle bulge alone
            // doesn't cover - without these the bare bone shows from side angles.
            new PartDef { Name = "UpperArm_L", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.32f, 1.35f, 0.09f), LocalScale = new Vector3(0.16f, 0.24f, 0.16f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "UpperArm_R", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.32f, 1.35f, 0.09f), LocalScale = new Vector3(0.16f, 0.24f, 0.16f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Thigh_L", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.1f, 0.55f, 0.09f), LocalScale = new Vector3(0.2f, 0.36f, 0.2f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Thigh_R", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.1f, 0.55f, 0.09f), LocalScale = new Vector3(0.2f, 0.36f, 0.2f), Color = SkinColor, Smoothness = SkinGloss },
            // Hands/feet complete the limbs, which previously just stopped at the
            // wrist/ankle.
            new PartDef { Name = "Hand_L", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.32f, 0.83f, 0.05f), LocalScale = new Vector3(0.1f, 0.1f, 0.1f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Hand_R", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.32f, 0.83f, 0.05f), LocalScale = new Vector3(0.1f, 0.1f, 0.1f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Foot_L", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.1f, -0.22f, -0.03f), LocalScale = new Vector3(0.11f, 0.09f, 0.22f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Foot_R", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.1f, -0.22f, -0.03f), LocalScale = new Vector3(0.11f, 0.09f, 0.22f), Color = SkinColor, Smoothness = SkinGloss },

            // --- Skeletal ---
            new PartDef { Name = "Pelvis", EntityId = "SYS_SK_PELVIS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0, 0.9f, 0.07f), LocalScale = new Vector3(0.36f, 0.22f, 0.2f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Spine", EntityId = "SYS_SK_SPINE", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0, 1.25f, 0.09f), LocalScale = new Vector3(0.16f, 0.35f, 0.16f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "RibCage", EntityId = "SYS_SK_RIBCAGE", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0, 1.4f, 0.07f), LocalScale = new Vector3(0.44f, 0.34f, 0.28f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Skull", EntityId = "SYS_SK_SKULL", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0, 1.65f, -0.02f), LocalScale = new Vector3(0.2f, 0.2f, 0.2f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Humerus_L", EntityId = "SYS_SK_HUMERUS", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.32f, 1.35f, 0.05f), LocalScale = new Vector3(0.1f, 0.22f, 0.1f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Humerus_R", EntityId = "SYS_SK_HUMERUS", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.32f, 1.35f, 0.05f), LocalScale = new Vector3(0.1f, 0.22f, 0.1f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Femur_L", EntityId = "SYS_SK_FEMUR", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.1f, 0.55f, 0.05f), LocalScale = new Vector3(0.14f, 0.35f, 0.14f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Femur_R", EntityId = "SYS_SK_FEMUR", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.1f, 0.55f, 0.05f), LocalScale = new Vector3(0.14f, 0.35f, 0.14f), Color = BoneColor, Smoothness = BoneGloss },

            // --- Muscular ---
            new PartDef { Name = "Biceps_L", EntityId = "SYS_MUSC_BICEPS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.34f, 1.4f, -0.03f), LocalScale = new Vector3(0.16f, 0.16f, 0.16f), Color = MuscleColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Biceps_R", EntityId = "SYS_MUSC_BICEPS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.34f, 1.4f, -0.03f), LocalScale = new Vector3(0.16f, 0.16f, 0.16f), Color = MuscleColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Quadriceps_L", EntityId = "SYS_MUSC_QUADS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.1f, 0.55f, -0.06f), LocalScale = new Vector3(0.2f, 0.2f, 0.2f), Color = MuscleColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Quadriceps_R", EntityId = "SYS_MUSC_QUADS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.1f, 0.55f, -0.06f), LocalScale = new Vector3(0.2f, 0.2f, 0.2f), Color = MuscleColor, Smoothness = OrganGloss, Organic = true },

            // --- Nervous (nested inside skull, front-offset so both stay clickable) ---
            new PartDef { Name = "Brain", EntityId = "SYS_NERV_BRAIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0, 1.65f, -0.09f), LocalScale = new Vector3(0.14f, 0.14f, 0.14f), Color = NerveColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "SpinalCord", EntityId = "SYS_NERV_SPINALCORD", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0, 1.25f, 0.11f), LocalScale = new Vector3(0.05f, 0.35f, 0.05f), Color = NerveColor, Smoothness = OrganGloss },

            // --- Cardiovascular ---
            new PartDef { Name = "Heart", EntityId = "SYS_CV_HEART", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.05f, 1.4f, -0.14f), LocalScale = new Vector3(0.22f, 0.22f, 0.22f), Color = HeartColor, Smoothness = OrganGloss, Organic = true },

            // --- Respiratory ---
            new PartDef { Name = "Lung_L", EntityId = "SYS_RESP_LUNG_L", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.16f, 1.42f, -0.11f), LocalScale = new Vector3(0.2f, 0.3f, 0.16f), Color = RespColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Lung_R", EntityId = "SYS_RESP_LUNG_R", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.16f, 1.42f, -0.11f), LocalScale = new Vector3(0.2f, 0.3f, 0.16f), Color = RespColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Trachea", EntityId = "SYS_RESP_TRACHEA", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0, 1.55f, -0.06f), LocalScale = new Vector3(0.05f, 0.08f, 0.05f), Color = RespColor, Smoothness = OrganGloss },

            // --- Digestive ---
            new PartDef { Name = "Stomach", EntityId = "SYS_DIG_STOMACH", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.14f, 1.15f, -0.12f), LocalScale = new Vector3(0.19f, 0.15f, 0.15f), Color = new Color(0.8f, 0.45f, 0.4f), Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Liver", EntityId = "SYS_DIG_LIVER", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.15f, 1.15f, -0.12f), LocalScale = new Vector3(0.21f, 0.15f, 0.15f), Color = new Color(0.45f, 0.15f, 0.1f), Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Intestines", EntityId = "SYS_DIG_INTESTINES", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0, 0.95f, -0.12f), LocalScale = new Vector3(0.25f, 0.17f, 0.15f), Color = new Color(0.78f, 0.58f, 0.48f), Smoothness = OrganGloss, Organic = true },

            // --- Renal ---
            new PartDef { Name = "Kidney_L", EntityId = "SYS_REN_KIDNEY_L", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.17f, 1.0f, -0.03f), LocalScale = new Vector3(0.1f, 0.15f, 0.08f), Color = RenalColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Kidney_R", EntityId = "SYS_REN_KIDNEY_R", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.17f, 1.0f, -0.03f), LocalScale = new Vector3(0.1f, 0.15f, 0.08f), Color = RenalColor, Smoothness = OrganGloss, Organic = true },
            new PartDef { Name = "Bladder", EntityId = "SYS_REN_BLADDER", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0, 0.75f, -0.09f), LocalScale = new Vector3(0.13f, 0.13f, 0.13f), Color = new Color(0.75f, 0.7f, 0.2f), Smoothness = OrganGloss, Organic = true },

            // --- Endocrine ---
            new PartDef { Name = "Thyroid", EntityId = "SYS_ENDO_THYROID", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0, 1.58f, -0.06f), LocalScale = new Vector3(0.08f, 0.05f, 0.05f), Color = EndoColor, Smoothness = OrganGloss, Organic = true },

            // --- Lymphatic ---
            new PartDef { Name = "Spleen", EntityId = "SYS_LYMPH_SPLEEN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.23f, 1.2f, -0.09f), LocalScale = new Vector3(0.11f, 0.15f, 0.09f), Color = LymphColor, Smoothness = OrganGloss, Organic = true },

            // --- Cardiovascular vessels (visible through the now-translucent skin) ---
            new PartDef { Name = "Aorta", EntityId = "SYS_CV_ARTERY", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.02f, 1.15f, -0.05f), LocalScale = new Vector3(0.045f, 0.28f, 0.045f), Color = ArteryColor },
            new PartDef { Name = "VenaCava", EntityId = "SYS_CV_VEIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.02f, 1.15f, -0.03f), LocalScale = new Vector3(0.04f, 0.28f, 0.04f), Color = VeinColor },
            new PartDef { Name = "Artery_Arm_L", EntityId = "SYS_CV_ARTERY", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.32f, 1.1f, 0.06f), LocalScale = new Vector3(0.03f, 0.28f, 0.03f), Color = ArteryColor },
            new PartDef { Name = "Artery_Arm_R", EntityId = "SYS_CV_ARTERY", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.32f, 1.1f, 0.06f), LocalScale = new Vector3(0.03f, 0.28f, 0.03f), Color = ArteryColor },
            new PartDef { Name = "Vein_Arm_L", EntityId = "SYS_CV_VEIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.32f, 1.1f, 0.11f), LocalScale = new Vector3(0.028f, 0.28f, 0.028f), Color = VeinColor },
            new PartDef { Name = "Vein_Arm_R", EntityId = "SYS_CV_VEIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.32f, 1.1f, 0.11f), LocalScale = new Vector3(0.028f, 0.28f, 0.028f), Color = VeinColor },
            new PartDef { Name = "Artery_Leg_L", EntityId = "SYS_CV_ARTERY", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.1f, 0.3f, 0.06f), LocalScale = new Vector3(0.032f, 0.42f, 0.032f), Color = ArteryColor },
            new PartDef { Name = "Artery_Leg_R", EntityId = "SYS_CV_ARTERY", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.1f, 0.3f, 0.06f), LocalScale = new Vector3(0.032f, 0.42f, 0.032f), Color = ArteryColor },
            new PartDef { Name = "Vein_Leg_L", EntityId = "SYS_CV_VEIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.1f, 0.3f, 0.12f), LocalScale = new Vector3(0.03f, 0.42f, 0.03f), Color = VeinColor },
            new PartDef { Name = "Vein_Leg_R", EntityId = "SYS_CV_VEIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.1f, 0.3f, 0.12f), LocalScale = new Vector3(0.03f, 0.42f, 0.03f), Color = VeinColor },
        };

        [MenuItem("Human Body Explorer/Build Full Explorer (Figure + UI)")]
        public static void BuildExplorer()
        {
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
            WireUpCamera(mainCameraGO, root);
            SetupPostProcessing(mainCameraGO);
            BuildUI(mainCameraGO);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[HumanBodyExplorerSetup] Built the full explorer: " + Parts.Length +
                      " tagged body parts, info panel, and quiz mode with live timer + flash feedback. " +
                      "Press Play - the camera auto-frames the figure. Orbit with drag, zoom with scroll, " +
                      "click any part to learn about it, and press Start Quiz to test yourself.");
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

        private static void RemoveIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }

        private static void CreateGroundAndLight()
        {
            if (GameObject.Find("DemoGround") == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "DemoGround";
                ground.transform.position = new Vector3(0, -0.5f, 0);
                ground.transform.localScale = Vector3.one * 0.5f;
            }

            // Replaces the old single flat directional light (which made every part
            // look like matte plastic) with a three-point rig: the key light does
            // the heavy lifting, a dim cool fill keeps the shadow side from going
            // pure black, and a warm rim from behind separates the figure's
            // silhouette from the background.
            RemoveIfExists("Directional Light");

            if (GameObject.Find("KeyLight") == null)
            {
                var keyGO = new GameObject("KeyLight");
                var key = keyGO.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.1f;
                key.color = new Color(1f, 0.97f, 0.92f);
                keyGO.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            if (GameObject.Find("FillLight") == null)
            {
                var fillGO = new GameObject("FillLight");
                var fill = fillGO.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.35f;
                fill.color = new Color(0.75f, 0.82f, 1f);
                fillGO.transform.rotation = Quaternion.Euler(30, 150, 0);
            }

            if (GameObject.Find("RimLight") == null)
            {
                var rimGO = new GameObject("RimLight");
                var rim = rimGO.AddComponent<Light>();
                rim.type = LightType.Directional;
                rim.intensity = 0.6f;
                rim.color = new Color(1f, 0.85f, 0.65f);
                rimGO.transform.rotation = Quaternion.Euler(15, 200, 0);
            }
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
            var skinNoiseTex = CreateNoiseTexture(64, 0.85f, 1f, 6f, 11);

            foreach (var part in Parts)
            {
                var go = GameObject.CreatePrimitive(part.Shape);
                go.name = part.Name;
                go.layer = anatomyLayer;
                go.transform.SetParent(root.transform, worldPositionStays: false);
                go.transform.localPosition = part.LocalPosition;
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
                        MakeTransparent(material, alpha: 0.42f);
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

            return root;
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
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.25f);
            bloom.scatter.Override(0.6f);

            if (!profile.TryGet(out ColorAdjustments colorAdjustments)) colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.Override(0.1f);
            colorAdjustments.contrast.Override(8f);
            colorAdjustments.saturation.Override(6f);

            if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
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

        private static void BuildUI(GameObject mainCameraGO)
        {
            RemoveIfExists("ExplorerCanvas");
            RemoveIfExists("ExplorerUIController");
            RemoveIfExists("AnatomyPartFeedback");

            var canvasGO = new GameObject("ExplorerCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Info panel (bottom-left)
            var infoPanel = CreatePanel(canvasGO.transform, "InfoPanel",
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(0, 0),
                pivot: new Vector2(0, 0), anchoredPos: new Vector2(30, 30), size: new Vector2(560, 220));
            var infoText = CreateText(infoPanel.transform, "InfoText", "Click on a body part to learn what it does.", 24);
            StretchToParent(infoText.rectTransform, padding: 20);

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
            buttonRect.anchoredPosition = new Vector2(30, -30);
            buttonRect.sizeDelta = new Vector2(200, 60);
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
            filterButtonRect.anchoredPosition = new Vector2(30, -100);
            filterButtonRect.sizeDelta = new Vector2(200, 50);
            filterButtonGO.GetComponent<Image>().color = new Color(0.25f, 0.3f, 0.45f);

            var filterText = CreateText(filterButtonGO.transform, "Text", "Study: All", 20);
            StretchToParent(filterText.rectTransform, padding: 0);
            filterText.alignment = TextAlignmentOptions.Center;

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
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            var feedbackGO = new GameObject("AnatomyPartFeedback", typeof(AnatomyPartFeedback));
            var feedback = feedbackGO.GetComponent<AnatomyPartFeedback>();
            var serializedFeedback = new SerializedObject(feedback);
            serializedFeedback.FindProperty("explorerUI").objectReferenceValue = controller;
            serializedFeedback.ApplyModifiedPropertiesWithoutUndo();
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
