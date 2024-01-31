Shader "Custom RP/SkyBox"
{
    Properties
    {
        _SunSize ("Sun Size", Range(0, 1)) = 0.05
        _SunIntensity("Sun Intensity", Float) = 2
        _SunCol ("Sun Colour", Color) = (1, 1, 1, 1)
        _SunDirectionWS("Sun DirectionWS", Vector) = (1, 1, 1, 1)
        _Exposure("Exposure", Range(0, 8))  = 1.3
        _AtmosphereThickness("Atmosphere Thickness", Range(0, 5)) = 1.0
        _SkyTint ("Sky Tint", Color) = (.5, .5, .5, 1)
        _GroundColor ("Ground", Color) = (.369, .349, .341, 1)
        _ScatteringIntensity("Scattering Intensity", Float) = 1
        _StarTex("Star Texture", 2D) = "white" {}
        _MilkyWayTex("Milky Way Texture", 2D) = "white" {}
        _MilkyWayNoise("Milky Way Noise", 2D) = "white" {}
        [HDR]_MilkyWayColor1("Milky Way Color 1", Color) = (1, 1, 1, 1)
        [HDR]_MilkyWayColor2("Milky Way Color 2", Color) = (1, 1, 1, 1)
        _MilkyWayIntensity("Milky Way Intensity", Float) = 1
        _FlowSpeed("Flow Speed", Float) = 0.05
        _MoonCol ("Moon Color", Color) = (1, 1, 1, 1)
        _MoonIntensity("Moon Intensity", Range(1, 3)) = 1.5
        _MoonDirectionWS("Moon DirectionWS", Vector) = (1, 1, 1, 1)
        _StarIntensity("Star Intensity", Float) = 1
        [Toggle(_ENABLED_MOON)] _Enabled_Moon("Enabled Moon", Float) = 0
    }
    
    SubShader
    {
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "../ShaderLibrary/SkyBoxInput.hlsl"

        ENDHLSL

        Tags
        {
            "Queue" = "Background" 
            "RenderType" = "Background" 
//            "LightMode" = "CustomLit"
            "RenderPipeline" = "UniversalRenderPipeline" 
            "PreviewType"="Skybox"
        }
        
        Cull Off
        ZWrite Off
        
        LOD 100

        Pass
        {
            HLSLPROGRAM
           
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ _ENABLED_MOON
            // #pragma enable_d3d11_debug_symbols
            
            #include "../ShaderLibrary/ProceduralSkyBox.hlsl"
            ENDHLSL
        }
    }
}