#ifndef  __SHADER_LIBRARY_CLUSTER_DATA_HLSL__
#define  __SHADER_LIBRARY_CLUSTER_DATA_HLSL__

// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include  "../Common.hlsl"

CBUFFER_START(ClusterStruct)
#include "ClusterStruct.hlsl"
CBUFFER_END

struct CSInput
{
    uint3 groupThreadID : SV_GroupThreadID;
    uint3 dispatchThreadID : SV_DispatchThreadID;
    uint groupIndex : SV_GroupIndex;
    uint3 groupID : SV_GroupID;
};

groupshared  uint lightIndexOffset = 0;

StructuredBuffer<AdditionalLightData> _cluster_LightList;
StructuredBuffer<uint> _cluster_LightIndex;
StructuredBuffer<uint> _cluster_GridIndex;

RWStructuredBuffer<VolumeTileAABB> _cluster_Grid_RW;
RWStructuredBuffer<uint> _cluster_LightIndex_RW;
RWStructuredBuffer<uint> _cluster_GridIndex_RW;
RWStructuredBuffer<uint> _cluster_Active_RW;


float GridPos2GridIndex(uint3 pos)
{
    uint index = pos.x + pos.y * _cluster_Data.x + pos.z * _cluster_Data.x * _cluster_Data.y;
    return index;
}


float PosSS2GridIndex(float3 posNDC)
{
    float zView = UNITY_MATRIX_P._m23 / (UNITY_MATRIX_P._32 * posNDC.z - UNITY_MATRIX_P._m22);
    uint zTile = uint(max(log(-zView) * _cluster_Data.z + _cluster_Data.w , 0.0));
    uint2 xyTiles = uint2(posNDC.xy * _cluster_Data.xy);

    return GridPos2GridIndex(uint3(xyTiles, zTile));
}

void DecodeLightIndex(uint data, out uint start , out uint count)
{
    count = data >> 24;
    start = data & 0xffffff;
}

uint EncodeLightIndex(uint start, uint count)
{
    return start | (count<< 24);
}


void ZIndex2ZPlane(uint slice, uint numSlices, float zNear, float zFar, out float clusterNear, float clusterFar)
{
    clusterNear = -zNear * pow(zFar / zNear, slice / float(numSlices));
    clusterFar = -zNear * pow(zFar / zNear, (slice + 1) / float(numSlices));
}

uint Depth2Slice(float depth, float scale, float bias)
{
    return log2(depth) * scale - bias;
}

float AABBCollsion(VolumeTileAABB light, VolumeTileAABB grid)
{
    bool x = light.minPoint.x <= grid.maxPoint.x && light.maxPoint.x >= grid.minPoint.x;
    bool y = light.minPoint.y <= grid.maxPoint.y && light.maxPoint.y >= grid.minPoint.y;
    bool z = light.minPoint.z <= grid.maxPoint.z && light.maxPoint.z >= grid.minPoint.z;
    return x && y && z;
}

bool LightGridIntersection(uint lightIndex, uint gridIndex)
{
    float4 min = _cluster_LightList[lightIndex].minPoint;
    float4 max = _cluster_LightList[lightIndex].maxPoint;
    VolumeTileAABB light;
    light.minPoint = min;
    light.maxPoint = max;

    VolumeTileAABB grid = _cluster_Grid_RW[gridIndex];
    return AABBCollsion(light, grid);
}

// float4 clipToView(float4 clip, uint index)
// {
//     float4 view = _cluster_LightList[index].inverseProjection * clip ;
//     view = view / view.w;
//     return view;
// }

// float4 screen2View(float4 screen, uint index)
// {
//     //Convert to NDC
//     float2 texCoord = screen.xy * screen.yx;
//
//     //Convert to clipSpace
//     float4 clip = float4(float2(texCoord.x, texCoord.y) * 2.0 - 1.0, screen.z, screen.w);
//     return clipToView(clip);
// }

// float3 lineIntersectionToZPlane(float3 A, float3 B, float zDistance)
// {
//     float3 normal = float3(0.0, 0.0, 1.0);
//     float3 ab = B - A;
//     float t = (zDistance - dot(normal, A)) / dot(normal.ab);
//     float3 result = A + t * ab;
//     return result;
// }



#endif