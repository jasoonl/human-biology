using HumanBodyExplorer.CameraSystem;
using UnityEngine;

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

        private Input.InputManager _inputManager;

        private System.Collections.IEnumerator Start()
        {
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
            _inputManager.OnPrimaryInteract += HandlePrimaryInteract;
            AnatomyRaycaster.OnNodeSelected += HandleNodeSelected;

            Debug.Log("[DemoInputBridge] Wired up successfully. Drag to orbit, scroll to zoom, click the cube to select it.");
        }

        private void HandleNodeSelected(string entityId)
        {
            Debug.Log($"[DemoInputBridge] OnNodeSelected fired: {entityId}");
        }

        private void HandleOrbit(Vector2 delta) => orbitalCamera?.Orbit(delta);
        private void HandleZoom(float delta) => orbitalCamera?.Zoom(delta);
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
            _inputManager.OnPrimaryInteract -= HandlePrimaryInteract;
            AnatomyRaycaster.OnNodeSelected -= HandleNodeSelected;
        }
    }
}
