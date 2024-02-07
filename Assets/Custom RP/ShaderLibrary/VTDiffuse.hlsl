#ifndef __SHADER_LIBRARY_VT_DIFFUSE_HLSL__
#define __SHADER_LIBRARY_VT_DIFFUSE_HLSL__

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

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 uv : TEXCOORD0;
    float4 uvSplat01 : TEXCOORD1;
    float4 uvSplat23 : TEXCOORD2;
};

void SplatmapMix(float4 uvSplat01, float4 uvSplat23,
            inout float4 splatControl, out float weight,
            out float4 mixedDiffuse, out float4 defaultSmoothness)
{
    float4 diffAlbedo[4];
    diffAlbedo[0] = SAMPLE_TEXTURE2D(_Splat0, sampler_linear_repeat_Splat0, uvSplat01.xy);
    diffAlbedo[1] = SAMPLE_TEXTURE2D(_Splat1, sampler_linear_repeat_Splat0, uvSplat01.zw);
    diffAlbedo[2] = SAMPLE_TEXTURE2D(_Splat2, sampler_linear_repeat_Splat0, uvSplat23.xy);
    diffAlbedo[3] = SAMPLE_TEXTURE2D(_Splat3, sampler_linear_repeat_Splat0, uvSplat23.zw);

    defaultSmoothness = float4(diffAlbedo[0].a, diffAlbedo[1].a, diffAlbedo[2].a, diffAlbedo[3].a);
    defaultSmoothness *= float4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);

    weight = dot(splatControl, 1.0);

    mixedDiffuse = 0.0;
    mixedDiffuse += diffAlbedo[0] * float4(_DiffuseRemapScale0.rgb * splatControl.rrr, 1.0);
    mixedDiffuse += diffAlbedo[1] * float4(_DiffuseRemapScale0.rgb * splatControl.ggg, 1.0);
    mixedDiffuse += diffAlbedo[2] * float4(_DiffuseRemapScale0.rgb * splatControl.bbb, 1.0);
    mixedDiffuse += diffAlbedo[3] * float4(_DiffuseRemapScale0.rgb * splatControl.aaa, 1.0);
}

float ComputeSmoothness(float4 uvSplat01, float4 uvSplat23, float4 splatControl, float4 defaultSmoothness)
{
    float4 masks[4];
    masks[0] = 0.5;
    masks[1] = 0.5;
    masks[2] = 0.5;
    masks[3] = 0.5;

    float4 hasMask = float4(_HasMask0, _HasMask1, _HasMask2, _HasMask3);
	
    masks[0] = lerp(masks[0], SAMPLE_TEXTURE2D(_Mask0, sampler_linear_repeat_Mask0, uvSplat01.xy), hasMask.x);
    masks[1] = lerp(masks[1], SAMPLE_TEXTURE2D(_Mask1, sampler_linear_repeat_Mask0, uvSplat01.zw), hasMask.y);
    masks[2] = lerp(masks[2], SAMPLE_TEXTURE2D(_Mask2, sampler_linear_repeat_Mask0, uvSplat23.xy), hasMask.z);
    masks[3] = lerp(masks[3], SAMPLE_TEXTURE2D(_Mask3, sampler_linear_repeat_Mask0, uvSplat23.zw), hasMask.w);
	
    float4 maskSmoothness = float4(masks[0].a, masks[1].a, masks[2].a, masks[3].a);
    defaultSmoothness = lerp(defaultSmoothness, maskSmoothness, hasMask);
    return dot(splatControl, defaultSmoothness);
}

Varyings vert(Attributes input)
{
    Varyings o;

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    o.positionCS = TransformWorldToHClip(positionWS);
    
    o.uv.zw = input.uv;
    input.uv.xy = input.uv.xy * _BakeScaleOffset.xy + _BakeScaleOffset.zw;
    o.uv.xy = input.uv;

    o.uvSplat01.xy = TRANSFORM_TEX(input.uv, _Splat0);
    o.uvSplat01.zw = TRANSFORM_TEX(input.uv, _Splat1);
    o.uvSplat23.xy = TRANSFORM_TEX(input.uv, _Splat2);
    o.uvSplat23.zw = TRANSFORM_TEX(input.uv, _Splat3);

    return o;
}

float4 frag(Varyings input) : SV_TARGET
{
    float2 splatUV = (input.uv.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    float4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);

    float weight;
    float4 mixedDiffuse;
    float4 defaultSmoothness;
    SplatmapMix(input.uvSplat01, input.uvSplat23, splatControl, weight, mixedDiffuse, defaultSmoothness);
    float smoothness = ComputeSmoothness(input.uvSplat01, input.uvSplat23, splatControl, defaultSmoothness);
    
    return float4(mixedDiffuse.rgb, smoothness);
}

float4 fragAdd(Varyings input) : SV_TARGET
{
    float2 splatUV = (input.uv.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    float4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_linear_clamp_Control, splatUV);
    
    float weight;
    float4 mixedDiffuse;
    float4 defaultSmoothness;
    SplatmapMix(input.uvSplat01, input.uvSplat23, splatControl, weight, mixedDiffuse, defaultSmoothness);
    clip(weight <= 0.005 ? -1.0 : 1.0);
    float smoothness = ComputeSmoothness(input.uvSplat01, input.uvSplat23, splatControl, defaultSmoothness);
    
    return float4(mixedDiffuse.rgb, smoothness * weight);
}


#endif