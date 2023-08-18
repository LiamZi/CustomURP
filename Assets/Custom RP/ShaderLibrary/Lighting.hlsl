#ifndef __SHADER_LIBRARY_LIGHTING_HLSL__
#define __SHADER_LIBRARY_LIGHTING_HLSL__




float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
    // return saturate(dot(surface.normal, light.direction) ) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light) ;
}


float3 GetLighting(Surface surface, BRDF brdf, GI gi)
{
    ShadowData shadowData = GetShadowData(surface);
    shadowData.shadowMask = gi.shadowMask;
    // return gi.shadowMask.shadows.rgb;

    float3 color = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    // float3 color = gi.diffuse * brdf.diffuse;
    // float3 color = gi.diffuse;
    for(int i = 0; i < GetDirectionalLightSize(); ++i)
    {
        color += GetLighting(surface, brdf, GetDirectionalLight(i, surface, shadowData));
    }

    return color;
}


#endif