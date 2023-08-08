using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline AAA")]

public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool _dynamicBatching = true;

    [SerializeField]
    bool _instancing = true;

    [SerializeField]
    bool _useSRPBatcher = true;

    private RenderPipeline _pipeline = null;

    protected override RenderPipeline CreatePipeline()
    {   

        _pipeline = new CustomRenderPipeline(_dynamicBatching, _instancing, _useSRPBatcher);

        return _pipeline;
    }
}
