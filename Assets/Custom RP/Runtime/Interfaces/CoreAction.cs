using System;
using System.Collections;
using UnityEngine;

namespace CustomURP
{
    [System.Serializable]
    public unsafe abstract class CoreAction : ScriptableObject
    {
        public void Prepare()
        {
            
        }
        
        protected abstract void Initialization(CustomRenderPipelineAsset asset);

        public virtual void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
        }

        public virtual void BeginFrameRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            
        }

        protected abstract void Dispose();
    }
    
    
}