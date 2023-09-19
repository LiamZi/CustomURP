#ifndef __SHADER_LIBRARY_POST_FX_STACK_PASSES_HLSL__
#define __SHADER_LIBRARY_POST_FX_STACK_PASSES_HLSL__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"


TEXTURE2D(_PostFXSource);
TEXTURE2D(_PostFXSource2);
SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);

TEXTURE2D(_ColorGradingLUT);

float4 _PostFXSource_TexelSize;

bool _BloomBicubicUpSampling;
float4 _BloomThreshold;
float _BloomIntensity;

float4 _ColorAdjustments;
float4 _ColorFilter;
float4 _WhiteBalance;
float4 _SplitToningShadows;
float4 _SplitToningHighlights;
float4 _ChannelMixerRed;
float4 _ChannelMixerGreen;
float4 _ChannelMixerBlue;
float4 _SMHShadows;
float4 _SMHMidtones;
float4 _SMHHighlights; 
float4 _SMHRange;
float4 _ColorGradingLUTParameters;
bool _ColorGradingLUTInLogC;
// float _FinalSrcBlend;
// float _FinalDstBlend;


struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};  


float4 GetSource(float2 screenUV)
{
    // return SAMPLE_TEXTURE2D(_PostFXSource, sampler_linear_clamp, screenUV);
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource, sampler_linear_clamp, screenUV, 0);
}

float4 GetSource2(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource2, sampler_linear_clamp, screenUV, 0);
}

float4 GetSourceTexelSize()
{
    return _PostFXSource_TexelSize;
}

float4 GetSourceBicubic(float2 screenUV)
{
    return SampleTexture2DBicubic(TEXTURE2D_ARGS(_PostFXSource, sampler_linear_clamp), screenUV, _PostFXSource_TexelSize.zwxy, 1.0, 1.0);
}


Varyings DefaultPassVertex(uint vertexID : SV_VERTEXID)
{
    Varyings o;
    o.positionCS = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0 : -1.0, 0.0, 1.0); 
    o.screenUV = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);
    if(_ProjectionParams.x < 0.0)
    {
        o.screenUV.y = 1.0 - o.screenUV.y;
    }
    return o;
}

float4 CopyPassFragment(Varyings input) : SV_TARGET
{
    return GetSource(input.screenUV);
}

float4 BloomHorizontalPassFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    float offsets[] = { -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0 };

    float weights[] = { 0.01621622, 0.05405405, 0.12162162, 
                        0.19459459, 0.22702703, 0.19459459, 
                        0.12162162, 0.05405405, 0.01621622 };
    
    for(int i = 0; i < 9; ++i)
    {
        float offset = offsets[i] * 2.0 * GetSourceTexelSize().x;
        color += GetSource(input.screenUV + float2(offset, 0.0)).rgb * weights[i];
    }

    return float4(color, 1.0);
}

float4 BloomVerticalPassFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    // float offsets[] = { -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0 };
    float offsets[] = { -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923 };

    // float weights[] = { 0.01621622, 0.05405405, 0.12162162, 
    //                     0.19459459, 0.22702703, 0.19459459, 
    //                     0.12162162, 0.05405405, 0.01621622 };
    float weights[] = { 0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027 };
    
    for(int i = 0; i < 5; i++)
    {
        float offset = offsets[i] * GetSourceTexelSize().y;
        color += GetSource(input.screenUV + float2(0.0, offset)).rgb * weights[i];
    }

    return float4(color, 1.0);
}

float4 BloomCombinePassFragment(Varyings input) : SV_TARGET
{
    // float3 lowRes = GetSourceBicubic(input.screenUV).rgb;
    float3 lowRes;
    if(_BloomBicubicUpSampling)
    {
        lowRes = GetSourceBicubic(input.screenUV).rgb;
    }
    else
    {
        lowRes = GetSource(input.screenUV).rgb;
    }
    float4 highRes = GetSource2(input.screenUV);
    return float4(lowRes * _BloomIntensity + highRes.rgb, highRes.a);
}


// w=(max(s,b-t))/(max(b,0.00001))
// s=(min(max(0,b-t+tk),2tk)^2)/(4tk+0.00001)
float3 ApplyBloomThreshold(float3 color)
{
    float b = Max3(color.r, color.g, color.b);
    float s = b + _BloomThreshold.y;
    s = clamp(s, 0.0, _BloomThreshold.z);
    s = s * s * _BloomThreshold.w;

    float contribution = max(s, b - _BloomThreshold.x);
    contribution /= max(b, 0.00001);
    return color * contribution;
}

