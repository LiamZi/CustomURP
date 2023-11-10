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

        public override void Tick(CustomRenderPipelineCamera camera, ref ScriptableRenderContext context)
        {
            base.Tick(camera, ref context);
        }

        public override void BeginRendering(CustomRenderPipelineCamera camera, ref ScriptableRenderContext context)
        {
            base.BeginRendering(camera, ref context);
        }
        
        public override void EndRendering(CustomRenderPipelineCamera camera, ref ScriptableRenderContext context)
        {
            base.EndRendering(camera, ref context);
        }
        
        public override bool InspectProperty()
        {
            return true;
        }
    }
}