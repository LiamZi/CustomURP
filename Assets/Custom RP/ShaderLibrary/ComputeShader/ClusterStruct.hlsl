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

struct AdditionalLightData
{
    float4 minPoint;
    float4 maxPoint; 
    float3 PosWS;
    float AttenuationCoef;
    float3 Color;
    uint renderingLayerMask;
    float3 SpotDir;
    float2 SpotAngle;
};

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
    uint renderingLayerMask;
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

int _directionalLightCount;
float3 _cluster_directionalLightColor[CLUSTER_MAX_LIGHTS_COUNT];
float4 _cluster_directionalLightDirectionAndMasks[CLUSTER_MAX_LIGHTS_COUNT];
float4 _cluster_directionalLightShadowData[CLUSTER_MAX_LIGHTS_COUNT];
float4 _cluster_otherLightShadowData[CLUSTER_MAX_LIGHTS_COUNT];

#endif  