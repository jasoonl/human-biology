using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        [SerializeField] private TMP_Text colorblindToggleText;
        [SerializeField] private Button colorblindToggleButton;
        [SerializeField] private ColorblindAccessibilityToggle colorblindToggle;
        [SerializeField] private TMP_Text detailLevelText;
        [SerializeField] private Button detailLevelButton;
        [SerializeField] private AnatomyLayerVisibility layerVisibility;
        [SerializeField] private Button[] layerButtons;
        [SerializeField] private TMP_Text[] layerButtonLabels;
        [SerializeField] private Button reproductiveSexButton;
        [SerializeField] private TMP_Text reproductiveSexText;

        private enum DetailLevel { Plain = 0, ApBiology = 1, Clinical = 2 }
        private static readonly string[] DetailLevelLabels = { "Plain English", "AP Biology", "Clinical" };

        /// <summary>"All" plus every systemCategory value used in anatomy_dictionary.json.
        /// A teacher cycling this before Start Quiz limits the quiz to one body system.</summary>
        private static readonly string[] SystemCategories =
        {
            "All", "Skeletal", "Muscular", "Cardiovascular", "Respiratory", "Digestive",
            "Nervous", "Renal", "Endocrine", "Lymphatic", "Integumentary", "Sensory", "Reproductive"
        };

        private QuizController _quizController;
        private bool _quizActive;
        private int _systemFilterIndex;
        private DetailLevel _detailLevel = DetailLevel.Plain;
        private string _selectedEntityId;
        private AudioSource _audioSource;
        private AudioClip _correctClip;
        private AudioClip _wrongClip;
        public QuizController QuizController => _quizController;
        private const string DefaultInfoText = "Point at a part to see its name; click to isolate it and read about it. Esc or empty space clears; H toggles ghosting.";

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

            if (colorblindToggleButton != null) colorblindToggleButton.onClick.AddListener(ToggleColorblindMode);
            UpdateColorblindToggleText();

            if (detailLevelButton != null) detailLevelButton.onClick.AddListener(CycleDetailLevel);
            UpdateDetailLevelText();

            SetupLayerButtons();

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

        private void SetupLayerButtons()
        {
            if (layerButtons == null) return;

            int count = Mathf.Min(layerButtons.Length, AnatomyLayerVisibility.AllGroups.Length);
            for (int i = 0; i < count; i++)
            {
                var group = AnatomyLayerVisibility.AllGroups[i];
                if (layerButtons[i] != null) layerButtons[i].onClick.AddListener(() => ToggleLayer(group));
            }

            if (reproductiveSexButton != null) reproductiveSexButton.onClick.AddListener(ToggleReproductiveSex);
            RefreshLayerLabels();
        }

        private void ToggleReproductiveSex()
        {
            if (layerVisibility == null) return;
            layerVisibility.ToggleSex();
            RefreshLayerLabels();
        }

        private void ToggleLayer(AnatomyLayerGroup group)
        {
            if (layerVisibility == null) return;
            layerVisibility.Toggle(group);
            RefreshLayerLabels();
        }

        private void RefreshLayerLabels()
        {
            if (reproductiveSexText != null && layerVisibility != null)
                reproductiveSexText.text = $"Reproductive: {layerVisibility.Sex}";
            if (layerButtonLabels == null) return;

            int count = Mathf.Min(layerButtonLabels.Length, AnatomyLayerVisibility.AllGroups.Length);
            for (int i = 0; i < count; i++)
            {
                if (layerButtonLabels[i] == null) continue;

                var group = AnatomyLayerVisibility.AllGroups[i];
                bool shown = layerVisibility == null || layerVisibility.IsVisible(group);
                layerButtonLabels[i].text =
                    $"{(shown ? "●" : "○")}  {AnatomyLayerVisibility.DisplayName(group)}";
                layerButtonLabels[i].color = shown ? Color.white : new Color(0.55f, 0.55f, 0.58f);
            }
        }

        private void ToggleColorblindMode()
        {
            if (colorblindToggle == null) return;
            colorblindToggle.SetEnabled(!colorblindToggle.IsEnabled);
            UpdateColorblindToggleText();
        }

        private void UpdateColorblindToggleText()
        {
            if (colorblindToggleText == null) return;
            bool on = colorblindToggle != null && colorblindToggle.IsEnabled;
            colorblindToggleText.text = on ? "Colorblind Mode: On" : "Colorblind Mode: Off";
        }

        private void Update()
        {
            if (!_quizActive || quizTimerText == null || _quizController == null) return;
            quizTimerText.text = $"Time: {Mathf.CeilToInt(_quizController.TimeRemaining)}s";
        }

        private void ShowInfoFor(string entityId)
        {
            _selectedEntityId = entityId;
            RenderInfoPanel();
        }

        private void CycleDetailLevel()
        {
            _detailLevel = (DetailLevel)(((int)_detailLevel + 1) % DetailLevelLabels.Length);
            UpdateDetailLevelText();
            RenderInfoPanel();
        }

        private void UpdateDetailLevelText()
        {
            if (detailLevelText != null) detailLevelText.text = $"Detail: {DetailLevelLabels[(int)_detailLevel]}";
        }

        private void RenderInfoPanel()
        {
            if (infoPanelText == null) return;

            if (string.IsNullOrEmpty(_selectedEntityId))
            {
                infoPanelText.text = DefaultInfoText;
                return;
            }

            try
            {
                var node = GameManager.Instance.DataController.GetNode(_selectedEntityId);
                var body = new StringBuilder();
                body.Append($"<b>{node.CommonName}</b>  <i>({node.LatinName})</i>");

                if (node.SystemCategory != null && node.SystemCategory.Count > 0)
                {
                    body.Append($"  <size=80%>[{string.Join(" / ", node.SystemCategory)}]</size>");
                }
                body.Append("\n\n");

                switch (_detailLevel)
                {
                    case DetailLevel.ApBiology:
                        body.Append(node.DescriptionProfessional);
                        AppendBullets(body, node.ApBiologyFacts);
                        break;

                    case DetailLevel.Clinical:
                        body.Append(node.DescriptionProfessional);
                        AppendBullets(body, node.ClinicalNotes);
                        // Blank when the code has not been verified: better to show nothing
                        // than a code a clinician might trust and find to be wrong.
                        if (!string.IsNullOrWhiteSpace(node.SnomedCTCode))
                            body.Append($"\n\n<size=80%>SNOMED CT {node.SnomedCTCode}</size>");
                        break;

                    default:
                        body.Append(node.DescriptionPatient);
                        break;
                }

                infoPanelText.text = body.ToString();
            }
            catch (AnatomyNotFoundException)
            {
                infoPanelText.text = DefaultInfoText;
            }
        }

        private static void AppendBullets(StringBuilder body, List<string> lines)
        {
            if (lines == null) return;
            foreach (var line in lines)
            {
                body.Append($"\n\n<b>-</b>  {line}");
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
