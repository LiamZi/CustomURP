#ifndef  __SHADER_LIBRARY_SPACE_TRANSFORMS_HLSL__
#define  __SHADER_LIBRARY_SPACE_TRANSFORMS_HLSL__

#include  "Common.hlsl"

float3 ScreenToNDC(float3 screenPos)
{
    float3 posNDC = float3(screenPos.xy / _ScreenParams.xy, screenPos.z);
#if UNITY_UV_STARTS_AT_TOP
    if (!(_ProjectionParams.x < 0))
    {
        posNDC.y = 1 - posNDC.y;
    }
#endif
    return posNDC;
}

#endif