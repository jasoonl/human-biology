using System;
using HumanBodyExplorer.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HumanBodyExplorer.Input
{
    /// <summary>
    /// Phase 11: builds an InputActionMap programmatically (mouse orbit/zoom/click,
    /// touch primary/secondary position) and exposes C# events for the rest of the
    /// app to subscribe to, rather than requiring a serialized .inputactions asset.
    /// XR bindings are declared but cannot be exercised without a headset in this
    /// environment (see Planning.md).
    /// </summary>
    public class InputManager : IInputController, IDisposable
    {
        private InputActionMap _map;
        private InputAction _pointerPosition;
        private InputAction _pointerDelta;
        private InputAction _scroll;
        private InputAction _primaryClick;
        private InputAction _secondaryClick;
        private InputAction _touch0Position;
        private InputAction _touch1Position;

        public event Action<Vector2> OnOrbit;
        public event Action<float> OnZoom;
        public event Action<Vector2> OnPrimaryInteract;

        public bool IsEnabled { get; private set; }

        public System.Threading.Tasks.Task InitializeAsync()
        {
            BuildActionMap();
            Enable();
            Debug.Log("[InputManager] Initialized (mouse + touch bindings).");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private void BuildActionMap()
        {
            _map = new InputActionMap("AnatomyExplorer");

            _pointerPosition = _map.AddAction("PointerPosition", InputActionType.Value, "<Pointer>/position");
            _pointerDelta = _map.AddAction("PointerDelta", InputActionType.Value, "<Pointer>/delta");
            _scroll = _map.AddAction("Scroll", InputActionType.Value, "<Mouse>/scroll");
            _primaryClick = _map.AddAction("PrimaryClick", InputActionType.Button, "<Mouse>/leftButton");
            _secondaryClick = _map.AddAction("SecondaryClick", InputActionType.Button, "<Mouse>/rightButton");
            _touch0Position = _map.AddAction("Touch0Position", InputActionType.Value, "<Touchscreen>/touch0/position");
            _touch1Position = _map.AddAction("Touch1Position", InputActionType.Value, "<Touchscreen>/touch1/position");

            // XR bindings declared for future hardware; unexercised without a headset.
            _map.AddAction("XRPinch", InputActionType.Button, "<XRController>/gripButton");
            _map.AddAction("XRTrigger", InputActionType.Button, "<XRController>/triggerButton");

            _primaryClick.performed += ctx => OnPrimaryInteract?.Invoke(_pointerPosition.ReadValue<Vector2>());
            _pointerDelta.performed += ctx =>
            {
                if (_secondaryClick.IsPressed() || _primaryClick.IsPressed())
                {
                    OnOrbit?.Invoke(ctx.ReadValue<Vector2>());
                }
            };
            _scroll.performed += ctx => OnZoom?.Invoke(ctx.ReadValue<Vector2>().y);
        }

        public void Enable()
        {
            if (IsEnabled) return;
            _map?.Enable();
            IsEnabled = true;
        }

        public void Disable()
        {
            if (!IsEnabled) return;
            _map?.Disable();
            IsEnabled = false;
        }

        public Vector2 ReadTouch0Position() => _touch0Position?.ReadValue<Vector2>() ?? Vector2.zero;
        public Vector2 ReadTouch1Position() => _touch1Position?.ReadValue<Vector2>() ?? Vector2.zero;
        public bool IsSecondTouchActive() => Touchscreen.current != null && Touchscreen.current.touches.Count > 1
            && Touchscreen.current.touches[1].press.isPressed;

        public void Dispose()
        {
            _map?.Disable();
            _map?.Dispose();
        }
    }
}
