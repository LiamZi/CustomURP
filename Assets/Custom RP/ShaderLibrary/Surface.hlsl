#ifndef __SHADER_LIBRARY_SURFACE_HLSL__
#define #ifndef __SHADER_LIBRARY_SURFACE_HLSL__

struct Surface
{
    float3 position;
    float3 positionCS;
    float3 normal;
    float3 interpolatedNormal;
    float3 viewDirection;
    float depth;
    float3 color;
    float alpha;
    float metallic;
    float occlusion;
    float smoothness;
    float fresnelStrength;
    float dither;
    uint renderingLayerMask;
};


#endif