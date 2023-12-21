#ifndef __SHADER_LIBRARY_BLEND_CLOUD_HLSL__
#define __SHADER_LIBRARY_BLEND_CLOUD_HLSL__

#include "../ShaderLibrary/CloudFunctions.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_TexelSize;

TEXTURE2D(_UndersamplerCloudTex);
SAMPLER(sampler_UndersamplerCloudTex);
float4 _UndersampleCloudTex_TexelSize;

float4x4 _PrevVP;

TEXTURE2D(_DownsampledDepth);
SAMPLER(sampler_DownsampledDepth);
float2 _TexelSize;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float2 vsray : TEXCOORD1;
    float4 screenPos : TEXCOORD2;
};

float2 PrevUV(float4 pos, out half outOfBound)
{
    float4 prevUV = mul(_PrevVP, pos);
    prevUV.xy = 0.5 * (prevUV.xy / prevUV.w) + 0.5;
    half oobmax = max(0.0 - prevUV.x, 0.0 - prevUV.y);
    half oobmin = min(prevUV.x - 1.0, prevUV.y - 1.0);
    outOfBound = step(0, max(oobmin, oobmax));
    return prevUV;
}

float4 ClipAABB(float4 aabbMin, float4 aabbMax, float4 prevSample)
{
    float4 pClip = 0.5 * (aabbMax + aabbMin);
    float4 eClip = 0.5 * (aabbMax - aabbMin);

    float4 vClip = prevSample - pClip;
    float4 vUnit = vClip - eClip;
    float4 aUnit = abs(vUnit);
    float maUnit = max(max(aUnit.x, max(aUnit.y, aUnit.z)), aUnit.w);

    if(maUnit > 1.0)
    {
        return pClip + vClip / maUnit;
    }
    else
    {
        return prevSample;
    }
}

Varyings vert(Attributes input)
{
    Varyings o;
    o.positionCS = TransformObjectToHClip(input.positionOS);
    o.uv = input.uv;
    o.vsray = (2.0 * input.uv - 1.0) * _ProjectionExtents.xy + _ProjectionExtents.zw;
    o.screenPos = ComputeScreenPos(input.positionOS, _ProjectionParams.x);
    return o;
}

float4 frag(Varyings input) : SV_Target
{
    float3 vspos = float3(input.vsray, 1.0);
    float4 worldPos = mul(unity_CameraToWorld, float4(vspos, 1.0f));
    worldPos /= worldPos.w;
    float4 raymarchResult = SAMPLE_TEXTURE2D(_UndersamplerCloudTex, sampler_UndersamplerCloudTex, input.uv);
    float distance = raymarchResult.y;
    float intensity = raymarchResult.x;
    half outOfBound;
    float2 prevUV = PrevUV(mul(unity_CameraToWorld, float4(normalize(vspos) * distance, 1.0)), outOfBound);
        
    {
        float4 prevSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, prevUV);
        float2 xoffset = float2(_UndersampleCloudTex_TexelSize.x, 0.0f);
        float2 yoffset = float2(0.0f, _UndersampleCloudTex_TexelSize.y);
        float4 m1 = 0.0f, m2 = 0.0f;

#if ALLOW_CLOUD_FRONT_OBJECT
        float originalPointDepth = LinearEyeDepth(_DownsampledDepth.Sample(sampler_DownsampledDepth, input.uv), _ZBufferParams);
        float validSampleCount = 1.0f;
#else
        float validSampleCount = 9.0f;
#endif
        
        UNITY_UNROLL
        for(int x = -1; x <= 1; x++)
        {
            UNITY_UNROLL
            for(int y = -1; y <= 1; y++)
            {
                float4 val;
                if(x == 0 && y == 0)
                {
                    val = raymarchResult;
                    m1 += val;
                    m2 += val * val;
                }
                else
                {
                    float2 uv = input.uv + float2(x * _UndersampleCloudTex_TexelSize.x , y * _UndersampleCloudTex_TexelSize.y);
                    val = SAMPLE_TEXTURE2D_LOD(_UndersamplerCloudTex, sampler_UndersamplerCloudTex, uv, 0.0);
#if ALLOW_CLOUD_FRONT_OBJECT
                    float depth = LinearEyeDepth(_DownsampledDepth.Sample(sampler_DownsampledDepth, uv), _ZBufferParams);
                    if(abs(originPointDepth - depth < 1.5f))
                    {
                        m1 += val;
                        m2 += val * val;
                        vaildSampleCount += 1.0f;
                    }
#else
                    m1 += val;
                    m2 += val * val;
#endif
                }
            }
        }

        float gamma = 1.0f;
        float4 mu = m1 / validSampleCount;
        float4 sigma = sqrt(abs(m2 / validSampleCount - mu * mu));
        float minc = mu - gamma * sigma;
        float maxc = mu + gamma * sigma;
        prevSample = ClipAABB(minc, maxc, prevSample);

        raymarchResult = lerp(prevSample, raymarchResult, max(0.05f , outOfBound));
    }
    return raymarchResult;
}

#endif