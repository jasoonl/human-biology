using UnityEngine;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// Phase 91: STUB ONLY. PreEmptive Dotfuscator requires a commercial license
    /// and CLI installation that don't exist in this environment - there is
    /// nothing to genuinely integrate against. Documents the intended build hook.
    /// </summary>
    public static class SecurityObfuscator
    {
        public static bool TryObfuscateBuild(string assemblyPath)
        {
            Debug.LogWarning("[SecurityObfuscator] No Dotfuscator CLI/license is configured for this project; " +
                              $"'{assemblyPath}' was not obfuscated.");
            return false;
        }
    }
}
