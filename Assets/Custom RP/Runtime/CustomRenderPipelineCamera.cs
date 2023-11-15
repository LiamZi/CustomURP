using System;
using Core;
using CustomPipeline;
using CustomURP;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

// [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
[ExecuteInEditMode, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public unsafe sealed class CustomRenderPipelineCamera : MonoBehaviour
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

    [NonSerialized] public Camera _camera;
    [NonSerialized] public RenderTarget _renderTarget;
    public RenderTargetIdentifier _cameraRT = BuiltinRenderTextureType.CameraTarget;
    public static UnsafeHashMap* _cameraMap = null;
    public float3 _frustumMinPoint { get; private set; } = float3.zero;
    public float3 _frustumMaxPoint { get; private set; } = Vector3.zero;
    public float[] _layerCullDistance = new float[32];
    public UnsafeList* _frustumArray = null;

    public CustomRenderPipelineAsset.CameraRenderType _renderingType =
        CustomRenderPipelineAsset.CameraRenderType.Forward;
   
    public void ResetMatrix()
    {
        Camera camera = GetComponent<Camera>();
        // camera.orthographic = !camera.orthographic;
        camera.ResetCullingMatrix();
        camera.ResetProjectionMatrix();
        camera.ResetStereoProjectionMatrices();
        camera.ResetStereoViewMatrices();
        camera.ResetWorldToCameraMatrix();
    }

    public static UnsafeHashMap* CameraMap
    {
        get { return _cameraMap; }
    }

    public void InitRenderTarget(ref Command cmd, CustomRenderPipelineAsset asset, CustomRenderPipelineCamera camera)
    {
        if (!(_renderTarget is { _initialized: true }))
        {
            float renderScale = Setting.GetRenderScale(asset.CameraBuffer._renderScale);
            bool useHDR = asset.CameraBuffer._allowHDR && _camera.allowHDR;

            CameraClearFlags clearFlags = _camera.clearFlags;
            if (clearFlags > CameraClearFlags.Color)
            {
                clearFlags = CameraClearFlags.Color;
            }
            
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
        UnsafeHashMap.Set(_cameraMap, gameObject.GetInstanceID(), (ulong)CustomPipeline.UnsafeUtility.GetPtr(this));
        if (_frustumArray == null)
        {
            _frustumArray = UnsafeList.Allocate<float4>(6, true);
        }
    }

    private void OnDisable()
    {
        if (_cameraMap != null)
        {
            UnsafeHashMap.Remove( _cameraMap, gameObject.GetInstanceID());
        }
        UnsafeList.Free(_frustumArray); 
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
};