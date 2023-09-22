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
    };

    public FXAA _fxaa;


};