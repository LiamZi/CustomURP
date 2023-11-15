using CustomPipeline;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public sealed unsafe class RenderTarget
    {
        public int _colorAttachmentId = -1;
        public int _depthAttachmentId = -1;
        public int _colorTextureId = -1;
        public int _depthTextureId = -1;
        public bool _initialized = false;
        public bool _isUseIntermediateBuffer = false;
        public bool _isUseScaledRendering = false;
        public bool _isUseColorTexture = false;
        public bool _isUseDepthTexture = false;
        public int2 _size = int2.zero;
        public bool _isUseHDR = false;
        public Color _backgroundColor = Color.clear;
        
        private Texture2D _lostTexture = null;


        public RenderTarget(ref Command cmd, CameraType type, CameraSettings cameraSettings,
            float renderScale, int2 pixelSize,
            CameraClearFlags flags, bool useHDR, Color backgroundColor)
        {
            _isUseScaledRendering = renderScale < 0.99f || renderScale > 1.01f;
            _isUseHDR = useHDR;
            _backgroundColor = backgroundColor;
            _lostTexture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "Lost"
            };
            _lostTexture.SetPixel(0, 0, Color.white * 0.5f);
            _lostTexture.Apply(true, true);

            SetRenderTextureSize(renderScale, pixelSize);
            SetUseColorTexAndDepthTex(type, cameraSettings);
            
            _isUseIntermediateBuffer = _isUseScaledRendering || _isUseColorTexture || _isUseDepthTexture;
            // CameraClearFlags flags = camera._camera.clearFlags;
            SetColorAndDepthAttachment(ref cmd, flags);
            _initialized = true;
        }

        public void CopyAttachments(ref Command cmd, Material material)
        {
            cmd.Name = "Geometry Pass Copy Attachments";
            if (_isUseColorTexture)
            {
                cmd.GetTemporaryRT(ShaderParams._CameraColorTextureId, _size.x, 
                    _size.y, 0, 
                    FilterMode.Bilinear, _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                if(DeviceUtility.CopyTextureSupported)
                {
                    cmd.CopyTexture(_colorAttachmentId, ShaderParams._CameraColorTextureId);
                }
                else
                {
                    Draw(ref cmd, _colorAttachmentId, ShaderParams._CameraColorTextureId, material);
                }
            }

            if(_isUseDepthTexture)
            {
                cmd.GetTemporaryRT(ShaderParams._CameraDepthTextureId, _size.x, _size.y, 
                    32, FilterMode.Point, RenderTextureFormat.Depth);
 
                if(DeviceUtility.CopyTextureSupported)
                {
                    cmd.CopyTexture(_depthAttachmentId, ShaderParams._CameraDepthTextureId);
                }
                else
                {
                    Draw(ref cmd, _depthAttachmentId, ShaderParams._CameraDepthTextureId,  material, true);
                }
                
                // cmd.Execute();
            }

            if(!DeviceUtility.CopyTextureSupported)
            {
                cmd.SetRenderTarget(_colorAttachmentId, 
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, 
                    _depthAttachmentId, 
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            }
            
            cmd.Execute();
        }

        private void SetUseColorTexAndDepthTex(CameraType type, CameraSettings cameraSettings)
        {
            if (type == CameraType.Reflection)
            {
                _isUseColorTexture = CustomRenderPipeline._cameraBufferSettings._copyColorReflection;
                _isUseDepthTexture = CustomRenderPipeline._cameraBufferSettings._copyDepthReflection;
            }
            else
            {
                _isUseColorTexture =
                    CustomRenderPipeline._cameraBufferSettings._copyColor && cameraSettings._copyColor;
                _isUseDepthTexture =
                    CustomRenderPipeline._cameraBufferSettings._copyDepth && cameraSettings._copyDepth;
            }

            _colorTextureId = ShaderParams._CameraColorTextureId;
            _depthTextureId = ShaderParams._CameraDepthTextureId;
        }

        private void SetRenderTextureSize(float renderScale, int2 pixelSize)
        {
            _isUseScaledRendering = renderScale < 0.99f || renderScale > 1.01f;
        
            if (_isUseScaledRendering)
            {
                renderScale = Mathf.Clamp(renderScale, 0.1f, 2f);
                _size.x = (int)(pixelSize.x * renderScale);
                _size.y = (int)(pixelSize.y * renderScale);
            }
            else
            {
                _size.x = pixelSize.x;
                _size.y = pixelSize.y;
            }
        }

        private void SetColorAndDepthAttachment(ref Command cmd, CameraClearFlags flags)
        {
            //var color = Shader.PropertyToID("_CameraColorAttachment");
            //var depth = Shader.PropertyToID("_CameraDepthAttachment");
            //_colorAttachmentId = color;
            //_depthAttachmentId = depth;
             _colorAttachmentId = ShaderParams._CameraColorAttachmentId;
             _depthAttachmentId = ShaderParams._CameraDepthAttachmentId;
            
            if (_isUseIntermediateBuffer)
            {
                if (flags > CameraClearFlags.Color)
                {
                    flags = CameraClearFlags.Color;
                }

                //cmd.Cmd.GetTemporaryRT(_colorAttachmentId, _size.x, _size.y, 32, FilterMode.Bilinear,
                //    _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);

                //cmd.Cmd.GetTemporaryRT(_depthAttachmentId, _size.x, _size.y,
                //    32, FilterMode.Point, RenderTextureFormat.Depth);

                //cmd.Cmd.SetRenderTarget(_colorAttachmentId, RenderBufferLoadAction.DontCare,
                //    RenderBufferStoreAction.Store, _depthAttachmentId,
                //    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

                cmd.Cmd.GetTemporaryRT(
                    _colorAttachmentId, _size.x, _size.y,
                    0, FilterMode.Bilinear, _isUseHDR ?
                        RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
                );
                cmd.Cmd.GetTemporaryRT(
                    _depthAttachmentId, _size.x, _size.y,
                    32, FilterMode.Point, RenderTextureFormat.Depth
                );
                cmd.Cmd.SetRenderTarget(
                    _colorAttachmentId,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    _depthAttachmentId,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
            }

            cmd.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags <= CameraClearFlags.Color,
                Color.magenta
            );

            cmd.Name = "Render Target Init";
            cmd.BeginSample();
            cmd.SetGlobalTexture(_colorTextureId, _lostTexture);
            cmd.SetGlobalTexture(_depthTextureId, _lostTexture);
            cmd.Execute();
        }
        private void Draw(ref Command cmd, RenderTargetIdentifier from, RenderTargetIdentifier to, Material material, bool isDepth = false)
        {
            cmd.SetGlobalTexture(ShaderParams._SourceTextureId, from);
            cmd.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
        }

    };
}