// Screen-space ink contours, so structures read as drawn shapes rather than shaded blobs.
//
// Two rules keep this from wrecking the image, both learned from the first version,
// which darkened the entire backdrop and erased every structure under ~3 pixels wide:
//
//  1. Lines are drawn on the FAR side of a depth step, never the near side. A rib in
//     front of a lung gets its outline painted on the lung pixels next to it, so the
//     rib itself keeps its full width and colour. Drawing centred on the edge (the
//     obvious approach) covers a thin structure completely: at full-figure distance a
//     rib, a finger bone or a vessel is only 2-3 pixels across.
//
//  2. Pixels with no geometry are left alone. The normals texture holds nothing
//     meaningful where nothing was drawn, and treating that as "the normal changed"
//     turns the whole backdrop into one giant edge.
//
// Depth steps are judged relative to distance, so the same threshold means the same
// thing whether the camera is 3.6 m from the whole figure or 0.4 m from a hand.
//
// Requires the DepthNormals prepass; the renderer feature declares that in its
// `requirements` rather than relying on something else in the frame to request it.
Shader "HumanBodyExplorer/AnatomyOutline"
{
    Properties
    {
        _OutlineColor ("Outline Colour", Color) = (0.03, 0.03, 0.05, 1)
        _Thickness ("Thickness (pixels)", Range(0.5, 4)) = 1.0
        _DepthThreshold ("Depth Step (fraction of distance)", Range(0.001, 0.05)) = 0.012
        _NormalThreshold ("Crease Sharpness", Range(0.1, 1.5)) = 0.85
        _Strength ("Strength", Range(0, 1)) = 0.9
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
            float _DepthThreshold;
            float _NormalThreshold;
            float _Strength;

            float EyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel);

                float farLimit = _ProjectionParams.z * 0.98;
                float d0 = EyeDepth(uv);

                float2 t = _Thickness / _ScreenParams.xy;
                float2 offsets[4] = { float2(t.x, 0), float2(-t.x, 0), float2(0, t.y), float2(0, -t.y) };

                // Nearest neighbour, and whether every neighbour is empty space too.
                float nearest = d0;
                float nearestNeighbour = 1e9;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    float d = EyeDepth(uv + offsets[i]);
                    nearestNeighbour = min(nearestNeighbour, d);

                    // A neighbour that is nearer but only one pixel wide - the pixel beyond it is
                    // back at this depth - is a hairline structure (a nerve, a small vessel). Ink on
                    // both sides of it would turn it into a black line, so leave it alone.
                    float dBeyond = EyeDepth(uv + offsets[i] * 2.0);
                    bool hairline = (d0 - d) > d0 * _DepthThreshold && abs(dBeyond - d0) < d0 * _DepthThreshold;
                    if (!hairline) nearest = min(nearest, d);
                }

                // Rule 2: a pixel of pure backdrop with only backdrop around it has
                // nothing to outline.
                if (d0 > farLimit && nearestNeighbour > farLimit) return color;

                // Rule 1: only a pixel that is FARTHER than a neighbour is on the far
                // side of the step. Steps are compared as a fraction of the nearer
                // depth, so the threshold is scale-invariant.
                float step = (d0 - nearest) / max(nearest, 1e-3);
                float depthEdge = smoothstep(_DepthThreshold, _DepthThreshold * 3.0, step);

                // Creases inside a surface, where depth barely changes but the normal
                // does. Only on real geometry, and only against neighbours at nearly
                // the same depth - anything else is already a silhouette above.
                float creaseEdge = 0.0;
                if (d0 < farLimit)
                {
                    float3 n0 = SampleSceneNormals(uv);
                    if (dot(n0, n0) > 0.25)
                    {
                        [unroll] for (int j = 0; j < 4; j++)
                        {
                            float2 nuv = uv + offsets[j];
                            float dn = EyeDepth(nuv);
                            if (abs(dn - d0) / max(d0, 1e-3) > _DepthThreshold) continue;

                            float3 nn = SampleSceneNormals(nuv);
                            if (dot(nn, nn) < 0.25) continue;
                            creaseEdge = max(creaseEdge, 1.0 - saturate(dot(normalize(n0), normalize(nn))));
                        }
                        creaseEdge = smoothstep(_NormalThreshold * 0.5, _NormalThreshold, creaseEdge);
                    }
                }

                float edge = saturate(max(depthEdge, creaseEdge)) * _Strength;
                return half4(lerp(color.rgb, _OutlineColor.rgb, edge), color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
