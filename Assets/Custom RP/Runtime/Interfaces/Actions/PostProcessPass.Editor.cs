using UnityEditor;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public sealed unsafe partial class PostProcessPass : CoreAction
    {
        partial void DrawGizmosBeforePostPass();
        partial void DrawGizmosAfterPostPass();
        
#if UNITY_EDITOR
        partial void DrawGizmosBeforePostPass()
        {
            if (Handles.ShouldRenderGizmos())
            {
                if (_isUseIntermediateBuffer)
                {
                    //Draw(_depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
                    _camera._renderTarget.Draw(ref _cmd, _camera._renderTarget._depthAttachmentId, BuiltinRenderTextureType.CameraTarget, _material, true);
                    //ExcuteBuffer();
                    _cmd.Execute();
                }
                _cmd.Context.DrawGizmos(_camera._camera, GizmoSubset.PreImageEffects);
            }
        }

        partial void DrawGizmosAfterPostPass()
        {
            if (Handles.ShouldRenderGizmos())
            {
                if (_pass.IsActive)
                {
                    _camera._renderTarget.Draw(ref _cmd, _camera._renderTarget._depthAttachmentId, BuiltinRenderTextureType.CameraTarget, _material, true);
                    //ExcuteBuffer();
                    _cmd.Execute();
                }
                _cmd.Context.DrawGizmos(_camera._camera, GizmoSubset.PostImageEffects);
            }
        }
#endif
    };
}
