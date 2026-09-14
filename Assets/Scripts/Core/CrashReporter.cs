using System;
using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    [Serializable]
    public struct CrashBreadcrumb
    {
        public string Message;
        public string StackTrace;
        public GameState GameStateAtCrash;
        public Vector3 CameraPosition;
        public string Timestamp;
    }

    /// <summary>
    /// Phase 99 (deviates from spec): no Sentry account/SDK license exists in
    /// this environment, so this hooks Application.logMessageReceived and
    /// captures the same breadcrumb data (GameState, camera position, timestamp)
    /// the spec describes, but writes it to a local JSON file instead of
    /// uploading to an external crash-reporting service.
    /// </summary>
    public class CrashReporter : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera trackedCamera;
        [SerializeField] private int maxBreadcrumbs = 20;

        private readonly List<CrashBreadcrumb> _breadcrumbs = new List<CrashBreadcrumb>();
        public IReadOnlyList<CrashBreadcrumb> Breadcrumbs => _breadcrumbs;

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Exception) return;

            var breadcrumb = new CrashBreadcrumb
            {
                Message = message,
                StackTrace = stackTrace,
                GameStateAtCrash = GameManager.Instance != null ? GameManager.Instance.CurrentGameState : GameState.Initializing,
                CameraPosition = trackedCamera != null ? trackedCamera.transform.position : Vector3.zero,
                Timestamp = DateTime.UtcNow.ToString("O")
            };

            _breadcrumbs.Add(breadcrumb);
            if (_breadcrumbs.Count > maxBreadcrumbs) _breadcrumbs.RemoveAt(0);

            WriteBreadcrumbsToDisk();
        }

        private void WriteBreadcrumbsToDisk()
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, "crash_breadcrumbs.json");
            string json = JsonUtility.ToJson(new BreadcrumbListWrapper { Breadcrumbs = _breadcrumbs }, prettyPrint: true);
            System.IO.File.WriteAllText(path, json);
        }

        [Serializable]
        private struct BreadcrumbListWrapper
        {
            public List<CrashBreadcrumb> Breadcrumbs;
        }
    }
}
