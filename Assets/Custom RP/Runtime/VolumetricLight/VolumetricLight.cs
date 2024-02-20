using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomURP
{
    [RequireComponent(typeof(Light))]
    public class VolumetricLight : MonoBehaviour
    {
        Light _light;
        Material _material;
        Command _cmd;
        Command _shadowCmd;
        Vector4[] _frustum = new Vector4[4];
        bool _reversedZ = false;
        RenderTexture _cameraDepthRT;

        [Range(1, 64)]
        public int _samplerCount = 8;
        [Range(0.0f, 1.0f)]
        public float _scatteringCoef = 0.5f;
        [Range(0.0f, 0.1f)]
        public float _exinctionCoef = 0.01f;
        [Range(0.0f, 1.0f)]
        public float _skyBoxExtinctionCoef = 0.9f;
        [Range(0.0f, 0.999f)]
        public float _mieG = 0.1f;
        public bool _fog = false;
        [Range(0.0f, 0.0f)]
        public float _heightScale = 0.1f;
        public float _groundLevel = 0;
        public bool _noise = false;
        public float _noiseScale = 0.015f;
        public float _noiseIntensity = 1.0f;
        public float _noiseIntensityOffset = 0.3f;
        public Vector2 _noiseVelocity = new Vector2(3.0f, 3.0f);
        public float _maxRayLength = 400.0f;

        public Light Light
        {
            get
            {
                return _light;
            }
        }

        public Material Material
        {
            get
            {
                return _material;
            }
        }

        void Start()
        {
#if UNITY_2020_3_OR_NEWER
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                _reversedZ = true;
            }
#endif
        }

        public void Init(CustomRenderPipelineCamera camera)
        {
            _cmd = new Command("Volumetric Light");

            _cameraDepthRT = RenderTexture.GetTemporary(camera._renderTarget._size.x, camera._renderTarget._size.y, 0, RenderTextureFormat.Depth);
            _cameraDepthRT.filterMode = FilterMode.Point;
        }

        void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            if (_light == null || _light.gameObject == null) return;

            if (!_light.gameObject.activeInHierarchy || _light.enabled == false) return;

            _material.SetVector(ShaderParams._cameraForward, camera._camera.transform.forward);
            _material.SetFloat(ShaderParams._zTest, (int)UnityEngine.Rendering.CompareFunction.Always);
            _material.SetInt(ShaderParams._sampleCount, _samplerCount);
            _material.SetVector(ShaderParams._noiseVelocity, new Vector4(_noiseVelocity.x, _noiseVelocity.y) * _noiseScale);
            _material.SetVector(ShaderParams._noiseData, new Vector4(_noiseScale, _noiseIntensity, _noiseIntensityOffset));
            _material.SetVector(ShaderParams._mieG, new Vector4(1 - (_mieG * _mieG), 1 + (_mieG * _mieG), 2 * _mieG, 1.0f / (4.0f * Mathf.PI)));
            _material.SetVector(ShaderParams._volumetircLight, new Vector4(_scatteringCoef, _exinctionCoef, _light.range, 1.0f - _skyBoxExtinctionCoef));
            camera._renderTarget.GetDepthTexture(ref cmd, ref _cameraDepthRT);
            _material.SetTexture(ShaderParams._CameraDepthTextureId, _cameraDepthRT);
            if (_fog)
            {
                _material.EnableKeyword("_HEIGHT_FOG");
                _material.SetVector(ShaderParams._heightFog, new Vector4(_groundLevel, _heightScale));
            }
            else
            {
                _material.DisableKeyword("_HEIGHT_FOG");
            }
            
            switch (_light.type)
            {
                case LightType.Directional:
                    SetDirectionalLight(camera._camera);
                    break;
                case LightType.Point:
                    break;
                case LightType.Spot:
                    break;
            }

        }

        void SetDirectionalLight(Camera camera)
        {
            _cmd.Clear();
            int pass = 0;
            _material.SetPass(pass);

            if (_noise)
            {
                _material.EnableKeyword("_NOISE");
            }
            else
            {
                _material.DisableKeyword("_NOISE");
            }
            
            // _material.SetVector(ShaderParams._lightColor);
            _material.SetFloat(ShaderParams._maxRayLength, _maxRayLength);

            if (_light.cookie == null)
            {
                _material.EnableKeyword("DIRECTIONAL");
                _material.DisableKeyword("DIRECTIONAL_COOKIE");
            }
            else
            {
                _material.EnableKeyword("DIRECTIONAL_COOKIE");
                _material.DisableKeyword("DIRECTIONAL");

                _material.SetTexture(ShaderParams._lightTexture0, _light.cookie);
            }
            
            _frustum[0] = camera.ViewportToWorldPoint(new Vector3(0, 0, camera.farClipPlane));
            _frustum[2] = camera.ViewportToWorldPoint(new Vector3(0, 1, camera.farClipPlane));
            _frustum[3] = camera.ViewportToWorldPoint(new Vector3(1, 1, camera.farClipPlane));
            _frustum[1] = camera.ViewportToWorldPoint(new Vector3(1, 0, camera.farClipPlane));
            
            _material.SetVectorArray(ShaderParams._frustumCorners, _frustum);

            if (_light.shadows != LightShadows.None)
            {
                //TODO: draw volumetric light to dest rt
                // _cmd.Cmd.Blit(null, );
            }
        }
    };
};
