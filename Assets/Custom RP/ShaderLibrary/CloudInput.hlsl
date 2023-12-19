#ifndef __SHADER_LIBRARY_CLOUD_INPUT_HLSL__
#define __SHADER_LIBRARY_CLOUD_INPUT_HLSL__


#define EARTH_RADIUS 6371000.0
#define EARTH_CENTER float3(0, -EARTH_RADIUS, 0)
#define TRANSMITTANCE_SAMPLE_STEP 512.0f

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

float _ScatteringCoefficient;
float _ExtinctionCoefficient;
float _SilverIntensity;
float _SilverSpread;

CBUFFER_END


#endif