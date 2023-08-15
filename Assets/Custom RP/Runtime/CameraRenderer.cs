using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Conditional = System.Diagnostics.ConditionalAttribute;
// using UnityEditor.Experimental;
using System.Linq;
using Unity.Collections;

public partial class CameraRenderer
{
    private ScriptableRenderContext _context;

    private Camera _camera;

    private const string _bufferName = "Render Camera Buffer";

    private CommandBuffer _commandBuffer = new CommandBuffer
    {
        name = _bufferName,
    };

    private CullingResults _cullingResults;

    private Lighting _lighting = new Lighting();

    private static ShaderTagId _customURPShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    private static ShaderTagId _litShaderTagId = new ShaderTagId("CustomLit");

    // private bool _isEnabledDynamicBatch = false;
    // public bool EnabledDynamicBatch
    // {
    //     get => _isEnabledDynamicBatch;
    //     set => _isEnabledDynamicBatch = value;
    // }

    // private bool _isEnabledInstacing = false;
    // public bool EnabledInstacing
    // {
    //     get => _isEnabledInstacing;
    //     set => _isEnabledInstacing = value;
    // }


    // private static int _visibleLightColorsId = Shader.PropertyToID("_VisibleLightColors");
    // private static int _visibleLightDirectionsOrPositionsId = Shader.PropertyToID("_VisibleLightDirectionsOrPositions");
    // private static int _visibleLightAttenuationsId = Shader.PropertyToID("_VisibleLightAttenuations");
    // private static int _visibleLightSpotDirectionsId = Shader.PropertyToID("_VisibleLightSpotsDirections");
    // private static int _lightIndicesOffsetAndCountId = Shader.PropertyToID("unity_LightIndicesOffsetAndCount");


    // private Vector4[] _visibleLightColors = new Vector4[MAX_VISIBLE_LIGHTS];
    // private Vector4[] _visibleLightDirectionsOrPositions = new Vector4[MAX_VISIBLE_LIGHTS];
    // private Vector4[] _visibleLightAttenuations = new Vector4[MAX_VISIBLE_LIGHTS];
    // private Vector4[] _visibleLightSpotsDirections = new Vector4[MAX_VISIBLE_LIGHTS];

    public CameraRenderer()
    {
        // _isEnabledDynamicBatch = isEnabledDynamicBatch;
        // _isEnabledInstacing = isEnabledInstancing;
        // QualitySettings.pixelLightCount = 8;
    }

