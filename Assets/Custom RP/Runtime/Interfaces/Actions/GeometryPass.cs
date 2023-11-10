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
        private CustomRenderPipelineAsset _asset;

        private CameraSettings _defaultCameraSettings = new CameraSettings();

        // private bool _isHDR;
        private bool _isUseHDR;
        private bool _isUseColorTexture;
        private bool _isUseDepthTexture;
        private bool _isUseIntermediateBuffer;
        private bool _isUseScaledRendering;
        private Command _cmd = null;
        private Material _material = null;

        private int2 _rtSize;

        static readonly int _colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
        static readonly int _depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");
        static readonly int _colorTextureId = Shader.PropertyToID("_CameraColorTexture");
        static readonly int _depthTextureId = Shader.PropertyToID("_CameraDepthTexture");
        static readonly int _sourceTextureId = Shader.PropertyToID("_SourceTexture");
        static readonly int _srcBlendId = Shader.PropertyToID("_CameraSrcBlend");
        static readonly int _dstBlendId = Shader.PropertyToID("_CameraDstBlend");
        static readonly int _bufferSizeId = Shader.PropertyToID("_CameraBufferSize");
        private static ShaderTagId _customURPShaderTagId;
        private static ShaderTagId _litShaderTagId;
        private CullingResults _cullingResults;

        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            // throw new System.NotImplementedException();
            _asset = asset;
            _cmd = CmdManager.Singleton.GetTemporaryCMD("GeometryPass");
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

        public override void Tick(CustomRenderPipelineCamera camera, ref ScriptableRenderContext context)
        {
            base.Tick(camera, ref context);
            
            CameraSettings cameraSettings = camera ? camera.Setting : _defaultCameraSettings;
            if (camera._camera.cameraType == CameraType.Reflection)
            {
                _isUseColorTexture = CustomRenderPipeline._cameraBufferSettings._copyColorReflection;
                _isUseDepthTexture = CustomRenderPipeline._cameraBufferSettings._copyDepthReflection;
            }
            else
            {
                _isUseColorTexture = CustomRenderPipeline._cameraBufferSettings._copyColor && cameraSettings._copyColor;
                _isUseDepthTexture = CustomRenderPipeline._cameraBufferSettings._copyDepth && cameraSettings._copyDepth;
            }

            if (cameraSettings._enabledHizDepth)
            {

            }

            if (cameraSettings._overridePostFx)
            {
                // PostFXSettings
            }
            
            PrepareBuffer();
            PrepareForSceneWindow(camera);

            if (!Cull(_asset.Shadows._maxDistance, ref context)) return;
            _isUseHDR = _asset.CameraBuffer._allowHDR && camera._camera.allowHDR;
            SetRenderTextureSize(cameraSettings);
            
            _cmd.Name = "Geometry Pass Begin";
            _cmd.BeginSample();
            _cmd.Cmd.SetGlobalVector(_bufferSizeId, new Vector4(1f / _rtSize.x, 1f / _rtSize.y, _rtSize.x, _rtSize.y));
            _cmd.Execute(context);
            _cmd.EndSampler();

            InitializationRenderTarget();
            DrawVisibleGeometry(cameraSettings._renderingLayerMask);
            
            DrawFinal(cameraSettings._finalBlendMode);
            _cmd.Execute(_context);
            Cleanup();
           
            context.Submit();
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

        private bool Cull(float maxShadowDistance, ref ScriptableRenderContext context)
        {
            if (_camera._camera.TryGetCullingParameters(out ScriptableCullingParameters p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, _camera._camera.farClipPlane);
                _cullingResults = context.Cull(ref p);
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

        private void InitializationRenderTarget()
        {
            
            CameraClearFlags flags = _camera._camera.clearFlags;
            _isUseIntermediateBuffer = _isUseScaledRendering || _isUseColorTexture || _isUseDepthTexture;
            //|| _postPass.IsActive;
            
            if (_isUseIntermediateBuffer)
            {
                if (flags > CameraClearFlags.Color)
                {
                    flags = CameraClearFlags.Color;
                }

                _cmd.Cmd.GetTemporaryRT(_colorAttachmentId, _rtSize.x, _rtSize.y, 32, FilterMode.Bilinear,
                    _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);

                _cmd.Cmd.GetTemporaryRT(_depthAttachmentId, _rtSize.x, _rtSize.y,
                    32, FilterMode.Point, RenderTextureFormat.Depth);
                
                _cmd.Cmd.SetRenderTarget(_colorAttachmentId, RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, _depthAttachmentId,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            }
            
            _cmd.Name = "Geometry Initilization RenderTarget Begin";
            _cmd.Cmd.ClearRenderTarget(flags <= CameraClearFlags.Depth,
                flags <= CameraClearFlags.Color,
                flags == CameraClearFlags.Color ? _camera._camera.backgroundColor.linear : Color.clear);
            
            _cmd.Cmd.SetGlobalTexture(_colorTextureId, MiscUtility.MissingTexture);
            _cmd.Cmd.SetGlobalTexture(_depthTextureId, MiscUtility.MissingTexture);
            _cmd.BeginSample();
            _cmd.Execute(_context);
            _cmd.EndSampler();
        }
        
        void DrawVisibleGeometry(int renderingLayerMask)
        {
            PerObjectData lightsPerObjectFlags = _asset.LightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;

            var sortingSettings = new SortingSettings(_camera._camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };

            // PerObjectData lightsPerObjectFlags = PerObjectData.LightData | PerObjectData.LightIndices;

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

            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
       
            _context.DrawSkybox(_camera._camera);

            if(_isUseColorTexture || _isUseDepthTexture)
            {
                CopyAttachments();
            }
       
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
       
            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }
        
        void CopyAttachments()
        {
            // var buffer = CmdManager.Singleton.Get(_sampleName).Cmd;
            var cmd = new Command("Geometry Pass Copy Attachments");
            if (_isUseColorTexture)
            {
                cmd.Cmd.GetTemporaryRT(_colorTextureId, _rtSize.x, 
                    _rtSize.y, 0, 
                    FilterMode.Bilinear, _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                if(DeviceUtility.CopyTextureSupported)
                {
                    cmd.Cmd.CopyTexture(_colorAttachmentId, _colorTextureId);
                }
                else
                {
                    Draw(ref cmd, _colorAttachmentId, _colorTextureId);
                }
            }

            if(_isUseDepthTexture)
            {
                cmd.Cmd.GetTemporaryRT(_depthTextureId, _rtSize.x, _rtSize.y, 
                    32, FilterMode.Point, RenderTextureFormat.Depth);
 
                if(DeviceUtility.CopyTextureSupported)
                {
                    cmd.Cmd.CopyTexture(_depthAttachmentId, _depthTextureId);
                }
                else
                {
                    Draw(ref cmd, _depthAttachmentId, _depthTextureId, true);
                }

                // ExcuteBuffer();
                _context.ExecuteCommandBuffer(cmd.Cmd);
                cmd.Cmd.Clear();
            }

            if(!DeviceUtility.CopyTextureSupported)
            {
                cmd.Cmd.SetRenderTarget(_colorAttachmentId, 
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, 
                    _depthAttachmentId, 
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            }
            
            cmd.Execute(_context);
        }
        
        void Draw(ref Command cmd, RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth = false)
        {
            // var buffer = CmdManager.Singleton.Get(_sampleName).Cmd;
            // var cmd = new Command("Geometry Pass DrawProcedural");
            cmd.Cmd.SetGlobalTexture(_sourceTextureId, from);
            cmd.Cmd.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.Cmd.DrawProcedural(Matrix4x4.identity, _material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
        }
        
        void Cleanup()
        {
            // _lightingPass.Clearup();
            // if(postPass.IsActive)
            if(_isUseIntermediateBuffer)
            {
                // var buffer = CmdManager.Singleton.Get(_sampleName).Cmd;
                _cmd.Name = "Geometry Pass End";
                _cmd.Cmd.ReleaseTemporaryRT(_colorAttachmentId);
                _cmd.Cmd.ReleaseTemporaryRT(_depthAttachmentId);

                if(_isUseColorTexture)
                {
                    _cmd.Cmd.ReleaseTemporaryRT(_colorTextureId);
                }

                if(_isUseDepthTexture)
                {
                    _cmd.Cmd.ReleaseTemporaryRT(_depthTextureId);
                }
                
                _cmd.Execute(_context);
            }
        }
        
        void DrawFinal(CameraSettings.FinalBlendMode finalBlendMode)
        {
            _cmd.Name = "Geometry Pass present";
            _cmd.Cmd.SetGlobalFloat(_srcBlendId, (float)finalBlendMode._source);
            _cmd.Cmd.SetGlobalFloat(_dstBlendId, (float)finalBlendMode._destiantion);
            _cmd.Cmd.SetGlobalTexture(_sourceTextureId, _colorAttachmentId);

            _cmd.Cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, 
                finalBlendMode._destiantion == BlendMode.Zero && _camera._camera.rect == MiscUtility.FullViewRect ? 
                    RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);

            _cmd.Cmd.SetViewport(_camera._camera.pixelRect);
            _cmd.Cmd.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);

            _cmd.Cmd.SetGlobalFloat(_srcBlendId, 1f);
            _cmd.Cmd.SetGlobalFloat(_dstBlendId, 0f);
        }

    };

}