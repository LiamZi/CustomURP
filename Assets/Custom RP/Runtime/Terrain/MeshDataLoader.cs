using TMPro;
using UnityEngine;
namespace CustomURP
{
    public interface IMeshDataLoader
    {
        byte[] LoadMeshData(string path);
        void UnloadAsset(string path);
    }
    
    public class MeshDataResLoader : IMeshDataLoader
    {

        public byte[] LoadMeshData(string path)
        {
            var resPath = string.Format("MeshData/{0}", path);
            var asset = Resources.Load(resPath) as TextAsset;
            return asset.bytes;
        }
        public void UnloadAsset(string path)
        {
            
        }
    }
}
