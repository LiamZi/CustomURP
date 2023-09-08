using System;
using UnityEngine.Rendering;


[Serializable]
public class CamearSettings
{
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