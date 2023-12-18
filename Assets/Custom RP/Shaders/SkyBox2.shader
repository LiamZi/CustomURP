Shader "Custom RP/SkyBox2"
{
    Properties
    {
        [KeywordEnum(None, Simple, Hight Quality)] _SunDisk("Sun", Int) = 2
        _SunSize("Sun Size", Range(0, 1)) = 0.04
        _SunSizeConvergence("Sun Size Convergence", Range(1, 10)) = 5
        _AtmosphereThickness("Atmosphere Thickness", Range(0, 5)) = 1.5
        _SkyTint("Sky Tint", Color) = (0.5, 0.5, 0.5, 1.0)
        _GroundColor("Ground", Color) = (0.369, 0.349, 0.341, 1.0)
        _Exposure("Exposure", Range(0, 8)) = 1.3
    }
    SubShader
    {
        Tags
        {
           "Queue"="Background" 
            "RenderType"="Background" 
            "PreviewType"="Skybox"
        }
        
        Cull Off
        ZWrite Off
        LOD 100

        Pass
        {
            HLSLINCLUDE
            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/SkyBoxInput2.hlsl"
            ENDHLSL
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile_local _SUNDISK_NONE _SUNDISK_SIMPLE _SUNDISK_HIGH_QUALITY

            #include "../ShaderLibrary/ProceduralSkyBox2.hlsl"

            ENDHLSL
        }
    }
}