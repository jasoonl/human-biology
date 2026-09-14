using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace HumanBodyExplorer.UI
{
    public interface ITextToSpeechProvider
    {
        void Speak(string text);
        void StopSpeaking();
    }

    /// <summary>
    /// Phase 57 (deviates from spec): "native OS speech APIs" (e.g.
    /// NSSpeechSynthesizer on macOS, System.Speech.Synthesis on Windows) require
    /// either a native Objective-C plugin bridge or a Windows-only .NET API -
    /// neither is buildable/testable headlessly in this environment. On macOS
    /// (this dev machine), shells out to the built-in `say` command instead,
    /// which is a real, working TTS path for Editor/Mac-standalone use, but is
    /// not a portable solution for other build targets (WebGL/mobile need a
    /// different provider entirely).
    /// </summary>
    public class AccessibilityManager : MonoBehaviour, ITextToSpeechProvider
    {
        private Process _activeProcess;

        public void Speak(string text)
        {
            StopSpeaking();

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            _activeProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "say",
                Arguments = $"\"{EscapeForShell(text)}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
#else
            Debug.LogWarning($"[AccessibilityManager] No TTS provider wired up for this platform. Text: \"{text}\"");
#endif
        }

        public void StopSpeaking()
        {
            if (_activeProcess != null && !_activeProcess.HasExited)
            {
                _activeProcess.Kill();
            }
            _activeProcess = null;
        }

        private static string EscapeForShell(string text) => text.Replace("\"", "'");

        private void OnDestroy() => StopSpeaking();
    }
}
