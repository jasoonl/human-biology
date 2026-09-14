using HumanBodyExplorer.Core;
using NUnit.Framework;

namespace HumanBodyExplorer.Tests
{
    public class PointCloudCsvTests
    {
        [Test]
        public void ParseCsv_ValidRows_ParsesAllPoints()
        {
            string csv = "1,2,3\n4.5,5.5,6.5\n-1,-2,-3";

            var points = PointCloudRenderer.ParseCsv(csv);

            Assert.AreEqual(3, points.Count);
            Assert.AreEqual(new UnityEngine.Vector3(1, 2, 3), points[0]);
            Assert.AreEqual(new UnityEngine.Vector3(4.5f, 5.5f, 6.5f), points[1]);
            Assert.AreEqual(new UnityEngine.Vector3(-1, -2, -3), points[2]);
        }

        [Test]
        public void ParseCsv_MalformedRows_AreSkipped()
        {
            string csv = "1,2,3\nnot,a,point\n\n4,5,6";

            var points = PointCloudRenderer.ParseCsv(csv);

            Assert.AreEqual(2, points.Count);
        }

        [Test]
        public void ParseCsv_EmptyInput_ReturnsEmptyList()
        {
            var points = PointCloudRenderer.ParseCsv(string.Empty);
            Assert.AreEqual(0, points.Count);
        }
    }
}
