using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private const string _bufferName = "Shadows";

    private const int MaxShadowedDirectionalLightCount = 4;
    private const int MaxCascades                      = 4;
    private const int MaxShadowedOtherLightCount       = 16;

    private static readonly int _dirShadowAtlasId       = Shader.PropertyToID("_DirectionalShadowAtlas");
    private static readonly int _dirShadowMatricesId    = Shader.PropertyToID("_DirectionalShadowMatrices");
    private static readonly int _cascadCountId          = Shader.PropertyToID("_CascadCount");
    private static readonly int _cascadCullingSphererId = Shader.PropertyToID("_CascadCullingSpheres");
    private static readonly int _shadowDistanceFadeId   = Shader.PropertyToID("_ShadowDistanceFade");
    private static readonly int _cascadDataId           = Shader.PropertyToID("_CascadData");
    private static readonly int _shadowAtlasSizeId      = Shader.PropertyToID("_ShadowAtlasSize");
    private static readonly int _otherShadwAtlasId      = Shader.PropertyToID("_OtherShadowAtlas");
    private static readonly int _otherShadowMatricesId  = Shader.PropertyToID("_OtherShadowMatrices");
    private static readonly int _shadowPancakingId      = Shader.PropertyToID("_ShadowPancaking");

    private static readonly int _otherShadowTilesId = Shader.PropertyToID("_OtherShadowTiles");

    private static readonly Matrix4x4[] _dirShadowMatrices =
        new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascades];

    private static readonly Matrix4x4[] _otherShadowMatrices = new Matrix4x4[MaxShadowedOtherLightCount];

    private static readonly Vector4[] _cascadCullingSpheres = new Vector4[MaxCascades];
    private static readonly Vector4[] _cascadData           = new Vector4[MaxCascades];

    private static readonly Vector4[] _otherShadowTiles = new Vector4[MaxShadowedOtherLightCount];

    private static readonly string[] _directionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7"
    };

    private static readonly string[] _cascadeBlendKeywords =
    {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };

    private static readonly string[] _shadowMaskKeywords =
    {
        "_SHADOW_MASK_ALWAYS",
        "_SHADOW_MASK_DISTANCE"
    };

    private static readonly string[] _otherFilterKeywords =
    {
        "_OTHER_PCF3",
        "_OTHER_PCF5",
        "_OTHER_PCF7"
    };

    private readonly CommandBuffer _commandBuffer = new CommandBuffer { name = _bufferName };

    private readonly ShadowedDirectionalLight[] _shadowedDirectionalLights =
        new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];

    private readonly ShadowedOtherLight[] _shadowedOtherLights = new ShadowedOtherLight[MaxShadowedOtherLightCount];

    private Vector4 _atlasSize;

    private ScriptableRenderContext _context;

    private CullingResults _cullingResults;
    private ShadowSettings _settings;

    private int _shadowedDirectinnalLightCount;

    private int _shadowedOtherLightCount;

    private bool _useShadowMask;

    public void Setup(ScriptableRenderContext context, CullingResults culling, ShadowSettings shadowSettings)
    {
        _context                       = context;
        _cullingResults                = culling;
        _settings                      = shadowSettings;
        _shadowedDirectinnalLightCount = 0;
        _useShadowMask                 = false;
        _shadowedOtherLightCount       = 0;
    }

    private void ExcuteBuffer()
    {
        if (_context == null || _commandBuffer == null) return;
        Debug.Log("shadows commandbuffer size : " + _commandBuffer.sizeInBytes);
        // if (_shadowedDirectinnalLightCount <= 0 || _shadowedOtherLightCount <= 0) return;
        
        _context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }

    public Vector4 ReserveDirectinalShadows(Light light, int visibleLightIndex)
    {
        if (_shadowedDirectinnalLightCount < MaxShadowedDirectionalLightCount
            && light.shadows               != LightShadows.None && light.shadowStrength > 0f)
            // && _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            float maskChannel = -1;
            var   lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType  == LightmapBakeType.Mixed &&
                lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                _useShadowMask = true;
                maskChannel    = lightBaking.occlusionMaskChannel;
            }

            if (!_cullingResults.GetShadowCasterBounds(visibleLightIndex, out var b))
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);

            _shadowedDirectionalLights[_shadowedDirectinnalLightCount] = new ShadowedDirectionalLight
            {
                _visibleLightIndex = visibleLightIndex,
                _slopeScaleBias    = light.shadowBias,
                _nearPlaneOffset   = light.shadowNearPlane
            };

            return new Vector4(light.shadowStrength,
                _settings._directional._cascadeCount * _shadowedDirectinnalLightCount++, light.shadowNormalBias,
                maskChannel);
        }

        return new Vector4(0f, 0f, 0f, -1f);
    }

    public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
    {
        if (light.shadows == LightShadows.None || light.shadowStrength <= 0f) return new Vector4(0f, 0f, 0f, -1f);

        var maskChannel = -1f;
        var lightBaking = light.bakingOutput;
        if (lightBaking.lightmapBakeType  == LightmapBakeType.Mixed &&
            lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
        {
            _useShadowMask = true;
            maskChannel    = lightBaking.occlusionMaskChannel;
        }

        var isPoint       = light.type == LightType.Point;
        var newLightCount = _shadowedOtherLightCount + (isPoint ? 6 : 1);
        if (newLightCount >= MaxShadowedOtherLightCount ||
            !_cullingResults.GetShadowCasterBounds(visibleLightIndex, out var b))
            return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);

        _shadowedOtherLights[_shadowedOtherLightCount] = new ShadowedOtherLight
        {
            _visibleLightIndex = visibleLightIndex,
            _slopeScaleBias    = light.shadowBias,
            _normalBias        = light.shadowNormalBias,
            _isPoint           = isPoint
        };

        var data = new Vector4(light.shadowStrength, _shadowedOtherLightCount, isPoint ? 1f : 0f, maskChannel);

        _shadowedOtherLightCount = newLightCount;

        return data;
    }

    public void Render()
    {
        if (_shadowedDirectinnalLightCount > 0)
            RenderDirectionalShadows();
        else
            _commandBuffer.GetTemporaryRT(_dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear,
                RenderTextureFormat.Shadowmap);

        if (_shadowedOtherLightCount > 0)
            RenderOtherShadows();
        else
            _commandBuffer.SetGlobalTexture(_otherShadwAtlasId, _dirShadowAtlasId);
        
        _commandBuffer.BeginSample(_bufferName);
        SetKeywords(_shadowMaskKeywords,
            _useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);
        _commandBuffer.SetGlobalInt(_cascadCountId,
            _shadowedDirectinnalLightCount > 0 ? _settings._directional._cascadeCount : 0);
        
        var f = 1f - _settings._directional._cascadeFade;
        _commandBuffer.SetGlobalVector(_shadowDistanceFadeId,
            new Vector4(1f / _settings._maxDistance, 1f / _settings._distanceFade, 1f / (1f - f * f)));
        _commandBuffer.SetGlobalVector(_shadowAtlasSizeId, _atlasSize);
        _commandBuffer.EndSample(_bufferName);
        ExcuteBuffer();
    }

    private void RenderDirectionalShadows()
    {
        var atlasSize = (int)_settings._directional._atlasSize;
        _atlasSize.x = atlasSize;
        _atlasSize.y = 1f / atlasSize;

        _commandBuffer.GetTemporaryRT(_dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear,
            RenderTextureFormat.Shadowmap);
        _commandBuffer.SetRenderTarget(_dirShadowAtlasId, RenderBufferLoadAction.DontCare,
            RenderBufferStoreAction.Store);
        _commandBuffer.ClearRenderTarget(true, false, Color.clear);
        _commandBuffer.SetGlobalFloat(_shadowPancakingId, 1f);
        _commandBuffer.BeginSample(_bufferName);
        ExcuteBuffer();

        var tiles = _shadowedDirectinnalLightCount * _settings._directional._cascadeCount;
        // int split = _shadowedDirectinnalLightCount <= 1 ?  1 : tiles <= 4 ? 2 : 4;
        var split    = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        var tileSize = atlasSize / split;

        for (var i = 0; i < _shadowedDirectinnalLightCount; ++i) RenderDirectionalShadows(i, split, tileSize);

        // _commandBuffer.SetGlobalInt(_cascadCountId, _settings._directional._cascadeCount);
        _commandBuffer.SetGlobalVectorArray(_cascadCullingSphererId, _cascadCullingSpheres);
        _commandBuffer.SetGlobalVectorArray(_cascadDataId,           _cascadData);
        _commandBuffer.SetGlobalMatrixArray(_dirShadowMatricesId, _dirShadowMatrices);
        // float f = 1f - _settings._directional._cascadeFade;
        // _commandBuffer.SetGlobalVector(_shadowDistanceFadeId, new Vector4(1f / _settings._maxDistance, 1f / _settings._distanceFade, 1f / (1f - f * f)));

        SetKeywords(_directionalFilterKeywords, (int)_settings._directional._filter          - 1);
        SetKeywords(_cascadeBlendKeywords,      (int)_settings._directional._cascadblendMode - 1);
        // _commandBuffer.SetGlobalVector(_shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
        _commandBuffer.EndSample(_bufferName);

        ExcuteBuffer();
    }

    private void RenderDirectionalShadows(int index, int split, int atlasSize)
    {
        var light          = _shadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(_cullingResults, light._visibleLightIndex);
        shadowSettings.useRenderingLayerMaskTest = true;
        var cascadeCount  = _settings._directional._cascadeCount;
        var tileOffset    = index * cascadeCount;
        var ratios        = _settings._directional.GetCascadeRatios;
        var cullingFactor = Mathf.Max(0f, 0.8f - _settings._directional._cascadeFade);
        var tileScale     = 1f / split;
        for (var i = 0; i < cascadeCount; ++i)
        {
            _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light._visibleLightIndex, i,
                cascadeCount, ratios, atlasSize, light._nearPlaneOffset,
                out var viewMatrix, out var projectMatrix, out var splitData);
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowSettings.splitData                  = splitData;
            if (index == 0)
                // Vector4 cullingSphere = splitData.cullingSphere;
                // cullingSphere.w *= cullingSphere.w;
                // _cascadCullingSpheres[i] = cullingSphere;
                SetCascadData(i, splitData.cullingSphere, atlasSize);
            // SetTileViewport(index, split, atlasSize);
            var titleIndex = tileOffset + i;
            // _dirShadowMatrices[titleIndex] = ConvertToAtlasMatrix(projectMatrix * viewMatrix, SetTileViewport(titleIndex, split, atlasSize), tileScale);

            _dirShadowMatrices[titleIndex] = ConvertToAtlasMatrix(projectMatrix * viewMatrix,
                SetTileViewport(titleIndex, split, atlasSize), tileScale);

            _commandBuffer.SetViewProjectionMatrices(viewMatrix, projectMatrix);
            _commandBuffer.SetGlobalDepthBias(0f, light._slopeScaleBias);
            ExcuteBuffer();
            _context.DrawShadows(ref shadowSettings);
            _commandBuffer.SetGlobalDepthBias(0f, 0f);
        }
    }


    private void RenderOtherShadows()
    {
        var atlasSize = (int)_settings._other._atlasSize;
        _atlasSize.z = atlasSize;
        _atlasSize.w = 1f / atlasSize;

        _commandBuffer.GetTemporaryRT(_otherShadwAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear,
            RenderTextureFormat.Shadowmap);
        _commandBuffer.SetRenderTarget(_otherShadwAtlasId, RenderBufferLoadAction.DontCare,
            RenderBufferStoreAction.Store);
        _commandBuffer.ClearRenderTarget(true, false, Color.clear);
        _commandBuffer.SetGlobalFloat(_shadowPancakingId, 0f);
        _commandBuffer.BeginSample(_bufferName);
        ExcuteBuffer();

        var tiles    = _shadowedOtherLightCount;
        var split    = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        var tileSize = atlasSize / split;

        for (var i = 0; i < _shadowedOtherLightCount;)
            if (_shadowedOtherLights[i]._isPoint)
            {
                RenderPointShadows(i, split, tileSize);
                i += 6;
            }
            else
            {
                RenderSpotShadows(i, split, tileSize);
                i += 1;
            }

        _commandBuffer.SetGlobalMatrixArray(_otherShadowMatricesId, _otherShadowMatrices);
        _commandBuffer.SetGlobalVectorArray(_otherShadowTilesId, _otherShadowTiles);

        SetKeywords(_otherFilterKeywords, (int)_settings._other._filter - 1);
        _commandBuffer.EndSample(_bufferName);

        ExcuteBuffer();
    }

    private void RenderSpotShadows(int index, int split, int tileSize)
    {
        var light = _shadowedOtherLights[index];
        var shadowSettings = new ShadowDrawingSettings(_cullingResults, light._visibleLightIndex)
        {
            useRenderingLayerMaskTest = true
        };
        // _cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light._visibleLightIndex, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
        _cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light._visibleLightIndex, out var viewMatrix,
            out var projectionMatrix, out var splitData);
        shadowSettings.splitData = splitData;
        var texelSize  = 2f                / (tileSize * projectionMatrix.m00);
        var filterSize = texelSize         * ((float)_settings._other._filter + 1f);
        var bias       = light._normalBias * filterSize * 1.4142136f;

        var offset    = SetTileViewport(index, split, tileSize);
        var tileScale = 1f / split;
        SetOtherTileData(index, offset, tileScale, bias);


        _otherShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);
        _commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        _commandBuffer.SetGlobalDepthBias(0f, light._slopeScaleBias);
        ExcuteBuffer();
        _context.DrawShadows(ref shadowSettings);
        _commandBuffer.SetGlobalDepthBias(0f, 0f);
    }


    private void RenderPointShadows(int index, int split, int tileSize)
    {
        var light = _shadowedOtherLights[index];
        var shadowSettings = new ShadowDrawingSettings(_cullingResults, light._visibleLightIndex)
        {
            useRenderingLayerMaskTest = true
        };

        var texelSize  = 2f                / tileSize;
        var filterSize = texelSize         * ((float)_settings._other._filter + 1f);
        var bias       = light._normalBias * filterSize * 1.4142136f;
        var tileScale  = 1f                / split;

        var fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;
        // float fovBias = 0f;

        for (var i = 0; i < 6; i++)
        {
            // _cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light._visibleLightIndex, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
            _cullingResults.ComputePointShadowMatricesAndCullingPrimitives(light._visibleLightIndex, (CubemapFace)i,
                fovBias, out var viewMatrix,
                out var projectionMatrix, out var splitData);
            viewMatrix.m11 = -viewMatrix.m11;
            viewMatrix.m12 = -viewMatrix.m12;
            viewMatrix.m13 = -viewMatrix.m13;

            shadowSettings.splitData = splitData;
            var tileIndex = index + i;

            var offset = SetTileViewport(tileIndex, split, tileSize);

            SetOtherTileData(tileIndex, offset, tileScale, bias);


            _otherShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);
            _commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            _commandBuffer.SetGlobalDepthBias(0f, light._slopeScaleBias);
            ExcuteBuffer();
            _context.DrawShadows(ref shadowSettings);
            _commandBuffer.SetGlobalDepthBias(0f, 0f);
        }
    }

    private void SetKeywords(string[] keywords, int enablendIndex)
    {
        // int index = (int)_settings._directional._filter - 1;

        for (var i = 0; i < keywords.Length; ++i)
            if (i == enablendIndex)
                _commandBuffer.EnableShaderKeyword(keywords[i]);
            else
                _commandBuffer.DisableShaderKeyword(keywords[i]);
    }

    private void SetCascadData(int index, Vector4 cullingSphere, float tileSize)
    {
        var texelSize  = 2f * cullingSphere.w / tileSize;
        var filterSize = texelSize            * ((float)_settings._directional._filter + 1f);
        // _cascadData[index].x = 1f / cullingSphere.w;
        cullingSphere.w              -= filterSize;
        cullingSphere.w              *= cullingSphere.w;
        _cascadCullingSpheres[index] =  cullingSphere;
        _cascadData[index]           =  new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
    }

    private Vector2 SetTileViewport(int index, int split, int tileSize)
    {
        var offset = new Vector2(index               % split, index       / split);
        _commandBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    public void Clearup()
    {
        _commandBuffer.ReleaseTemporaryRT(_dirShadowAtlasId);
        if (_shadowedOtherLightCount > 0)
        {
            _commandBuffer.ReleaseTemporaryRT(_otherShadwAtlasId);
        }
        ExcuteBuffer();
    }

    private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
    {
        if (SystemInfo.usesReversedZBuffer)
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
        m.m20 = 0.5f                                        * (m.m20 + m.m30);
        m.m21 = 0.5f                                        * (m.m21 + m.m31);
        m.m22 = 0.5f                                        * (m.m22 + m.m32);
        m.m23 = 0.5f                                        * (m.m23 + m.m33);

        return m;
    }

    private void SetOtherTileData(int index, Vector2 offset, float scale, float bias)
    {
        var board = _atlasSize.w * 0.5f;
        var data  = Vector4.zero;
        data.x                   = offset.x * scale + board;
        data.y                   = offset.y * scale + board;
        data.z                   = scale            - board - board;
        data.w                   = bias;
        _otherShadowTiles[index] = data;
    }

    private struct ShadowedDirectionalLight
    {
        public int   _visibleLightIndex;
        public float _slopeScaleBias;
        public float _nearPlaneOffset;
    }

    private struct ShadowedOtherLight
    {
        public int   _visibleLightIndex;
        public float _slopeScaleBias;
        public float _normalBias;
        public bool  _isPoint;
    }
}