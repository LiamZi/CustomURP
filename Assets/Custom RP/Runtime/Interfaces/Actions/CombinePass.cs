using CustomPipeline;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Pass/Combine")]
    
    public sealed unsafe class CombinePass : CoreAction
    {
        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
           base.InspectDependActions();
           _isInitialized = true;
        }

        public override bool InspectProperty()
        {
            return true;
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            // DrawFinal(cameraSettings._finalBlendMode);
            // _cmd.Execute();
            // Cleanup();
        }
        
        public override void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.BeginRendering(camera, ref cmd);
        }

        public override void EndRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.EndRendering(camera, ref cmd);
            Cleanup(camera);
        }

        private void Cleanup(CustomRenderPipelineCamera camera)
        {
            // lighting.Cleanup();
            var rt = camera._renderTarget;
            var useIntermediateBuffer = rt._isUseIntermediateBuffer;
            var useColorTexture = rt._isUseColorTexture;
            var useDepthTexture = rt._isUseDepthTexture;

            if (useIntermediateBuffer)
            {
                 //CustomRenderPipeline.DelayReleaseRTAfterFrame(rt._colorAttachmentId);
                 //CustomRenderPipeline.DelayReleaseRTAfterFrame(rt._depthAttachmentId);

                if (useColorTexture) CustomRenderPipeline.DelayReleaseRTAfterFrame(rt._colorTextureId);

                if (useDepthTexture) CustomRenderPipeline.DelayReleaseRTAfterFrame(rt._depthTextureId);
            }
        }

        void DrawFinal(CameraSettings.FinalBlendMode finalBlendMode)
        {
            // _cmd.Name = "Geometry Pass present";
            // _cmd.Cmd.SetGlobalFloat(_srcBlendId, (float)finalBlendMode._source);
            // _cmd.Cmd.SetGlobalFloat(_dstBlendId, (float)finalBlendMode._destiantion);
            // _cmd.Cmd.SetGlobalTexture(_sourceTextureId, _colorAttachmentId);
            //
            // _cmd.Cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, 
            //     finalBlendMode._destiantion == BlendMode.Zero && _camera._camera.rect == MiscUtility.FullViewRect ? 
            //         RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            //
            // _cmd.Cmd.SetViewport(_camera._camera.pixelRect);
            // _cmd.Cmd.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
            //
            // _cmd.Cmd.SetGlobalFloat(_srcBlendId, 1f);
            // _cmd.Cmd.SetGlobalFloat(_dstBlendId, 0f);
        }
        
    }
}