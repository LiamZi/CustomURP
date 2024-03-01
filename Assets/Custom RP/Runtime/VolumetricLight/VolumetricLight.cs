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
        CustomRenderPipelineAsset _asset;
        
       

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
        [Range(0.0f, 0.5f)]
        public float _heightScale = 0.1f;
        public float _groundLevel = 0;
        public bool _noise = false;
        public float _noiseScale = 0.015f;
        public float _noiseIntensity = 1.0f;
        public float _noiseIntensityOffset = 0.3f;
        public Vector2 _noiseVelocity = new Vector2(3.0f, 3.0f);
        public float _maxRayLength = 400.0f;

        public bool Inited { set; get; }
        
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

        public CustomRenderPipelineAsset Asset
        {
            get
            {
                return _asset;
            }

            set
            {
                _asset = value;
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
            if (camera == null)
            {
                Inited = false;
                return;
            }
            
            _light = GetComponent<Light>();
            
            _cmd = new Command("Volumetric Light");

            // _cameraDepthRT = RenderTexture.GetTemporary(camera._renderTarget._size.x, camera._renderTarget._size.y, 0, RenderTextureFormat.Depth);
            // _cameraDepthRT.filterMode = FilterMode.Point;

            Shader shader = Shader.Find("Custom RP/VolumetricLight");
            if (shader == null)
            {
                throw new Exception("Error: \"Custom RP/VolumetricLight\" shader is missing.");
            }
            _material = new Material(shader);
            
            Inited = true;
        }

        public void Tick(CustomRenderPipelineCamera camera, ref Command cmd, 
                        ref RenderTexture depthRT, ref RenderTexture volumetricLightRT)
        {
            
            if (_light == null || _light.gameObject == null) return;

            if (!_light.gameObject.activeInHierarchy || _light.enabled == false) return;

            _cmd.Context = cmd.Context; ;

            _material.SetVector(ShaderParams._cameraForward, camera._camera.transform.forward);
            _material.SetFloat(ShaderParams._zTest, (int)UnityEngine.Rendering.CompareFunction.Always);
            _material.SetInt(ShaderParams._sampleCount, _samplerCount);
            _material.SetVector(ShaderParams._noiseVelocity, new Vector4(_noiseVelocity.x, _noiseVelocity.y) * _noiseScale);
            _material.SetVector(ShaderParams._noiseData, new Vector4(_noiseScale, _noiseIntensity, _noiseIntensityOffset));
            _material.SetVector(ShaderParams._mieG, new Vector4(1 - (_mieG * _mieG), 1 + (_mieG * _mieG), 2 * _mieG, 1.0f / (4.0f * Mathf.PI)));
            _material.SetVector(ShaderParams._volumetircLight, new Vector4(_scatteringCoef, _exinctionCoef, _light.range, 1.0f - _skyBoxExtinctionCoef));
            _material.SetTexture(ShaderParams._CameraDepthTextureId, depthRT);
            
            
            if (_fog)
            {
                _material.EnableKeyword("_HEIGHT_FOG");
                _material.SetVector(ShaderParams._heightFog, new Vector4(_groundLevel, _heightScale));
            }
            else
            {
                _material.DisableKeyword("_HEIGHT_FOG");
            }
            
            _cmd.Cmd.SetRenderTarget(volumetricLightRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            _cmd.Cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 1));
            
            switch (_light.type)
            {
                case LightType.Directional:
                    SetDirectionalLight(camera._camera, ref volumetricLightRT);
                    break;
                case LightType.Point:
                    break;
                case LightType.Spot:
                    break;
            }

        }

        void SetDirectionalLight(Camera camera, ref RenderTexture rt)
        {
            // _cmd.Clear();
            int pass = 0;
            _material.SetPass(pass);
            
            var scene = ((CustomRenderPipeline)_asset.Pipeline).SceneController;
            if (scene._gpuDriven)
            {
                _material.EnableKeyword("USE_CLUSTER_LIGHT");
            }
            else
            {
                _material.DisableKeyword("USE_CLUSTER_LIGHT");
            }

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
                _material.EnableKeyword("_DIRECTIONAL");
                _material.DisableKeyword("_DIRECTIONAL_COOKIE");
            }
            else
            {
                _material.EnableKeyword("_DIRECTIONAL_COOKIE");
                _material.DisableKeyword("_DIRECTIONAL");

                _material.SetTexture(ShaderParams._lightTexture0, _light.cookie);
            }
            
            _frustum[0] = camera.ViewportToWorldPoint(new Vector3(0, 0, camera.farClipPlane));
            _frustum[2] = camera.ViewportToWorldPoint(new Vector3(0, 1, camera.farClipPlane));
            _frustum[3] = camera.ViewportToWorldPoint(new Vector3(1, 1, camera.farClipPlane));
            _frustum[1] = camera.ViewportToWorldPoint(new Vector3(1, 0, camera.farClipPlane));
            
            _material.SetVectorArray(ShaderParams._frustumCorners, _frustum);

            if (_light.shadows != LightShadows.None)
            {
                _cmd.Cmd.Blit(null, rt, _material, pass);
            }
            else
            {
            //     _cmd.Cmd.Blit(null, action.GetVolumetricLightRT(), _material, pass);
            }
            _cmd.Execute();
        }

        void OnDestroy()
        {
            if (_cmd != null)
            {
                _cmd.Release();
                _cmd = null;
            }
        }


        public void SaveRenderTexture (RenderTexture renderTexture, string file) {
            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D (renderTexture.width, renderTexture.height, TextureFormat.RFloat, false, false);
            texture.ReadPixels (new Rect (0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply ();

            byte[] bytes = texture.EncodeToPNG ();
            UnityEngine.Object.Destroy (texture);

            System.IO.File.WriteAllBytes (file, bytes);
            Debug.Log ("write to File over");
            UnityEditor.AssetDatabase.Refresh (); //自动刷新资源
        }
        
    };
};
