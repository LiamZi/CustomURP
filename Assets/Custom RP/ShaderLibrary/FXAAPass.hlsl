#ifndef __SHADER_LIBRARY_FXAA_PASS_HLSL__
#define __SHADER_LIBRARY_FXAA_PASS_HLSL__

float4 _FXAAConfig;

struct LumaNeighborhood
{
    float m, n, e, s, w;
    float highest, lowest, range;
};

float GetLuma(float2 uv, float uOffset = 0.0, float vOffset = 0.0)
{
    uv += float2(uOffset , vOffset) * GetSourceTexelSize().xy;

#if defined(FXAA_ALPHA_CONTAINS_LUMA)
    return GetSource(uv).a;
#else
    return GetSource(uv).g;
#endif
}

/////////////////////////////
// +1        N
// +0     W  M  E
// -1        S
//       -1  0  1
/////////////////////////////

LumaNeighborhood GetLumaNeighborhood(float2 uv)
{
    LumaNeighborhood luma;
    luma.m = GetLuma(uv);
    luma.n = GetLuma(uv, 0.0, 1.0);
    luma.e = GetLuma(uv, 1.0, 0.0);
    luma.s = GetLuma(uv, 0.0, -1.0);
    luma.w = GetLuma(uv, -1.0, 0.0);
    luma.highest = max(max(max(max(luma.m, luma.n), luma.e), luma.s), luma.w);
    luma.lowest = min(min(min(min(luma.m, luma.n), luma.e), luma.s), luma.w);
    luma.range = luma.highest - luma.lowest;
    return luma;
}

bool CanSkipFXAA(LumaNeighborhood luma)
{
    return luma.range <  max(_FXAAConfig.x, _FXAAConfig.y * luma.highest);
}


float4 FXAAPassFragment(Varyings input) : SV_TARGET
{
    LumaNeighborhood luma = GetLumaNeighborhood(input.screenUV);
    if(CanSkipFXAA(luma)) return 0.0;

    return luma.m;
}

#endif