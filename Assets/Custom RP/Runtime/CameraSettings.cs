using System;
using UnityEngine.Rendering;


[Serializable]
public class CameraSettings
{
    [Serializable]
    public struct FinalBlendMode
    {
        public BlendMode _source;
        public BlendMode _destiantion;
    };
    public bool _overridePostFx = false;
    public PostFXSettings _postFXSettings = default;

    public FinalBlendMode _finalBlendMode = new FinalBlendMode
    {
        _source = BlendMode.One,
        _destiantion = BlendMode.Zero
    };
};