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
        BloomCombine,
        BloomHorizontal,
        BloomPrefilter,
        BloomVertical,
        Copy,
    };

    int _fxSourceId = Shader.PropertyToID("_PostFXSource");
    int _fxSourceId2 = Shader.PropertyToID("_PostFXSource2");
    int _bloomBucibicUpSamplingId = Shader.PropertyToID("_BloomBicubicUpSampling");
    int _bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
    int _bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    int _bloomIntensityId = Shader.PropertyToID("_BloomIntensity");


    public bool IsActive => _settings != null;

    int _bloomPyramidId;

    public PostFXStack()
    {
        _bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for(int i = 1; i < _maxBloomPyramidLevels * 2; ++i)
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
        PostFXSettings.BloomSettings bloom = _settings.Bloom;
        int width = _camera.pixelWidth / 2;
        int height = _camera.pixelHeight / 2;

        if(bloom._maxIterations == 0 || bloom._intensity < 0f || height < bloom._downScaleLimit * 2 || width < bloom._downScaleLimit * 2)
        {
            Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            _commandBuffer.EndSample("Bloom");
            return;
        }

        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom._threshold);
        threshold.y = threshold.x * bloom._thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        _commandBuffer.SetGlobalVector(_bloomThresholdId, threshold);

        RenderTextureFormat format = RenderTextureFormat.Default;
        _commandBuffer.GetTemporaryRT(_bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
        Draw(sourceId, _bloomPrefilterId, Pass.BloomPrefilter);
        width /= 2;
        height /= 2;

        int fromId = _bloomPrefilterId;
        int toId = _bloomPyramidId + 1;

        int i;
        for(i = 0; i < bloom._maxIterations; i++)
        {
            if(height < bloom._downScaleLimit || width < bloom._downScaleLimit) break;

            int midId = toId - 1;
            
            _commandBuffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            _commandBuffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);

            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);
            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }

        _commandBuffer.ReleaseTemporaryRT(_bloomPrefilterId);

        // Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        _commandBuffer.SetGlobalFloat(_bloomBucibicUpSamplingId, bloom._bicubicUpSampling ?  1f :  0f);
        _commandBuffer.SetGlobalFloat(_bloomIntensityId, bloom._intensity);
        if(i > 1)
        {
            _commandBuffer.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;

            for(i -=1; i >= 0; i--)
            {
                _commandBuffer.SetGlobalTexture(_fxSourceId2, toId + 1);
                Draw(fromId, toId, Pass.BloomCombine);

                _commandBuffer.ReleaseTemporaryRT(fromId);
                _commandBuffer.ReleaseTemporaryRT(fromId + 1);
                fromId = toId;
                fromId -= 2;
            }
        }
        else
        {
            _commandBuffer.ReleaseTemporaryRT(_bloomPyramidId);
        }

        _commandBuffer.SetGlobalTexture(_fxSourceId2, sourceId);
        Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.BloomCombine);
        _commandBuffer.ReleaseTemporaryRT(fromId);

        _commandBuffer.EndSample("Bloom");

    }

};