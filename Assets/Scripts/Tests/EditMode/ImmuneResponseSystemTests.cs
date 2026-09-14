using System.Collections.Generic;
using HumanBodyExplorer.MicroSim;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    public class ImmuneResponseSystemTests
    {
        [Test]
        public void ComputeBoidForce_NoNeighbors_PointsTowardPathogen()
        {
            Vector3 force = ImmuneResponseSystem.ComputeBoidForce(
                Vector3.zero, new List<Vector3>(), new Vector3(10, 0, 0),
                neighborRadius: 1f, separationRadius: 0.3f, pathogenAttractionWeight: 2f);

            Assert.Greater(Vector3.Dot(force, Vector3.right), 0.9f);
        }

        [Test]
        public void ComputeBoidForce_ReturnsNormalizedVector()
        {
            var neighbors = new List<Vector3> { new Vector3(0.5f, 0, 0), new Vector3(-0.5f, 0, 0) };
            Vector3 force = ImmuneResponseSystem.ComputeBoidForce(
                Vector3.zero, neighbors, new Vector3(0, 0, 5),
                neighborRadius: 2f, separationRadius: 0.3f, pathogenAttractionWeight: 1f);

            Assert.AreEqual(1f, force.magnitude, 0.01f);
        }

        [Test]
        public void ComputeBoidForce_CloseNeighbor_AddsSeparation()
        {
            var closeNeighbor = new List<Vector3> { new Vector3(0.1f, 0, 0) };
            Vector3 forceWithClose = ImmuneResponseSystem.ComputeBoidForce(
                Vector3.zero, closeNeighbor, new Vector3(0, 0, 5),
                neighborRadius: 2f, separationRadius: 0.5f, pathogenAttractionWeight: 0.01f);

            // With a very close neighbor and negligible pathogen pull, force should
            // point away from the neighbor (negative X), not toward it.
            Assert.Less(forceWithClose.x, 0f);
        }
    }
}
