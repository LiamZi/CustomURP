using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


public class Lighting
{
    const string _bufferName = "Lighting";

    const int MAX_VISIBLE_LIGHTS = 4;
    const int MAX_OTHER_LIGHTS = 64;

    static string LIGHTS_PER_OBJECT_KEYWORD = "_LIGHTS_PER_OBJECT";

    CommandBuffer _commandBuffer = new CommandBuffer { name = _bufferName };

    static int _dirLightCountId = Shader.PropertyToID("_directionalLightCount");
    static int _dirLightColorId = Shader.PropertyToID("_directionalLightColor");
    static int _dirLightDirectionId = Shader.PropertyToID("_directionalLightDirection");
    static int _dirLightShadowDataId = Shader.PropertyToID("_directionalLightShadowData");

    static int _otherLightSizeId = Shader.PropertyToID("_otherLightSize");
    static int _otherLightColorsId = Shader.PropertyToID("_otherLightColors");
    static int _otherLightPositionsId = Shader.PropertyToID("_otherLightPositions");
    static int _otherLightDirectionsId = Shader.PropertyToID("_otherLightDirections");
    static int _otherLightSpotAnglesId = Shader.PropertyToID("_otherLightAngles");
    static int _otherLightShadowDataId = Shader.PropertyToID("_otherLightShadowData");

    CullingResults _cullingResults;

    static Vector4[] _dirLightColors = new Vector4[MAX_VISIBLE_LIGHTS];
    static Vector4[] _dirLightDirections = new Vector4[MAX_VISIBLE_LIGHTS];
    static Vector4[] _dirLightShadowData = new Vector4[MAX_VISIBLE_LIGHTS];
    
    static Vector4[] _otherLightColors = new Vector4[MAX_OTHER_LIGHTS];
    static Vector4[] _otherLightPositions = new Vector4[MAX_OTHER_LIGHTS];
    static Vector4[] _otherLightDirections = new Vector4[MAX_OTHER_LIGHTS];
    static Vector4[] _otherLightAngles = new Vector4[MAX_OTHER_LIGHTS];
    static Vector4[] _otherLightShadowData = new Vector4[MAX_OTHER_LIGHTS];

    Shadows _shadows = new Shadows();
    

    public void Setup(ScriptableRenderContext context, CullingResults cull, ShadowSettings shadowSettings, bool useLightsPerObject)
    {
        this._cullingResults = cull;

        _commandBuffer.BeginSample(_bufferName);
        // SetupDirectionalLight();
        _shadows.Setup(context, cull, shadowSettings);
        SetupLights(useLightsPerObject);
        _shadows.Render();
        _commandBuffer.EndSample(_bufferName);
        context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }
    void SetupLights(bool useLightsPerObject)
    {
        int dirLightCount = 0;
        int otherLightCount = 0;
        NativeArray<int> indexMap = useLightsPerObject ? _cullingResults.GetLightIndexMap(Allocator.Temp) : default;

        NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;
        int i = 0;
        for(i = 0; i < visibleLights.Length; ++i)
        {
            int newIndex = -1;
            VisibleLight visibleLight = visibleLights[i];
            // if(visibleLight.lightType == LightType.Directional)
            // {
            //     SetupDirectionalLight(dirLightCount++, ref visibleLight);
            //     if(dirLightCount >= MAX_VISIBLE_LIGHTS) break;
            // }
            switch(visibleLight.lightType)
            {
            case LightType.Directional:
            {
                if(dirLightCount < MAX_VISIBLE_LIGHTS)
                {
                    SetupDirectionalLight(dirLightCount++, ref visibleLight);
                }
            }
                break;

            case LightType.Point:
            {
                if(otherLightCount < MAX_OTHER_LIGHTS)
                {
                    newIndex = otherLightCount;
                    SetupPointLight(otherLightCount++, ref visibleLight);
                }
            }
                break;
            case LightType.Spot:
                {
                    if(otherLightCount < MAX_OTHER_LIGHTS)
                    {
                        newIndex = otherLightCount;
                        SetupSpotLight(otherLightCount++, ref visibleLight);
                    }
                }
                break;
            }

            if(useLightsPerObject)
            {
                indexMap[i] = newIndex;
            }

        }

        if(useLightsPerObject)
        {
            for(; i < indexMap.Length; ++i)
            {
                indexMap[i] = -1;
            }
            _cullingResults.SetLightIndexMap(indexMap);
            indexMap.Dispose();
            Shader.EnableKeyword(LIGHTS_PER_OBJECT_KEYWORD);

        }
        else
        {
            Shader.DisableKeyword(LIGHTS_PER_OBJECT_KEYWORD);
        }

        _commandBuffer.SetGlobalInt(_dirLightCountId, visibleLights.Length);

        if(dirLightCount > 0)
        {
            _commandBuffer.SetGlobalVectorArray(_dirLightColorId, _dirLightColors);
            _commandBuffer.SetGlobalVectorArray(_dirLightDirectionId, _dirLightDirections);
            _commandBuffer.SetGlobalVectorArray(_dirLightShadowDataId, _dirLightShadowData);
        }

        _commandBuffer.SetGlobalInt(_otherLightSizeId, otherLightCount);
        if(otherLightCount > 0)
        {
            _commandBuffer.SetGlobalVectorArray(_otherLightColorsId, _otherLightColors);
            _commandBuffer.SetGlobalVectorArray(_otherLightPositionsId, _otherLightPositions);
            _commandBuffer.SetGlobalVectorArray(_otherLightDirectionsId, _otherLightDirections);
            _commandBuffer.SetGlobalVectorArray(_otherLightSpotAnglesId, _otherLightAngles);
            _commandBuffer.SetGlobalVectorArray(_otherLightShadowDataId, _otherLightShadowData);
        }
    }

    void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        _dirLightColors[index] = visibleLight.finalColor;
        _dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        _dirLightShadowData[index] = _shadows.ReserveDirectinalShadows(visibleLight.light, index);

        // Light light = RenderSettings.sun;
        // _commandBuffer.SetGlobalVector(_dirLightColorId, light.color.linear * light.intensity);
        // _commandBuffer.SetGlobalVector(_dirLightDirectionId, -light.transform.forward);
    }

    void SetupPointLight(int index, ref VisibleLight visibleLight)
    {
        _otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.000001f);
        _otherLightPositions[index] = position;
        _otherLightAngles[index] = new Vector4(0f, 1f);
        _otherLightShadowData[index] = _shadows.ReserveOtherShadows(visibleLight.light, index);
    }

    void SetupSpotLight(int index, ref VisibleLight visibleLight)
    {
        _otherLightColors[index] = visibleLight.finalColor;
        // Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        // position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.000001f);
        // _otherLightPositions[index] = position;
        // _otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
		position.w =
			1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
		_otherLightPositions[index] = position;
		_otherLightDirections[index] =
			-visibleLight.localToWorldMatrix.GetColumn(2);


        Light light  = visibleLight.light;
        // float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        // float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);

        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
		float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        // float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        // _otherLightAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
        
		_otherLightAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
        _otherLightShadowData[index] = _shadows.ReserveOtherShadows(visibleLight.light, index);
    }

    public void Clearup()
    {
        _shadows.Clearup();
    }
};