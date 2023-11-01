using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Actions/Geometry")]
    public unsafe sealed class GeometryAction : CoreAction
    {
        protected override void Initialization(CustomRenderPipelineAsset asset)
        {
            // throw new System.NotImplementedException();
        }

        protected override void Dispose()
        {
            // throw new System.NotImplementedException();
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