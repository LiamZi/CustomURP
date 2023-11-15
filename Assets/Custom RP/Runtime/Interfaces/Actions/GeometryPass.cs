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
        private Material _material = null;
        private int2 _rtSize;
        
        private static ShaderTagId _customURPShaderTagId;
        private static ShaderTagId _litShaderTagId;
        private CullingResults _cullingResults;

        private CommandBuffer buffer = null;
        
        static int
            bufferSizeId = Shader.PropertyToID("_CameraBufferSize"),
            colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment"),
            depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment"),
            colorTextureId = Shader.PropertyToID("_CameraColorTexture"),
            depthTextureId = Shader.PropertyToID("_CameraDepthTexture"),
            sourceTextureId = Shader.PropertyToID("_SourceTexture"),
            srcBlendId = Shader.PropertyToID("_CameraSrcBlend"),
            dstBlendId = Shader.PropertyToID("_CameraDstBlend");
        
        ScriptableRenderContext context;
        Camera camera;
        static CameraSettings defaultCameraSettings = new CameraSettings();
        bool useHDR, useScaledRendering;
        bool useColorTexture, useDepthTexture, useIntermediateBuffer;
        Vector2Int bufferSize;
        Material material;
        Texture2D missingTexture;
        public const float renderScaleMin = 0.1f, renderScaleMax = 2f;
        static Rect fullViewRect = new Rect(0f, 0f, 1f, 1f);

        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            _asset = asset;
            // _cmd = CmdManager.Singleton.GetTemporaryCMD("GeometryPass");
            _material = CoreUtils.CreateEngineMaterial(_asset.DefaultShader);
            material = CoreUtils.CreateEngineMaterial(_asset.DefaultShader);
            _customURPShaderTagId = new ShaderTagId("SRPDefaultUnlit");
            _litShaderTagId = new ShaderTagId("CustomLit");
            buffer =  new CommandBuffer {
	            name = "Geometry Pass"
            };
            
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
            
            // CameraSettings cameraSettings = camera ? camera.Setting : CustomRenderPipeline._defaultCameraSettings;
            //
            // _cmd.Name = "Geometry Pass Begain";
            // PrepareBuffer();
            // PrepareForSceneWindow(camera);
            //
            // if (!Cull(_asset.Shadows._maxDistance, ref cmd)) return;
            // _isUseHDR = _asset.CameraBuffer._allowHDR && camera._camera.allowHDR;
            //
            // cmd.SetRenderTarget(camera._renderTarget._colorAttachmentId, RenderBufferLoadAction.DontCare,
            //     RenderBufferStoreAction.Store, camera._renderTarget._depthAttachmentId,
            //     RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            // cmd.Execute();
            // DrawVisibleGeometry(cameraSettings._renderingLayerMask);
            //
            // DrawGizmos();
            // UnsupportedShaders();
            
   //          this.context = cmd.Context;
			this.camera = camera._camera;
			
			CameraSettings cameraSettings =
				camera ? camera.Setting : defaultCameraSettings;

			if (camera._camera.cameraType == CameraType.Reflection) {
				useColorTexture = _asset.CameraBuffer._copyColorReflection;
				useDepthTexture = _asset.CameraBuffer._copyDepthReflection;
			}
			else {
				useColorTexture = _asset.CameraBuffer._copyColor && cameraSettings._copyColor;
				useDepthTexture = _asset.CameraBuffer._copyDepth && cameraSettings._copyDepth;
			}

			// if (cameraSettings.overridePostFX) {
			// 	postFXSettings = cameraSettings.postFXSettings;
			// }

			float renderScale = cameraSettings.GetRenderScale(_asset.CameraBuffer._renderScale);
			useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;
			PrepareBuffer();
			PrepareForSceneWindow(camera);
			if (!Cull(_asset.Shadows._maxDistance, ref cmd)) {
				return;
			}

			useHDR = _asset.CameraBuffer._allowHDR && this.camera.allowHDR;
			if (useScaledRendering) {
				renderScale = Mathf.Clamp(renderScale, renderScaleMin, renderScaleMax);
				bufferSize.x = (int)(camera._camera.pixelWidth * renderScale);
				bufferSize.y = (int)(camera._camera.pixelHeight * renderScale);
			}
			else {
				bufferSize.x = camera._camera.pixelWidth;
				bufferSize.y = camera._camera.pixelHeight;
			}

			cmd.Cmd.BeginSample("xxxxxxxxxxx");
			cmd.Cmd.SetGlobalVector(bufferSizeId, new Vector4(
				1f / bufferSize.x, 1f / bufferSize.y,
				bufferSize.x, bufferSize.y
			));
			ExecuteBuffer();
			// cmd.Execute();
			// lighting.Setup(
			// 	context, cullingResults, shadowSettings, useLightsPerObject,
			// 	cameraSettings.maskLights ? cameraSettings.renderingLayerMask : -1
			// );

			// _asset.CameraBuffer._fxaa._enabled &= cameraSettings._allowFXAA;
			// postFXStack.Setup(
			// 	context, camera, bufferSize, postFXSettings, cameraSettings.keepAlpha, useHDR,
			// 	colorLUTResolution, cameraSettings.finalBlendMode,
			// 	bufferSettings.bicubicRescaling, bufferSettings.fxaa
			// );
			cmd.Cmd.EndSample("xxxxxxxxxxx");
			Setup(camera._camera);
			// DrawVisibleGeometry(
			// 	useDynamicBatching, useGPUInstancing, useLightsPerObject,
			// 	cameraSettings.renderingLayerMask
			// );
			DrawVisibleGeometry(cameraSettings._renderingLayerMask);
			
			UnsupportedShaders();
			DrawGizmosBeforeFX();
			// if (postFXStack.IsActive) {
			// 	postFXStack.Render(colorAttachmentId);
			// }
			// else if (useIntermediateBuffer) {
				DrawFinal(cameraSettings._finalBlendMode);
				ExecuteBuffer();
			// }
			// DrawGizmosAfterFX();
			Cleanup();
			Submit();
			// Submit();
        }
        
        void Cleanup () {
	        // lighting.Cleanup();
	        if (useIntermediateBuffer) {
		        // _cmd.Cmd.ReleaseTemporaryRT(colorAttachmentId);
		        // _cmd.Cmd.ReleaseTemporaryRT(depthAttachmentId);
		        _cmd.ReleaseTemporaryRT(colorAttachmentId);
		        _cmd.ReleaseTemporaryRT(depthAttachmentId);
		        // CustomRenderPipeline.DelayReleaseRTAfterFrame(colorAttachmentId);
		        // CustomRenderPipeline.DelayReleaseRTAfterFrame(depthAttachmentId);
		        if (useColorTexture) {
			        // _cmd.Cmd.ReleaseTemporaryRT(colorTextureId);
			        // CustomRenderPipeline.DelayReleaseRTAfterFrame(colorTextureId);
			        _cmd.ReleaseTemporaryRT(colorTextureId);
		        }
		        if (useDepthTexture) {
			        // _cmd.Cmd.ReleaseTemporaryRT(depthTextureId);
			        // CustomRenderPipeline.DelayReleaseRTAfterFrame(depthTextureId);
			        _cmd.ReleaseTemporaryRT(depthTextureId);
		        }
	        }
        }
        
        void Submit () {
	        _cmd.Cmd.EndSample("Geometry pass");
	        ExecuteBuffer();
	        // _cmd.Execute();
	        // context.Submit();
	        _cmd.Context.Submit();
        }
        
        void DrawFinal (CameraSettings.FinalBlendMode finalBlendMode) {
	        _cmd.Cmd.SetGlobalFloat(srcBlendId, (float)finalBlendMode._source);
	        _cmd.Cmd.SetGlobalFloat(dstBlendId, (float)finalBlendMode._destiantion);
	        _cmd.Cmd.SetGlobalTexture(sourceTextureId, colorAttachmentId);
	        _cmd.Cmd.SetRenderTarget(
		        BuiltinRenderTextureType.CameraTarget,
		        finalBlendMode._destiantion == BlendMode.Zero && camera.rect == fullViewRect ?
			        RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load,
		        RenderBufferStoreAction.Store
	        );
	        _cmd.Cmd.SetViewport(camera.pixelRect);
	        _cmd.Cmd.DrawProcedural(
		        Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3
	        );
	        _cmd.Cmd.SetGlobalFloat(srcBlendId, 1f);
	        _cmd.Cmd.SetGlobalFloat(dstBlendId, 0f);
        }
        
        void ExecuteBuffer () {
	        _cmd.Context.ExecuteCommandBuffer(_cmd.Cmd);
	        _cmd.Cmd.Clear();
        }

        public override void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            // base.BeginRendering(camera, ref cmd);
            // cmd.Name = "Render Target Init";
            // cmd.BeginSample();
            // cmd.SetGlobalVector(ShaderParams._CamerabufferSizeId,
            //     new Vector4(1f / camera._renderTarget._size.x, 1f / camera._renderTarget._size.y,
            //         camera._renderTarget._size.x, camera._renderTarget._size.y));
            // cmd.Execute();
            // cmd.EndSampler();
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
                _cullingResults = _cmd.Context.Cull(ref p);
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

            if(_camera._renderTarget._isUseColorTexture || _camera._renderTarget._isUseDepthTexture)
            {
                // _camera._renderTarget.CopyAttachments(ref _cmd, _material);
                CopyAttachments();
            }
       
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
       
            _cmd.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }
        
        private void Draw(ref Command cmd, RenderTargetIdentifier from, RenderTargetIdentifier to, Material material, bool isDepth = false)
        {
	        cmd.SetGlobalTexture(ShaderParams._SourceTextureId, from);
	        cmd.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
	        cmd.DrawProcedural(Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
        }
        
        void CopyAttachments()
        {
            // var _cmd.Cmd = CmdManager.Singleton.Get(_sampleName).Cmd;
            var cmd = new Command("Geometry Pass Copy Attachments");
            _cmd.Name = "Geometry Pass Copy Attachments";
            if (_isUseColorTexture)
            {
                _cmd.GetTemporaryRT(colorTextureId, _rtSize.x, 
                    _rtSize.y, 0, 
                    FilterMode.Bilinear, _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                if(DeviceUtility.CopyTextureSupported)
                {
                    _cmd.CopyTexture(colorAttachmentId, colorTextureId);
                }
                else
                {
                    Draw(ref _cmd, colorAttachmentId, colorTextureId, material);
                }
            }
        
            if(_isUseDepthTexture)
            {
                _cmd.GetTemporaryRT(depthTextureId, _rtSize.x, _rtSize.y, 
                    32, FilterMode.Point, RenderTextureFormat.Depth);
        
                if(DeviceUtility.CopyTextureSupported)
                {
                    _cmd.CopyTexture(depthAttachmentId, depthTextureId);
                }
                else
                {
                    Draw(ref _cmd, depthAttachmentId, depthTextureId, material, true);
                }
                
                _cmd.Execute();
            }
        
            if(!DeviceUtility.CopyTextureSupported)
            {
                _cmd.SetRenderTarget(colorAttachmentId, 
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, 
                    depthAttachmentId, 
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            }
            
            _cmd.Execute();
        }
        
        void Setup (Camera camera) 
        {
            _cmd.Context.SetupCameraProperties(camera);
            CameraClearFlags flags = camera.clearFlags;

            useIntermediateBuffer = useScaledRendering ||
                                    useColorTexture || useDepthTexture ;
                                    // || postFXStack.IsActive;
            if (useIntermediateBuffer) {
                if (flags > CameraClearFlags.Color) {
                    flags = CameraClearFlags.Color;
                }
                _cmd.Cmd.GetTemporaryRT(
                    colorAttachmentId, bufferSize.x, bufferSize.y,
                    0, FilterMode.Bilinear, useHDR ?
                        RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
                );
                _cmd.Cmd.GetTemporaryRT(
                    depthAttachmentId, bufferSize.x, bufferSize.y,
                    32, FilterMode.Point, RenderTextureFormat.Depth
                );
                _cmd.Cmd.SetRenderTarget(
                    colorAttachmentId,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    depthAttachmentId,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
            }

            _cmd.Cmd.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags <= CameraClearFlags.Color,
                flags == CameraClearFlags.Color ?
                    camera.backgroundColor.linear : Color.clear
            );
            _cmd.Cmd.BeginSample("Geometry pass");
            _cmd.Cmd.SetGlobalTexture(colorTextureId, missingTexture);
            _cmd.Cmd.SetGlobalTexture(depthTextureId, missingTexture);
            // ExecuteBuffer();
            _cmd.Execute();
        }
        
        

        
    };

}