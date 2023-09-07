using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using static PostFXSettings;

public partial class PostFXStack 
{
    const string _bufferName = "Post Fx";

    const int _maxBloomPyramidLevels = 16;

    CommandBuffer _commandBuffer = new CommandBuffer 
    {
        name = _bufferName
    } ;

    ScriptableRenderContext _context;
    Camera _camera;
    PostFXSettings _settings;

    // enum Pass
    // {
    //     BloomAdd,
    //     // BloomCombine,
    //     BloomHorizontal,
    //     BloomPrefilter,
    //     BloomPrefilterFireflies,
    //     BloomScatter,
    //     BloomScatterFinal,
    //     BloomVertical,
    //     Copy,
    //     ColorGradingNone,
    //     ColorGradingACES,
    //     ColorGradingNeutral,
    //     ColorGradingReinhard,
    //     Final
    // };

    enum Pass {
		BloomAdd,
		BloomHorizontal,
		BloomPrefilter,
		BloomPrefilterFireflies,
		BloomScatter,
		BloomScatterFinal,
		BloomVertical,
		Copy,
		ColorGradingNone,
		ColorGradingACES,
		ColorGradingNeutral,
		ColorGradingReinhard,
		Final
	}

    int _fxSourceId = Shader.PropertyToID("_PostFXSource");
    int _fxSourceId2 = Shader.PropertyToID("_PostFXSource2");
    int _bloomBucibicUpSamplingId = Shader.PropertyToID("_BloomBicubicUpSampling");
    int _bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
    int _bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    int _bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    int _bloomResultId = Shader.PropertyToID("_BloomResult");
    int _colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments");
    int _colorFilterId = Shader.PropertyToID("_ColorFilter");
    int _whiteBalanceId = Shader.PropertyToID("_WhiteBalance");
    int _splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows");
    int _splitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights");
	int	_channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed");
	int	_channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen");
	int	_channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue");
    int _smhShadowsId = Shader.PropertyToID("_SMHShadows");
    int _smhMidtonesId = Shader.PropertyToID("_SMHMidtones");
    int _smhHighlightsId = Shader.PropertyToID("_SMHHighlights"); 
    int _smhRangeId = Shader.PropertyToID("_SMHRange");
    int _colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT");
    int _colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters"); 
    int _colorGradingLUTInLogCId = Shader.PropertyToID("_ColorGradingLUTInLogC");

    bool _isUseHDR = false;

    public bool IsActive => _settings != null;

    int _bloomPyramidId;

    int _colorLUTResolution;

    public PostFXStack()
    {
        _bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for(int i = 1; i < _maxBloomPyramidLevels * 2; ++i)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings, bool isUseHDR, int colorLUTResolution)
    {
        _context = context;
        _camera = camera;
        _isUseHDR = isUseHDR;
        _colorLUTResolution = colorLUTResolution;
        // _settings = settings;
        _settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        // _commandBuffer.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        if(Bloom(sourceId))
        {
            ColorGradingAndToneMapping(_bloomResultId);
            _commandBuffer.ReleaseTemporaryRT(_bloomResultId);
        }
        else
        {
            ColorGradingAndToneMapping(sourceId);
        }

        _context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        _commandBuffer.SetGlobalTexture(_fxSourceId, from);
        _commandBuffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _commandBuffer.DrawProcedural(Matrix4x4.identity, _settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    bool Bloom(int sourceId)
    {
        // _commandBuffer.BeginSample("Bloom");
        PostFXSettings.BloomSettings bloom = _settings.Bloom;
        int width = _camera.pixelWidth / 2;
        int height = _camera.pixelHeight / 2;

        if(bloom._maxIterations == 0 || bloom._intensity < 0f || 
            height < bloom._downScaleLimit * 2 || width < bloom._downScaleLimit * 2)
        {
            // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            // _commandBuffer.EndSample("Bloom");
            return false;
        }

        _commandBuffer.BeginSample("Bloom");

        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom._threshold);
        threshold.y = threshold.x * bloom._thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        _commandBuffer.SetGlobalVector(_bloomThresholdId, threshold);

        RenderTextureFormat format = _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        _commandBuffer.GetTemporaryRT(_bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
        // Draw(sourceId, _bloomPrefilterId, bloom._fadeFireFlies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);
        Draw(sourceId, _bloomPrefilterId, bloom._fadeFireFlies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);
        width /= 2;
        height /= 2;

        int fromId = _bloomPrefilterId;
        int toId = _bloomPyramidId + 1;

        int i;
        for(i = 0; i < bloom._maxIterations; i++)
        {
            if(height < bloom._downScaleLimit || width < bloom._downScaleLimit) break;

            int midId = toId - 1;
            
            _commandBuffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            _commandBuffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);

            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);
            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }

        _commandBuffer.ReleaseTemporaryRT(_bloomPrefilterId);

        // Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        _commandBuffer.SetGlobalFloat(_bloomBucibicUpSamplingId, bloom._bicubicUpSampling ?  1f :  0f);

        Pass combinePass;
        Pass finalPass;
        float finalIntensity;

        if(bloom._mode == PostFXSettings.BloomSettings.Mode.Additive)
        {
            combinePass = finalPass = Pass.BloomAdd;
            _commandBuffer.SetGlobalFloat(_bloomIntensityId, 1f);
            finalIntensity = bloom._intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            _commandBuffer.SetGlobalFloat(_bloomIntensityId, bloom._scatter);
            finalIntensity = Mathf.Min(bloom._intensity, 0.95f);
        }

        if(i > 1)
        {
            _commandBuffer.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;

            for(i -=1; i >= 0; i--)
            {
                _commandBuffer.SetGlobalTexture(_fxSourceId2, toId + 1);
                Draw(fromId, toId, combinePass);

                _commandBuffer.ReleaseTemporaryRT(fromId);
                _commandBuffer.ReleaseTemporaryRT(fromId + 1);
                fromId = toId;
                fromId -= 2;
            }
        }
        else
        {
            _commandBuffer.ReleaseTemporaryRT(_bloomPyramidId);
        }

        // _commandBuffer.SetGlobalFloat(_bloomIntensityId, bloom._intensity);
        _commandBuffer.SetGlobalFloat(_bloomIntensityId, finalIntensity);
        _commandBuffer.SetGlobalTexture(_fxSourceId2, sourceId);
        _commandBuffer.GetTemporaryRT(_bloomResultId, _camera.pixelWidth, _camera.pixelHeight, 0, FilterMode.Bilinear, format);
        Draw(fromId, _bloomResultId, finalPass);
        _commandBuffer.ReleaseTemporaryRT(fromId);

        _commandBuffer.EndSample("Bloom");

        return true;
    }

