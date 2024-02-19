Shader "Custom RP/VolumetricLight"
{
    Properties
    {
        [HideInInspector]_MainTex("Main Texture", 2D) = "white" {}
        [HideInInspector]_ZTest("ZTest", Float) = 0
        [HideInInspector]_LightColor("Light Color" , Color) = (1, 1, 1, 1)
        
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
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "../ShaderLibrary/VolumetricLightInput.hlsl"
            #include "../ShaderLibrary/VolumetricLight.hlsl"
            
            ENDHLSL
        }
    }
}