using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Pass/LightPass")]
    public unsafe sealed class LightPass : CoreAction
    {
        private const int MAX_VISIBLE_LIGHTS = 4;
        private const int MAX_OTHER_LIGHTS = 64;
        private CullingResults _cullingResults;
        private Shadows _shadows = null;
        private static string LIGHTS_PER_OBJECT_KEYWORD = "_LIGHTS_PER_OBJECT";


        private static Vector4[] _dirLightColors = new Vector4[MAX_VISIBLE_LIGHTS];
        private static Vector4[] _dirLightDirectionsAndMasks = new Vector4[MAX_VISIBLE_LIGHTS];
        private static Vector4[] _dirLightShadowData = new Vector4[MAX_VISIBLE_LIGHTS];
        private static Vector4[] _otherLightColors = new Vector4[MAX_OTHER_LIGHTS];
        private static Vector4[] _otherLightPositions = new Vector4[MAX_OTHER_LIGHTS];
        private static Vector4[] _otherLightDirectionsAndMasks = new Vector4[MAX_OTHER_LIGHTS];
        private static Vector4[] _otherLightAngles = new Vector4[MAX_OTHER_LIGHTS];
        private static Vector4[] _otherLightShadowData = new Vector4[MAX_OTHER_LIGHTS];

        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            base.InspectDependActions();
            _isInitialized = true;
            _asset = asset;
            _shadows = new Shadows();
        }

        protected internal override void Dispose()
        {
            base.Dispose();
        }

        public override void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.Tick(camera, ref cmd);
            // _shadows.Render();
            _cmd.EndSampler();
            _cmd.Name = "Light Pass End";
            _cmd.Execute();
        }

        public override void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.BeginRendering(camera, ref cmd);
            if (!Cull(_asset.Shadows._maxDistance, ref cmd)) return;
            _cmd.Name = "Light Pass Begin";
            _cmd.BeginSample();
            _shadows.Setup(_cmd.Context, _cullingResults, _asset.Shadows);
            CameraSettings cameraSettings = camera ? camera.Setting : new CameraSettings();
            SetupLights(_asset.LightsPerObject, cameraSettings._maskLights ? cameraSettings._renderingLayerMask : -1);
        }

        public override void EndRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            base.EndRendering(camera, ref cmd);
        }

        public override bool InspectProperty()
        {
            return true;
        }

        private bool Cull(float maxShadowDistance, ref Command cmd)
        {
            if (_camera._camera.TryGetCullingParameters(out ScriptableCullingParameters p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, _camera._camera.farClipPlane);
                _cullingResults = cmd.Context.Cull(ref p);
                return true;
            }

            return false;
        }

        void SetupLights(bool useLightsPerObject, int renderingLayerMask)
        {
            int dirLightCount = 0;
            int otherLightCount = 0;
            NativeArray<int> indexMap = useLightsPerObject ? _cullingResults.GetLightIndexMap(Allocator.Temp) : default;

            NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;
            int i = 0;
            for (i = 0; i < visibleLights.Length; ++i)
            {
                int newIndex = -1;
                VisibleLight visibleLight = visibleLights[i];
                Light light = visibleLight.light;

                if ((light.renderingLayerMask & renderingLayerMask) != 0)
                {
                    switch (visibleLight.lightType)
                    {
                        case LightType.Directional:
                        {
                            if (dirLightCount < MAX_VISIBLE_LIGHTS)
                            {
                                SetupDirectionalLight(dirLightCount++, i, ref visibleLight, light);
                            }
                        }
                            break;

                        case LightType.Point:
                        {
                            if (otherLightCount < MAX_OTHER_LIGHTS)
                            {
                                newIndex = otherLightCount;
                                SetupPointLight(otherLightCount++, i, ref visibleLight, light);
                            }
                        }
                            break;
                        case LightType.Spot:
                        {
                            if (otherLightCount < MAX_OTHER_LIGHTS)
                            {
                                newIndex = otherLightCount;
                                SetupSpotLight(otherLightCount++, i, ref visibleLight, light);
                            }
                        }
                            break;
                    }
                }

                if (useLightsPerObject)
                {
                    indexMap[i] = newIndex;
                }
            }

            if (useLightsPerObject)
            {
                for (; i < indexMap.Length; ++i)
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

            _cmd.SetGlobalInt(ShaderParams._DirLightCountId, dirLightCount);

            if (dirLightCount > 0)
            {
                _cmd.SetGlobalVectorArray(ShaderParams._DirLightColorId, _dirLightColors);
                _cmd.SetGlobalVectorArray(ShaderParams._DirLightDirectionId, _dirLightDirectionsAndMasks);
                _cmd.SetGlobalVectorArray(ShaderParams._DirLightShadowDataId, _dirLightShadowData);
            }

            _cmd.SetGlobalInt(ShaderParams._OtherLightSizeId, otherLightCount);

            if (otherLightCount > 0)
            {
                _cmd.SetGlobalVectorArray(ShaderParams._OtherLightColorsId, _otherLightColors);
                _cmd.SetGlobalVectorArray(ShaderParams._OtherLightPositionsId, _otherLightPositions);
                _cmd.SetGlobalVectorArray(ShaderParams._OtherLightDirectionsId, _otherLightDirectionsAndMasks);
                _cmd.SetGlobalVectorArray(ShaderParams._OtherLightSpotAnglesId, _otherLightAngles);
                _cmd.SetGlobalVectorArray(ShaderParams._OtherLightShadowDataId, _otherLightShadowData);
            }
        }

        void SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
        {
            _dirLightColors[index] = visibleLight.finalColor;
            Vector4 dirAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
            dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();

            _dirLightDirectionsAndMasks[index] = dirAndMask;
            _dirLightShadowData[index] = _shadows.ReserveDirectinalShadows(light, visibleIndex);

            // Light light = RenderSettings.sun;
            // _commandBuffer.SetGlobalVector(_dirLightColorId, light.color.linear * light.intensity);
            // _commandBuffer.SetGlobalVector(_dirLightDirectionId, -light.transform.forward);
        }

        void SetupPointLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
        {
            _otherLightColors[index] = visibleLight.finalColor;
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.000001f);
            _otherLightPositions[index] = position;
            _otherLightAngles[index] = new Vector4(0f, 1f);
            Vector4 dirAndMask = Vector4.zero;
            dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
            _otherLightDirectionsAndMasks[index] = dirAndMask;
            _otherLightShadowData[index] = _shadows.ReserveOtherShadows(light, visibleIndex);
        }

        void SetupSpotLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
        {
            _otherLightColors[index] = visibleLight.finalColor;
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            _otherLightPositions[index] = position;
            Vector4 dirAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
            dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
            _otherLightDirectionsAndMasks[index] = dirAndMask;

            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
            float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);

            _otherLightAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
            _otherLightShadowData[index] = _shadows.ReserveOtherShadows(light, visibleIndex);

        }
    };
}