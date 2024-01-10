#ifndef __SHADER_LIBRARY_BOUNDS_DEBUG_HLSL__
#define __SHADER_LIBRARY_BOUNDS_DEBUG_HLSL__



struct Attribute
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positonCS : SV_POSITION;
    float3 color : TEXCOORD0;
};

StructuredBuffer<BoundsDebug> _BoundsList;

Varyings vert(Attribute input, uint instanceID : SV_InstanceID)
{
    Varyings o;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    
    float4 pos = input.positionOS;
    BoundsDebug debug = _BoundsList[instanceID];
    Bounds bounds = debug.bounds;
    float3 center = (bounds.min + bounds.max) * 0.5;
    float3 scale = (bounds.max - center) / 0.5;
    pos.xyz = pos.xyz * scale + center;

    o.positonCS = TransformObjectToHClip(pos.xyz);
    o.color = debug.color.rgb;
    return o;
}

half4 frag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    half4 col = half4(input.color, 1.0);
    return col;
}



#endif