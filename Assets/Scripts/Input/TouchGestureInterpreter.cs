using HumanBodyExplorer.CameraSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HumanBodyExplorer.Input
{
    /// <summary>
    /// Phase 13: interprets two-finger pinch/pan gestures from the touchscreen and
    /// maps them onto the orbital camera's zoom/orbit inputs.
    /// </summary>
    public class TouchGestureInterpreter : MonoBehaviour
    {
        [SerializeField] private AdvancedOrbitalCamera orbitalCamera;
        [SerializeField] private float panOrbitSensitivity = 0.05f;
        [SerializeField] private float pinchZoomSensitivity = 0.01f;

        private bool _hadTwoTouchesLastFrame;
        private float _previousDistance;
        private Vector2 _previousMidpoint;

        private void Update()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null || touchscreen.touches.Count < 2) return;

            var t0 = touchscreen.touches[0];
            var t1 = touchscreen.touches[1];

            if (!t0.press.isPressed || !t1.press.isPressed)
            {
                _hadTwoTouchesLastFrame = false;
                return;
            }

            Vector2 p0 = t0.position.ReadValue();
            Vector2 p1 = t1.position.ReadValue();

            float currentDistance = Vector2.Distance(p0, p1);
            Vector2 currentMidpoint = (p0 + p1) * 0.5f;

            if (_hadTwoTouchesLastFrame && orbitalCamera != null)
            {
                float distanceDelta = currentDistance - _previousDistance;
                orbitalCamera.Zoom(distanceDelta * pinchZoomSensitivity);

                Vector2 midpointDelta = currentMidpoint - _previousMidpoint;
                orbitalCamera.Orbit(midpointDelta * panOrbitSensitivity);
            }

            _previousDistance = currentDistance;
            _previousMidpoint = currentMidpoint;
            _hadTwoTouchesLastFrame = true;
        }
    }
}
