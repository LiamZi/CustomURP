using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Conditional = System.Diagnostics.ConditionalAttribute;
// using UnityEditor.Experimental;
using System.Linq;
using Unity.Collections;
using TMPro;
using System;

public partial class CameraRenderer
{
    private ScriptableRenderContext _context;

    private Camera _camera;

    private const string _bufferName = "Render Camera Buffer";

    private CommandBuffer _commandBuffer = new CommandBuffer
    {
        name = _bufferName, 
    };

    private CullingResults _cullingResults;
    private Lighting _lighting = new Lighting();
    private PostFXStack _postStack = new PostFXStack();

    // static int _frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
    static int _colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
    static int _depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");
    static int _colorTextureId = Shader.PropertyToID("_CameraColorTexture");
    static int _depthTextureId = Shader.PropertyToID("_CameraDepthTexture");
    static int _sourceTextureId = Shader.PropertyToID("_SourceTexture");
    static int _srcBlendId = Shader.PropertyToID("_CameraSrcBlend");
    static int _dstBlendId = Shader.PropertyToID("_CameraDstBlend");
    static int _bufferSizeId = Shader.PropertyToID("_CameraBufferSize");
    private static ShaderTagId _customURPShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    private static ShaderTagId _litShaderTagId = new ShaderTagId("CustomLit");


    private bool _isUseHDR;
    private bool _isUseColorTexture;
    private bool _isUseDepthTexture;
    private bool _isUseIntermediateBuffer;
    private bool _isUseScaledRendering;

    static CameraSettings _defaultCameraSettings = new CameraSettings();

    private Material _material;

    private Texture2D _missingTexture;

    static bool CopyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;

    static Rect FullViewRect = new Rect(0f, 0f, 1f, 1f);

    private Vector2Int _bufferSize;

    public const float _renderScaleMin = 0.1f;
    public const float _renderScaleMax = 2f;

    // private bool _isEnabledDynamicBatch = false;
    // public bool EnabledDynamicBatch
    // {
    //     get => _isEnabledDynamicBatch;
    //     set => _isEnabledDynamicBatch = value;
    // }

    // private bool _isEnabledInstacing = false;
    // public bool EnabledInstacing
    // {
    //     get => _isEnabledInstacing;
    //     set => _isEnabledInstacing = value;
    // }


    // private static int _visibleLightColorsId = Shader.PropertyToID("_VisibleLightColors");
    // private static int _visibleLightDirectionsOrPositionsId = Shader.PropertyToID("_VisibleLightDirectionsOrPositions");
    // private static int _visibleLightAttenuationsId = Shader.PropertyToID("_VisibleLightAttenuations");
    // private static int _visibleLightSpotDirectionsId = Shader.PropertyToID("_VisibleLightSpotsDirections");
    // private static int _lightIndicesOffsetAndCountId = Shader.PropertyToID("unity_LightIndicesOffsetAndCount");


    // private Vector4[] _visibleLightColors = new Vector4[MAX_VISIBLE_LIGHTS];
    // private Vector4[] _visibleLightDirectionsOrPositions = new Vector4[MAX_VISIBLE_LIGHTS];
    // private Vector4[] _visibleLightAttenuations = new Vector4[MAX_VISIBLE_LIGHTS];
    // private Vector4[] _visibleLightSpotsDirections = new Vector4[MAX_VISIBLE_LIGHTS];

    public CameraRenderer(Shader shader)
    {
        // _isEnabledDynamicBatch = isEnabledDynamicBatch;
        // _isEnabledInstacing = isEnabledInstancing;
        // QualitySettings.pixelLightCount = 8;
        _material = CoreUtils.CreateEngineMaterial(shader);
        _missingTexture = new Texture2D(1, 1) 
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = "Missing"
        };

