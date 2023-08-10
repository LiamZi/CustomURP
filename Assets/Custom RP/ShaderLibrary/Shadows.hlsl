#ifndef __SHADER_LIBRARY_SHADOWS_HLSL__
#define __SHADER_LIBRARY_SHADOWS_HLSL__

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);


CBUFFER_START(_CustomShadows)
    int _CasadeCount;
    float4 _CasadeCullingSpheres[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
CBUFFER_END

struct ShadowData
{
    int cascadeIndex;
};

struct DirectinalShadowData
{
    float strength;
    int tileIndex;
};

float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float GetDirectionalShadowAttenuation(DirectinalShadowData data, Surface surface)
{
    if(data.strength <= 0.0) return 1.0;

    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surface.position, 1.0)).xyz;
    float shadow = SampleDirectionalShadowAtlas(positionSTS);
    // return shadow;
    return lerp(1.0, shadow, data.strength);
}

ShadowData GetShadowData(Surface surface)
{
    ShadowData data;
    int i;
    for(i = 0; i < _CasadeCount; ++i)
    {
        float4 sphere = _CasadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surface.position, sphere.xyz);
        if(distanceSqr < sphere.w)
        {
            break;
        }

    }
    data.cascadeIndex = i;
    return data;
}


#endif