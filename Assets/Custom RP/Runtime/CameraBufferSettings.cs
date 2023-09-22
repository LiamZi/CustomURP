using System;
using UnityEngine;

[System.Serializable]

public struct CameraBufferSettings
{
    public bool _allowHDR;
    public bool _copyColor;
    public bool _copyColorReflection;
    public bool _copyDepth;
    public bool _copyDepthReflection;

    [Range(0.1f, 2f)]
    public float _renderScale;
    // public bool _bicubicRescaling;

    public enum BicubicRescalingMode
    {
        Off,
        UpOnly,
        UpAndDown
    };

    public BicubicRescalingMode _bicubicRescaling;

    [Serializable]
    public struct FXAA
    {
        public bool _enabled;

        // Trims the algorithm from processing darks.
        //   0.0833 - upper limit (default, the start of visible unfiltered edges)
        //   0.0625 - high quality (faster)
        //   0.0312 - visible limit (slower)
        
        [Range(0.0312f, 0.0833f)]
        public float _fixedThreshold;

        // The minimum amount of local contrast required to apply algorithm.
        //   0.333 - too little (faster)
        //   0.250 - low quality
        //   0.166 - default
        //   0.125 - high quality 
        //   0.063 - overkill (slower)

        [Range(0.063f, 0.333f)]
        public float _relativeThreshold;
    };

    public FXAA _fxaa;


};