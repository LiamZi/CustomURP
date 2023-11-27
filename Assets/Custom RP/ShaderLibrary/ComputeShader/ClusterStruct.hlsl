#ifndef __SHADER_LIBRARY_COMPUTE_SHADER_CLUSTER_STRUCT_HLSL__
#define __SHADER_LIBRARY_COMPUTE_SHADER_CLUSTER_STRUCT_HLSL__

#define CLUSTER_MAX_LIGHTS_COUNT (255)
#define CLUSTER_GRID_BUILD_NUMTHREADS_X (8)
#define CLUSTER_GRID_BUILD_NUMTHREADS_Y (4)
#define CLUSTER_GRID_BUILD_NUMTHREADS_Z (16)

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

float _cluster_zNear;
float _cluster_zFar;
float4 _cluster_Data;
uint _clusterLightCount;

#endif  