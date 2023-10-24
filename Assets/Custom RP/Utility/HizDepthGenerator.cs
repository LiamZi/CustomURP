using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

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
        private const string _cmdName = "Hiz Depth";
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
            
            // _texture = new RenderTexture(TextureSize, TextureSize, 0, _format);
            _texture = RenderTexture.GetTemporary(TextureSize, TextureSize, 0, _format);
            _texture.filterMode = FilterMode.Point;
            _texture.useMipMap = true;
            _texture.autoGenerateMips = false;
            _texture.hideFlags = HideFlags.HideAndDontSave;
            _texture.Create();
            // _destId = new RenderTargetIdentifier(_texture);
        }
        
        public void OnPostRender()
        {
            if (_cmd == null) return;
            
            int w = _texture.width;
            int mipmapLevel = 0;
            
            RenderTargetIdentifier destId = new RenderTargetIdentifier(_texture);
            while (w > 8)
            {
                RenderTexture current = RenderTexture.GetTemporary(w, w, 0, _format);
                current.filterMode = FilterMode.Point;
                RenderTargetIdentifier currId = new RenderTargetIdentifier(current);
                
                var srcTexture = Shader.GetGlobalTexture(_shaderID);
                if(srcTexture == null) break;
                var srcId = new RenderTargetIdentifier(srcTexture);
                _material.SetTexture("_HizMap", srcTexture);
                _cmd.Blit(srcId, currId, _material);
                _cmd.CopyTexture(currId, 0, 0, destId, 0, mipmapLevel);
                RenderTexture.ReleaseTemporary(current);

                w /= 2;
                mipmapLevel++;
            }
            
            _context.ExecuteCommandBuffer(_cmd);
            _cmd.Clear();
            
            // SaveToFile(ref _texture, "111");
        }
        
        public void OnDestroy()
        {
            _texture?.Release();
            RenderTexture.DestroyImmediate(_texture);
        }

        public void SaveToFile(ref RenderTexture current, string name, bool mipChain = false)
        {
            int width = current.width;
            int height = current.height;
            var saveTex = new Texture2D(width, height, TextureFormat.RGBAFloat, mipChain);
            RenderTexture.active = current;
            saveTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            saveTex.Apply();
            byte[] vs = saveTex.EncodeToPNG();
                        
            string path = @"E:\"+ "abc" + name + ".png";
            FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            fileStream.Write(vs, 0, vs.Length);
            fileStream.Dispose();
            fileStream.Close();
        }
    };
};