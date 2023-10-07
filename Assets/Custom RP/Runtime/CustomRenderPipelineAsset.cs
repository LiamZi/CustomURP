using System.Collections;
using System.Collections.Generic;
using Core;
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
    Core.IndirectSettings _indirectSettings = default;

    [SerializeField]
    // bool _allowHDR = true;
    CameraBufferSettings _cameraBuffer = new CameraBufferSettings
    {
        _allowHDR = true,
        _renderScale = 1f,
        _fxaa = new CameraBufferSettings.FXAA 
        {
            _fixedThreshold = 0.0833f,
            _relativeThreshold = 0.166f,
            _subpixelBlending = 0.75f
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

    public CameraBufferSettings CameraBuffer
    {
        get => _cameraBuffer;
    }

    public bool DynamicBatching
    {
        get => _dynamicBatching;
    }

    public bool GPUInstancing
    {
        get => _instancing;
    }

    public bool LightsPerObject
    {
        get => _useLightsPerObject;
    }

    public Core.IndirectSettings IndirectSettings
    {
        get => _indirectSettings;
    }

    public ShadowSettings Shadows
    {
        get => _shadows;
    }

    public PostFXSettings PostProcessing
    {
        get => _postFXSettings;
    }

    public ColorLUTResolution ColorLUT
    {
        get => _colorLUTResolution;
    }

    public bool SRPBatcher
    {
        get => _useSRPBatcher;
    }

    public Shader DefaultShader
    {
        get => _cameraRendererShader;
    }

    public LoadingThread _loadingThread;

    public CustomPipeline.PipelineShaders _pipelineShaders = new CustomPipeline.PipelineShaders();
    
    protected override RenderPipeline CreatePipeline()
    {   
        _pipeline = new CustomRenderPipeline(this);

        return _pipeline;
    }


}
