#ifndef __SHADER_LIBRARY_VOLUMETRIC_LIGHT_HLSL__
#define __SHADER_LIBRARY_VOLUMETRIC_LIGHT_HLSL__




struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    uint vertexId : SV_VertexID;
};

struct Varyins
{
    float4 positionCS : SV_POSITION; 
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
};

Varyins vert(Attributes input)
{
    Varyins o;
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    o.positionCS = TransformWorldToHClip(positionWS);
    o.uv = input.uv;
    o.positionWS = _FrustumCorners[input.uv.x + input.uv.y * 2];
    return o;
}

float4 frag(Varyins input) : SV_TARGET
{
    Surface surface;
    ShadowData shadowData = GetShadowData(surface);
    float2 uv = input.uv;
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_point_clamp, uv);
    float linearDepth = Linear01Depth(depth, _ZBufferParams);

    float3 positionWS = input.positionWS;
    float3 rayStart = _WorldSpaceCameraPos;
    float3 rayDir = positionWS - rayStart;
    rayDir *= linearDepth;

    float rayLength = length(rayDir);
    rayDir /= rayLength;

    rayLength = min(rayLength, _MaxRayLength);
    float4 color = RayMarch(input.positionCS.xy, rayStart, rayDir, rayLength, surface, shadowData);

    if(linearDepth > 0.999999)
    {
        color.w = lerp(color.w, 1, _VolumetricLight.w);
    }
    
    return color;
}

#endif