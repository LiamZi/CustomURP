#ifndef __SHADER_LIBRARY_SHADOWS_HLSL__
#define __SHADER_LIBRARY_SHADOWS_HLSL__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif


#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);


CBUFFER_START(_CustomShadows)
    int _CascadCount;
    float4 _ShadowAtlasSize;
    float4 _ShadowDistanceFade;
    float4 _CascadCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadData[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
CBUFFER_END

struct ShadowData
{
    int cascadeIndex;
    float cascadBlend;
    float strength;
};

struct DirectinalShadowData
{
    float strength;
    int tileIndex;
    float normalBias;
};



float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterDirectionalShadow(float3 positionSTS)
{
#if defined(DIRECTIONAL_FILTER_SETUP)
    float weights[DIRECTIONAL_FILTER_SAMPLES];
    float2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.yyxx;
    DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;

    for(int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; ++i)
    {
        shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
    }

    return shadow;
#else
    return SampleDirectionalShadowAtlas(positionSTS);
#endif
}

float GetDirectionalShadowAttenuation(DirectinalShadowData data, ShadowData global, Surface surface)
{
    if(data.strength <= 0.0) return 1.0;

    float3 normalBias = surface.normal * (data.normalBias * _CascadData[global.cascadeIndex].y);

    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surface.position + normalBias, 1.0)).xyz;
    // float shadow = SampleDirectionalShadowAtlas(positionSTS);
    float shadow = FilterDirectionalShadow(positionSTS);
    if(global.cascadBlend < 1.0)
    {
        normalBias = surface.normal * (data.normalBias * _CascadData[global.cascadeIndex + 1].y);
        positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex + 1], float4(surface.position + normalBias, 1.0)).xyz;
        shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadBlend);
    }
    // return shadow;
    return lerp(1.0, shadow, data.strength);
}

float FadeShadowStrength(float distance, float scale, float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}


ShadowData GetShadowData(Surface surface)
{
    ShadowData data;
    // data.strength = surface.depth < _ShadowDistance ? 1.0 : 0.0;
    data.cascadBlend = 1.0;
    data.strength = FadeShadowStrength(surface.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
    int i;
    for(i = 0; i < _CascadCount; ++i)
    {
        float4 sphere = _CascadCullingSpheres[i];
        float distanceSqr = DistanceSquared(surface.position, sphere.xyz);
        if(distanceSqr < sphere.w)
        {
            float fade = FadeShadowStrength(distanceSqr, _CascadData[i].x, _ShadowDistanceFade.z);
            if(i == _CascadCount - 1)
            {
                // data.strength *= FadeShadowStrength(distanceSqr, 1.0 / sphere.w, _ShadowDistanceFade.z);
                // data.strength *= FadeShadowStrength(distanceSqr, _CascadData[i].x, _ShadowDistanceFade.z);
                data.strength *= fade;
            }
            else
            {
                data.cascadBlend = fade;
            }
            break;
        }
    }

    if(i == _CascadCount)
    {
        data.strength = 0.0;
    }
#if defined(_CASCADE_BLEND_DITHER)
    else if(data.cascadBlend < surface.dither)
    {
        i += 1;
    }
#endif

#if !defined(_CASCADE_BLEND_SOFT)
    data.cascadBlend = 1.0;
#endif

    data.cascadeIndex = i;
    return data;
}


#endif