using System.Collections;
using UnityEngine;

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

        public override void Tick(CustomRenderPipelineCamera camera)
        {
            base.Tick(camera);
        }

        public override void BeginRendering(CustomRenderPipelineCamera camera)
        {
            base.BeginRendering(camera);
        }
        
        public override void EndRendering(CustomRenderPipelineCamera camera)
        {
            base.EndRendering(camera);
        }
        
        public override bool InspectProperty()
        {
            return true;
        }
    }
}