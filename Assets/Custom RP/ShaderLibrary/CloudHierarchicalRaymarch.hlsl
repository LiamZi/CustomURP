#ifndef __SHADER_LIBRARY_CLOUD_HIERARCHICAL_RAY_MARCH_HLSL__
#define __SHADER_LIBRARY_CLOUD_HIERARCHICAL_RAY_MARCH_HLSL__

#include "CloudFunctions.hlsl"

TEXTURE2D(_HiHeightMap);
SAMPLER(sampler_HiHeightMap);
uint _HeightMapSize;
uint _HiHeightMaxLevel;
uint _HiHeightMinLevel;

int2 GetCellIndex(float2 pos, uint level)
{
    float2 uv = pos / _WeatherTexSize;
    uint currentLevelSize = _HeightMapSize >> level;
    return (int2)floor(uv * (float)currentLevelSize);
}

float IntersectWithHeightPlane(float3 origin, float3 v, float height)
{
    return (height - origin.y) / v.y;
}

float IntersectWithCellBoundary(float3 origin, float3 v, uint zlevel, int2 cellIndex)
{
    uint currentLevelSize = _HeightMapSize >> zlevel;
    float cellSpacing = _WeatherTexSize / currentLevelSize;

    float2 xAxisPlanes = (cellIndex.x + float2(0.0, 1.0)) * cellSpacing;
    float2 xAxisintersectT = (xAxisPlanes - origin.x) / v.x;

    float2 zAxisPlanes = (cellIndex.y + float2(0.0, 1.0)) * cellSpacing;
    float2 zAxisIntersectT = (zAxisPlanes - origin.z) / v.z;

    return min(max(xAxisintersectT.x, xAxisintersectT.y), max(zAxisIntersectT.x, zAxisIntersectT.y));
}

float HierarchicalRaymarch(float3 viewerPos, float3 dir, float maxSampleDistance,
                        int maxSampleCount, float raymarchOffset, float4 worldLightPos,
                        out float intensity, out float depth, out int iterationCount)
{
    float sampleStart, sampleEnd;
    if(!ResolveRayStartEnd(viewerPos, dir, sampleStart, sampleEnd))
    {
        intensity = 0.0;
        depth = 1e6;
        return 0.0;
    }

    float3 sampleStartPos = viewerPos + dir * sampleStart;
    if(sampleEnd <= sampleStart || sampleStartPos.y < -200)
    {
        intensity = 0.0;
        depth = 1e6;
        return 0.0;
    }

    sampleEnd = min(maxSampleDistance, sampleEnd);

    float sampleStep = min((sampleEnd - sampleStart) / maxSampleCount, 1000);
    float3 v = sampleStep * dir;
    float stepSize = length(v);
    float maxStepCount = sampleEnd / stepSize;

    uint currentZLevel = 2;
    float currentStep = sampleStart / stepSize + raymarchOffset;

    RaymarchStatus result;
    InitRayMarchStatus(result);

    iterationCount = 0;

    UNITY_LOOP
    while(currentStep < maxStepCount && iterationCount++ < 64)
    {
        float3 rayPos = viewerPos + currentStep * v;
        float2 uv = (rayPos.xz / _WeatherTexSize) + 0.5;
        float samplerUV = float4(uv, 0.0, currentZLevel) * (_CloudEndHeight - _CloudStartHeight );
        float height = SAMPLE_TEXTURE2D_LOD(_HiHeightMap, samplerUV.xy, samplerUV.w) + _CloudStartHeight;
        int2 oldCellIndex = GetCellIndex(rayPos.xz, currentZLevel);

        float rayhitStepSize;
        bool intersected = false;
        if(rayPos.y < height)
        {
            rayhitStepSize = 0.0f;
            intersected = true;
        }
        else
        {
            rayhitStepSize = IntersectWithHeightPlane(rayPos, v, height);
            if(rayhitStepSize > 0.0f)
            {
                float3 tmpRay = rayPos + v * rayhitStepSize;
                int2 newCellIndex = GetCellIndex(tmpRay.xz, currentZLevel);
                intersected = newCellIndex.x == oldCellIndex.x && newCellIndex.y == oldCellIndex.y;
            }
            else
            {
                intersected = false;
            }
        }

        if(intersected)
        {
            currentStep += ceil(rayhitStepSize);
            if(currentZLevel == _HiHeightMinLevel)
            {
                IntegrateRaymarch(viewerPos, rayPos, dir, stepSize, worldLightPos, result);
                if(result.intTransmittance < 0.01f)
                {
                    break;
                }
                currentStep += 1.0f;
            }
            else
            {
                currentZLevel -= 1;
            }
        }
        else
        {
            rayhitStepSize = IntersectWithCellBoundary(rayPos, v, currentZLevel, oldCellIndex);
            currentStep += ceil(rayhitStepSize + 0.0001);
            currentZLevel  = min(currentZLevel + 1, _HiHeightMaxLevel);
        }
    }

    depth = result.depth / result.depthweightsum;
    if(depth == 0.0f)
    {
        depth = sampleEnd;
    }

    intensity = result.intensity;
    return (1.0f - result.intTransmittance);
}

#endif