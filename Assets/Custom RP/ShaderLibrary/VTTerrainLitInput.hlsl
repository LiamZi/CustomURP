#ifndef __SHADER_LIBRARY_VT_TERRAIN_LIT_INPUT_HLSL__
#define __SHADER_LIBRARY_VT_TERRAIN_LIT_INPUT_HLSL__

CBUFFER_START(VTTerrainLitInput)
half4 _Diffuse_ST;
CBUFFER_END

TEXTURE2D(_Diffuse);
SAMPLER(sampler_Diffuse);
TEXTURE2D(_Normal);
SAMPLER(sampler_Normal);



#endif