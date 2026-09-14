using HumanBodyExplorer.Core;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    public class CapillaryGeneratorTests
    {
        [Test]
        public void Generate_ProducesSegmentsAcrossGenerations()
        {
            var generator = new CapillaryGenerator(seed: 42);
            var segments = generator.Generate(Vector3.zero, Vector3.up, maxGenerations: 3,
                initialLength: 1f, lengthDecay: 0.7f, branchAngleDegrees: 30f);

            Assert.Greater(segments.Count, 1);
            Assert.IsTrue(segments.Exists(s => s.Generation == 0));
            Assert.IsTrue(segments.Exists(s => s.Generation == 2));
        }

        [Test]
        public void Generate_SameSeed_IsDeterministic()
        {
            var genA = new CapillaryGenerator(seed: 7);
            var genB = new CapillaryGenerator(seed: 7);

            var segmentsA = genA.Generate(Vector3.zero, Vector3.forward, 3, 1f, 0.7f, 25f);
            var segmentsB = genB.Generate(Vector3.zero, Vector3.forward, 3, 1f, 0.7f, 25f);

            Assert.AreEqual(segmentsA.Count, segmentsB.Count);
            for (int i = 0; i < segmentsA.Count; i++)
            {
                Assert.AreEqual(segmentsA[i].Start, segmentsB[i].Start);
                Assert.AreEqual(segmentsA[i].End, segmentsB[i].End);
            }
        }

        [Test]
        public void Generate_ZeroGenerations_ProducesNoSegments()
        {
            var generator = new CapillaryGenerator(seed: 1);
            var segments = generator.Generate(Vector3.zero, Vector3.up, 0, 1f, 0.7f, 30f);

            Assert.AreEqual(0, segments.Count);
        }
    }
}
