using System.Collections.Generic;
using HumanBodyExplorer.Dicom;
using HumanBodyExplorer.Multiplayer;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    public class Module67Tests
    {
        [Test]
        public void DicomVolumeLoader_CreatesTextureOfCorrectDimensions()
        {
            float[] data = new float[2 * 2 * 2];
            for (int i = 0; i < data.Length; i++) data[i] = i / 8f;

            var texture = DicomVolumeLoader.CreateVolumeTexture(data, 2, 2, 2);

            Assert.AreEqual(2, texture.width);
            Assert.AreEqual(2, texture.height);
            Assert.AreEqual(2, texture.depth);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void DicomVolumeLoader_MismatchedLength_Throws()
        {
            float[] data = new float[4];
            Assert.Throws<System.ArgumentException>(() => DicomVolumeLoader.CreateVolumeTexture(data, 2, 2, 2));
        }

        [Test]
        public void DicomAnonymizer_ScrubsPhiTags_KeepsOthers()
        {
            var tags = new Dictionary<string, string>
            {
                { DicomAnonymizer.PatientNameTag, "Jane Doe" },
                { DicomAnonymizer.PatientIdTag, "12345" },
                { "0008,0060", "CT" } // modality, not PHI
            };

            var result = DicomAnonymizer.Anonymize(tags);

            Assert.AreEqual(string.Empty, result[DicomAnonymizer.PatientNameTag]);
            Assert.AreEqual(string.Empty, result[DicomAnonymizer.PatientIdTag]);
            Assert.AreEqual("CT", result["0008,0060"]);
        }

        [Test]
        public void SurgicalTrajectoryPlanner_ComputesDistanceAndAngle()
        {
            var go = new GameObject("Planner");
            var planner = go.AddComponent<SurgicalTrajectoryPlanner>();

            planner.AddPoint(Vector3.zero);
            planner.AddPoint(new Vector3(0, 1, 0));

            var segments = planner.ComputeSegments();

            Assert.AreEqual(1, segments.Count);
            Assert.AreEqual(1f, segments[0].Distance, 0.001f);
            Assert.AreEqual(0f, segments[0].AngleFromWorldUp, 0.001f);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void FourDVolumeController_ComputeFrameIndex_WrapsAcrossFrames()
        {
            int index0 = FourDVolumeController.ComputeFrameIndex(0f, 10f, 5);
            int index5 = FourDVolumeController.ComputeFrameIndex(0.5f, 10f, 5);
            int indexWrap = FourDVolumeController.ComputeFrameIndex(1.0f, 10f, 5);

            Assert.AreEqual(0, index0);
            Assert.AreEqual(5 % 5, index5); // t*fps = 5 -> wraps to 0
            Assert.AreEqual(0, indexWrap);
        }

        [Test]
        public void NetworkTransformInterpolator_CatmullRom_MatchesMidpointAtControlPoints()
        {
            Vector3 result = NetworkTransformInterpolator.CatmullRom(
                Vector3.zero, Vector3.one, Vector3.one * 2f, Vector3.one * 3f, t: 0f);

            Assert.AreEqual(Vector3.one, result);
        }
    }
}
