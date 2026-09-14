using System.Collections;
using HumanBodyExplorer.CameraSystem;
using HumanBodyExplorer.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HumanBodyExplorer.Tests
{
    /// <summary>
    /// Phase 89, following the spec's literal scenario: a primitive cube tagged
    /// with a known EntityID, camera at (0,0,-5), ExecuteClick fired programmatically.
    /// </summary>
    public class RaycastIntegrationTests
    {
        private GameObject _cameraGO;
        private GameObject _cubeGO;
        private string _selectedId;
        private System.Action<string> _handler;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _cameraGO = new GameObject("TestCamera");
            var cam = _cameraGO.AddComponent<Camera>();
            _cameraGO.tag = "MainCamera"; // AnatomyRaycaster falls back to Camera.main when unset
            _cameraGO.transform.position = new Vector3(0, 0, -5);
            _cameraGO.transform.LookAt(Vector3.zero);

            _cubeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cubeGO.transform.position = Vector3.zero;
            _cubeGO.AddComponent<AnatomyNodeReference>().SetEntityId("TEST_01");

            _selectedId = null;
            _handler = id => _selectedId = id;
            AnatomyRaycaster.OnNodeSelected += _handler;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AnatomyRaycaster.OnNodeSelected -= _handler;
            Object.Destroy(_cameraGO);
            Object.Destroy(_cubeGO);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExecuteClick_OnCenteredCube_ResolvesToTestId()
        {
            var raycasterGO = new GameObject("Raycaster");
            var raycaster = raycasterGO.AddComponent<AnatomyRaycaster>();

            // Explicit assignment instead of relying on the Camera.main tag lookup,
            // which is unreliable across test fixtures sharing one Play session
            // (another fixture's MainCamera-tagged object can still be alive).
            var testCam = _cameraGO.GetComponent<Camera>();
            raycaster.SourceCamera = testCam;
            yield return null;

            raycaster.ExecuteClick(new Vector2(testCam.pixelWidth / 2f, testCam.pixelHeight / 2f));

            Assert.AreEqual("TEST_01", _selectedId);

            Object.Destroy(raycasterGO);
        }
    }
}
