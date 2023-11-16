using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Pass/Geometry")]
    public sealed partial class GeometryPass : CoreAction
    {
        public const float renderScaleMin = 0.1f, renderScaleMax = 2f;

        private static ShaderTagId _customURPShaderTagId;
        private static ShaderTagId _litShaderTagId;

        private static readonly int
            sourceTextureId = Shader.PropertyToID("_SourceTexture");

        private static readonly int
            srcBlendId = Shader.PropertyToID("_CameraSrcBlend");

        private static readonly int
            dstBlendId = Shader.PropertyToID("_CameraDstBlend");

        // ScriptableRenderContext context;
        // Camera camera;
        private static          CameraSettings defaultCameraSettings = new CameraSettings();
        private static readonly Rect           fullViewRect          = new Rect(0f, 0f, 1f, 1f);
        private                 CullingResults _cullingResults;
        private                 Material       _material;

        private int2      _rtSize;
        private Texture2D missingTexture;

        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            _asset                = asset;
            _material             = CoreUtils.CreateEngineMaterial(_asset.DefaultShader);
            _customURPShaderTagId = new ShaderTagId("SRPDefaultUnlit");
            _litShaderTagId       = new ShaderTagId("CustomLit");

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

            var cameraSettings = camera.Setting;

            PrepareBuffer();
            PrepareForSceneWindow(camera);
            if (!Cull(_asset.Shadows._maxDistance, ref cmd)) return;

            _cmd.BeginSample();
            _cmd.SetGlobalVector(camera._renderTarget._bufferSizeId, new Vector4(
                1f / camera._renderTarget._size.x, 1f / camera._renderTarget._size.y,
                camera._renderTarget._size.x, camera._renderTarget._size.y
            ));
            _cmd.Execute();
            _cmd.EndSampler();

            Setup(camera);
            DrawVisibleGeometry(cameraSettings._renderingLayerMask);

            UnsupportedShaders();
            DrawGizmosBeforeFX();

            // DrawFinal(camera, cameraSettings._finalBlendMode);
            // _cmd.Execute();

            // Cleanup(camera);
            _cmd.Name = "Geometry End";
            _cmd.EndSampler();

            _cmd.Execute();
        }

        private void Cleanup(CustomRenderPipelineCamera camera)
        {
            // lighting.Cleanup();
            var rt                    = camera._renderTarget;
            var useIntermediateBuffer = rt._isUseIntermediateBuffer;
            var useColorTexture       = rt._isUseColorTexture;
            var useDepthTexture       = rt._isUseDepthTexture;

            if (useIntermediateBuffer)
            {
                CustomRenderPipeline.DelayReleaseRTAfterFrame(rt._colorAttachmentId);
                CustomRenderPipeline.DelayReleaseRTAfterFrame(rt._depthAttachmentId);

                if (useColorTexture) CustomRenderPipeline.DelayReleaseRTAfterFrame(rt._colorTextureId);

                if (useDepthTexture) CustomRenderPipeline.DelayReleaseRTAfterFrame(rt._depthTextureId);
            }
        }

        private void DrawFinal(CustomRenderPipelineCamera camera, CameraSettings.FinalBlendMode finalBlendMode)
        {
            var rt = camera._renderTarget;
            _cmd.Cmd.SetGlobalFloat(rt._srcBlendId, (float)finalBlendMode._source);
            _cmd.Cmd.SetGlobalFloat(rt._dstBlendId, (float)finalBlendMode._destiantion);
            _cmd.Cmd.SetGlobalTexture(rt._sourceTextureId, rt._colorAttachmentId);
            _cmd.Cmd.SetRenderTarget(
                BuiltinRenderTextureType.CameraTarget,
                finalBlendMode._destiantion == BlendMode.Zero && camera._camera.rect == fullViewRect
                    ? RenderBufferLoadAction.DontCare
                    : RenderBufferLoadAction.Load,
                RenderBufferStoreAction.Store
            );
            _cmd.Cmd.SetViewport(camera._camera.pixelRect);
            _cmd.Cmd.DrawProcedural(
                Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3
            );
            _cmd.Cmd.SetGlobalFloat(rt._srcBlendId, 1f);
            _cmd.Cmd.SetGlobalFloat(rt._dstBlendId, 0f);
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
            if (_camera._camera.TryGetCullingParameters(out var p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, _camera._camera.farClipPlane);
                _cullingResults  = _cmd.Context.Cull(ref p);
                return true;
            }

            return false;
        }

        private void DrawVisibleGeometry(int renderingLayerMask)
        {
            var lightsPerObjectFlags = _asset.LightsPerObject
                ? PerObjectData.LightData | PerObjectData.LightIndices
                : PerObjectData.None;

            var sortingSettings = new SortingSettings(_camera._camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };

            var drawingSettings = new DrawingSettings(_customURPShaderTagId, sortingSettings)
            {
                enableDynamicBatching = _asset.DynamicBatching,
                enableInstancing      = _asset.GPUInstancing,

                perObjectData = PerObjectData.LightIndices              | PerObjectData.Lightmaps        |
                                PerObjectData.ShadowMask                | PerObjectData.LightProbe       |
                                PerObjectData.LightProbeProxyVolume     | PerObjectData.OcclusionProbe   |
                                PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ReflectionProbes |
                                lightsPerObjectFlags
            };

            drawingSettings.SetShaderPassName(1, _litShaderTagId);

            var filteringSettings =
                new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: (uint)renderingLayerMask);

            _cmd.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);

            _cmd.DrawSkybox(_camera);

            if (_camera._renderTarget._isUseColorTexture || _camera._renderTarget._isUseDepthTexture)
                _camera._renderTarget.CopyAttachments(ref _cmd, _material);
            // CopyAttachments();

            sortingSettings.criteria           = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings    = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;

            _cmd.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private void Draw(ref Command cmd, RenderTargetIdentifier from, RenderTargetIdentifier to, Material material,
            bool                      isDepth = false)
        {
            cmd.SetGlobalTexture(ShaderParams._SourceTextureId, from);
            cmd.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
        }

        // private void CopyAttachments()
        // {
        //_cmd.Name = "Geometry Pass Copy Attachments";
        // if (_isUseColorTexture)
        // {
        //     _cmd.GetTemporaryRT(colorTextureId, _rtSize.x,
        //         _rtSize.y, 0,
        //         FilterMode.Bilinear, _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
        //     if (DeviceUtility.CopyTextureSupported)
        //         _cmd.CopyTexture(colorAttachmentId, colorTextureId);
        //     else
        //         Draw(ref _cmd, colorAttachmentId, colorTextureId, _material);
        // }
        //
        // if (_isUseDepthTexture)
        // {
        //     _cmd.GetTemporaryRT(depthTextureId, _rtSize.x, _rtSize.y,
        //         32, FilterMode.Point, RenderTextureFormat.Depth);
        //
        //     if (DeviceUtility.CopyTextureSupported)
        //         _cmd.CopyTexture(depthAttachmentId, depthTextureId);
        //     else
        //         Draw(ref _cmd, depthAttachmentId, depthTextureId, _material, true);
        //
        //     _cmd.Execute();
        // }
        //
        // if (!DeviceUtility.CopyTextureSupported)
        //     _cmd.SetRenderTarget(colorAttachmentId,
        //         RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
        //         depthAttachmentId,
        //         RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        //
        // _cmd.Execute();
        // }

        private void Setup(CustomRenderPipelineCamera camera)
        {
            _cmd.Context.SetupCameraProperties(camera._camera);
            var flags = camera._camera.clearFlags;

            var useIntermediateBuffer = camera._renderTarget._isUseIntermediateBuffer;
            var rt                    = camera._renderTarget;
            if (useIntermediateBuffer)
            {
                if (flags > CameraClearFlags.Color) flags = CameraClearFlags.Color;

                _cmd.GetTemporaryRT(
                    rt._colorAttachmentId, rt._size.x, rt._size.y,
                    0, FilterMode.Bilinear,
                    rt._isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
                );
                _cmd.GetTemporaryRT(
                    rt._depthAttachmentId, rt._size.x, rt._size.y,
                    32, FilterMode.Point, RenderTextureFormat.Depth
                );
                _cmd.SetRenderTarget(
                    rt._colorAttachmentId,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    rt._depthAttachmentId,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
            }

            _cmd.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags <= CameraClearFlags.Color,
                flags == CameraClearFlags.Color ? camera._camera.backgroundColor.linear : Color.clear
            );
            //_cmd.Cmd.BeginSample("Geometry pass");
            _cmd.Name = "Geometry pass";
            _cmd.BeginSample();
            _cmd.SetGlobalTexture(rt._colorTextureId, missingTexture);
            _cmd.SetGlobalTexture(rt._depthTextureId, missingTexture);
            _cmd.Execute();
        }
    }
}