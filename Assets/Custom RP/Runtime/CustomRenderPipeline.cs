using System;
using System.Collections.Generic;
using Core;
using CustomPipeline;
using CustomURP;
using UnityEngine;
using UnityEngine.Rendering;

public unsafe partial class CustomRenderPipeline : RenderPipeline
{
    // bool _useHDR;
    public static  CameraBufferSettings                _cameraBufferSettings;
    private static UnsafeHashMap*                      _actions        = null;
    private static List<CustomRenderPipelineCameraSet> _preFrameCamera = new List<CustomRenderPipelineCameraSet>(10);

    private static readonly List<CustomRenderPipelineCameraSet> _renderCamera =
        new List<CustomRenderPipelineCameraSet>(20);

    private static List<CustomRenderPipelineCameraSet> _endFrameCamera = new List<CustomRenderPipelineCameraSet>(10);

    private static UnsafeList*    _delayReleaseRenderTarget;
    public static  Command        _cmd;
    public static  CameraSettings _defaultCameraSettings = new CameraSettings();

    //private static List<int> _delayReleaseRenderTarget;
    private readonly Scene _scene;

    public CustomRenderPipelineAsset _asset;

    private int              _colorLUTResolution;
    private IndirectSettings _indirectSettings;
    private PostFXSettings   _postFXSettings;
    private ShadowSettings   _shadowSettings;

    private bool _useDynamicBatching;
    private bool _useGPUInstanceing;

    private bool _useLightsPerObject;

    public CustomRenderPipeline(CustomRenderPipelineAsset asset)
    {
        RenderPipelineManager.beginFrameRendering  += BeginFrameRendering;
        RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        RenderPipelineManager.endCameraRendering   += EndCameraRendering;
        RenderPipelineManager.endFrameRendering    += EndFrameRendering;

        _asset = asset;

        GraphicsSettings.lightsUseLinearIntensity            = true;
        _useDynamicBatching                                  = asset.DynamicBatching;
        _cameraBufferSettings                                = asset.CameraBuffer;
        _useGPUInstanceing                                   = asset.GPUInstancing;
        _useLightsPerObject                                  = asset.LightsPerObject;
        _shadowSettings                                      = asset.Shadows;
        _postFXSettings                                      = asset.PostProcessing;
        _colorLUTResolution                                  = (int)asset.ColorLUT;
        GraphicsSettings.useScriptableRenderPipelineBatching = asset.SRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity            = true;
        _indirectSettings                                    = asset.IndirectSettings;

        if (_asset._loadingThread == null) _asset._loadingThread = LoadingThread.Singleton();

        _scene = new Scene(_asset);
        _scene.Awake();

        if (_actions == null) _actions = UnsafeHashMap.Allocate<ulong, int>(_asset._availiableActions.Length);

        _asset.SetRenderingData();
        var actions = _asset._actions;
        DeviceUtility.SetPlatform();
        MiscUtility.Initialization();
        _cmd = new Command("Pipeline Begin");

        for (var i = 0; i < _asset._availiableActions.Length; ++i)
        {
            var action = _asset._availiableActions[i];
            var key    = new UIntPtr(UnsafeUtility.GetPtr(action.GetType()));
            UnsafeHashMap.Add(_actions, key.ToUInt64(), i);
            action.Prepare();
            action.Initialization(_asset);
        }

        _delayReleaseRenderTarget = UnsafeList.Allocate<int>(20);
        //_delayReleaseRenderTarget = new List<int>(20);

        // _renderer = new CameraRenderer(asset.DefaultShader);

        InitializeForEditor();
    }

    public CustomRenderPipeline(CameraBufferSettings cameraBufferSettings, bool isEnabledDynamicBatch,
        bool isEnabledInstancing, bool useSRPBatcher, bool useLightsPerObject,
        ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution,
        Shader cameraRendererShader)
    {
        RenderPipelineManager.beginFrameRendering  += BeginFrameRendering;
        RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        RenderPipelineManager.endCameraRendering   += EndCameraRendering;
        RenderPipelineManager.endFrameRendering    += EndFrameRendering;

        GraphicsSettings.lightsUseLinearIntensity = true;
        _useDynamicBatching                       = isEnabledDynamicBatch;
        _useGPUInstanceing                        = isEnabledInstancing;
        _useLightsPerObject                       = useLightsPerObject;
        _shadowSettings                           = shadowSettings;
        _postFXSettings                           = postFXSettings;
        // this._useHDR = isEnabledHDR;
        _cameraBufferSettings = cameraBufferSettings;
        _colorLUTResolution   = colorLUTResolution;

        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity            = true;
        // _renderer = new CameraRenderer(isEnabledDynamicBatch, isEnabledInstancing);
        // _renderer = new CameraRenderer(cameraRendererShader);

        InitializeForEditor();
    }

    public CameraRenderer Renderer { get; }

    public static bool Editor { get; private set; }

    public static void DelayReleaseRTAfterFrame(int renderTarget)
    {
        UnsafeList.Add(_delayReleaseRenderTarget, renderTarget);
        //_delayReleaseRenderTarget.Add(renderTarget);
    }

    public CoreAction GetAction(Type type)
    {
        var key = new UIntPtr(UnsafeUtility.GetPtr(type));
        if (UnsafeHashMap.TryGetValue(_actions, key.ToUInt64(), out ulong value))
            return _asset._availiableActions[value];

        return null;
    }

