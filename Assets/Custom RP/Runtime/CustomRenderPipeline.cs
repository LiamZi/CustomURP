using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Core;
using Custom_RP.Logic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;


public unsafe partial class CustomRenderPipeline : RenderPipeline
{
    private CameraRenderer _renderer;

    bool _useDynamicBatching;
    bool _useGPUInstanceing;

    bool _useLightsPerObject;

    // bool _useHDR;
    CameraBufferSettings _cameraBufferSettings;
    ShadowSettings _shadowSettings;
    PostFXSettings _postFXSettings;
    Core.IndirectSettings _indirectSettings;

    public CameraRenderer Renderer
    {
        get => _renderer;
    }

    int _colorLUTResolution;

    public CustomRenderPipelineAsset _resources;
    private static unsafe Core.UnsafeHashMap* _actions = null;
    private static List<CustomRenderPipelineCamera> _preFrameCamera = new List<CustomRenderPipelineCamera>(10);
    private static List<CustomRenderPipelineCamera> _renderCamera = new List<CustomRenderPipelineCamera>(20);
    private static List<CustomRenderPipelineCamera> _endFrameCamera = new List<CustomRenderPipelineCamera>(10);

    public CustomRenderPipeline(CustomRenderPipelineAsset asset)
    {
        RenderPipelineManager.beginFrameRendering += BeginFrameRendering;
        RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        RenderPipelineManager.endCameraRendering += EndCameraRendering;
        RenderPipelineManager.endFrameRendering += EndFrameRendering;

        this._resources = asset;

        GraphicsSettings.lightsUseLinearIntensity = true;
        this._useDynamicBatching = asset.DynamicBatching;
        this._cameraBufferSettings = asset.CameraBuffer;
        this._useGPUInstanceing = asset.GPUInstancing;
        this._useLightsPerObject = asset.LightsPerObject;
        this._shadowSettings = asset.Shadows;
        this._postFXSettings = asset.PostProcessing;
        this._colorLUTResolution = (int)asset.ColorLUT;
        GraphicsSettings.useScriptableRenderPipelineBatching = asset.SRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        this._indirectSettings = asset.IndirectSettings;

        if (_resources._loadingThread == null)
        {
            _resources._loadingThread = LoadingThread.Singleton();
        }

        SceneController.Awake(_resources);

        if (_actions == null)
        {
            _actions = UnsafeHashMap.Allocate<ulong, int>(_resources._availiableActions.Length);
        }
        
        _resources.SetRenderingData();
        var actions = _resources._actions;
        CustomPipeline.GraphicsUtility.SetPlatform();

        _renderer = new CameraRenderer(asset.DefaultShader);

        InitializeForEditor();
    }

    private new void BeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
    {
        // Debug.Log("BeginFrameRendering");
    }

    private new void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        // Debug.Log("BeginCameraRendering");
    }

    private new void EndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        // Debug.Log("EndCameraRendering");
    }

    private new void EndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
    {
        // Debug.Log("EndFrameRendering");
    }

    public CustomRenderPipeline(CameraBufferSettings cameraBufferSettings, bool isEnabledDynamicBatch,
        bool isEnabledInstancing, bool useSRPBatcher, bool useLightsPerObject,
        ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution,
        Shader cameraRendererShader)
    {
        RenderPipelineManager.beginFrameRendering += BeginFrameRendering;
        RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        RenderPipelineManager.endCameraRendering += EndCameraRendering;
        RenderPipelineManager.endFrameRendering += EndFrameRendering;

        GraphicsSettings.lightsUseLinearIntensity = true;
        this._useDynamicBatching = isEnabledDynamicBatch;
        this._useGPUInstanceing = isEnabledInstancing;
        this._useLightsPerObject = useLightsPerObject;
        this._shadowSettings = shadowSettings;
        this._postFXSettings = postFXSettings;
        // this._useHDR = isEnabledHDR;
        this._cameraBufferSettings = cameraBufferSettings;
        this._colorLUTResolution = colorLUTResolution;

        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        // _renderer = new CameraRenderer(isEnabledDynamicBatch, isEnabledInstancing);
        _renderer = new CameraRenderer(cameraRendererShader);

        InitializeForEditor();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context, cameras);

        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context, camera);
            _renderer.Render(context, camera, _cameraBufferSettings,
                _useDynamicBatching, _useGPUInstanceing,
                _useLightsPerObject, _shadowSettings, _postFXSettings, _colorLUTResolution);
            EndCameraRendering(context, camera);
        }

        EndFrameRendering(context, cameras);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DisposeForEditor();
        _renderer.Dispose();
        RenderPipelineManager.beginFrameRendering -= BeginFrameRendering;
        RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= EndCameraRendering;
        RenderPipelineManager.endFrameRendering -= EndFrameRendering;
    }




};
