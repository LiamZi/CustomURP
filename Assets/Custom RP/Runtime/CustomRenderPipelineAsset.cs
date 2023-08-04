using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline AAA")]

public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool _dynamicBatching;

    [SerializeField]
    bool _instancing;

    private RenderPipeline _pipeline = null;

    protected override RenderPipeline CreatePipeline()
    {   

        _pipeline = new CustomRenderPipeline(_dynamicBatching, _instancing);

        return _pipeline;
    }
}
