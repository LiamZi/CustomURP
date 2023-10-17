#ifndef __CUSTOM_UNITY_INPUT_INCLUDE__
#define __CUSTOM_UNITY_INPUT_INCLUDE__


CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade;
    float4 unity_WorldTransformParams;
    // float3 _WorldSpaceCameraPos;
    float4 unity_ProbesOcclusion;
    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;
    float4 unity_LightData;
    float4 unity_LightIndices[2];

    float4 unity_SHAr;
    float4 unity_SHAg;
    float4 unity_SHAb;
    float4 unity_SHBr;
    float4 unity_SHBg;
    float4 unity_SHBb;
    float4 unity_SHC;

    float4 unity_ProbeVolumeParams;
	float4x4 unity_ProbeVolumeWorldToObject;
	float4 unity_ProbeVolumeSizeInv;
	float4 unity_ProbeVolumeMin;

    float4 unity_SpecCube0_HDR;
    float4 unity_RenderingLayer;
    
CBUFFER_END




//TODO: do not write those in unity cbuffer.

// CBUFFER_START(UnityPerFrame)
    float4x4 unity_MatrixVP;
    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 unity_prev_MatrixM;
    float4x4 unity_prev_MatrixIM;
    float4x4 glstate_matrix_projection;
// CBUFFER_END

CBUFFER_START(UnityPerCamera)
    float4 _Time;
    float4 _SinTime;
    float4 _CosTime;
    float4 unity_DeltaTime;

#if !defined(USING_STEREO_MATRICES)
    float3 _WorldSpaceCameraPos;
#endif

    float4 unity_OrthoParams;
    float4 _ProjectionParams;
    float4 _ScreenParams;
    float4 _ZBufferParams;

#if defined(STEREO_CUBEMAP_RENDER_ON)
//x-component is the half stereo separation value, which a positive for right eye and negative for left eye. The y,z,w components are unused.
float4 unity_HalfStereoSeparation;
#endif
CBUFFER_END

#endif