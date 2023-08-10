#ifndef __SHADER_LIBRARY_SURFACE_HLSL__
#define #ifndef __SHADER_LIBRARY_SURFACE_HLSL__

struct Surface
{
    float3 position;
    float3 normal;
    float3 viewDirection;
    float3 color;
    float alpha;
    float metallic;
    float smoothness;
};


#endif