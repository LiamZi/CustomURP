#ifndef __SHADER_LIBRARY_CAMERA_RENDERER_PASS_HLSL__
#define __SHADER_LIBRARY_CAMERA_RENDERER_PASS_HLSL__


TEXTURE2D(_SourceTexture);

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};  


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
    return SAMPLE_TEXTURE2D_LOD(_SourceTexture, sampler_linear_clamp, input.screenUV, 0);
}

float4 CopyDepthPassFragment(Varyings input) : SV_TARGET
{
    return SAMPLE_DEPTH_TEXTURE_LOD(_SourceTexture, sampler_linear_clamp, input.screenUV, 0);
}

#endif