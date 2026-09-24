using System;
using System.Collections.Generic;
using System.IO;
using HumanBodyExplorer.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// Renders the Bootstrap scene from a set of fixed viewpoints to PNG files, so the
    /// figure can be looked at without a person driving the Editor. This is what makes
    /// visual problems - a backdrop that swallows bone, an egg where a pelvis should be -
    /// findable in the same session that introduces them, instead of only when a
    /// screenshot comes back from someone else.
    ///
    /// Run without -nographics (batchmode alone still gets a Metal device on macOS):
    ///   Unity -batchmode -projectPath . -executeMethod HumanBodyExplorer.EditorTools.ScreenshotTool.CaptureViews -outDir /path
    ///
    /// Nothing here saves the scene; layer toggles and camera moves are throwaway.
    /// </summary>
    public static class ScreenshotTool
    {
        private const int Width = 1400;
        private const int Height = 1100;

        private struct View
        {
            public string Name;
            public Vector3 Target;
            public float Distance;
            public float Yaw;          // degrees; 0 = looking at the figure's front (+Z gaze)
            public float Pitch;
            public AnatomyLayerGroup[] Visible; // null = leave every layer on
            public float Fov;          // 0 = the camera's own
        }

        private static readonly View[] Views =
        {
            new View { Name = "01_full_all", Target = new Vector3(0f, 0.88f, 0f), Distance = 3.6f },
            new View { Name = "02_full_skeleton", Target = new Vector3(0f, 0.88f, 0f), Distance = 3.6f,
                Visible = new[] { AnatomyLayerGroup.Skeletal } },
            new View { Name = "03_full_muscle", Target = new Vector3(0f, 0.88f, 0f), Distance = 3.6f,
                Visible = new[] { AnatomyLayerGroup.Muscular } },
            new View { Name = "04_full_organs", Target = new Vector3(0f, 0.88f, 0f), Distance = 3.6f,
                Visible = new[] { AnatomyLayerGroup.Organs } },
            new View { Name = "05_full_vessels", Target = new Vector3(0f, 0.88f, 0f), Distance = 3.6f,
                Visible = new[] { AnatomyLayerGroup.Circulatory } },
            new View { Name = "06_full_nerves", Target = new Vector3(0f, 0.88f, 0f), Distance = 3.6f,
                Visible = new[] { AnatomyLayerGroup.Nervous } },
            new View { Name = "07_head_all", Target = new Vector3(0f, 1.62f, 0f), Distance = 0.75f },
            new View { Name = "08_head_skull", Target = new Vector3(0f, 1.62f, 0f), Distance = 0.75f,
                Visible = new[] { AnatomyLayerGroup.Skeletal } },
            new View { Name = "09_torso_all", Target = new Vector3(0f, 1.2f, 0f), Distance = 1.5f },
            new View { Name = "10_torso_organs_vessels", Target = new Vector3(0f, 1.2f, 0f), Distance = 1.5f,
                Visible = new[] { AnatomyLayerGroup.Organs, AnatomyLayerGroup.Circulatory } },
            new View { Name = "11_pelvis_skeleton", Target = new Vector3(0f, 0.92f, 0f), Distance = 1.1f,
                Visible = new[] { AnatomyLayerGroup.Skeletal } },
            new View { Name = "12_shoulder_skeleton", Target = new Vector3(0f, 1.34f, 0f), Distance = 1.1f,
                Visible = new[] { AnatomyLayerGroup.Skeletal } },
            new View { Name = "13_side_skeleton", Target = new Vector3(0f, 0.88f, 0f), Distance = 3.6f, Yaw = 90f,
                Visible = new[] { AnatomyLayerGroup.Skeletal } },
            new View { Name = "14_back_muscle", Target = new Vector3(0f, 1.1f, 0f), Distance = 2.4f, Yaw = 180f,
                Visible = new[] { AnatomyLayerGroup.Muscular } },
            new View { Name = "15_torso_vessels", Target = new Vector3(0f, 1.2f, 0f), Distance = 1.5f,
                Visible = new[] { AnatomyLayerGroup.Circulatory } },
            new View { Name = "16_torso_nerves", Target = new Vector3(0f, 1.2f, 0f), Distance = 1.5f,
                Visible = new[] { AnatomyLayerGroup.Nervous } },
            new View { Name = "17_head_vessels_nerves", Target = new Vector3(0f, 1.62f, 0f), Distance = 0.75f,
                Visible = new[] { AnatomyLayerGroup.Circulatory, AnatomyLayerGroup.Nervous } },
            new View { Name = "18_arm_vessels_nerves", Target = new Vector3(0.2f, 1.1f, 0f), Distance = 1.1f,
                Visible = new[] { AnatomyLayerGroup.Circulatory, AnatomyLayerGroup.Nervous } },
            new View { Name = "19_leg_vessels_nerves", Target = new Vector3(0.09f, 0.5f, 0f), Distance = 1.4f,
                Visible = new[] { AnatomyLayerGroup.Circulatory, AnatomyLayerGroup.Nervous } },
            new View { Name = "20_hand_vessels_nerves", Target = new Vector3(0.22f, 0.8f, 0f), Distance = 0.35f,
                Visible = new[] { AnatomyLayerGroup.Circulatory, AnatomyLayerGroup.Nervous } },
            new View { Name = "21_back_spine_nerves", Target = new Vector3(0f, 1.2f, 0.05f), Distance = 1.8f, Yaw = 180f,
                Visible = new[] { AnatomyLayerGroup.Skeletal, AnatomyLayerGroup.Nervous } },
            new View { Name = "22_full_vessels_nerves_bones", Target = new Vector3(0f, 0.88f, 0f), Distance = 3.6f,
                Visible = new[] { AnatomyLayerGroup.Skeletal, AnatomyLayerGroup.Circulatory, AnatomyLayerGroup.Nervous } },
            new View { Name = "24_neck_thorax_vessels", Target = new Vector3(0f, 1.36f, 0f), Distance = 0.9f, Fov = 30f,
                Visible = new[] { AnatomyLayerGroup.Circulatory } },
            new View { Name = "25_thorax_vessels_organs", Target = new Vector3(0f, 1.28f, 0f), Distance = 0.9f, Fov = 30f,
                Visible = new[] { AnatomyLayerGroup.Circulatory, AnatomyLayerGroup.Organs } },
            new View { Name = "26_thorax_vessels_bones", Target = new Vector3(0f, 1.28f, 0f), Distance = 0.9f, Fov = 30f,
                Visible = new[] { AnatomyLayerGroup.Circulatory, AnatomyLayerGroup.Skeletal } },
            new View { Name = "27_abdomen_vessels_organs", Target = new Vector3(0f, 1.05f, 0f), Distance = 0.9f, Fov = 30f,
                Visible = new[] { AnatomyLayerGroup.Circulatory, AnatomyLayerGroup.Organs } },
            new View { Name = "28_abdomen_vessels_only", Target = new Vector3(0f, 1.05f, 0f), Distance = 0.9f, Fov = 30f,
                Visible = new[] { AnatomyLayerGroup.Circulatory } },
            new View { Name = "29_head_nerves_skull", Target = new Vector3(0f, 1.62f, 0f), Distance = 0.7f, Fov = 30f,
                Visible = new[] { AnatomyLayerGroup.Nervous, AnatomyLayerGroup.Skeletal } },
            new View { Name = "30_head_nerves_only", Target = new Vector3(0f, 1.62f, 0f), Distance = 0.7f, Fov = 30f,
                Visible = new[] { AnatomyLayerGroup.Nervous } },
            new View { Name = "31_neck_nerves_only", Target = new Vector3(0f, 1.42f, 0f), Distance = 0.9f, Fov = 30f,
                Visible = new[] { AnatomyLayerGroup.Nervous } },
            new View { Name = "32_pelvis_vessels_bones", Target = new Vector3(0f, 0.92f, 0f), Distance = 0.9f, Fov = 30f,
                Visible = new[] { AnatomyLayerGroup.Circulatory, AnatomyLayerGroup.Nervous, AnatomyLayerGroup.Skeletal } },
            new View { Name = "23_foot_vessels_nerves", Target = new Vector3(0.09f, 0.06f, -0.04f), Distance = 0.5f,
                Visible = new[] { AnatomyLayerGroup.Circulatory, AnatomyLayerGroup.Nervous } },
        };

        [MenuItem("Human Body Explorer/Capture Review Screenshots")]
        public static void CaptureViews()
        {
            string outDir = ArgValue("-outDir") ?? Path.Combine(Application.dataPath, "../Temp/Screenshots");
            Directory.CreateDirectory(outDir);

            // Batch mode renders a flat placeholder while shaders compile in the background, which
            // tints the first frames; compile synchronously so every render is the real one.
            bool previousAsync = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;
            EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity");

            var cameraGO = GameObject.FindWithTag("MainCamera");
            if (cameraGO == null)
            {
                Debug.LogError("[ScreenshotTool] No Main Camera in Bootstrap.unity.");
                EditorApplication.Exit(1);
                return;
            }

            var cam = cameraGO.GetComponent<Camera>();
            float defaultFov = cam.fieldOfView;
            var layers = UnityEngine.Object.FindAnyObjectByType<AnatomyLayerVisibility>();
            if (layers == null)
                Debug.LogWarning("[ScreenshotTool] No AnatomyLayerVisibility in scene; layer isolation views will show everything.");
            else
                layers.Rebuild();

            // Report exactly what the camera clears to. The backdrop is one of the things
            // this tool exists to check, and it is set in code that a post-processing or
            // camera-stack change could quietly override.
            Debug.Log($"[ScreenshotTool] camera clearFlags={cam.clearFlags} background={cam.backgroundColor} " +
                      $"fov={cam.fieldOfView} near={cam.nearClipPlane} far={cam.farClipPlane}");

            // The very first frame is drawn before textures and materials have finished uploading, so
            // it comes out washed out. Render once and throw it away.
            Render(cam, Path.Combine(outDir, "_warmup.png"));
            File.Delete(Path.Combine(outDir, "_warmup.png"));

            foreach (var view in Views)
            {
                if (layers != null)
                {
                    foreach (var group in AnatomyLayerVisibility.AllGroups)
                        layers.SetVisible(group, view.Visible == null || Array.IndexOf(view.Visible, group) >= 0);
                }

                cam.fieldOfView = view.Fov > 0f ? view.Fov : defaultFov;
                var rotation = Quaternion.Euler(view.Pitch, view.Yaw, 0f);
                // The figure faces -Z, so the default camera sits at -Z looking toward +Z.
                cam.transform.rotation = rotation;
                cam.transform.position = view.Target - rotation * Vector3.forward * view.Distance;

                string path = Path.Combine(outDir, view.Name + ".png");
                Render(cam, path);
                Debug.Log($"[ScreenshotTool] wrote {path}");
            }

            EditorSettings.asyncShaderCompilation = previousAsync;
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Renders one view several times with individual pieces of the render pipeline
        /// switched off, reporting the backdrop pixel each time. When something is
        /// tinting or darkening the whole frame, whichever toggle brings the value back
        /// to the camera's own clear colour is the culprit.
        /// </summary>
        [MenuItem("Human Body Explorer/Diagnose Backdrop Darkening")]
        public static void DiagnoseBackdrop()
        {
            string outDir = ArgValue("-outDir") ?? Path.Combine(Application.dataPath, "../Temp/Screenshots");
            Directory.CreateDirectory(outDir);
            EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity");

            var cam = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
            var camData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            cam.transform.rotation = Quaternion.identity;
            cam.transform.position = new Vector3(0f, 0.88f, -3.6f);

            var rendererData = GetRendererData();
            UnityEngine.Rendering.Universal.ScriptableRendererFeature outline = null;
            if (rendererData != null)
                foreach (var f in rendererData.rendererFeatures)
                    if (f != null && f.name == "Anatomy Outline") outline = f;

            Debug.Log($"[Diag] clear colour {cam.backgroundColor}; colorSpace={PlayerSettings.colorSpace}; " +
                      $"renderPostProcessing={camData.renderPostProcessing}; outlineFeature={(outline != null ? outline.isActive.ToString() : "MISSING")}");

            var volumes = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None);
            Debug.Log($"[Diag] {volumes.Length} volume(s) in scene");

            void Probe(string label)
            {
                string path = Path.Combine(outDir, "diag_" + label + ".png");
                Render(cam, path);
                var rt = RenderTexture.active;
                var t = new Texture2D(2, 2);
                var bytes = File.ReadAllBytes(path);
                t.LoadImage(bytes);
                var c = t.GetPixel(20, 20);
                Debug.Log($"[Diag] {label,-28} backdrop = ({Mathf.RoundToInt(c.r * 255)}, {Mathf.RoundToInt(c.g * 255)}, {Mathf.RoundToInt(c.b * 255)})");
                UnityEngine.Object.DestroyImmediate(t);
            }

            Probe("baseline");

            if (outline != null) { outline.SetActive(false); Probe("no_outline"); outline.SetActive(true); }

            camData.renderPostProcessing = false;
            Probe("no_postprocessing");
            camData.renderPostProcessing = true;

            foreach (var v in volumes) v.enabled = false;
            Probe("no_volumes");
            foreach (var v in volumes) v.enabled = true;

            if (outline != null) outline.SetActive(false);
            camData.renderPostProcessing = false;
            Probe("no_outline_no_post");

            EditorApplication.Exit(0);
        }

        private static UnityEngine.Rendering.Universal.ScriptableRendererData GetRendererData()
        {
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline
                as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (urp == null) return null;
            var field = typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)
                .GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = field?.GetValue(urp) as UnityEngine.Rendering.Universal.ScriptableRendererData[];
            return list != null && list.Length > 0 ? list[0] : null;
        }

        private static void Render(Camera cam, string path)
        {
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = cam.targetTexture;
            var previousActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());

            cam.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static string ArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
