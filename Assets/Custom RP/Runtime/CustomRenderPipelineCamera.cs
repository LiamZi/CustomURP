using System;
using Core;
using CustomPipeline;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

// [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
[ExecuteInEditMode, RequireComponent(typeof(Camera))]
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
    public RenderTargetIdentifier _cameraRT = BuiltinRenderTextureType.CameraTarget;
    public static UnsafeHashMap* _cameraMap = null;
    public float3 _frustumMinPoint { get; private set; } = float3.zero;
    public float3 _frustumMaxPoint { get; private set; } = Vector3.zero;
    public float[] _layerCullDistance = new float[32];
    public UnsafeList* _frustumArray = null;
   
    public void ResetMatrix()
    {
        Camera camera = GetComponent<Camera>();
        camera.orthographic = !camera.orthographic;
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

};