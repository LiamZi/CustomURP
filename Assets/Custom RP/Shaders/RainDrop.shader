Shader "Custom RP/RainDrop"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            HLSLINCLUDE
            #include "../ShaderLibrary/Common.hlsl"
            ENDHLSL

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "../ShaderLibrary/RainDrop.hlsl"

         
            ENDHLSL
        }
    }
}