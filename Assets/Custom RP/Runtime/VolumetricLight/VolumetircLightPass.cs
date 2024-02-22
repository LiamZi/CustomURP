using System;
using UnityEngine;

namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Pass/VolumetircLight Pass")]
    public class VolumetircLightPass : CoreAction
    {
        static Mesh _pointLightMesh;
        static Mesh _spotLightMesh;
        static Material _lightMaterial;
        static Texture _defaultSpotCookie;
        
        Matrix4x4 _viewProj;
        Material _blitAddMaterial;
        Material _bilateralBlurMaterial;
        RenderTexture _volumeLightTexture;
        RenderTexture _halfVolumeLightTexture;
        RenderTexture _quarterVolumeLightTexture;
        RenderTexture _halfDepthBuffer;
        RenderTexture _quartDepthBuffer;
        VolumetricLightConfig.VolumtericRes _currentRes;
        VolumetricLightConfig.VolumtericRes _res = VolumetricLightConfig.VolumtericRes.Half;
        VolumetricLight Light { get; set; }
        
        
        public VolumetricLightConfig _lightConfig;
        public Texture2D _ditheringTex;
        public Texture3D _noiseTex;
        

        public static Material LightMaterial
        {
            get
            {
                return _lightMaterial;
            }
        }

        public static Mesh PointLightMesh
        {
            get
            {
                return _pointLightMesh;
            }
        }

        public static Mesh SpotLightMesh
        {
            get
            {
                return _spotLightMesh;
            }
        }

        public RenderTexture GetVolumetricLightRT()
        {
            switch (_lightConfig._res)
            {
                case  VolumetricLightConfig.VolumtericRes.Quarter:
                    return _quarterVolumeLightTexture;
                case  VolumetricLightConfig.VolumtericRes.Half:
                    return _halfVolumeLightTexture;
                default:
                    return _volumeLightTexture;
            }
        }

        public RenderTexture GetVolumetricDepthBuffer()
        {
            switch (_lightConfig._res)
            {
                case  VolumetricLightConfig.VolumtericRes.Quarter:
                    return _quartDepthBuffer;
                case  VolumetricLightConfig.VolumtericRes.Half:
                    return _halfDepthBuffer;
                default:
                    return null;
            }
        }

        public static Texture DefaultSpotCookie
        {
            get
            {
                return _defaultSpotCookie;
            }
        }
        
        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            Shader shader = Shader.Find("Custom URP/BlitAdd");
            if (shader == null)
            {
                throw new Exception("Error: \"Custom URP/BlitAdd\" shader is missing.");
            }
            _blitAddMaterial = new Material(shader);

            shader = Shader.Find("Custom RP/BilateralBlur");
            if (shader == null)
            {
                throw new Exception("Error: \"Custom RP/BilateralBlur\" shader is missing.");
            }
            _bilateralBlurMaterial = new Material(shader);

            _cmd = new Command("preLightPass");

            ChangeRes();

            if (_lightMaterial == null)
            {
                shader = Shader.Find("Custom RP/VolumetricLight");
                if (shader == null)
                {
                    throw new Exception("Error: \"Custom RP/VolumetricLight\" shader is missing.");
                }
                _lightMaterial = new Material(shader);
            }
            
            GenerateDitherTexture();
        }

        void ChangeRes()
        {
            var camera = Camera.current;
            int width = camera.pixelWidth;
            int height = camera.pixelHeight;

            if (_volumeLightTexture != null)
            {
                 Destroy(_volumeLightTexture);
            }

            _volumeLightTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf);
            _volumeLightTexture.name = "VolumeLightRT";
            _volumeLightTexture.filterMode = FilterMode.Bilinear;

            if (_halfDepthBuffer != null)
            {
                Destroy(_halfDepthBuffer);
            }

            if (_halfVolumeLightTexture != null)
            {
                Destroy(_halfVolumeLightTexture);
            }

            if (_res == VolumetricLightConfig.VolumtericRes.Half || _res == VolumetricLightConfig.VolumtericRes.Quarter)
            {
                _halfVolumeLightTexture = new RenderTexture(width / 2, height / 2, 0, RenderTextureFormat.ARGBHalf);
                _halfVolumeLightTexture.name = "VolumeLightHalfRT";
                _halfVolumeLightTexture.filterMode = FilterMode.Bilinear;

                _halfDepthBuffer = new RenderTexture(width / 2, height / 2, 0, RenderTextureFormat.Depth);
                _halfDepthBuffer.name = "VolumeLightHalfDepth";
                _halfDepthBuffer.Create();
                _halfDepthBuffer.filterMode = FilterMode.Point;
            }

            if (_quarterVolumeLightTexture != null)
            {
                Destroy(_quarterVolumeLightTexture);
            }

            if (_quartDepthBuffer != null)
            {
                Destroy(_quartDepthBuffer);
            }

            if (_res == VolumetricLightConfig.VolumtericRes.Quarter)
            {
                _quarterVolumeLightTexture = new RenderTexture(width / 4, height / 4, 0, RenderTextureFormat.ARGBHalf);
                _quarterVolumeLightTexture.name = "VolumeLightQuarterRT";
                _quarterVolumeLightTexture.filterMode = FilterMode.Bilinear;

                _quartDepthBuffer = new RenderTexture(width / 4, height / 4, 0, RenderTextureFormat.Depth);
                _quartDepthBuffer.name = "VolumeLightQuarterDepth";
                _quartDepthBuffer.Create();
                _quartDepthBuffer.filterMode = FilterMode.Point;
            }
        }
        
        public override bool InspectProperty()
        {
            return true;
        }
        
        public virtual void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            // _cmd = cmd;
            _camera = camera;
            if (_currentRes != _res)
            {
                _currentRes = _res;
                ChangeRes();
            }

            if (_volumeLightTexture.width != _camera._camera.pixelWidth || _volumeLightTexture.height != _camera._camera.pixelHeight)
            {
                ChangeRes();
            }
        }

        public virtual void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            // _cmd = cmd;
            _camera = camera;

            var currCamera = camera._camera;
            Matrix4x4 proj = Matrix4x4.Perspective(currCamera.fieldOfView, currCamera.aspect, 0.01f, currCamera.farClipPlane);

            proj = GL.GetGPUProjectionMatrix(proj, true);
            _viewProj = proj * currCamera.worldToCameraMatrix;

            bool shaderLevel = SystemInfo.graphicsShaderLevel > 40;
            
            _cmd.Clear();
            if (_res == VolumetricLightConfig.VolumtericRes.Quarter)
            {
                _cmd.Cmd.Blit(null, _halfDepthBuffer, _bilateralBlurMaterial, shaderLevel ? 4 : 10);
                _cmd.Cmd.Blit(null, _quartDepthBuffer, _bilateralBlurMaterial, shaderLevel ? 6 : 11);
                _cmd.Cmd.SetRenderTarget(_quarterVolumeLightTexture);
            }
            else if (_res == VolumetricLightConfig.VolumtericRes.Half)
            {
                _cmd.Cmd.Blit(null, _halfDepthBuffer, _bilateralBlurMaterial, shaderLevel ? 4 : 10);
                _cmd.Cmd.SetRenderTarget(_halfVolumeLightTexture);
            }
            else
            {
                _cmd.Cmd.SetRenderTarget(_volumeLightTexture);
            }
            
            _cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 1));
            BindMaterialParams();

            if (this.Light != null)
            {
                this.Light.Tick(_camera, ref _cmd);
            }
        }

        void BindMaterialParams()
        {
            _bilateralBlurMaterial.SetTexture("_HalfResDepthBuffer", _halfDepthBuffer);
            _bilateralBlurMaterial.SetTexture("_HalfResColor", _halfVolumeLightTexture);
            _bilateralBlurMaterial.SetTexture("_QuaterResDepthBuffer", _quartDepthBuffer);
            _bilateralBlurMaterial.SetTexture("_QuaterResColor", _quarterVolumeLightTexture);
            
            Shader.SetGlobalTexture("_DitherTexture", _ditheringTex);
            Shader.SetGlobalTexture("_NoiseTexture", _noiseTex);
            
        }

        public virtual void EndRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            // _cmd = cmd;
            _camera = camera;
            if (_res == VolumetricLightConfig.VolumtericRes.Quarter)
            {
                var temp = RenderTexture.GetTemporary(_quartDepthBuffer.width, _quartDepthBuffer.height, 0, RenderTextureFormat.ARGBHalf);
                temp.filterMode = FilterMode.Bilinear;
                
                _cmd.Cmd.Blit(_quarterVolumeLightTexture, temp, _bilateralBlurMaterial, 8);
                _cmd.Cmd.Blit(temp, _quarterVolumeLightTexture, _bilateralBlurMaterial, 9);
                _cmd.Cmd.Blit(_quarterVolumeLightTexture, _volumeLightTexture, _bilateralBlurMaterial, 7);
                RenderTexture.ReleaseTemporary(temp);
            }
            else if (_res == VolumetricLightConfig.VolumtericRes.Half)
            {
                var temp = RenderTexture.GetTemporary(_halfVolumeLightTexture.width, _halfVolumeLightTexture.height, 0, RenderTextureFormat.ARGBHalf);
                temp.filterMode = FilterMode.Bilinear;
                _cmd.Cmd.Blit(_halfVolumeLightTexture, temp, _bilateralBlurMaterial, 2);
                _cmd.Cmd.Blit(temp, _halfVolumeLightTexture, _bilateralBlurMaterial, 3);
                _cmd.Cmd.Blit(_halfVolumeLightTexture, _volumeLightTexture, _bilateralBlurMaterial, 5);
                RenderTexture.ReleaseTemporary(temp);
            }
            else
            {
                var temp = RenderTexture.GetTemporary(_volumeLightTexture.width, _volumeLightTexture.height, 0, RenderTextureFormat.ARGBHalf);
                temp.filterMode = FilterMode.Bilinear;
                _cmd.Cmd.Blit(_volumeLightTexture, temp, _bilateralBlurMaterial, 0);
                _cmd.Cmd.Blit(temp, _volumeLightTexture, _bilateralBlurMaterial, 1);
               
                RenderTexture.ReleaseTemporary(temp);
            }
            
            _cmd.Cmd.SetGlobalTexture("_Source", ShaderParams._CameraColorTextureId);
            _cmd.Cmd.Blit(_volumeLightTexture, ShaderParams._CameraColorTextureId, _blitAddMaterial);
            cmd.Context.ExecuteCommandBuffer(_cmd.Cmd);
        }

        void GenerateDitherTexture()
        {
            if (_ditheringTex != null) return;

            int size = 8;
            _ditheringTex = new Texture2D(size, size, TextureFormat.Alpha8, false, true);
            _ditheringTex.filterMode = FilterMode.Point;
            Color32[] c = new Color32[size * size];
            
            byte b;
            int i = 0;
            b = (byte)(1.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(49.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(13.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(61.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(4.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(52.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(16.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(64.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(33.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(17.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(45.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(29.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(36.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(20.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(48.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(32.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(9.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(57.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(5.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(53.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(12.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(60.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(8.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(56.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(41.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(25.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(37.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(21.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(44.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(28.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(40.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(24.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(3.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(51.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(15.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(63.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(2.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(50.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(14.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(62.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(35.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(19.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(47.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(31.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(34.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(18.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(46.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(30.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(11.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(59.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(7.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(55.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(10.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(58.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(6.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(54.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);

            b = (byte)(43.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(27.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(39.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(23.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(42.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(26.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(38.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            b = (byte)(22.0f / 65.0f * 255); c[i++] = new Color32(b, b, b, b);
            
            _ditheringTex.SetPixels32(c);
            _ditheringTex.Apply();
        }
    };
};
