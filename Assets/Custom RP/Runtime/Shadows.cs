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
    const int MaxCascades = 4;

    int _shadowedDirectinnalLightCount;

    static int _dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    static int _dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
    static int _cascadCountId = Shader.PropertyToID("_CascadCount");
    static int _cascadCullingSphererId = Shader.PropertyToID("_CascadCullingSpheres");
    static int _shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
    static int _cascadDataId = Shader.PropertyToID("_CascadData");
    static int _shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
    
    static Matrix4x4[] _dirShadowMatrices = new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascades];
    static Vector4[] _cascadCullingSpheres = new Vector4[MaxCascades];
    static Vector4[] _cascadData = new Vector4[MaxCascades];

    struct ShadowedDirectionalLight
    {
        public int _visibleLightIndex;
        public float _slopeScaleBias;
        public float _nearPlaneOffset;
    };

    ShadowedDirectionalLight[] _shadowedDirectionalLights = new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];

    static string [] _directionalFilterKeywords = 
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };

    static string[] _cascadeBlendKeywords = 
    {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };

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

    public Vector3 ReserveDirectinalShadows(Light light, int visibleLightIndex)
    {
        if(_shadowedDirectinnalLightCount < MaxShadowedDirectionalLightCount 
            && light.shadows != LightShadows.None && light.shadowStrength > 0f
            && _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            _shadowedDirectionalLights[_shadowedDirectinnalLightCount] =  new ShadowedDirectionalLight 
            {   _visibleLightIndex = visibleLightIndex, 
                _slopeScaleBias = light.shadowBias, 
                _nearPlaneOffset = light.shadowNearPlane 
            };

            return new Vector3(light.shadowStrength, _settings._directional._cascadeCount * _shadowedDirectinnalLightCount++, light.shadowNormalBias);
        }

        return Vector3.zero;
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

        int tiles = _shadowedDirectinnalLightCount * _settings._directional._cascadeCount;
        int split = _shadowedDirectinnalLightCount <= 1 ?  1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for(int i = 0; i < _shadowedDirectinnalLightCount; ++i)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        _commandBuffer.SetGlobalInt(_cascadCountId, _settings._directional._cascadeCount);
        _commandBuffer.SetGlobalVectorArray(_cascadCullingSphererId, _cascadCullingSpheres);
        _commandBuffer.SetGlobalVectorArray(_cascadDataId, _cascadData);
        _commandBuffer.SetGlobalMatrixArray(_dirShadowMatricesId, _dirShadowMatrices);
        float f = 1f - _settings._directional._cascadeFade;
        _commandBuffer.SetGlobalVector(_shadowDistanceFadeId, new Vector4(1f / _settings._maxDistance, 1f / _settings._distanceFade, 1f / (1f - f * f)));
        
        SetKeywords(_directionalFilterKeywords, (int)_settings._directional._filter - 1);
        SetKeywords(_cascadeBlendKeywords, (int)_settings._directional._cascadblendMode - 1);
        _commandBuffer.SetGlobalVector(_shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
        _commandBuffer.EndSample(_bufferName);
        
        ExcuteBuffer();
    }

    void RenderDirectionalShadows(int index, int split, int atlasSize)
    {
        ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
        var shadowSettings =  new ShadowDrawingSettings(_cullingResults, light._visibleLightIndex);
        int cascadeCount = _settings._directional._cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = _settings._directional.GetCascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - _settings._directional._cascadeFade);

        for(int i = 0; i < cascadeCount; ++i)
        {
            _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light._visibleLightIndex, i, 
                                                cascadeCount, ratios, atlasSize, light._nearPlaneOffset, 
                                                out Matrix4x4 viewMatrix, out Matrix4x4 projectMatrix, out ShadowSplitData splitData);
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowSettings.splitData = splitData;
            if(index == 0)
            {
                // Vector4 cullingSphere = splitData.cullingSphere;
                // cullingSphere.w *= cullingSphere.w;
                // _cascadCullingSpheres[i] = cullingSphere;
                SetCascadData(i, splitData.cullingSphere, atlasSize);
            }
            // SetTileViewport(index, split, atlasSize);
            int titleIndex = tileOffset + i;
            _dirShadowMatrices[titleIndex] = ConvertToAtlasMatrix(projectMatrix * viewMatrix, SetTileViewport(titleIndex, split, atlasSize), split);
            _commandBuffer.SetViewProjectionMatrices(viewMatrix, projectMatrix);
            _commandBuffer.SetGlobalDepthBias(0f, light._slopeScaleBias);
            ExcuteBuffer();
            _context.DrawShadows(ref shadowSettings);
            _commandBuffer.SetGlobalDepthBias(0f, 0f);
        }
    }

    void SetKeywords(string[] keywords, int enablendIndex)
    {
        // int index = (int)_settings._directional._filter - 1;

        for(int i = 0; i < keywords.Length; ++i)
        {
            if(i == enablendIndex)
            {
                _commandBuffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                _commandBuffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }

    void SetCascadData(int index , Vector4 cullingSphere, float tileSize)
    {
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)_settings._directional._filter + 1f);
        // _cascadData[index].x = 1f / cullingSphere.w;
        cullingSphere.w -= filterSize;
        cullingSphere.w *= cullingSphere.w;
        _cascadCullingSpheres[index] = cullingSphere;
        _cascadData[index] = new Vector4(1f / cullingSphere.w,  filterSize * 1.4142136f);
    }

    Vector2 SetTileViewport(int index, int split, int tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        _commandBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    public void Clearup()
    {
        _commandBuffer.ReleaseTemporaryRT(_dirShadowAtlasId);
        ExcuteBuffer();
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        if(SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        float scale  = 1f / split;

        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale; 
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);

        return m;
    }
};