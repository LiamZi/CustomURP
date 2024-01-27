#ifndef __SHADER_LIBRARY_TERRAIN_LIT_INPUT_HLSL__
#define __SHADER_LIBRARY_TERRAIN_LIT_INPUT_HLSL__

#define _Surface 0.0

CBUFFER_START(_Terrian)
half _NormalScale0;
half _NormalScale1;
half _NormalScale2;
half _NormalScale3;
half _Metallic0;
half _Metallic1;
half _Metallic2;
half _Metallic3;
half _Smoothness0;
half _Smoothness1;
half _Smoothness2;
half _Smoothness3;
float4 _Control_ST;
float4 _Control_TexelSize;
half _LayerHasMask0;
half _LayerHasMask1;
half _LayerHasMask2;
half _LayerHasMask3;
half4 _Splat0_ST;
half4 _Splat1_ST;
half4 _Splat2_ST;
half4 _Splat3_ST;
CBUFFER_END

TEXTURE2D(_Control);
SAMPLER(sampler_Control);
TEXTURE2D(_Splat0);
SAMPLER(sampler_Splat0);
TEXTURE2D(_Splat1);
TEXTURE2D(_Splat2);
TEXTURE2D(_Splat3);

#ifdef _NORMALMAP
TEXTURE2D(_Normal0);
SAMPLER(sampler_Normal0);
TEXTURE2D(_Normal1);
TEXTURE2D(_Normal2);
TEXTURE2D(_Normal3);
#endif

#ifdef _MASKMAP
TEXTURE2D(_Mask0);
SAMPLER(sampler_Mask0);
TEXTURE2D(_Mask1);
TEXTURE2D(_Mask2);
TEXTURE2D(_Mask3);
#endif

#endif