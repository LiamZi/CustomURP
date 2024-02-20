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
    
#ifdef NOISE
    float noiseUV = frac(pos * _NoiseData.x + float3(_Time.y * _NoiseVelocity.x, 0, _Time.y * _NoiseVelocity.y));
    float noise = SAMPLE_TEXTURE3D(_NoiseTexture, sampler_NoiseTexture, noiseUV);
    noise = saturate(noise - _NoiseData.z) * _NoiseData.y;
    density = saturate(noise);
#endif
    
    // ApplyHeightFog(pos, density);

    return density;
}

float MieScattering(float angle, float4 g)
{
    return g.w * (g.x / (pow(g.y - g.z * angle, 1.5)));
}


float4 RayMarch(float2 screenPos, float3 rayStart, float3 rayDir, float rayLength, Surface surface, ShadowData shadowData)
{
    float inerleavedPos = fmod(floor(screenPos.xy), 8.0);
    float offset = SAMPLE_TEXTURE2D(_DitherTexture, sampler_DitherTexture, inerleavedPos / 8.0 + float2(0.5 / 8.0, 0.5 / 8.0)).w;

    
    int stepCount = _SampleCount;

    float stepSize = rayLength / stepCount;
    float3 step = rayDir * stepSize;
    float3 currentPos = rayStart + step * offset;
    float4 vlight = 0;
    float cosAngle;

#if defined(_DIRECTIONAL) || defined(_DIRECTIONAL_COOKIE)
    float extinction = 0;
    cosAngle = dot(_directionalLightDirectionAndMasks[0].xyz, -rayDir);
#else
    float extinction = length(_WorldSpaceCameraPos - currentPos) * _VolumetricLight.y * 0.5;
#endif

    Light light = GetDirectionalLight(0, surface, shadowData);
    
    UNITY_LOOP
    for(int i = 0; i < stepCount; ++i)
    {
        float atten = light.attenuation;
        float density = GetDensity(currentPos);
        float scattering = _VolumetricLight.x * stepSize * density;
        extinction += _VolumetricLight.y * stepSize * density;

        float4 l = atten * scattering * exp(-extinction);
        
#if !defined(_DIRECTIONAL) && !defined(_DIRECTIONAL_COOKIE)
        float3 tolight = normalize(currentPos - light.direction.xyz);
        cosAngle = dot(tolight, -rayDir);
        l *= MieScattering(cosAngle, _MieG);
        
#endif
        vlight += l;
        currentPos += step;
    }

#if defined(_DIRECTIONAL) ||  defined(_DIRECTIONAL_COOKIE)
    vlight *= MieScattering(cosAngle, _MieG);
#endif
    vlight *= float4(light.color, 1.0);
    vlight = max(0, vlight);

#if defined(_DIRECTIONAL) ||  defined(_DIRECTIONAL_COOKIE)
    vlight.w = exp(-extinction);
#else
    vlight.w = 0;
#endif
    return vlight;
}


#endif