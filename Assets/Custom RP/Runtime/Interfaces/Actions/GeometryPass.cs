using System.Collections;
using Core;
using CustomPipeline;
using TMPro.EditorUtilities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Pass/Geometry")]
    public sealed unsafe partial class GeometryPass : CoreAction
    {

        // private bool _isHDR;
        // private bool _isUseHDR;
        // private bool _isUseColorTexture;
        // private bool _isUseDepthTexture;
        // private bool _isUseIntermediateBuffer;
        // private bool _isUseScaledRendering;
        private Material _material = null;

        private int2 _rtSize;


        private static ShaderTagId _customURPShaderTagId;
        private static ShaderTagId _litShaderTagId;
        private CullingResults _cullingResults;

        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            _asset = asset;
            // _cmd = CmdManager.Singleton.GetTemporaryCMD("GeometryPass");
            _material = CoreUtils.CreateEngineMaterial(_asset.DefaultShader);
            _customURPShaderTagId = new ShaderTagId("SRPDefaultUnlit");
            _litShaderTagId = new ShaderTagId("CustomLit");
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
            
            CameraSettings cameraSettings = camera ? camera.Setting : CustomRenderPipeline._defaultCameraSettings;

            // if (cameraSettings._enabledHizDepth)
            // {
            //
            // }
            //
            // if (cameraSettings._overridePostFx)
            // {
            //     // PostFXSettings
            // }
            _cmd.Name = "Geometry Pass Begain";
            PrepareBuffer();
            PrepareForSceneWindow(camera);

            if (!Cull(_asset.Shadows._maxDistance, ref cmd)) return;
            _isUseHDR = _asset.CameraBuffer._allowHDR && camera._camera.allowHDR;

            DrawVisibleGeometry(cameraSettings._renderingLayerMask);
            UnsupportedShaders();
            DrawGizmos();
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

        private bool Cull(float maxShadowDistance, ref Command cmd)
        {
            if (_camera._camera.TryGetCullingParameters(out ScriptableCullingParameters p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, _camera._camera.farClipPlane);
                _cullingResults = cmd.Context.Cull(ref p);
                return true;
            }

            return false;
        }

        private void SetRenderTextureSize(CameraSettings cameraSettings)
        {
            float renderScale = cameraSettings.GetRenderScale(_asset.CameraBuffer._renderScale);
            _isUseScaledRendering = renderScale < 0.99f || renderScale > 1.01f;

            if (_isUseScaledRendering)
            {
                renderScale = Mathf.Clamp(renderScale, 0.1f, 2f);
                _rtSize.x = (int)(_camera._camera.pixelWidth * renderScale);
                _rtSize.y = (int)(_camera._camera.pixelHeight * renderScale);
            }
            else
            {
                _rtSize.x = _camera._camera.pixelWidth;
                _rtSize.y = _camera._camera.pixelHeight;
            }
        }
        
        void DrawVisibleGeometry(int renderingLayerMask)
        {
            PerObjectData lightsPerObjectFlags = _asset.LightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;

            var sortingSettings = new SortingSettings(_camera._camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };
            
            var drawingSettings = new DrawingSettings(_customURPShaderTagId, sortingSettings)
            {
                enableDynamicBatching = _asset.DynamicBatching,
                enableInstancing = _asset.GPUInstancing,

                perObjectData = PerObjectData.LightIndices | PerObjectData.Lightmaps | 
                                PerObjectData.ShadowMask | PerObjectData.LightProbe | 
                                PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbe |
                                PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ReflectionProbes |
                                lightsPerObjectFlags

            };
            
            drawingSettings.SetShaderPassName(1, _litShaderTagId);
       
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: (uint)renderingLayerMask);

            _cmd.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
       
            _cmd.DrawSkybox(_camera);

            if(_isUseColorTexture || _isUseDepthTexture)
            {
                _camera._renderTarget.CopyAttachments(ref _cmd, _material);
            }
       
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
       
            _cmd.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }
        
        // void CopyAttachments()
        // {
        //     // var buffer = CmdManager.Singleton.Get(_sampleName).Cmd;
        //     var cmd = new Command("Geometry Pass Copy Attachments");
        //     _cmd.Name = "Geometry Pass Copy Attachments";
        //     if (_isUseColorTexture)
        //     {
        //         _cmd.GetTemporaryRT(_colorTextureId, _rtSize.x, 
        //             _rtSize.y, 0, 
        //             FilterMode.Bilinear, _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
        //         if(DeviceUtility.CopyTextureSupported)
        //         {
        //             _cmd.CopyTexture(_colorAttachmentId, _colorTextureId);
        //         }
        //         else
        //         {
        //             Draw(ref _cmd, _colorAttachmentId, _colorTextureId);
        //         }
        //     }
        //
        //     if(_isUseDepthTexture)
        //     {
        //         _cmd.GetTemporaryRT(_depthTextureId, _rtSize.x, _rtSize.y, 
        //             32, FilterMode.Point, RenderTextureFormat.Depth);
        //
        //         if(DeviceUtility.CopyTextureSupported)
        //         {
        //             _cmd.CopyTexture(_depthAttachmentId, _depthTextureId);
        //         }
        //         else
        //         {
        //             Draw(ref _cmd, _depthAttachmentId, _depthTextureId, true);
        //         }
        //         
        //         _cmd.Execute();
        //     }
        //
        //     if(!DeviceUtility.CopyTextureSupported)
        //     {
        //         _cmd.SetRenderTarget(_colorAttachmentId, 
        //             RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, 
        //             _depthAttachmentId, 
        //             RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        //     }
        //     
        //     _cmd.Execute();
        // }
        

        
    };

}