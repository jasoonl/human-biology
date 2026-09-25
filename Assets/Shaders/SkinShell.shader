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
        // Solid mode: fully opaque, lit like skin. The script also moves the material to the opaque
        // queue and turns depth writing on so nothing inside can draw over it.
        [Toggle] _Solid ("Solid", Float) = 0
        [HideInInspector] _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "SkinShell"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite [_ZWrite]
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
                float _Solid;
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
                float3 viewDirWS : TEXCOORD1;
                float3 tint : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positions.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(positions.positionWS);
                OUT.tint = IN.color.rgb;   // hair, brows and lips are painted onto the skin mesh as vertex colour
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                float3 v = normalize(IN.viewDirWS);

                if (_Solid > 0.5)
                {
                    Light sun = GetMainLight();
                    float ndl = dot(n, sun.direction);
                    float wrap = saturate((ndl + 0.45) / 1.45);          // soft terminator, like skin scatters light
                    float rim = pow(1.0 - saturate(dot(n, v)), 3.0);
                    float3 h = normalize(sun.direction + v);
                    float spec = pow(saturate(dot(n, h)), 40.0) * 0.07;
                    float3 lit = _BaseColor.rgb * IN.tint * (0.30 + 0.80 * wrap) * (0.6 + 0.4 * sun.color);
                    lit += float3(0.16, 0.05, 0.03) * rim * wrap;         // warm blood-colour glow at grazing angles
                    lit += spec;
                    return half4(lit, 1.0);
                }

                float facing = saturate(dot(n, v));
                float fresnel = pow(1.0 - facing, _FresnelPower);
                float alpha = lerp(_CenterAlpha, _EdgeAlpha, fresnel);

                Light light = GetMainLight();
                float diffuse = saturate(dot(n, light.direction)) * 0.5 + 0.5;
                float3 colour = _BaseColor.rgb * IN.tint * (_Ambient + (1.0 - _Ambient) * diffuse);

                return half4(colour, alpha);
            }
            ENDHLSL
        }

        // Depth passes, used only while the skin is drawn solid (in the opaque queue). Without them the depth
        // texture holds the bones and organs beneath the skin, and screen-space ambient occlusion paints their
        // outlines onto it as dark bands.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half frag(Varyings IN) : SV_Target { return IN.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { return half4(NormalizeNormalPerPixel(IN.normalWS), 0.0); }
            ENDHLSL
        }
    }

    Fallback Off
}
