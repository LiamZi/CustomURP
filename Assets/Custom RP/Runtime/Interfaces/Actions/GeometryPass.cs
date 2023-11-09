using System.Collections;
using Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Pass/Geometry")]
    public sealed unsafe partial class GeometryPass : CoreAction
    {
        private CustomRenderPipelineAsset _asset;
        private CameraSettings _defaultCameraSettings = new CameraSettings();
        private bool _isHDR;
        private bool _isUseHDR;
        private bool _isUseColorTexture;
        private bool _isUseDepthTexture;
        private bool _isUseIntermediateBuffer;
        private bool _isUseScaledRendering;
        
        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            // throw new System.NotImplementedException();
            _asset = asset;
            InspectDependActions();
            _isInitialized = true;
        }

        protected internal override void Dispose()
        {
            base.Dispose();
        }

        public override void Tick(CustomRenderPipelineCamera camera)
        {
            base.Tick(camera);
            
            CameraSettings cameraSettings = camera ? camera.Setting : _defaultCameraSettings;
            if (camera._camera.cameraType == CameraType.Reflection)
            {
                _isUseColorTexture = CustomRenderPipeline._cameraBufferSettings._copyColorReflection;
                _isUseDepthTexture = CustomRenderPipeline._cameraBufferSettings._copyDepthReflection;
            }
            else
            {
                _isUseColorTexture = CustomRenderPipeline._cameraBufferSettings._copyColor && cameraSettings._copyColor;
                _isUseDepthTexture = CustomRenderPipeline._cameraBufferSettings._copyDepth && cameraSettings._copyDepth;
            }

            if (cameraSettings._enabledHizDepth)
            {
                
            }

            if (cameraSettings._overridePostFx)
            {
                // PostFXSettings
            }

            float renderScale = cameraSettings.GetRenderScale(CustomRenderPipeline._cameraBufferSettings._renderScale);
            _isUseScaledRendering = renderScale < 0.99f || renderScale > 1.01f;

            PrepareBuffer();
            PrepareForSceneWindow(camera);
        }

        public override void BeginRendering(CustomRenderPipelineCamera camera)
        {
            base.BeginRendering(camera);
        }

        public override void EndRendering(CustomRenderPipelineCamera camera)
        {
            base.EndRendering(camera);
        }

        public override bool InspectProperty()
        {
            return true;
        }
    }
    
}