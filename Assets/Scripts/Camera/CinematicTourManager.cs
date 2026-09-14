using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace HumanBodyExplorer.CameraSystem
{
    /// <summary>
    /// Phase 21: mounts the main camera to a Unity Spline (e.g. one traced through
    /// the digestive tract) and flies it along the spline, looking along the
    /// tangent direction as it goes.
    /// </summary>
    public class CinematicTourManager : MonoBehaviour
    {
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField] private Transform cameraToMove;
        [SerializeField] private float speed = 0.1f; // normalized-t per second

        private Coroutine _activeTour;
        public bool IsTouring { get; private set; }

        public void StartTour()
        {
            if (splineContainer == null || cameraToMove == null) return;
            if (_activeTour != null) StopCoroutine(_activeTour);
            _activeTour = StartCoroutine(FlyThrough());
        }

        public void StopTour()
        {
            if (_activeTour != null) StopCoroutine(_activeTour);
            IsTouring = false;
        }

        private IEnumerator FlyThrough()
        {
            IsTouring = true;
            float t = 0f;

            while (t < 1f)
            {
                t += speed * Time.deltaTime;
                t = Mathf.Clamp01(t);

                Spline spline = splineContainer.Spline;
                spline.Evaluate(t, out float3 position, out float3 tangent, out float3 upVector);

                Vector3 worldPosition = splineContainer.transform.TransformPoint((Vector3)position);
                cameraToMove.position = worldPosition;

                Vector3 forward = ((Vector3)tangent).normalized;
                if (forward.sqrMagnitude > 0.0001f)
                {
                    cameraToMove.rotation = Quaternion.LookRotation(forward, (Vector3)upVector);
                }

                yield return null;
            }

            IsTouring = false;
        }
    }
}
