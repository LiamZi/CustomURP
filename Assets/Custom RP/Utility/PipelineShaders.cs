using System;
using UnityEngine;

namespace CustomPipeline
{
    [Serializable]

    public struct PipelineShaders
    {
        public ComputeShader _gpuFrustumCulling;
        public ComputeShader _streamingShader;
        public ComputeShader HizLodShader;
        public ComputeShader _ClusterRenderShader;
    };
};