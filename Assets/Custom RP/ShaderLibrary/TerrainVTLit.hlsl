#ifndef __SHADER_LIBRARY_TERRAIN_VT_LIT_HLSL__
#define __SHADER_LIBRARY_TERRAIN_VT_LIT_HLSL__

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
    float4 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 uvMainAndLM : TEXCOORD0;
#if defined(_NORMAL_MAP)
    float4 normalWS : TEXCOORD1;
    float4 tangentWS : TEXCOORD2;
    float4 bitangentWS : TEXCOORD3;
#else
    float3 normalWS : TEXCOORD1;
    // half3 vertexSH : TEXCOORD3;
#endif
    float3 positionWS : TEXCOORD4;
    // half4 fogFactorAndVertexLight : TEXCOORD4;
    GI_VARYINGS_DATA
   UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings vert(Attributes input)
{
    Varyings o;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    TRANSFER_GI_DATA(input, o);
    
    o.uvMainAndLM.xy = TRANSFORM_TEX(input.uv, _Diffuse);
    o.uvMainAndLM.zw = input.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    
#if defined(_NORMAL_MAP)
    float4 vertexTangent = float4(cross(float3(0, 0, 1), input.normalOS), 1.0);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float3 tangentWS = TransformObjectToWorldDir(vertexTangent);
    float sign = input.normalOS.w * GetOddNegativeScale();
    float3 bitangentWS = cross(normalWS, tangentWS) * sign;
    o.normalWS = half4(normalWS, positionWS.x);
    o.tangentWS = half4(tangentWS, positionWS.y);
    o.bitangentWS = half4(bitangentWS, positionWS.z);
#else
    o.normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif
    
    o.positionWS = positionWS;
    o.positionCS = TransformWorldToHClip(positionWS);

    return o;
}

float4 frag(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    // 
    
    InputConfig config = GetInputConfig(input.positionCS, input.uvMainAndLM.xy);

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
    
    float4 col = SAMPLE_TEXTURE2D(_Diffuse, sampler_Diffuse, input.uvMainAndLM.xy);

#if defined(_CLIPPING)
    clip(col.a - GetCutoff(config));
#endif

    Surface surface;
    surface.position = input.positionWS;
    surface.positionCS = input.positionCS;

    float4 map = SAMPLE_TEXTURE2D(_Normal, sampler_Normal, input.uvMainAndLM.xy);
#if defined(_NORMAL_MAP)
    float scale = INPUT_PROP(_NormalScale);
    float3 normalTS = 0;
    normalTS.xy = map.xy * 2 - 1;
    normalTS.z = sqrt(1 - normalTS.x * normalTS.x - normalTS.y * normalTS.y);
    // surface.normal = NormalTangentToWorld(normalTS, input.normalWS, input.tangentWS);
    // surface.normal = normalize(input.normalWS);
    surface.normal = input.normalWS;
    surface.interpolatedNormal = input.normalWS;
#else
    surface.normal = input.normalWS;
    surface.interpolatedNormal = surface.normal;
#endif

    surface.depth = -TransformWorldToView(input.positionWS).z;
    surface.color = col.rgb;
    surface.alpha = 1.0;
    surface.metallic = map.a;
    surface.smoothness = col.a;
    surface.occlusion = map.b;
    surface.fresnelStrength = 1.0;
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
    surface.renderingLayerMask = asuint(unity_RenderingLayer.x);


#if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface, true);
#else
    BRDF brdf = GetBRDF(surface);
#endif
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
    float3 finalColor = GetLighting(surface, brdf, gi);
    // finalColor += GetEmission(config);
    
    
    return float4(finalColor, GetFinalAlpha(surface.alpha));
    // return float4(1.0, 1.0, 0.0, 1.0);
}


#endif