using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public partial class PostFXStack 
{
    const string _bufferName = "Post Fx";

    const int _maxBloomPyramidLevels = 16;

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

    int _bloomPyramidId;

    public PostFXStack()
    {
        _bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for(int i = 1; i < _maxBloomPyramidLevels; ++i)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

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
        // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);/
        Bloom(sourceId);
        _context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        _commandBuffer.SetGlobalTexture(_fxSourceId, from);
        _commandBuffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _commandBuffer.DrawProcedural(Matrix4x4.identity, _settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    void Bloom(int sourceId)
    {
        _commandBuffer.BeginSample("Bloom");
        int width = _camera.pixelWidth / 2;
        int height = _camera.pixelHeight / 2;
        RenderTextureFormat format = RenderTextureFormat.Default;
        int fromId = sourceId;
        int toId = _bloomPyramidId;

        int i;
        for(i = 0; i < _maxBloomPyramidLevels; i++)
        {
            if(height < 1 || width < 1) break;

            _commandBuffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            Draw(fromId, toId, Pass.Copy);
            fromId = toId;
            toId += 1;
            width /= 2;
            height /= 2;
        }

        // Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);

        for(i -=1; i >= 0; i--)
        {
            _commandBuffer.ReleaseTemporaryRT(_bloomPyramidId + i);
        }

        _commandBuffer.EndSample("Bloom");

    }

};