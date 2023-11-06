using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Core;
using CustomURP;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class RenderingTypeAttribute : Attribute
{
    public CustomRenderPipelineAsset.CameraRenderType _path { get; private set; }

    public RenderingTypeAttribute(CustomRenderPipelineAsset.CameraRenderType type)
    {
        this._path = type;
    }
}

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline AAA")]
public unsafe partial class CustomRenderPipelineAsset : RenderPipelineAsset
{
    public CoreAction[] _availiableActions;
    
    [SerializeField]
    bool _dynamicBatching = true;

    [SerializeField]
    bool _instancing = true;

    [SerializeField]
    bool _useSRPBatcher = true;
    
    [SerializeField]
    bool _useLightsPerObject = true;

    [SerializeField]  
    Core.IndirectSettings _indirectSettings = default;

    [SerializeField]
    // bool _allowHDR = true;
    CameraBufferSettings _cameraBuffer = new CameraBufferSettings
    {
        _allowHDR = true,
        _renderScale = 1f,
        _fxaa = new CameraBufferSettings.FXAA 
        {
            _fixedThreshold = 0.0833f,
            _relativeThreshold = 0.166f,
            _subpixelBlending = 0.75f
        }
    };

    [SerializeField]
    ShadowSettings _shadows = default;

    [SerializeField]
    PostFXSettings _postFXSettings = default;

    public enum ColorLUTResolution 
    {
        _16 = 16,
        _32 = 32,
        _64 = 64
    };

    [SerializeField]
    ColorLUTResolution _colorLUTResolution = ColorLUTResolution._32;

    [SerializeField]
    Shader _cameraRendererShader = default;
    
    private RenderPipeline _pipeline = null;

    public CameraBufferSettings CameraBuffer
    {
        get => _cameraBuffer;
    }

    public bool DynamicBatching
    {
        get => _dynamicBatching;
    }

    public bool GPUInstancing
    {
        get => _instancing;
    }

    public bool LightsPerObject
    {
        get => _useLightsPerObject;
    }

    public Core.IndirectSettings IndirectSettings
    {
        get => _indirectSettings;
    }

    public ShadowSettings Shadows
    {
        get => _shadows;
    }

    public PostFXSettings PostProcessing
    {
        get => _postFXSettings;
    }

    public ColorLUTResolution ColorLUT
    {
        get => _colorLUTResolution;
    }

    public bool SRPBatcher
    {
        get => _useSRPBatcher;
    }

    public Shader DefaultShader
    {
        get => _cameraRendererShader;
    }

    public LoadingThread _loadingThread;

    public CustomPipeline.PipelineShaders _pipelineShaders = new CustomPipeline.PipelineShaders();
    public CoreAction[][] _actions { get; private set; }

    public enum CameraRenderType
    {
        Forward
    };

    
    protected override RenderPipeline CreatePipeline()
    {
        _pipeline = new CustomRenderPipeline(this);
        return _pipeline;
    }

    private static NativeArray<UIntPtr> GetPaths()
    {
        UnsafeList* sets = UnsafeList.Allocate<UIntPtr>(10);
        UnsafeList* typeSets = UnsafeList.Allocate<int>(10);
        FieldInfo[] infos = typeof(Actions).GetFields();

        foreach (var i in infos)
        {
            var tmp = i.GetCustomAttribute(typeof(RenderingTypeAttribute)) as RenderingTypeAttribute;
            if (tmp != null && i.FieldType == typeof(Type[]))
            {
                UnsafeList.Add(sets, new UIntPtr(CustomPipeline.UnsafeUtility.GetPtr(i)));
                UnsafeList.Add(typeSets, (int)tmp._path);
            }
        }

        var count = UnsafeList.Count(sets);
        NativeArray<UIntPtr> paths = new NativeArray<UIntPtr>(count, Allocator.Temp);
        for (int i = 0; i < count; ++i)
        {
            var index = UnsafeList.Get<int>(typeSets, i);
            var value = UnsafeList.Get<UIntPtr>(sets, i);
            paths[index] = value;
        }

        return paths;
    }
    
    public void SetRenderingData()
    {
        NativeArray<UIntPtr> paths = GetPaths();
        _actions = new CoreAction[paths.Length][];
        Dictionary<Type, CoreAction> actionsDict = new Dictionary<Type, CoreAction>(_availiableActions.Length);

        foreach (var i in _availiableActions)
        {
            actionsDict.Add(i.GetType(), i);
        }

        for (int i = 0; i < paths.Length; ++i)
        {
            FieldInfo info = CustomPipeline.UnsafeUtility.GetObject<FieldInfo>(paths[i].ToPointer());
            Type[] t = info.GetValue(null) as Type[];
            _actions[i] = GetAllActions(t, actionsDict);
        }
    }

    public static CoreAction[] GetAllActions(Type[] types, Dictionary<Type, CoreAction> dict)
    {
        CoreAction[] actions = new CoreAction[types.Length];
        for (int i = 0; i < actions.Length; ++i)
        {
            actions[i] = dict[types[i]];
        }

        return actions;
    }
}
