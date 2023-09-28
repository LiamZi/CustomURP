#ifndef __SHADER_LIBRARY_PROCEDURAL_HLSL__
#define __SHADER_LIBRARY_PROCEDURAL_HLSL__

#define CLUSTERCLIPCOUNT 384
#define CLUSTERTRIANGLECOUNT 128

struct TriangleLayout
{
    float3 vertex;
    float3 normal;
    float4 tangent;
    float2 uv0;
};

#endif  