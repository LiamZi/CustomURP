Shader "Custom RP/Lit"
{
    Properties 
    {
        _BaseMap("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _CutOff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Meteallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
    }

    SubShader
    {
        // Tags { "RenderType"="Opaque"  "LightMode" = "CustomLit"}
        Tags { "RenderType"="Opaque"}
        LOD 100

        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            // #pragma enable_d3d11_debug_symbols

            #pragma vertex vert
            #pragma fragment frag

            #include "../ShaderLibrary/Lit.hlsl"
            ENDHLSL
        }
    }
}