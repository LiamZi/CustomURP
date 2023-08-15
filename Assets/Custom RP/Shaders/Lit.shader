Shader "Custom RP/Lit"
{
    Properties 
    {
        _BaseMap("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _CutOff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premultiply Alpha", Float) = 0
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
        [NoScaleOffset] _EmissionMap("Emission", 2D) = "white" {}
        [HDR] _EmissionColor("Emission", Color) = (0.0, 0.0, 0.0, 0.0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque"  "LightMode" = "CustomLit"}
        // Tags { "RenderType"="Opaque"}
        LOD 100

        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            // #pragma enable_d3d11_debug_symbols

            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            
 


            #include "../ShaderLibrary/Lit.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags{ "LightMode" = "ShadowCaster" }

            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            // #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing
            #pragma vertex ShadowCasterVert
            #pragma fragment ShadowCasterFrag
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            // #pragma enable_d3d11_debug_symbols
            #include "../ShaderLibrary/ShadowCaster.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            // #pragma enable_d3d11_debug_symbols
            #pragma vertex MetaPassVert
            #pragma fragment MetaPassFrag
            #include "../ShaderLibrary/MetaPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "CustomShaderGUI"
}