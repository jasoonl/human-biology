// Phase 33: fullscreen Daltonization pass. Shifts the red/green confusion axis
// (Protanopia) toward blue/yellow, increasing contrast between arteries (red) and
// veins (blue) for colorblind-assisted viewing.
Shader "HumanBodyExplorer/Colorblind"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Daltonize"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord.xy, _BlitMipLevel);

                // Simplified Protanopia daltonization matrix: pushes red/green
                // confusion toward the more discriminable blue/yellow axis.
                half3x3 daltonize = half3x3(
                    0.567, 0.433, 0.000,
                    0.558, 0.442, 0.000,
                    0.000, 0.242, 0.758
                );

                half3 shifted = mul(daltonize, color.rgb);
                half3 result = lerp(color.rgb, shifted, 0.6);

                return half4(result, color.a);
            }
            ENDHLSL
        }
    }
}
