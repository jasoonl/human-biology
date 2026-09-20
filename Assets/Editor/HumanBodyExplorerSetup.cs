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
        private static readonly Color BoneColor = new Color(0.93f, 0.90f, 0.83f);
        private static readonly Color MuscleColor = new Color(0.58f, 0.19f, 0.16f);
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
            new PartDef { Name = "SkinHead", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.635f, 0.005f), LocalScale = new Vector3(0.165f, 0.235f, 0.21f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Neck", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0f, 1.485f, 0.005f), LocalScale = new Vector3(0.13f, 0.06f, 0.13f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "SkinChest", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.28f, -0.005f), LocalScale = new Vector3(0.36f, 0.42f, 0.25f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "SkinAbdomen", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.06f, -0.005f), LocalScale = new Vector3(0.33f, 0.34f, 0.23f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "SkinPelvis", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 0.9f, 0f), LocalScale = new Vector3(0.355f, 0.28f, 0.245f), Color = SkinColor, Smoothness = SkinGloss },
            // The deltoid cap is the widest point of the body (bideltoid breadth
            // ~0.50 m), so the shoulder needs its own skin segment rather than being
            // squeezed under the chest ellipsoid.
            new PartDef { Name = "Shoulder_L", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.19f, 1.39f, -0.01f), LocalScale = new Vector3(0.145f, 0.165f, 0.145f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Shoulder_R", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.19f, 1.39f, -0.01f), LocalScale = new Vector3(0.145f, 0.165f, 0.145f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "UpperArm_L", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.19f, 1.26f, 0f), LocalScale = new Vector3(0.115f, 0.165f, 0.115f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "UpperArm_R", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.19f, 1.26f, 0f), LocalScale = new Vector3(0.115f, 0.165f, 0.115f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Forearm_L", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.2f, 0.96f, 0f), LocalScale = new Vector3(0.09f, 0.145f, 0.09f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Forearm_R", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.2f, 0.96f, 0f), LocalScale = new Vector3(0.09f, 0.145f, 0.09f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Hand_L", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.205f, 0.735f, 0f), LocalScale = new Vector3(0.085f, 0.17f, 0.045f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Hand_R", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.205f, 0.735f, 0f), LocalScale = new Vector3(0.085f, 0.17f, 0.045f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Thigh_L", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.09f, 0.65f, 0f), LocalScale = new Vector3(0.155f, 0.215f, 0.17f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Thigh_R", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.09f, 0.65f, 0f), LocalScale = new Vector3(0.155f, 0.215f, 0.17f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "LowerLeg_L", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.085f, 0.28f, 0.005f), LocalScale = new Vector3(0.115f, 0.22f, 0.13f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "LowerLeg_R", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.085f, 0.28f, 0.005f), LocalScale = new Vector3(0.115f, 0.22f, 0.13f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Foot_L", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.085f, 0.035f, -0.06f), LocalScale = new Vector3(0.10f, 0.07f, 0.25f), Color = SkinColor, Smoothness = SkinGloss },
            new PartDef { Name = "Foot_R", EntityId = "SYS_INTEG_SKIN", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.085f, 0.035f, -0.06f), LocalScale = new Vector3(0.10f, 0.07f, 0.25f), Color = SkinColor, Smoothness = SkinGloss },

            // ================= SKELETAL =================
            new PartDef { Name = "Skull", EntityId = "SYS_SK_SKULL", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.64f, 0.01f), LocalScale = new Vector3(0.145f, 0.19f, 0.175f), Color = BoneColor, Smoothness = BoneGloss },
            // Runs from the sacrum (~0.88) to the atlas (~1.50), behind the body's
            // mid-plane, where a real vertebral column sits.
            new PartDef { Name = "VertebralColumn", EntityId = "SYS_SK_SPINE", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0f, 1.19f, 0.065f), LocalScale = new Vector3(0.045f, 0.31f, 0.045f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "RibCage", EntityId = "SYS_SK_RIBCAGE", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.30f, -0.01f), LocalScale = new Vector3(0.275f, 0.30f, 0.19f), Color = BoneColor, Smoothness = BoneGloss },
            // Near-horizontal struts from sternum to acromion.
            new PartDef { Name = "Clavicle_L", EntityId = "SYS_SK_CLAVICLE", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.095f, 1.415f, -0.055f), LocalScale = new Vector3(0.022f, 0.075f, 0.022f),
                LocalEuler = new Vector3(0f, 0f, 78f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Clavicle_R", EntityId = "SYS_SK_CLAVICLE", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.095f, 1.415f, -0.055f), LocalScale = new Vector3(0.022f, 0.075f, 0.022f),
                LocalEuler = new Vector3(0f, 0f, -78f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Pelvis", EntityId = "SYS_SK_PELVIS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 0.90f, 0.02f), LocalScale = new Vector3(0.29f, 0.17f, 0.195f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Humerus_L", EntityId = "SYS_SK_HUMERUS", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.19f, 1.26f, 0.01f), LocalScale = new Vector3(0.04f, 0.17f, 0.04f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Humerus_R", EntityId = "SYS_SK_HUMERUS", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.19f, 1.26f, 0.01f), LocalScale = new Vector3(0.04f, 0.17f, 0.04f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "RadiusUlna_L", EntityId = "SYS_SK_RADIUS_ULNA", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.2f, 0.96f, 0.01f), LocalScale = new Vector3(0.035f, 0.13f, 0.035f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "RadiusUlna_R", EntityId = "SYS_SK_RADIUS_ULNA", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.2f, 0.96f, 0.01f), LocalScale = new Vector3(0.035f, 0.13f, 0.035f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Femur_L", EntityId = "SYS_SK_FEMUR", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.09f, 0.675f, 0.01f), LocalScale = new Vector3(0.05f, 0.205f, 0.05f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Femur_R", EntityId = "SYS_SK_FEMUR", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.09f, 0.675f, 0.01f), LocalScale = new Vector3(0.05f, 0.205f, 0.05f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Tibia_L", EntityId = "SYS_SK_TIBIA", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.08f, 0.27f, 0.01f), LocalScale = new Vector3(0.042f, 0.20f, 0.042f), Color = BoneColor, Smoothness = BoneGloss },
            new PartDef { Name = "Tibia_R", EntityId = "SYS_SK_TIBIA", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.08f, 0.27f, 0.01f), LocalScale = new Vector3(0.042f, 0.20f, 0.042f), Color = BoneColor, Smoothness = BoneGloss },

            // ================= MUSCULAR =================
            new PartDef { Name = "Pectoralis_L", EntityId = "SYS_MUSC_PECTORALIS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.085f, 1.335f, -0.065f), LocalScale = new Vector3(0.13f, 0.12f, 0.045f), Color = MuscleColor, Smoothness = MuscleGloss, Organic = true },
            new PartDef { Name = "Pectoralis_R", EntityId = "SYS_MUSC_PECTORALIS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.085f, 1.335f, -0.065f), LocalScale = new Vector3(0.13f, 0.12f, 0.045f), Color = MuscleColor, Smoothness = MuscleGloss, Organic = true },
            new PartDef { Name = "Deltoid_L", EntityId = "SYS_MUSC_DELTOID", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.19f, 1.395f, -0.01f), LocalScale = new Vector3(0.115f, 0.13f, 0.115f), Color = MuscleColor, Smoothness = MuscleGloss, Organic = true },
            new PartDef { Name = "Deltoid_R", EntityId = "SYS_MUSC_DELTOID", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.19f, 1.395f, -0.01f), LocalScale = new Vector3(0.115f, 0.13f, 0.115f), Color = MuscleColor, Smoothness = MuscleGloss, Organic = true },
            new PartDef { Name = "Biceps_L", EntityId = "SYS_MUSC_BICEPS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.19f, 1.235f, -0.025f), LocalScale = new Vector3(0.085f, 0.17f, 0.06f), Color = MuscleColor, Smoothness = MuscleGloss, Organic = true },
            new PartDef { Name = "Biceps_R", EntityId = "SYS_MUSC_BICEPS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.19f, 1.235f, -0.025f), LocalScale = new Vector3(0.085f, 0.17f, 0.06f), Color = MuscleColor, Smoothness = MuscleGloss, Organic = true },
            new PartDef { Name = "RectusAbdominis", EntityId = "SYS_MUSC_RECTUS_ABDOMINIS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.11f, -0.085f), LocalScale = new Vector3(0.15f, 0.25f, 0.045f), Color = MuscleColor, Smoothness = MuscleGloss, Organic = true },
            new PartDef { Name = "Quadriceps_L", EntityId = "SYS_MUSC_QUADS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.09f, 0.685f, -0.035f), LocalScale = new Vector3(0.14f, 0.3f, 0.09f), Color = MuscleColor, Smoothness = MuscleGloss, Organic = true },
            new PartDef { Name = "Quadriceps_R", EntityId = "SYS_MUSC_QUADS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.09f, 0.685f, -0.035f), LocalScale = new Vector3(0.14f, 0.3f, 0.09f), Color = MuscleColor, Smoothness = MuscleGloss, Organic = true },
            // The calf sits behind the leg (+z is posterior here).
            new PartDef { Name = "Gastrocnemius_L", EntityId = "SYS_MUSC_GASTROCNEMIUS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0.085f, 0.355f, 0.03f), LocalScale = new Vector3(0.11f, 0.19f, 0.07f), Color = MuscleColor, Smoothness = MuscleGloss, Organic = true },
            new PartDef { Name = "Gastrocnemius_R", EntityId = "SYS_MUSC_GASTROCNEMIUS", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(-0.085f, 0.355f, 0.03f), LocalScale = new Vector3(0.11f, 0.19f, 0.07f), Color = MuscleColor, Smoothness = MuscleGloss, Organic = true },
            // Domed sheet at the thoracic/abdominal boundary, just under the lung bases.
            new PartDef { Name = "Diaphragm", EntityId = "SYS_RESP_DIAPHRAGM", Shape = PrimitiveType.Sphere,
                LocalPosition = new Vector3(0f, 1.205f, -0.005f), LocalScale = new Vector3(0.26f, 0.085f, 0.175f), Color = MuscleColor, Smoothness = MuscleGloss, Organic = true },

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
            new PartDef { Name = "Artery_Arm_L", EntityId = "SYS_CV_ARTERY", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.185f, 1.12f, -0.015f), LocalScale = new Vector3(0.016f, 0.155f, 0.016f), Color = ArteryColor, Smoothness = OrganGloss },
            new PartDef { Name = "Artery_Arm_R", EntityId = "SYS_CV_ARTERY", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.185f, 1.12f, -0.015f), LocalScale = new Vector3(0.016f, 0.155f, 0.016f), Color = ArteryColor, Smoothness = OrganGloss },
            new PartDef { Name = "Vein_Arm_L", EntityId = "SYS_CV_VEIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.205f, 1.12f, -0.03f), LocalScale = new Vector3(0.014f, 0.155f, 0.014f), Color = VeinColor, Smoothness = OrganGloss },
            new PartDef { Name = "Vein_Arm_R", EntityId = "SYS_CV_VEIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.205f, 1.12f, -0.03f), LocalScale = new Vector3(0.014f, 0.155f, 0.014f), Color = VeinColor, Smoothness = OrganGloss },
            new PartDef { Name = "Artery_Leg_L", EntityId = "SYS_CV_ARTERY", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.085f, 0.50f, -0.025f), LocalScale = new Vector3(0.017f, 0.195f, 0.017f), Color = ArteryColor, Smoothness = OrganGloss },
            new PartDef { Name = "Artery_Leg_R", EntityId = "SYS_CV_ARTERY", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.085f, 0.50f, -0.025f), LocalScale = new Vector3(0.017f, 0.195f, 0.017f), Color = ArteryColor, Smoothness = OrganGloss },
            new PartDef { Name = "Vein_Leg_L", EntityId = "SYS_CV_VEIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(0.1f, 0.5f, -0.03f), LocalScale = new Vector3(0.015f, 0.195f, 0.015f), Color = VeinColor, Smoothness = OrganGloss },
            new PartDef { Name = "Vein_Leg_R", EntityId = "SYS_CV_VEIN", Shape = PrimitiveType.Capsule,
                LocalPosition = new Vector3(-0.1f, 0.5f, -0.03f), LocalScale = new Vector3(0.015f, 0.195f, 0.015f), Color = VeinColor, Smoothness = OrganGloss },

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
                LocalPosition = new Vector3(-0.07f, 1.18f, -0.04f), LocalScale = new Vector3(0.175f, 0.11f, 0.13f), Color = LiverColor, Smoothness = OrganGloss, Organic = true },
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
            // The figure is built to real scale (1.75 m, soles at y = 0), so the floor
            // belongs at y = 0. Set it every rebuild rather than only on creation, so
            // an existing ground plane from an older layout gets corrected.
            var ground = GameObject.Find("DemoGround");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "DemoGround";
            }
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one * 0.5f;

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

            // A click through the torso now crosses the skin shell, ribcage, a muscle,
            // a lung, the heart, the diaphragm and more. The default 16-hit buffer
            // silently truncates at that depth, which would hide the deepest organs.
            var serializedRaycaster = new SerializedObject(raycaster);
            serializedRaycaster.FindProperty("maxHits").intValue = 48;
            serializedRaycaster.ApplyModifiedPropertiesWithoutUndo();

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
            // Taller than before: at AP Biology / Clinical detail this panel carries
            // several paragraphs of facts rather than a single sentence.
            var infoPanel = CreatePanel(canvasGO.transform, "InfoPanel",
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(0, 0),
                pivot: new Vector2(0, 0), anchoredPos: new Vector2(30, 30), size: new Vector2(760, 560));
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

            // Colorblind accessibility toggle (below the system filter) - off by
            // default, boosts red/blue contrast between arteries and veins.
            var colorblindButtonGO = new GameObject("ColorblindToggleButton", typeof(Image), typeof(Button));
            colorblindButtonGO.transform.SetParent(canvasGO.transform, false);
            var colorblindButtonRect = colorblindButtonGO.GetComponent<RectTransform>();
            colorblindButtonRect.anchorMin = new Vector2(0, 1);
            colorblindButtonRect.anchorMax = new Vector2(0, 1);
            colorblindButtonRect.pivot = new Vector2(0, 1);
            colorblindButtonRect.anchoredPosition = new Vector2(30, -160);
            colorblindButtonRect.sizeDelta = new Vector2(200, 50);
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
            detailButtonRect.anchoredPosition = new Vector2(30, -220);
            detailButtonRect.sizeDelta = new Vector2(200, 50);
            detailButtonGO.GetComponent<Image>().color = new Color(0.22f, 0.36f, 0.34f);

            var detailText = CreateText(detailButtonGO.transform, "Text", "Detail: Plain English", 18);
            StretchToParent(detailText.rectTransform, padding: 0);
            detailText.alignment = TextAlignmentOptions.Center;

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
