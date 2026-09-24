using System;
using System.IO;
using HumanBodyExplorer.EditorTools.Geometry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// Builds one region of the figure into a throwaway scene and renders close-ups, so
    /// a bone or muscle can be judged in seconds rather than after a full scene rebuild.
    /// The scene is never saved. Same lighting and backdrop as the real one.
    ///
    ///   Unity -batchmode -projectPath . -executeMethod HumanBodyExplorer.EditorTools.PreviewTool.Run
    ///         -what skeleton -outDir /path
    /// </summary>
    public static class PreviewTool
    {
        private struct Shot
        {
            public string Name;
            public Vector3 Target;
            public float Distance;
            public float Yaw, Pitch;
        }

        private static readonly Shot[] SkeletonShots =
        {
            new Shot { Name = "pelvis_front", Target = new Vector3(0f, 0.91f, 0f), Distance = 0.75f },
            new Shot { Name = "pelvis_back", Target = new Vector3(0f, 0.91f, 0f), Distance = 0.75f, Yaw = 180f },
            new Shot { Name = "pelvis_oblique", Target = new Vector3(0.03f, 0.90f, 0f), Distance = 0.7f, Yaw = -40f, Pitch = 15f },
            new Shot { Name = "shoulder_back", Target = new Vector3(0.10f, 1.35f, 0.04f), Distance = 0.55f, Yaw = 180f },
            new Shot { Name = "shoulder_front", Target = new Vector3(0.10f, 1.35f, 0.02f), Distance = 0.55f },
            new Shot { Name = "leg_front", Target = new Vector3(0.09f, 0.48f, 0f), Distance = 1.35f },
            new Shot { Name = "arm_front", Target = new Vector3(0.19f, 1.10f, 0f), Distance = 0.95f },
            new Shot { Name = "thorax_front", Target = new Vector3(0f, 1.30f, 0f), Distance = 1.0f },
            new Shot { Name = "thorax_back", Target = new Vector3(0f, 1.30f, 0f), Distance = 1.0f, Yaw = 180f },
            new Shot { Name = "thorax_side", Target = new Vector3(0f, 1.30f, 0f), Distance = 1.1f, Yaw = 90f },
            new Shot { Name = "spine_side", Target = new Vector3(0f, 1.25f, 0.04f), Distance = 1.9f, Yaw = 90f },
            new Shot { Name = "neck_side", Target = new Vector3(0f, 1.50f, 0.03f), Distance = 0.45f, Yaw = 90f },
            new Shot { Name = "head_front", Target = new Vector3(0f, 1.63f, -0.02f), Distance = 0.55f },
            new Shot { Name = "head_side", Target = new Vector3(0f, 1.63f, 0.0f), Distance = 0.55f, Yaw = 90f },
            new Shot { Name = "head_oblique", Target = new Vector3(0f, 1.63f, 0.0f), Distance = 0.5f, Yaw = -35f, Pitch = 10f },
            new Shot { Name = "head_back", Target = new Vector3(0f, 1.64f, 0.0f), Distance = 0.55f, Yaw = 180f },
            new Shot { Name = "hand_front", Target = new Vector3(0.215f, 0.75f, 0f), Distance = 0.42f },
            new Shot { Name = "foot_side", Target = new Vector3(0.085f, 0.06f, -0.06f), Distance = 0.55f, Yaw = 90f },
            new Shot { Name = "foot_top", Target = new Vector3(0.085f, 0.03f, -0.06f), Distance = 0.5f, Pitch = 80f },
            new Shot { Name = "skeleton_full", Target = new Vector3(0f, 0.88f, 0f), Distance = 2.6f },
        };

        private static readonly Shot[] MuscleShots =
        {
            new Shot { Name = "torso_front", Target = new Vector3(0f, 1.22f, 0f), Distance = 1.15f },
            new Shot { Name = "torso_back", Target = new Vector3(0f, 1.25f, 0f), Distance = 1.15f, Yaw = 180f },
            new Shot { Name = "torso_side", Target = new Vector3(0.05f, 1.22f, 0f), Distance = 1.2f, Yaw = -90f },
            new Shot { Name = "shoulder_front", Target = new Vector3(0.14f, 1.30f, -0.03f), Distance = 0.7f, Yaw = -20f },
            new Shot { Name = "arm_front", Target = new Vector3(0.20f, 1.05f, 0f), Distance = 1.0f },
            new Shot { Name = "forearm_back", Target = new Vector3(0.20f, 0.98f, 0f), Distance = 0.7f, Yaw = 180f },
            new Shot { Name = "pelvis_back", Target = new Vector3(0.04f, 0.93f, 0.05f), Distance = 0.8f, Yaw = 180f },
            new Shot { Name = "thigh_front", Target = new Vector3(0.09f, 0.68f, 0f), Distance = 1.0f },
            new Shot { Name = "thigh_back", Target = new Vector3(0.09f, 0.68f, 0f), Distance = 1.0f, Yaw = 180f },
            new Shot { Name = "calf_back", Target = new Vector3(0.085f, 0.27f, 0.02f), Distance = 0.9f, Yaw = 180f },
            new Shot { Name = "shin_front", Target = new Vector3(0.085f, 0.27f, 0f), Distance = 0.9f },
            new Shot { Name = "face_front", Target = new Vector3(0f, 1.63f, -0.02f), Distance = 0.55f },
            new Shot { Name = "face_side", Target = new Vector3(0f, 1.62f, 0f), Distance = 0.6f, Yaw = -90f },
            new Shot { Name = "full_front", Target = new Vector3(0f, 0.9f, 0f), Distance = 3.0f },
            new Shot { Name = "full_back", Target = new Vector3(0f, 0.9f, 0f), Distance = 3.0f, Yaw = 180f },
        };

        [MenuItem("Human Body Explorer/Preview Region")]
        public static void Run()
        {
            string what = ArgValue("-what") ?? "skeleton";
            string outDir = ArgValue("-outDir") ?? Path.Combine(Application.dataPath, "../Temp/Preview");
            Directory.CreateDirectory(outDir);

            AnatomyOutlineFeatureSetup.AddFeatureToActiveRenderer();   // outline + ambient occlusion, as in the real scene
            var camera = NewPreviewScene();
            var root = new GameObject("HumanBodyRoot").transform;
            int layer = 0;

            var boneMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.93f, 0.90f, 0.83f) };
            boneMat.SetFloat("_Smoothness", 0.14f);
            var cartMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.85f, 0.86f, 0.83f) };

            MeshAssets.BeginBuild();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Shot[] shots;
            switch (what)
            {
                case "muscles":
                {
                    SkeletonBuilder.Build(root, boneMat, cartMat, layer);
                    var muscleMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = HumanBodyExplorerSetup.MuscleColor };
                    muscleMat.SetFloat("_Smoothness", 0.3f);
                    muscleMat.SetTexture("_BaseMap", HumanBodyExplorerSetup.CreateFibreTexture());
                    var tendonMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.90f, 0.86f, 0.76f) };
                    tendonMat.SetFloat("_Smoothness", 0.35f);
                    MuscleBuilder.Build(root, muscleMat, tendonMat, layer);
                    shots = MuscleShots;
                    break;
                }
                default:
                    SkeletonBuilder.Build(root, boneMat, cartMat, layer);
                    shots = SkeletonShots;
                    break;
            }
            Debug.Log($"[PreviewTool] built '{what}' in {watch.Elapsed.TotalSeconds:F1}s");
            MeshAssets.EndBuild();

            foreach (var shot in shots)
            {
                Quaternion rotation = Quaternion.Euler(shot.Pitch, shot.Yaw, 0f);
                camera.transform.rotation = rotation;
                camera.transform.position = shot.Target - rotation * Vector3.forward * shot.Distance;
                Render(camera, Path.Combine(outDir, $"{what}_{shot.Name}.png"));
            }

            EditorApplication.Exit(0);
        }

        private static Camera NewPreviewScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.24f, 0.28f, 0.35f);
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 50f;
            var data = camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;

            AddLight("Key", new Vector3(38f, -32f, 0f), 1.05f, new Color(1f, 0.99f, 0.97f));
            AddLight("Fill", new Vector3(28f, 148f, 0f), 0.55f, new Color(0.92f, 0.95f, 1f));
            AddLight("Rim", new Vector3(-32f, 110f, 0f), 0.5f, new Color(1f, 0.97f, 0.92f));

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.60f, 0.60f, 0.63f);
            return cam;
        }

        private static void AddLight(string name, Vector3 euler, float intensity, Color color)
        {
            var go = new GameObject(name);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.None;
            go.transform.rotation = Quaternion.Euler(euler);
        }

        private static void Render(Camera cam, string path)
        {
            const int w = 1200, h = 1000;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());

            cam.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log($"[PreviewTool] wrote {path}");
        }

        private static string ArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
