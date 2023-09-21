using System.Runtime.InteropServices.ComTypes;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using LightType = UnityEngine.LightType;

public partial class CustomRenderPipeline
{
    partial void InitializeForEditor();

    partial void DisposeForEditor();

#if UNITY_EDITOR

    static Lightmapping.RequestLightsDelegate lightsDelegate = (Light[] lights, NativeArray<LightDataGI> output) => {
        var lightData = new LightDataGI();
        for(int i = 0; i < lights.Length; ++i)
        {
            Light light = lights[i];
            switch(light.type)
            {
                case LightType.Directional:
                {
                    var dirLight = new DirectionalLight();
                    LightmapperUtils.Extract(light, ref dirLight);
                    lightData.Init(ref dirLight);
                }
                    break;
                
                case LightType.Point:
                {
                    var pointLight = new PointLight();
                    LightmapperUtils.Extract(light, ref pointLight);
                    lightData.Init(ref pointLight);
                }
                    break;
                
                case LightType.Spot:
                {
                    var spotLight = new SpotLight();
                    LightmapperUtils.Extract(light, ref spotLight);
                    spotLight.innerConeAngle = light.innerSpotAngle * Mathf.Deg2Rad;
                    spotLight.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;
                    lightData.Init(ref spotLight);
                }
                    break;
                
                case LightType.Area:
                {
                    var areaLight = new RectangleLight();
                    LightmapperUtils.Extract(light, ref areaLight);
                    areaLight.mode = LightMode.Baked;
                    lightData.Init(ref areaLight);
                }
                    break;

                default:
                    lightData.InitNoBake(light.GetInstanceID());
                    break;
            }
            lightData.falloff = FalloffType.InverseSquared;
            output[i] = lightData;
        }
    };

    partial void InitializeForEditor()
    {
        Lightmapping.SetDelegate(lightsDelegate);
    }

    partial void DisposeForEditor()
    {
        Lightmapping.ResetDelegate();
    }


#endif
}
