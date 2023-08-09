using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    const string _bufferName = "Shadows";

    CommandBuffer _commandBuffer = new CommandBuffer{ name = _bufferName };

    ScriptableRenderContext _context;

    CullingResults _cullingResults;
    ShadowSettings _settings;

    const int MaxShadowedDirectionalLightCount = 4;

    int _shadowedDirectinnalLightCount;

    static int _dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");

    struct ShadowedDirectionalLight
    {
        public int _visibleLightIndex;
    };

    ShadowedDirectionalLight[] _shadowedDirectionalLights = new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];

    public void Setup(ScriptableRenderContext context, CullingResults culling, ShadowSettings shadowSettings)
    {
        _context = context;
        _cullingResults = culling;
        _settings = shadowSettings;
        _shadowedDirectinnalLightCount = 0;
    }

    void ExcuteBuffer()
    {
        _context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }

    public void ReserveDirectinalShadows(Light light, int visibleLightIndex)
    {
        if(_shadowedDirectinnalLightCount < MaxShadowedDirectionalLightCount 
            && light.shadows != LightShadows.None && light.shadowStrength > 0f
            && _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            _shadowedDirectionalLights[_shadowedDirectinnalLightCount++] =  new ShadowedDirectionalLight { _visibleLightIndex = visibleLightIndex };            
        }
    }

    public void Render()
    {
        if(_shadowedDirectinnalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            _commandBuffer.GetTemporaryRT(_dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }

       
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)_settings._directional._atlasSize;
        _commandBuffer.GetTemporaryRT(_dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        _commandBuffer.SetRenderTarget(_dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _commandBuffer.ClearRenderTarget(true, false, Color.clear);
        _commandBuffer.BeginSample(_bufferName);
        ExcuteBuffer();

        int split = _shadowedDirectinnalLightCount <= 1 ?  1 : 2;
        int tileSize = atlasSize / split;
        for(int i = 0; i < _shadowedDirectinnalLightCount; ++i)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        _commandBuffer.EndSample(_bufferName);
        ExcuteBuffer();
    }

    void RenderDirectionalShadows(int index, int split, int atlasSize)
    {
        ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
        var shadowSettings =  new ShadowDrawingSettings(_cullingResults, light._visibleLightIndex);
        _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light._visibleLightIndex, 0, 
                                                1, Vector3.zero, atlasSize, 0f, 
                                                out Matrix4x4 viewMatrix, out Matrix4x4 projectMatrix, out ShadowSplitData splitData);
        shadowSettings.splitData = splitData;
        SetTileViewport(index, split, atlasSize);
        _commandBuffer.SetViewProjectionMatrices(viewMatrix, projectMatrix);
        ExcuteBuffer();
        _context.DrawShadows(ref shadowSettings);
    }

    void SetTileViewport(int index, int split, int tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        _commandBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
    }

    public void Clearup()
    {
        _commandBuffer.ReleaseTemporaryRT(_dirShadowAtlasId);
        ExcuteBuffer();
    }
};