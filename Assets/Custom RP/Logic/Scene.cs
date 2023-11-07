
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;

namespace CustomPipeline
{
    public unsafe class Scene
    {
        private CustomRenderPipelineAsset _asset;
        public Scene(CustomRenderPipelineAsset asset)
        {
            _asset = asset;
        }
        public void Awake()
        {
            
        }

        public void Dispose()
        {
            
        }
    }
}