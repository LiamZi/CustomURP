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
    ShadowSettings _shadowSettings;

    public CameraRenderer Renderer
    {
        get => _renderer;
    }


    public CustomRenderPipeline(bool isEnabledDynamicBatch, bool isEnabledInstancing, bool useSRPBatcher, ShadowSettings shadowSettings)
    {
        GraphicsSettings.lightsUseLinearIntensity = true;
        this._useDynamicBatching = isEnabledDynamicBatch;
        this._useGPUInstanceing = isEnabledInstancing;
        this._shadowSettings = shadowSettings;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        // _renderer = new CameraRenderer(isEnabledDynamicBatch, isEnabledInstancing);

        InitializeForEditor();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras)
        {
            _renderer.Render(context, camera, _useDynamicBatching, _useGPUInstanceing, _shadowSettings);
        }
    }
}
