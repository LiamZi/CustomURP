using System;
using UnityEngine;
using UnityEngine.Rendering;


[Serializable]
public class CameraSettings
{
    public bool _enabledHizDepth = false;
    public bool _copyColor = true;
    public bool _copyDepth = true;
    
    [RenderingLayerMaskField]
    public int _renderingLayerMask = -1;

    public enum RenderScaleMode
    {
        Inherit,
        Multiply,
        Override,
    };

    public RenderScaleMode _renderScaleMode = RenderScaleMode.Inherit;

    [Range(0.1f, 2f)]
    public float _renderScale = 1f;
    public bool _maskLights = false;
    public bool _overridePostFx = false;
    public PostFXSettings _postFXSettings = default;
    public bool _allowFXAA = false;
    public bool _keepAlpha = false;

    [Serializable]
    public struct FinalBlendMode
    {
        public BlendMode _source;
        public BlendMode _destiantion;
    };

    public FinalBlendMode _finalBlendMode = new FinalBlendMode
    {
        _source = BlendMode.One,
        _destiantion = BlendMode.Zero
    };

    public float GetRenderScale(float scale)
    {
        return _renderScaleMode == RenderScaleMode.Inherit ? scale : _renderScaleMode == RenderScaleMode.Override ? _renderScale : scale * _renderScale;
    }

    // public bool _enabledHiz = false;
};

public struct CustomRenderPipelineCameraSet
{
    public Camera _camera;
    public CustomRenderPipelineCamera _ppCamera;
    public bool _isEditor;
};

