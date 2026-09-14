// Phase 23: fake subsurface-scattering approximation for URP (no HDRP SSS profiles
// available in this project). Approximates "thickness" translucency by adding a
// wrapped, back-lit transmission term on top of standard Lambertian shading.
Shader "HumanBodyExplorer/TissueSSS"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.8, 0.5, 0.45, 1)
        _TransmissionColor("Transmission Color", Color) = (0.9, 0.2, 0.15, 1)
        _ThicknessMultiplier("Thickness Multiplier", Range(0, 3)) = 1
        _TransmissionIntensity("Transmission Intensity", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "AnatomyClipping.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TransmissionColor;
                float _ThicknessMultiplier;
                float _TransmissionIntensity;
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

            half4 frag(Varyings IN) : SV_Target
            {
                ApplyAnatomyClipping(IN.positionWS, false);

                Light mainLight = GetMainLight();
                float3 normal = normalize(IN.normalWS);

                float ndotl = saturate(dot(normal, mainLight.direction));
                float wrappedTransmission = saturate((-dot(normal, mainLight.direction) + 0.5) / 1.5) * _ThicknessMultiplier;

                float3 diffuse = _BaseColor.rgb * ndotl;
                float3 transmission = _TransmissionColor.rgb * wrappedTransmission * _TransmissionIntensity;

                float3 finalColor = (diffuse + transmission) * mainLight.color + _BaseColor.rgb * 0.1; // fake ambient
                return half4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
