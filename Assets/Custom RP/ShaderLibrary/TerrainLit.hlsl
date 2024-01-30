#ifndef __SHADER_LIBRARY_TERRAIN_LIT_HLSL__
#define __SHADER_LIBRARY_TERRAIN_LIT_HLSL__

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
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;
    float2 texcoord1 : TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 uvMainAndLM : TEXCOORD0;
    float4 uvSplat01 : TEXCOORD1;
    float4 uvSplat23 : TEXCOORD2;

#if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    float4 normal : TEXCOORD3;
    float4 tangent : TEXCOORD4;
    float4 bitangent : TEXCOORD5;
#else
    float3 normal                   : TEXCOORD3;
    float3 viewDir                  : TEXCOORD4;
    half3 vertexSH                  : TEXCOORD5; // SH
#endif
    
    half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light
    float3 positionWS               : TEXCOORD7;
    
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD8;
#endif
    float4 positionCS                  : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

// void TerrainInstancing(inout float4 positionOS, inout float3 normal, inout float2 uv)
// {
//     #ifdef UNITY_INSTANCING_ENABLED
//     float2 patchVertex = positionOS.xy;
//     float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);
//
//     float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z; // (xy + float2(xBase,yBase)) * skipScale
//     float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));
//
//     positionOS.xz = sampleCoords * _TerrainHeightmapScale.xz;
//     positionOS.y = height * _TerrainHeightmapScale.y;
//
//     #ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
//     normal = float3(0, 1, 0);
//     #else
//     normal = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1;
//     #endif
//     uv = sampleCoords * _TerrainHeightmapRecipSize.zw;
//     #endif
// }
//
// void TerrainInstancing(inout float4 positionOS, inout float3 normal)
// {
//     float2 uv = { 0, 0 };
//     TerrainInstancing(positionOS, normal, uv);
// }


Varyings vert(Attributes input)
{
    Varyings o;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(input);
    // TerrainInstancing(input.positionOS, input.normalOS, input.texcoord);

    o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    o.positionCS = TransformWorldToHClip(o.positionWS);
    o.uvMainAndLM.xy = input.texcoord;
    o.uvMainAndLM.zw = input.texcoord1 * unity_LightmapST.xy + unity_LightmapST.zw;

    o.uvSplat01.xy = TRANSFORM_TEX(input.texcoord, _Splat0);
    o.uvSplat01.zw = TRANSFORM_TEX(input.texcoord, _Splat1);
    o.uvSplat23.xy = TRANSFORM_TEX(input.texcoord, _Splat2);
    o.uvSplat23.zw = TRANSFORM_TEX(input.texcoord, _Splat3);

    half3 viewDirWS = _WorldSpaceCameraPos - o.positionWS;
#if !SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

#if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    float4 vertexTangent = float4(cross(float3(0, 0, 1), input.normalOS), 1.0);
    float sign = float(vertexTangent.w) * unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0;
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float3 tangentWS =  float3(TransformObjectToWorldDir(vertexTangent));
    float3 bitangentWS = float3(cross(normalWS, tangentWS)) * sign;
#else
    o.normal = TransformObjectToWorldNormal(input.normalOS);
    o.viewDir = viewDirWS;
    o.vertexSH = SampleSH(o.normal);
#endif
    o.fogFactorAndVertexLight = ComputeFogFactor(o.positionCS.z);
    o.fogFactorAndVertexLight.yzw = half3(0.0, 0.0, 0.0);

    return o;
}

void ComputeMasks(out half4 masks[4], half4 hasMask, Varyings input)
{
    masks[0] = 0.5h;
    masks[1] = 0.5h;
    masks[2] = 0.5h;
    masks[3] = 0.5h;

#ifdef _MASKMAP
    masks[0] = lerp(masks[0], SAMPLE_TEXTURE2D(_Mask0, sampler_Mask0, input.uvSplat01.xy), hasMask.x);
    masks[1] = lerp(masks[1], SAMPLE_TEXTURE2D(_Mask1, sampler_Mask0, input.uvSplat01.zw), hasMask.y);
    masks[2] = lerp(masks[2], SAMPLE_TEXTURE2D(_Mask2, sampler_Mask0, input.uvSplat23.xy), hasMask.z);
    masks[3] = lerp(masks[3], SAMPLE_TEXTURE2D(_Mask3, sampler_Mask0, input.uvSplat23.zw), hasMask.w);
#endif
}

