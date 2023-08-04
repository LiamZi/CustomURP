using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;


public class CustomRenderPipeline : RenderPipeline
{
    private CameraRenderer _renderer = null;

    public CameraRenderer Renderer
    {
        get => _renderer;
    }


    public CustomRenderPipeline(bool isEnabledDynamicBatch, bool isEnabledInstancing)
    {
        //if (_renderer == null)
        //{

        //}
        //else
        //{
        //    _renderer.EnabledDynamicBatch = isEnabledDynamicBatch;
        //    _renderer.EnabledInstacing = isEnabledInstancing;
        //}
        GraphicsSettings.lightsUseLinearIntensity = true;
        _renderer = new CameraRenderer(isEnabledDynamicBatch, isEnabledInstancing);
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras)
        {
            _renderer.Render(context, camera);
        }
    }


}
