// Phase 37: GPU-instanced point cloud for raw MRI sample points. Deviates from the
// spec's geometry-shader billboarding: Metal (this project's target GPU backend on
// macOS) does not support geometry shaders at all, so each point is instead
// expanded into a camera-facing quad directly in the vertex shader by indexing a
// StructuredBuffer<float3> with SV_InstanceID, which is the portable modern
// equivalent and works via Graphics.DrawProcedural.
Shader "HumanBodyExplorer/PointCloud"
{
    Properties
    {
        _PointColor("Point Color", Color) = (1, 1, 1, 1)
        _PointSize("Point Size", Float) = 0.01
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "PointCloud"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            StructuredBuffer<float3> _PointPositions;

            CBUFFER_START(UnityPerMaterial)
                float4 _PointColor;
                float _PointSize;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            static const float2 kQuadOffsets[6] = {
                float2(-1, -1), float2(1, -1), float2(-1, 1),
                float2(-1, 1), float2(1, -1), float2(1, 1)
            };

            Varyings vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;

                float3 worldCenter = _PointPositions[instanceID];
                float2 offset = kQuadOffsets[vertexID % 6] * _PointSize;

                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = UNITY_MATRIX_V[1].xyz;
                float3 worldPos = worldCenter + camRight * offset.x + camUp * offset.y;

                OUT.positionCS = TransformWorldToHClip(worldPos);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _PointColor;
            }
            ENDHLSL
        }
    }
}
