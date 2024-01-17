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



Varyings vert(Attribute input, uint instanceID : SV_InstanceID)
{
    Varyings o;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);

    float4 inPos = input.positionOS;
    float2 uv = input.uv;

    RenderPatch patch = _PatchList[instanceID];
#if _ENABLE_LOD_SEAMLESS
    
#endif

    uint lod = patch.lod;
    float scale = pow(2, lod);
    uint4 loadTrans = patch.lodTrans;
    inPos.xz *= scale;
    
    inPos.xz += patch.position;

    float2 heightUV = (inPos.xz + (_WorldSize.xz * 0.5) + 0.5) / (_WorldSize.xz + 1);
    float height = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap_linear_clamp, heightUV, 0).r;
    // inPos.y = height * _WorldSize.y;
    inPos.y = height * _WorldSize.y;

    float3 normal = SampleNormal(heightUV);
    o.color = max(0.05, dot(normal, half3(1, 1, 0)));
    // o.color = half3(1.0, 1.0, 1.0);

    o.positionCS = TransformObjectToHClip(inPos.xyz);
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