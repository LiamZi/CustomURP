#ifndef __SHADER_LIBRARY_CLOUD_NORMAL_RAR_MARCH_HLSL__
#define __SHADER_LIBRARY_CLOUD_NORMAL_RAR_MARCH_HLSL__

#include "CloudFunctions.hlsl"

float GetDensity(float3 startPos, float3 dir, float maxSampleDistance,
                int sampleCount, float raymarchOffset, float4 worldLightPos,
                out float intensity, out float depth)
{
    float sampleStart, sampleEnd;
    if(!ResolveRayStartEnd(startPos, dir, sampleStart, sampleEnd))
    {
        intensity = 0.0;
        depth = 1e6;
        return 0.0;
    }

    sampleEnd = min(maxSampleDistance, sampleEnd);
    float sampleStep = min((sampleEnd - sampleStart) / sampleCount, 1000);
    float3 sampleStartPos = startPos + dir * sampleStart;
    if(sampleEnd <= sampleStart ||sampleStartPos.y < -200)
    {
        intensity = 0.0;
        depth = 1e6;
        return 0.0;
    }

    float raymarchDistance = sampleStart + raymarchOffset * sampleStep;

    RaymarchStatus result;
    InitRayMarchStatus(result);

    UNITY_LOOP
    for(int j = 0; j < sampleCount; j++, raymarchDistance += sampleStep)
    {
        if(raymarchDistance > maxSampleDistance)
            break;

        float3 rayPos = startPos + dir * raymarchDistance;
        IntegrateRaymarch(startPos, rayPos, dir, sampleStep, worldLightPos, result);
        if(result.intTransmittance < 0.005f)
        {
            break;
        }
    }

    depth = result.depth / result.depthweightsum;
    if(depth == 0.0f)
    {
        depth = length(sampleEnd - startPos);
    }

    intensity = result.intensity;
    return (1.0f - result.intTransmittance);
}

#endif