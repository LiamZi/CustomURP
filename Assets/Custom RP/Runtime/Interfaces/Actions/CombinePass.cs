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

        protected internal override void Dispose()
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
        }
        
        void Cleanup()
        {
            // _lightingPass.Clearup();
            // if(postPass.IsActive)
            if(_isUseIntermediateBuffer)
            {
                // _cmd.Name = "Geometry Pass End";
                
                // CustomRenderPipeline.DelayReleaseRTAfterFrame(_colorAttachmentId);
                // CustomRenderPipeline.DelayReleaseRTAfterFrame(_depthAttachmentId);
                
                if(_isUseColorTexture)
                {
                    // _cmd.Cmd.ReleaseTemporaryRT(_colorTextureId);
                    // CustomRenderPipeline.DelayReleaseRTAfterFrame(_colorTextureId);
                }

                if(_isUseDepthTexture)
                {
                    // _cmd.Cmd.ReleaseTemporaryRT(_depthTextureId);
                    // CustomRenderPipeline.DelayReleaseRTAfterFrame(_depthTextureId);

                }
                
                // _cmd.Execute(_context);
                // _cmd.Execute();
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