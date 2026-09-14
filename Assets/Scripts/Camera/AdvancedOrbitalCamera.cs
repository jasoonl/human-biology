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
            _targetPitch = Mathf.Clamp(_targetPitch, -85f, 85f);
        }

        public void Zoom(float delta)
        {
            _targetDistance = Mathf.Clamp(_targetDistance - delta * zoomSensitivity, minDistance, maxDistance);
        }

        public void SetZoomDistanceImmediate(float value)
        {
            _targetDistance = Mathf.Clamp(value, minDistance, maxDistance);
        }

        private void LateUpdate()
        {
            if (target == null) return;

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
