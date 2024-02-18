#ifndef __SHADER_LIBRARY_WAVING_GRASS_HLSL__
#define __SHADER_LIBRARY_WAVING_GRASS_HLSL__

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

// void InitializeInputData(Vayings input, half3 normalTS, out InputData inputData)
// {
// 	inputData = (InputData)0;
// 	inputData.positionWS = input.posWSShininess.xyz;
//
// 	half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
// #if SHADER_HINT_NICE_QUALITY
// 	viewDirWS = SafeNormalize(viewDirWS);
// #endif
//
// #ifdef _NORMAL_MAP
// 	float sgn = input.tangentWS.w;      // should be either +1 or -1
//     float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
//     half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
//     inputData.tangentToWorld = tangentToWorld;
//     inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
//     inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
// #else
// 	inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
// #endif
//
// 	inputData.viewDirectionWS = viewDirWS;
// #ifdef _MAIN_LIGHT_SHADOWS
// 	inputData.shadowCoord = input.shadowCoord;
// #else
// 	inputData.shadowCoord = float4(0, 0, 0, 0);
// #endif
// 	inputData.fogCoord = input.fogFactorAndVertexLight.x;
// 	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
// 	// inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
// }

// float4 MyTransformWorldToShadowCoord(float3 positionWS)
// {
// #ifdef _MAIN_LIGHT_SHADOWS_CASCADE
//     half cascadeIndex = ComputeCascadeIndex(positionWS);
// #else
//     half cascadeIndex = 0;
// #endif
//
//     float4 shadowCoord = mul(_MainLightWorldToShadow[cascadeIndex], float4(positionWS, 1.0));
// 	if (cascadeIndex > 0) {
// 		shadowCoord.z = -1;
// 	}
//
//     return float4(shadowCoord.xyz, cascadeIndex);
// }

void InitializeVertData(Attributes input, inout Vayings vertData)
{
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	// float3 worldPos = TransformWorldToHClip(positionWS);
	float3 samplePos = positionWS.xyz / _WindControl.w;
	float waveT = dot(samplePos, samplePos) + _Time.y * _WaveSpeed;
	float3 waveOffset = sin(waveT) * _WindControl.xyz * input.color.a;
	//press
	#if defined(INTERACTIVE)
	float radius = max(_Grass_Press_Point.w, 0.01);
	float3 rangeV = positionWS - _Grass_Press_Point.xyz;
	float pressVal = clamp(dot(rangeV, rangeV), 0, radius);
	pressVal = lerp(1, 0, pressVal / radius);
	float3 pressOffset = radius * pow(pressVal, 2.0) * normalize(rangeV) * input.color.a;
	#else
	float3 pressOffset = 0;
	#endif
	//wave
	positionWS = positionWS + waveOffset + pressOffset;	

	vertData.uv = input.texcoord;
	vertData.posWSShininess.xyz = positionWS;
	vertData.posWSShininess.w = 32;
	vertData.positionCS = TransformWorldToHClip(positionWS);

	#if defined(FORCE_UP_NORMAL)
		vertData.normalWS = float3(0, 1, 0); 
		vertData.tangentWS = float4(0, 0, 1, 1); 
	#else
		// VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangent);
		real sign = input.tangent.w * GetOddNegativeScale();
		float3 normalWS = TransformObjectToWorldNormal(input.normal);
		half4 tangentWS = half4(TransformObjectToWorldDir(input.tangent.xyz).xyz, sign);
		float3 bitangentWS = cross(normalWS, tangentWS) * sign;
	
		vertData.normalWS = normalWS;
		vertData.tangentWS = tangentWS;	
	#endif

	// We either sample GI from lightmap or SH.
	// Lightmap UV and vertex SH coefficients use the same interpolator ("float2 lightmapUV" for lightmap or "half3 vertexSH" for SH)
	// see DECLARE_LIGHTMAP_OR_SH macro.
	// The following funcions initialize the correct variable with correct data
	// OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, vertData.lightmapUV);
	// OUTPUT_SH(vertData.normalWS, vertData.vertexSH);

	// half3 vertexLight = VertexLighting(vertexInput.positionWS, vertData.normalWS.xyz);
	// half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
	// vertData.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#ifdef _MAIN_LIGHT_SHADOWS
	vertData.shadowCoord = TransformWorldToShadowCoord(vertexInput.positionWS);	
#endif
}

