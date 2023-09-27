using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;


public partial class CustomRenderPipeline : RenderPipeline
{
    private CameraRenderer _renderer;

    bool _useDynamicBatching;
    bool _useGPUInstanceing;
    bool _useLightsPerObject;
    // bool _useHDR;
    CameraBufferSettings _cameraBufferSettings;
    ShadowSettings _shadowSettings;

    PostFXSettings _postFXSettings;

    public CameraRenderer Renderer
    {
        get => _renderer;
    }

    int _colorLUTResolution;

    public CustomRenderPipeline(CustomRenderPipelineAsset asset)
    {
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

        _renderer = new CameraRenderer(asset.DefaultShader);

        InitializeForEditor();
    }


    public CustomRenderPipeline(CameraBufferSettings cameraBufferSettings, bool isEnabledDynamicBatch, bool isEnabledInstancing, 
                            bool useSRPBatcher, bool useLightsPerObject, 
                            ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution , Shader cameraRendererShader)
    {
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
        foreach(Camera camera in cameras)
        {
            _renderer.Render(context, camera, _cameraBufferSettings,
                    _useDynamicBatching, _useGPUInstanceing, 
                    _useLightsPerObject, _shadowSettings, _postFXSettings, _colorLUTResolution);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DisposeForEditor();
        _renderer.Dispose();
    }
}
