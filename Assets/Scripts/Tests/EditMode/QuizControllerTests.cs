using System.Collections.Generic;
using HumanBodyExplorer.CameraSystem;
using HumanBodyExplorer.Data;
using HumanBodyExplorer.UI;
using NUnit.Framework;
using UnityEngine;

namespace HumanBodyExplorer.Tests
{
    /// <summary>Minimal in-memory IDataController for tests that don't need real JSON I/O.</summary>
    public class FakeDataController : IDataController
    {
        private readonly Dictionary<string, AnatomyNode> _nodes;

        public FakeDataController(Dictionary<string, AnatomyNode> nodes)
        {
            _nodes = nodes;
        }

        public IReadOnlyDictionary<string, AnatomyNode> AllNodes => _nodes;

        public System.Threading.Tasks.Task<Dictionary<string, AnatomyNode>> LoadDataAsync(string path)
            => System.Threading.Tasks.Task.FromResult(_nodes);

        public AnatomyNode GetNode(string entityId)
        {
            if (_nodes.TryGetValue(entityId, out var node)) return node;
            throw new AnatomyNotFoundException(entityId);
        }
    }

    public class QuizControllerTests
    {
        private GameObject _controllerGO;
        private QuizController _quizController;
        private FakeDatabaseManager _db;

        private static AnatomyNode MakeNode(string id, string commonName, params string[] systems) => new AnatomyNode
        {
            EntityID = id,
            CommonName = commonName,
            LatinName = commonName,
            SystemCategory = new List<string>(systems),
        };

        [SetUp]
        public void SetUp()
        {
            _controllerGO = new GameObject("QuizController");
            _quizController = _controllerGO.AddComponent<QuizController>();
            _db = new FakeDatabaseManager(); // empty: nothing is "due" yet
        }

        [TearDown]
        public void TearDown()
        {
            _quizController.StopQuiz();
            Object.DestroyImmediate(_controllerGO);
        }

        [Test]
        public void StartQuiz_WithSystemFilter_OnlyPromptsNodesFromThatSystem()
        {
            var nodes = new Dictionary<string, AnatomyNode>
            {
                ["SYS_SK_SKULL"] = MakeNode("SYS_SK_SKULL", "Skull", "Skeletal"),
                ["SYS_MUSC_BICEPS"] = MakeNode("SYS_MUSC_BICEPS", "Biceps Brachii", "Muscular"),
                ["SYS_CV_HEART"] = MakeNode("SYS_CV_HEART", "Heart", "Cardiovascular"),
            };
            _quizController.Initialize(new StudyTracker(_db), new FakeDataController(nodes));

            string prompted = null;
            _quizController.OnQuestionPrompted += p => prompted = p;

            _quizController.StartQuiz("Cardiovascular");

            Assert.AreEqual("Locate: Heart", prompted);
        }

        [Test]
        public void StartQuiz_NoFilter_CanPromptAnyKnownNode()
        {
            var nodes = new Dictionary<string, AnatomyNode>
            {
                ["SYS_SK_SKULL"] = MakeNode("SYS_SK_SKULL", "Skull", "Skeletal"),
            };
            _quizController.Initialize(new StudyTracker(_db), new FakeDataController(nodes));

            string prompted = null;
            _quizController.OnQuestionPrompted += p => prompted = p;

            _quizController.StartQuiz();

            Assert.AreEqual("Locate: Skull", prompted);
        }

        [Test]
        public void StartQuiz_FilterMatchesNothingDue_FallsBackToAllNodesInThatSystem()
        {
            var nodes = new Dictionary<string, AnatomyNode>
            {
                ["SYS_MUSC_BICEPS"] = MakeNode("SYS_MUSC_BICEPS", "Biceps Brachii", "Muscular"),
                ["SYS_MUSC_QUADS"] = MakeNode("SYS_MUSC_QUADS", "Quadriceps", "Muscular"),
            };
            // Mark one node as due under a system that isn't requested, proving the
            // filter still finds the Muscular nodes via the "nothing due in that
            // system" fallback rather than starting an empty quiz.
            _db.UpsertProgress(new UserProgressRow
            {
                NodeID = "SYS_SK_SKULL",
                CorrectStrikes = 1,
                ReviewInterval = 1f,
                EaseFactor = 2.5f,
                LastReviewed = System.DateTime.UtcNow.AddDays(-5).ToString("O"),
            });
            _quizController.Initialize(new StudyTracker(_db), new FakeDataController(nodes));

            string prompted = null;
            _quizController.OnQuestionPrompted += p => prompted = p;

            _quizController.StartQuiz("Muscular");

            Assert.IsNotNull(prompted);
            StringAssert.StartsWith("Locate:", prompted);
        }
    }
}
