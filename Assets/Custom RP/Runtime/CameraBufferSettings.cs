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

        // Choose the amount of sub-pixel aliasing removal.
        // This can effect sharpness.
        //   1.00 - upper limit (softer)
        //   0.75 - default amount of filtering
        //   0.50 - lower limit (sharper, less sub-pixel aliasing removal)
        //   0.25 - almost off
        //   0.00 - completely off

        [Range(0f, 1f)]
        public float _subpixelBlending;

        public enum Quality
        {
            Low,
            Medium,
            High
        };

        public Quality _quality;
    };

    public FXAA _fxaa;


};