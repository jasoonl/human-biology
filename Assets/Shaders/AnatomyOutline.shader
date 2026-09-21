// Screen-space edge detection that gives every structure a drawn ink contour,
// which is what makes a 3D render read as an anatomical plate rather than a pile
// of shaded blobs. Primitive geometry benefits most: a capsule and a real bone
// look far more alike once both are bounded by a line.
//
// Two independent edge signals are combined, because neither alone is enough:
//   - Depth discontinuity catches silhouettes and structures overlapping in front
//     of one another (a rib crossing the lung behind it).
//   - Normal discontinuity catches creases where two surfaces meet at an angle but
//     sit at the same depth (the ridge where a muscle belly meets its tendon),
//     which depth alone cannot see.
//
// Requires the DepthNormals prepass; the renderer feature declares that via its
// `requirements` field rather than relying on something else in the frame to
// happen to request it.
Shader "HumanBodyExplorer/AnatomyOutline"
{
    Properties
    {
        _OutlineColor ("Outline Colour", Color) = (0.04, 0.04, 0.05, 1)
        _Thickness ("Thickness (pixels)", Range(0.5, 4)) = 1.2
        _DepthSensitivity ("Depth Sensitivity", Range(0.05, 20)) = 4.0
        _NormalSensitivity ("Normal Sensitivity", Range(0.05, 8)) = 2.5
        _Strength ("Strength", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "AnatomyOutline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutlineColor;
            float _Thickness;
            float _DepthSensitivity;
            float _NormalSensitivity;
            float _Strength;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel);

                float2 t = _Thickness / _ScreenParams.xy;

                // Roberts cross: two diagonal pairs, four taps total. Cheaper than a
                // full Sobel and plenty for a line this thin.
                float2 uvA = uv + float2(-t.x, -t.y);
                float2 uvB = uv + float2( t.x,  t.y);
                float2 uvC = uv + float2(-t.x,  t.y);
                float2 uvD = uv + float2( t.x, -t.y);

                float dA = LinearEyeDepth(SampleSceneDepth(uvA), _ZBufferParams);
                float dB = LinearEyeDepth(SampleSceneDepth(uvB), _ZBufferParams);
                float dC = LinearEyeDepth(SampleSceneDepth(uvC), _ZBufferParams);
                float dD = LinearEyeDepth(SampleSceneDepth(uvD), _ZBufferParams);

                // Scale the difference by the nearer depth, or every structure in the
                // background would outline more strongly than one close to the camera
                // purely because the same step is a larger absolute distance out there.
                float centreDepth = max(1e-4, min(min(dA, dB), min(dC, dD)));
                float depthEdge = (abs(dA - dB) + abs(dC - dD)) / centreDepth;
                depthEdge = saturate(depthEdge * _DepthSensitivity);

                float3 nA = SampleSceneNormals(uvA);
                float3 nB = SampleSceneNormals(uvB);
                float3 nC = SampleSceneNormals(uvC);
                float3 nD = SampleSceneNormals(uvD);

                float normalEdge = (1.0 - saturate(dot(nA, nB))) + (1.0 - saturate(dot(nC, nD)));
                normalEdge = saturate(normalEdge * _NormalSensitivity);

                float edge = saturate(max(depthEdge, normalEdge)) * _Strength;

                return half4(lerp(color.rgb, _OutlineColor.rgb, edge), color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
