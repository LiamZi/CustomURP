using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Pass/PostProcessPass")]
    public sealed partial class PostProcessPass : CoreAction
    {
        private static readonly Rect     fullViewRect = new Rect(0f, 0f, 1f, 1f);
        private                 Material _material;
        private                 PostPass _pass;
        VolumetircLightAction _vlight = null;

        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            InspectDependActions();
            _asset         =   asset;
            _material      =   CoreUtils.CreateEngineMaterial(_asset.DefaultShader);
            _pass          ??= new PostPass();
            _isInitialized =   true;
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_vlight != null)
            {
                _vlight.Dispose();
                _vlight = null;
            }
        }

        public override void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.Tick(camera, ref cmd);
            if (_pass == null) return;
            var useIntermediateBuffer = camera._renderTarget._isUseIntermediateBuffer;
            


            if (_vlight)
            {
                _vlight.BeginRendering(camera, ref _cmd);
                _vlight.EndRendering(camera, ref _cmd);
            }
            
            if (_pass.IsActive)
            {
                _pass.Render(camera._renderTarget._colorAttachmentId);
            }
            else if (useIntermediateBuffer)
            {
                Present();
            }
        }

        public override void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            cmd.Name = "PostProcess Pass";
            base.BeginRendering(camera, ref cmd);
            DrawGizmosBeforePostPass();

            if (_pass == null) return;
            var cameraSettings = camera.Setting;
            var useHDR         = _asset.CameraBuffer._allowHDR && camera._camera.allowHDR;
            var bufferSize     = new Vector2Int(camera._renderTarget._size.x, camera._renderTarget._size.y);
            
            var settings = _asset.PostProcessing;
            if (cameraSettings._overridePostFx)
            {
                settings = cameraSettings._postFXSettings;    
            }
            
            _pass.Setup(cmd.Context, camera._camera, bufferSize, settings,
                cameraSettings._keepAlpha, useHDR, (int)_asset.ColorLUT, cameraSettings._finalBlendMode,
                _asset.CameraBuffer._bicubicRescaling, _asset.CameraBuffer._fxaa);
            
            if (_vlight == null)
            {
                _vlight = _asset.VolumetircLightAction;
                _vlight.Initialization(_asset);
                _vlight.SetCamera(_camera);
                _vlight.ChangeRes();
            }
            
        }

        public override void EndRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.EndRendering(camera, ref cmd);
           DrawGizmosAfterPostPass();
        }

        public override bool InspectProperty()
        {
            return true;
        }

        private void Present()
        {
            var rt             = _camera._renderTarget;
            var finalBlendMode = _camera.Setting._finalBlendMode;
            _cmd.SetGlobalFloat(rt._srcBlendId, (float)finalBlendMode._source);
            _cmd.SetGlobalFloat(rt._dstBlendId, (float)finalBlendMode._destiantion);
            _cmd.SetGlobalTexture(rt._sourceTextureId, rt._colorAttachmentId);
            _cmd.SetRenderTarget(
                BuiltinRenderTextureType.CameraTarget,
                // rt._colorAttachmentId,
                finalBlendMode._destiantion == BlendMode.Zero && _camera._camera.rect == fullViewRect
                    ? RenderBufferLoadAction.DontCare
                    : RenderBufferLoadAction.Load,
                RenderBufferStoreAction.Store
            );
            _cmd.Cmd.SetViewport(_camera._camera.pixelRect);
            _cmd.DrawProcedural(
                Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3
            );
            _cmd.SetGlobalFloat(rt._srcBlendId, 1f);
            _cmd.SetGlobalFloat(rt._dstBlendId, 0f);
            _cmd.Execute();
        }
    }
}