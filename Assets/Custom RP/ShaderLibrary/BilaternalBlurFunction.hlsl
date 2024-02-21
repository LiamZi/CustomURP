#ifndef __SHADER_LIBRARY_BILATERNAL_BLUR_FUNCTION_HLSL__
#define __SHADER_LIBRARY_BILATERNAL_BLUR_FUNCTION_HLSL__

#include "Fragment.hlsl"

#define DOWNSAMPLE_DEPTH_MODE 2
#define UPSAMPLE_DEPTH_THRESHOLD 1.5f
#define BLUR_DEPTH_FACTOR 0.5
#define GAUSS_BLUR_DEVIATION 1.5        
#define FULL_RES_BLUR_KERNEL_SIZE 7
#define HALF_RES_BLUR_KERNEL_SIZE 5
#define QUARTER_RES_BLUR_KERNEL_SIZE 6

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_HalfResDepthBuffer);
SAMPLER(sampler_HalfResDepthBuffer);
TEXTURE2D(_QuarterResDepthBuffer);
SAMPLER(sampler_QuarterResDepthBuffer);
TEXTURE2D(_HalfResColor);
SAMPLER(sampler_HalfResColor);
TEXTURE2D(_QuarterResColor);
SAMPLER(sampler_QuarterResColor);


float4 _HalfResDepthBuffer_TexelSize;
float4 _QuarterResDepthBuffer_TexelSize;


struct DownSample
{
#if SHADER_TARGET > 40
    float2 uv : TEXCOORD0;
#else
    float2 uv00 : TEXCOORD0;
    float2 uv01 : TEXCOORD1;
    float2 uv10 : TEXCOORD2;
    float2 uv11 : TEXCOORD3;
#endif
    float4 positionCS : SV_POSITION;
};

struct UpSample
{
    float2 uv : TEXCOORD0;
    float2 uv00 : TEXCOORD1;
    float2 uv01 : TEXCOORD2;
    float2 uv10 : TEXCOORD3;
    float2 uv11 : TEXCOORD4;
    float4 positionCS : SV_POSITION;
};


struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyins
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

DownSample vertDownSampleDepth(Attributes input, float2 texelSize)
{
    DownSample o;
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    o.positionCS = TransformWorldToHClip(positionWS);
#if SHADER_TARGET > 40
    o.uv = input.uv;
#else
    o.uv00 = input.uv - 0.5 * texelSize.xy;
    o.uv10 = o.uv00 + float2(texelSize.x , 0);
    o.uv01 = o.uv00 + float2(0, texelSize.y);
    o.uv11 = o.uv00 + texelSize.xy;
#endif
    return o;
}

UpSample vertUpSample(Attributes input, float2 texelSize)
{
    UpSample o;
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    o.positionCS = TransformWorldToHClip(positionWS);
    o.uv = input.uv;

    o.uv00 = input.uv - 0.5 * texelSize.xy;
    o.uv10 = o.uv00 + float2(texelSize.x, 0);
    o.uv01 = o.uv00 + float2(0, texelSize.y);
    o.uv11 = o.uv00 + texelSize.xy;
    return o;
}

float4 BilateralUpSample(UpSample input, Texture2D hiDepth, Texture2D lowDepth,
                    Texture2D lowColor, SamplerState linearSampler, SamplerState pointSampler)
{
    const float threshold = UPSAMPLE_DEPTH_THRESHOLD;
    float4 hightResDepth = LinearEyeDepth(hiDepth.Sample(pointSampler, input.uv), _ZBufferParams).xxxx;
    float4 lowResDepth;

    lowResDepth[0] = LinearEyeDepth(lowDepth.Sample(pointSampler, input.uv00), _ZBufferParams);
    lowResDepth[1] = LinearEyeDepth(lowDepth.Sample(pointSampler, input.uv10), _ZBufferParams);
    lowResDepth[2] = LinearEyeDepth(lowDepth.Sample(pointSampler, input.uv01), _ZBufferParams);
    lowResDepth[3] = LinearEyeDepth(lowDepth.Sample(pointSampler, input.uv11), _ZBufferParams);
    
    float4 depthDiff = abs(lowResDepth - hightResDepth);
    float accumDiff = dot(depthDiff, float4(1, 1, 1, 1));

    UNITY_BRANCH
    if(accumDiff < threshold)
    {
        return lowColor.Sample(linearSampler, input.uv);
    }

    float minDepthDiff = depthDiff[0];
    float2 nearestUV = input.uv00;
    
    if(depthDiff[1] < minDepthDiff)
    {
        nearestUV = input.uv10;
        minDepthDiff = depthDiff[1];
    }

    if(depthDiff[2] < minDepthDiff)
    {
        nearestUV = input.uv01;
        minDepthDiff = depthDiff[2];
    }

    if(depthDiff[3] < minDepthDiff)
    {
        nearestUV = input.uv11;
        minDepthDiff = depthDiff[3];
    }

    return lowColor.Sample(pointSampler, nearestUV);
}

