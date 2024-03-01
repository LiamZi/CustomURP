#ifndef __SHADER_LIBRARY_VOLUMETRIC_LIGHT_INPUT_HLSL__
#define __SHADER_LIBRARY_VOLUMETRIC_LIGHT_INPUT_HLSL__

#include "Surface.hlsl"
#include "Shadows.hlsl"
#if defined(USE_CLUSTER_LIGHT)
#include "ClusterLight.hlsl"
#else
#include "Light.hlsl"
#endif

#include "BRDF.hlsl"
#include "GI.hlsl"
#include "Lighting.hlsl"



CBUFFER_START(VolmetricLight)
float4x4 _WorldViewProj;
float4x4 _MyLightMatrix0;
float4x4 _MyWorld2Shadow;
float4 _FrustumCorners[4];
float4 _VolumetricLight;
float4 _MieG;
float4 _NoiseData;
float4 _NoiseVelocity;
float4 _HeightFog;
float3 _CameraForward;
float _MaxRayLength;
int _SampleCount;
CBUFFER_END

TEXTURE3D(_NoiseTexture);
SAMPLER(sampler_NoiseTexture);
TEXTURE2D(_DitherTexture);
SAMPLER(sampler_DitherTexture);

float GetDensity(float3 pos)
{
    float density = 1;
    
#if defined(_NOISE)
    float noiseUV = frac(pos * _NoiseData.x + float3(_Time.y * _NoiseVelocity.x, 0, _Time.y * _NoiseVelocity.y));
    float noise = SAMPLE_TEXTURE3D(_NoiseTexture, sampler_NoiseTexture, noiseUV);
    noise = saturate(noise - _NoiseData.z) * _NoiseData.y;
    density = saturate(noise);
#endif

    //TODO: use the fog
    // ApplyHeightFog(pos, density);

    return density;
}

float MieScattering(float angle, float4 g)
{
    return g.w * (g.x / (pow(g.y - g.z * angle, 1.5)));
}

inline half4 GetCascadeWeightsSplitSpheres(float3 pos)
{
    float3 fromCenter0 = pos.xyz - _CascadCullingSpheres[0].xyz;
    float3 fromCenter1 = pos.xyz - _CascadCullingSpheres[1].xyz;
    float3 fromCenter2 = pos.xyz - _CascadCullingSpheres[2].xyz;
    float3 fromCenter3 = pos.xyz - _CascadCullingSpheres[3].xyz;

    float4 distances = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    float4 splitSqRadii = float4(_shadowSplitSqRadii[0], _shadowSplitSqRadii[1], _shadowSplitSqRadii[2], _shadowSplitSqRadii[3]);
    
    half4 weights = float4(distances < splitSqRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);
    return weights;
}

inline float4 GetCascadeShadowCoord(float4 pos, half4 weights)
{
    float3 sc0 = mul(_DirectionalShadowMatrices[0], pos).xyz;
    float3 sc1 = mul(_DirectionalShadowMatrices[1], pos).xyz;
    float3 sc2 = mul(_DirectionalShadowMatrices[2], pos).xyz;
    float3 sc3 = mul(_DirectionalShadowMatrices[3], pos).xyz;

    float4 smCoordinate = float4(sc0 * weights[0] + sc1 * weights[1] + sc2 * weights[2] + sc3 * weights[3], 1.0);
#if defined(UNITY_REVERSED_Z)
    float  noCascadeWeights = 1 - dot(weights, float4(1, 1, 1, 1));
    smCoordinate.z += noCascadeWeights;
#endif
    return smCoordinate;
}

float GetLightAttenuation(float3 pos)
{
    float atten = 1;
    float4 cascadeWeights = GetCascadeWeightsSplitSpheres(pos);
    // atten = cascadeWeights.g;
    bool inside = dot(cascadeWeights, float4(1, 1, 1, 1)) < 4;
    float4 samplePos = GetCascadeShadowCoord(float4(pos, 1), cascadeWeights);
    atten = inside ? SampleDirectionalShadowAtlas(samplePos.xyz) : 1.0f;
    // atten = _directionalLightShadowData[0].r + atten * (1 - _directionalLightShadowData[0].r);
    return atten;
}


float4 RayMarch(float2 screenPos, float3 rayStart, float3 rayDir, float rayLength)
{
    float2 interleavedPos = (fmod(floor(screenPos.xy), 8.0));
    float offset = SAMPLE_TEXTURE2D(_DitherTexture, sampler_DitherTexture, interleavedPos / 8.0 + float2(0.5 / 8.0, 0.5 / 8.0)).w;
    
    int stepCount = _SampleCount;

    float stepSize = rayLength / stepCount;
    float3 step = rayDir * stepSize;
    float3 currentPos = rayStart + step * offset;
    float4 vlight = 0;
    float cosAngle;

#if defined(USE_CLUSTER_LIGHT)
    float3 color = _cluster_directionalLightColor[0].rgb;
    float3 direction = _cluster_directionalLightDirectionAndMasks[0].xyz;
#else
    float3 color = _directionalLightColor[0].rgb;
    float3 direction = _directionalLightDirectionAndMasks[0].xyz;
#endif
   

#if defined(_DIRECTIONAL) || defined(_DIRECTIONAL_COOKIE)
    float extinction = 0;
    cosAngle = dot(direction, rayDir);
#else
     float extinction = length(_WorldSpaceCameraPos - currentPos) * _VolumetricLight.y * 0.5;
#endif
    
    UNITY_LOOP
    for(int i = 0; i < stepCount; ++i)
    {
        float atten = GetLightAttenuation(currentPos);
        float density = GetDensity(currentPos);
        float scattering = _VolumetricLight.x * stepSize * density;
        extinction += _VolumetricLight.y * stepSize * density;

        float4 light = atten * scattering * exp(-extinction);
        
#if !defined(_DIRECTIONAL) && !defined(_DIRECTIONAL_COOKIE)
        float3 tolight = normalize(currentPos - direction.xyz);
        cosAngle = dot(tolight, -rayDir);
        light *= MieScattering(cosAngle, _MieG);
        
#endif
        vlight += light;
        currentPos += step;
    }

#if defined(_DIRECTIONAL) ||  defined(_DIRECTIONAL_COOKIE)
    vlight *= MieScattering(cosAngle, _MieG);
#endif
    
    vlight *= float4(color, 1.0);
    vlight = max(0, vlight);

#if defined(_DIRECTIONAL) ||  defined(_DIRECTIONAL_COOKIE)
    vlight.w = exp(-extinction);
#else
     vlight.w = 0;
#endif
    return vlight;
}


#endif