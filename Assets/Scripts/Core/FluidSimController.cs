using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 34 (reduced scope): dispatches FluidAdvection.compute each frame over
    /// a small 2D grid, mapped to a RenderTexture for a stomach/bladder fill
    /// visual. This drives the advection term only - see the shader file's header
    /// comment for the full scope-reduction note (no pressure-projection step,
    /// i.e. not an incompressible solver).
    /// </summary>
    public class FluidSimController : MonoBehaviour
    {
        [SerializeField] private ComputeShader advectionShader;
        [SerializeField] private int gridSize = 128;
        [SerializeField] private float dissipation = 0.995f;

        private RenderTexture _fieldA;
        private RenderTexture _fieldB;
        private int _kernelIndex;
        private bool _pingPong;

        public RenderTexture OutputField => _pingPong ? _fieldA : _fieldB;

        private void Awake()
        {
            if (advectionShader == null) return;

            _fieldA = CreateField();
            _fieldB = CreateField();
            _kernelIndex = advectionShader.FindKernel("Advect");
        }

        private RenderTexture CreateField()
        {
            var rt = new RenderTexture(gridSize, gridSize, 0, RenderTextureFormat.ARGBFloat)
            {
                enableRandomWrite = true
            };
            rt.Create();
            return rt;
        }

        private void Update()
        {
            if (advectionShader == null || _fieldA == null || _fieldB == null) return;

            RenderTexture source = _pingPong ? _fieldB : _fieldA;
            RenderTexture destination = _pingPong ? _fieldA : _fieldB;

            advectionShader.SetTexture(_kernelIndex, "_VelocityField", destination);
            advectionShader.SetTexture(_kernelIndex, "_VelocityFieldRead", source);
            advectionShader.SetFloat("_DeltaTime", Time.deltaTime);
            advectionShader.SetFloat("_Dissipation", dissipation);
            advectionShader.SetInt("_TextureWidth", gridSize);
            advectionShader.SetInt("_TextureHeight", gridSize);

            int threadGroups = Mathf.CeilToInt(gridSize / 8f);
            advectionShader.Dispatch(_kernelIndex, threadGroups, threadGroups, 1);

            _pingPong = !_pingPong;
        }

        private void OnDestroy()
        {
            if (_fieldA != null) _fieldA.Release();
            if (_fieldB != null) _fieldB.Release();
        }
    }
}
