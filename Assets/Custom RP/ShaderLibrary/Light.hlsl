#ifndef __SHADER_LIBRARY_LIGHT_HLSL__
#define __SHADER_LIBRARY_LIGHT_HLSL__

#define MAX_VISIBLE_LIGHTS 4
#define MAX_OTHER_LIGHT 64


CBUFFER_START(_CustomLight)
    int _directionalLightCount;
    float3 _directionalLightColor[MAX_VISIBLE_LIGHTS];
    float3 _directionalLightDirection[MAX_VISIBLE_LIGHTS];
    float4 _directionalLightShadowData[MAX_VISIBLE_LIGHTS];

    int _otherLightSize;
    float4 _otherLightColors[MAX_OTHER_LIGHT];
    float4 _otherLightPositions[MAX_OTHER_LIGHT];
    float4 _otherLightDirections[MAX_OTHER_LIGHT];
    float4 _otherLightAngles[MAX_OTHER_LIGHT];
    float4 _otherLightShadowData[MAX_OTHER_LIGHT];

CBUFFER_END

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};

int GetDirectionalLightSize()
{
    return _directionalLightCount;
}

int GetOtherLightSize()
{
    return _otherLightSize;
}

DirectinalShadowData GetDirectionalShadowData(int index, ShadowData shadowData)
{
    DirectinalShadowData data;
    // data.strength =  _directionalLightShadowData[index].x * shadowData.strength;
    data.strength =  _directionalLightShadowData[index].x;

    data.tileIndex =  _directionalLightShadowData[index].y + shadowData.cascadeIndex;
    data.normalBias = _directionalLightShadowData[index].z;
    data.shadowMaskChannel = _directionalLightShadowData[index].w;
    return data;
} 

Light GetDirectionalLight(int index, Surface surface, ShadowData shadowData)
{
    Light light;
    light.color = _directionalLightColor[index].rgb;
    light.direction = _directionalLightDirection[index].xyz;
    DirectinalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surface);
    // light.attenuation = shadowData.cascadeIndex * 0.25;
    return light;
}

OtherShadowData GetOtherShadowData(int index)
{
    OtherShadowData data;
    data.strength = _otherLightShadowData[index].x;
    data.shadowMaskChannel = _otherLightShadowData[index].w;
    return data;
}


Light GetOtherLight(int index, Surface surface, ShadowData shadowData)
{
    Light light;
    light.color = _otherLightColors[index].rgb;
    float3 ray = _otherLightPositions[index].xyz - surface.position;
    light.direction = normalize(ray);
    float distanceSqr = max(dot(ray, ray), 0.00001);
    // light.attenuation = 1.0 / distanceSqr;
    // light.attenuation = 1.0;
    float rangeAttenuation = Square(saturate(1.0 -  Square(distanceSqr * _otherLightPositions[index].w)));
    OtherShadowData otherShadowData = GetOtherShadowData(index);

    // light.attenuation = rangeAttenuation / distanceSqr;

    float4 spotAngles = _otherLightAngles[index];
    float spotAttenuation = Square(saturate(dot(_otherLightDirections[index].xyz, light.direction) * spotAngles.x + spotAngles.y));
    light.attenuation = GetOtherShadowAttenuation(otherShadowData, shadowData, surface) * spotAttenuation * rangeAttenuation / distanceSqr;

    return light;
}


#endif