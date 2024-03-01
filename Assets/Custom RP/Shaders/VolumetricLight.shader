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

			ZTest Off
			Cull Off
			ZWrite Off
			Blend One One, One Zero
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ _HEIGHT_FOG
            #pragma shader_feature _ _NOISE
            #pragma shader_feature _ _DIRECTIONAL
            #pragma shader_feature _ _DIRECTIONAL_COOKIE
            #pragma multi_compile _ USE_CLUSTER_LIGHT
            #pragma enable_d3d11_debug_symbols


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