using System;
using System.Collections.Generic;
using Core;
using CustomPipeline;
using CustomURP;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

// [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed unsafe class CustomRenderPipelineCamera : MonoBehaviour
{
    public static            UnsafeHashMap* _cameraMap = null;
    // public static Dictionary<int, ulong> _cameraMap = Nullable;
    [SerializeField] private CameraSettings _settings;

    [SerializeField] private HizDepthGenerator      _hizDepthGenerator;
    public                   RenderTargetIdentifier _cameraRT          = BuiltinRenderTextureType.CameraTarget;
    public                   float[]                _layerCullDistance = new float[32];

    public CustomRenderPipelineAsset.CameraRenderType _renderingType =
        CustomRenderPipelineAsset.CameraRenderType.Forward;

    [NonSerialized] public Camera         _camera;
    public                 UnsafeList*    _frustumArray = null;
    [NonSerialized] public RenderTarget   _renderTarget;
    public                 CameraSettings Setting => _settings ?? (_settings = new CameraSettings());

    [NotNull]
    public HizDepthGenerator HizDepth
    {
        get => _hizDepthGenerator;

        set => _hizDepthGenerator = value ?? throw new ArgumentNullException(nameof(value));
    }

    public float3 _frustumMinPoint { get; private set; } = float3.zero;
    public float3 _frustumMaxPoint { get; private set; } = Vector3.zero;

    public static UnsafeHashMap* CameraMap => _cameraMap;

    private void OnDisable()
    {
        // if (_cameraMap != null)
        // {
        //     UnsafeHashMap.Remove(_cameraMap, gameObject.GetInstanceID());
        // }
        // UnsafeList.Free(_frustumArray);
    }

    public void OnDestroy()
    {
        if (_cameraMap != null)
        {
            UnsafeHashMap.Remove(_cameraMap, gameObject.GetInstanceID());
        }
        UnsafeList.Free(_frustumArray);
    }

    public void ResetMatrix()
    {
        var camera = GetComponent<Camera>();
        if(camera == null) return;
        // camera.orthographic = !camera.orthographic;
        camera.ResetCullingMatrix();
        camera.ResetProjectionMatrix();
        camera.ResetStereoProjectionMatrices();
        camera.ResetStereoViewMatrices();
        camera.ResetWorldToCameraMatrix();
    }

    public void InitRenderTarget(ref Command cmd, CustomRenderPipelineAsset asset, CustomRenderPipelineCamera camera)
    {
        if (!(_renderTarget is { _initialized: true }))
        {
            var renderScale = Setting.GetRenderScale(asset.CameraBuffer._renderScale);
            var useHDR      = asset.CameraBuffer._allowHDR && _camera.allowHDR;

            var clearFlags                                      = _camera.clearFlags;
            if (clearFlags > CameraClearFlags.Color) clearFlags = CameraClearFlags.Color;

            _renderTarget = new RenderTarget(ref cmd, _camera.cameraType, Setting, renderScale,
                new int2(_camera.pixelWidth, _camera.pixelHeight), clearFlags, useHDR,
                clearFlags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear);
        }

        ResetMatrix();
    }

    public static void Initialized()
    {
        if (_cameraMap == null)
        {
            _cameraMap = UnsafeHashMap.Allocate<int, ulong>(20);
        }
    }

    public void Add()
    {
        Initialized();
        UnsafeHashMap.Set(_cameraMap, gameObject.GetInstanceID(), (ulong)UnsafeUtility.GetPtr(this));
        if (_frustumArray == null) _frustumArray = UnsafeList.Allocate<float4>(6, true);
    }

    public void BeforeFrameRending()
    {
    }

    public void AfterFrameRendering()
    {
    }

    public void BeforeCameraRendering()
    {
    }

    public void AfterCameraRendering()
    {
    }
}