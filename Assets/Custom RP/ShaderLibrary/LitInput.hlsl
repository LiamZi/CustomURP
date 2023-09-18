#ifndef __SHADER_LIBRARY_LIT_INPUT_HLSL__
#define __SHADER_LIBRARY_LIT_INPUT_HLSL__

struct InputConfig
{
    float2 baseUV;
    float2 detailUV;
    bool useMask;
    bool useDetail;
};

TEXTURE2D(_BaseMap);
TEXTURE2D(_MaskMap);
TEXTURE2D(_NormalMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_DetailMap);
TEXTURE2D(_DetailNormalMap);
SAMPLER(sampler_DetailMap);


UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _DetailMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
	UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Occlusion)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
	UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)
	UNITY_DEFINE_INSTANCED_PROP(float, _DetailAlbedo)
	UNITY_DEFINE_INSTANCED_PROP(float, _DetailSmoothness)
	UNITY_DEFINE_INSTANCED_PROP(float, _DetailNormalScale)
	UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

InputConfig GetInputConfig(float2 baseUV, float2 detailUV = 0.0)
{
    InputConfig c;
    c.baseUV = baseUV;
    c.detailUV = detailUV;
    c.useMask = false;
    c.useDetail = false;
    return c;
}

float2 TransformBaseUV(float2 baseUV)
{
    // float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    float4 baseST = INPUT_PROP(_BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

// float4 GetDetail(float2 detailUV)
float4 GetDetail(InputConfig c)
{
    if(c.useDetail)
    {
        float4 map = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, c.detailUV);
        // return map;
        return map * 2.0 - 1.0;
    }
    return 0.0;
}

// float4 GetMask(float2 baseUV)
float4 GetMask(InputConfig c)
{
    if(c.useMask)
    {
        return SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, c.baseUV);
    }
    return 1.0;
}

// float4 GetBase(float2 baseUV, float2 detailUV = 0.0)
float4 GetBase(InputConfig c)
{
    float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.baseUV);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);

    if(c.useDetail)
    {
        float detail = GetDetail(c).r * INPUT_PROP(_DetailAlbedo);
        float mask = GetMask(c).b;
        // map += detail;
        // map.rgb = lerp(sqrt(map.rgb), detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
        map.rgb = lerp(sqrt(map.rgb), detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
        map.rgb *= map.rgb;
    }

    return map * color;
}


// float GetCutoff(float2 baseUV)
float GetCutoff(InputConfig c)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

// float GetMetallic(float2 baseUV)
float GetMetallic(InputConfig c)
{
    float metallic = INPUT_PROP(_Metallic);
    metallic *= GetMask(c).r;
    return metallic;
}

// float GetSmoothness(float2 baseUV, float detailUV = 0.0)
float GetSmoothness(InputConfig c)

{
    // return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
    float smoothness = INPUT_PROP(_Smoothness);
    smoothness *= GetMask(c).a;

    if(c.useDetail)
    {
        float detail = GetDetail(c).b * INPUT_PROP(_DetailSmoothness);
        float mask = GetMask(c).b;
        smoothness = lerp(smoothness, detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
    }

    return smoothness;
}

// float3 GetEmission(float2 baseUV)
float3 GetEmission(InputConfig c)
{
    float4 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, c.baseUV);
    float4 col = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissionColor);
    return map.rgb * col.rgb;
    // return GetBase(baseUV).rgb;
}

// float GetFresnel(float2 baseUV)
float GetFresnel(InputConfig c)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Fresnel);
}

// float GetOcclusion(float2 baseUV)
float GetOcclusion(InputConfig c)

{
    float strength = INPUT_PROP(_Occlusion);
    float occlusion = GetMask(c).g;
    occlusion = lerp(occlusion, 1.0, strength);
    return occlusion;
}

float2 TransformDetailUV(float2 detailUV)
{
    float4 detailST = INPUT_PROP(_DetailMap_ST);
    return detailUV * detailST.xy + detailST.zw;
}

// float3 GetNormalTS(float2 baseUV, float detailUV = 0.0)
float3 GetNormalTS(InputConfig c)
{
    float4 map = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, c.baseUV);
	float scale = INPUT_PROP(_NormalScale);
	float3 normal = DecodeNormal(map, scale);

    if(c.useDetail)
    {
        map = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailMap, c.detailUV);
        scale = INPUT_PROP(_DetailNormalScale) * GetMask(c).b;
        float3 detail = DecodeNormal(map, scale);
        normal = BlendNormalRNM(normal, detail);
    }

    return normal;
}

float GetFinalAlpha(float alpha)
{
    return INPUT_PROP(_ZWrite) ? 1.0 : alpha;
}

#endif