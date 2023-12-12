#ifndef __CUSTOM_COMMON_HLSL__
#define __CUSTOM_COMMON_HLSL__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"


#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_PREV_MATRIX_M unity_prev_MatrixM
#define UNITY_PREV_MATRIX_I_M unity_prev_MatrixIM
#define PI 3.1415926535
#define MIE_G (-0.990)
#define MIE_G2 0.9801


#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
    #define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);

bool isOrthographicCamera()
{
    return unity_OrthoParams.w;
}

float OrthographicDepthBufferToLinear(float rawDepth)
{
#if UNITY_REVERSED_Z
    rawDepth = 1.0 - rawDepth;
#endif
    return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;
}

#include "Fragment.hlsl"

float Square(float v)
{
    return v * v;
}

float DistanceSquared(float3 p1, float3 p2)
{
    return dot(p1 - p2, p1 - p2);
}

// void ClipLOD(float2 positionsCS, float fade)
void ClipLOD(Fragment fragment, float fade)
{
    // float dither = (positionsCS.y % 32) / 32;
#if defined(LOD_FADE_CROSSFADE)
    float dither = InterleavedGradientNoise(fragment.positionSS, 0);
    clip(fade + (fade < 0.0 ? dither : -dither));
#endif
}

float3 DecodeNormal(float4 sample, float scale)
{
// #if defined(UNITY_NO_DXT5nm)
//     return normalize(UnpackNormalRGB(sample, scale));
// #else
//     return normalize(UnpackNormalmapRGorAG(sample, scale));
// #endif

#if defined(UNITY_NO_DXT5nm)
    return normalize(UnpackNormalRGB(sample, scale));
//    return UnpackNormalRGB(sample, scale);
#else
    return normalize(UnpackNormalmapRGorAG(sample, scale));
//    return UnpackNormalmapRGorAG(sample, scale);
#endif
}

float3 NormalTangentToWorld(float3 normalTS, float3 normalWS, float4 tangentWS)
{
    // float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    // return TransformTangentToWorld(normalTS, tangentToWorld);

    float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
	return TransformTangentToWorld(normalTS, tangentToWorld);
}

#endif