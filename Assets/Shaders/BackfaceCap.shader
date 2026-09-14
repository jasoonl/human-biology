// Phase 25: renders the same mesh's back faces as a solid, unlit color and applies
// the *inverted* slice-plane clip, so where the front-facing AnatomyClippableLit
// surface is cut away, this cap fills in behind it, giving the illusion of a
// solid interior mass rather than a hollow shell.
Shader "HumanBodyExplorer/BackfaceCap"
{
    Properties
    {
        _CapColor("Cap Color", Color) = (0.29, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+1" }
        Cull Front

        Pass
        {
            Name "BackfaceCap"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "AnatomyClipping.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _CapColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Inverted: only draw the cap where the normal clip would have discarded.
                ApplyAnatomyClipping(IN.positionWS, true);
                return _CapColor;
            }
            ENDHLSL
        }
    }
}
