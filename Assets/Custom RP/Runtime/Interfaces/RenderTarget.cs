using CustomPipeline;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public sealed class RenderTarget
    {
        private readonly Texture2D _lostTexture;
        public Color _backgroundColor = Color.clear;
        public int _bufferSizeId = -1;
        public int _colorAttachmentId = -1;
        public int _colorTextureId = -1;
        public int _depthAttachmentId = -1;
        public int _depthTextureId = -1;
        public int _dstBlendId = -1;

        public bool _initialized;
        public bool _isUseColorTexture;
        public bool _isUseDepthTexture;
        public bool _isUseHDR;
        public bool _isUseIntermediateBuffer;
        public bool _isUseScaledRendering;
        public int2 _size = int2.zero;
        public int _sourceTextureId = 1;

        public int _srcBlendId = -1;
        public RenderTexture _colorAttachmentTex;
        public RenderTexture _depthAttachmentTex;


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
            // SetColorAndDepthAttachment(ref cmd, flags);
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
                if (DeviceUtility.CopyTextureSupported)
                    cmd.CopyTexture(_colorAttachmentId, _colorTextureId);
                else
                    Draw(ref cmd, _colorAttachmentId, _colorTextureId, material);
            }

            if (_isUseDepthTexture)
            {
                cmd.GetTemporaryRT(ShaderParams._CameraDepthTextureId, _size.x, _size.y,
                    32, FilterMode.Point, RenderTextureFormat.Depth);

                if (DeviceUtility.CopyTextureSupported)
                    cmd.CopyTexture(_depthAttachmentId, _depthTextureId);
                else
                    Draw(ref cmd, _depthAttachmentId, _depthTextureId, material, true);
            }

            if (!DeviceUtility.CopyTextureSupported)
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
            _colorAttachmentId = ShaderParams._CameraColorAttachmentId;
            _depthAttachmentId = ShaderParams._CameraDepthAttachmentId;
            // _colorAttachmentId = new RenderTargetIdentifier(ShaderParams._CameraColorAttachmentId);
            // _depthAttachmentId = new RenderTargetIdentifier(ShaderParams._CameraDepthAttachmentId);
            _bufferSizeId = ShaderParams._CamerabufferSizeId;
            _srcBlendId = ShaderParams._CameraSrcBlendId;
            _dstBlendId = ShaderParams._CameraDstBlendId;
            _sourceTextureId = ShaderParams._SourceTextureId;

            // var flags = _camera.clearFlags;
            // if (_isUseIntermediateBuffer)
            // {
            //     if (flags > CameraClearFlags.Color) flags = CameraClearFlags.Color;

            // cmd.GetTemporaryRT(
            //     (int)_colorAttachmentId, _size.x, _size.y,
            //     0, FilterMode.Bilinear, _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
            // );

            // cmd.GetTemporaryRT(
            //     (int)_depthAttachmentId, _size.x, _size.y,
            //     32, FilterMode.Point, RenderTextureFormat.Depth
            // );
            // _colorAttachmentTex = RenderTexture.GetTemporary(_size.x, _size.y, 0,
            //     _isUseHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            // _colorAttachmentTex.filterMode = FilterMode.Bilinear;
            //
            // _depthAttachmentTex = RenderTexture.GetTemporary(_size.x, _size.y, 32, RenderTextureFormat.Depth);
            // _depthAttachmentTex.filterMode = FilterMode.Point;

            // cmd.SetGlobalTexture(_colorAttachmentId, colorAttachmentTex);
            // cmd.SetGlobalTexture(_depthAttachmentId, deptAttachmentTex);
            //
            // cmd.SetRenderTarget(
            //     _colorAttachmentId,
            //     RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
            //     _depthAttachmentId,
            //     RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            // );
            // }

            // cmd.ClearRenderTarget(
            //     flags <= CameraClearFlags.Depth,
            //     flags <= CameraClearFlags.Color,
            //     Color.magenta
            // );
            //
            // cmd.Name = "Render Target Init";
            // cmd.BeginSample();
            // cmd.SetGlobalTexture(_colorTextureId, _lostTexture);
            // cmd.SetGlobalTexture(_depthTextureId, _lostTexture);
            // cmd.Execute();
        }

        public void Draw(ref Command cmd, RenderTargetIdentifier from, RenderTargetIdentifier to, Material material, bool isDepth = false)
        {
            cmd.SetGlobalTexture(ShaderParams._SourceTextureId, from);
            cmd.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
        }

        public void GetDepthTexture(ref Command cmd, ref RenderTexture depthTexture)
        {
            if (DeviceUtility.CopyTextureSupported)
            {
                cmd.CopyTexture(ShaderParams._CameraDepthTextureId, depthTexture);
                cmd.Execute();
            }
        }
    }
}