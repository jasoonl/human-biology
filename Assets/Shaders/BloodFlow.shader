// Phase 36: scrolls a procedural noise pattern along the V-axis of vessel UVs to
// visually simulate blood rushing beneath the vessel wall. _FlowSpeed/_PulseIntensity
// are intended to be driven by CardioAnimationController's BPM (Module V, Phase 50).
Shader "HumanBodyExplorer/BloodFlow"
{
    Properties
    {
        _BaseColor("Vessel Color", Color) = (0.55, 0.05, 0.05, 1)
        _FlowColor("Flow Highlight Color", Color) = (0.9, 0.1, 0.1, 1)
        _FlowSpeed("Flow Speed", Float) = 1
        _PulseIntensity("Pulse Intensity", Range(0, 1)) = 0.5
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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _FlowColor;
                float _FlowSpeed;
                float _PulseIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positionInputs.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            float Hash1(float x)
            {
                return frac(sin(x * 127.1) * 43758.5453);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float scrolledV = IN.uv.y - _Time.y * _FlowSpeed;
                float band = frac(scrolledV * 6.0);
                float pulse = smoothstep(0.4, 0.5, band) * smoothstep(0.6, 0.5, band);

                float3 color = lerp(_BaseColor.rgb, _FlowColor.rgb, pulse * _PulseIntensity);

                Light mainLight = GetMainLight();
                float ndotl = saturate(dot(normalize(IN.normalWS), mainLight.direction)) * 0.7 + 0.3;

                return half4(color * ndotl * mainLight.color, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
