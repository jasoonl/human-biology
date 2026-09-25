using UnityEngine;

namespace HumanBodyExplorer.CameraSystem
{
    /// <summary>
    /// Phase 12: smooth-damped orbital camera with gimbal-lock protection and a
    /// collision spherecast so the camera never clips through anatomy geometry.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class AdvancedOrbitalCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 3f;
        [SerializeField] private float minDistance = 0.1f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private float orbitSensitivity = 0.2f;
        [SerializeField] private float zoomSensitivity = 1f;
        [SerializeField] private float smoothTime = 0.12f;
        [SerializeField] private float collisionRadius = 0.05f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private float _yaw;
        private float _pitch;
        private float _targetYaw;
        private float _targetPitch;
        private float _targetDistance;

        // The point being orbited. Pan and focus move it; the target transform follows it, and
        // anything else that moves the target directly (CameraFocusTargeter) is adopted, not fought.
        private Vector3 _targetFocus;
        private Vector3 _focusVelocity;
        private Vector3 _lastApplied;
        private bool _focusInitialised;
        private Vector3 _homeFocus, _homeAngles;
        private float _homeDistance = -1f;

        /// <summary>The figure stands about 1.75 m tall; the focus may not wander far outside it.</summary>
        private static readonly Vector3 FocusMin = new Vector3(-0.6f, -0.05f, -0.6f);
        private static readonly Vector3 FocusMax = new Vector3(0.6f, 1.9f, 0.6f);

        private float _yawVelocity;
        private float _pitchVelocity;
        private float _distanceVelocity;

        public float ZoomDistance => distance;
        public Transform Target { get => target; set => target = value; }

        private void Awake()
        {
            _targetDistance = distance;
            var angles = transform.eulerAngles;
            _yaw = _targetYaw = angles.y;
            _pitch = _targetPitch = angles.x;
        }

        public void Orbit(Vector2 delta)
        {
            _targetYaw += delta.x * orbitSensitivity;
            _targetPitch -= delta.y * orbitSensitivity;
            _targetPitch = Mathf.Clamp(_targetPitch, -89f, 89f);
        }

        public void Zoom(float delta)
        {
            // Scale the step by how far out we already are, so one notch covers a lot
            // of ground when looking at the whole body and becomes fine-grained when
            // inspecting a single organ. A fixed step feels broken at both ends.
            float step = delta * zoomSensitivity * Mathf.Max(0.3f, _targetDistance);
            _targetDistance = Mathf.Clamp(_targetDistance - step, minDistance, maxDistance);
        }

        public void SetZoomDistanceImmediate(float value)
        {
            _targetDistance = Mathf.Clamp(value, minDistance, maxDistance);
            // The first framing (CameraFocusTargeter at start-up) is the "home" view that Reset returns to.
            if (_homeDistance < 0f && Application.isPlaying) _homeDistance = _targetDistance;
        }

        /// <summary>Slide the view sideways and up/down by a screen-space drag, as if grabbing the body.</summary>
        public void Pan(Vector2 pixelDelta)
        {
            if (target == null) return;
            SyncFocus();
            float perPixel = 2f * _targetDistance * Mathf.Tan(GetComponent<Camera>().fieldOfView * 0.5f * Mathf.Deg2Rad)
                / Mathf.Max(1f, Screen.height);
            var move = -(transform.right * pixelDelta.x + transform.up * pixelDelta.y) * perPixel;
            _targetFocus = ClampFocus(_targetFocus + move);
        }

        /// <summary>Keyboard flight: x = right, y = up, z = forward along the ground plane.</summary>
        public void Move(Vector3 input, float dt)
        {
            if (target == null || input == Vector3.zero) return;
            SyncFocus();
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 world = transform.right * input.x + Vector3.up * input.y + forward * input.z;
            _targetFocus = ClampFocus(_targetFocus + world * (0.5f * Mathf.Max(0.4f, _targetDistance) * dt));
        }

        /// <summary>Centre the view on a point, optionally closing in on it.</summary>
        public void FocusPoint(Vector3 point, float closeDistance = -1f)
        {
            if (target == null) return;
            SyncFocus();
            _targetFocus = ClampFocus(point);
            if (closeDistance > 0f) _targetDistance = Mathf.Clamp(Mathf.Min(_targetDistance, closeDistance), minDistance, maxDistance);
        }

        /// <summary>Back to the opening view of the whole figure.</summary>
        public void ResetView()
        {
            if (target == null || !_focusInitialised) return;
            _targetFocus = _homeFocus;
            _targetYaw = _homeAngles.y;
            _targetPitch = _homeAngles.x;
            if (_homeDistance > 0f) _targetDistance = _homeDistance;
        }

        private static Vector3 ClampFocus(Vector3 p) => new Vector3(
            Mathf.Clamp(p.x, FocusMin.x, FocusMax.x), Mathf.Clamp(p.y, FocusMin.y, FocusMax.y), Mathf.Clamp(p.z, FocusMin.z, FocusMax.z));

        // Someone else (the auto-framing) may have repositioned the target since we last did.
        private void SyncFocus()
        {
            if (!_focusInitialised || (target.position - _lastApplied).sqrMagnitude > 1e-8f)
            {
                _targetFocus = target.position;
                _lastApplied = target.position;
                if (!_focusInitialised)
                {
                    _focusInitialised = true;
                    _homeFocus = target.position;
                    _homeAngles = new Vector3(_targetPitch, _targetYaw, 0f);
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            SyncFocus();
            target.position = Vector3.SmoothDamp(target.position, _targetFocus, ref _focusVelocity, smoothTime);
            _lastApplied = target.position;

            _yaw = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVelocity, smoothTime);
            _pitch = Mathf.SmoothDampAngle(_pitch, _targetPitch, ref _pitchVelocity, smoothTime);
            distance = Mathf.SmoothDamp(distance, _targetDistance, ref _distanceVelocity, smoothTime);

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var desiredPosition = target.position - rotation * Vector3.forward * distance;

            float effectiveDistance = distance;
            Vector3 directionFromTarget = (desiredPosition - target.position);
            float rayLength = directionFromTarget.magnitude;

            if (rayLength > 0.0001f &&
                Physics.SphereCast(target.position, collisionRadius, directionFromTarget.normalized,
                    out RaycastHit hit, rayLength, collisionMask, QueryTriggerInteraction.Ignore))
            {
                effectiveDistance = Mathf.Max(minDistance, hit.distance - 0.1f);
            }

            transform.rotation = rotation;
            transform.position = target.position - rotation * Vector3.forward * effectiveDistance;
        }
    }
}
