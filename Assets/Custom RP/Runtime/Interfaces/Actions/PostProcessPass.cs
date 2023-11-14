using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Pass/PostProcessPass")]
    public unsafe sealed class PostProcessPass : CoreAction
    {
        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            base.InspectDependActions();
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

        public override void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.BeginRendering(camera, ref cmd);
        }
        
        public override void EndRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.EndRendering(camera, ref cmd);
        }
        
        public override bool InspectProperty()
        {
            return true;
        }
    }
}