#ifndef __SHADER_LIBRARY_SKY_BOX_INPUT_HLSL__
#define __SHADER_LIBRARY_SKY_BOX_INPUT_HLSL__


TEXTURE2D(_SkyGradientTex);
SAMPLER(sampler_SkyGradientTex);
TEXTURE2D(_StarTex);
SAMPLER(sampler_StarTex);
TEXTURE2D(_MoonTex);
SAMPLER(sampler_linear_clamp_MoonTex);
TEXTURE2D(_MilkyWayTex);
SAMPLER(sampler_MilkyWayTex);
TEXTURE2D(_MilkyWayNoise);
SAMPLER(sampler_MilkyWayNoise);

#define kRAYLEIGH (lerp(0.0, 0.0025, pow(_AtmosphereThickness,2.5)))
#define kSUN_BRIGHTNESS 20.0
#define kMIE 0.0010 

#define OUTER_RADIUS 1.025
static const float kOuterRadius = OUTER_RADIUS;
static const float kOuterRadius2 = OUTER_RADIUS*OUTER_RADIUS;
static const float kInnerRadius = 1.0;
static const float kInnerRadius2 = 1.0;
static const float kCameraHeight = 0.0001;
static const float kScaleOverScaleDepth = (1.0 / (OUTER_RADIUS - 1.0)) / 0.25;
static const float3 kDefaultScatteringWavelength = float3(.65, .57, .475);
static const float3 kVariableRangeForScatteringWavelength = float3(.15, .15, .15);
static const float kKmESun = kMIE * kSUN_BRIGHTNESS;
static const float kKm4PI = kMIE * 4.0 * 3.14159265;

CBUFFER_START(SKYBOXINPUT)
float4 _SunCol;
float4 _SunDirectionWS;
float4 _MoonDirectionWS;
float3 _GroundColor;
float4 _SkyTint;
float _Exposure;
float _SunSize;
float _SunIntensity;
float _ScatteringIntensity;
float _AtmosphereThickness;


float4x4 _MoonWorld2Obj;
float4 _MoonCol;
float _MoonIntensity;
float4x4 _MilkyWayWorld2Local;
float4 _MilkyWayTex_ST;
float4 _MilkyWayNoise_ST;
float4 _MilkyWayColor1;
float4 _MilkyWayColor2;
float _FlowSpeed;
float _StarIntensity;
float _MilkywayIntensity;

CBUFFER_END

#endif