#ifndef __SHADER_LIBRARY_FRGMENT_HLSL__
#define __SHADER_LIBRARY_FRGMENT_HLSL__

struct Fragment
{
    float2 positionSS;
    float depth;
};

Fragment GetFragment (float4 positionSS) 
{
	Fragment f;
	f.positionSS = positionSS.xy;
    f.depth = isOrthographicCamera() ? OrthographicDepthBufferToLinear(positionSS.z) : positionSS.w;
    return f;
}

#endif