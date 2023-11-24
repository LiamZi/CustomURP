#ifndef  __SHADER_LIBRARY_CLUSTER_DATA_HLSL__
#define  __SHADER_LIBRARY_CLUSTER_DATA_HLSL__

#define CLUSTER_GRID_BUILD_NUMTHREADS_X (16)
#define CLUSTER_GRID_BUILD_NUMTHREADS_Y (9)
#define CLUSTER_GRID_BUILD_NUMTHREADS_Z (24)

struct ScreenToView
{
    float4x4 inverseProjection;
    uint tileSizeX;
    uint tileSizeY;
    uint tileSizeZ;
    uint padding1;
    float2 tileSizePx;
    float2 viewPxSize;
    float scale;
    float bias;
    uint padding2;
    uint padding3;
};


struct VolumeTileAABB
{
    float4 minPoint;
    float4 maxPoint;
};

StructuredBuffer<ScreenToView> _screenToView;
RWStructuredBuffer<VolumeTileAABB> _clusterAABB;


float4 clipToView(float4 clip, uint index)
{
    float4 view = _screenToView[index].inverseProjection * clip ;
    view = view / view.w;
    return view;
}

float4 screen2View(float4 screen, uint index)
{
    //Convert to NDC
    float2 texCoord = screen.xy * screen.yx;

    //Convert to clipSpace
    float4 clip = float4(float2(texCoord.x, texCoord.y) * 2.0 - 1.0, screen.z, screen.w);
    return clipToView(clip);
}

float3 lineIntersectionToZPlane(float3 A, float3 B, float zDistance)
{
    float3 normal = float3(0.0, 0.0, 1.0);
    float3 ab = B - A;
    float t = (zDistance - dot(normal, A)) / dot(normal.ab);
    float3 result = A + t * ab;
    return result;
}

#endif