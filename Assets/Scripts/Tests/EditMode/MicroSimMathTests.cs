using HumanBodyExplorer.MicroSim;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    public class MicroSimMathTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void SarcomereAnimator_ComputePhase_WrapsAtCycleDuration()
        {
            _go = new GameObject("Sarcomere");
            var animator = _go.AddComponent<SarcomereAnimator>();

            float phase = animator.ComputePhase(0.5f);
            Assert.GreaterOrEqual(phase, 0f);
            Assert.Less(phase, 1f);
        }

        [Test]
        public void SynapticCleftManager_GetColorFor_ReturnsDistinctColorsPerTransmitter()
        {
            Color dopamine = SynapticCleftManager.GetColorFor(Neurotransmitter.Dopamine);
            Color serotonin = SynapticCleftManager.GetColorFor(Neurotransmitter.Serotonin);

            Assert.AreNotEqual(dopamine, serotonin);
        }
    }
}
