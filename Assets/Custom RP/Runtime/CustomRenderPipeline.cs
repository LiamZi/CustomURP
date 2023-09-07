using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;


public partial class CustomRenderPipeline : RenderPipeline
{
    private CameraRenderer _renderer = new CameraRenderer();

    bool _useDynamicBatching;
    bool _useGPUInstanceing;
    bool _useLightsPerObject;
    bool _useHDR;
    ShadowSettings _shadowSettings;

    PostFXSettings _postFXSettings;

    public CameraRenderer Renderer
    {
        get => _renderer;
    }

    int _colorLUTResolution;


    public CustomRenderPipeline(bool isEnabledHDR, bool isEnabledDynamicBatch, bool isEnabledInstancing, 
                            bool useSRPBatcher, bool useLightsPerObject, 
                            ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution)
    {
        GraphicsSettings.lightsUseLinearIntensity = true;
        this._useDynamicBatching = isEnabledDynamicBatch;
        this._useGPUInstanceing = isEnabledInstancing;
        this._useLightsPerObject = useLightsPerObject;
        this._shadowSettings = shadowSettings;
        this._postFXSettings = postFXSettings;
        this._useHDR = isEnabledHDR;
        this._colorLUTResolution = colorLUTResolution;

        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        // _renderer = new CameraRenderer(isEnabledDynamicBatch, isEnabledInstancing);

        InitializeForEditor();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras)
        {
            _renderer.Render(context, camera, _useHDR,
                    _useDynamicBatching, _useGPUInstanceing, 
                    _useLightsPerObject, _shadowSettings, _postFXSettings, _colorLUTResolution);
        }
    }
}
