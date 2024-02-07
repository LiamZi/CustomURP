#ifndef __SHADER_LIBRARY_VT_BUMP_HLSL__
#define __SHADER_LIBRARY_VT_BUMP_HLSL__

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

void SplatmapMix(float4 uvSplat01, float4 uvSplat23, inout half4 splatControl, out half weight, out half3 mixedNormal)
{
	// Now that splatControl has changed, we can compute the final weight and normalize
	weight = dot(splatControl, 1.0h);

//#ifdef TERRAIN_SPLAT_ADDPASS
//	clip(weight <= 0.005h ? -1.0h : 1.0h);
//#endif
	half3 nrm = 0.0f;
	nrm += splatControl.r * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal0, sampler_linear_repeat_Normal0, uvSplat01.xy), _NormalScale0);
	nrm += splatControl.g * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal1, sampler_linear_repeat_Normal0, uvSplat01.zw), _NormalScale1);
	nrm += splatControl.b * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal2, sampler_linear_repeat_Normal0, uvSplat23.xy), _NormalScale2);
	nrm += splatControl.a * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal3, sampler_linear_repeat_Normal0, uvSplat23.zw), _NormalScale3);

	// avoid risk of NaN when normalizing.
#if HAS_HALF
	nrm.z += 0.01h;
#else
	nrm.z += 1e-5f;
#endif
	mixedNormal = normalize(nrm.xyz);
}

void ComputeMask(float4 uvSplat01, float4 uvSplat23, half4 splatControl, out half3 mixedMask)
{
	half4 masks[4];
    masks[0] = 0.5h;
    masks[1] = 0.5h;
    masks[2] = 0.5h;
    masks[3] = 0.5h;
    half4 hasMask = half4(_HasMask0, _HasMask1, _HasMask2, _HasMask3);
	
    masks[0] = lerp(masks[0], SAMPLE_TEXTURE2D(_Mask0, sampler_linear_repeat_Mask0, uvSplat01.xy), hasMask.x);
    masks[1] = lerp(masks[1], SAMPLE_TEXTURE2D(_Mask1, sampler_linear_repeat_Mask0, uvSplat01.zw), hasMask.y);
    masks[2] = lerp(masks[2], SAMPLE_TEXTURE2D(_Mask2, sampler_linear_repeat_Mask0, uvSplat23.xy), hasMask.z);
    masks[3] = lerp(masks[3], SAMPLE_TEXTURE2D(_Mask3, sampler_linear_repeat_Mask0, uvSplat23.zw), hasMask.w);
	
    half4 defaultMetallic = half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3);
    half4 defaultOcclusion = 1.0;

    half4 maskMetallic = half4(masks[0].r, masks[1].r, masks[2].r, masks[3].r);
    defaultMetallic = lerp(defaultMetallic, maskMetallic, hasMask);
    half metallic = dot(splatControl, defaultMetallic);

    half4 maskOcclusion = half4(masks[0].g, masks[1].g, masks[2].g, masks[3].g);
    defaultOcclusion = lerp(defaultOcclusion, maskOcclusion, hasMask);
    half occlusion = dot(splatControl, defaultOcclusion);
	mixedMask.rgb = half3(metallic, occlusion, 0);
}

Varyings vert(Attributes input)
{
    Varyings o;
    float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
    o.positionCS = TransformObjectToHClip(worldPos);

    o.uv.zw = input.uv;
    input.uv = input.uv * _BakeScaleOffset.xy + _BakeScaleOffset.zw;
    o.uv.xy = input.uv;

    o.uvSplat01.xy = TRANSFORM_TEX(input.uv, _Normal0);
    o.uvSplat01.zw = TRANSFORM_TEX(input.uv, _Normal1);
    o.uvSplat23.xy = TRANSFORM_TEX(input.uv, _Normal2);
    o.uvSplat23.zw = TRANSFORM_TEX(input.uv, _Normal3);
    
    return o;
}

float4 frag(Varyings input) : SV_TARGET
{
    float2 splatUV = (input.uv.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);

    half weight;
    half3 mixedNormal;
    SplatmapMix(input.uvSplat01, input.uvSplat23, splatControl, weight, mixedNormal);
    mixedNormal = (mixedNormal.xyz + 1) / 2;
    half3 mixedMask;
    ComputeMask(input.uvSplat01, input.uvSplat23, splatControl, mixedMask);
    half4 col = half4(mixedNormal.rg, mixedMask.g, mixedMask.r);
    return weight * col;
}

half4 fragAdd(Varyings input) : SV_TARGET
{
	float2 splatUV = (input.uv.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
	half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_linear_clamp_Control, splatUV);

	half weight;
	half3 mixedNormal;
	SplatmapMix(input.uvSplat01, input.uvSplat23, splatControl, weight, mixedNormal);
	clip(weight <= 0.005h ? -1.0h : 1.0h);
	mixedNormal = (mixedNormal.xyz + 1) / 2;
	half3 mixedMask;
	ComputeMask(input.uvSplat01, input.uvSplat23, splatControl, mixedMask);
	half4 col = half4(mixedNormal.rg, mixedMask.g, mixedMask.r);
	return weight * col;
}


#endif