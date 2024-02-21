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
        // Material _bilateralBlurMaterial;
        RenderTexture _volumeLightTexture;
        RenderTexture _halfVolumeLightTexture;
        RenderTexture _quarterVolumeLightTexture;
        RenderTexture _halfDepthBuffer;
        RenderTexture _quartDepthBuffer;
        VolumetricLightConfig.VolumtericRes _res = VolumetricLightConfig.VolumtericRes.Half;
        
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

            // shader = Shader.Find("Custom RP/BilateralBlur");
            // if (shader == null)
            // {
            //     throw new Exception("Error: \"Custom RP/BilateralBlur\" shader is missing.");
            // }
            // _bilateralBlurMaterial = new Material(shader);
        }
        
        public override bool InspectProperty()
        {
            return true;
        }
        
        public virtual void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            _cmd = cmd;
            _camera = camera;
        }

        public virtual void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            _cmd = cmd;
            _camera = camera;
        }

        public virtual void EndRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            _cmd = cmd;
            _camera = camera;
        }
    };
};
