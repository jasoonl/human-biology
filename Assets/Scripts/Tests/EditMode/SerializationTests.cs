using HumanBodyExplorer.Core;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    /// <summary>Phase 90, following the spec's literal scenario for SessionSaveManager.</summary>
    public class SerializationTests
    {
        [Test]
        public void Serialize_ThenDeserialize_RoundTripsExactly()
        {
            var original = new SessionData
            {
                CameraPosition = new Vector3(12, 3, 5),
                CameraRotation = Quaternion.Euler(10, 20, 30),
                SliceDepth = 0.42f,
                HiddenSystemIds = new[] { "SYS_SK_FEMUR", "SYS_CV_HEART_LV" }
            };

            string serialized = SessionSaveManager.Serialize(original);
            var restored = SessionSaveManager.Deserialize(serialized);

            Assert.AreEqual(original.CameraPosition, restored.CameraPosition);
            Assert.AreEqual(original.CameraRotation, restored.CameraRotation);
            Assert.AreEqual(original.SliceDepth, restored.SliceDepth, 0.0001f);
            CollectionAssert.AreEqual(original.HiddenSystemIds, restored.HiddenSystemIds);
        }

        [Test]
        public void Serialize_EmptyHiddenSystems_RoundTrips()
        {
            var original = new SessionData
            {
                CameraPosition = Vector3.zero,
                CameraRotation = Quaternion.identity,
                SliceDepth = 0f,
                HiddenSystemIds = null
            };

            string serialized = SessionSaveManager.Serialize(original);
            var restored = SessionSaveManager.Deserialize(serialized);

            Assert.AreEqual(0, restored.HiddenSystemIds.Length);
        }
    }
}
