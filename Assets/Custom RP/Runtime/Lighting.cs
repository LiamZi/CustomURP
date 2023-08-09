using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


public class Lighting
{
    const string _bufferName = "Lighting";

    const int MAX_VISIBLE_LIGHTS = 4;


    CommandBuffer _commandBuffer = new CommandBuffer { name = _bufferName };

    static int _dirLightCountId = Shader.PropertyToID("_directionalLightCount");
    static int _dirLightColorId = Shader.PropertyToID("_directionalLightColor");
    static int _dirLightDirectionId = Shader.PropertyToID("_directionalLightDirection");

    CullingResults _cullingResults;

    static Vector4[] _dirLightColors = new Vector4[MAX_VISIBLE_LIGHTS];
    static Vector4[] _dirLightDirections = new Vector4[MAX_VISIBLE_LIGHTS];

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

        NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;
        for(int i = 0; i < visibleLights.Length; ++i)
        {
            VisibleLight visibleLight = visibleLights[i];
            if(visibleLight.lightType == LightType.Directional)
            {
                SetupDirectionalLight(dirLightCount++, ref visibleLight);
                if(dirLightCount >= MAX_VISIBLE_LIGHTS) break;
            }

        }

        _commandBuffer.SetGlobalInt(_dirLightCountId, visibleLights.Length);
        _commandBuffer.SetGlobalVectorArray(_dirLightColorId, _dirLightColors);
        _commandBuffer.SetGlobalVectorArray(_dirLightDirectionId, _dirLightDirections);

    }

    void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        _dirLightColors[index] = visibleLight.finalColor;
        _dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        _shadows.ReserveDirectinalShadows(visibleLight.light, index);

        // Light light = RenderSettings.sun;
        // _commandBuffer.SetGlobalVector(_dirLightColorId, light.color.linear * light.intensity);
        // _commandBuffer.SetGlobalVector(_dirLightDirectionId, -light.transform.forward);
    }

    public void Clearup()
    {
        _shadows.Clearup();
    }
};