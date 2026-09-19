using System.Collections;
using HumanBodyExplorer.Core;
using UnityEngine;

namespace HumanBodyExplorer.UI
{
    /// <summary>
    /// Subscribes to QuizController.OnPartFeedback and pulses the corresponding
    /// 3D part(s) green (correct answer) or red (wrong click) via
    /// MaterialPropertyBlock emission, so quiz feedback lands on the figure
    /// itself instead of only appearing as text.
    /// </summary>
    public class AnatomyPartFeedback : MonoBehaviour
    {
        [SerializeField] private ExplorerUIController explorerUI;
        [SerializeField] private float flashDuration = 0.6f;
        [SerializeField] private Color correctFlashColor = new Color(0.2f, 1f, 0.3f);
        [SerializeField] private Color wrongFlashColor = new Color(1f, 0.15f, 0.15f);

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private QuizController _quizController;

        private IEnumerator Start()
        {
            // QuizController is created dynamically at runtime by ExplorerUIController
            // (it doesn't exist at edit time to wire up directly), so poll for it.
            if (explorerUI == null) yield break;

            while (explorerUI.QuizController == null)
            {
                yield return null;
            }

            _quizController = explorerUI.QuizController;
            _quizController.OnPartFeedback += HandleFeedback;
        }

        private void OnDestroy()
        {
            if (_quizController != null) _quizController.OnPartFeedback -= HandleFeedback;
        }

        private void HandleFeedback(string entityId, bool isCorrect)
        {
            foreach (var nodeRef in AnatomyNodeReference.GetByEntityId(entityId))
            {
                var renderer = nodeRef.GetComponent<Renderer>();
                if (renderer == null) continue;

                StartCoroutine(FlashRoutine(renderer, isCorrect ? correctFlashColor : wrongFlashColor));
            }
        }

        private IEnumerator FlashRoutine(Renderer renderer, Color flashColor)
        {
            var block = new MaterialPropertyBlock();
            float elapsed = 0f;

            // Enable emission on the material so the pulse is actually visible;
            // URP/Lit only reads _EmissionColor when the _EMISSION keyword is on.
            renderer.sharedMaterial.EnableKeyword("_EMISSION");

            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float intensity = Mathf.Sin(Mathf.Clamp01(elapsed / flashDuration) * Mathf.PI);

                renderer.GetPropertyBlock(block);
                block.SetColor(EmissionColorId, flashColor * intensity * 2f);
                renderer.SetPropertyBlock(block);

                yield return null;
            }

            renderer.GetPropertyBlock(block);
            block.SetColor(EmissionColorId, Color.black);
            renderer.SetPropertyBlock(block);
        }
    }
}
