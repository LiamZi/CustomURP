using System.Collections;
using Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Pass/Geometry")]
    public unsafe sealed class GeometryPass : CoreAction
    {
        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            // throw new System.NotImplementedException();
            
            InspectDependActions();
            _isInitialized = true;
        }

        protected internal override void Dispose()
        {
            base.Dispose();
        }

        public override void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.Tick(camera, ref cmd);
        }

        public override void BeginFrameRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.BeginFrameRendering(camera, ref cmd);
        }

        public override bool InspectProperty()
        {
            return true;
        }
    }
    
}