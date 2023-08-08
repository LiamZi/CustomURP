using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;


public class CustomRenderPipeline : RenderPipeline
{
    private CameraRenderer _renderer = new CameraRenderer();

    bool _useDynamicBatching;
    bool _useGPUInstanceing;

    public CameraRenderer Renderer
    {
        get => _renderer;
    }


    public CustomRenderPipeline(bool isEnabledDynamicBatch, bool isEnabledInstancing, bool useSRPBatcher)
    {
        GraphicsSettings.lightsUseLinearIntensity = true;
        this._useDynamicBatching = isEnabledDynamicBatch;
        this._useGPUInstanceing = isEnabledInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        // _renderer = new CameraRenderer(isEnabledDynamicBatch, isEnabledInstancing);
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras)
        {
            _renderer.Render(context, camera, _useDynamicBatching, _useGPUInstanceing);
        }
    }
}
