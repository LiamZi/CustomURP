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
            Name "Bloom Add"
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
            Name "Bloom PrefilterFireflies"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomPrefilterFirefliesPassFragment
            #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Scatter"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomScatterPassFragment
            #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Scatter Final"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomScatterFinalPassfragment
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

        Pass
        {
            Name "ToneMapping ACES"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ToneMappingACESPassFragment
            #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }

        Pass
        {
            Name "ToneMapping Neutral"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ToneMappingNeutralPassFragment
            #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }

        Pass
        {
            Name "ToneMapping Reinhard"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ToneMappingReinhardPassFragment
            #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }
    }

}