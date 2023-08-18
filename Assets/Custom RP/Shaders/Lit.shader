Shader "Custom RP/Lit"
{
    Properties 
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_MASK_MAP)] _MaskMapToggle("Mask Map", Float) = 0
        [NoScaleOffset]_MaskMap("Mask (MODS)", 2D) = "white" {}
        _Metallic("Metallic", Range(0, 1)) = 0
        _Occlusion("Occlusion", Range(0, 1)) = 1
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _Fresnel("Fresnel", Range(0, 1)) = 1
        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premultiply Alpha", Float) = 0
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
        [NoScaleOffset] _EmissionMap("Emission", 2D) = "white" {}
        [HDR] _EmissionColor("Emission", Color) = (0.0, 0.0, 0.0, 0.0)

        [Toggle(_DETAIL_MAP)] _DetailMapToggle("Detail Maps", Float) = 0
        _DetailMap("Details", 2D) = "linearGery" {}
        [NoScaleOffset] _DetailNormalMap("Detail Normals", 2D) = "bump" {}
        _DetailAlbedo("Detail Albedo", Range(0, 1)) = 1
        _DetailSmoothness("Detail Smoothness", Range(0, 1)) = 1
        _DetailNormalScale("Detail Normal Scale", Range(0, 1)) = 1
        [Toggle(_NORMAL_MAP)] _NormalMapToggle ("Normal Map", Float) = 0
        [NoScaleOffset] _NormalMap("Normals", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0, 1)) = 1

        [HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
        [HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)


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
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE 
            #pragma multi_compile _ LOD_FADE_CROSSFADE LOD_FADE_PERCENTAGE 
            #pragma shader_feature _NORMAL_MAP
            #pragma shader_feature _MASK_MAP
            #pragma shader_feature _DETAIL_MAP

            #pragma vertex vert
            #pragma fragment frag

             
            // #pragma enable_d3d11_debug_symbols

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
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #pragma multi_compile _ LOD_FADE_CROSSFADE LOD_FADE_PERCENTAGE 

            #pragma vertex ShadowCasterVert
            #pragma fragment ShadowCasterFrag
            
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