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
        private static Material _errorMaterial;
        
        partial void PrepareForSceneWindow(CustomRenderPipelineCamera camera);
        partial void PrepareBuffer();
        partial void UnsupportedShaders();
        partial void DrawGizmos();
        partial void DrawGizmosBeforeFX();
        partial void DrawGizmosAfterFX();

#if UNITY_EDITOR
        partial void PrepareForSceneWindow(CustomRenderPipelineCamera camera)
        {
            if (camera._camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera._camera);
                _isUseScaledRendering = false;
            }
        }

        partial void PrepareBuffer()
        {
            Profiler.BeginSample("Editor Only");
            _cmd.Name = _camera.name;
            Profiler.EndSample();
        }

        partial void UnsupportedShaders()
        {
            if (_errorMaterial == null)
            {
                _errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }
            
            var drawSettings = new DrawingSettings(CustomURP.ShaderParams._LegacyShaderTagIdsagId[0], new SortingSettings(_camera._camera))
            {
                overrideMaterial = _errorMaterial,
            };
            
            for (int i = 0; i < CustomURP.ShaderParams._LegacyShaderTagIdsagId.Length; i++)
            {
                drawSettings.SetShaderPassName(i, CustomURP.ShaderParams._LegacyShaderTagIdsagId[i]);
            }
            
            var filteringSettings = FilteringSettings.defaultValue;
            _cmd.DrawRenderers(_cullingResults, ref drawSettings, ref filteringSettings);
        }

        partial void DrawGizmos()
        {
            if (Handles.ShouldRenderGizmos())
            {
                _cmd.DrawGizmos(_camera._camera, GizmoSubset.PreImageEffects);
                _cmd.DrawGizmos(_camera._camera, GizmoSubset.PostImageEffects);
            }
        }

        partial void DrawGizmosBeforeFX()
        {
            
        }

        partial void DrawGizmosAfterFX()
        {
            
        }

#endif
    };
}