        _missingTexture.SetPixel(0, 0, Color.white * 0.5f);
        _missingTexture.Apply(true, true);
    }

    public void Render(ScriptableRenderContext context, Camera camera, 
                    CameraBufferSettings cameraBufferSetting, bool useDynamicBatching, bool useGPUInstanceing, 
                    bool useLightsPerObject, ShadowSettings shadowSettings, 
                    PostFXSettings postFXSettings, int colorLUTResolution)
    {
        this._context = context;
        this._camera = camera;

        var crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
        CameraSettings cameraSettings = crpCamera ? crpCamera.Setting : _defaultCameraSettings;

        // _isUseDepthTexture = true;
		if (camera.cameraType == CameraType.Reflection) {
			_isUseColorTexture = cameraBufferSetting._copyColorReflection;
			_isUseDepthTexture = cameraBufferSetting._copyDepthReflection;
		}
		else {
			_isUseColorTexture = cameraBufferSetting._copyColor && cameraSettings._copyColor;
			_isUseDepthTexture = cameraBufferSetting._copyDepth && cameraSettings._copyDepth;
		}

        if(cameraSettings._overridePostFx)
        {
            postFXSettings = cameraSettings._postFXSettings;
        }

        // float renderScale = cameraBufferSetting._renderScale;
        float renderScale = cameraSettings.GetRenderScale(cameraBufferSetting._renderScale);

        _isUseScaledRendering = renderScale < 0.99f || renderScale > 1.01f;

        PrepareBuffer();
        PrepareForSceneWindow();

        if (!Cull(shadowSettings._maxDistance)) return;

        // _isUseHDR = useHDR && camera.allowHDR;
        _isUseHDR = cameraBufferSetting._allowHDR && camera.allowHDR;


        if(_isUseScaledRendering)
        {
            renderScale = Mathf.Clamp(renderScale, 0.1f, 2f);
            _bufferSize.x = (int)(camera.pixelWidth * renderScale);
            _bufferSize.y = (int)(camera.pixelHeight * renderScale);
        }
        else
        {
            _bufferSize.x = camera.pixelWidth;
            _bufferSize.y = camera.pixelHeight;
        }

        _commandBuffer.BeginSample(_sampleName);
        _commandBuffer.SetGlobalVector(_bufferSizeId, new Vector4(1f / _bufferSize.x, 1f / _bufferSize.y, _bufferSize.x, _bufferSize.y));
        ExcuteBuffer();

        _lighting.Setup(context, _cullingResults, shadowSettings, 
                    useLightsPerObject, cameraSettings._maskLights ? cameraSettings._renderingLayerMask : -1);
        _postStack.Setup(context, camera, _bufferSize, postFXSettings, _isUseHDR, colorLUTResolution, cameraSettings._finalBlendMode, cameraBufferSetting._bicubicRescaling);
        _commandBuffer.EndSample(_sampleName);

        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstanceing, useLightsPerObject, cameraSettings._renderingLayerMask);
        DrawUnsupportedShaders();
        // DrawGizmos();
        DrawGizmosBeforeFX();

        if(_postStack.IsActive)
        {
            _postStack.Render(_colorAttachmentId);
        }
        else if(_isUseIntermediateBuffer)
        {
            // Draw(_colorAttachmentId, BuiltinRenderTextureType.CameraTarget);
            DrawFinal(cameraSettings._finalBlendMode);
            ExcuteBuffer();
        }
        
        DrawGizmosAfterFX();

        Cleanup();
        // _lighting.Clearup();
        Submit();
    }

    void Setup()
    {
        _context.SetupCameraProperties(_camera);
        CameraClearFlags flags = _camera.clearFlags;

        _isUseIntermediateBuffer = _isUseScaledRendering || _isUseColorTexture || _isUseDepthTexture || _postStack.IsActive;
        if(_isUseIntermediateBuffer)
        {
            if(flags > CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;
            }

            _commandBuffer.GetTemporaryRT(_colorAttachmentId, _bufferSize.x, 
                                _bufferSize.y, 32, FilterMode.Bilinear, 
                                _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);

            _commandBuffer.GetTemporaryRT(_depthAttachmentId, _bufferSize.x, _bufferSize.y, 
                                        32, FilterMode.Point, RenderTextureFormat.Depth);

            _commandBuffer.SetRenderTarget(_colorAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, 
                                        _depthAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        }

        //_commandBuffer.ClearRenderTarget(true, true, Color.clear);
        _commandBuffer.ClearRenderTarget(flags <= CameraClearFlags.Depth,
                                        flags <= CameraClearFlags.Color,
                                        flags == CameraClearFlags.Color ?
                                        _camera.backgroundColor.linear : Color.clear);
        // ConfigureLights();
        // if(_cullingResults.visibleLights.Length > 0)
        // {
        //     ConfigureLights();
        // }
        // else
        // {
        //     _commandBuffer.SetGlobalVector(_lightIndicesOffsetAndCountId, Vector4.zero);
        // }

        _commandBuffer.BeginSample(_sampleName);
        _commandBuffer.SetGlobalTexture(_colorTextureId, _missingTexture);
        _commandBuffer.SetGlobalTexture(_depthTextureId, _missingTexture);

        // _commandBuffer.SetGlobalVectorArray(_visibleLightColorsId, _visibleLightColors);
        // _commandBuffer.SetGlobalVectorArray(_visibleLightDirectionsOrPositionsId, _visibleLightDirectionsOrPositions);
        // _commandBuffer.SetGlobalVectorArray(_visibleLightAttenuationsId, _visibleLightAttenuations);
        // _commandBuffer.SetGlobalVectorArray(_visibleLightSpotDirectionsId, _visibleLightSpotsDirections);
        
        ExcuteBuffer();
       
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstanceing, bool useLightsPerObject, int renderingLayerMask)
    {
        PerObjectData lightsPerObjectFlags = useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;

        var sortingSettings = new SortingSettings(_camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };

        // PerObjectData lightsPerObjectFlags = PerObjectData.LightData | PerObjectData.LightIndices;

        var drawingSettings = new DrawingSettings(_customURPShaderTagId, sortingSettings)
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstanceing,

            perObjectData = PerObjectData.LightIndices | PerObjectData.Lightmaps | 
                            PerObjectData.ShadowMask | PerObjectData.LightProbe | 
                            PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbe |
                            PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ReflectionProbes |
                            lightsPerObjectFlags

        };

       

       drawingSettings.SetShaderPassName(1, _litShaderTagId);
       
       var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: (uint)renderingLayerMask);

       _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
       
       _context.DrawSkybox(_camera);

       if(_isUseColorTexture || _isUseDepthTexture)
       {
            CopyAttachments();
       }
       
       sortingSettings.criteria = SortingCriteria.CommonTransparent;
       drawingSettings.sortingSettings = sortingSettings;
       filteringSettings.renderQueueRange = RenderQueueRange.transparent;
       
       _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
    }

    void Submit()
    {
        _commandBuffer.EndSample(_sampleName);
        ExcuteBuffer();
        _context.Submit();
    }

    void ExcuteBuffer()
    {
        _context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }

    bool Cull(float maxShadowDistance)
    {
       if(_camera.TryGetCullingParameters(out ScriptableCullingParameters p))
       {
            p.shadowDistance = Mathf.Min(maxShadowDistance, _camera.farClipPlane);
            _cullingResults = _context.Cull(ref p);
            return true;
       }

       return false;

    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth = false)
    {
        _commandBuffer.SetGlobalTexture(_sourceTextureId, from);
        _commandBuffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _commandBuffer.DrawProcedural(Matrix4x4.identity, _material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
    }

    void DrawFinal(CameraSettings.FinalBlendMode finalBlendMode)
    {
        _commandBuffer.SetGlobalFloat(_srcBlendId, (float)finalBlendMode._source);
        _commandBuffer.SetGlobalFloat(_dstBlendId, (float)finalBlendMode._destiantion);
        _commandBuffer.SetGlobalTexture(_sourceTextureId, _colorAttachmentId);

        _commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, finalBlendMode._destiantion == BlendMode.Zero && 
            _camera.rect == FullViewRect ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);

        _commandBuffer.SetViewport(_camera.pixelRect);
        _commandBuffer.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);

        _commandBuffer.SetGlobalFloat(_srcBlendId, 1f);
        _commandBuffer.SetGlobalFloat(_dstBlendId, 0f);
    }

    void CopyAttachments()
    {
        if(_isUseColorTexture)
        {
            _commandBuffer.GetTemporaryRT(_colorTextureId, _bufferSize.x, 
                                    _bufferSize.y, 0, 
                                    FilterMode.Bilinear, _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            if(CopyTextureSupported)
            {
                _commandBuffer.CopyTexture(_colorAttachmentId, _colorTextureId);
            }
            else
            {
                Draw(_colorAttachmentId, _colorTextureId);
            }
        }

        if(_isUseDepthTexture)
        {
            _commandBuffer.GetTemporaryRT(_depthTextureId, _bufferSize.x, _bufferSize.y, 
                                        32, FilterMode.Point, RenderTextureFormat.Depth);
            // _commandBuffer.CopyTexture(_depthAttachmentId, _depthTextureId);
 
            if(CopyTextureSupported)
            {
                _commandBuffer.CopyTexture(_depthAttachmentId, _depthTextureId);
            }
            else
            {
                Draw(_depthAttachmentId, _depthTextureId, true);
                // _commandBuffer.SetRenderTarget(_colorAttachmentId, 
                //                     RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, _depthAttachmentId, 
                //                     RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            }

            // ExcuteBuffer();
        }

        if(!CopyTextureSupported)
        {
            _commandBuffer.SetRenderTarget(_colorAttachmentId, 
                                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, 
                                _depthAttachmentId, 
                                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }

        ExcuteBuffer();
    }


    void ConfigureLights()
    {
        // for(int i = 0; i < _cullingResults.visibleLights.Length; ++i)
        // {
        //     if(i == MAX_VISIBLE_LIGHTS) { break; }

        //     VisibleLight light = _cullingResults.visibleLights[i];
        //     _visibleLightColors[i] = light.finalColor;

        //     Vector4 attenuation = Vector4.zero;
        //     attenuation.w = 1f;

        //     if(light.lightType == LightType.Directional)
        //     {
        //         Vector4 v = light.localToWorldMatrix.GetColumn(2);
        //         v.x = -v.x;
        //         v.y = -v.y;
        //         v.z = -v.z;
        //         _visibleLightDirectionsOrPositions[i] = v;
        //     }
        //     else
        //     {
        //         _visibleLightDirectionsOrPositions[i] = light.localToWorldMatrix.GetColumn(3);
        //         attenuation.x = 1f / Mathf.Max(light.range * light.range, 0.000001f);

        //         if(light.lightType == LightType.Spot)
        //         {
        //             Vector4 v = light.localToWorldMatrix.GetColumn(2);
        //             v.x = -v.x;
        //             v.y = -v.y;
        //             v.z = -v.z;
        //             _visibleLightSpotsDirections[i] = v;

        //             float outerRad = Mathf.Deg2Rad * 0.5f * light.spotAngle;
        //             float outerCos = Mathf.Cos(outerRad);
        //             float outerTan = Mathf.Tan(outerRad);
        //             float innerCos = Mathf.Cos(Mathf.Atan(46f / 64f * outerTan));
        //             float angleRange = Mathf.Max(innerCos - outerCos, 0.001f);
        //             attenuation.z = 1f / angleRange;
        //             attenuation.w = -outerCos *  attenuation.z;
        //         }
        //     }   

        //     _visibleLightAttenuations[i] = attenuation;
        // }


        // if(_cullingResults.visibleLights.Length > MAX_VISIBLE_LIGHTS)
        // {
        //     var lightIndices = _cullingResults.GetLightIndexMap(Allocator.Temp);
        //     for(int i = MAX_VISIBLE_LIGHTS; i < _cullingResults.visibleLights.Length; ++i)
        //     {
        //         lightIndices[i] = -1;
        //     }
        //     _cullingResults.SetLightIndexMap(lightIndices);
        // }


        // for(; i < MAX_VISIBLE_LIGHTS; ++i)
        // {
        //     _visibleLightColors[i] = Color.clear;
        // }
    }

    public void Dispose()
    {
        CoreUtils.Destroy(_material);
        CoreUtils.Destroy(_missingTexture);
    }

    void Cleanup()
    {
        _lighting.Clearup();
        // if(_postStack.IsActive)
        if(_isUseIntermediateBuffer)
        {
            _commandBuffer.ReleaseTemporaryRT(_colorAttachmentId);
            _commandBuffer.ReleaseTemporaryRT(_depthAttachmentId);

            if(_isUseColorTexture)
            {
                _commandBuffer.ReleaseTemporaryRT(_colorTextureId);
            }

            if(_isUseDepthTexture)
            {
                _commandBuffer.ReleaseTemporaryRT(_depthTextureId);
            }
        }
    }

}
