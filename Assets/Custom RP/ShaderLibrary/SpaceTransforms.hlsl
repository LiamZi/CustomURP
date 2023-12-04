#ifndef  __SHADER_LIBRARY_SPACE_TRANSFORMS_HLSL__
#define  __SHADER_LIBRARY_SPACE_TRANSFORMS_HLSL__

#include  "Common.hlsl"

float3 ScreenToNDC(float3 pos)
{
    float3 ndc = float3(pos.xy / _ScreenParams.xy, pos.z);
#if UNITY_UV_STARTS_AT_TOP
    if(!(_ProjectionParams.x < 0))
    {
        ndc.y = 1 - ndc.y;
    }
#endif
    return ndc;
}

#endif