    public T GetAction<T>() where T : CoreAction
    {
        var type = typeof(T);
        var key  = new UIntPtr(UnsafeUtility.GetPtr(type));
        if (UnsafeHashMap.TryGetValue(_actions, key.ToUInt64(), out ulong value))
            return _asset._availiableActions[value] as T;

        return null;
    }

    private void BeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
    {
        // Debug.Log("BeginFrameRendering");
        _renderCamera.Clear();
        CustomRenderPipelineCamera.Initialized();
    }

    private void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        // Debug.Log("BeginCameraRendering");
    }

    private void EndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        // Debug.Log("EndCameraRendering");
    }

    private void EndFrameRendering(ScriptableRenderContext context, Camera[] cameras)
    {
        //TODO： Forward rendering has to submit here.
        if (_delayReleaseRenderTarget != null)
        {
            var iters = UnsafeList.GetIterator<int>(_delayReleaseRenderTarget);
            foreach (var i in iters)
                //TODO: release delaying rt at the frame end.
                _cmd.ReleaseTemporaryRT(i);
        }

        _cmd.Context.Submit();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context, cameras);

        foreach (var camera in cameras)
        {
            CustomRenderPipelineCameraSet cameraSet;

            if (!UnsafeHashMap.TryGetValue(CustomRenderPipelineCamera.CameraMap, camera.gameObject.GetInstanceID(),
                    out ulong ppCameraPtr))
            {
                if (!camera.TryGetComponent(out cameraSet._ppCamera))
                {
#if UNITY_EDITOR
                    if (camera.cameraType == CameraType.SceneView)
                    {
                        cameraSet._isEditor = true;
                        var eulerAngle = camera.transform.eulerAngles;
                        eulerAngle.z                 = 0;
                        camera.transform.eulerAngles = eulerAngle;
                        if (!Camera.main ||
                            !(cameraSet._ppCamera = Camera.main.GetComponent<CustomRenderPipelineCamera>()))
                            continue;
                    }
                    else if (camera.cameraType == CameraType.Game)
                    {
                        cameraSet._isEditor = false;
                        cameraSet._ppCamera = camera.gameObject.AddComponent<CustomRenderPipelineCamera>();
                    }
                    else
                    {
                        continue;
                    }
#else
                    cameraSet._isEditor = false;
                    cameraSet._ppCamera = camera.gameObject.AddComponent<CustomRenderPipelineCamera>();
#endif
                }
                else
                {
                    cameraSet._isEditor = false;
                    cameraSet._ppCamera.Add();
                }
            }
            else
            {
                cameraSet._isEditor = false;
                cameraSet._ppCamera =
                    UnsafeUtility.GetObject<CustomRenderPipelineCamera>((void*)ppCameraPtr);
            }

            cameraSet._camera           = camera;
            cameraSet._ppCamera._camera = camera;
            cameraSet._ppCamera.BeforeFrameRending();
            _renderCamera.Add(cameraSet);
        }

        var size          = _asset._actions.Length;
        var propertyFlags = stackalloc bool[size];
        var needSubmit    = false;
        _scene.SetState();
        _cmd.Context = context;
        _cmd.Asset   = _asset;

        Native.MemClear(propertyFlags, size);

        foreach (var camera in _renderCamera)
        {
            Editor = camera._isEditor;
            // BeginCameraRendering(context, camera);
            Render(camera._ppCamera, ref _cmd, propertyFlags);
        }

        // cameraSet._ppCamera._ppCamera.AfterFrameRendering();

        foreach (var camera in _renderCamera) camera._ppCamera.AfterFrameRendering();

        EndFrameRendering(context, cameras);
    }

    private void Render(CustomRenderPipelineCamera camera, ref Command cmd, bool* propertyFlags)
    {
        camera.BeforeCameraRendering();

        cmd.Context.SetupCameraProperties(camera._camera);

        camera.InitRenderTarget(ref cmd, _asset, camera);
        var path    = camera._renderingType;
        var collect = _asset._actions[(int)path];

#if UNITY_EDITOR
        if (!propertyFlags[(int)path])
        {
            propertyFlags[(int)path] = true;
            foreach (var i in collect)
                if (!i.InspectProperty())
                    i.Initialization(_asset);
        }
#endif

        foreach (var i in collect)
        {
            i.BeginRendering(camera, ref _cmd);
            i.Tick(camera, ref _cmd);
            i.EndRendering(camera, ref _cmd);
        }

        // _renderer.Render(context, camera._camera, _cameraBufferSettings, _useDynamicBatching, _useGPUInstanceing,
        // _useLightsPerObject, _shadowSettings, _postFXSettings, _colorLUTResolution);
        camera.AfterCameraRendering();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (_actions != null)
        {
            UnsafeHashMap.Free(_actions);
            _actions = null;
        }

        if (_delayReleaseRenderTarget != null)
        {
            UnsafeList.Free(_delayReleaseRenderTarget);
            _delayReleaseRenderTarget = null;
        }

        if (_scene != null) _scene.Dispose();

        _asset._loadingThread.Dispose();

        for (var i = 0; i < _asset._availiableActions.Length; ++i)
        {
            var action = _asset._availiableActions[i];
            if (action != null) continue;
            action.Dispose();
        }

        DisposeForEditor();
        // _renderer.Dispose();
        RenderPipelineManager.beginFrameRendering  -= BeginFrameRendering;
        RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
        RenderPipelineManager.endCameraRendering   -= EndCameraRendering;
        RenderPipelineManager.endFrameRendering    -= EndFrameRendering;
    }
}