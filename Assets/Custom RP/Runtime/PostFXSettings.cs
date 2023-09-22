using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using UnityEngine.SocialPlatforms;
using System.Linq.Expressions;

[CreateAssetMenu(fileName = "PostFXSettings", menuName = "Rendering/Custom Post FX Settings", order = 0)]
public class PostFXSettings : ScriptableObject 
{
    [SerializeField]
    Shader _shader = default;

    [System.NonSerialized]
    Material _material;

    [System.Serializable]
    public struct BloomSettings
    {
        public bool _ignoreRenderScale;

        [Range(0f, 16f)]
        public int _maxIterations;

        [Min(1f)]
        public int _downScaleLimit;
        public bool _bicubicUpSampling;

        [Min(0f)]
        public float _threshold;
        
        [Range(0f, 1f)]
        public float _thresholdKnee;
        
        [Min(0f)]
        public float _intensity;

        public bool _fadeFireFlies;

        public enum Mode 
        {
            Additive,
            Scattering
        };

        public Mode _mode;

        [Range(0.05f, 0.95f)]
        public float _scatter;
    }

    [SerializeField]
    BloomSettings _bloom = new BloomSettings 
    {
        _scatter = 0.7f
    };

    public BloomSettings Bloom => _bloom;

    [System.Serializable]
    public struct ToneMappingSettings
    {
        public enum Mode 
        {
            None,
            ACES,
            Neutral,
            Reinhard
        };

        public Mode _mode;
    };

    [SerializeField]
    ToneMappingSettings _toneMapping = default;

    public ToneMappingSettings ToneMaping => _toneMapping;

    [Serializable]
    public struct ColorAdjustmentsSettings
    {
        //曝光
        public float _postExposure;
        
        //对比度
        [Range(-100f, 100f)]
        public float _contrast;
        //滤色器
        [ColorUsage(false, true)]
        public Color _colorFilter;
        //色调偏移
        [Range(-180f, 180f)]
        public float _hueShift;
        //饱和度
        [Range(-100f, 100f)]
        public float _saturation;


    };

	[SerializeField]
	ColorAdjustmentsSettings _colorAdjustments = new ColorAdjustmentsSettings 
    {
		_colorFilter = Color.white
	};

    public ColorAdjustmentsSettings ColorAdjustments => _colorAdjustments;


    [Serializable]
    public struct WhiteBalanceSettings
    {
        [Range(-100f, 100f)]
        public float _temperature;
        [Range(-100f, 100f)]
        public float _tint;
    };

    [SerializeField]
    WhiteBalanceSettings _whiteBalanceSettings = default;

    public WhiteBalanceSettings WhiteBalance => _whiteBalanceSettings;

    [Serializable]
    public struct SplitToningSettings
    {
        [ColorUsage(false)]
        public Color _shadows;
        
        [ColorUsage(false)]
        public Color _highlights;

        [Range(-100f, 100f)]
        public float _balance;
    };

    [SerializeField]
    SplitToningSettings _splitToningSettings = new SplitToningSettings
    {
        _shadows = Color.gray,
        _highlights = Color.gray
    };

    public SplitToningSettings SplitToning => _splitToningSettings;


    [Serializable]
    public struct ChannelMixerSettings
    {
        public Vector3 _red;
        public Vector3 _green;
        public Vector3 _blue;
    };

    [SerializeField]
    ChannelMixerSettings _channelMixerSettings = new ChannelMixerSettings
    {
        _red = Vector3.right,
        _green = Vector3.up,
        _blue = Vector3.forward
    };

    public ChannelMixerSettings ChannelMixer => _channelMixerSettings;

    [Serializable]
    public struct ShadowMidtonesHighlightsSettings
    {
        [ColorUsage(false, true)]
        public Color _shadows;
        
        [ColorUsage(false, true)]
        public Color _midtone;

        [ColorUsage(false, true)]
        public Color _highlights;

        [Range(0f, 2f)]
        public float _shadowStart;
        [Range(0f, 2f)]
        public float _shadowEnd;
        [Range(0f, 2f)]
        public float _highlightsStart;
        [Range(0f, 2f)]
        public float _hightlightsEnd;
    };

    [SerializeField]
    ShadowMidtonesHighlightsSettings _shadowMidtonesHighlightsSettings = new ShadowMidtonesHighlightsSettings
    {
        _shadows = Color.white,
        _midtone = Color.white,
        _highlights = Color.white,
        _shadowEnd = 0.3f,
        _highlightsStart = 0.55f,
        _hightlightsEnd = 1f
    };

    public ShadowMidtonesHighlightsSettings ShadowMidtonesHighlights => _shadowMidtonesHighlightsSettings;

    public Material Material
    {
        get 
        {
            if(_material == null && _shader != null)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.HideAndDontSave;
            }
            return _material;
        }
    }
}