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
    
    [SerializeField]
    bool _useLightsPerObject = true;

    [SerializeField]
    ShadowSettings _shadows = default;

    private RenderPipeline _pipeline = null;

    protected override RenderPipeline CreatePipeline()
    {   

        _pipeline = new CustomRenderPipeline(_dynamicBatching, _instancing, _useSRPBatcher, _useLightsPerObject, _shadows);

        return _pipeline;
    }
}
