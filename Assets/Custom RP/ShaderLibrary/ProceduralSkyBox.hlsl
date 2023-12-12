#ifndef __SHADER_LIBRARY_PROCEDURAL_SKY_BOX_HLSL__
#define __SHADER_LIBRARY_PROCEDURAL_SKY_BOX_HLSL__

#include "SkyBoxFunctions.hlsl"

static const float PI2 = PI * 2;
static const float halfPI = PI * 0.5;

struct VertexInput
{
    float4 positionOS : POSITION;
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD1;
    float3 positionOS : TEXCOORD3;
    float2 moonPos : TEXCOORD2;
    float3 milkyWayPos : TEXCOORD4;
};


VertexOutput vert(VertexInput input)
{
    VertexOutput o;
    o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    o.positionCS = TransformWorldToHClip(o.positionWS);
    o.positionOS = input.positionOS.xyz;
    
    o.moonPos = mul((float3x3)_MoonWorld2Obj, input.positionOS.xyz) * 6;
    o.moonPos.x *= -1;

    o.milkyWayPos = mul((float3x3)_MilkyWayWorld2Local, input.positionOS.xyz) * _MilkyWayTex_ST.xyz;
    
    return o;
}

half4 frag(VertexOutput input) : SV_Target
{
    float3 normalizePosWS = normalize(input.positionOS);
    // float2 sphereUV = float2(atan2(normalizePosWS.x, normalizePosWS.z) / TWO_PI, asin(positionWS.y) / HALF_PI);
    // float2 sphereUV = float2(atan2(normalizePosWS.x, normalizePosWS.z) / TWO_PI, asin(normalizePosWS.y) / HALF_PI);
    float2 sphereUV = float2(atan2(normalizePosWS.x, normalizePosWS.z) / PI2, asin(normalizePosWS.y) / halfPI);

    
    //This is sun's algorithm comes from unity.
    half4 sun = CalcSunAttenuation(normalizePosWS, -_SunDirectionWS) * _SunIntensity * _SunColor;
    half4 scattering = smoothstep(0.5, 1.5, dot(normalizePosWS, -_SunDirectionWS.xyz)) * _SunColor * _ScatteringIntensity;
    half scatteringInstensity = max(0.15, smoothstep(0.6, 0.0, -_SunDirectionWS.y));
    scattering *= scatteringInstensity;
    
    sun += scattering;

    half4 skyColor = SAMPLE_TEXTURE2D(_SkyGradientTex, sampler_SkyGradientTex, float2(sphereUV.y, 0.5));

    float star = SAMPLE_TEXTURE2D(_StarTex, sampler_StarTex, sphereUV).r;
    star = saturate(star * star * star * 3) * _StarIntensity;

    // half4 moon = SAMPLE_TEXTURE2D(_MoonTex, sampler_MoonTex, (input.moonPos.xy + 0.5)) * step(0.5, dot(positionWS, -_MoonDirectionWS.xyz));
    half4 moon = SAMPLE_TEXTURE2D(_MoonTex, sampler_MoonTex, (input.moonPos.xy + 0.5)) * step(0.5, dot(normalizePosWS, -_MoonDirectionWS.xyz));

    // half4 moonScattering = smoothstep(0.97, 1.3, dot(positionWS, -_MoonDirectionWS.xyz));
    half4 moonScattering = smoothstep(0.97, 1.3, dot(normalizePosWS, -_MoonDirectionWS.xyz));

    // moon = (moon * _MoonIntensity + moonScattering * 0.8) * _MoonColor;
    moon = (moon * _MoonIntensity + moonScattering * 0.8) * _MoonColor;
    
    
    return skyColor + sun + star + moon;
}

#endif