#ifndef __SHADER_LIBRARY_VT_BUMP_INPUT_HLSL__
#define __SHADER_LIBRARY_VT_BUMP_INPUT_HLSL__

CBUFFER_START(VtBump)
float4 _Control_ST;
float4 _Control_TexelSize;
half _NormalScale0;
half _NormalScale1;
half _NormalScale2;
half _NormalScale3;
half4 _Normal0_ST;
half4 _Normal1_ST;
half4 _Normal2_ST;
half4 _Normal3_ST;
half _Metallic0;
half _Metallic1;
half _Metallic2;
half _Metallic3;
half _HasMask0;
half _HasMask1;
half _HasMask2;
half _HasMask3;
float4 _BakeScaleOffset;
CBUFFER_END

TEXTURE2D(_Control);
SAMPLER(sampler_Control);
SAMPLER(sampler_linear_clamp_Control);
TEXTURE2D(_Normal0);
SAMPLER(sampler_Normal0);
SAMPLER(sampler_linear_repeat_Normal0);
TEXTURE2D(_Normal1);
TEXTURE2D(_Normal2);
TEXTURE2D(_Normal3);
TEXTURE2D(_Mask0);
SAMPLER(sampler_Mask0);
SAMPLER(sampler_linear_repeat_Mask0);
TEXTURE2D(_Mask1);
TEXTURE2D(_Mask2);
TEXTURE2D(_Mask3);

#endif