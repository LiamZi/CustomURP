#ifndef __SHADER_LIBRARY_GPU_TERRIAN_HLSL__
#define __SHADER_LIBRARY_GPU_TERRIAN_HLSL__


struct Attribute
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    half3 color : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

StructuredBuffer<RenderPatch> _PatchList;

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_HeightMap);
SAMPLER(sampler_HeightMap_linear_clamp);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
float3 _WorldSize;
float4x4 _WorldToNormalMapMatrix;

static half3 DebugColorForMip[6] =
{
    half3(0, 1, 0),
    half3(0, 0, 1),
    half3(1, 0, 0),
    half3(1, 1, 0),
    half3(0, 1, 1),
    half3(1, 0, 1)
};

float3 TransformNormalToWS(float3 normal)
{
    return SafeNormalize(mul(normal, (float3x3)_WorldToNormalMapMatrix));
}

float3 SampleNormal(float2 uv)
{
    float3 normal;
    normal.xz = SAMPLE_TEXTURE2D_LOD(_NormalMap, sampler_linear_repeat, uv, 0).xy * 2 - 1;
    normal.y = sqrt(max(0, 1 - dot(normal.xz, normal.xz)));
    normal = TransformNormalToWS(normal);
    return normal;
}

float3 ApplyNodeDebug(RenderPatch patch, float3 pos)
{
    uint nodeCount = (uint)(5 * pow(2, 5 - patch.lod));
    float nodeSize = _WorldSize.x / nodeCount;
    uint2 node = floor((patch.position + _WorldSize.xz * 0.5) / nodeSize);
    float2 nodeCenterPos = - _WorldSize.xz * 0.5 + (node + 0.5) * nodeSize;
    pos.xz = nodeCenterPos + (pos.xz - nodeCenterPos) * 0.95;
    return pos;
}

void FixedBetweenPatchSeam(inout float4 pos, inout float2 uv, RenderPatch patch)
{
    uint4 lodTrans = patch.lodTrans;
    uint2 posIndex = floor((pos.xz + PATCH_MESH_SIZE * 0.5 + 0.01) / PATCH_MESH_GRID_SIZE);
    float uvGridStrip = 1.0 / PATCH_MESH_GRID_COUNT;

    uint lodDelta = lodTrans.x;
    if(lodDelta > 0 && posIndex.x == 0)
    {
        uint gridStripCount = pow(2, lodDelta);
        uint modIndex = posIndex.y % gridStripCount;
        if(modIndex != 0)
        {
            pos.z -= PATCH_MESH_GRID_SIZE * modIndex;
            uv.y -= uvGridStrip * modIndex;
            return;
        }
    }

    lodDelta = lodTrans.y;
    if(lodDelta > 0 && posIndex.y == 0)
    {
        uint gridStripCount = pow(2, lodDelta);
        uint modIndex = posIndex.x % gridStripCount;
        if(modIndex != 0)
        {
            pos.x -= PATCH_MESH_GRID_SIZE * modIndex;
            uv.x -= uvGridStrip * modIndex;
            return;
        }
    }

    lodDelta = lodTrans.z;
    if(lodDelta > 0 && posIndex.x == PATCH_MESH_GRID_COUNT)
    {
        uint gridStripCount = pow(2, lodDelta);
        uint modIndex = posIndex.y % gridStripCount;
        if(modIndex != 0)
        {
            pos.z += PATCH_MESH_GRID_SIZE * (gridStripCount - modIndex);
            uv.y += uvGridStrip * (gridStripCount - modIndex);
            return;
        }
    }

    lodDelta = lodTrans.w;
    if(lodDelta > 0 && posIndex.y == PATCH_MESH_GRID_COUNT)
    {
        uint gridStripCount = pow(2, lodDelta);
        uint modIndex = posIndex.x % gridStripCount;
        if(modIndex != 0)
        {
            pos.x += PATCH_MESH_GRID_SIZE * (gridStripCount - modIndex);
            uv.x += uvGridStrip * (gridStripCount - modIndex);
            return;
        }
    }
}

Varyings vert(Attribute input, uint instanceID : SV_InstanceID)
{
    Varyings o;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);

    float4 pos = input.positionOS;
    float2 uv = input.uv;

    RenderPatch patch = _PatchList[instanceID];
#if _ENABLE_LOD_SEAMLESS
    FixedBetweenPatchSeam(pos, uv, patch);
#endif

    uint lod = patch.lod;
    float scale = pow(2, lod);
    uint4 loadTrans = patch.lodTrans;
    pos.xz *= scale;
    
#if _ENABLE_PATCH_DEBUG
    pos.xz *= 0.9;
#endif
    
    pos.xz += patch.position;
    
#if _ENABLE_NODE_DEBUG
    pos.xyz = ApplyNodeDebug(patch, pos.xyz);
#endif  

    float2 heightUV = (pos.xz + (_WorldSize.xz * 0.5) + 0.5) / (_WorldSize.xz + 1);
    float height = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap_linear_clamp, heightUV, 0).r;
    pos.y = height * _WorldSize.y;
    // pos.y = height * 1500.0 ;

    float3 normal = SampleNormal(heightUV);
    o.color = max(0.05, dot(float3(0, 0.25, 1.0), normal));
    // o.color = half3(1.0, 1.0, 1.0);

    o.positionCS = TransformObjectToHClip(pos.xyz);
    o.uv = uv * scale * 8;

#if _ENABLE_MIP_DEBUG
    uint4 lodColorIndex = lod + loadTrans;
    o.color *= (DebugColorForMip[lodColorIndex.x] +
                DebugColorForMip[lodColorIndex.y] +
                DebugColorForMip[lodColorIndex.z] +
                DebugColorForMip[lodColorIndex.w]) * 0.25;
#endif
    
    return o;
    
}

float4 frag(Varyings input) : SV_Target
{
    float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    col.rgb *= input.color;
    return col;
    
}


#endif