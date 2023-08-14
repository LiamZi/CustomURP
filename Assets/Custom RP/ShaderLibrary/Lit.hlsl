#ifndef __CUSTOM_RP_SHADER_LIT_HLSL__
#define __CUSTOM_RP_SHADER_LIT_HLSL__

// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Common.hlsl"
#include "Surface.hlsl"
#include "Shadows.hlsl"
#include "Light.hlsl"
#include "BRDF.hlsl"
#include "GI.hlsl"
#include "Lighting.hlsl"


// CBUFFER_START(UnityPerDraw)
// 	// float4x4 unity_ObjectToWorld;
// 	// float4 unity_LightIndicesOffsetAndCount;
//     float4 unity_LightData;
// 	float4 unity_LightIndices[2];
//     // float4 unity_4LightIndices0, unity_4LightIndices1;
// CBUFFER_END

// CBUFFER_START(UnityPerFrame)
//     float4x4 unity_MatrixVP;
// CBUFFER_END



// CBUFFER_START(_LightBUffer)
//     float4 _VisibleLightColors[MAX_VISIBLE_LIGHTS];
//     float4 _VisibleLightDirectionsOrPositions[MAX_VISIBLE_LIGHTS];
//     float4 _VisibleLightAttenuations[MAX_VISIBLE_LIGHTS];
//     float4 _VisibleLightSpotsDirections[MAX_VISIBLE_LIGHTS];
// CBUFFER_END

// float3 DiffuseLight(int index, float3 normal, float3 worldPos)
// {
//     float3 lightColor = _VisibleLightColors[index].rgb;
//     float4 lightDirOrPos = _VisibleLightDirectionsOrPositions[index];
//     float4 lightAttenuation = _VisibleLightAttenuations[index];
//     float3 lightSpotDir = _VisibleLightSpotsDirections[index].xyz;

//     float3 lightVector = lightDirOrPos - worldPos * lightDirOrPos.w;
//     float3 direction = normalize(lightVector);
//     float diffuse = saturate(dot(normal, direction));

//     float rangeFade = dot(lightVector, lightVector) * lightAttenuation.x;
//     rangeFade = saturate(1.0 - rangeFade * rangeFade);
//     rangeFade *= rangeFade;

//     float spotFade = dot(lightSpotDir, direction);
//     spotFade = saturate(spotFade * lightAttenuation.z + lightAttenuation.w);
//     spotFade *= spotFade;


//     float distanceSqr = max(dot(lightVector, lightVector), 0.00001);
//     diffuse *= spotFade * rangeFade / distanceSqr;
//     return diffuse * lightColor;
// }

// #define UNITY_MATRIX_M unity_ObjectToWorld
// #include "../ShaderLibrary/UnityInput.hlsl"

// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(PerInstance)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float, _CutOff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_INSTANCING_BUFFER_END(PerInstance)

struct VertexInput
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float3 normalWS : VAR_NORMAL;
    float2 baseUV : VAR_BASE_UV;
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
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _BaseMap_ST);
    o.baseUV = input.baseUV * baseST.xy + baseST.zw;
    return o;
}

float4 frag(VertexOutput input) : SV_TARGET
{

    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color);
    float4 col = baseMap * baseColor;
#if defined(_CLIPPING)
        clip(col.a - UNITY_ACCESS_INSTANCED_PROP(PerInstance, _CutOff));
#endif

    Surface surface;
    surface.position = input.positionWS;
    surface.normal = normalize(input.normalWS);
    surface.depth = -TransformWorldToView(input.positionWS).z;
    surface.color = col.rgb;
    surface.alpha = col.a;
    surface.metallic = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Metallic);
    surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Smoothness);
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);

#if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface, true);
#else
    BRDF brdf = GetBRDF(surface);
#endif

    GI gi = GetGI(GI_FRAGMENT_DATA(input));
    return float4(GetLighting(surface, brdf, gi), surface.alpha);
    // return float4(surface.color, surface.alpha);

    // UNITY_SETUP_INSTANCE_ID(input);
    // half3 rgb = abs(length( input.normalWS) - 1.0) * 20.0;
    // half3 rgb = normalize(input.normalWS);
    // half3 rgb = input.normalWS;
    // return half4(rgb, 1.0);
    // input.normal = normalize(input.normal);

    // float3 albedo =  UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color).rgb;
    // float3 diffuse = 0;
    
    // for(int i = 0; i < min(unity_LightData.y, 8); i++)    
    // // for(int i = 0; i < 5; i++)               
    // {
    //     int lightIndex = unity_LightIndices[i / 4][i % 4];
    //     // int lightIndex = unity_4LightIndices0[i];

    //     diffuse += DiffuseLight(lightIndex, input.normal, input.worldPos);
    // }

    // // for(int i = 4; i < min(unity_LightData.y, 8); i++)
    // // {
    // //     int lightIndex = unity_LightIndices[i - 4];
    // //     diffuse += DiffuseLight(lightIndex, input.normal, input.worldPos);
    // // }

    // float3 color = diffuse * albedo;
    // return half4(color, 1);
}

#endif