using HumanBodyExplorer.CameraSystem;
using HumanBodyExplorer.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// Populates the currently-open scene with visible placeholder content and
    /// wires up orbit/zoom/click so pressing Play actually demonstrates the
    /// Module I/II systems end-to-end, instead of an empty scene with only the
    /// bootstrap logic running invisibly.
    /// </summary>
    public static class DemoSceneSetup
    {
        [MenuItem("Human Body Explorer/Add Demo Content To Current Scene")]
        public static void AddDemoContent()
        {
            var mainCameraGO = GameObject.FindWithTag("MainCamera");
            if (mainCameraGO == null)
            {
                Debug.LogError("[DemoSceneSetup] No Main Camera found in the current scene. Open Bootstrap.unity first.");
                return;
            }

            CreateGround();
            CreateDirectionalLight();
            GameObject heartCube = CreateHeartPlaceholder();
            WireUpCamera(mainCameraGO, heartCube);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[DemoSceneSetup] Demo content added. Press Play, then: " +
                      "left-click-drag to orbit, scroll to zoom, click the red cube to select it " +
                      "(watch the Console for the OnNodeSelected log).");
        }

        private static void CreateGround()
        {
            if (GameObject.Find("DemoGround") != null) return;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "DemoGround";
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = Vector3.one * 0.5f;
        }

        private static void CreateDirectionalLight()
        {
            if (Object.FindFirstObjectByType<Light>() != null) return;

            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        private static GameObject CreateHeartPlaceholder()
        {
            var existing = GameObject.Find("DemoHeart_LeftVentricle");
            if (existing != null) return existing;

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "DemoHeart_LeftVentricle";
            cube.transform.position = Vector3.zero;

            var nodeRef = cube.AddComponent<AnatomyNodeReference>();
            nodeRef.SetEntityId("SYS_CV_HEART_LV");

            var renderer = cube.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.7f, 0.1f, 0.1f);
            renderer.sharedMaterial = material;

            return cube;
        }

        private static void WireUpCamera(GameObject mainCameraGO, GameObject target)
        {
            var orbitalCamera = mainCameraGO.GetComponent<AdvancedOrbitalCamera>();
            if (orbitalCamera == null) orbitalCamera = mainCameraGO.AddComponent<AdvancedOrbitalCamera>();
            orbitalCamera.Target = target.transform;

            var raycaster = mainCameraGO.GetComponent<AnatomyRaycaster>();
            if (raycaster == null) raycaster = mainCameraGO.AddComponent<AnatomyRaycaster>();

            var bridgeGO = GameObject.Find("DemoInputBridge") ?? new GameObject("DemoInputBridge");
            var bridge = bridgeGO.GetComponent<DemoInputBridge>() ?? bridgeGO.AddComponent<DemoInputBridge>();

            var serializedBridge = new SerializedObject(bridge);
            serializedBridge.FindProperty("orbitalCamera").objectReferenceValue = orbitalCamera;
            serializedBridge.FindProperty("raycaster").objectReferenceValue = raycaster;
            serializedBridge.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
