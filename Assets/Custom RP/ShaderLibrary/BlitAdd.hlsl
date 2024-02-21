#ifndef __SHADER_LIBRARY_BLIT_ADD_HLSL__
#define __SHADER_LIBRARY_BLIT_ADD_HLSL__

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_Source);
float4 _MainTex_ST;
float4 _Source_TexelSize;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

Varyings vert(Attributes input)
{
    Varyings o;
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    o.positionCS = TransformWorldToHClip(positionWS);
    o.uv = TRANSFORM_TEX(input.uv, _MainTex);
    return o;
}

float4 frag(Varyings input) : SV_TARGET
{
    float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
#if UNITY_UV_STARTS_AT_TOP
    if(_Source_TexelSize.y < 0)
        input.uv.y = 1 - input.uv.y;
#endif
    float4 source = SAMPLE_TEXTURE2D(_Source, sampler_MainTex, input.uv);
    source *= col.w;
    source.xyz += col.xyz;
    return source;
}

#endif