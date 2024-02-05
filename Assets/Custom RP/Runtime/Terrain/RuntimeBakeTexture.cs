using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace CustomURP
{
    public class RuntimeBakeTexture : ITVirtualTexture
    {
        static Mesh _fullScreenMesh = null;
        static int _rttCount = 0;
        int _texSize = 32;
       
        public RenderTexture Rtt { get; private set; }
        Vector4 _scaleOffset;
        Command _cmd;

        
        public static Mesh FullScreenMesh
        {
            get
            {
                if (_fullScreenMesh != null)
                    return _fullScreenMesh;

                float topV = 1.0f;
                float bottomV = 0.0f;

                _fullScreenMesh = new Mesh { name = "FullScreen Quad" };
                _fullScreenMesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f,  1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f,  1.0f, 0.0f)
                });
                
                _fullScreenMesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0.0f, bottomV),
                    new Vector2(0.0f, topV),
                    new Vector2(1.0f, bottomV),
                    new Vector2(1.0f, topV)
                });
                
                _fullScreenMesh.SetIndices(new [] {0, 1, 2, 2, 1, 3}, MeshTopology.Triangles, 0, false);
                _fullScreenMesh.UploadMeshData(true);
                return _fullScreenMesh;
            }
        }
        
        int ITVirtualTexture.size
        {
            get
            {
                return _texSize;
            }
        }
        Texture ITVirtualTexture.tex
        {
            get
            {
                return Rtt;
            }
        }
        
        public Material[] _layers { get; private set; }
 
        public RuntimeBakeTexture(int size)
        {
            _texSize = size;
            _scaleOffset = new Vector4(1, 1, 0, 0);
            _cmd = new Command("RuntimeBakeTexture");
            CreateRTT();
        }

        void CreateRTT()
        {
            var format = RenderTextureFormat.Default;
            Rtt = new RenderTexture(_texSize, _texSize, 0, format, RenderTextureReadWrite.Default);
            Rtt.wrapMode = TextureWrapMode.Clamp;
            Rtt.Create();
            Rtt.DiscardContents();
            ++_rttCount;
        }

        public void Reset(Vector2 uvMin, Vector2 uvMax, Material[] mats)
        {
            _scaleOffset.x = uvMax.x - uvMin.x;
            _scaleOffset.y = uvMax.y - uvMin.y;
            _scaleOffset.z = uvMin.x;
            _scaleOffset.w = uvMin.y;
            _layers = mats;
            Validate();
        }

        public void Tick()
        {
            for (int i = 0; i < _layers.Length; ++i)
            {
                _layers[i].SetVector("_BakeScaleOffset", _scaleOffset);
            }
            
            Rtt.DiscardContents();
            _cmd.Clear();
            _cmd.Cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            _cmd.SetViewport(new Rect(0, 0, Rtt.width, Rtt.height));
            _cmd.Cmd.SetRenderTarget(Rtt, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);

            for (int i = 0; i < _layers.Length; ++i)
            {
                _layers[i].SetVector("_BakeScaleOffset", _scaleOffset);
                _cmd.Cmd.DrawMesh(FullScreenMesh, Matrix4x4.identity, _layers[i]);
            }
            Graphics.ExecuteCommandBuffer(_cmd.Cmd);
        }

        public bool Validate()
        {
            if (!Rtt.IsCreated())
            {
                Rtt.Release();
                CreateRTT();
                return true;
            }
            return false;
        }

        public void Clear()
        {
            if (Rtt != null)
            {
                Rtt.Release();
                Rtt = null;
            }
            _layers = null;
            _cmd.Clear();
            _cmd = null;
        }
    };

    public class VTRenderJob
    {
        static Queue<VTRenderJob> _pool = new Queue<VTRenderJob>();
        public RuntimeBakeTexture[] _textures;
        ITVirtualTextureReceiver _receiver;
        long _cmdId = 0;

        public static VTRenderJob Pop()
        {
            if (_pool.Count > 0)
            {
                return _pool.Dequeue();
            }
            return new VTRenderJob();
        }

        public static void Push(VTRenderJob job)
        {
            job._textures = null;
            job._receiver = null;
            _pool.Enqueue(job);
        }

        public void Clear()
        {
            _pool.Clear();
        }

        public void Reset(long cmd, RuntimeBakeTexture[] ts, ITVirtualTextureReceiver r)
        {
            _cmdId = cmd;
            _textures = ts;
            _receiver = r;
        }

        public void Tick()
        {
            for (int i = 0; i < _textures.Length; ++i)
            {
                var tex = _textures[i];
                tex.Tick();
            }
        }

        public void SendTexturesReady()
        {
            _receiver.OnTextureReady(_cmdId, _textures);
            _receiver = null;
        }
    };
}
