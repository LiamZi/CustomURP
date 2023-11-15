using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Pass/PostProcessPass")]
    public unsafe sealed class PostProcessPass : CoreAction
    {
        private PostPass _pass = null;
        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            base.InspectDependActions();
            _asset = asset;
            _pass ??= new PostPass();
            _isInitialized = true;
        }

        protected internal override void Dispose()
        {
            base.Dispose();
        }

        public override void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.Tick(camera, ref cmd);
            if (_pass == null) return;
            var useIntermediateBuffer = camera._renderTarget._isUseIntermediateBuffer;
            if (_pass.IsActive)
            {
                _pass.Render(camera._renderTarget._colorAttachmentId);
            }
            else if (useIntermediateBuffer)
            {
                DrawFinal(ref cmd, camera, camera.Setting._finalBlendMode);
                _cmd.Execute();
            }
        }

        public override void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.BeginRendering(camera, ref cmd);
            if (_pass == null) return;
            var cameraSettings = camera.Setting;
            var useHDR = _asset.CameraBuffer._allowHDR && camera._camera.allowHDR;
            var bufferSize = new Vector2Int(camera._renderTarget._size.x, camera._renderTarget._size.y);
            _pass.Setup(cmd.Context, camera._camera, bufferSize, cameraSettings._postFXSettings,
                cameraSettings._keepAlpha, useHDR, (int)_asset.ColorLUT, cameraSettings._finalBlendMode,
                _asset.CameraBuffer._bicubicRescaling, _asset.CameraBuffer._fxaa);
        }
        
        public override void EndRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.EndRendering(camera, ref cmd);
        }
        
        public override bool InspectProperty()
        {
            return true;
        }

        private void DrawFinal(ref Command cmd, CustomRenderPipelineCamera camera, CameraSettings.FinalBlendMode mode)
        {
            cmd.SetGlobalFloat(ShaderParams._CameraSrcBlendId, (float)mode._source);
            cmd.SetGlobalFloat(ShaderParams._CameraDstBlendId, (float)mode._destiantion);
            cmd.SetGlobalTexture(ShaderParams._SourceTextureId, camera._renderTarget._colorAttachmentId);
        }
    }
}