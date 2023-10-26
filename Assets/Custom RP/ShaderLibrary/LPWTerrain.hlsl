#ifndef __SHADER_LIBRARY_LPW_TERRAIN_HLSL__
#define __SHADER_LIBRARY_LPW_TERRAIN_HLSL__

#include "Surface.hlsl"
#include "Shadows.hlsl"
#include "Light.hlsl"
#include "BRDF.hlsl"
#include "GI.hlsl"
#include "Lighting.hlsl"

#if defined(_GPU_RESULT)
    StructuredBuffer<float4x4> positionBuffer;
#endif

TEXTURE2D(_NoiseTexture);

struct VertexInput
{
    // float4 positionOS : POSITION;
// # ifndef  UNITY_INSTANCING_ENABLED
    float4 positionOS : POSITION;
// #endif
    
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
    float C = cos( angle );
    float S = sin( angle );
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
    float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
    return mul( finalMatrix, original ) + center;
}


VertexOutput vert(VertexInput input, uint instanceID : SV_InstanceID)
{
    VertexOutput o;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o)
    TRANSFER_GI_DATA(input, o);
    
#ifdef _GPU_RESULT
    unity_ObjectToWorld = positionBuffer[instanceID];
    unity_WorldToObject = unity_ObjectToWorld;
    unity_WorldToObject._14_24_34 *= -1;
    unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
#endif
    
    float3 worldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
    float2 worldSpaceUVs = float2(worldPos.x, worldPos.z);
    
#if defined(_ENABLED_WINDZONE)
    float offset = 0.1 * _Time.y * _NoisePannerSpeed;
    
    float4 animateWorldNoise = SAMPLE_TEXTURE2D_LOD(_NoiseTexture, sampler_point_clamp, float2(worldSpaceUVs * _NoiseTextureTilling.zw + offset), 0);
    
    float windyRadians = radians((_WindDir + (_WindDirOffset * (-1.0 + (animateWorldNoise.r - 0.0) * (1.0 - -1.0) / (1.0 - 0.0)))) * -1.0);
    float3 rotate = float3(cos(windyRadians), 0.0, sin(windyRadians));
    float3 objectDir = mul(unity_WorldToObject, float4(rotate, 1.0)).xyz;
    float3 objectPos = mul(unity_WorldToObject, float4(float3(0, 0, 0), 1.0)).xyz;
 
    float3 rotaionAxis = normalize(objectDir - objectPos);
    
    float4 staticWorldNoise = SAMPLE_TEXTURE2D_LOD(_NoiseTexture, sampler_point_clamp, float2(worldSpaceUVs * _NoiseTextureTilling.xy), 0);
    
    float amplitudeResult = _Amplitude + _AmplitudeOffset * staticWorldNoise.r;
    float frequencyResult = _Time.y * (_Frequency + (_FrequencyOffset * staticWorldNoise.r)); 
    float waveType = sin(((worldPos.x + worldPos.z) + frequencyResult) * _Phase);
    
    float rotationAngle = radians(((amplitudeResult * waveType) + _DefaultBending) * (input.positionOS.y / _MaxHeight));

    float3 center = float3(0.0, input.positionOS.y, 0.0);
    float3 rotatedValueCenter  = RotateAroundAxis(center, input.positionOS.xyz, rotaionAxis, rotationAngle);
    float3 rotatedValueZero = RotateAroundAxis(float3(0.0, 0.0, 0.0), rotatedValueCenter, rotaionAxis, rotationAngle);
    float3 localVertexOffset = (rotatedValueZero - input.positionOS.xyz) * step(0.01, input.positionOS.y);
    
    input.positionOS.xyz += localVertexOffset;
    input.positionOS.w = 1;
#endif
    
    // float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 positionWS = mul(unity_ObjectToWorld, float4(input.positionOS.xyz, 1.0));
    o.positionCS = TransformWorldToHClip(positionWS);
    o.normalWS = TransformObjectToWorldNormal(input.normalOS);
    o.worldPos = positionWS.xyz;
    o.positionWS = positionWS;
    
#if defined(_DETAIL_MAP)
    o.detailUV = TransformDetailUV(input.baseUV);
#endif

#if defined(_NORMAL_MAP)
    o.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif

    o.baseUV = TransformBaseUV(input.baseUV);
    return o;
}

float4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GetInputConfig(input.positionCS, input.baseUV);

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

    Surface surface;
    surface.position = input.positionWS;
    
#if defined(_NORMAL_MAP)
    surface.normal = NormalTangentToWorld(GetNormalTS(config), input.normalWS, input.tangentWS);
    surface.interpolatedNormal = input.normalWS;
#else
    surface.normal = normalize(input.normalWS);
    surface.interpolatedNormal = surface.normal;
#endif

    surface.depth = -TransformWorldToView(input.positionWS).z;
    surface.color = col.rgb;
    surface.alpha = col.a;

    surface.metallic = GetMetallic(config);
    surface.smoothness = GetSmoothness(config);
    surface.occlusion = GetOcclusion(config);
    surface.fresnelStrength = GetFresnel(config);
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
    finalColor += GetEmission(config);
    
    return float4(finalColor, GetFinalAlpha(surface.alpha));
}

#endif
