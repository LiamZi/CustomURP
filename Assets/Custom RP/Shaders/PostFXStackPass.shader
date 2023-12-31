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
            Name "Color Grading None"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ColorGradingNonePassFragment
            #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }

        Pass
        {
            Name "Color Grading ACES"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ColorGradingACESPassFragment
            #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }

        Pass
        {
            Name "Color Grading Neutral"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ColorGradingNeutralPassFragment
            #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }

        Pass
        {
            Name "Color Grading Reinhard"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ColorGradingReinhardPassFragment
            // #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }

        Pass
        {
            Name "Apply Color Grading"

            // Blend SrcAlpha OneMinusSrcAlpha
            Blend [_FinalSrcBlend] [_FinalDstBlend]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment ApplyColorGradingPassFragment
            #pragma enable_d3d11_debug_symbols
            ENDHLSL
        }

        Pass
        {
            Name "Apply Color Grading With Luma"
            HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ApplyColorGradingWithLumaPassFragment
			ENDHLSL
        }

        Pass
        {
            Name "Final Rescale"

			Blend [_FinalSrcBlend] [_FinalDstBlend]
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment FinalPassFragmentRescale
                #pragma enable_d3d11_debug_symbols
			ENDHLSL
        }

        Pass
        {
            Name "FXAA"

			Blend [_FinalSrcBlend] [_FinalDstBlend]
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment FXAAPassFragment
                #pragma enable_d3d11_debug_symbols
                #include "../ShaderLibrary/FXAAPass.hlsl"
			ENDHLSL
        }

        
        Pass
        {
            Name "FXAA With Luma"

			Blend [_FinalSrcBlend] [_FinalDstBlend]
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment FXAAPassFragment
                #pragma multi_compile _ FXAA_QUALITY_MEDIUM FXAA_QUALITY_LOW
                // #pragma enable_d3d11_debug_symbols
                #define FXAA_ALPHA_CONTAINS_LUMA
                #include "../ShaderLibrary/FXAAPass.hlsl"
			ENDHLSL
        }
    }

}