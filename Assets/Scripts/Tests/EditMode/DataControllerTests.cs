using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using HumanBodyExplorer.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HumanBodyExplorer.Tests
{
    /// <summary>
    /// Phase 8 coverage. The spec calls for Moq; no NuGet-for-Unity/Moq package is
    /// available offline in this environment, so DataController is exercised
    /// directly against a temp JSON fixture instead of a mocked dependency.
    /// </summary>
    public class DataControllerTests
    {
        private string _tempJsonPath;

        [SetUp]
        public void SetUp()
        {
            _tempJsonPath = Path.Combine(Application.temporaryCachePath, "dummy_anatomy.json");
            File.WriteAllText(_tempJsonPath, @"
            [
                {
                    ""entityID"": ""TEST_NODE_1"",
                    ""commonName"": ""Test Node"",
                    ""latinName"": ""Testus Nodus"",
                    ""boundsCenterOffset"": { ""x"": 0, ""y"": 0, ""z"": 0 }
                }
            ]");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempJsonPath)) File.Delete(_tempJsonPath);
        }

        [UnityTest]
        public IEnumerator LoadDataAsync_ValidJson_PopulatesDictionary()
        {
            var controller = new DataController();
            var task = controller.LoadDataAsync(_tempJsonPath);

            while (!task.IsCompleted) yield return null;

            Assert.IsNotNull(task.Result);
            Assert.Greater(task.Result.Count, 0);
            Assert.IsTrue(task.Result.ContainsKey("TEST_NODE_1"));
        }

        [UnityTest]
        public IEnumerator LoadDataAsync_MissingFile_ReturnsEmptyDictionary()
        {
            LogAssert.Expect(LogType.Error, new Regex("Anatomy JSON not found.*"));

            var controller = new DataController();
            var task = controller.LoadDataAsync(Path.Combine(Application.temporaryCachePath, "does_not_exist.json"));

            while (!task.IsCompleted) yield return null;

            Assert.IsNotNull(task.Result);
            Assert.AreEqual(0, task.Result.Count);
        }

        [UnityTest]
        public IEnumerator GetNode_InvalidId_ThrowsAnatomyNotFoundException()
        {
            var controller = new DataController();
            var task = controller.LoadDataAsync(_tempJsonPath);
            while (!task.IsCompleted) yield return null;

            Assert.Throws<AnatomyNotFoundException>(() => controller.GetNode("DOES_NOT_EXIST"));
        }
    }
}
