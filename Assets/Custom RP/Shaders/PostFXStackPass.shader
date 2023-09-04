Shader "Hidden/Custom RP/Post FX Stack"
{
    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "../ShaderLibrary/PostFXStackPasses.hlsl"
        ENDHLSL

        Pass
        {
            Name "Bloom Combine"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomCombinePassFragment
             #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Horizontal"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomHorizontalPassFragment
            #pragma enable_d3d11_debug_symbols

            ENDHLSL
        }

        Pass
        {
            Name "Bloom Prefilter"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomPrefilterPassFragment
            #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Vertical"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomVerticalPassFragment
            #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }

        Pass
        {
            Name "Copy"
            HLSLPROGRAM
            
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment CopyPassFragment
            #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }
    }

}