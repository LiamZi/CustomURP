#ifndef __CUSTOM_RP_SHADER_LIT_HLSL__
#define __CUSTOM_RP_SHADER_LIT_HLSL__

// #include "Common.hlsl"
// #include "LitInput.hlsl"
#include "Surface.hlsl"
#include "Shadows.hlsl"
#include "Light.hlsl"
#include "BRDF.hlsl"
#include "GI.hlsl"
#include "Lighting.hlsl"

struct VertexInput
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;
    float4 tangentOS : TANGENT;

    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float3 normalWS : VAR_NORMAL;
#if defined(_NORMAL_MAP)
    float4 tangentWS : VAR_TANGENT;
#endif
    float2 baseUV : VAR_BASE_UV;
#if defined(_DETAIL_MAP)
    float2 detailUV : VAR_DETAIL_UV;
#endif
    float3 worldPos : TEXCOORD1;
    GI_VARYINGS_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

VertexOutput vert(VertexInput input)
{
    VertexOutput o;
    // o.positionWS = input.positionOS;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    TRANSFER_GI_DATA(input, o);
    // float4 positionWS = mul(UNITY_MATRIX_M, float4(input.positionOS.xyz, 1.0));
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    // o.positionCS = mul(unity_MatrixVP, positionWS);
    o.positionCS = TransformWorldToHClip(positionWS);
    // o.normalWS = mul((float3x3)UNITY_MATRIX_M, input.normal);
    o.normalWS = TransformObjectToWorldNormal(input.normalOS);
    // o.normalWS = TransformWorldToView(input.normalOS);
    o.worldPos = positionWS.xyz;
    o.positionWS = positionWS;
#if defined(_DETAIL_MAP)
    o.detailUV = TransformDetailUV(input.baseUV);
#endif
    // o.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#if defined(_NORMAL_MAP)
    o.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif
    // float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    // o.baseUV = input.baseUV * baseST.xy + baseST.zw;
    o.baseUV = TransformBaseUV(input.baseUV);
    return o;
}

float4 frag(VertexOutput input) : SV_TARGET
{

    UNITY_SETUP_INSTANCE_ID(input);
// #if defined(LOD_FADE_CROSSFADE) || defined (LOD_FADE_PERCENTAGE)
// #if defined(LOD_FADE_CROSSFADE)
//     // return unity_LODFade.x;
//     ClipLOD(input.positionCS.xy, unity_LODFade.x);
// #endif

    // float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    // float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    // float4 col = baseMap * baseColor;
    InputConfig config = GetInputConfig(input.positionCS, input.baseUV);
    // return float4(config.fragment.depth.xxx / 20.0, 1.0);

#if defined(LOD_FADE_CROSSFADE)
    // return unity_LODFade.x;
    // ClipLOD(input.positionCS.xy, unity_LODFade.x);
    ClipLOD(config.fragment, unity_LODFade.x);
#endif

#if defined(_MASK_MAP)
    config.useMask = true;
#endif

#if defined(_DETAIL_MAP)
    config.detailUV = input.detailUV;
    config.useDetail = true;
#endif

    // float4 col = GetBase(input.baseUV, input.detailUV);
    float4 col = GetBase(config);
#if defined(_CLIPPING)
        // clip(col.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
        // clip(col.a - GetCutoff(input.baseUV));
        clip(col.a - GetCutoff(config));
#endif

    Surface surface;
    surface.position = input.positionWS;
    // surface.normal = normalize(input.normalWS);

#if defined(_NORMAL_MAP)
    // surface.normal = NormalTangentToWorld(GetNormalTS(input.baseUV, input.detailUV), input.normalWS, input.tangentWS);
    surface.normal = NormalTangentToWorld(GetNormalTS(config), input.normalWS, input.tangentWS);

    surface.interpolatedNormal = input.normalWS;
#else
    surface.normal = normalize(input.normalWS);
    surface.interpolatedNormal = surface.normal;
#endif
    surface.depth = -TransformWorldToView(input.positionWS).z;
    surface.color = col.rgb;
    surface.alpha = col.a;
    // surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
    // surface.metallic = GetMetallic(input.baseUV);
    surface.metallic = GetMetallic(config);

    // surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
    // surface.smoothness = GetSmoothness(input.baseUV, input.detailUV);
    surface.smoothness = GetSmoothness(config);

    // surface.occlusion = GetOcclusion(input.baseUV);
    surface.occlusion = GetOcclusion(config);

    // surface.fresnelStrength = GetFresnel(input.baseUV);
    surface.fresnelStrength = GetFresnel(config);

    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
    // surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);
    surface.dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
    surface.renderingLayerMask = asuint(unity_RenderingLayer.x);


#if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface, true);
#else
    BRDF brdf = GetBRDF(surface);
#endif

    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
    float3 finalColor = GetLighting(surface, brdf, gi);
    finalColor += GetEmission(config);
    // return float4(finalColor, surface.alpha);
    return float4(finalColor, GetFinalAlpha(surface.alpha));
    // return float4(surface.color, surface.alpha);
}

#endif