Shader "Custom RP/HizDepthTexture"
{
    Properties 
    {
        [HideInInspector]_HizMap("Previous Mipmap", 2D) = "black" {}
    }
    
    SubShader
    {
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "../ShaderLibrary/LitInput.hlsl"
        ENDHLSL

        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 200

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma enable_d3d11_debug_symbols
            
            #pragma vertex vert
            #pragma fragment frag
    
            #include "../ShaderLibrary/HizDepthGenerator.hlsl"
            ENDHLSL
        }
    }
}