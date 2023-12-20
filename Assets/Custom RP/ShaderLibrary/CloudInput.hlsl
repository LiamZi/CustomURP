#ifndef __SHADER_LIBRARY_CLOUD_INPUT_HLSL__
#define __SHADER_LIBRARY_CLOUD_INPUT_HLSL__




static const float bayerOffsets[3][3] = {
    {0, 7, 3},
    {6, 5, 2},
    {4, 1, 8}
};

TEXTURE3D(_CloudTex);
SAMPLER(sampler_CloudTex);
TEXTURE3D(_DetailTex);
SAMPLER(sampler_DetailTex);
TEXTURE2D(_CurlNoise);
SAMPLER(sampler_CurlNoise);
TEXTURE2D(_WeatherTex);
SAMPLER(sampler_WeatherTex);
TEXTURE2D(_HeightDensity);
SAMPLER(sampler_HeightDensity);


CBUFFER_START(CLOUDINPUT)
float _CloudStartHeight;
float _CloudEndHeight;
float _CloudTile;
float _DetailTile;
float _DetailStrength;
float _CurlTile;
float _CurlStrength;
float _CloudTopOffset;
float _CloudSize;
float _CloudOverallDensity;
float _CloudCoverageModifier;
float _CloudTypeModifier;
half4 _WindDirection;
float _WeatherTexSize;

float _ScatteringCoefficient;
float _ExtinctionCoefficient;
float _SilverIntensity;
float _SilverSpread;

float4 _ProjectionExtents;

float _MultiScatteringA;
float _MultiScatteringB;
float _MultiScatteringC;
float4 _WorldLightPos;

CBUFFER_END


#endif