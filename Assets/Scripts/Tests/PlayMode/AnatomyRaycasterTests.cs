using System.Collections;
using HumanBodyExplorer.CameraSystem;
using HumanBodyExplorer.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HumanBodyExplorer.Tests
{
    public class AnatomyRaycasterTests
    {
        private GameObject _cameraGO;
        private GameObject _targetGO;
        private string _selectedId;
        private System.Action<string> _onNodeSelectedHandler;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _cameraGO = new GameObject("TestCamera");
            var cam = _cameraGO.AddComponent<Camera>();
            _cameraGO.transform.position = new Vector3(0, 0, -5);
            _cameraGO.transform.LookAt(Vector3.zero);

            _targetGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _targetGO.transform.position = Vector3.zero;
            _targetGO.AddComponent<AnatomyNodeReference>().SetEntityId("SYS_TEST_CUBE");

            _selectedId = null;
            _onNodeSelectedHandler = id => _selectedId = id;
            AnatomyRaycaster.OnNodeSelected += _onNodeSelectedHandler;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AnatomyRaycaster.OnNodeSelected -= _onNodeSelectedHandler;
            Object.Destroy(_cameraGO);
            Object.Destroy(_targetGO);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TryRaycast_HitsTaggedCube_FiresOnNodeSelectedWithCorrectId()
        {
            var raycasterGO = new GameObject("Raycaster");
            var raycaster = raycasterGO.AddComponent<AnatomyRaycaster>();

            yield return null;

            var ray = new Ray(_cameraGO.transform.position, _cameraGO.transform.forward);
            bool hit = raycaster.TryRaycast(ray);

            Assert.IsTrue(hit);
            Assert.AreEqual("SYS_TEST_CUBE", _selectedId);

            Object.Destroy(raycasterGO);
        }

        [UnityTest]
        public IEnumerator TryRaycast_MissesEverything_ReturnsFalse()
        {
            var raycasterGO = new GameObject("Raycaster");
            var raycaster = raycasterGO.AddComponent<AnatomyRaycaster>();

            yield return null;

            var ray = new Ray(new Vector3(100, 100, -5), Vector3.forward);
            bool hit = raycaster.TryRaycast(ray);

            Assert.IsFalse(hit);
            Assert.IsNull(_selectedId);

            Object.Destroy(raycasterGO);
        }
    }
}
