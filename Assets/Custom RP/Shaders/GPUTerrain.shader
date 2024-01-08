Shader "Custom RP/GPUTerrain"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _HeightMap ("HeightMap", 2D) = "white" {}
        _NormalMap ("NormalMap", 2D) = "white" {}
        
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "LightMode" = "CustomLit"
        }
        
        LOD 100

        Pass
        {
            HLSLINCLUDE
            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/TerrainInput.hlsl"
            ENDHLSL

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ENABLE_MIP_DEBUG
            #pragma shader_feature _ENABLE_PATCH_DEBUG
            #pragma shader_feature _ENABLE_LOD_SEAMLESS
            #pragma shader_feature _ENABLE_NODE_DEBUG
            

            #include "../ShaderLibrary/GPUTerrain.hlsl"
            
            
            ENDHLSL
        }
    }
}