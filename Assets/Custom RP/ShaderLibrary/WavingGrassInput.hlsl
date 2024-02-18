#ifndef WAVING_GRASS_INPUT_INCLUDED
#define WAVING_GRASS_INPUT_INCLUDED


CBUFFER_START(WavingGrassInput)
half4 _WavingTint;
float4 _WindControl;    // wind speed x, y, z, scale
float _WaveSpeed;  
float4 _MainTex_ST;
// half4 _BaseColor;
half4 _SpecColor;
// half4 _EmissionColor;
half _GrassCutoff;
// half _Smoothness;
half _Transluency;
CBUFFER_END

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
// ---- Grass helpers

struct Attributes
{
	float4 positionOS       : POSITION;
	float3 normal			: NORMAL;
	float4 tangent      	: TANGENT;
	half4 color         	: COLOR;
	float2 texcoord     	: TEXCOORD0;
	float2 lightmapUV   	: TEXCOORD1;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Vayings
{
	float2 uv                       : TEXCOORD0;
	// DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

	float4 posWSShininess           : TEXCOORD2;    // xyz: posWS, w: Shininess * 128

	half3 normalWS                  : TEXCOORD3;
    half4 tangentWS                 : TEXCOORD4;    // xyz: tangent, w: sign

	half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light

#ifdef _MAIN_LIGHT_SHADOWS
	float4 shadowCoord              : TEXCOORD6;
#endif
	half4 color                     : TEXCOORD7;

	float4 positionCS                  : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};



#endif
