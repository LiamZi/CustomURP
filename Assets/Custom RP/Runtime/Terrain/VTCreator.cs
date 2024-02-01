using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{

    public class TVTCreateCmd
    {
        static Queue<TVTCreateCmd> _pool = new Queue<TVTCreateCmd>();
        public long _cmdId = 0;
        public int _size = 64;
        public Material[] _bakeDiffuse;
        public Material[] _bakeNormal;
        public Vector2 _uvMin;
        public Vector2 _uvMax;
        public ITVirtualTextureReceiver _receiver;
        static long _cmdIdSeed = 0;
        
        protected TVTCreateCmd()
        {
            
        }

        public static TVTCreateCmd Pop()
        {
            if (_pool.Count > 0)
            {
                return _pool.Dequeue();
            }
            return new TVTCreateCmd();
        }
        
        public static void Push(TVTCreateCmd cmd)
        {
            cmd._bakeDiffuse = null;
            cmd._bakeNormal = null;
            cmd._receiver = null;
            _pool.Enqueue(cmd);
        }

        public static void Clear()
        {
            _pool.Clear();
        }

        public static long GenerateId()
        {
            ++_cmdIdSeed;
            return _cmdIdSeed;
        }
    }

    public interface ITVirtualTexture
    {
        int size { get; }
        Texture tex { get; }
    }

    public interface IVTCreator
    {
        void AppendCmd(TVTCreateCmd cmd);
        void DiposeTextures(ITVirtualTexture[] textures);
    }
    
    public class VTCreator : MonoBehaviour, IVTCreator
    {
        public enum TextureQuality
        {
            Full,
            Half,
            Quater,
        };

        public TextureQuality _texQuality = TextureQuality.Full;
        public int _maxBakeCountPerFrame = 8;
        Queue<TVTCreateCmd> _vtCreateCmds = new Queue<TVTCreateCmd>();
        Dictionary<int, Queue<ITVirtualTexture[]>> _texturePools = new Dictionary<int, Queue<ITVirtualTexture[]>>();
        Queue<VTRenderJob> _bakedJobs = new Queue<VTRenderJob>();
        List<ITVirtualTexture> _activeTextures = new List<ITVirtualTexture>();
        

        RuntimeBakeTexture[] PopTexture(int size)
        {
            var texSize = size;
            if (_texQuality == TextureQuality.Half)
            {
                texSize = size >> 1;
            }
            else if (_texQuality == TextureQuality.Quater)
            {
                texSize = size >> 2;
            }

            RuntimeBakeTexture[] ret = null;
            if (!_texturePools.ContainsKey(texSize))
            {
                _texturePools.Add(texSize, new Queue<ITVirtualTexture[]>());
            }

            var q = _texturePools[texSize];
            if (q.Count > 0)
            {
                ret = q.Dequeue() as RuntimeBakeTexture[];
            }
            else
            {
                ret = new RuntimeBakeTexture[]
                {
                    new RuntimeBakeTexture(texSize), new RuntimeBakeTexture(texSize)
                };
            }
            return ret;
        }

        public void AppendCmd(TVTCreateCmd cmd)
        {
            _vtCreateCmds.Enqueue(cmd);
        }
        public void DiposeTextures(ITVirtualTexture[] textures)
        {
            var size = textures[0].size;
            _activeTextures.Remove(textures[0]);
            _activeTextures.Remove(textures[1]);
            
            if (_texturePools.ContainsKey(size))
            {
                _texturePools[size].Enqueue(textures);
            }
            else
            {
                Debug.LogWarning("DisposeTextures invlid texture size : " + size);
            }
        }

        void OnDestroy()
        {
            foreach (var q in _texturePools.Values)
            {
                while (q.Count > 0)
                {
                    var rbt = q.Dequeue() as RuntimeBakeTexture[];
                    rbt[0].Clear();
                    rbt[1].Clear();
                }
            }
            _texturePools.Clear();
        }

        void Tick()
        {
            while (_bakedJobs.Count > 0)
            {
                var job = _bakedJobs.Dequeue();
                job.SendTexturesReady();
                _activeTextures.Add(job._textures[0]);
                _activeTextures.Add(job._textures[1]);
                VTRenderJob.Push(job);
            }

            int bakeCount = 0;
            while (_vtCreateCmds.Count > 0 && bakeCount < _maxBakeCountPerFrame)
            {
                var cmd = _vtCreateCmds.Dequeue();
                if (cmd._receiver.WaitCmdId == cmd._cmdId)
                {
                    var ts = PopTexture(cmd._size);
                    ts[0].Reset(cmd._uvMin, cmd._uvMax, cmd._bakeDiffuse);
                    ts[1].Reset(cmd._uvMin, cmd._uvMax, cmd._bakeNormal);
                    var job = VTRenderJob.Pop();
                    job.Reset(cmd._cmdId, ts, cmd._receiver);
                    job.Tick();
                    _bakedJobs.Enqueue(job);
                    TVTCreateCmd.Push(cmd);
                    ++bakeCount;
                }
                else
                {
                    TVTCreateCmd.Push(cmd);
                }
            }

            for (int count = _activeTextures.Count - 1; count >= 0; --count)
            {
                var tex = _activeTextures[count] as RuntimeBakeTexture;
                bool needRender = tex.Validate();
                if (needRender)
                {
                    tex.Tick();
                }
            }
        }
    }
}
