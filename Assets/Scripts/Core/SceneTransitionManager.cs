using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 97: always loads scenes asynchronously with a progress callback,
    /// holding at 0.9f (Unity's async load caps there until activation is
    /// allowed) until a fade-to-black completes, then activates the new scene.
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float fadeDuration = 0.5f;

        public event Action<float> OnLoadProgress;

        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                OnLoadProgress?.Invoke(operation.progress);
                yield return null;
            }

            OnLoadProgress?.Invoke(1f);

            yield return Fade(0f, 1f);

            operation.allowSceneActivation = true;
            yield return operation;

            yield return Fade(1f, 0f);
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadeOverlay == null) yield break;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeOverlay.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }
            fadeOverlay.alpha = to;
        }
    }
}
