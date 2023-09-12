#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"


// CBUFFER_START(UnityPerDraw)
// float4x4 unity_ObjectToWorld;
// float4x4 unity_MatrixVP;
// CBUFFER_END

// #include "../ShaderLibrary/UnityInput.hlsl"



// CBUFFER_START(UnityPerMaterial)
//     float4 _BaseColor;
// CBUFFER_END

#include "Common.hlsl"
#include "UnLitInput.hlsl"

// TEXTURE2D(_BaseMap);
// SAMPLER(sampler_BaseMap);


// UNITY_INSTANCING_BUFFER_START(PerInstance)
//     UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
//     UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
//     UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
// UNITY_INSTANCING_BUFFER_END(PerInstance)

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
    // float4 baseST = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _BaseMap_ST);
    // o.baseUV = input.baseUV * baseST.xy + baseST.zw;
    o.baseUV = TransformBaseUV(input.baseUV);
    return o;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    // float4 diffuse = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    // float4 col = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _BaseColor);
    // col *= diffuse;
    float4 col = GetBase(input.baseUV);

#if defined(_CLIPPING)
    // clip(col.a - UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Cutoff));
    clip(col.a - GetCutoff(input.baseUV));
#endif
    return  float4(col.rgb, GetFinalAlpha(col.a));
}

#endif