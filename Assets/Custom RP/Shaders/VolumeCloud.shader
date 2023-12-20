Shader "Custom RP/VolumeCloud"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
        _HeightDensity("Height Density", 2D) = "white" {}
        _CloudTex ("Cloud Tex", 3D) = "white" {}
        _CloudTile ("Cloud Tile", Float) = 3.0
        _DetailTex ("Detail Tex", 3D) = "white" {}
        _DetailTile("Detail Tile", Float) = 10.0
        _DetailStrength("Detail Strength", Float) = 0.2
        _CurlNoise("Curl Noise", 2D) = "white" {}
        _CurlTile("Curl Tile", Float) = 0.2
        _CurlStrength("Curl Strength", Float) = 1
        _CloudTopOffset("Cloud Top Offset", Float) = 100
        _CloudSize("Cloud Size", Float) = 50000
        
        _CloudStartHeight("Cloud Start Height", Float) = 2000
        _CloudEndHeight("Cloud End Height", Float) = 8000
        
        _CloudOverallDensity("Cloud Overall Density", Float) = 0.1
        _CloudTypeModifier("Cloud Type Modifier", Float) = 1.0
        _CloudCoverageModifier("Cloud Coverage Modifier", Float) = 1.0
        
        _WeatherTex("Weather Tex", 2D) = "white" {}
        _WeatherTexSize("Weather Tex Size", Float) = 50000
        _WindDirection("Wind Direction", Vector) = (1, 1, 0, 0)
        _SilverIntensity("Silver Intensity", Float) = 0.8
        _ScatteringCoefficient("Scattering Coefficient", Float) = 0.04
        _ExtinctionCoefficient("Extinction Coefficient", Float) = 0.04
        _MultiScatteringA("Multi Scattering A", FLoat) = 0.5
        _MultiScatteringB("Multi Scattering B", FLoat) = 0.5
        _MultiScatteringC("Multi Scattering C", FLoat) = 0.5
        _SilverSpread("Silver Spread", Float) = 0.75
        _AtmosphereColor("Atmosphere Color", Color) = (1, 1, 1, 1)
        _AmbientColor("Ambient Color", Color) = (1, 1, 1, 1)
        _AtmosphereColorSaturateDistance("Atmosphere Color Saturate Distance", Float) = 80000
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "PreviewType"="Sphere"
        }
        LOD 100
        Cull Off ZWrite Off ZTest Always
        
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "../ShaderLibrary/CloudInput.hlsl"
        ENDHLSL
        
        //Generator Cloud 
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ALLOW_CLOUD_FRONT_OBJECT
            #pragma multi_compile _ USE_HI_HEIGHT
            #pragma multi_compile LOW_QUALITY MEDIUM_QUALITY HIGH_QUALITY

            #include "../ShaderLibrary/VolumeCloudGenerator.hlsl"
            ENDHLSL
        }
        
        //Blend Cloud
        Pass
        {

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ALLOW_CLOUD_FRONT_OBJECT
			#pragma multi_compile LOW_QUALITY MEDIUM_QUALITY HIGH_QUALITY
            
            #include "../ShaderLibrary/BlendCloud.hlsl"
            ENDHLSL
        }


    }
}