using System.Collections.Generic;
using UnityEngine;

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

        public void AppendCmd(TVTCreateCmd cmd)
        {
            throw new System.NotImplementedException();
        }
        public void DiposeTextures(ITVirtualTexture[] textures)
        {
            throw new System.NotImplementedException();
        }
    }
}
