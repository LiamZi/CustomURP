#ifndef __SHADER_LIBRARY_LIGHTING_HLSL__
#define __SHADER_LIBRARY_LIGHTING_HLSL__




float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction)) * light.color;
}

float3 GetLighting(Surface surface, Light light)
{
    return IncomingLight(surface, light) * surface.color;
}


float3 GetLighting(Surface surface)
{
    float3 color = 0;
    for(int i = 0; i < GetDirectionalLightSize(); ++i)
    {
        color += GetLighting(surface, GetDirectionalLight(i));
    }

    return color;
}


#endif