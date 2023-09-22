using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using static PostFXSettings;

public partial class PostFXStack 
{
    const string _bufferName = "Post Fx";

    const int _maxBloomPyramidLevels = 16;

    static Rect FullViewRect = new Rect(0f, 0f, 1f, 1f);

    CommandBuffer _commandBuffer = new CommandBuffer 
    {
        name = _bufferName
    } ;

    ScriptableRenderContext _context;
    Camera _camera;
    PostFXSettings _settings;
    Vector2Int _bufferSize;
    CameraBufferSettings.FXAA _fxaa;

    enum Pass 
    {
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
		ApplyColorGrading,
        ApplyColorGradingWithLuma,
        FinalRescale,
        FXAA,
        FXAAWithLuam
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
   	int	_finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend");
	int	_finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");
    int _colorGradingResultId = Shader.PropertyToID("_ColorGradingResult");
    int _finalResultId = Shader.PropertyToID("_FinalResult");
    int _copyBicubicId = Shader.PropertyToID("_CopyBicubic");


    bool _isUseHDR = false;
    bool _isKeepAlpha = false;

    public bool IsActive => _settings != null;

    int _bloomPyramidId;

    int _colorLUTResolution;

    CameraSettings.FinalBlendMode _finalBlendMode;

    CameraBufferSettings.BicubicRescalingMode _isBicubicRescaling;

