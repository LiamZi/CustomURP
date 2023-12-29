#ifndef __SHADER_LIBRARY_RAIN_DROP_HLSL__
#define __SHADER_LIBRARY_RAIN_DROP_HLSL__


struct Attribute
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

Varyings vert(Attribute input)
{
    Varyings o;

    o.positionCS = TransformObjectToHClip(input.positionOS);
    o.uv = input.uv;

    return o;
}

float4 frag(Varyings input) : SV_Target
{
    return float4(1.0, 0.0, 0.0, 1.0);
}

#endif