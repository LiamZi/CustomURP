using System.Collections;
using UnityEngine;

namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Pass/LightPass")]
    public unsafe sealed class LightPass : CoreAction
    {
        protected override void Initialization(CustomRenderPipelineAsset asset)
        {
            
        }

        protected override void Dispose()
        {
        }

        public override void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.Tick(camera, ref cmd);
        }

        public override void BeginFrameRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.BeginFrameRendering(camera, ref cmd);
        }
    }
}