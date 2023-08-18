using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


public class Lighting
{
    const string _bufferName = "Lighting";

    const int MAX_VISIBLE_LIGHTS = 4;
    const int MAX_OTHER_LIGHTS = 64;


    CommandBuffer _commandBuffer = new CommandBuffer { name = _bufferName };

    static int _dirLightCountId = Shader.PropertyToID("_directionalLightCount");
    static int _dirLightColorId = Shader.PropertyToID("_directionalLightColor");
    static int _dirLightDirectionId = Shader.PropertyToID("_directionalLightDirection");
    static int _dirLightShadowDataId = Shader.PropertyToID("_directionalLightShadowData");

    static int _otherLightSizeId = Shader.PropertyToID("_otherLightSize");
    static int _otherLightColorsId = Shader.PropertyToID("_otherLightsColors");
    static int _otherLightPositionsId = Shader.PropertyToID("_otherLightPositions");

    CullingResults _cullingResults;

    static Vector4[] _dirLightColors = new Vector4[MAX_VISIBLE_LIGHTS];
    static Vector4[] _dirLightDirections = new Vector4[MAX_VISIBLE_LIGHTS];
    static Vector4[] _dirLightShadowData = new Vector4[MAX_VISIBLE_LIGHTS];
    
    static Vector4[] _otherLightColors = new Vector4[MAX_OTHER_LIGHTS];
    static Vector4[] _otherLightPositions = new Vector4[MAX_OTHER_LIGHTS];

    Shadows _shadows = new Shadows();
    

    public void Setup(ScriptableRenderContext context, CullingResults cull, ShadowSettings shadowSettings)
    {
        this._cullingResults = cull;

        _commandBuffer.BeginSample(_bufferName);
        // SetupDirectionalLight();
        _shadows.Setup(context, cull, shadowSettings);
        SetupLights();
        _shadows.Render();
        _commandBuffer.EndSample(_bufferName);
        context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }
    void SetupLights()
    {
        int dirLightCount = 0;
        int otherLightCount = 0;

        NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;
        for(int i = 0; i < visibleLights.Length; ++i)
        {
            VisibleLight visibleLight = visibleLights[i];
            // if(visibleLight.lightType == LightType.Directional)
            // {
            //     SetupDirectionalLight(dirLightCount++, ref visibleLight);
            //     if(dirLightCount >= MAX_VISIBLE_LIGHTS) break;
            // }
            switch(visibleLight.lightType)
            {
            case LightType.Directional:
                if(dirLightCount < MAX_VISIBLE_LIGHTS)
                {
                    SetupDirectionalLight(dirLightCount++, ref visibleLight);
                }
                break;

            case LightType.Point:
                if(otherLightCount < MAX_OTHER_LIGHTS)
                {
                    SetupPointLight(otherLightCount++, ref visibleLight);
                }
                break;
            }

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
        _otherLightPositions[index] = visibleLight.localToWorldMatrix.GetColumn(3);
    }

    public void Clearup()
    {
        _shadows.Clearup();
    }
};