float4 BloomPrefilterPassFragment(Varyings input) : SV_TARGET
{
    float3 color = ApplyBloomThreshold(GetSource(input.screenUV).rgb);
    return float4(color, 1.0);
}

// A sample's weight is 1/(l+1) with l its luminance. thus or luminance 0 the weight is 1, for luminance 1 the weight is 1/2, 
// for 3 its 1/4 ..., and so on,
float4 BloomPrefilterFirefliesPassFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    float weightSum = 0.0;

    float2 offsets[] = { float2(0.0, 0.0), float2(-1.0, -1.0), float2(-1.0, 1.0), 
                         float2(1.0, -1.0), float2(1.0, 1.0) };
                        //  float2(-1.0, 0.0), float2(1.0, 0.0), float2(0.0, -1.0), float2(0.0, 1.0) };

    for(int i = 0; i < 5; i++)
    {
        float3 c = GetSource(input.screenUV + offsets[i] * GetSourceTexelSize().xy * 2.0).rgb;
        c = ApplyBloomThreshold(c);
        float w = 1.0 / (Luminance(c) + 1.0);
        color += c * w;
        weightSum += w;
    }            

    // color *= 1.0 / 9.0;
    color /= weightSum;
    return float4(color, 1.0);
}

float4 BloomScatterPassFragment(Varyings input) : SV_TARGET
{
    float3 lowRes;
    if(_BloomBicubicUpSampling)
    {
        lowRes = GetSourceBicubic(input.screenUV).rgb;
    }
    else
    {
        lowRes = GetSource(input.screenUV).rgb;
    }
    float3 highRes = GetSource2(input.screenUV).rgb;
    return float4(lerp(highRes, lowRes, _BloomIntensity), 1.0);
}

float4 BloomScatterFinalPassfragment(Varyings input) : SV_TARGET
{
    float3 lowRes;
    if(_BloomBicubicUpSampling)
    {
        lowRes = GetSourceBicubic(input.screenUV).rgb;
    }
    else
    {
        lowRes = GetSource(input.screenUV).rgb;
    }

    float4 highRes = GetSource2(input.screenUV);
    lowRes += highRes.rgb - ApplyBloomThreshold(highRes.rgb);
    return float4(lerp(highRes.rgb, lowRes, _BloomIntensity), highRes.a);
}

float Luminance(float3 color, bool useACES)
{
    return useACES ?  AcesLuminance(color) : Luminance(color);
}

float3 ColorGradePostExposure(float3 color)
{
    return color * _ColorAdjustments.x;
}

float3 ColorGradingWhiteBalance(float3 color)
{
    color = LinearToLMS(color);
    color *= _WhiteBalance.rgb;
    return LMSToLinear(color);
}

float3 ColorGradingContrast(float3 color, bool useACES)
{
    color = useACES ? ACES_to_ACEScc(unity_to_ACES(color)) : LinearToLogC(color);
    color = (color - ACEScc_MIDGRAY) * _ColorAdjustments.y + ACEScc_MIDGRAY;
    return useACES ? ACES_to_ACEScg(ACEScc_to_ACES(color)) : LogCToLinear(color);
}

float3 ColorGradeColorFilter(float3 color)
{
    return color * _ColorFilter.rgb;
}

float3 ColorGradingHueShift(float3 color)
{
    color = RgbToHsv(color);
    float hue = color.x + _ColorAdjustments.z;
    color.x = RotateHue(hue, 0.0, 1.0);
    return HsvToRgb(color);
}

float3 ColorGradingSaturation(float3 color, bool useACES)
{
    float luminance = Luminance(color, useACES);
    color = (color - luminance) * _ColorAdjustments.w + luminance;
    return color;
}

float3 ColorGradingSplitToning(float3 color, bool useACES)
{
    color = PositivePow(color, 1.0 / 2.2);
    float t = saturate(Luminance(saturate(color), useACES) + _SplitToningShadows.w);
    float3 shadows = lerp(0.5, _SplitToningShadows.rgb, 1.0 - t);
    float3 highlights = lerp(0.5, _SplitToningHighlights.rgb, t) ;
    color = SoftLight(color, shadows);
    color = SoftLight(color, highlights);
    return PositivePow(color, 2.2);
}

float3 ColorGradingChannelMixer(float3 color)
{
    return mul(float3x3(_ChannelMixerRed.rgb, _ChannelMixerGreen.rgb, _ChannelMixerBlue.rgb), color);
}

