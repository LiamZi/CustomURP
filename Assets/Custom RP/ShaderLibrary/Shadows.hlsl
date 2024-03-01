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



#if defined(_OTHER_PCF3)
    #define OTHER_FILTER_SAMPLES 4
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3 
#elif defined(_OTHER_PCF5)
    #define OTHER_FILTER_SAMPLES 9
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_OTHER_PCF7)
    #define OTHER_FILTER_SAMPLES 16
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_SHADOWED_OTHER_LIGHT_COUNT 16
#define MAX_CASCADE_COUNT 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
TEXTURE2D_SHADOW(_OtherShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);


CBUFFER_START(_CustomShadows)
    int _CascadCount;
    float4 _ShadowAtlasSize;
    float4 _ShadowDistanceFade;
    float4 _CascadCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadData[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float4x4 _OtherShadowMatrices[MAX_SHADOWED_OTHER_LIGHT_COUNT];
    float4 _OtherShadowTiles[MAX_SHADOWED_OTHER_LIGHT_COUNT];
    float _shadowSplitSqRadii[MAX_CASCADE_COUNT];
CBUFFER_END

struct ShadowMask
{
    bool always;
    bool distance;
    float4 shadows;
};

struct ShadowData
{
    int cascadeIndex;
    float cascadBlend;
    float strength;
    ShadowMask shadowMask;
};

struct DirectinalShadowData
{
    float strength;
    int tileIndex;
    float normalBias;
    int shadowMaskChannel;
};

struct OtherShadowData
{
    float strength;
    int tileIndex;
    bool isPoint;
    int shadowMaskChannel;
    float3 lightPositionWS;
    float3 spotDirectionWS;
    float3 lightDirectionWS;
};


static const float3 pointShadowPlanes[6] = 
{
    float3(-1.0, 0.0, 0.0),
    float3(1.0, 0.0, 0.0),
    float3(0.0, -1.0, 0.0),
    float3(0.0, 1.0, 0.0),
    float3(0.0, 0.0, -1.0),
    float3(0.0, 0.0, 1.0)
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

float GetCascadeShadow(DirectinalShadowData data, ShadowData global, Surface surface)
{
    // float3 normalBias = surface.normal * (data.normalBias * _CascadData[global.cascadeIndex].y);
    float3 normalBias = surface.interpolatedNormal * (data.normalBias * _CascadData[global.cascadeIndex].y);


    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surface.position + normalBias, 1.0)).xyz;
    // float shadow = SampleDirectionalShadowAtlas(positionSTS);
    float shadow = FilterDirectionalShadow(positionSTS);
    if(global.cascadBlend < 1.0)
    {
        // normalBias = surface.normal * (data.normalBias * _CascadData[global.cascadeIndex + 1].y);
        normalBias = surface.interpolatedNormal * (data.normalBias * _CascadData[global.cascadeIndex + 1].y);

        positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex + 1], float4(surface.position + normalBias, 1.0)).xyz;
        shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadBlend);
    }

    return shadow;
}

float GetBakedShadow(ShadowMask mask, int channel)
{
    float shadow = 1.0;
    if(mask.distance || mask.always)
    {
        if(channel >= 0)
        {
             shadow = mask.shadows[channel];
        }
       
        // shadow = lerp(1.0, GetBakedShadow(mask), strength);
    }

    return shadow;
}

float GetBakedShadow(ShadowMask mask, int channel, float strength)
{
    if(mask.distance || mask.always)
    {
        return lerp(1.0, GetBakedShadow(mask, channel), strength);
    }

    return 1.0;
}


float MixBakedAndRealtimeShadows(ShadowData global, float shadow, int shadowMaskChannel, float strength)
{
	float baked = GetBakedShadow(global.shadowMask, shadowMaskChannel);
    if(global.shadowMask.always)
    {
        shadow = lerp(1.0, shadow, global.strength);
        shadow = min(baked, shadow);
        return lerp(1.0, shadow, strength);
    }

	if (global.shadowMask.distance) 
    {
		shadow = lerp(baked, shadow, global.strength);
		return lerp(1.0, shadow, strength);
        // shadow = baked;
        // shadow = lerp(baked, shadow, global.strength);
	}

	return lerp(1.0, shadow, strength * global.strength);
    // return lerp(1.0, shadow, strength);
}

float GetDirectionalShadowAttenuation(DirectinalShadowData data, ShadowData global, Surface surface)
{
#if !defined(_RECEIVE_SHADOWS)
		return 1.0;
#endif


    float shadow = 0.0;
    // float shadow;
    if(data.strength * global.strength <= 0.0) 
    {
        // return shadow;
        shadow = GetBakedShadow(global.shadowMask, data.shadowMaskChannel, abs(data.strength));
    }
    else
    {
        shadow = GetCascadeShadow(data, global, surface);
        shadow = MixBakedAndRealtimeShadows(global, shadow, data.shadowMaskChannel, data.strength);
        // shadow = lerp(1.0, shadow, data.strength);
    }

    return shadow;
    // return 1.0;
}


