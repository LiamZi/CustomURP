#ifndef __Shader_LIBRARY_CLUSTER_LIGHT_HLSL__
#define __Shader_LIBRARY_CLUSTER_LIGHT_HLSL__

// #include "Surface.hlsl"
#include "Shadows.hlsl"
#include "ComputeShader/ClusterData.hlsl"
#include "SpaceTransforms.hlsl"

uint _cluster_offset;

int GetDirectionalLightSize()
{
    return _cluster_directionalLightCount;
}


int GetOtherLightSize()
{
    return _clusterLightCount;
}

uint GetAdditionalLightCount(Surface surface)
{
    // float3 posNDC = ScreenToNDC(surface.positionCS);
    // float grid = PosSS2GridIndex(posNDC);
    // uint data = _cluster_GridIndex[grid];
    // uint count;
    // DecodeLightIndex(data, _cluster_offset, count);
    // return count;

    float3 posNDC = ScreenToNDC(surface.positionCS);
    float grid = PosSS2GridIndex(posNDC);
    uint data=_cluster_GridIndex[grid];
    uint count;
    DecodeLightIndex(data, _cluster_offset, count);
    return count;
}

// OtherShadowData GetOtherShadowData(int index)
OtherShadowData GetOtherShadowData(AdditionalLightData light)
{
    OtherShadowData data;
    // data.strength = _cluster_otherLightShadowData[index].x;
    // data.tileIndex = _cluster_otherLightShadowData[index].y;
    // data.shadowMaskChannel = _cluster_otherLightShadowData[index].w;
    // data.isPoint = _cluster_otherLightShadowData[index].z == 1.0;
    // data.lightPositionWS = 0.0;
    // data.spotDirectionWS = 0.0;
    // data.lightDirectionWS = 0.0;
    data.strength = light.ShadowData.x;
    data.tileIndex = light.ShadowData.y;
    data.shadowMaskChannel = light.ShadowData.w;
    data.isPoint = light.ShadowData.z == 1.0;
    data.lightPositionWS = 0.0;
    data.spotDirectionWS = 0.0;
    data.lightDirectionWS = 0.0;

    return data;
}

DirectinalShadowData GetClusterDirectionalShadowData(int index, ShadowData shadowData)
{
    DirectinalShadowData data;

    data.strength =  _cluster_directionalLightShadowData[index].x;
    data.tileIndex =  _cluster_directionalLightShadowData[index].y + shadowData.cascadeIndex;
    data.normalBias = _cluster_directionalLightShadowData[index].z;
    data.shadowMaskChannel = _cluster_directionalLightShadowData[index].w;
    return data;
}


Light GetDirectionalLight(int index, Surface surface, ShadowData shadowData)
{
    Light light;
    // light.color = _cluster_directionalLightColor[index].rgb;
    // light.direction = _cluster_directionalLightDirectionAndMasks[index].xyz;
    // light.renderingLayerMask = asuint(_cluster_directionalLightDirectionAndMasks[index].w);
    // DirectinalShadowData dirShadowData = GetClusterDirectionalShadowData(index, shadowData);
    // light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surface);
    // light.attenuation = shadowData.cascadeIndex * 0.25;
    // light.color = float3(1.0, 0, 0);
    light.color = _cluster_directionalLightColor[index].rgb;
    light.direction = _cluster_directionalLightDirectionAndMasks[index].xyz;
    light.renderingLayerMask = asuint(_cluster_directionalLightDirectionAndMasks[index].w);
    // light.attenuation = 1.0;
    DirectinalShadowData dirShadowData = GetClusterDirectionalShadowData(index, shadowData);
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surface);
    // light.attenuation = shadowData.cascadeIndex * 0.25;
    light.attenuation = 1.0;
    return light;
}

Light GetOtherLight(int index, Surface surface, ShadowData shadowData)
{
    uint gridIndex = _cluster_offset + index;
    AdditionalLightData otherData = _cluster_LightList[_cluster_LightIndex[gridIndex]];
    Light light;
    light.color = otherData.Color;
    // light.color = float3(1.0, 0.0, 0.0);
    float3 ray = otherData.PosWS - surface.position;
    light.direction = normalize(ray);
    float distanceSqr = max(dot(ray, ray), 0.00001);
    float rangeAttenuation = Square(saturate(1.0 -  Square(distanceSqr * otherData.AttenuationCoef)));
    float4 spotAngles =  float4(otherData.SpotAngle.x, otherData.SpotAngle.y, 1.0, 1.0);
    float3 spotDirection = otherData.SpotDir.xyz;
    light.renderingLayerMask = asuint(otherData.dirAndMask.w);
    float spotAttenuation = Square(saturate(dot(spotDirection, light.direction) * spotAngles.x + spotAngles.y));
    // OtherShadowData otherShadowData = GetOtherShadowData(index);
    OtherShadowData otherShadowData = GetOtherShadowData(otherData);
    otherShadowData.lightPositionWS = otherData.PosWS;
    otherShadowData.lightDirectionWS = light.direction;
    otherShadowData.spotDirectionWS = spotDirection;
    // light.attenuation = GetOtherShadowAttenuation(otherShadowData, shadowData, surface) * spotAttenuation * rangeAttenuation / distanceSqr;
    // light.attenuation = 0.1;
    float distanceAttenuation = Square(max(0, 1 - Square(distanceSqr * otherData.AttenuationCoef)));
    float attenuation = distanceAttenuation * rcp(distanceSqr) * spotAttenuation;
    light.attenuation = attenuation;
    return light;
}

#endif