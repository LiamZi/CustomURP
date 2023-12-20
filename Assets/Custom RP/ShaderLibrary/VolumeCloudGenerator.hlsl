#ifndef __SHADER_LIBRARY_VOLUME_CLOUD_HLSL__
#define __SHADER_LIBRARY_VOLUME_CLOUD_HLSL__

#if defined(USE_HI_HEIGHT)
#include "CloudHierarchicalRaymarch.hlsl"
#else
#include "CloudNormalRayMarch.hlsl"
#endif

#if defined(HIGH_QUALITY)
#define MIN_SAMPLE_COUNT 32
#define MAX_SAMPLE_COUNT 32
#elif defined(MEDIUM_QUALITY)
#define MIN_SAMPLE_COUNT 24
#define MAX_SAMPLE_COUNT 24
#else
#define MIN_SAMPLE_COUNT 16
#define MAX_SAMPLE_COUNT 16
#endif

TEXTURE2D(_DownsampledDepth);
SAMPLER(sampler_DownsampledDepth);
float _RaymarchOffset;
float2 _TexelSize;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};


struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 screenPos : TEXCOORD0;
    float2 vsray : TEXCOORD1;
};

Varyings vert(Attributes input)
{
    Varyings o;
    o.positionCS = TransformObjectToHClip(input.positionOS);
    input.positionOS.z = 0.5;
    o.screenPos = ComputeScreenPos(o.positionCS, _ProjectionParams.x);
    o.vsray = (2.0 * input.uv - 1.0) * _ProjectionExtents.xy + _ProjectionExtents.zw;

    return o;
}

float4 frag(Varyings input) : SV_Target
{
    float3 vspos = float3(input.vsray, 1.0);
    float4 worldPos = mul(unity_CameraToWorld, float4(vspos, 1.0));
    worldPos /= worldPos.w;
    
    float sceneDepth = Linear01Depth(_DownsampledDepth.Sample(sampler_DownsampledDepth, (input.screenPos / input.screenPos.w).xy), _ZBufferParams);
    float raymarchEnd = GetRaymarchEndFromSceneDepth(sceneDepth);
    float3 viewDir = normalize(worldPos.xyz - _WorldSpaceCameraPos);
    int sampleCount = lerp(MAX_SAMPLE_COUNT, MIN_SAMPLE_COUNT, abs(viewDir.y));
    float2 screenPos = input.screenPos.xy / input.screenPos.w;
    int2 texelID = int2(fmod(screenPos / _TexelSize, 3.0));

    float bayerOffset = (bayerOffsets[texelID.x][texelID.y]) / 9.0f;
    float offset = -fmod(_RaymarchOffset + bayerOffset, 1.0f);

    float intensity, distance;

 
#if defined(USE_HI_HEIGHT)
    int iterationCount;
    float density = HierarchicalRaymarch(worldPos, viewDir,raymarchEnd, sampleCount, offset, _WorldLightPos, intensity, distance, iterationCount);
#else
    float density = GetDensity(worldPos, viewDir, raymarchEnd, sampleCount, offset, _WorldLightPos, intensity, distance);
#endif
    
    return float4(intensity, distance, 1.0f, density);
}


#endif