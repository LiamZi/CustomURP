Shader "Custom RP/Lit"
{
    Properties 
    {
        _Color("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #pragma enable_d3d11_debug_symbols

            #pragma vertex vert
            #pragma fragment frag

            #include "../ShaderLibrary/Lit.hlsl"
            ENDHLSL
        }
    }
}