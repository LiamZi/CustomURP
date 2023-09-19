using UnityEditor.UIElements;
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
    const int MaxShadowedOtherLightCount = 16;

    int _shadowedDirectinnalLightCount;
    int _shadowedOtherLightCount;

    bool _useShadowMask;

    Vector4 _atlasSize;

    static int _dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    static int _dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
    static int _cascadCountId = Shader.PropertyToID("_CascadCount");
    static int _cascadCullingSphererId = Shader.PropertyToID("_CascadCullingSpheres");
    static int _shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
    static int _cascadDataId = Shader.PropertyToID("_CascadData");
    static int _shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
    static int _otherShadwAtlasId = Shader.PropertyToID("_OtherShadowAtlas");
    static int _otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices");
    static int _shadowPancakingId = Shader.PropertyToID("_ShadowPancaking");

    static int _otherShadowTilesId = Shader.PropertyToID("_OtherShadowTiles");
    
    static Matrix4x4[] _dirShadowMatrices = new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascades];
    static Matrix4x4[] _otherShadowMatrices = new Matrix4x4[MaxShadowedOtherLightCount];

    static Vector4[] _cascadCullingSpheres = new Vector4[MaxCascades];
    static Vector4[] _cascadData = new Vector4[MaxCascades];

    static Vector4[] _otherShadowTiles = new Vector4[MaxShadowedOtherLightCount];

    struct ShadowedDirectionalLight
    {
        public int _visibleLightIndex;
        public float _slopeScaleBias;
        public float _nearPlaneOffset;
    };

    ShadowedDirectionalLight[] _shadowedDirectionalLights = new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];
    struct ShadowedOtherLight
    {
        public int _visibleLightIndex;
        public float _slopeScaleBias;
        public float _normalBias;
        public bool _isPoint;
    };

    ShadowedOtherLight[] _shadowedOtherLights = new ShadowedOtherLight[MaxShadowedOtherLightCount];

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

    static string[] _shadowMaskKeywords = 
    {
        "_SHADOW_MASK_ALWAYS",
        "_SHADOW_MASK_DISTANCE"
    };

    static string[] _otherFilterKeywords =
    {
        "_OTHER_PCF3",
        "_OTHER_PCF5",
        "_OTHER_PCF7",
    };

    public void Setup(ScriptableRenderContext context, CullingResults culling, ShadowSettings shadowSettings)
    {
        _context = context;
        _cullingResults = culling;
        _settings = shadowSettings;
        _shadowedDirectinnalLightCount = 0;
        _useShadowMask = false;
        _shadowedOtherLightCount = 0;
    }

    void ExcuteBuffer()
    {
        _context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }

    public Vector4 ReserveDirectinalShadows(Light light, int visibleLightIndex)
    {
        if(_shadowedDirectinnalLightCount < MaxShadowedDirectionalLightCount 
            && light.shadows != LightShadows.None && light.shadowStrength > 0f)
            // && _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            float maskChannel = -1;
            LightBakingOutput lightBaking = light.bakingOutput;
            if(lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                _useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }

            if(!_cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            {
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
            }

            _shadowedDirectionalLights[_shadowedDirectinnalLightCount] =  new ShadowedDirectionalLight 
            {   _visibleLightIndex = visibleLightIndex, 
                _slopeScaleBias = light.shadowBias, 
                _nearPlaneOffset = light.shadowNearPlane 
            };

            return new Vector4(light.shadowStrength, _settings._directional._cascadeCount * _shadowedDirectinnalLightCount++, light.shadowNormalBias, maskChannel);
        }

        return new Vector4(0f, 0f, 0f, -1f);
    }

    public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
    {
        // if(light.shadows != LightShadows.None && light.shadowStrength > 0f)
        // {
        if(light.shadows == LightShadows.None || light.shadowStrength <= 0f)
        {
            return new Vector4(0f, 0f, 0f, -1f);
        }

        float maskChannel = -1f;
        LightBakingOutput lightBaking = light.bakingOutput;
        if(lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
        {
            _useShadowMask = true;
            maskChannel = lightBaking.occlusionMaskChannel;
            
        }
        
        bool isPoint = light.type == LightType.Point;
        int newLightCount = _shadowedOtherLightCount + (isPoint ? 6 : 1);
        if(newLightCount >= MaxShadowedOtherLightCount || !_cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
        }

        _shadowedOtherLights[_shadowedOtherLightCount] = new ShadowedOtherLight 
        {
            _visibleLightIndex = visibleLightIndex,
            _slopeScaleBias = light.shadowBias,
            _normalBias = light.shadowNormalBias,
            _isPoint = isPoint
        };

        Vector4 data = new Vector4(light.shadowStrength, _shadowedOtherLightCount, isPoint ? 1f : 0f, maskChannel);

        _shadowedOtherLightCount = newLightCount;

        return data;
        // return new Vector4(light.shadowStrength, _shadowedOtherLightCount++, 0f, lightBaking.occlusionMaskChannel);
        // }

        // return new Vector4(0f, 0f, 0f, -1f);
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

        if(_shadowedOtherLightCount > 0)
        {
            RenderOtherShadows();
        }
        else
        {
            _commandBuffer.SetGlobalTexture(_otherShadwAtlasId, _dirShadowAtlasId);
        }

        _commandBuffer.BeginSample(_bufferName);
        SetKeywords(_shadowMaskKeywords, _useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);
        _commandBuffer.SetGlobalInt(_cascadCountId, _shadowedDirectinnalLightCount > 0 ? _settings._directional._cascadeCount : 0);
        
        float f = 1f - _settings._directional._cascadeFade;
        _commandBuffer.SetGlobalVector(_shadowDistanceFadeId, new Vector4(1f / _settings._maxDistance, 1f / _settings._distanceFade, 1f / (1f - f * f)));
        _commandBuffer.SetGlobalVector(_shadowAtlasSizeId, _atlasSize);
        _commandBuffer.EndSample(_bufferName);
        ExcuteBuffer();
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)_settings._directional._atlasSize;
        _atlasSize.x = atlasSize;
        _atlasSize.y = 1f / atlasSize;

        _commandBuffer.GetTemporaryRT(_dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        _commandBuffer.SetRenderTarget(_dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _commandBuffer.ClearRenderTarget(true, false, Color.clear);
        _commandBuffer.SetGlobalFloat(_shadowPancakingId, 1f);
        _commandBuffer.BeginSample(_bufferName);
        ExcuteBuffer();

        int tiles = _shadowedDirectinnalLightCount * _settings._directional._cascadeCount;
        // int split = _shadowedDirectinnalLightCount <= 1 ?  1 : tiles <= 4 ? 2 : 4;
        int split = tiles <= 1 ?  1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for(int i = 0; i < _shadowedDirectinnalLightCount; ++i)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        // _commandBuffer.SetGlobalInt(_cascadCountId, _settings._directional._cascadeCount);
        _commandBuffer.SetGlobalVectorArray(_cascadCullingSphererId, _cascadCullingSpheres);
        _commandBuffer.SetGlobalVectorArray(_cascadDataId, _cascadData);
        _commandBuffer.SetGlobalMatrixArray(_dirShadowMatricesId, _dirShadowMatrices);
        // float f = 1f - _settings._directional._cascadeFade;
        // _commandBuffer.SetGlobalVector(_shadowDistanceFadeId, new Vector4(1f / _settings._maxDistance, 1f / _settings._distanceFade, 1f / (1f - f * f)));
        
        SetKeywords(_directionalFilterKeywords, (int)_settings._directional._filter - 1);
        SetKeywords(_cascadeBlendKeywords, (int)_settings._directional._cascadblendMode - 1);
        // _commandBuffer.SetGlobalVector(_shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
        _commandBuffer.EndSample(_bufferName);
        
        ExcuteBuffer();
    }

    void RenderDirectionalShadows(int index, int split, int atlasSize)
    {
        ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
        var shadowSettings =  new ShadowDrawingSettings(_cullingResults, light._visibleLightIndex);
        shadowSettings.useRenderingLayerMaskTest = true;
        int cascadeCount = _settings._directional._cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = _settings._directional.GetCascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - _settings._directional._cascadeFade);
        float tileScale = 1f / split;
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
            // _dirShadowMatrices[titleIndex] = ConvertToAtlasMatrix(projectMatrix * viewMatrix, SetTileViewport(titleIndex, split, atlasSize), tileScale);
            _dirShadowMatrices[titleIndex] = ConvertToAtlasMatrix(projectMatrix * viewMatrix, SetTileViewport(titleIndex, split, atlasSize), tileScale);
            
            _commandBuffer.SetViewProjectionMatrices(viewMatrix, projectMatrix);
            _commandBuffer.SetGlobalDepthBias(0f, light._slopeScaleBias);
            ExcuteBuffer();
            _context.DrawShadows(ref shadowSettings);
            _commandBuffer.SetGlobalDepthBias(0f, 0f);
        }
    }


    void RenderOtherShadows()
    {
        int atlasSize = (int)_settings._other._atlasSize;
        _atlasSize.z = atlasSize;
        _atlasSize.w = 1f / atlasSize;

        _commandBuffer.GetTemporaryRT(_otherShadwAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        _commandBuffer.SetRenderTarget(_otherShadwAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _commandBuffer.ClearRenderTarget(true, false, Color.clear);
        _commandBuffer.SetGlobalFloat(_shadowPancakingId, 0f);
        _commandBuffer.BeginSample(_bufferName);
        ExcuteBuffer();

        int tiles = _shadowedOtherLightCount;
        int split = tiles <= 1 ?  1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for(int i = 0; i < _shadowedOtherLightCount;)
        {
            if(_shadowedOtherLights[i]._isPoint)
            {
                RenderPointShadows(i, split, tileSize);
                i += 6;
            }
            else
            {
                RenderSpotShadows(i, split, tileSize);
                i += 1;
            }
           
        }

        _commandBuffer.SetGlobalMatrixArray(_otherShadowMatricesId, _otherShadowMatrices);
        _commandBuffer.SetGlobalVectorArray(_otherShadowTilesId, _otherShadowTiles);
    
        SetKeywords(_otherFilterKeywords, (int)_settings._other._filter - 1);
        _commandBuffer.EndSample(_bufferName);
        
        ExcuteBuffer();
    }

    void RenderSpotShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = _shadowedOtherLights[index];
        var shadowSettings = new ShadowDrawingSettings(_cullingResults, light._visibleLightIndex)
        {
            useRenderingLayerMaskTest = true
        };
        // _cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light._visibleLightIndex, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
        _cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light._visibleLightIndex, out Matrix4x4 viewMatrix, 
                                                                out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
        shadowSettings.splitData = splitData;
        float texelSize = 2f / (tileSize * projectionMatrix.m00);
        float filterSize = texelSize * ((float)_settings._other._filter + 1f);
        float bias = light._normalBias * filterSize * 1.4142136f;
        
        Vector2 offset = SetTileViewport(index, split, tileSize);
        float tileScale = 1f / split;
        SetOtherTileData(index, offset, tileScale, bias);


        _otherShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);
        _commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        _commandBuffer.SetGlobalDepthBias(0f, light._slopeScaleBias);
        ExcuteBuffer();
        _context.DrawShadows(ref shadowSettings);
        _commandBuffer.SetGlobalDepthBias(0f, 0f);
    }


    void RenderPointShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = _shadowedOtherLights[index];
        var shadowSettings = new ShadowDrawingSettings(_cullingResults, light._visibleLightIndex) 
        {
            useRenderingLayerMaskTest = true
        };

        float texelSize = 2f / tileSize;
        float filterSize = texelSize * ((float)_settings._other._filter + 1f);
        float bias = light._normalBias * filterSize * 1.4142136f;
        float tileScale = 1f / split;

        float fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;
        // float fovBias = 0f;

        for(int i = 0; i < 6; i++)
        {
            // _cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light._visibleLightIndex, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
            _cullingResults.ComputePointShadowMatricesAndCullingPrimitives(light._visibleLightIndex, (CubemapFace)i, 
                                                                    fovBias, out Matrix4x4 viewMatrix, 
                                                                    out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
            viewMatrix.m11 = -viewMatrix.m11;
            viewMatrix.m12 = -viewMatrix.m12;
            viewMatrix.m13 = -viewMatrix.m13;

            shadowSettings.splitData = splitData;
            int tileIndex = index + i;
    
            Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
            
            SetOtherTileData(tileIndex, offset, tileScale, bias);


            _otherShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);
            _commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
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

        if(_shadowedOtherLightCount > 0)
        {
            _commandBuffer.ReleaseTemporaryRT(_otherShadwAtlasId);
        }

        ExcuteBuffer();
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
    {
        if(SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        // float scale  = 1f / split;

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

    void SetOtherTileData(int index, Vector2 offset, float scale,  float bias)
    {
        float board = _atlasSize.w * 0.5f;
        Vector4 data = Vector4.zero;
        data.x = offset.x * scale + board;
        data.y = offset.y * scale + board;
        data.z = scale - board - board;
        data.w = bias;
        _otherShadowTiles[index] = data;
    }
};