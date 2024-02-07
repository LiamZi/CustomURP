#ifndef __SHADER_LIBRARY_VT_DIFFUSE_INPUT_HLSL__
#define __SHADER_LIBRARY_VT_DIFFUSE_INPUT_HLSL__

CBUFFER_START(VTDiffuseInput)
float4 _Control_ST;
float4 _Control_TexelSize;
half4 _Splat0_ST;
half4 _Splat1_ST;
half4 _Splat2_ST;
half4 _Splat3_ST;
half4 _DiffuseRemapScale0;
half4 _DiffuseRemapScale1;
half4 _DiffuseRemapScale2;
half4 _DiffuseRemapScale3;
half _Smoothness0;
half _Smoothness1;
half _Smoothness2;
half _Smoothness3;
half _HasMask0;
half _HasMask1;
half _HasMask2;
half _HasMask3;
float4 _BakeScaleOffset;
CBUFFER_END

TEXTURE2D(_Control);
SAMPLER(sampler_Control);
SAMPLER(sampler_linear_clamp_Control);
TEXTURE2D(_Splat0);
SAMPLER(sampler_Splat0);
SAMPLER(sampler_linear_repeat_Splat0);
TEXTURE2D(_Splat1);
TEXTURE2D(_Splat2);
TEXTURE2D(_Splat3);
TEXTURE2D(_Mask0);
SAMPLER(sampler_Mask0);
SAMPLER(sampler_linear_repeat_Mask0);
TEXTURE2D(_Mask1);
TEXTURE2D(_Mask2);
TEXTURE2D(_Mask3);


#endif