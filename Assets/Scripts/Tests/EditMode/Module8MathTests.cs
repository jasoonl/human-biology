using HumanBodyExplorer.Core;
using NUnit.Framework;

namespace HumanBodyExplorer.Tests
{
    public class Module8MathTests
    {
        [Test]
        public void DynamicResolution_ComputeAverageFPS_ZeroCount_ReturnsZero()
        {
            float result = DynamicResolutionManager.ComputeAverageFPS(new float[10], 0);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void DynamicResolution_ComputeAverageFPS_ConstantDeltaTime_MatchesExpectedFPS()
        {
            float[] frameTimes = { 1f / 60f, 1f / 60f, 1f / 60f };
            float result = DynamicResolutionManager.ComputeAverageFPS(frameTimes, 3);

            Assert.AreEqual(60f, result, 0.1f);
        }

        [Test]
        public void BatteryOptimizer_ShouldGoIdle_TrueAfterTimeout()
        {
            Assert.IsTrue(BatteryOptimizer.ShouldGoIdle(5f, 5f));
            Assert.IsTrue(BatteryOptimizer.ShouldGoIdle(6f, 5f));
            Assert.IsFalse(BatteryOptimizer.ShouldGoIdle(4f, 5f));
        }

        [Test]
        public void SSAO_ComputeCloseness_AtCloseDistance_ReturnsOne()
        {
            float result = SSAOController.ComputeCloseness(0.1f, 0.1f, 5f);
            Assert.AreEqual(1f, result, 0.01f);
        }

        [Test]
        public void SSAO_ComputeCloseness_AtFarDistance_ReturnsZero()
        {
            float result = SSAOController.ComputeCloseness(5f, 0.1f, 5f);
            Assert.AreEqual(0f, result, 0.01f);
        }

        [Test]
        public void SSAO_ComputeCloseness_ClampsBeyondRange()
        {
            float resultBeyondFar = SSAOController.ComputeCloseness(100f, 0.1f, 5f);
            float resultBeyondClose = SSAOController.ComputeCloseness(0f, 0.1f, 5f);

            Assert.AreEqual(0f, resultBeyondFar, 0.01f);
            Assert.AreEqual(1f, resultBeyondClose, 0.01f);
        }
    }
}
