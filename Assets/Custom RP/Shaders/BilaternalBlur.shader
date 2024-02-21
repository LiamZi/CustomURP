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

        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off
            Blend One Zero
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment highHorizontalFrag

            #include "../ShaderLibrary/BilaternalBlurFunction.hlsl"
            #include "../ShaderLibrary/BilaternalBlur.hlsl"
            ENDHLSL
        }
    }
}