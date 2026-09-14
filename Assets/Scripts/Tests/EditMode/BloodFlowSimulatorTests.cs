using HumanBodyExplorer.MicroSim;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    public class BloodFlowSimulatorTests
    {
        [Test]
        public void EvaluateBezier_AtZero_ReturnsStartPoint()
        {
            Vector3 result = BloodFlowSimulator.EvaluateBezier(Vector3.zero, Vector3.up, Vector3.right, 0f);
            Assert.AreEqual(Vector3.zero, result);
        }

        [Test]
        public void EvaluateBezier_AtOne_ReturnsEndPoint()
        {
            Vector3 result = BloodFlowSimulator.EvaluateBezier(Vector3.zero, Vector3.up, Vector3.right, 1f);
            Assert.AreEqual(Vector3.right, result);
        }

        [Test]
        public void EvaluateBezier_AtHalf_MatchesQuadraticBezierFormula()
        {
            // B(0.5) = 0.25*p0 + 0.5*p1 + 0.25*p2
            Vector3 result = BloodFlowSimulator.EvaluateBezier(Vector3.zero, new Vector3(0, 2, 0), new Vector3(2, 0, 0), 0.5f);
            Assert.AreEqual(new Vector3(0.5f, 1f, 0f), result);
        }
    }
}
