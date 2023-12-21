#ifndef __SHADER_LIBRARY_CLOUD_INPUT_HLSL__
#define __SHADER_LIBRARY_CLOUD_INPUT_HLSL__




// static const float bayerOffsets[3][3] = {
//     {0, 7, 3},
//     {6, 5, 2},
//     {4, 1, 8}
// };

// TEXTURE3D(_CloudTex);
// SAMPLER(sampler_CloudTex);
// TEXTURE3D(_DetailTex);
// SAMPLER(sampler_DetailTex);
// TEXTURE2D(_CurlNoise);
// SAMPLER(sampler_CurlNoise);
// TEXTURE2D(_WeatherTex);
// SAMPLER(sampler_WeatherTex);
// TEXTURE2D(_HeightDensity);
// SAMPLER(sampler_HeightDensity);

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
// TEXTURE2D(_CameraDepthTexture);
// SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_BlueNoiseTex);
SAMPLER(sampler_BlueNoiseTex);

TEXTURE3D(_BaseShapeTex);
SAMPLER(sampler_BaseShapeTex);
TEXTURE3D(_DetailShapeTex);
SAMPLER(sampler_DetailShapeTex);
TEXTURE2D(_WeatherTex);  //r 密度, g 吸收率, b 云类型(0~1 => 层云~积云) , 当渲染模式为No3DTex时， r 密度， g 吸收率， b云层底部高度 ， a云层顶部高度
SAMPLER(sampler_WeatherTex);


CBUFFER_START(CLOUDINPUT)
float _WeatherTexTiling;
float2 _WeatherTexOffset;
// float _WeatherTexRepair;
float _BaseShapeTexTiling;
float _BaseShapeDetailEffect;
float3 _BaseShapeRatio;

float _DetailShapeTexTiling;
float _DetailEffect;
        
float4 _CloudHeightRange;
float4 _StratusRange;
float _StratusFeather;
float4 _CumulusRange;
float _CumulusFeather;
float _CloudCover;
float _CloudOffsetLower;
float _CloudOffsetUpper;
float _CloudFeather;
        
float _SDFScale;
float _ShapeMarchLength;
int _ShapeMarchMax;
        
float _BlueNoiseEffect;
float3 _WindDirecton;
float _WindSpeed;
float _DensityScale;
        
float _CloudAbsorb;
float _ScatterForward;
float _ScatterForwardIntensity;
float _ScatterBackward;
float _ScatterBackwardIntensity;
float _ScatterBase;
float _ScatterMultiply;
        
half4 _ColorBright;
half4 _ColorCentral;
half4 _ColorDark;
float _ColorCentralOffset;
float _DarknessThreshold;
        
int _LightingMarchMax;

float2 _BlueNoiseTexUV;
int _FrameCount;
int _Width;
int _Height;
        
float3 _BoundBoxMin;
float3 _BoundBoxMax;
float4 _MainLightDirection;
float3 _MainLightColor;

CBUFFER_END


#endif