float SampleOtherShadowAtlas(float3 positionSTS, float3 bounds)
{
    positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
    return SAMPLE_TEXTURE2D_SHADOW(_OtherShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterOtherShadow(float3 positionSTS, float3 bounds)
{
#if defined(OTHER_FILTER_SETUP)
    float weights[OTHER_FILTER_SAMPLES];
    float2 positions[OTHER_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.wwzz;
    OTHER_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;

    for(int i = 0; i < OTHER_FILTER_SAMPLES; ++i)
    {
        shadow += weights[i] * SampleOtherShadowAtlas(float3(positions[i].xy, positionSTS.z), bounds);
    }

    return shadow;
#else
    return SampleOtherShadowAtlas(positionSTS, bounds);
#endif
}

float GetOtherShadow(OtherShadowData other, ShadowData global, Surface surface)
{
    float tileIndex = other.tileIndex;
    float3 lightPlane = other.spotDirectionWS;
    if(other.isPoint)
    {
        float faceOffset = CubeMapFaceID(-other.lightDirectionWS);
        tileIndex += faceOffset;
        lightPlane = pointShadowPlanes[faceOffset];
    }

    float4 tileData = _OtherShadowTiles[tileIndex];
    float3 surfaceToLight = other.lightPositionWS - surface.position;
    float distanceToLightPlane = dot(surfaceToLight, lightPlane);

    float3 normalBias = surface.interpolatedNormal * (distanceToLightPlane * tileData.w);
    float4 positionSTS = mul(_OtherShadowMatrices[tileIndex], float4(surface.position + normalBias, 1.0));
    // return 1.0;
    return FilterOtherShadow(positionSTS.xyz / positionSTS.w, tileData.xyz);
}

float GetOtherShadowAttenuation(OtherShadowData data, ShadowData global, Surface surface)
{
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif

    float shadow;
    if(data.strength * global.strength <= 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask, data.shadowMaskChannel, abs(data.strength));
    }
    else
    {
        // shadow = 1.0;
        shadow = GetOtherShadow(data, global, surface);
        shadow = MixBakedAndRealtimeShadows(global, shadow, data.shadowMaskChannel, data.strength);
    }

    return shadow;
}

float FadeShadowStrength(float distance, float scale, float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}




ShadowData GetShadowData(Surface surface)
{
//     ShadowData data;
//     data.shadowMask.always = false;
//     data.shadowMask.distance = false;
//     data.shadowMask.shadows = 1.0;
//     // data.strength = surface.depth < _ShadowDistance ? 1.0 : 0.0;
//     data.cascadBlend = 1.0;
//     data.strength = FadeShadowStrength(surface.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
//     int i;
//     for(i = 0; i < _CascadCount; ++i)
//     {
//         float4 sphere = _CascadCullingSpheres[i];
//         float distanceSqr = DistanceSquared(surface.position, sphere.xyz);
//         if(distanceSqr < sphere.w)
//         {
//             float fade = FadeShadowStrength(distanceSqr, _CascadData[i].x, _ShadowDistanceFade.z);
//             // if(i == _CascadCount && _CascadCount > 0)
//             if(i == _CascadCount - 1)
//             {
//                 // data.strength *= FadeShadowStrength(distanceSqr, 1.0 / sphere.w, _ShadowDistanceFade.z);
//                 // data.strength *= FadeShadowStrength(distanceSqr, _CascadData[i].x, _ShadowDistanceFade.z);
//                 data.strength *= fade;
//                 // data.strength = 0.0;
//             }
//             else
//             {
//                 data.cascadBlend = fade;
//             }
//             break;
//         }
//     }

//     if(i == _CascadCount && _CascadCount > 0)
//     {
//         data.strength = 0.0;
//     }
// #if defined(_CASCADE_BLEND_DITHER)
//     else if(data.cascadBlend < surface.dither)
//     {
//         i += 1;
//     }
// #endif

// #if !defined(_CASCADE_BLEND_SOFT)
//     data.cascadBlend = 1.0;
// #endif

//     data.cascadeIndex = i;
//     return data;

    ShadowData data;
	data.shadowMask.always = false;
	data.shadowMask.distance = false;
	data.shadowMask.shadows = 1.0;
	data.cascadBlend = 1.0;
	data.strength = FadeShadowStrength(
		surface.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y
	);
	int i;
	for (i = 0; i < _CascadCount; i++) {
		float4 sphere = _CascadCullingSpheres[i];
		float distanceSqr = DistanceSquared(surface.position, sphere.xyz);
		if (distanceSqr < sphere.w) {
			float fade = FadeShadowStrength(
				distanceSqr, _CascadData[i].x, _ShadowDistanceFade.z
			);
			if (i == _CascadCount - 1) {
				data.strength *= fade;
			}
			else {
				data.cascadBlend = fade;
			}
			break;
		}
	}
	
	if (i == _CascadCount && _CascadCount > 0) {
		data.strength = 0.0;
	}
	#if defined(_CASCADE_BLEND_DITHER)
		else if (data.cascadBlend < surface.dither) {
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