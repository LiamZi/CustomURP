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
    // bool _allowHDR = true;
    CameraBufferSettings _cameraBuffer = new CameraBufferSettings
    {
        _allowHDR = true,
        _renderScale = 1f,
        _fxaa = new CameraBufferSettings.FXAA 
        {
            _fixedThreshold = 0.0833f,
            _relativeThreshold = 0.166f 
        }
    };

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

    [SerializeField]
    Shader _cameraRendererShader = default;

    private RenderPipeline _pipeline = null;

    protected override RenderPipeline CreatePipeline()
    {   

        _pipeline = new CustomRenderPipeline(_cameraBuffer, _dynamicBatching, _instancing, 
                                        _useSRPBatcher, _useLightsPerObject, 
                                        _shadows, _postFXSettings, (int)_colorLUTResolution, _cameraRendererShader);

        return _pipeline;
    }


}
