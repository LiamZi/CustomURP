// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
Shader "Custom RP/WavingGrass"
{
    Properties
    {
        _WavingTint ("Fade Color", Color) = (1, 1, 1, 1)
        _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
		[Toggle(_NORMAL_MAP)]_NormalMap("Use Normal", Float) = 0
        _BumpMap("Normal Map", 2D) = "bump" {}
		_GrassCutoff("_GrassCutoff", float) = 0.5
		_WindControl("Wind Control", Vector) = (1, 1, 1, 1)
		_WaveSpeed("Wave Speed", Float) = 1
		_Smoothness("Smoothness", Float) = 1.0
		_Transluency("Transluency", Range(0, 1)) = 0.5
		[Toggle(FORCE_UP_NORMAL)]FORCE_UP_NORMAL("Force Up Normal", Float) = 0
		[Toggle(INTERACTIVE)]INTERACTIVE("Interactive", Float) = 0
    	[Toggle(_CLIPPING)]_Clipping("Clipping", Float) = 0
	}
    
	SubShader
	{
		HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "../ShaderLibrary/LitInput.hlsl"
        ENDHLSL

		Tags 
		{
			"LightMode" = "CustomLit" 
			"Queue" = "Geometry+200" 
			"RenderType" = "Grass" 
			"IgnoreProjector" = "True" 
		} //"DisableBatching"="True"
		
		Cull Off
		LOD 200
		AlphaTest Greater[_Cutoff]
		ColorMask RGB

		Pass
		{
			HLSLPROGRAM
			
			#pragma prefer_hlslcc gles
			// #pragma exclude_renderers d3d11_9x
			#pragma target 3.5

			// -------------------------------------
			// Universal Pipeline keywords
			// #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			// #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			// #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			// #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			// #pragma multi_compile _ _SHADOWS_SOFT
			// #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			#pragma shader_feature _CLIPPING
            #pragma shader_feature _RECEIVE_SHADOWS
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma shader_feature _NORMAL_MAP
            #pragma shader_feature _MASK_MAP
            #pragma shader_feature _DETAIL_MAP
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _OTHER_PCF3 _OTHER_PCF5 _OTHER_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE 
            #pragma multi_compile _ LOD_FADE_CROSSFADE LOD_FADE_PERCENTAGE 
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _LIGHTS_PER_OBJECT
            #pragma multi_compile _ USE_CLUSTER_LIGHT
			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fog
			#pragma enable_d3d11_debug_symbols

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			///////////my defined
			#pragma multi_compile _ FORCE_UP_NORMAL
			#pragma multi_compile _ INTERACTIVE
			// #pragma multi_compile _ _NORMALMAP			
			#pragma vertex vert
			#pragma fragment frag
			#define _ALPHATEST_ON
			
			#if defined(INTERACTIVE)
			uniform float4 _Grass_Press_Point;
			#endif

			UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_DEFINE_INSTANCED_PROP(float4, _PerInstanceColor)
			UNITY_INSTANCING_BUFFER_END(Props)

			#include "../ShaderLibrary/WavingGrassInput.hlsl"
			#include "../ShaderLibrary/WavingGrass.hlsl"
			ENDHLSL
		}
    }
}
