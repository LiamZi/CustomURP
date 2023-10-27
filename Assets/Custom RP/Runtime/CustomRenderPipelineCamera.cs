using System;
using CustomPipeline;
using JetBrains.Annotations;
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Camera))]

public class CustomRenderPipelineCamera : MonoBehaviour
{
    [SerializeField] CameraSettings _settings = default;

    [SerializeField] CustomPipeline.HizDepthGenerator _hizDepthGenerator = null;
    public CameraSettings Setting => _settings ?? (_settings = new CameraSettings());

    [NotNull]
    public CustomPipeline.HizDepthGenerator HizDepth
    {
        get => _hizDepthGenerator;

        set => _hizDepthGenerator = value ?? throw new ArgumentNullException(nameof(value));
    }
    
};