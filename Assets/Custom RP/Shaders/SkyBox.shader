Shader "Custom RP/SkyBox"
{
    Properties
    {
        _SunSize ("Sun Size", Range(0, 1)) = 0.05
        _SunIntensity("Sun Intensity", Float) = 2
        _SunColor("Sun Color", Color) = (1, 1, 1, 1)
        _SunDirectionWS("Sun DirectionWS", Vector) = (1, 1, 1, 1)
        _ScatteringIntensity("Scattering Intensity", Float) = 1
        _StarTex("Star Texture", 2D) = "white" {}
        _MilkyWayTex("Milky Way Texture", 2D) = "white" {}
        _MilkyWayNoise("Milky Way Noise", 2D) = "white" {}
        [HDR]_MilkyWayColor1("Milky Way Color 1", Color) = (1, 1, 1, 1)
        [HDR]_MilkyWayColor2("Milky Way Color 2", Color) = (1, 1, 1, 1)
        _MilkyWayIntensity("Milky Way Intensity", Float) = 1
        _FlowSpeed("Flow Speed", Float) = 0.05
        _MoonColor("Moon Color", Color) = (1, 1, 1, 1)
        _MoonIntensity("Moon Intensity", Range(1, 3)) = 1.5
        _MoonDirectionWS("Moon DirectionWS", Vector) = (1, 1, 1, 1)
        _StarIntensity("Star Intensity", Float) = 1
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
            
            #include "../ShaderLibrary/ProceduralSkyBox.hlsl"
            ENDHLSL
        }
    }
}