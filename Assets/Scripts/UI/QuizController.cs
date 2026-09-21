using System;
using System.Collections;
using System.Collections.Generic;
using HumanBodyExplorer.CameraSystem;
using HumanBodyExplorer.Core;
using HumanBodyExplorer.Data;
using UnityEngine;

namespace HumanBodyExplorer.UI
{
    /// <summary>
    /// Phase 52: pulls due nodes from StudyTracker, prompts "Locate [Anatomy]",
    /// runs a countdown timer, and scores the player's raycast selection against
    /// the expected node.
    /// </summary>
    public class QuizController : MonoBehaviour
    {
        [SerializeField] private float questionTimeSeconds = 15f;

        private IStudyTracker _studyTracker;
        private IDataController _dataController;
        private readonly ScoreManager _scoreManager = new ScoreManager();
        private Queue<string> _dueNodeQueue;
        private string _currentExpectedNodeId;
        private Coroutine _timerCoroutine;
        private float _timeRemaining;

        public event Action<string> OnQuestionPrompted;
        public event Action<bool, int> OnAnswerResolved; // (wasCorrect, scoreDelta)
        public event Action OnQuizComplete;

        /// <summary>(entityId, isCorrectAnswer) - fired once per resolved question for
        /// the correct target (always) and, on a wrong click, once more for the
        /// clicked part - lets the scene flash green/red feedback on the 3D figure.</summary>
        public event Action<string, bool> OnPartFeedback;

        public ScoreManager Score => _scoreManager;
        public float TimeRemaining => _timeRemaining;
        public float QuestionTimeSeconds => questionTimeSeconds;

        public void Initialize(IStudyTracker studyTracker, IDataController dataController)
        {
            _studyTracker = studyTracker;
            _dataController = dataController;
        }

        /// <summary>Null/empty quizzes every due (or, if none are due, every known) node.
        /// A non-null systemCategory (e.g. "Cardiovascular", matching AnatomyNode.SystemCategory)
        /// restricts the quiz to that body system, for a teacher covering one unit at a time.</summary>
        public void StartQuiz(string systemCategory = null)
        {
            var dueIds = _studyTracker.GetDueNodes(DateTime.UtcNow);

            // A brand-new database has no progress rows at all, so nothing is
            // technically "due" yet under SM-2 even though the user has never
            // studied anything. Fall back to quizzing every known node instead
            // of silently completing a zero-question quiz.
            if (dueIds.Count == 0)
            {
                dueIds = new List<string>(_dataController.AllNodes.Keys);
            }

            if (!string.IsNullOrEmpty(systemCategory))
            {
                dueIds = dueIds.FindAll(id => NodeIsInSystem(id, systemCategory));

                // The due set can legitimately miss a whole system (e.g. everything
                // due right now happens to be Skeletal); fall back to every node in
                // the requested system rather than starting an empty quiz.
                if (dueIds.Count == 0)
                {
                    dueIds = new List<string>();
                    foreach (var id in _dataController.AllNodes.Keys)
                    {
                        if (NodeIsInSystem(id, systemCategory)) dueIds.Add(id);
                    }
                }
            }

            // Never ask the player to locate something that has no geometry in the
            // scene. The dictionary describes structures (e.g. individual heart
            // chambers) that are not modelled as separately clickable parts, and
            // asking for one is an unanswerable question that can only time out.
            var locatable = dueIds.FindAll(id => AnatomyNodeReference.GetByEntityId(id).Count > 0);
            if (locatable.Count > 0) dueIds = locatable;

            _dueNodeQueue = new Queue<string>(dueIds);
            AnatomyRaycaster.OnNodesUnderCursor += HandleNodesUnderCursor;
            NextQuestion();
        }

        private bool NodeIsInSystem(string nodeId, string systemCategory)
        {
            if (!_dataController.AllNodes.TryGetValue(nodeId, out var node)) return false;
            return node.SystemCategory != null && node.SystemCategory.Contains(systemCategory);
        }

        public void StopQuiz()
        {
            AnatomyRaycaster.OnNodesUnderCursor -= HandleNodesUnderCursor;
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        }

        private void NextQuestion()
        {
            if (_dueNodeQueue == null || _dueNodeQueue.Count == 0)
            {
                StopQuiz();
                OnQuizComplete?.Invoke();
                return;
            }

            _currentExpectedNodeId = _dueNodeQueue.Dequeue();
            var node = _dataController.GetNode(_currentExpectedNodeId);
            OnQuestionPrompted?.Invoke($"Locate: {node.CommonName}");

            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(QuestionTimer());
        }

        private IEnumerator QuestionTimer()
        {
            _timeRemaining = questionTimeSeconds;
            while (_timeRemaining > 0f)
            {
                _timeRemaining -= Time.deltaTime;
                yield return null;
            }

            _timeRemaining = 0f;
            ResolveAnswer(isCorrect: false, timeLeft: 0f, wrongClickId: null);
        }

        private void HandleNodesUnderCursor(IReadOnlyList<string> idsUnderCursor)
        {
            if (_currentExpectedNodeId == null || idsUnderCursor == null || idsUnderCursor.Count == 0) return;

            // Count it correct if the target is anywhere along the ray, not only the
            // frontmost surface. Clicking over the heart should score even though the
            // skin, a rib and a lung are technically in front of it.
            bool correct = false;
            for (int i = 0; i < idsUnderCursor.Count; i++)
            {
                if (idsUnderCursor[i] != _currentExpectedNodeId) continue;
                correct = true;
                break;
            }

            // Was hardcoded to 0 before, silently disabling the speed bonus this
            // was meant to reward - now uses the actual time left on the clock.
            ResolveAnswer(correct, Mathf.Max(0f, _timeRemaining), correct ? null : idsUnderCursor[0]);
        }

        private void ResolveAnswer(bool isCorrect, float timeLeft, string wrongClickId)
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);

            int scoreDelta;
            if (isCorrect)
            {
                int baseScore = Mathf.RoundToInt(timeLeft * 10f) + 10;
                scoreDelta = _scoreManager.RegisterCorrectAnswer(baseScore);
                _studyTracker.LogQuizResult(_currentExpectedNodeId, qualityScore0To5: 5);
            }
            else
            {
                scoreDelta = -10;
                _scoreManager.RegisterWrongAnswer(10);
                _studyTracker.LogQuizResult(_currentExpectedNodeId, qualityScore0To5: 1);
            }

            OnAnswerResolved?.Invoke(isCorrect, scoreDelta);
            OnPartFeedback?.Invoke(_currentExpectedNodeId, true);
            if (wrongClickId != null) OnPartFeedback?.Invoke(wrongClickId, false);

            _currentExpectedNodeId = null;

            NextQuestion();
        }
    }
}
