using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace HumanBodyExplorer.UI
{
    /// <summary>
    /// Phase 92: installs the browser-side touch-event override (TouchOverride.jslib)
    /// so panning/pinching inside the canvas doesn't also scroll or zoom the host
    /// page. Compiled out entirely on non-WebGL targets. Not build-verified - no
    /// WebGL build has been executed in this environment.
    /// </summary>
    public class TouchGestureManager : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void HBE_InstallTouchOverride();
#endif

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            HBE_InstallTouchOverride();
#else
            Debug.Log("[TouchGestureManager] Touch override is WebGL-only; no-op on this platform.");
#endif
        }
    }
}
