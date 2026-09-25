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
    /// Builds the classroom-facing explorer: a full human figure - skeleton, muscles, organs,
    /// vessels, nerves and a translucent skin - each structure tagged with its dictionary id,
    /// plus an info panel and quiz HUD with live timer and on-figure flash feedback. The
    /// geometry is generated procedurally (see Geometry/) because no licensed anatomical
    /// meshes exist in this project; every structure is placed against the same landmarks
    /// so the layers line up with each other.
    /// </summary>
    public static partial class HumanBodyExplorerSetup
    {
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
        private static readonly Color NerveColor = new Color(0.96f, 0.82f, 0.30f);
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

            Debug.Log("[HumanBodyExplorerSetup] Built the full explorer: " + _partCount +
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
        private static int _partCount;
        private const string AnatomyLayerName = "Anatomy";

        private static GameObject BuildHumanFigure()
        {
            var root = new GameObject("HumanBodyRoot");
            int anatomyLayer = EnsureLayer(AnatomyLayerName);

            var shaderLit = Shader.Find("Universal Render Pipeline/Lit");
            var shaderVessel = Shader.Find("HumanBodyExplorer/BloodFlow");

            // Shared, generated (not imported) noise texture modulates base color
            // as a brightness multiplier so flat skin/bone color reads as mottled
            // tissue instead of flat plastic.
            var skinNoiseTex = CreateNoiseTexture(64, 0.94f, 1f, 6f, 11);

            // Every structure is a sculpted or lofted mesh built by the generators in Geometry/.
            var boneMat = CreateSimpleMaterial(shaderLit, BoneColor, BoneGloss, skinNoiseTex);
            var cartilageMat = CreateSimpleMaterial(shaderLit, CartilageColor, OrganGloss, skinNoiseTex);

            // Muscle is striped along its fibres, and its tendons are pale, glossy connective tissue.
            var muscleMat = CreateSimpleMaterial(shaderLit, MuscleColor, MuscleGloss, CreateFibreTexture());
            var tendonMat = CreateSimpleMaterial(shaderLit, new Color(0.90f, 0.86f, 0.76f), 0.35f, skinNoiseTex);
            var nerveMat = CreateSimpleMaterial(shaderLit, NerveColor, 0.35f, skinNoiseTex);
            var lymphMat = CreateSimpleMaterial(shaderLit, new Color(0.50f, 0.78f, 0.58f), 0.45f, skinNoiseTex);

            // The animated flowing-blood shader, built once and shared by every vessel so the
            // whole vascular tree pulses along its length.
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
            OrganBuilder.Build(root.transform, anatomyLayer);
            int vessels = VesselBuilder.Build(root.transform, arteryFlowMat, veinFlowMat, lymphMat, anatomyLayer);
            int nerves = NerveBuilder.Build(root.transform, nerveMat, anatomyLayer);
            Debug.Log($"[HumanBodyExplorerSetup] {vessels} vessel and {nerves} nerve meshes.");
            Debug.Log($"[HumanBodyExplorerSetup] {ZAnatomy.Apply(root.transform)} structures replaced with Z-Anatomy meshes.");
            MeshAssets.KeepOnlyUsedBy(root.transform);
            MeshAssets.EndBuild();
            _partCount = root.GetComponentsInChildren<AnatomyNodeReference>().Length;

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
            serializedOrbit.FindProperty("minDistance").floatValue = 0.08f;
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
            RemoveIfExists("AnatomyHighlighter");

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
            var infoText = CreateText(infoPanel.transform, "InfoText", "Point at a part to see its name; click to isolate it and read about it. Esc or empty space clears; H toggles ghosting.\n\nView: drag to orbit, right-drag or Shift+drag (or WASD/arrows, Q/E) to move, scroll to zoom, double-click to centre on a spot, R to reset.\n\nAnatomy models: Z-Anatomy and BodyParts3D (CC BY-SA).", 20);
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

            // Which reproductive anatomy the Reproductive layer shows.
            var sexButtonGO = new GameObject("ReproductiveSexButton", typeof(Image), typeof(Button));
            sexButtonGO.transform.SetParent(canvasGO.transform, false);
            var sexRect = sexButtonGO.GetComponent<RectTransform>();
            sexRect.anchorMin = new Vector2(0, 1);
            sexRect.anchorMax = new Vector2(0, 1);
            sexRect.pivot = new Vector2(0, 1);
            sexRect.anchoredPosition = new Vector2(30, -276 - layerGroups.Length * 46);
            sexRect.sizeDelta = new Vector2(210, 40);
            sexButtonGO.GetComponent<Image>().color = new Color(0.30f, 0.22f, 0.36f, 0.92f);
            var sexText = CreateText(sexButtonGO.transform, "Text", "Reproductive: Male", 17);
            StretchToParent(sexText.rectTransform, padding: 0);
            sexText.alignment = TextAlignmentOptions.Center;

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
            serializedController.FindProperty("reproductiveSexButton").objectReferenceValue = sexButtonGO.GetComponent<Button>();
            serializedController.FindProperty("reproductiveSexText").objectReferenceValue = sexText;

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

            // Hover tint, name tooltip and select-to-isolate highlighting.
            var highlighterGO = new GameObject("AnatomyHighlighter", typeof(AnatomyHighlighter));
            var serializedHighlighter = new SerializedObject(highlighterGO.GetComponent<AnatomyHighlighter>());
            serializedHighlighter.FindProperty("explorerUI").objectReferenceValue = controller;
            serializedHighlighter.FindProperty("sourceCamera").objectReferenceValue = mainCameraGO.GetComponent<Camera>();
            serializedHighlighter.FindProperty("layerVisibility").objectReferenceValue = layerVisibility;
            serializedHighlighter.ApplyModifiedPropertiesWithoutUndo();
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
