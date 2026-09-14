using UnityEngine;

namespace HumanBodyExplorer.MicroSim
{
    /// <summary>
    /// Phases 40+41 (reduced scope - plain C# instead of ECS/Burst, see
    /// Planning.md M4). Updates each blood cell's progress along a Bezier curve in
    /// a single tight loop, then renders all of them in one draw call via
    /// Graphics.RenderMeshInstanced. Not Burst-compiled, so it will not sustain
    /// 100,000 cells the way the spec's ECS version targets - untested above a few
    /// thousand in this environment.
    /// </summary>
    public class BloodFlowSimulator : MonoBehaviour
    {
        [SerializeField] private Mesh cellMesh;
        [SerializeField] private Material cellMaterial;
        [SerializeField] private Vector3 bezierStart = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 bezierControl = new Vector3(0, 1, 0);
        [SerializeField] private Vector3 bezierEnd = new Vector3(0, 2, 0);
        [SerializeField] private int cellCount = 256;
        [SerializeField] private float cellScale = 0.01f;

        private float[] _progress;
        private float[] _speed;
        private Matrix4x4[] _matrices;
        private const int BatchSize = 1023; // Graphics.RenderMeshInstanced hard limit

        private void Awake()
        {
            _progress = new float[cellCount];
            _speed = new float[cellCount];
            _matrices = new Matrix4x4[cellCount];

            var random = new System.Random(1);
            for (int i = 0; i < cellCount; i++)
            {
                _progress[i] = (float)random.NextDouble();
                _speed[i] = 0.2f + (float)random.NextDouble() * 0.3f;
            }
        }

        public static Vector3 EvaluateBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        private void Update()
        {
            if (cellMesh == null || cellMaterial == null) return;

            for (int i = 0; i < cellCount; i++)
            {
                _progress[i] += _speed[i] * Time.deltaTime;
                if (_progress[i] > 1f) _progress[i] -= 1f;

                Vector3 position = EvaluateBezier(bezierStart, bezierControl, bezierEnd, _progress[i]);
                _matrices[i] = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * cellScale);
            }

            for (int offset = 0; offset < cellCount; offset += BatchSize)
            {
                int batchLength = Mathf.Min(BatchSize, cellCount - offset);
                var batch = new Matrix4x4[batchLength];
                System.Array.Copy(_matrices, offset, batch, 0, batchLength);
                Graphics.RenderMeshInstanced(new RenderParams(cellMaterial), cellMesh, 0, batch);
            }
        }

        public float GetProgress(int index) => _progress[index];
    }
}
