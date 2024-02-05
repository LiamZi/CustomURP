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
    float4 normal : TEXCOORD1;
    float4 tangent : TEXCOORD2;
    float4 bitangent : TEXCOORD3;
#else
    float3 normal : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
    // half3 vertexSH : TEXCOORD3;
#endif
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
    float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
    

#if defined(_NORMAL_MAP)
    float4 vertexTangent = float4(cross(float3(0, 0, 1), input.normalOS), 1.0);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float3 tangentWS = TransformObjectToWorldDir(vertexTangent);
    float sign = input.normalOS.w * GetOddNegativeScale();
    float3 bitangentWS = cross(normalWS, tangentWS) * sign;
    o.normal = half4(normalWS, worldPos.x);
    o.tangent = half4(tangentWS, worldPos.y);
    o.bitangent = half4(bitangentWS, worldPos.z);
    
#else
    o.normal = TransformObjectToWorldNormal(input.normalOS);
    o.positionWS = worldPos;
#endif
    o.positionCS = TransformObjectToHClip(worldPos);

    return o;
}

float4 frag(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    float4 mixedDiffuse = SAMPLE_TEXTURE2D(_Diffuse, sampler_Diffuse, input.uvMainAndLM.xy);
    float4 mixedNormal = SAMPLE_TEXTURE2D(_Normal, sampler_Normal, input.uvMainAndLM.xy);
    float3 normalTS = 0;
    normalTS.xy = mixedNormal.xy * 2 - 1;
    normalTS.z = sqrt(1 - normalTS.x * normalTS.x - normalTS.y * normalTS.y);
    float3 albedo = mixedDiffuse.rgb;
    
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
    
    float4 col = GetBase(config);

#if defined(_CLIPPING)
    clip(col.a - GetCutoff(config));
#endif
    
    return mixedDiffuse;
}


#endif