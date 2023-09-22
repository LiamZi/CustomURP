#ifndef __SHADER_LIBRARY_FRGMENT_HLSL__
#define __SHADER_LIBRARY_FRGMENT_HLSL__

TEXTURE2D(_CameraColorTexture);
TEXTURE2D(_CameraDepthTexture);

float4 _CameraBufferSize;

struct Fragment
{
    float2 positionSS;
    float2 screenUV;
    float depth;
    float bufferDepth;
};

Fragment GetFragment (float4 positionSS) 
{
	Fragment f;
	f.positionSS = positionSS.xy;
    // f.screenUV = f.positionSS / _ScreenParams.xy;
    f.screenUV = f.positionSS * _CameraBufferSize.xy;
    f.depth = isOrthographicCamera() ? OrthographicDepthBufferToLinear(positionSS.z) : positionSS.w;
    f.bufferDepth = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, sampler_point_clamp, f.screenUV, 0);
    f.bufferDepth = isOrthographicCamera() ? OrthographicDepthBufferToLinear(f.bufferDepth) : LinearEyeDepth(f.bufferDepth, _ZBufferParams);
    return f;
}

float4 GetBufferColor(Fragment fragment, float2 uvOffset = float2(0.0, 0.0))
{
    float2 uv = fragment.screenUV + uvOffset;
    return SAMPLE_TEXTURE2D_LOD(_CameraColorTexture, sampler_linear_clamp, uv, 0);
}

#endif