// Phase 27: vertex shader "soft body fake" - displaces vertices along their normal
// by _BulgeAmount, masked by painted vertex color (red channel = bulge weight,
// e.g. painted at the center of a bicep) so only the intended region swells.
Shader "HumanBodyExplorer/MuscleBulge"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.75, 0.35, 0.3, 1)
        _BulgeAmount("Bulge Amount", Range(0, 1)) = 0
        _MaxBulgeDistance("Max Bulge Distance", Float) = 0.05
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
                float _BulgeAmount;
                float _MaxBulgeDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float mask = IN.color.r;
                float3 displacedPositionOS = IN.positionOS.xyz + IN.normalOS * (_BulgeAmount * mask * _MaxBulgeDistance);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(displacedPositionOS);
                OUT.positionCS = positionInputs.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                Light mainLight = GetMainLight();
                float ndotl = saturate(dot(normalize(IN.normalWS), mainLight.direction)) * 0.8 + 0.2;
                return half4(_BaseColor.rgb * ndotl * mainLight.color, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
