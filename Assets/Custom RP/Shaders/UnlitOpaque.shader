Shader "Custom RP/UnlitOpaque"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #pragma vertex vert
            #pragma fragment frag

            #include "../ShaderLibrary/Unlit.hlsl"

          
            ENDHLSL
        }
    }
}