    public void Render(ScriptableRenderContext context, Camera camera, 
                    bool useDynamicBatching, bool useGPUInstanceing, 
                    ShadowSettings shadowSettings)
    {
        this._context = context;
        this._camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();

        if (!Cull(shadowSettings._maxDistance)) return;

        _commandBuffer.BeginSample(_sampleName);
        ExcuteBuffer();

        _lighting.Setup(context, _cullingResults, shadowSettings);
        _commandBuffer.EndSample(_sampleName);

        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstanceing);
        DrawUnsupportedShaders();
        DrawGizmos();
        _lighting.Clearup();
        Submit();
    }

    void Setup()
    {
        _context.SetupCameraProperties(_camera);
        CameraClearFlags flags = _camera.clearFlags;

        //_commandBuffer.ClearRenderTarget(true, true, Color.clear);
        _commandBuffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, 
                                        flags == CameraClearFlags.Color, 
                                        flags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear);
        // ConfigureLights();
        // if(_cullingResults.visibleLights.Length > 0)
        // {
        //     ConfigureLights();
        // }
        // else
        // {
        //     _commandBuffer.SetGlobalVector(_lightIndicesOffsetAndCountId, Vector4.zero);
        // }

        _commandBuffer.BeginSample(_sampleName);

        // _commandBuffer.SetGlobalVectorArray(_visibleLightColorsId, _visibleLightColors);
        // _commandBuffer.SetGlobalVectorArray(_visibleLightDirectionsOrPositionsId, _visibleLightDirectionsOrPositions);
        // _commandBuffer.SetGlobalVectorArray(_visibleLightAttenuationsId, _visibleLightAttenuations);
        // _commandBuffer.SetGlobalVectorArray(_visibleLightSpotDirectionsId, _visibleLightSpotsDirections);
        
        ExcuteBuffer();
       
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstanceing)
    {
        var sortingSettings = new SortingSettings(_camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };

        // PerObjectData lightsPerObjectFlags = PerObjectData.LightData | PerObjectData.LightIndices;

        var drawingSettings = new DrawingSettings(_customURPShaderTagId, sortingSettings)
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstanceing,
            // perObjectData = drawingSettings.perObjectData |  PerObjectData.LightIndices;
            // perObjectData = PerObjectData.LightIndices,
            perObjectData = PerObjectData.LightIndices | PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume
        };

       

       drawingSettings.SetShaderPassName(1, _litShaderTagId);
       
       var filteringSettings = new FilteringSettings(RenderQueueRange.all);
       
       _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
       
       _context.DrawSkybox(_camera);
       
       sortingSettings.criteria = SortingCriteria.CommonTransparent;
       
       drawingSettings.sortingSettings = sortingSettings;
       
       filteringSettings.renderQueueRange = RenderQueueRange.transparent;
       
       _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
    }

    void Submit()
    {
        _commandBuffer.EndSample(_sampleName);
        ExcuteBuffer();
        _context.Submit();
    }

    void ExcuteBuffer()
    {
        _context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }

    bool Cull(float maxShadowDistance)
    {
       if(_camera.TryGetCullingParameters(out ScriptableCullingParameters p))
       {
            p.shadowDistance = Mathf.Min(maxShadowDistance, _camera.farClipPlane);
            _cullingResults = _context.Cull(ref p);
            return true;
       }

       return false;

    }

    void ConfigureLights()
    {
        // for(int i = 0; i < _cullingResults.visibleLights.Length; ++i)
        // {
        //     if(i == MAX_VISIBLE_LIGHTS) { break; }

        //     VisibleLight light = _cullingResults.visibleLights[i];
        //     _visibleLightColors[i] = light.finalColor;

        //     Vector4 attenuation = Vector4.zero;
        //     attenuation.w = 1f;

        //     if(light.lightType == LightType.Directional)
        //     {
        //         Vector4 v = light.localToWorldMatrix.GetColumn(2);
        //         v.x = -v.x;
        //         v.y = -v.y;
        //         v.z = -v.z;
        //         _visibleLightDirectionsOrPositions[i] = v;
        //     }
        //     else
        //     {
        //         _visibleLightDirectionsOrPositions[i] = light.localToWorldMatrix.GetColumn(3);
        //         attenuation.x = 1f / Mathf.Max(light.range * light.range, 0.000001f);

        //         if(light.lightType == LightType.Spot)
        //         {
        //             Vector4 v = light.localToWorldMatrix.GetColumn(2);
        //             v.x = -v.x;
        //             v.y = -v.y;
        //             v.z = -v.z;
        //             _visibleLightSpotsDirections[i] = v;

        //             float outerRad = Mathf.Deg2Rad * 0.5f * light.spotAngle;
        //             float outerCos = Mathf.Cos(outerRad);
        //             float outerTan = Mathf.Tan(outerRad);
        //             float innerCos = Mathf.Cos(Mathf.Atan(46f / 64f * outerTan));
        //             float angleRange = Mathf.Max(innerCos - outerCos, 0.001f);
        //             attenuation.z = 1f / angleRange;
        //             attenuation.w = -outerCos *  attenuation.z;
        //         }
        //     }   

        //     _visibleLightAttenuations[i] = attenuation;
        // }


        // if(_cullingResults.visibleLights.Length > MAX_VISIBLE_LIGHTS)
        // {
        //     var lightIndices = _cullingResults.GetLightIndexMap(Allocator.Temp);
        //     for(int i = MAX_VISIBLE_LIGHTS; i < _cullingResults.visibleLights.Length; ++i)
        //     {
        //         lightIndices[i] = -1;
        //     }
        //     _cullingResults.SetLightIndexMap(lightIndices);
        // }


        // for(; i < MAX_VISIBLE_LIGHTS; ++i)
        // {
        //     _visibleLightColors[i] = Color.clear;
        // }
    }


}
