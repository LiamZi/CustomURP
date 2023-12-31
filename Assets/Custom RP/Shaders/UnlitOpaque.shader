Shader "Custom RP/UnlitOpaque"
{
    Properties
    {
        _BaseColor("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "../ShaderLibrary/UnlitInput.hlsl"
        ENDHLSL

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
