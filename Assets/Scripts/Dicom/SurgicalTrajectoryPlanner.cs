using System.Collections.Generic;
using UnityEngine;

namespace HumanBodyExplorer.Dicom
{
    /// <summary>
    /// Phase 80: lets a surgeon click points on a generated mesh (e.g. Marching
    /// Cubes output) to define a drilling trajectory, then reports distance and
    /// angle-to-world-axis for each segment.
    /// </summary>
    public class SurgicalTrajectoryPlanner : MonoBehaviour
    {
        private readonly List<Vector3> _points = new List<Vector3>();

        public IReadOnlyList<Vector3> Points => _points;

        public void AddPoint(Vector3 point)
        {
            _points.Add(point);
        }

        public void ClearTrajectory()
        {
            _points.Clear();
        }

        public struct TrajectorySegment
        {
            public float Distance;
            public float AngleFromWorldUp;
        }

        public List<TrajectorySegment> ComputeSegments()
        {
            var segments = new List<TrajectorySegment>();

            for (int i = 0; i < _points.Count - 1; i++)
            {
                Vector3 delta = _points[i + 1] - _points[i];
                segments.Add(new TrajectorySegment
                {
                    Distance = delta.magnitude,
                    AngleFromWorldUp = Vector3.Angle(delta, Vector3.up)
                });
            }

            return segments;
        }
    }
}
