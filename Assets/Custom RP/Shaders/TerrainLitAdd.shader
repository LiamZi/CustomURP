Shader "Custom RP/TerrainLitAdd"
{
    Properties
    {
		[HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
		[HideInInspector] _Splat3("Layer 3 (A)", 2D) = "white" {}
		[HideInInspector] _Splat2("Layer 2 (B)", 2D) = "white" {}
		[HideInInspector] _Splat1("Layer 1 (G)", 2D) = "white" {}
		[HideInInspector] _Splat0("Layer 0 (R)", 2D) = "white" {}
		[HideInInspector] _Normal3("Normal 3 (A)", 2D) = "bump" {}
		[HideInInspector] _Normal2("Normal 2 (B)", 2D) = "bump" {}
		[HideInInspector] _Normal1("Normal 1 (G)", 2D) = "bump" {}
		[HideInInspector] _Normal0("Normal 0 (R)", 2D) = "bump" {}
		[HideInInspector][Gamma] _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
		[HideInInspector][Gamma] _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
		[HideInInspector][Gamma] _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0
		[HideInInspector][Gamma] _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0.0
		[HideInInspector] _Mask3("Mask 3 (A)", 2D) = "grey" {}
		[HideInInspector] _Mask2("Mask 2 (B)", 2D) = "grey" {}
		[HideInInspector] _Mask1("Mask 1 (G)", 2D) = "grey" {}
		[HideInInspector] _Mask0("Mask 0 (R)", 2D) = "grey" {}
		[HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _NormalScale0("NormalScale 0", Range(0.0, 16.0)) = 1.0
		[HideInInspector] _NormalScale1("NormalScale 1", Range(0.0, 16.0)) = 1.0
		[HideInInspector] _NormalScale2("NormalScale 2", Range(0.0, 16.0)) = 1.0
		[HideInInspector] _NormalScale3("NormalScale 3", Range(0.0, 16.0)) = 1.0
		[HideInInspector] _LayerHasMask0("Layer Has Mask 0", Float) = 0.0
		[HideInInspector] _LayerHasMask1("Layer Has Mask 1", Float) = 0.0
		[HideInInspector] _LayerHasMask2("Layer Has Mask 2", Float) = 0.0
		[HideInInspector] _LayerHasMask3("Layer Has Mask 3", Float) = 0.0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
        	Blend One One
        	
        	HLSLINCLUDE
            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/LitInput.hlsl"
            ENDHLSL

            HLSLPROGRAM
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _TERRAIN_BLEND_HEIGHT
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _MASKMAP
            // Sample normal in pixel shader when doing instancing
            #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL
                        
            #pragma vertex vert
            #pragma fragment frag
            
            #include "../ShaderLibrary/TerrainLitInput.hlsl"
            #include "../ShaderLibrary/TerrainLit.hlsl"
            ENDHLSL
        }
    }
}