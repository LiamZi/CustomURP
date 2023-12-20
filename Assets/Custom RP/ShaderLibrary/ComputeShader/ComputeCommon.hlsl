#ifndef __SHADER_LIBRARY_COMPUTE_SHADER_COMPUTE_COMMON_HLSL__
#define __SHADER_LIBRARY_COMPUTE_SHADER_COMPUTE_COMMON_HLSL__

struct CSInput
{
    uint3 groupThreadID : SV_GroupThreadID;
    uint3 dispatchThreadID : SV_DispatchThreadID;
    uint groupIndex : SV_GroupIndex;
    uint3 groupID : SV_GroupID;
};

#endif