Vayings vert(Attributes v)
{
    Vayings o = (Vayings)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			
    InitializeVertData(v, o);
    o.color = v.color;

    return o;
}
			
half4 frag(Vayings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); 
    half4 perInstance_c = UNITY_ACCESS_INSTANCED_PROP(Props, _PerInstanceColor);
	InputConfig config = GetInputConfig(input.positionCS, input.uv);
	
#if defined(LOD_FADE_CROSSFADE)
	ClipLOD(config.fragment, unity_LODFade.x);
#endif

#if defined(_MASK_MAP)
	config.useMask = true;
#endif

#if defined(_DETAIL_MAP)
	config.detailUV = input.detailUV;
	config.useDetail = true;
#endif

	float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

#if defined(_CLIPPING)
	clip(col.a - _GrassCutoff);
#endif
	
    Surface surface;
	surface.position = input.posWSShininess.xyz;
	surface.positionCS = input.positionCS;
	
	float4 bump = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
	
#if defined(_NORMAL_MAP)
	float scale = INPUT_PROP(_NormalScale);
	float3 normalTS = 0;
	normalTS.xy = bump.xy * 2 - 1;
	normalTS.z = sqrt(1 - normalTS.x * normalTS.x - normalTS.y * normalTS.y);
	// surface.normal = NormalTangentToWorld(normalTS, input.normalWS, input.tangentWS);
	surface.normal = bump;
	surface.interpolatedNormal = input.normalWS;
#else
	surface.normal = input.normalWS;
	surface.interpolatedNormal = surface.normal;
#endif

	surface.depth = -TransformWorldToView(input.posWSShininess.xyz).z;
	surface.color = col.rgb;
	surface.alpha = col.a;
	surface.metallic = col.rga * perInstance_c.xyz * _WavingTint.rgb;
	surface.smoothness = 1.0;
	surface.occlusion = 1.0;
	surface.fresnelStrength = GetFresnel(config);
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.posWSShininess.xyz);
	surface.dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
	surface.renderingLayerMask = asuint(unity_RenderingLayer.x);

	// AlphaDiscard(surface.alpha, _Cutoff);
	
#if defined(_PREMULTIPLY_ALPHA)
	BRDF brdf = GetBRDF(surface, true);
#else
	BRDF brdf = GetBRDF(surface);
#endif

	GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
	float3 finalColor = GetLighting(surface, brdf, gi);
	// finalColor += GetEmission(config);
	//
	// finalColor += float3(0.5, 0.0, 0.0);
	return float4(finalColor, GetFinalAlpha(surface.alpha));
	// return float4(col.rgb, GetFinalAlpha(surface.alpha));
	
 //    surface.alpha = col.a;
 //    surface.albedo = col.rgb * perInstance_c.xyz * _WavingTint.rgb;
 //    #ifdef _NORMALMAP
 //   
 //    #else
 //    surface.normalTS = 0;
 //    #endif
 //    // surfaceData.smoothness = _Smoothness;
	// surface.smoothness = _Smoothness;
 //    surface.occlusion = 1.0;
 //    surface.emission = 0.0;
 //    surface.clearCoatMask = half(0.0);
 //    surface.clearCoatSmoothness = half(0.0);

    // AlphaDiscard(surfaceData.alpha, _Cutoff);

    // InputData inputData;
    // InitializeInputData(input, surfaceData.normalTS, inputData);
    // #ifdef _NORMALMAP
    // half4 color = FragmentTransluentPBR(inputData, surfaceData);
    // #else
    // half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData.albedo, half4(surfaceData.specular, 1.0), surfaceData.smoothness, surfaceData.emission, surfaceData.alpha);
    // #endif
    // color.rgb = MixFog(color.rgb, inputData.fogCoord);
    // return half4(1.0, 0.0, 0.0, 1.0);
    // return diffuseAlpha;
}


#endif