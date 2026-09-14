using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace HumanBodyExplorer.EditorTools
{
    /// <summary>
    /// Phase 88: watches for Addressables handles that were released but whose
    /// backing mesh memory didn't actually drop, flagging a likely VRAM leak
    /// during Macro-to-Micro scene transitions.
    /// </summary>
    [InitializeOnLoad]
    public static class AssetLeakDetector
    {
        private static long _lastMeshMemory;

        static AssetLeakDetector()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _lastMeshMemory = GetMeshMemory();
            }
        }

        // Profiler.GetRuntimeMemorySizeLong only accepts a specific instance, not a
        // Type, so there's no direct "total memory for all Meshes" API to call.
        // Total allocated memory is used as a coarser but still meaningful proxy.
        public static long GetMeshMemory() => Profiler.GetTotalAllocatedMemoryLong();

        /// <summary>
        /// Call after releasing an Addressables handle group. If mesh memory
        /// didn't drop by at least expectedMinimumDropBytes, logs an error.
        /// </summary>
        public static bool CheckForLeak(long expectedMinimumDropBytes)
        {
            long currentMeshMemory = GetMeshMemory();
            long actualDrop = _lastMeshMemory - currentMeshMemory;

            bool leaked = actualDrop < expectedMinimumDropBytes;
            if (leaked)
            {
                Debug.LogError($"[AssetLeakDetector] Expected mesh memory to drop by at least " +
                                $"{expectedMinimumDropBytes} bytes after Addressables release, but it only dropped " +
                                $"by {actualDrop} bytes. Possible VRAM leak.");
            }

            _lastMeshMemory = currentMeshMemory;
            return leaked;
        }
    }
}
