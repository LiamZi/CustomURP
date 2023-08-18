#ifndef __SHADER_LIBRARY_META_PASS_HLSL__
#define __SHADER_LIBRARY_META_PASS_HLSL__

// #include "Surface.hlsl"
// #include "Shadows.hlsl"
// #include "Light.hlsl"
// #include "BRDF.hlsl"
#include "Common.hlsl"
#include "LitInput.hlsl"
#include "Surface.hlsl"
#include "Shadows.hlsl"
#include "Light.hlsl"
#include "BRDF.hlsl"

bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float2 lightMapUV : TEXCOORD1;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
};

Varyings MetaPassVert(Attributes input)
{
    Varyings output;
    input.positionOS.xy = input.lightMapUV.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
    output.positionCS =  TransformWorldToHClip(input.positionOS);
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

float4 MetaPassFrag(Varyings input) : SV_TARGET
{
    InputConfig config = GetInputConfig(input.baseUV);
    float4 base = GetBase(config);
    Surface surface;
    ZERO_INITIALIZE(Surface, surface);
    surface.color = base.rgb;
    surface.metallic = GetMetallic(config);
    surface.smoothness = GetSmoothness(config);
    BRDF brdf = GetBRDF(surface);
    float4 meta = 0.0;
    if(unity_MetaFragmentControl.x)
    {
        meta = float4(brdf.diffuse, 1.0);
        meta.rgb += brdf.specular * brdf.roughness * 0.5;
        meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
    }
	else if (unity_MetaFragmentControl.y) 
    {
		meta = float4(GetEmission(config), 1.0);
	}
    return meta;
}

#endif