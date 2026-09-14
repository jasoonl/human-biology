// Phase 30: raymarches a Texture3D of Hounsfield-unit-like density values, sampling
// a 1D ramp texture to map density -> color/opacity, accumulating front-to-back
// until alpha saturates. No real DICOM/MRI dataset exists in this environment
// (see Planning.md M7) - this compiles and runs against any Texture3D, but has
// only been exercised against a small synthetic volume, not real patient data.
Shader "HumanBodyExplorer/DicomRaymarch"
{
    Properties
    {
        _VolumeTex("Volume (Texture3D)", 3D) = "" {}
        _RampTex("Density Ramp (1D)", 2D) = "white" {}
        _StepCount("Step Count", Range(8, 256)) = 64
        _DensityScale("Density Scale", Range(0, 4)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        Cull Front
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Raymarch"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE3D(_VolumeTex);
            SAMPLER(sampler_VolumeTex);
            TEXTURE2D(_RampTex);
            SAMPLER(sampler_RampTex);

            CBUFFER_START(UnityPerMaterial)
                float _StepCount;
                float _DensityScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.positionOS = IN.positionOS.xyz; // assumes a unit cube [-0.5, 0.5]
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 rayOriginOS = IN.positionOS;
                float3 rayDirWS = normalize(IN.positionWS - _WorldSpaceCameraPos);
                float3 rayDirOS = normalize(mul((float3x3)unity_WorldToObject, rayDirWS));

                float3 uvw = rayOriginOS + 0.5; // [-0.5,0.5] -> [0,1]
                float3 stepVec = rayDirOS * (1.0 / _StepCount);

                float4 accumulated = float4(0, 0, 0, 0);
                int steps = (int)_StepCount;

                for (int i = 0; i < steps; i++)
                {
                    if (any(uvw < 0.0) || any(uvw > 1.0)) break;

                    float density = SAMPLE_TEXTURE3D_LOD(_VolumeTex, sampler_VolumeTex, uvw, 0).r * _DensityScale;
                    float4 sampleColor = SAMPLE_TEXTURE2D_LOD(_RampTex, sampler_RampTex, float2(density, 0.5), 0);

                    accumulated.rgb += (1.0 - accumulated.a) * sampleColor.a * sampleColor.rgb;
                    accumulated.a += (1.0 - accumulated.a) * sampleColor.a;

                    if (accumulated.a >= 0.995) break;

                    uvw += stepVec;
                }

                return accumulated;
            }
            ENDHLSL
        }
    }
}
