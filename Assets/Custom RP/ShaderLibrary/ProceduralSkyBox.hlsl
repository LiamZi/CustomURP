#ifndef __SHADER_LIBRARY_PROCEDURAL_SKY_BOX_HLSL__
#define __SHADER_LIBRARY_PROCEDURAL_SKY_BOX_HLSL__

#include "SkyBoxFunctions.hlsl"

struct VertexInput
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS: SV_POSITION;
    float3 positionWS: TEXCOORD1;
    float3 moonPos: TEXCOORD2;
    float3 positionOS: TEXCOORD3;
    float3 milkyWayPos: TEXCOORD4;
    float3 eyeRay : TEXCOORD5;

    UNITY_VERTEX_OUTPUT_STEREO
};

struct VertexPositionInputs
{
    float3 positionWS; // World space position
    float3 positionVS; // View space position
    float4 positionCS; // Homogeneous clip space position
    float4 positionNDC;// Homogeneous normalized device coordinates
};

VertexPositionInputs GetVertexPositionInputs(float3 positionOS)
{
    VertexPositionInputs input;
    input.positionWS = TransformObjectToWorld(positionOS);
    input.positionVS = TransformWorldToView(input.positionWS);
    input.positionCS = TransformWorldToHClip(input.positionWS);

    float4 ndc = input.positionCS * 0.5f;
    input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    input.positionNDC.zw = input.positionCS.zw;

    return input;
}

VertexOutput vert(VertexInput input)
{
    VertexOutput o;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    o.positionCS = TransformWorldToHClip(o.positionWS);
    o.positionOS = input.positionOS.xyz;
    
    o.moonPos = mul((float3x3)_MoonWorld2Obj, input.positionOS.xyz) * 6;
    o.moonPos.x *= -1;
    o.eyeRay = normalize(mul((float3x3)unity_ObjectToWorld, input.positionOS.xyz));
    
    o.milkyWayPos = mul((float3x3)_MilkyWayWorld2Local, input.positionOS.xyz) * _MilkyWayTex_ST.xyz;
    
    return o;
}

