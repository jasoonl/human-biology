using System.Collections;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 98: forces GC.Collect followed by Resources.UnloadUnusedAssets
    /// during major scene transitions, encouraging the OS to reclaim contiguous
    /// memory blocks rather than leaving mobile/WebGL memory fragmented.
    /// </summary>
    public static class MemoryDefragger
    {
        public static IEnumerator DefragmentRoutine()
        {
            System.GC.Collect();
            yield return null;

            var unloadOperation = Resources.UnloadUnusedAssets();
            while (!unloadOperation.isDone)
            {
                yield return null;
            }
        }
    }
}
