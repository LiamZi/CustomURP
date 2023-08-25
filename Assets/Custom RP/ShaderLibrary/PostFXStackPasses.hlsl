#ifndef __SHADER_LIBRARY_POST_FX_STACK_PASSES_HLSL__
#define __SHADER_LIBRARY_POST_FX_STACK_PASSES_HLSL__


TEXTURE2D(_PostFXSource);
SAMPLER(sampler_linear_clamp);

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};  


float4 GetSource(float2 screenUV)
{
    // return SAMPLE_TEXTURE2D(_PostFXSource, sampler_linear_clamp, screenUV);
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource, sampler_linear_clamp, screenUV, 0);

}

Varyings DefaultPassVertex(uint vertexID : SV_VERTEXID)
{
    Varyings o;
    o.positionCS = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0 : -1.0, 0.0, 1.0); 
    o.screenUV = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);
    if(_ProjectionParams.x < 0.0)
    {
        o.screenUV.y = 1.0 - o.screenUV.y;
    }
    return o;
}

float4 CopyPassFragment(Varyings input) : SV_TARGET
{
    return GetSource(input.screenUV);
}

#endif