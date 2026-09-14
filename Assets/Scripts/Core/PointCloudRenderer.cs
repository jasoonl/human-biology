using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 37: reads a CSV of Vector3 coordinates, uploads them to a
    /// ComputeBuffer, and renders them via Graphics.DrawProcedural with
    /// PointCloud.shader's vertex-shader billboard expansion (see that shader's
    /// header comment for why this replaces the spec's geometry-shader approach).
    /// </summary>
    public class PointCloudRenderer : MonoBehaviour
    {
        [SerializeField] private Material pointCloudMaterial;

        private ComputeBuffer _pointBuffer;
        private int _pointCount;

        public int PointCount => _pointCount;

        public static List<Vector3> ParseCsv(string csvText)
        {
            var points = new List<Vector3>();
            using var reader = new StringReader(csvText);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 3) continue;

                if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                    float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                {
                    points.Add(new Vector3(x, y, z));
                }
            }

            return points;
        }

        public void LoadPoints(IReadOnlyList<Vector3> points)
        {
            ReleaseBuffer();

            _pointCount = points.Count;
            if (_pointCount == 0) return;

            _pointBuffer = new ComputeBuffer(_pointCount, sizeof(float) * 3);
            var array = new Vector3[_pointCount];
            for (int i = 0; i < _pointCount; i++) array[i] = points[i];
            _pointBuffer.SetData(array);

            if (pointCloudMaterial != null)
            {
                pointCloudMaterial.SetBuffer("_PointPositions", _pointBuffer);
            }
        }

        private void OnRenderObject()
        {
            if (_pointBuffer == null || pointCloudMaterial == null || _pointCount == 0) return;

            pointCloudMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, _pointCount);
        }

        private void ReleaseBuffer()
        {
            _pointBuffer?.Release();
            _pointBuffer = null;
        }

        private void OnDestroy()
        {
            ReleaseBuffer();
        }
    }
}
