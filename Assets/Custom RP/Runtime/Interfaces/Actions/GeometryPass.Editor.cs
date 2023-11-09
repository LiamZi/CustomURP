using System.Collections;
using Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;


namespace CustomURP
{
    public sealed unsafe partial class GeometryPass : CoreAction
    {
        partial void PrepareForSceneWindow(CustomRenderPipelineCamera camera);
        partial void PrepareBuffer();
        
#if UNITY_EDITOR
        partial void PrepareForSceneWindow(CustomRenderPipelineCamera camera)
        {
            if(camera._camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView( camera._camera );
                _isUseScaledRendering = false;
            }
        }
        
        partial void PrepareBuffer()
        {
            Profiler.BeginSample("Editor Only");
            //
            // Command buffer = null;
            // var isExists = CmdManager.Singleton.Exists(_sampleName);
            // if (isExists)
            // {
            //     buffer = CmdManager.Singleton.Get(_sampleName);
            // }
            // else
            // {
            //     isExists = CmdManager.Singleton.Exists(_bufferName);
            //     buffer = isExists ? CmdManager.Singleton.Get(_bufferName) : CmdManager.Singleton.GetTemporaryCMD(_bufferName);
            // }
            //
            // buffer.Name = _sampleName = _camera.name;
            Profiler.EndSample();
        }
        
#endif
    }
}