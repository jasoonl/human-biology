using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_WSA
using UnityEngine.Windows.Speech;
#endif

namespace HumanBodyExplorer.UI
{
    /// <summary>
    /// Phase 55: hands-free navigation via keyword recognition. Unity's
    /// KeywordRecognizer (UnityEngine.Windows.Speech) is a Windows/WSA-only API -
    /// this development machine is macOS, so the recognizer path is compiled out
    /// there and SetKeywords/Start log a warning instead of silently doing
    /// nothing, so the gap is visible rather than hidden.
    /// </summary>
    public class VoiceCommandManager : MonoBehaviour
    {
        // Only ever raised on the Windows/WSA branch below; harmless CS0067 on
        // other platforms since the public API shape must stay the same
        // regardless of platform.
#pragma warning disable 0067
        public event Action<string> OnAnatomyNameRecognized;
#pragma warning restore 0067

#if UNITY_STANDALONE_WIN || UNITY_WSA
        private KeywordRecognizer _recognizer;
        private Dictionary<string, string> _phraseToEntityId;

        public void SetKeywords(Dictionary<string, string> phraseToEntityId)
        {
            _phraseToEntityId = phraseToEntityId;
            _recognizer?.Dispose();

            var keywords = new string[_phraseToEntityId.Count];
            _phraseToEntityId.Keys.CopyTo(keywords, 0);

            _recognizer = new KeywordRecognizer(keywords);
            _recognizer.OnPhraseRecognized += OnPhraseRecognized;
        }

        public void StartListening() => _recognizer?.Start();
        public void StopListening() => _recognizer?.Stop();

        private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            if (_phraseToEntityId.TryGetValue(args.text, out string entityId))
            {
                OnAnatomyNameRecognized?.Invoke(entityId);
            }
        }

        private void OnDestroy() => _recognizer?.Dispose();
#else
        public void SetKeywords(Dictionary<string, string> phraseToEntityId)
        {
            Debug.LogWarning("[VoiceCommandManager] KeywordRecognizer is Windows/WSA-only; " +
                              "voice navigation is unavailable on this platform.");
        }

        public void StartListening()
        {
            Debug.LogWarning("[VoiceCommandManager] Voice navigation unavailable on this platform.");
        }

        public void StopListening() { }
#endif
    }
}
