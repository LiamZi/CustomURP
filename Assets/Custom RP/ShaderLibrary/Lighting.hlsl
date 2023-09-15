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

bool RenderingLayersOverlap(Surface surface, Light light)
{
    return (surface.renderingLayerMask & light.renderingLayerMask) != 0;
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
        Light light = GetDirectionalLight(i, surface, shadowData);
        if(RenderingLayersOverlap(surface, light))
        {
            color += GetLighting(surface, brdf, light);
        }
    }

#if defined(_LIGHTS_PER_OBJECT)
    for(int j = 0; j < min(unity_LightData.y, 8); ++j)
    {
        int index = unity_LightIndices[(uint)j / 4][(uint)j % 4];
        Light light = GetOtherLight(index, surface, shadowData);
        if(RenderingLayersOverlap(surface, light))
        {
            color += GetLighting(surface, brdf, light);
        }
        
    }

#else
    for(int j = 0; j < GetOtherLightSize(); ++j)
    {
        Light light = GetOtherLight(j, surface, shadowData);
        if(RenderingLayersOverlap(surface, light))
        {
            color += GetLighting(surface, brdf, light);
        }
    }
#endif

    return color;
}


#endif