void SplatmapMix(float4 uvMainAndLM, float4 uvSplat01, float4 uvSplat23, inout half4 splatControl, out half weight, out half4 mixedDiffuse, out half4 defaultSmoothness, inout half3 mixedNormal)
{
    half4 diffuseAlbedo[4];

    diffuseAlbedo[0] = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uvSplat01.xy);
    diffuseAlbedo[1] = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, uvSplat01.zw);
    diffuseAlbedo[2] = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, uvSplat23.xy);
    diffuseAlbedo[3] = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, uvSplat23.zw);

    defaultSmoothness = half4(diffuseAlbedo[0].a, diffuseAlbedo[1].a, diffuseAlbedo[2].a, diffuseAlbedo[3].a);
    defaultSmoothness = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);

    weight = dot(splatControl, 1.0h);
    
#if TERRAIN_SPLAT_ADDPASS
    clip(weight <= 0.005h ? -1.0h : 1.0h);
#endif

#ifndef _TERRAIN_BASEMAP_GEN
    splatControl /= (weight + HALF_MIN);
#endif

    mixedDiffuse = 0.0h;
    mixedDiffuse += diffuseAlbedo[0] * half4(splatControl.rrr, 1.0h);
    mixedDiffuse += diffuseAlbedo[1] * half4(splatControl.ggg, 1.0h);
    mixedDiffuse += diffuseAlbedo[2] * half4(splatControl.bbb, 1.0h);
    mixedDiffuse += diffuseAlbedo[3] * half4(splatControl.aaa, 1.0h);
    
#ifdef _NORMALMAP
    half3 nrm = 0.0f;
    nrm += splatControl.r * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal0, sampler_Normal0, uvSplat01.xy), _NormalScale0);
    nrm += splatControl.g * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal1, sampler_Normal0, uvSplat01.zw), _NormalScale1);
    nrm += splatControl.b * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal2, sampler_Normal0, uvSplat23.xy), _NormalScale2);
    nrm += splatControl.a * UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal3, sampler_Normal0, uvSplat23.zw), _NormalScale3);

#if HAS_HALF
    nrm.z += 0.01h;
#else
    nrm.z += 1e-5f;
#endif

    mixedNormal = normalize(nrm.xyz);

#endif


}

half4 frag(Varyings input) : SV_TARGET
{
    half3 normalTS = half3(0.0h, 0.0h, 1.0h);
    half4 hasMask = half4(_LayerHasMask0, _LayerHasMask1, _LayerHasMask2, _LayerHasMask3);
    half4 masks[4];
    ComputeMasks(masks, hasMask, input);

    float2 splatUV = (input.uvMainAndLM.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f)  * _Control_TexelSize.xy;
    half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);

    half weight;
    half4 mixedDiffuse;
    half4 defaultSmoothness;
    SplatmapMix(input.uvMainAndLM, input.uvSplat01, input.uvSplat23, splatControl, weight, mixedDiffuse, defaultSmoothness, normalTS);
    half3 albedo = mixedDiffuse.rgb;

    half4 defaultMetallic = half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3);
    half4 defaultOcclusion = 1.0;

    half4 maskSmoothness = half4(masks[0].a, masks[1].a, masks[2].a, masks[3].a);
    defaultSmoothness = lerp(defaultSmoothness, maskSmoothness, hasMask);
    half smoothness = dot(splatControl, defaultSmoothness);

    half4 maskMetallic = half4(masks[0].r, masks[1].r, masks[2].r, masks[3].r);
    defaultMetallic = lerp(defaultMetallic, maskMetallic, hasMask);
    half metallic = dot(splatControl, defaultMetallic);

    half4 maskOcclusion = half4(masks[0].g, masks[1].g, masks[2].g, masks[3].g);
    defaultOcclusion = lerp(defaultOcclusion, maskOcclusion, hasMask);
    half occlusion = dot(splatControl, defaultOcclusion);
    half alpha = weight;
    
    
    return mixedDiffuse * alpha;
}

#endif