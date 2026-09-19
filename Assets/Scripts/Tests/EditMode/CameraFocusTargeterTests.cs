using HumanBodyExplorer.CameraSystem;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    /// <summary>
    /// Regression coverage for a real bug: FocusOn used to only move the orbit
    /// pivot to the figure's bounds center when the pivot happened to already be
    /// at exactly (0,0,0), otherwise leaving a pre-existing Target untouched. That
    /// made the zoom distance correct but the camera orbit around the wrong point
    /// (or, worse, a point buried inside the figure's own colliders).
    /// </summary>
    public class CameraFocusTargeterTests
    {
        private GameObject _cameraGO;
        private GameObject _targetGO;
        private GameObject _figureGO;
        private AdvancedOrbitalCamera _orbitalCamera;
        private CameraFocusTargeter _targeter;

        [SetUp]
        public void SetUp()
        {
            _cameraGO = new GameObject("Camera");
            _orbitalCamera = _cameraGO.AddComponent<AdvancedOrbitalCamera>();
            _targeter = _cameraGO.AddComponent<CameraFocusTargeter>();

            var so = new SerializedObject(_targeter);
            so.FindProperty("orbitalCamera").objectReferenceValue = _orbitalCamera;
            so.FindProperty("targetCamera").objectReferenceValue = _cameraGO.GetComponent<Camera>();
            so.ApplyModifiedPropertiesWithoutUndo();

            _targetGO = new GameObject("OrbitTarget");
            _orbitalCamera.Target = _targetGO.transform;

            _figureGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cameraGO);
            Object.DestroyImmediate(_targetGO);
            Object.DestroyImmediate(_figureGO);
        }

        [Test]
        public void FocusOn_PreExistingTarget_MovesItToFigureBoundsCenter()
        {
            _figureGO.transform.position = new Vector3(3f, 2f, -1f);
            var expectedCenter = _figureGO.GetComponent<Renderer>().bounds.center;

            _targeter.FocusOn(_figureGO);

            Assert.AreEqual(expectedCenter, _orbitalCamera.Target.position);
        }

        [Test]
        public void FocusOn_NoExistingTarget_AssignsFigureTransformAsTarget()
        {
            _orbitalCamera.Target = null;

            _targeter.FocusOn(_figureGO);

            Assert.AreEqual(_figureGO.transform, _orbitalCamera.Target);
        }
    }
}