    void ColorGradingAndToneMapping(int sourceId)
    {
        ConfigureColorAdjustments();
        ConfigureWhiteBalance();
        ConfigureSplitToning();
        ConfigureChannelMixer();
        ConfigureShadowMidtonesHighlights();

        int lutHeight = _colorLUTResolution;
        int lutWidth = lutHeight * lutHeight;
        _commandBuffer.GetTemporaryRT(_colorGradingLUTId, lutWidth, lutHeight, 
                                0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
        _commandBuffer.SetGlobalVector(_colorGradingLUTParametersId, 
                                    new Vector4(lutHeight, 0.5f / lutWidth, 
                                    0.5f / lutHeight, lutHeight / (lutHeight - 1f)));
       

        PostFXSettings.ToneMappingSettings.Mode mode = _settings.ToneMaping._mode;
        // Pass pass = mode < 0 ? Pass.Copy : Pass.ToneMapingNone + (int)mode;
        Pass pass = Pass.ColorGradingNone + (int)mode;
        _commandBuffer.SetGlobalFloat(_colorGradingLUTInLogCId, _isUseHDR && pass != Pass.ColorGradingNone ? 1f : 0f);

        Draw(sourceId, _colorGradingLUTId, pass);

        _commandBuffer.SetGlobalVector(_colorGradingLUTParametersId, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f));

        Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Final);
        _commandBuffer.ReleaseTemporaryRT(_colorGradingLUTId);
    }

    void ConfigureColorAdjustments()
    {
        ColorAdjustmentsSettings colorAdjustments = _settings.ColorAdjustments;
        //exposure to 2^
        var exposure = Mathf.Pow(2f, colorAdjustments._postExposure);
        //contrast and saturation to [0-2]
        var contrast = colorAdjustments._contrast * 0.01f + 1f;
        //hue to [-1, 1]
        var hueShift = colorAdjustments._hueShift * (1f / 360f);
        var stauration = colorAdjustments._saturation * 0.01f + 1f;

        Vector4 adjuestments = new Vector4(exposure, contrast, hueShift, stauration);
        _commandBuffer.SetGlobalVector(_colorAdjustmentsId, adjuestments);
        _commandBuffer.SetGlobalColor(_colorFilterId, colorAdjustments._colorFilter.linear);
    }

    void ConfigureWhiteBalance()
    {
        WhiteBalanceSettings whiteBalance = _settings.WhiteBalance;
        var white = ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance._temperature, whiteBalance._tint);
        _commandBuffer.SetGlobalVector(_whiteBalanceId, white);
    }

    void ConfigureSplitToning()
    {
        SplitToningSettings splitToningSetting = _settings.SplitToning;
        Color splitColor = splitToningSetting._shadows;
        splitColor.a = splitToningSetting._balance * 0.01f;
        _commandBuffer.SetGlobalColor(_splitToningShadowsId, splitColor);
        _commandBuffer.SetGlobalColor(_splitToningHighlightsId, splitToningSetting._highlights);
    }

    void ConfigureChannelMixer()
    {
        ChannelMixerSettings mixer = _settings.ChannelMixer;
        _commandBuffer.SetGlobalVector(_channelMixerRedId, mixer._red);
        _commandBuffer.SetGlobalVector(_channelMixerGreenId, mixer._green);
        _commandBuffer.SetGlobalVector(_channelMixerBlueId, mixer._blue);
    }

    void ConfigureShadowMidtonesHighlights()
    {
        ShadowMidtonesHighlightsSettings midtones = _settings.ShadowMidtonesHighlights;
        _commandBuffer.SetGlobalColor(_smhShadowsId, midtones._shadows.linear);
        _commandBuffer.SetGlobalColor(_smhMidtonesId, midtones._midtone.linear);
        _commandBuffer.SetGlobalColor(_smhHighlightsId, midtones._highlights.linear);
        var range = new Vector4(midtones._shadowStart, midtones._shadowEnd, midtones._highlightsStart, midtones._hightlightsEnd);
        _commandBuffer.SetGlobalVector(_smhRangeId, range);
        
    }

};