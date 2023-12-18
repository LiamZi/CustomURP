#ifndef __PROCEDURAL_SKY_BOX_2_HLSL_
#define __PROCEDURAL_SKY_BOX_2_HLSL_


struct VertexInput
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
#if SKYBOX_SUNDISK == SKYBOX_SUNDISK_HQ
    float3 vertex : TEXCOORD0;
#elif SKYBOX_SUNDISK == SKYBOX_SUNDISK_SIMPLE
    half3 rayDir : TEXCOORD0;
#else
    half skyGroundFactor : TEXCOORD0;
#endif

    half3 groundColor : TEXCOORD1;
    half3 skyColor : TEXCOORD2;

#if SKYBOX_SUNDISK != SKYBOX_SUNDISK_NONE
    half3 sunColor : TEXCOORD3;
#endif
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput input)
{
    VertexOutput o;
    UNITY_SETUP_INSTANCE_ID(o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.positionCS = float4(TransformObjectToWorld(input.positionOS.xyz), 1.0);
    
    return o;
}

half4 frag(VertexOutput input) : SV_Target
{
    return half4(1.0, 0.0, 0.0, 1.0);
}

#endif