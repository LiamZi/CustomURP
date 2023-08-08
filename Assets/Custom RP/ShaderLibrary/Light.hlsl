#ifndef __SHADER_LIBRARY_LIGHT_HLSL__
#define __SHADER_LIBRARY_LIGHT_HLSL__

#define MAX_VISIBLE_LIGHTS 4

CBUFFER_START(_CustomLight)
    int _directionalLightCount;
    float3 _directionalLightColor[MAX_VISIBLE_LIGHTS];
    float3 _directionalLightDirection[MAX_VISIBLE_LIGHTS];
CBUFFER_END

struct Light
{
    float3 color;
    float3 direction;
};

int GetDirectionalLightSize()
{
    return _directionalLightCount;
}

Light GetDirectionalLight(int index)
{
    Light light;
    light.color = _directionalLightColor[index].rgb;
    light.direction = _directionalLightDirection[index].xyz;
    return light;
}

#endif