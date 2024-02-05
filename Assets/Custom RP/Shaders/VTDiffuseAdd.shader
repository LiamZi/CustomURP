Shader "Custom RP/VTDiffuseAdd"
{
    Properties
    {
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
		[HideInInspector] _Splat3("Layer 3 (A)", 2D) = "grey" {}
		[HideInInspector] _Splat2("Layer 2 (B)", 2D) = "grey" {}
		[HideInInspector] _Splat1("Layer 1 (G)", 2D) = "grey" {}
		[HideInInspector] _Splat0("Layer 0 (R)", 2D) = "grey" {}
		[HideInInspector] _DiffuseRemapScale0("_DiffuseRemapScale0", Color) = (1, 1, 1, 1)
		[HideInInspector] _DiffuseRemapScale1("_DiffuseRemapScale1", Color) = (1, 1, 1, 1)
		[HideInInspector] _DiffuseRemapScale2("_DiffuseRemapScale2", Color) = (1, 1, 1, 1)
		[HideInInspector] _DiffuseRemapScale3("_DiffuseRemapScale3", Color) = (1, 1, 1, 1)
        [HideInInspector] _Mask3("Mask 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Mask2("Mask 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Mask1("Mask 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Mask0("Mask 0 (R)", 2D) = "grey" {}
        [HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _HasMask0("Has Mask 0", Float) = 0.0
		[HideInInspector] _HasMask1("Has Mask 1", Float) = 0.0
		[HideInInspector] _HasMask2("Has Mask 2", Float) = 0.0
		[HideInInspector] _HasMask3("Has Mask 3", Float) = 0.0
    }
    SubShader
    {
        Tags 
        { 
        	"RenderType"="Opaque" 
	        "LightMode" = "CustomLit"
        }
        Cull Off ZWrite Off ZTest Always
        
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "../ShaderLibrary/LitInput.hlsl"
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
			#pragma target 3.5
            #pragma vertex vert
            #pragma fragment fragAdd
            

			#include "../ShaderLibrary/VTDiffuseInput.hlsl"
            #include "../ShaderLibrary/VTDiffuse.hlsl"
            
            ENDHLSL
        }
    }
}