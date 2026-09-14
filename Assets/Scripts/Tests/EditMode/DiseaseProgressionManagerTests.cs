using HumanBodyExplorer.Core;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    public class DiseaseProgressionManagerTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void ComputeColorForSeverity_Zero_ReturnsHealthyColor()
        {
            _go = new GameObject("Disease");
            var manager = _go.AddComponent<DiseaseProgressionManager>();

            Color result = manager.ComputeColorForSeverity(0f);

            // Default healthy color set in the component.
            Assert.AreEqual(new Color(0.85f, 0.55f, 0.5f).r, result.r, 0.01f);
        }

        [Test]
        public void ComputeColorForSeverity_ClampsAboveHundred()
        {
            _go = new GameObject("Disease");
            var manager = _go.AddComponent<DiseaseProgressionManager>();

            Color at100 = manager.ComputeColorForSeverity(100f);
            Color at200 = manager.ComputeColorForSeverity(200f);

            Assert.AreEqual(at100, at200);
        }
    }
}
