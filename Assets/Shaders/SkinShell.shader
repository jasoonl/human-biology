// The skin: a single translucent shell over the whole figure.
//
// The previous skin was nineteen overlapping alpha-blended ellipsoids, which stacked into
// a muddy grey haze wherever two of them overlapped and obscured everything behind them.
// This is one continuous surface whose opacity depends on the viewing angle: nearly
// transparent where you look straight through it, denser toward the silhouette. The
// figure reads as a body with its structures visible inside it, which is how a
// layered anatomical model is meant to look.
Shader "HumanBodyExplorer/SkinShell"
{
    Properties
    {
        [MainColor] _BaseColor ("Skin Colour", Color) = (0.87, 0.70, 0.56, 0.35)
        _CenterAlpha ("Alpha Looking Straight Through", Range(0, 1)) = 0.05
        _EdgeAlpha ("Alpha At The Silhouette", Range(0, 1)) = 0.60
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 2.4
        _Ambient ("Ambient Light", Range(0, 1)) = 0.62
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "SkinShell"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _CenterAlpha;
                float _EdgeAlpha;
                float _FresnelPower;
                float _Ambient;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positions.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(positions.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                float3 v = normalize(IN.viewDirWS);

                float facing = saturate(dot(n, v));
                float fresnel = pow(1.0 - facing, _FresnelPower);
                float alpha = lerp(_CenterAlpha, _EdgeAlpha, fresnel);

                Light light = GetMainLight();
                float diffuse = saturate(dot(n, light.direction)) * 0.5 + 0.5;
                float3 colour = _BaseColor.rgb * (_Ambient + (1.0 - _Ambient) * diffuse);

                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
