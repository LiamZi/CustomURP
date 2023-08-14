#ifndef __CUSTOM_UNITY_INPUT_INCLUDE__
#define __CUSTOM_UNITY_INPUT_INCLUDE__


CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade;
    float4 unity_WorldTransformParams;
    float3 _WorldSpaceCameraPos;

    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;
CBUFFER_END

CBUFFER_START(UnityPerFrame)
    float4x4 unity_MatrixVP;
    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 unity_prev_MatrixM;
    float4x4 unity_prev_MatrixIM;
    float4x4 glstate_matrix_projection;
CBUFFER_END

#endif