float3 ColorGradingShadowMidtonesHighligths(float3 color, bool useACES)
{
    float luminance = Luminance(color, useACES);
    float shadowsWeight = 1.0 - smoothstep(_SMHRange.x, _SMHRange.y, luminance);
    float highlightsWeight = smoothstep(_SMHRange.z, _SMHRange.w, luminance);
    float midtonesWeight = 1.0 - shadowsWeight - highlightsWeight;
    return color * _SMHShadows.rgb * shadowsWeight + color * _SMHMidtones.rgb * midtonesWeight + color * _SMHHighlights.rgb * highlightsWeight;
}

float3 ColorGrade(float3 color, bool useACES)
{
    // color = min(color, 60.0);
    color = ColorGradePostExposure(color);
    color = ColorGradingWhiteBalance(color);
    color = ColorGradingContrast(color, useACES);
    color = ColorGradeColorFilter(color);
    color = max(color, 0.0);
    color = ColorGradingSplitToning(color, useACES);
    color = ColorGradingChannelMixer(color);
    color = max(color, 0.0);
    color = ColorGradingShadowMidtonesHighligths(color, useACES);
    color = ColorGradingHueShift(color);
    color = ColorGradingSaturation(color, useACES);
    return max(useACES ? ACEScg_to_ACES(color) : color, 0.0);
}


float3 GetColorGradeLUT(float2 uv, bool useACES = false)
{
    // float3 color = float3(uv, 0.0);
    float3 color = GetLutStripValue(uv, _ColorGradingLUTParameters);
    return ColorGrade(_ColorGradingLUTInLogC ?  LogCToLinear(color) : color, useACES);
}

float3 ApplyColorGradingLUT(float3 color)
{
    // return ApplyLut2D(TEXTURE2D_ARGS(_ColorGradingLUT, sampler_linear_clamp), saturate(_ColorGradingLUTInLogC ? LinearToLogC(color) : color), _ColorGradingLUTParameters.xyz);
	return ApplyLut2D(TEXTURE2D_ARGS(_ColorGradingLUT, sampler_linear_clamp), saturate(_ColorGradingLUTInLogC ? LinearToLogC(color) : color), _ColorGradingLUTParameters.xyz);
}

//ToneMapping Reinhard  c/(1+c).  c is color 
//the alternative function c(1+c/w^2)/(1+c), where w is the white point.
// float4 ToneMappingReinhardPassFragment(Varyings input) : SV_TARGET
float4 ColorGradingReinhardPassFragment(Varyings input) : SV_TARGET
{
    // float4 color = GetSource(input.screenUV);
    // // color.rgb = min(color.rgb, 60.0);
    // color.rgb = ColorGrade(color.rgb, false);
    // color.rgb /= color.rgb + 1.0;
    // return color;
    float3 color = GetColorGradeLUT(input.screenUV);
	color /= color + 1.0;
	return float4(color, 1.0);
}

//ToneMapping Neutral t(x)=(x(ax+cb)+de)/(x(ax+b)+df)-e/f The final color is then (t(ce))/(t(w))
// In this case x is an input color channel and the other values are constants that configure the curve.
// float4 ToneMappingNeutralPassFragment(Varyings input) : SV_TARGET
float4 ColorGradingNeutralPassFragment(Varyings input) : SV_TARGET
{
    // float4 color = GetSource(input.screenUV);
    // // color.rgb = min(color.rgb, 60.0);
    //  color.rgb = ColorGrade(color.rgb, false);
    float3 color = GetColorGradeLUT(input.screenUV);
    color = NeutralTonemap(color);
    return float4(color, 1.0);
}

// float4 ToneMappingACESPassFragment(Varyings input) : SV_TARGET
float4 ColorGradingACESPassFragment(Varyings input) : SV_TARGET
{
    // float4 color = GetSource(input.screenUV);
    // // color.rgb = min(color.rgb, 60.0f);
    // color.rgb = ColorGrade(color.rgb, true);
    // color.rgb = AcesTonemap(unity_to_ACES(color.rgb));
    // return color;

    float3 color = GetColorGradeLUT(input.screenUV, true);
    // color = AcesTonemap(unity_to_ACES(color));
    color = AcesTonemap(color);
    return float4(color, 1.0);
}

// float4 ToneMappingNonePassFragment(Varyings input) : SV_TARGET
float4 ColorGradingNonePassFragment(Varyings input) : SV_TARGET
{
    // float4 color = GetSource(input.screenUV);
    // color.rgb = ColorGrade(color.rgb, false);
    float3 color = GetColorGradeLUT(input.screenUV);

    return float4(color, 1.0);
}


float4 FinalPassFragment(Varyings input) : SV_TARGET
{
    float4 color = GetSource(input.screenUV);
    color.rgb = ApplyColorGradingLUT(color.rgb);
    return color;
}




#endif