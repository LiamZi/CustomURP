using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace CustomURP
{
    public struct HizDepth
    {
        ComputeShader _cs;

        public void InitHiz(CustomRenderPipelineAsset asset)
        {
            _cs = asset._pipelineShaders.HizLodShader;
        }

        public void GetMipMap(RenderTexture depthTexture, CommandBuffer commandBuffer, int mip)
        {
            commandBuffer.SetGlobalTexture(ShaderParams._MainTex, depthTexture);

            int2 size = int2(depthTexture.width, depthTexture.height);
            for(int i = 0; i < mip; i++) 
            {
                size = max(1, size / 2);
                commandBuffer.SetComputeTextureParam(_cs, 0, ShaderParams._SourceTex, depthTexture, i - 1);
            }
        }
    };

};