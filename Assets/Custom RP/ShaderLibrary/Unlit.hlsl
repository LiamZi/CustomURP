#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED



#include "Common.hlsl"
#include "UnLitInput.hlsl"

struct VertexInput
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

VertexOutput vert(VertexInput input)
{
    VertexOutput o;
    // o.positionWS = input.positionOS;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    // float4 positionWS = mul(UNITY_MATRIX_M, float4(input.positionOS.xyz, 1.0));
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

    // o.positionCS = mul(unity_MatrixVP, positionWS);
    o.positionCS = TransformWorldToHClip(positionWS);
    // float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    // o.baseUV = input.baseUV * baseST.xy + baseST.zw;
    o.baseUV = TransformBaseUV(input.baseUV);
    return o;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    // float4 diffuse = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    // float4 col = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    // col *= diffuse;
    float4 col = GetBase(input.baseUV);

#if defined(_CLIPPING)
    // clip(col.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    clip(col.a - GetCutoff(input.baseUV));
#endif
    return  float4(col.rgb, GetFinalAlpha(col.a));
}

#endif