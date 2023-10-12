#ifndef CUSTOM_GPU_CULL_TEST_PASS_INCLUDED
#define CUSTOM_GPU_CULL_TEST_PASS_INCLUDED

#if SHADER_TARGET >= 45
    StructuredBuffer<float4x4> positionBuffer;
#endif

struct VertexInput
{
    float3 positionOS : POSITION;
    float4 color : COLOR;

#if defined(_FLIPBOOK_BLENDING)
    float4 baseUV : TEXCOORD0;
    float flipbookBlend : TEXCOORD1;
#else
    float2 baseUV : TEXCOORD0;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
#if defined(_VERTEX_COLORS)
    float4 color : VAR_COLOR;
#endif

    float2 baseUV : VAR_BASE_UV;

#if defined(_FLIPBOOK_BLENDING)
    float3 flipbookUVB : VAR_FILPBOOk;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

VertexOutput vert(VertexInput input, uint inID : SV_InstanceID)
{

    VertexOutput o;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);

#if SHADER_TARGET >= 45
    float4x4 data = positionBuffer[inID];
#else
    float4x4 data = 0;
#endif

#if SHADER_TARGET >= 45
    float3 localPos = input.positionOS.xyz * data._11;
    float3 positionWS = TransformObjectToWorld(data._14_24_34 + localPos);
#else
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
#endif

   
    o.positionCS = TransformWorldToHClip(positionWS);

#if defined(_VERTEX_COLORS)
    o.color = input.color;
#endif

    o.baseUV.xy = TransformBaseUV(input.baseUV.xy);

#if defined(_FLIPBOOK_BLENDING)
    o.flipbookUVB.xy = TransformBaseUV(input.baseUV.zw);
    o.flipbookUVB.z = input.flipbookBlend;
#endif

    return o;
}

half4 frag(VertexOutput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    InputConfig config = GetInputConfig(input.positionCS, input.baseUV);
    // return GetBufferColor(config.fragment, 0.05);
    // return float4(config.fragment.bufferDepth.xxx / 20.0, 1.0);

#if defined(_VERTEX_COLORS)
    config.color = input.color;
#endif

#if defined(_FLIPBOOK_BLENDING)
    config.flipbookUVB = input.flipbookUVB;
    config.flipbookBlending = true;
#endif

#if defined(_NEAR_FADE)
    config.nearFade = true;
#endif

#if defined(_SOFT_PARTICLES)
    config.softParticles = true;
#endif

    float4 col = GetBase(config);

#if defined(_CLIPPING)
    clip(col.a - GetCutoff(config));
#endif

#if defined(_DISTORTION)
    float2 distortion = GetDistortion(config) * col.a;
    col.rgb = lerp(GetBufferColor(config.fragment, distortion).rgb, col.rgb, saturate(col.a - GetDistortionBlend(config))) ;
    // col.r = distortion;
#endif

    return float4(col.rgb, GetFinalAlpha(col.a));
}

#endif