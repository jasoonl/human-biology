using System;
using System.Collections;
using System.Collections.Generic;
using HumanBodyExplorer.CameraSystem;
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

        public event Action<string> OnQuestionPrompted;
        public event Action<bool, int> OnAnswerResolved; // (wasCorrect, scoreDelta)
        public event Action OnQuizComplete;

        public ScoreManager Score => _scoreManager;

        public void Initialize(IStudyTracker studyTracker, IDataController dataController)
        {
            _studyTracker = studyTracker;
            _dataController = dataController;
        }

        public void StartQuiz()
        {
            var dueIds = _studyTracker.GetDueNodes(DateTime.UtcNow);
            _dueNodeQueue = new Queue<string>(dueIds);
            AnatomyRaycaster.OnNodeSelected += HandleNodeSelected;
            NextQuestion();
        }

        public void StopQuiz()
        {
            AnatomyRaycaster.OnNodeSelected -= HandleNodeSelected;
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
            float timeLeft = questionTimeSeconds;
            while (timeLeft > 0f)
            {
                timeLeft -= Time.deltaTime;
                yield return null;
            }

            ResolveAnswer(isCorrect: false, timeLeft: 0f);
        }

        private void HandleNodeSelected(string selectedNodeId)
        {
            if (_currentExpectedNodeId == null) return;

            bool correct = selectedNodeId == _currentExpectedNodeId;
            ResolveAnswer(correct, timeLeft: 0f);
        }

        private void ResolveAnswer(bool isCorrect, float timeLeft)
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
            _currentExpectedNodeId = null;

            NextQuestion();
        }
    }
}
