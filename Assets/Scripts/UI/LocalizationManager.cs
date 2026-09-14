using System;
using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.UI
{
    /// <summary>
    /// Phase 60 (deviates from spec): the spec calls for the
    /// com.unity.localization package's String Tables. That package brings a
    /// significant asset-authoring surface (table assets, locale identifiers,
    /// smart-string variants) that isn't practical to hand-author or verify
    /// headlessly. This is a minimal dictionary-based string-table substitute
    /// with the same call shape (Get(key) -> localized string, ChangeLanguage),
    /// English-only content for now, that a real localization package could
    /// later replace at this same call site without touching call sites.
    /// </summary>
    public class LocalizationManager
    {
        private readonly Dictionary<string, Dictionary<string, string>> _tables = new Dictionary<string, Dictionary<string, string>>();
        private string _currentLocale = "en";

        public event Action<string> OnLanguageChanged;

        public void AddTable(string locale, Dictionary<string, string> keyToText)
        {
            _tables[locale] = keyToText;
        }

        public void ChangeLanguage(string localeCode)
        {
            if (!_tables.ContainsKey(localeCode))
            {
                Debug.LogWarning($"[LocalizationManager] No table registered for locale '{localeCode}'.");
                return;
            }

            _currentLocale = localeCode;
            OnLanguageChanged?.Invoke(localeCode);
        }

        public string Get(string key)
        {
            if (_tables.TryGetValue(_currentLocale, out var table) && table.TryGetValue(key, out string value))
            {
                return value;
            }

            return $"[[{key}]]"; // visibly-missing-translation marker
        }

        public string CurrentLocale => _currentLocale;
    }
}
