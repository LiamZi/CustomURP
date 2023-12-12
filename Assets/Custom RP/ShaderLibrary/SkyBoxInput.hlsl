#ifndef __SHADER_LIBRARY_SKY_BOX_INPUT_HLSL__
#define __SHADER_LIBRARY_SKY_BOX_INPUT_HLSL__


TEXTURE2D(_SkyGradientTex);
SAMPLER(sampler_SkyGradientTex);
TEXTURE2D(_StarTex);
SAMPLER(sampler_StarTex);
TEXTURE2D(_MoonTex);
SAMPLER(sampler_MoonTex);
TEXTURE2D(_MilkyWayTex);
SAMPLER(sampler_MilkyWayTex);
TEXTURE2D(_MilkyWayNoise);
SAMPLER(sampler_MilkyWayNoise);


CBUFFER_START(SKYBOXINPUT)
float4 _SunColor;
float4 _SunDirectionWS;
float4 _MoonDirectionWS;
float _SunSize;
float _SunIntensity;
float _ScatteringIntensity;
float4x4 _MoonWorld2Obj;
float4 _MoonColor;
float _MoonIntensity;
float4x4 _MilkyWayWorld2Local;
float4 _MilkyWayTex_ST;
float4 _MilkyWayNoise_ST;
float4 _MilkyWayCol1;
float4 _MilkyWayCol2;
float _FlowSpeed;
float _StarIntensity;
float _MilkywayIntensity;

CBUFFER_END

#endif