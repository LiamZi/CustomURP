#ifndef __SHADER_LIBRARY_SKY_BOX_INPUT_2_HLSL__
#define __SHADER_LIBRARY_SKY_BOX_INPUT_2_HLSL__


#if defined(UNITY_COLORSPACE_GAMMA)
    #define GAMMA 2
    #define COLOR_2_GAMMA(color) color
    #define COLOR_2_LINEAR(color) color * color
    #define LINEAR_2_OUTPUT(color) sqrt(color)
#else
    #define GAMMA 2.2
    #define ColorSpaceDouble fixed4(4.59479380, 4.59479380, 4.59479380, 2.0)
    #define COLOR_2_GAMMA(color) ((ColorSpaceDouble.r>2.0) ? pow(color,1.0 / GAMMA) : color)
    #define COLOR_2_LINEAR(color) color
    #define LINEAR_2_LINEAR(color) color
#endif

static const float3 kDefaultScatteringWavelength = float3(.65, .57, .475);
static const float3 kVariableRangeForScatteringWavelength = float3(.15, .15, .15);

#define OUTER_RADIUS 1.025
static const float kOuterRadius = OUTER_RADIUS;
static const float kOuterRadius2 = OUTER_RADIUS*OUTER_RADIUS;
static const float kInnerRadius = 1.0;
static const float kInnerRadius2 = 1.0;
static const float kCameraHeight = 0.0001;

#define kRAYLEIGH (lerp(0.0, 0.0025, pow(_AtmosphereThickness,2.5))) 
#define kMIE 0.0010             
#define kSUN_BRIGHTNESS 20.0    

#define kMAX_SCATTER 50.0   

static const half kHDSundiskIntensityFactor = 15.0;
static const half kSimpleSundiskIntensityFactor = 27.0;

static const half kSunScale = 400.0 * kSUN_BRIGHTNESS;
static const float kKmESun = kMIE * kSUN_BRIGHTNESS;
static const float kKm4PI = kMIE * 4.0 * 3.14159265;
static const float kScale = 1.0 / (OUTER_RADIUS - 1.0);
static const float kScaleDepth = 0.25;
static const float kScaleOverScaleDepth = (1.0 / (OUTER_RADIUS - 1.0)) / 0.25;
static const float kSamples = 2.0; 

#define MIE_G (-0.990)
#define MIE_G2 0.9801

#define SKY_GROUND_THRESHOLD 0.02

#define SKYBOX_SUNDISK_NONE 0
#define SKYBOX_SUNDISK_SIMPLE 1
#define SKYBOX_SUNDISK_HQ 2

CBUFFER_START(SKYBOXINPUT)
half _Exposure;
half3 _GroundColor;
half _SunSize;
half _SunSizeConvergence;
half3 _SkyTint;
half3 _AtmosphereThickness;
CBUFFER_END


#endif