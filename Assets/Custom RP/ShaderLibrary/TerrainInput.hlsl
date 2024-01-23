#ifndef __SHADER_LIBRARY_TERRAIN_INPUT_HLSL__
#define __SHADER_LIBRARY_TERRAIN_INPUT_HLSL__

#define  MAX_TERRAIN_LOD 5
#define  MAX_NODE_ID 34124

#define PATCH_MESH_GRID_COUNT 16
#define PATCH_MESH_SIZE 8
#define PATCH_COUNT_PER_NODE 8
#define PATCH_MESH_GRID_SIZE 0.5
#define SECTOR_COUNT_WORLD 160


struct NodeDescriptor
{
    uint branch;
    
};

struct RenderPatch
{
    float2 position;
    float2 minMaxHeight;
    uint lod;
    uint4 lodTrans;
};

struct Bounds
{
    float3 min;
    float3 max;
};

struct BoundsDebug
{
    Bounds bounds;
    float4 color;
};


#endif