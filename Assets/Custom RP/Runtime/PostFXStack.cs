using System;
using UnityEngine;
using UnityEngine.Rendering;

public partial class PostFXStack 
{
    const string _bufferName = "Post Fx";

    CommandBuffer _commandBuffer = new CommandBuffer 
    {
        name = _bufferName
    } ;

    ScriptableRenderContext _context;
    Camera _camera;
    PostFXSettings _settings;

    enum Pass
    {
        Copy,
    };

    int _fxSourceId = Shader.PropertyToID("_PostFXSource");

    public bool IsActive => _settings != null;

    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings)
    {
        _context = context;
        _camera = camera;
        // _settings = settings;
        _settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        ApplySceneViewState();
    }

    public void Render(int sourceId)
    {
        // _commandBuffer.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        _context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        _commandBuffer.SetGlobalTexture(_fxSourceId, from);
        _commandBuffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _commandBuffer.DrawProcedural(Matrix4x4.identity, _settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

};