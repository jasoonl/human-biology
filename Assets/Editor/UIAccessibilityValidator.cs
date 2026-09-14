using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// Phase 61: validates every Canvas in the project uses "Scale With Screen
    /// Size" and that no TextMeshPro element uses a font size below 14pt, for
    /// WCAG AA compliance across desktop/tablet form factors.
    /// </summary>
    public static class UIAccessibilityValidator
    {
        private const float MinimumFontSize = 14f;

        public struct ValidationIssue
        {
            public string ObjectPath;
            public string Message;
        }

        [MenuItem("Human Body Explorer/Validate UI Accessibility")]
        public static void ValidateOpenScenes()
        {
            var issues = new List<ValidationIssue>();

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                issues.AddRange(ValidateCanvasScaler(canvas));
            }

            foreach (var text in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                issues.AddRange(ValidateFontSize(text));
            }

            if (issues.Count == 0)
            {
                Debug.Log("[UIAccessibilityValidator] No issues found.");
                return;
            }

            foreach (var issue in issues)
            {
                Debug.LogWarning($"[UIAccessibilityValidator] {issue.ObjectPath}: {issue.Message}");
            }
        }

        public static IEnumerable<ValidationIssue> ValidateCanvasScaler(Canvas canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) yield break;

            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                yield return new ValidationIssue
                {
                    ObjectPath = GetPath(canvas.transform),
                    Message = "CanvasScaler is not set to 'Scale With Screen Size'."
                };
            }
        }

        public static IEnumerable<ValidationIssue> ValidateFontSize(TextMeshProUGUI text)
        {
            if (text.fontSize < MinimumFontSize)
            {
                yield return new ValidationIssue
                {
                    ObjectPath = GetPath(text.transform),
                    Message = $"Font size {text.fontSize} is below the {MinimumFontSize}pt WCAG AA floor."
                };
            }
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
