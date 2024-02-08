using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Pass/Geometry")]
    public sealed partial class GeometryPass : CoreAction
    {
        private static ShaderTagId _customURPShaderTagId;
        private static ShaderTagId _litShaderTagId;
        private static ShaderTagId _terrianLitShaderTagId;

        private static CameraSettings defaultCameraSettings = new CameraSettings();
        private static readonly Rect fullViewRect = new Rect(0f, 0f, 1f, 1f);
        private CullingResults _cullingResults;
        private Material _material;

        private int2 _rtSize;
        private Texture2D missingTexture;
        public const bool _useHiz = true;

        VolumeCloud _volmenCloud = null;
        

        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            _asset = asset;
            _material = CoreUtils.CreateEngineMaterial(_asset.DefaultShader);
            _customURPShaderTagId = new ShaderTagId("SRPDefaultUnlit");
            _litShaderTagId = new ShaderTagId("CustomLit");

            InspectDependActions();
            _isInitialized = true;
        }

        public override void Dispose()
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
            // _cmd.EndSampler();
            
            var scene = ((CustomRenderPipeline)_asset.Pipeline).SceneController;
            scene.SetClusterCullResult(_cullingResults);
            scene.BeginRendering(camera, ref cmd);
            scene.Tick(camera, ref cmd);

            
            Setup(camera);
            
            // var terrainGo = GameObject.Find("Terrain");
            // if (terrainGo)
            // {
            //     var terrain = terrainGo.GetComponent<Terrain>();
            //     if (terrain)
            //     {
            //         terrain.Tick(ref _cmd, _camera);
            //     }
            // }

            for (var i = 0; i < 16; ++i)
            {
                var terrainIndex = string.Format("heightfield_{0}", i);
                // var terrainGo = GameObject.Find("heightfield_1");
                var terrainGo = GameObject.Find(terrainIndex);
                if (terrainGo)
                {
                    var loader = terrainGo.GetComponent<TLoader>();
                    if (loader)
                    {
                        loader.Tick(camera, ref _cmd);
                    }
                }
            }
 
            
            DrawVisibleGeometry(cameraSettings._renderingLayerMask);
            
            if (_asset.VolumeCloudSettings != null)
            {
                if (_volmenCloud == null)
                {
                    // _volmenCloud = ScriptableObject.CreateInstance<VolumeCloud>();
                    // _volmenCloud = ScriptableObject.CreateInstance<VolumeCloud>(_asset.VolumeCloudSettings, "");
                    _volmenCloud = new VolumeCloud(_asset.VolumeCloudSettings);
                    // _volmenCloud.Initialization(_asset);
                }
            
                _volmenCloud.BeginRendering(_camera, ref _cmd);
                _volmenCloud.Tick(_camera, ref _cmd);
            }
            
            UnsupportedShaders();
            
            _cmd.EndSampler();
        }
        
        public override void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.BeginRendering(camera, ref cmd);
        }

        public override void EndRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.EndRendering(camera, ref cmd);
            //Cleanup(camera);
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
                _cullingResults = _cmd.Context.Cull(ref p);
                // Debug.Log("_culling results ptr : " + _cullingResults.GetHashCode());
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
                enableInstancing = _asset.GPUInstancing,

                perObjectData = PerObjectData.LightIndices | PerObjectData.Lightmaps |
                    PerObjectData.ShadowMask | PerObjectData.LightProbe |
                    PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbe |
                    PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ReflectionProbes |
                    lightsPerObjectFlags
            };

            drawingSettings.SetShaderPassName(1, _litShaderTagId);

            var filteringSettings =
                new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: (uint)renderingLayerMask);
            
            // _cmd.EnableShaderKeyword("USE_CLUSTERED_LIGHTLIST");
            
            _cmd.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);

            _cmd.DrawSkybox(_camera);
            
            if (_camera._renderTarget._isUseColorTexture || _camera._renderTarget._isUseDepthTexture)
                _camera._renderTarget.CopyAttachments(ref _cmd, _material);
            // CopyAttachments();
            
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;

            _cmd.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private void Draw(ref Command cmd, RenderTargetIdentifier from, RenderTargetIdentifier to, Material material,
            bool isDepth = false)
        {
            cmd.SetGlobalTexture(ShaderParams._SourceTextureId, from);
            cmd.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
        }
        
        private void Setup(CustomRenderPipelineCamera camera)
        {
            _cmd.Context.SetupCameraProperties(camera._camera);
            var flags = camera._camera.clearFlags;

            var rt = camera._renderTarget;
            var useIntermediateBuffer = camera._renderTarget._isUseIntermediateBuffer;

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
            _cmd.Name = "Geometry pass";
            _cmd.BeginSample();
            _cmd.SetGlobalTexture(rt._colorTextureId, missingTexture);
            _cmd.SetGlobalTexture(rt._depthTextureId, missingTexture);
            _cmd.Execute();
            _cmd.EndSampler();
        }
    }
}
