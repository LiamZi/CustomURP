Shader "Custom URP/BlitAdd"
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
            #pragma fragment frag

            #include "../ShaderLibrary/BlitAdd.hlsl"
            ENDHLSL
        }
    }
    Fallback Off
}