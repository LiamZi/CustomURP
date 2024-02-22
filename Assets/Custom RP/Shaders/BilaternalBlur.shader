Shader "Custom RP/BilateralBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        ENDHLSL

        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100
        ZTest Always
        Cull Off
        ZWrite Off
        Blend One Zero
        
        //pass 0 horizontal frag high
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment highHorizontalFrag
            #pragma target 4.0
            
            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
            #include "../ShaderLibrary/BilaternalBlur.hlsl"
            ENDHLSL
        }

        //pass 1 vertical frag high
        Pass
        {
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment highVerticalFrag
            #pragma target 4.0
            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
            #include "../ShaderLibrary/BilaternalBlur.hlsl"
            ENDHLSL
        }
        
        //pass 2 horizontal blur (low)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment lowHorizontalFrag
            #pragma target 4.0
            
            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
            #include "../ShaderLibrary/BilaternalBlur.hlsl"
            ENDHLSL
        }
        
        //pass 3 Vertica blur (low)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment lowVerticalFrag
            #pragma target 4.0
            
            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
            #include "../ShaderLibrary/BilaternalBlur.hlsl"
            ENDHLSL
        }
        
        // pass 4 - downsample depth to half
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vertHalfDepth
            #pragma fragment fragHalfDepth
            #pragma target 4.0
            
            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
            #include "../ShaderLibrary/BilaternalBlur.hlsl"
            ENDHLSL
        }

        // pass 5 - bilateral upsample
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vertUpSampleToFull
            #pragma fragment fragUpSampleToFull
            #pragma target 4.0
            
            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
            #include "../ShaderLibrary/BilaternalBlur.hlsl"
            ENDHLSL
        }
        
        
//        Pass
//        {
//            HLSLPROGRAM
//            #pragma vertex vertUpSampleToFull
//            #pragma fragment fragUpSampleToFull
//            #pragma target 4.0
//            
//            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
//            #include "../ShaderLibrary/BilaternalBlur.hlsl"
//            ENDHLSL
//        }
        
        // pass 6 - downsample depth to quarter
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vertQuarterDepth
            #pragma fragment fragQuarterDepth
            #pragma target 4.0
            
            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
            #include "../ShaderLibrary/BilaternalBlur.hlsl"
            ENDHLSL
        }
        
        // pass 7 - bilateral upsample quarter to full
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vertUpSampleToFull
            #pragma fragment fragUpSampleToFull
            #pragma target 4.0
            
            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
            #include "../ShaderLibrary/BilaternalBlur.hlsl"
            ENDHLSL
        }

        // pass 8 - horizontal blur (quarter res)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment quarterhorizontalFrag
            #pragma target 4.0
            
            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
            #include "../ShaderLibrary/BilaternalBlur.hlsl"
            ENDHLSL
        }
        
        // pass 9 - vertical blur (quarter res)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment quarterVerticalFrag
            #pragma target 4.0
            
            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
            #include "../ShaderLibrary/BilaternalBlur.hlsl"
            ENDHLSL
        }
        
        // pass 10 - downsample depth to half
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vertHalfDepth
            #pragma fragment fragHalfDepth
            #pragma target 4.0
            
            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
            #include "../ShaderLibrary/BilaternalBlur.hlsl"
            ENDHLSL
        }
        
        // pass 11 - downsample depth to quarter
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vertQuarterDepth
            #pragma fragment fragQuarterDepth
            #pragma target 4.0
            
            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
            #include "../ShaderLibrary/BilaternalBlur.hlsl"
            ENDHLSL
        }
    }
}