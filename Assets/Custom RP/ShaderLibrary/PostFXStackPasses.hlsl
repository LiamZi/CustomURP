#ifndef __SHADER_LIBRARY_POST_FX_STACK_PASSES_HLSL__
#define __SHADER_LIBRARY_POST_FX_STACK_PASSES_HLSL__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"


TEXTURE2D(_PostFXSource);
TEXTURE2D(_PostFXSource2);
SAMPLER(sampler_linear_clamp);

float4 _PostFXSource_TexelSize;

bool _BloomBicubicUpSampling;
float4 _BloomThreshold;
float _BloomIntensity;

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};  


float4 GetSource(float2 screenUV)
{
    // return SAMPLE_TEXTURE2D(_PostFXSource, sampler_linear_clamp, screenUV);
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource, sampler_linear_clamp, screenUV, 0);
}

float4 GetSource2(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource2, sampler_linear_clamp, screenUV, 0);
}

float4 GetSourceTexelSize()
{
    return _PostFXSource_TexelSize;
}

float4 GetSourceBicubic(float2 screenUV)
{
    return SampleTexture2DBicubic(TEXTURE2D_ARGS(_PostFXSource, sampler_linear_clamp), screenUV, _PostFXSource_TexelSize.zwxy, 1.0, 1.0);
}


Varyings DefaultPassVertex(uint vertexID : SV_VERTEXID)
{
    Varyings o;
    o.positionCS = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0 : -1.0, 0.0, 1.0); 
    o.screenUV = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);
    if(_ProjectionParams.x < 0.0)
    {
        o.screenUV.y = 1.0 - o.screenUV.y;
    }
    return o;
}

float4 CopyPassFragment(Varyings input) : SV_TARGET
{
    return GetSource(input.screenUV);
}

float4 BloomHorizontalPassFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    float offsets[] = { -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0 };

    float weights[] = { 0.01621622, 0.05405405, 0.12162162, 
                        0.19459459, 0.22702703, 0.19459459, 
                        0.12162162, 0.05405405, 0.01621622 };
    
    for(int i = 0; i < 9; ++i)
    {
        float offset = offsets[i] * 2.0 * GetSourceTexelSize().x;
        color += GetSource(input.screenUV + float2(offset, 0.0)).rgb * weights[i];
    }

    return float4(color, 1.0);
}

float4 BloomVerticalPassFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    // float offsets[] = { -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0 };
    float offsets[] = { -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923 };

    // float weights[] = { 0.01621622, 0.05405405, 0.12162162, 
    //                     0.19459459, 0.22702703, 0.19459459, 
    //                     0.12162162, 0.05405405, 0.01621622 };
    float weights[] = { 0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027 };
    
    for(int i = 0; i < 5; ++i)
    {
        float offset = offsets[i] * 2.0 * GetSourceTexelSize().y;
        color += GetSource(input.screenUV + float2(0.0, offset)).rgb * weights[i];
    }

    return float4(color, 1.0);
}

float4 BloomCombinePassFragment(Varyings input) : SV_TARGET
{
    // float3 lowRes = GetSourceBicubic(input.screenUV).rgb;
    float3 lowRes;
    if(_BloomBicubicUpSampling)
    {
        lowRes = GetSourceBicubic(input.screenUV).rgb;
    }
    else
    {
        lowRes = GetSource(input.screenUV).rgb;
    }
    float3 highRes = GetSource2(input.screenUV).rgb;
    return float4(lowRes * _BloomIntensity + highRes, 1.0);
}


// w=(max(s,b-t))/(max(b,0.00001))
// s=(min(max(0,b-t+tk),2tk)^2)/(4tk+0.00001)
float3 ApplyBloomThreshold(float3 color)
{
    float b = Max3(color.r, color.g, color.b);
    float s = b + _BloomThreshold.y;
    s = clamp(s, 0.0, _BloomThreshold.z);
    s = s * s * _BloomThreshold.w;

    float contribution = max(s, b - _BloomThreshold.x);
    contribution /= max(b, 0.00001);
    return color * contribution;
}

float4 BloomPrefilterPassFragment(Varyings input) : SV_TARGET
{
    float3 color = ApplyBloomThreshold(GetSource(input.screenUV).rgb);
    return float4(color, 1.0);
}

#endif