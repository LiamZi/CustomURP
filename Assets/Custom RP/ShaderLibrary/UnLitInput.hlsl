#ifndef __SHADER_LIBRARY_UNLIT_INPUT_HLSL__
#define __SHADER_LIBRARY_UNLIT_INPUT_HLSL__

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);


UNITY_INSTANCING_BUFFER_START(PerInstance)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float, _CutOff)
UNITY_INSTANCING_BUFFER_END(PerInstance)


float2 TransformBaseUV (float2 baseUV) {
	float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
	return baseUV * baseST.xy + baseST.zw;
}

float4 GetBase (float2 baseUV) {
	float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
	float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
	return baseMap * baseColor;
}

float3 GetEmission (float2 baseUV) {
	return GetBase(baseUV).rgb;
}

float GetCutoff (float2 baseUV) {
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CutOff);
}

float GetMetallic (float2 baseUV) {
	return 0.0;
}

float GetSmoothness (float2 baseUV) {
	return 0.0;
}

#endif