half4 frag(VertexOutput input) : SV_Target
{
    float3 normalizePosWS = normalize(input.positionOS);
    float2 sphereUV = float2(atan2(normalizePosWS.x, normalizePosWS.z) / TWO_PI, asin(normalizePosWS.y) / HALF_PI);


    float kKrESun = kRAYLEIGH * kSUN_BRIGHTNESS;
    float kKr4PI = kRAYLEIGH * 4.0 * 3.14159265;
    
    float3 kSkyTintInGammaSpace = pow(_SkyTint, 1.0 / 2.2) ; // convert tint from Linear back to Gamma
    float3 kScatteringWavelength = lerp (
        kDefaultScatteringWavelength-kVariableRangeForScatteringWavelength,
        kDefaultScatteringWavelength+kVariableRangeForScatteringWavelength,
        half3(1,1,1) - kSkyTintInGammaSpace);
        // _SunCol.rgb);// using Tint in sRGB gamma allows for more visually linear interpolation and to keep (.5) at (128, gray in sRGB) point
    float3 kInvWavelength = 1.0 / pow(kScatteringWavelength, 4);

    float3 cameraPos = float3(0,kInnerRadius + kCameraHeight,0);
    float height = kInnerRadius + kCameraHeight;
    float depth = exp(kScaleOverScaleDepth * (-kCameraHeight));
    
    half4 sun = CalcSunAttenuation(normalizePosWS, -_SunDirectionWS) * _SunIntensity * _SunCol;
    half4 scattering = smoothstep(0.5, 1.5, dot(normalizePosWS, -_SunDirectionWS.xyz)) * _SunCol * _ScatteringIntensity;
    half scatteringInstensity = max(0.15, smoothstep(0.6, 0.0, -_SunDirectionWS.y));
    // scattering *= float4(exp(-clamp(scatteringInstensity, 0.0, 50.0) * (kInvWavelength * kKr4PI + kKm4PI)) , 1.0);
    // scattering *= scatteringInstensity;
    scattering *= float4(exp(-clamp(scatteringInstensity, 0.0, 50.0) * (kInvWavelength * kKr4PI + kKm4PI)) , 1.0);
    
    // sun += (scattering * depth * height);
    sun += scattering;
    
    half4 skyColor = SAMPLE_TEXTURE2D(_SkyGradientTex, sampler_SkyGradientTex, float2(sphereUV.y, 0.5));
    half4 skyScattering = float4(exp(-clamp(scatteringInstensity, 0.0, 50.0) * (kInvWavelength * kKr4PI + kKm4PI)) , 1.0);
    skyScattering *= skyScattering * depth * height;
    half3 cIn = skyScattering * ( skyColor * kInvWavelength * kKrESun);
    skyColor = _Exposure * (cIn.rgbr * GetRayleighPhase(-_MoonDirectionWS.xyz, -input.eyeRay.xyz));
    // skyColor = GetRayleighPhase(_MoonDirectionWS.xyz, -input.eyeRay);

#if defined(_ENABLED_STAR)    
    float star = SAMPLE_TEXTURE2D(_StarTex, sampler_StarTex, sphereUV).r;
    star = saturate(star * star * star * 3) * _StarIntensity;
#else
    float star = 0.0;
#endif

#if defined(_ENABLED_MOON)    
    half4 moon = SAMPLE_TEXTURE2D(_MoonTex, sampler_linear_clamp_MoonTex, (input.moonPos.xy + 0.5)) * step(0.5, dot(normalizePosWS, -_MoonDirectionWS.xyz));
    half4 moonScattering = smoothstep(0.97, 1.3, dot(normalizePosWS, -_MoonDirectionWS.xyz));
    moon = (moon * _MoonIntensity + moonScattering * 0.8) * _MoonCol;
#else
    half4 moon = half4(0.0, 0.0, 0.0, 0.0);
#endif

    half4 milkyWayTex = SAMPLE_TEXTURE2D(_MilkyWayTex, sampler_MilkyWayTex, (input.milkyWayPos.xy + 0.5));
    half milkyWay = smoothstep(0, 0.7, milkyWayTex.r);
    
    half noiseMove1 = SAMPLE_TEXTURE2D(_MilkyWayNoise, sampler_MilkyWayNoise, (input.milkyWayPos.xy + 0.5) * _MilkyWayNoise_ST.xy + _MilkyWayNoise_ST.zw + float2(0, _Time.y * _FlowSpeed)).r;
    half noiseMove2 = SAMPLE_TEXTURE2D(_MilkyWayNoise, sampler_MilkyWayNoise, (input.milkyWayPos.xy + 0.5) * _MilkyWayNoise_ST.xy - _MilkyWayNoise_ST.zw - float2(0, _Time.y * _FlowSpeed)).r;
    half noiseStatic = SAMPLE_TEXTURE2D(_MilkyWayNoise, sampler_MilkyWayNoise, (input.milkyWayPos.xy + 0.5) * _MilkyWayNoise_ST.xy * 0.5).r;
                
    
    milkyWay *= smoothstep(-0.2, 0.8, noiseStatic + milkyWay);
    milkyWay *= smoothstep(-0.4, 0.8, noiseStatic);
    noiseMove1 = smoothstep(0.0, 1.2, noiseMove1);
    
    half milkyWay1 = milkyWay;
    half milkyWay2 = milkyWay;
    milkyWay1 -= noiseMove1 * (smoothstep(0.4, 1, milkyWayTex.g) + 0.4);
    milkyWay2 -= noiseMove2 * (smoothstep(0.4, 1, milkyWayTex.g) + 0.4);

    milkyWay1 = saturate(milkyWay1);
    milkyWay2 = saturate(milkyWay2);

    half3 milkyWayCol1 = milkyWay1 * _MilkyWayColor1.rgb * _MilkyWayColor1.a;
    half3 milkyWayCol2 = milkyWay2 * _MilkyWayColor2.rgb * _MilkyWayColor2.a;

    half milkyStar;
    half cell;
    VoronoiNoise(sphereUV, 20, 200, milkyStar, cell);
    
    milkyStar = pow(1 - saturate(milkyStar), 50) * (smoothstep(0.2, 1, milkyWayTex.g) + milkyWayTex.r * 0.5) * 3;
    
    half3 milkywayBG = smoothstep(0.1, 1.5, milkyWayTex.r) * _MilkyWayColor2.rgb * 0.2;

#if defined(_ENABLED_MILKYWAY)    
    half3 milkyCol = (SoftLight(milkyWayCol1, milkyWayCol2) + SoftLight(milkyWayCol2, milkyWayCol1)) * 0.5 * _MilkywayIntensity + milkywayBG + milkyStar;
    milkyCol *= _MilkywayIntensity;
#else
    half3 milkyCol = half3(0.0, 0.0, 0.0);
#endif

    half4 col = skyColor + sun + star + moon + milkyCol.rgbr;
    // half4 col = skyColor + sun + moon + milkyCol.rgbr;
    return sqrt(col);
}

#endif