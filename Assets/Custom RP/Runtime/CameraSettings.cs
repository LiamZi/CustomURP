using System;
using UnityEngine.Rendering;


[Serializable]
public class CameraSettings
{
    [RenderingLayerMaskField]
    public int _renderingLayerMask = -1;
    public bool _maskLights = false;
    public bool _overridePostFx = false;
    public PostFXSettings _postFXSettings = default;

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
    
};