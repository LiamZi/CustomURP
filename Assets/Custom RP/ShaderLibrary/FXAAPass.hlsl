#ifndef __SHADER_LIBRARY_FXAA_PASS_HLSL__
#define __SHADER_LIBRARY_FXAA_PASS_HLSL__

float GetLuma(float2 uv)
{
    // return Luminance(GetSource(uv));
#if defined(FXAA_ALPHA_CONTAINS_LUMA)
    return GetSource(uv).a;
#else
    return GetSource(uv).g;
#endif
}

float4 FXAAPassFragment(Varyings input) : SV_TARGET
{
    return sqrt(GetLuma(input.screenUV));
}

#endif