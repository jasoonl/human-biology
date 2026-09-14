using HumanBodyExplorer.Core;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    public class MuscleBulgeControllerTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void ComputeBulgeFromAngle_StraightAngle_ReturnsZero()
        {
            _go = new GameObject("MuscleBulge");
            var controller = _go.AddComponent<MuscleBulgeController>();

            float bulge = controller.ComputeBulgeFromAngle(180f);

            Assert.AreEqual(0f, bulge, 0.001f);
        }

        [Test]
        public void ComputeBulgeFromAngle_FullyFlexedAngle_ReturnsOne()
        {
            _go = new GameObject("MuscleBulge");
            var controller = _go.AddComponent<MuscleBulgeController>();

            float bulge = controller.ComputeBulgeFromAngle(30f);

            Assert.AreEqual(1f, bulge, 0.001f);
        }

        [Test]
        public void ComputeBulgeFromAngle_BeyondRange_Clamps()
        {
            _go = new GameObject("MuscleBulge");
            var controller = _go.AddComponent<MuscleBulgeController>();

            Assert.AreEqual(0f, controller.ComputeBulgeFromAngle(200f), 0.001f);
            Assert.AreEqual(1f, controller.ComputeBulgeFromAngle(0f), 0.001f);
        }
    }
}
