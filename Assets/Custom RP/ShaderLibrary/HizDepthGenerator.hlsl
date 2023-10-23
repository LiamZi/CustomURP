#ifndef __SHADER_LIBRARY_HIZ_DEPTH_GENERATOR_HLSL__
#define __SHADER_LIBRARY_HIZ_DEPTH_GENERATOR_HLSL__

TEXTURE2D(_HizMap);
SAMPLER(sampler_HizMap);
float4 _HizMap_TexelSize;

struct VertexInput
{
    float4 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
};

inline float CalculatorMipmapDepth(float2 uv)
{
    float4 depth;
    float offset = _HizMap_TexelSize.x / 2;
    depth.x = SAMPLE_TEXTURE2D(_HizMap, sampler_HizMap, uv);
    depth.y = SAMPLE_TEXTURE2D(_HizMap, sampler_HizMap, uv + float2(0, offset));
    depth.z = SAMPLE_TEXTURE2D(_HizMap, sampler_HizMap, uv + float2(offset, 0));
    depth.w = SAMPLE_TEXTURE2D(_HizMap, sampler_HizMap, uv + float2(offset, offset));
#if defined(UNITY_REVERSED_Z)
    return min(min(depth.x, depth.y), min(depth.z, depth.w));
#else
    return max(max(depth.x, depth.y), max(depth.z, depth.w));
#endif
}


VertexOutput vert(VertexInput input)
{
    VertexOutput o;
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    o.positionCS = TransformWorldToHClip(positionWS);
    o.baseUV = input.baseUV;
    return o;
}

float4 frag(VertexOutput input) : SV_TARGET
{
    float depth = CalculatorMipmapDepth(input.baseUV);
    return float4(depth, 0, 0, 1.0);
}




#endif  