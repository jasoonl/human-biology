using HumanBodyExplorer.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// One-off scene scaffolding, invoked via -executeMethod from the command line
    /// so the bootstrap scene can be (re)generated headlessly.
    /// </summary>
    public static class SceneSetup
    {
        [MenuItem("Human Body Explorer/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrapGO = new GameObject("Bootstrapper");
            bootstrapGO.AddComponent<GameBootstrapper>();

            var cameraGO = new GameObject("Main Camera");
            var cam = cameraGO.AddComponent<Camera>();
            cameraGO.tag = "MainCamera";
            cam.transform.position = new Vector3(0, 1, -3);

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Bootstrap.unity");

            var scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true)
            };
            EditorBuildSettings.scenes = scenes;

            Debug.Log("[SceneSetup] Bootstrap.unity created and added to Build Settings.");
        }
    }
}
