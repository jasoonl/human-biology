using System.Collections;
using HumanBodyExplorer.CameraSystem;
using HumanBodyExplorer.Core;
using HumanBodyExplorer.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HumanBodyExplorer.UI
{
    /// <summary>
    /// Ties the already-built systems together into the actual classroom-facing
    /// experience: clicking a body part shows its name/description in an info
    /// panel, and a "Start Quiz" button drives QuizController as visible
    /// gameplay (prompt, live score, pass/fail feedback) instead of a
    /// console-only test harness.
    /// </summary>
    public class ExplorerUIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text infoPanelText;
        [SerializeField] private Button startQuizButton;
        [SerializeField] private GameObject quizPanelRoot;
        [SerializeField] private TMP_Text quizPromptText;
        [SerializeField] private TMP_Text quizScoreText;
        [SerializeField] private TMP_Text quizFeedbackText;
        [SerializeField] private TMP_Text quizTimerText;
        [SerializeField] private TMP_Text systemFilterText;
        [SerializeField] private Button systemFilterButton;

        /// <summary>"All" plus every systemCategory value used in anatomy_dictionary.json.
        /// A teacher cycling this before Start Quiz limits the quiz to one body system.</summary>
        private static readonly string[] SystemCategories =
        {
            "All", "Skeletal", "Muscular", "Cardiovascular", "Respiratory", "Digestive",
            "Nervous", "Renal", "Endocrine", "Lymphatic", "Integumentary"
        };

        private QuizController _quizController;
        private bool _quizActive;
        private int _systemFilterIndex;
        private AudioSource _audioSource;
        private AudioClip _correctClip;
        private AudioClip _wrongClip;
        public QuizController QuizController => _quizController;
        private const string DefaultInfoText = "Click on a body part to learn what it does.";

        private IEnumerator Start()
        {
            while (GameManager.Instance == null ||
                   GameManager.Instance.DataController == null ||
                   GameManager.Instance.StudyTracker == null)
            {
                yield return null;
            }

            if (infoPanelText != null) infoPanelText.text = DefaultInfoText;
            if (quizPanelRoot != null) quizPanelRoot.SetActive(false);

            AnatomyRaycaster.OnNodeSelected += ShowInfoFor;

            _quizController = GetComponent<QuizController>();
            if (_quizController == null) _quizController = gameObject.AddComponent<QuizController>();
            _quizController.Initialize(GameManager.Instance.StudyTracker, GameManager.Instance.DataController);
            _quizController.OnQuestionPrompted += HandleQuestionPrompted;
            _quizController.OnAnswerResolved += HandleAnswerResolved;
            _quizController.OnQuizComplete += HandleQuizComplete;

            if (startQuizButton != null) startQuizButton.onClick.AddListener(BeginQuiz);

            if (systemFilterButton != null) systemFilterButton.onClick.AddListener(CycleSystemFilter);
            UpdateSystemFilterText();

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _correctClip = ProceduralAudio.GenerateCorrectChime();
            _wrongClip = ProceduralAudio.GenerateWrongBuzz();
        }

        private void CycleSystemFilter()
        {
            _systemFilterIndex = (_systemFilterIndex + 1) % SystemCategories.Length;
            UpdateSystemFilterText();
        }

        private void UpdateSystemFilterText()
        {
            if (systemFilterText != null) systemFilterText.text = $"Study: {SystemCategories[_systemFilterIndex]}";
        }

        private void Update()
        {
            if (!_quizActive || quizTimerText == null || _quizController == null) return;
            quizTimerText.text = $"Time: {Mathf.CeilToInt(_quizController.TimeRemaining)}s";
        }

        private void ShowInfoFor(string entityId)
        {
            if (infoPanelText == null) return;

            try
            {
                var node = GameManager.Instance.DataController.GetNode(entityId);
                infoPanelText.text = $"<b>{node.CommonName}</b>  <i>({node.LatinName})</i>\n\n{node.DescriptionPatient}";
            }
            catch (AnatomyNotFoundException)
            {
                infoPanelText.text = DefaultInfoText;
            }
        }

        private void BeginQuiz()
        {
            if (quizPanelRoot != null) quizPanelRoot.SetActive(true);
            if (quizFeedbackText != null) quizFeedbackText.text = string.Empty;
            _quizActive = true;
            UpdateScoreText();

            string filter = SystemCategories[_systemFilterIndex];
            _quizController.StartQuiz(filter == "All" ? null : filter);
        }

        private void HandleQuestionPrompted(string prompt)
        {
            if (quizPromptText != null) quizPromptText.text = prompt;
        }

        private void HandleAnswerResolved(bool wasCorrect, int scoreDelta)
        {
            if (quizFeedbackText != null)
            {
                quizFeedbackText.text = wasCorrect
                    ? $"Correct! +{scoreDelta}"
                    : $"Not quite. {scoreDelta}";
            }
            _audioSource.PlayOneShot(wasCorrect ? _correctClip : _wrongClip);
            UpdateScoreText();
        }

        private void HandleQuizComplete()
        {
            _quizActive = false;
            if (quizPromptText != null) quizPromptText.text = "Quiz complete!";
            if (quizFeedbackText != null) quizFeedbackText.text = $"Final score: {_quizController.Score.Score}";
            if (quizTimerText != null) quizTimerText.text = string.Empty;
        }

        private void UpdateScoreText()
        {
            if (quizScoreText != null)
            {
                quizScoreText.text = $"Score: {_quizController.Score.Score}  (Streak: {_quizController.Score.CurrentStreak})";
            }
        }

        private void OnDestroy()
        {
            AnatomyRaycaster.OnNodeSelected -= ShowInfoFor;
            if (_quizController != null)
            {
                _quizController.OnQuestionPrompted -= HandleQuestionPrompted;
                _quizController.OnAnswerResolved -= HandleAnswerResolved;
                _quizController.OnQuizComplete -= HandleQuizComplete;
            }
        }
    }
}
