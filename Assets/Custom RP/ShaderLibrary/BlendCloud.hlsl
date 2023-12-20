#ifndef __SHADER_LIBRARY_BLEND_CLOUD_HLSL__
#define __SHADER_LIBRARY_BLEND_CLOUD_HLSL__

#include "../ShaderLibrary/CloudFunctions.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_TexelSize;

TEXTURE2D(_UndersamplerCloudTex);
SAMPLER(sampler_UndersamplerCloudTex);
float4 _UndersampleCloudTex_TexelSize;

float4x4 _PrevVP;

TEXTURE2D(_DownsampledDepth);
SAMPLER(sampler_DownsampledDepth);
float2 _TexelSize;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float2 vsray : TEXCOORD1;
    float4 screenPos : TEXCOORD2;
};

Varyings vert(Attributes input)
{
    Varyings o;
    o.positionCS = TransformObjectToHClip(input.positionOS);
    o.uv = input.uv;
    o.vsray = (2.0 * input.uv - 1.0) * _ProjectionExtents.xy + _ProjectionExtents.zw;
    o.screenPos = ComputeScreenPos(input.positionOS, _ProjectionParams.x);
    return o;
}

float4 frag(Varyings input) : SV_Target
{
    return float4(1.0, 0.0, 0.0, 1.0);
}

#endif