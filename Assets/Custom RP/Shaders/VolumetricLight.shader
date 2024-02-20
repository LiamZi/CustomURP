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
            //pass 0 - directional light
            HLSLINCLUDE
            #include "../ShaderLibrary/Common.hlsl"
            ENDHLSL
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ _HEIGHT_FOG
            #pragma shader_feature _ _NOISE
            #pragma shader_feature _ _DIRECTIONAL
            #pragma shader_feature _ _DIRECTIONAL_COOKIE

            #include "../ShaderLibrary/VolumetricLightInput.hlsl"
            #include "../ShaderLibrary/VolumetricLight.hlsl"
            
            ENDHLSL
        }

        Pass
        {
            //pass 1 - point light
            
        }
        
        Pass
        {
            //pass 2 - spot light
        }
    }
}