#ifndef __SHADER_LIBRARY_BRDF_HLSL__
#define __SHADER_LIBRARY_BRDF_HLSL__


struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
};

BRDF GetBRDF(Surface surface)
{
    BRDF brdf;
    float onMinusReflectivity = 1.0 - surface.metallic;
    brdf.diffuse = surface.color * onMinusReflectivity;
    brdf.specular = 0.0;
    brdf.roughness = 1.0;
    return brdf;
}

#endif