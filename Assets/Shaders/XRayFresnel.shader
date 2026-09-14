// Phase 31: rim-lit X-ray effect. Fresnel term isolates the silhouette, additive
// blending, with a procedural scrolling hash-noise (no noise texture asset exists
// in this project) standing in for "radiological interference" in the emission.
Shader "HumanBodyExplorer/XRayFresnel"
{
    Properties
    {
        _RimColor("Rim Color", Color) = (0.3, 0.8, 1.0, 1)
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 3
        _NoiseScrollSpeed("Noise Scroll Speed", Float) = 0.5
        _NoiseIntensity("Noise Intensity", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        Blend One One
        ZWrite Off
        Cull Back

        Pass
        {
            Name "XRay"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _RimColor;
                float _FresnelPower;
                float _NoiseScrollSpeed;
                float _NoiseIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float3 normal = normalize(IN.normalWS);

                float fresnel = pow(1.0 - saturate(dot(viewDir, normal)), _FresnelPower);

                float2 noiseUV = IN.positionWS.xy * 10.0 + _Time.y * _NoiseScrollSpeed;
                float noise = Hash21(noiseUV) * _NoiseIntensity;

                float3 emission = _RimColor.rgb * (fresnel + noise);
                return half4(emission, fresnel);
            }
            ENDHLSL
        }
    }
}
