using HumanBodyExplorer.CameraSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Connects the already-implemented InputManager events (Module II, Phase 11)
    /// to an AdvancedOrbitalCamera and AnatomyRaycaster in the scene. Nothing
    /// wired these together previously since Module II's automated tests exercise
    /// each piece independently - this is purely for interactively demoing the
    /// integration in the Editor.
    /// </summary>
    public class DemoInputBridge : MonoBehaviour
    {
        [SerializeField] private CameraSystem.AdvancedOrbitalCamera orbitalCamera;
        [SerializeField] private AnatomyRaycaster raycaster;
        [SerializeField] private CameraSystem.CameraFocusTargeter focusTargeter;
        [SerializeField] private GameObject bodyRoot;

        private Input.InputManager _inputManager;

        private System.Collections.IEnumerator Start()
        {
            // Auto-fit the camera to the figure's actual rendered bounds rather
            // than trusting a hand-picked distance/position constant - robust to
            // any future change in part sizes/positions.
            if (focusTargeter != null && bodyRoot != null)
            {
                focusTargeter.FocusOn(bodyRoot);
            }

            while (GameManager.Instance == null || GameManager.Instance.InputController == null)
            {
                yield return null;
            }

            _inputManager = GameManager.Instance.InputController as Input.InputManager;
            if (_inputManager == null)
            {
                Debug.LogError("[DemoInputBridge] GameManager.Instance.InputController is not an InputManager - " +
                                "orbit/zoom/click input will not work.");
                yield break;
            }

            _inputManager.OnOrbit += HandleOrbit;
            _inputManager.OnZoom += HandleZoom;
            _inputManager.OnPan += HandlePan;
            _inputManager.OnDoubleClick += HandleDoubleClick;
            _inputManager.OnPrimaryInteract += HandlePrimaryInteract;
            AnatomyRaycaster.OnNodeSelected += HandleNodeSelected;

            Debug.Log("[DemoInputBridge] Wired up successfully. Drag to orbit, scroll to zoom, click a body part to select it.");
        }

        private void HandleNodeSelected(string entityId)
        {
            Debug.Log($"[DemoInputBridge] OnNodeSelected fired: {entityId}");
        }

        private void HandleOrbit(Vector2 delta) => orbitalCamera?.Orbit(delta);
        private void HandleZoom(float delta) => orbitalCamera?.Zoom(delta);
        private void HandlePan(Vector2 delta) => orbitalCamera?.Pan(delta);

        // Double-click a spot on the body to slide the view onto it and close in.
        private void HandleDoubleClick(Vector2 screenPos)
        {
            var cam = orbitalCamera != null ? orbitalCamera.GetComponent<Camera>() : null;
            if (cam == null) return;
            if (Physics.Raycast(cam.ScreenPointToRay(screenPos), out RaycastHit hit, 50f, ~0, QueryTriggerInteraction.Ignore))
                orbitalCamera.FocusPoint(hit.point, 0.5f);
        }

        // WASD / arrow keys slide the view (Q/E or PageDown/PageUp for height); R or Home resets it.
        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || orbitalCamera == null) return;

            Vector3 move = Vector3.zero;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.z += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.z -= 1f;
            if (kb.eKey.isPressed || kb.pageUpKey.isPressed) move.y += 1f;
            if (kb.qKey.isPressed || kb.pageDownKey.isPressed) move.y -= 1f;
            orbitalCamera.Move(move, Time.unscaledDeltaTime);

            if (kb.rKey.wasPressedThisFrame || kb.homeKey.wasPressedThisFrame) orbitalCamera.ResetView();
        }
        private void HandlePrimaryInteract(Vector2 screenPos)
        {
            Debug.Log($"[DemoInputBridge] Click received at screen position {screenPos}.");
            raycaster?.ExecuteClick(screenPos);
        }

        private void OnDestroy()
        {
            if (_inputManager == null) return;
            _inputManager.OnOrbit -= HandleOrbit;
            _inputManager.OnZoom -= HandleZoom;
            _inputManager.OnPan -= HandlePan;
            _inputManager.OnDoubleClick -= HandleDoubleClick;
            _inputManager.OnPrimaryInteract -= HandlePrimaryInteract;
            AnatomyRaycaster.OnNodeSelected -= HandleNodeSelected;
        }
    }
}
