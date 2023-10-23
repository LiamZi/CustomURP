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
        private const RenderTextureFormat _format = RenderTextureFormat.RHalf;
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

        public void Setup(ScriptableRenderContext context, Camera camera, ref RenderTexture depthTexture)
        {
            _context = context;
            _camera = camera;
            _material = new Material(_depthTextureShader);
            
            _camera.depthTextureMode |= DepthTextureMode.Depth;
            _shaderID = Shader.PropertyToID("_CameraDepthTexture");
        
            Initialization(ref depthTexture);

            OnPostRender();
        }
        
        void Initialization(ref RenderTexture depthTexture)
        {
            if (_texture != null) return;

            _cmd = new CommandBuffer()
            {
                name = _cmdName
            };
            
            // _texture = new RenderTexture(TextureSize, TextureSize, 0, _format);
            _texture = depthTexture;
            // _texture.width = TextureSize;
            // _texture.height = TextureSize;

            
        }
        
        public void OnPostRender()
        {

            int w = _texture.width;
            int mipmapLevel = 0;

            RenderTexture current = null;
            RenderTexture preTex = null;

            while (w > 8)
            {
                current = RenderTexture.GetTemporary(w, w, 0, _format);
                current.filterMode = FilterMode.Point;

                if (preTex == null)
                {
                    Graphics.Blit(Shader.GetGlobalTexture(_shaderID), current);
                }
                else
                {
                    _material.SetTexture("_HizMap", preTex);
                    Graphics.Blit(preTex, current, _material);
                    RenderTexture.ReleaseTemporary(preTex);
                }
                
                Graphics.CopyTexture(current, 0, 0, _texture, 0, mipmapLevel);
                preTex = current;
                w /= 2;
                mipmapLevel++;
            }
        }
        
        public void OnDestroy()
        {
            // _texture?.Release();
            // RenderTexture.Destroy(_texture);
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