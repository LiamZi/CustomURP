#ifndef __SHADER_LIBRARY_LPW_TERRAIN_HLSL__
#define __SHADER_LIBRARY_LPW_TERRAIN_HLSL__

#include "Surface.hlsl"
#include "Shadows.hlsl"
#include "Light.hlsl"
#include "BRDF.hlsl"
#include "GI.hlsl"
#include "Lighting.hlsl"

#ifdef UNITY_INSTANCING_ENABLED
    StructuredBuffer<float4x4> positionBuffer;
#endif

TEXTURE2D(_NoiseTexture);

struct VertexInput
{
    // float4 positionOS : POSITION;
# ifndef  UNITY_INSTANCING_ENABLED
    float4 positionOS : POSITION;
#endif
    
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

float3 RotateAroundAxis(float3 center, float3 original, float3 u, float angle)
{
    original -= center;
    float C = cos(angle);
    float S = sin(angle);
    float t = 1 - C;
    float m00 = t * u.x * u.x + C;
    float m01 = t * u.x * u.y - S * u.z;
    float m02 = t * u.x * u.z + S * u.y;
    float m10 = t * u.x * u.y + S * u.z;
    float m11 = t * u.y * u.y + C;
    float m12 = t * u.y * u.z - S * u.x;
    float m20 = t * u.x * u.z - S * u.y;
    float m21 = t * u.y * u.z + S * u.x;
    float m22 = t * u.z * u.z + C;

    float3x3 mat = float3x3(m00, m01, m02, m10, m11, m12, m20, m21, m22);
    return mul(mat, original) + center;
}


VertexOutput vert(VertexInput input, uint instanceID : SV_InstanceID)
{
    VertexOutput o;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o)
    TRANSFER_GI_DATA(input, o);
    
#ifdef UNITY_INSTANCING_ENABLED
    float4x4 positionWS = positionBuffer[instanceID];
    float3 positionOS = posWS;
    positionOS._14_24_34 *= -1;
    positionOS._11_22_33 = 1.0f / positionOS._11_22_33;
#else
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
#endif
    o.positionCS = float4(1.0, 1.0, 1.0, 1.0);
    return o;
}

float4 frag(VertexOutput input) : SV_TARGET
{
    return float4(1.0, 1.0, 1.0, 1.0);
}

#endif