    public PostFXStack()
    {
        _bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for(int i = 1; i < _maxBloomPyramidLevels * 2; ++i)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    public void Setup(ScriptableRenderContext context, Camera camera, 
                    Vector2Int bufferSize, PostFXSettings settings, bool keepAlpha,
                    bool isUseHDR, int colorLUTResolution, CameraSettings.FinalBlendMode finalBlendMode, 
                    CameraBufferSettings.BicubicRescalingMode isBicubicRescaling, CameraBufferSettings.FXAA fxaa)
    {
        _context = context;
        _camera = camera;
        _isUseHDR = isUseHDR;
        _colorLUTResolution = colorLUTResolution;
        _finalBlendMode = finalBlendMode;
        _bufferSize = bufferSize;
        _isBicubicRescaling = isBicubicRescaling;
        _settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        _fxaa = fxaa;
        _isKeepAlpha = keepAlpha;

        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        // _commandBuffer.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        if(Bloom(sourceId))
        {
            Final(_bloomResultId);
            _commandBuffer.ReleaseTemporaryRT(_bloomResultId);
        }
        else
        {
            Final(sourceId);
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

    void DrawFinal(RenderTargetIdentifier from, Pass pass)
    {
        _commandBuffer.SetGlobalFloat(_finalSrcBlendId, (float)_finalBlendMode._source);
        _commandBuffer.SetGlobalFloat(_finalDstBlendId, (float)_finalBlendMode._destiantion);
        _commandBuffer.SetGlobalTexture(_fxSourceId, from);
        // _commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, _finalBlendMode._destiantion == BlendMode.Zero && _camera.rect == FullViewRect ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        _commandBuffer.SetRenderTarget(
			BuiltinRenderTextureType.CameraTarget,
			_finalBlendMode._destiantion == BlendMode.Zero && _camera.rect == FullViewRect ?
				RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load,
			RenderBufferStoreAction.Store
		);
        // _commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, _camera.rect == FullViewRect ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        _commandBuffer.SetViewport(_camera.pixelRect);
        // _commandBuffer.DrawProcedural(Matrix4x4.identity, _settings.Material, (int)Pass.Final, MeshTopology.Triangles, 3);
        _commandBuffer.DrawProcedural(Matrix4x4.identity, _settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    bool Bloom(int sourceId)
    {
        BloomSettings bloom = _settings.Bloom;
		// int width = _camera.pixelWidth / 2, height = _camera.pixelHeight / 2;
        int width = 0;
        int height = 0;
        if(bloom._ignoreRenderScale)
        {
            width = _camera.pixelWidth / 2;
            height = _camera.pixelHeight / 2;
        }
        else
        {
            width = _bufferSize.x / 2;
            height = _bufferSize.y / 2; 
        }

		
		if (bloom._maxIterations == 0 || bloom._intensity <= 0f ||
			height < bloom._downScaleLimit * 2 || width < bloom._downScaleLimit * 2) 
        {
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

		RenderTextureFormat format = _isUseHDR ?
			RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
		_commandBuffer.GetTemporaryRT(
			_bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format
		);
		Draw(
			sourceId, _bloomPrefilterId, bloom._fadeFireFlies ?
				Pass.BloomPrefilterFireflies : Pass.BloomPrefilter
		);
		width /= 2;
		height /= 2;

		int fromId = _bloomPrefilterId, toId = _bloomPyramidId + 1;
		int i;
		for (i = 0; i < bloom._maxIterations; i++) {
			if (height < bloom._downScaleLimit || width < bloom._downScaleLimit) {
				break;
			}
			int midId = toId - 1;
			_commandBuffer.GetTemporaryRT(
				midId, width, height, 0, FilterMode.Bilinear, format
			);
			_commandBuffer.GetTemporaryRT(
				toId, width, height, 0, FilterMode.Bilinear, format
			);
			Draw(fromId, midId, Pass.BloomHorizontal);
			Draw(midId, toId, Pass.BloomVertical);
			fromId = toId;
			toId += 2;
			width /= 2;
			height /= 2;
		}

		_commandBuffer.ReleaseTemporaryRT(_bloomPrefilterId);
		_commandBuffer.SetGlobalFloat(
			_bloomBucibicUpSamplingId, bloom._bicubicUpSampling ? 1f : 0f
		);

		Pass combinePass, finalPass;
		float finalIntensity;
		if (bloom._mode == BloomSettings.Mode.Additive) {
			combinePass = finalPass = Pass.BloomAdd;
			_commandBuffer.SetGlobalFloat(_bloomIntensityId, 1f);
			finalIntensity = bloom._intensity;
		}
		else {
			combinePass = Pass.BloomScatter;
			finalPass = Pass.BloomScatterFinal;
			_commandBuffer.SetGlobalFloat(_bloomIntensityId, bloom._scatter);
			finalIntensity = Mathf.Min(bloom._intensity, 1f);
		}

		if (i > 1) {
			_commandBuffer.ReleaseTemporaryRT(fromId - 1);
			toId -= 5;
			for (i -= 1; i > 0; i--) {
				_commandBuffer.SetGlobalTexture(_fxSourceId2, toId + 1);
				Draw(fromId, toId, combinePass);
				_commandBuffer.ReleaseTemporaryRT(fromId);
				_commandBuffer.ReleaseTemporaryRT(toId + 1);
				fromId = toId;
				toId -= 2;
			}
		}
		else {
			_commandBuffer.ReleaseTemporaryRT(_bloomPyramidId);
		}
		_commandBuffer.SetGlobalFloat(_bloomIntensityId, finalIntensity);
		_commandBuffer.SetGlobalTexture(_fxSourceId2, sourceId);
		_commandBuffer.GetTemporaryRT(
			// _bloomResultId, _camera.pixelWidth, _camera.pixelHeight, 0,
            _bloomResultId, _bufferSize.x, _bufferSize.y, 0,
			FilterMode.Bilinear, format
		);
		Draw(fromId, _bloomResultId, finalPass);
		_commandBuffer.ReleaseTemporaryRT(fromId);
		_commandBuffer.EndSample("Bloom");
		return true;
    }

    void Final(int sourceId)
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

        _commandBuffer.SetGlobalFloat(_finalSrcBlendId, 1f);
        _commandBuffer.SetGlobalFloat(_finalDstBlendId, 0f);
        if(_fxaa._enabled)
        {
            _commandBuffer.GetTemporaryRT(_colorGradingResultId, _bufferSize.x, _bufferSize.y, 
                                            0, FilterMode.Bilinear, RenderTextureFormat.Default);
            Draw(sourceId, _colorGradingResultId, _isKeepAlpha ? Pass.ApplyColorGrading : Pass.ApplyColorGradingWithLuma);
        }

        // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Final);
        if(_bufferSize.x == _camera.pixelWidth)
        {
            if(_fxaa._enabled)
            {
                DrawFinal(_colorGradingResultId, _isKeepAlpha ? Pass.FXAA : Pass.FXAAWithLuam);
                _commandBuffer.ReleaseTemporaryRT(_colorGradingResultId);
            }
            else
            {
                DrawFinal(sourceId, Pass.ApplyColorGrading);
            }
           
        }
        else
        {
            _commandBuffer.GetTemporaryRT(_finalResultId, _bufferSize.x, _bufferSize.y , 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            // _commandBuffer.SetGlobalFloat(_finalSrcBlendId, 1f);
            // _commandBuffer.SetGlobalFloat(_finalDstBlendId, 0f);
            if(_fxaa._enabled)
            {
                Draw(_colorGradingResultId, _finalResultId, Pass.FXAA);
                _commandBuffer.ReleaseTemporaryRT(_colorGradingResultId);
            }
            else
            {
                Draw(sourceId, _finalResultId, Pass.ApplyColorGrading);
            }
            
            bool bicubicSamping = _isBicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpAndDown 
                                || _isBicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpOnly && _bufferSize.x < _camera.pixelWidth; 
            _commandBuffer.SetGlobalFloat(_copyBicubicId, bicubicSamping ? 1f : 0f);
            DrawFinal(_finalResultId, Pass.FinalRescale);
            _commandBuffer.ReleaseTemporaryRT(_finalResultId);
        }
        
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