float DownSampleDepth(DownSample input, Texture2D depthTexture, SamplerState depthSampler)
{
#if SHADER_TARGET > 40
    float4 depth = depthTexture.Gather(depthSampler, input.uv);
#else
    float4 depth;
    depth.x = depthTexture.Sample(depthSampler, input.uv00).x;
    depth.y = depthTexture.Sample(depthSampler, input.uv01).x;
    depth.z = depthTexture.Sample(depthSampler, input.uv10).x;
    depth.w = depthTexture.Sample(depthSampler, input.uv11).x;
#endif

#if DOWNSAMPLE_DEPTH_MODE == 0
    return min(min(depth.x, depth.y), min(dpeth.z, depth.w));
#elif DOWNSAMPLE_DEPTH_MODE == 1
    return max(max(depth.x, depth.y), max(depth.z, depth.w));
#elif DOWNSAMPLE_DEPTH_MODE == 2
    float minDepth = min(min(depth.x, depth.y), min(depth.z, depth.w));
    float maxDepth = max(max(depth.x, depth.y), max(depth.z, depth.w));

    int2 position = input.positionCS.xy % 2;
    int index = position.x + position.y;
    return index == 1 ? minDepth : maxDepth;
#endif
}

float GaussianWeight(float offset, float deviation)
{
    float weight = 1.f / sqrt(2.0f * PI * deviation * deviation);
    weight *= exp(-(offset * offset) / (2.0f * deviation *deviation));
    return weight;
}


float4 BilateralBlur(Varyins input, int2 direction, Texture2D depth,
                    SamplerState depthSampler, const int kernelRadius, float2 pixelSize)
{
    const float deviation = kernelRadius / GAUSS_BLUR_DEVIATION;
    float2 uv = input.uv;
    float4 centerCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    float3 color = centerCol.xyz;
    float centerDepth = LinearEyeDepth(SAMPLE_TEXTURE2D(depth, depthSampler, input.uv), _ZBufferParams);
    
    float weightSum = 0;
    float weight = GaussianWeight(0, deviation);
    color *= weight;
    weightSum += weight;

    UNITY_UNROLL
    for(int i = -kernelRadius; i < 0; i += 1)
    {
        float2 offset = direction * i;
        float3 sampleColor = _MainTex.Sample(sampler_MainTex, input.uv, offset);
        float sampleDepth = LinearEyeDepth(depth.Sample(depthSampler, input.uv, offset), _ZBufferParams);

        float depthDiff = abs(centerDepth - sampleDepth);
        float dFactor = depthDiff * BLUR_DEPTH_FACTOR;
        float w = exp(-(dFactor * dFactor));

        weight += GaussianWeight(i, deviation) * w;

        color += weight * sampleColor;
        weightSum += weight;
    }

    UNITY_UNROLL
    for(int i = 1; i <= kernelRadius; i += 1)
    {
        float2 offset = direction * i;
        float3 sampleColor = _MainTex.Sample(sampler_MainTex, input.uv, offset);
        float sampleDepth = LinearEyeDepth(depth.Sample(depthSampler, input.uv, offset), _ZBufferParams);
        float depthDiff = abs(centerDepth - sampleDepth);
        float dFactor = depthDiff * BLUR_DEPTH_FACTOR;
        float w = exp(-(dFactor * dFactor));

        weight = GaussianWeight(i , deviation) * w;
        color += weight * sampleColor;
        weightSum += weight;
    }

    color /= weightSum;
    return float4(color, centerCol.w);
}

#endif