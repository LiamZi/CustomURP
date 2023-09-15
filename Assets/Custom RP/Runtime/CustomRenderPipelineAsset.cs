using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline AAA")]

public partial class CustomRenderPipelineAsset : RenderPipelineAsset
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
    bool _allowHDR = true;

    [SerializeField]
    ShadowSettings _shadows = default;

    [SerializeField]
    PostFXSettings _postFXSettings = default;



    public enum ColorLUTResolution 
    {
        _16 = 16,
        _32 = 32,
        _64 = 64
    };

    [SerializeField]
    ColorLUTResolution _colorLUTResolution = ColorLUTResolution._32;

    private RenderPipeline _pipeline = null;

    protected override RenderPipeline CreatePipeline()
    {   

        _pipeline = new CustomRenderPipeline(_allowHDR, _dynamicBatching, _instancing, 
                                        _useSRPBatcher, _useLightsPerObject, 
                                        _shadows, _postFXSettings, (int)_colorLUTResolution);

        return _pipeline;
    }


}
