using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;

namespace CustomPipeline
{
    [Serializable]
    public class HizDepthGenerator
    {
        public Shader _depthTextureShader; 
        public RenderTexture Texture => _texture;
        private RenderTexture _texture;
        private int _textureSize = 0;
        private Material _material;
        private const RenderTextureFormat _format = RenderTextureFormat.Default;
        private int _shaderID;
        private const string _cmdName = "HizDepth";
        private Camera _camera;
        private ScriptableRenderContext _context;

        private CommandBuffer _cmd = null;
        public int TextureSize
        {
            get
            {
                if (_textureSize == 0)
                {
                    _textureSize = Mathf.NextPowerOfTwo(Mathf.Max(Screen.width, Screen.height));
                }

                return _textureSize;
            }
        }

        public void Setup(ScriptableRenderContext context, Camera camera)
        {
            _context = context;
            _camera = camera;
            _material = new Material(_depthTextureShader);
            
            _camera.depthTextureMode |= DepthTextureMode.Depth;
            _shaderID = Shader.PropertyToID("_CameraDepthTexture");
        
            Initialization();

            OnPostRender();
        }
        
        void Initialization()
        {
            if (_texture != null) return;

            _cmd = new CommandBuffer()
            {
                name = _cmdName
            };
            
            _texture = new RenderTexture(TextureSize, TextureSize, 0, _format);
            // _texture = _cmd.GetTemporaryRT(TextureSize, TextureSize, 0)
            _texture.autoGenerateMips = false;
            _texture.useMipMap = true;
            _texture.filterMode = FilterMode.Point;
            _texture.Create();
            
        }
        
        public void OnPostRender()
        {
            int w = _texture.width;
            int mipmapLevel = 0;
        
            // RenderTexture currentTexture = null;
            // int currentTextureID = Shader.PropertyToID("_HizMap");
            // int preRenderTextureId = -1;
            // RenderTexture preRenderTexture = null;
            bool isCopyCameraDepth = false;
            RenderTexture tempMipMapTexture = null;
        
            while (w > 8)
            {
                // _cmd.GetTemporaryRT(currentTextureID, w, w, 0, FilterMode.Point, _format);
                tempMipMapTexture = RenderTexture.GetTemporary(w, w, 0, _format);
                // if (isCopyCameraDepth == false)
                // {
                //     _cmd.Blit(Shader.GetGlobalTexture(_shaderID), currentTextureID);
                //     _context.ExecuteCommandBuffer(_cmd);
                //     _cmd.Clear();
                //     isCopyCameraDepth = true;   
                // }
                // else
                // {
                //     _cmd.Blit(currentTextureID, tempMipMapTexture, _material);
                //     _context.ExecuteCommandBuffer(_cmd);
                //     _cmd.Clear();
                //     // RenderTexture.ReleaseTemporary(tempMipMapTexture);
                // }
                
                _cmd.Blit(Shader.GetGlobalTexture(_shaderID), tempMipMapTexture);
                // _cmd.CopyTexture(currentTextureID, 0, 0, _texture, 0, mipmapLevel, _material);
                //
                _context.ExecuteCommandBuffer(_cmd);
                _cmd.Clear();
                
                Graphics.CopyTexture(tempMipMapTexture, 0, 0, _texture, 0, mipmapLevel);

                
                
                
                
                // _cmd.CopyTexture(tempMipMapTexture, 0, 0, _texture, 0, mipmapLevel);
                // _context.ExecuteCommandBuffer(_cmd);
                // _cmd.Clear();
                // _cmd.ReleaseTemporaryRT(currentTextureID);
                // RenderTexture.ReleaseTemporary(tempMipMapTexture);
                // // currentTexture = RenderTexture.GetTemporary(w, w, 0, _format);
                // _cmd.GetTemporaryRT(currentTextureID, w, w, 0, FilterMode.Point, _format);
                // // _cmd.SetRenderTarget(currentTexture);
                //
                // // currentTexture = _cmd.GetTemporaryRT("")
                // // currentTexture.filterMode = FilterMode.Point;
                // if (preRenderTextureId == -1)
                // {
                //     _cmd.Blit(Shader.GetGlobalTexture(_shaderID), currentTextureID);
                //     _context.ExecuteCommandBuffer(_cmd);
                //     _cmd.Clear();
                //     // _cmd.Blit(Shader.GetGlobalTexture(_shaderID), currentTexture.id);
                //     // Graphics.Blit(Shader.GetGlobalTexture(_shaderID), currentTexture);
                // }
                // else
                // {
                //     // Graphics.Blit(preRenderTexture, currentTexture, _material);
                //     // _material.SetTexture("_BaseMap", preRenderTexture);
                //     _cmd.Blit(preRenderTextureId, currentTextureID, _material);
                //     _context.ExecuteCommandBuffer(_cmd);
                //     _cmd.Clear();
                //     // RenderTexture.ReleaseTemporary(preRenderTexture);
                //
                // }
                //
                // // Graphics.CopyTexture(currentTexture, 0, 0, _texture, 0, mipmapLevel);
                // _cmd.CopyTexture(currentTextureID, 0, 0, _texture, 0, mipmapLevel);
                // _context.ExecuteCommandBuffer(_cmd);
                // _cmd.Clear();
                // // preRenderTexture = currentTexture;
                //
                // // var tex = Shader.GetGlobalTexture(currentTextureID);
                // // preRenderTexture = new RenderTexture(tex);
                // preRenderTextureId = currentTextureID;
                
                
                
                w /= 2;
                mipmapLevel++;
            }
            
            int width = _texture.width;
            int height = _texture.height;
            var saveTex = new Texture2D(_texture.width, _texture.height, TextureFormat.RGBAFloat, true);
            RenderTexture.active = _texture;
            saveTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            saveTex.Apply();
            byte[] vs = saveTex.EncodeToPNG();
                
            string path = @"E:\"+ "abc" + w + ".png";
            FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            fileStream.Write(vs, 0, vs.Length);
            fileStream.Dispose();
            fileStream.Close();
            
            // RenderTexture.ReleaseTemporary(preRenderTexture);
        }
        
        public void OnDestroy()
        {
            _texture?.Release();
            RenderTexture.DestroyImmediate(_texture);
        }
    };
};