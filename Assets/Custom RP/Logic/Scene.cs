
using System.Collections;
using System.Collections.Generic;
using CustomURP;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;

namespace CustomPipeline
{
    public unsafe class Scene
    {
        private CustomRenderPipelineAsset _asset;
        private int _clusterCount = 0;
        
        public bool _gpuDriven { get; private set; } = false;
        public Scene(CustomRenderPipelineAsset asset)
        {
            _asset = asset;
        }
        public void Awake()
        {
            int maxClusterCount = 0;
            ClusterAction cluster = _asset._clusterAction;
            if (Application.isPlaying && cluster)
            {
                //todo: init cluster
            }
            
            
        }

        public void Dispose()
        {
            
        }

        public void SetState()
        {
            _gpuDriven = _clusterCount > 0;
        }
    }
}