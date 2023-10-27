using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using CustomURP;
using CommandBuffer = CustomURP.CommandBuffer;

public partial class CameraRenderer
{
    partial void DrawGizmosBeforeFX();
    partial void DrawGizmosAfterFX();
    partial void DrawUnsupportedShaders();
    partial void DrawGizmos();
    partial void PrepareForSceneWindow();
    partial void PrepareBuffer();


#if UNITY_EDITOR

    string _sampleName { get; set; }

    private static ShaderTagId[] _legacyShadertagId =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM"),
    };

    private static Material _errorMaterial;

    partial void DrawUnsupportedShaders()
    {
        if(_errorMaterial == null)
        {
            _errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }    

        var drawingSettings = new DrawingSettings(_legacyShadertagId[0], new SortingSettings(_camera))
        {
            overrideMaterial = _errorMaterial,
        };
        
        for (int i = 0; i < _legacyShadertagId.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, _legacyShadertagId[i]);
        }

        var filteringSettings = FilteringSettings.defaultValue;
        _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
    }

    partial void DrawGizmos()
    {
        if(Handles.ShouldRenderGizmos())
        {
            _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
            _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void PrepareForSceneWindow()
    {
        if(_camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView( _camera );
            _isUseScaledRendering = false;
        }
    }

    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");

        CommandBuffer buffer = null;
        var isExists = CommandBufferManager.Singleton.Exists(_sampleName);
        if (isExists)
        {
            buffer = CommandBufferManager.Singleton.Get(_sampleName);
        }
        else
        {
            isExists = CommandBufferManager.Singleton.Exists(_bufferName);
            buffer = isExists ? CommandBufferManager.Singleton.Get(_bufferName) : CommandBufferManager.Singleton.GetTemporaryCMD(_bufferName);
        }
        
        buffer.Name = _sampleName = _camera.name;
        Profiler.EndSample();
    }

    partial void DrawGizmosBeforeFX()
    {
        if(Handles.ShouldRenderGizmos())
        {
            if(_isUseIntermediateBuffer)
            {
                Draw(_depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
                ExcuteBuffer();
            }
            _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
        }
    }

    partial void DrawGizmosAfterFX()
    {
        if(Handles.ShouldRenderGizmos())
        {
            if(_postStack.IsActive)
            {
                Draw(_depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
                ExcuteBuffer();
            }
            _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }
    }

#else
    const string _sampleName = _bufferName;
#endif
}
