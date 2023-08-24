#ifndef __SHADER_LIBRARY_SHADOW_CASTER_HLSL__
#define __SHADER_LIBRARY_SHADOW_CASTER_HLSL__


#include "Common.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
bool _ShadowPancaking;

UNITY_INSTANCING_BUFFER_START(PerInstance)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    // UNITY_DEFINE_INSTANCED_PROP(float3, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(PerInstance)

struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings ShadowCasterVert(Attributes input)
{
    Varyings o;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    o.positionCS = TransformWorldToHClip(positionWS);
    if(_ShadowPancaking)
    {
#if UNITY_REVERSED_Z
        o.positionCS.z = min(o.positionCS.z, o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
        o.positionCS.z = max(o.positionCS.z, o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif
    }

    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _BaseMap_ST);
    o.baseUV = input.baseUV * baseST.xy + baseST.zw;
    return o;
}

void ShadowCasterFrag(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _BaseColor);
    float4 col = baseMap * baseColor;

#if defined(_SHADOWS_CLIP)
    clip(col.a - UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Cutoff));
#elif defined(_SHADOWS_DITHER)
    float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
    clip(col.a - dither);
#endif

}

#endif