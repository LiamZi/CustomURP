using UnityEngine;
namespace CustomURP
{
    public class MeshData
    {
        public class Lod
        {
            public Vector3[] _vertices;
            public Vector3[] _normals;
            public Vector2[] _uvs;
            public int[] _faces;
            public Vector2 _uvMin;
            public Vector3 _uvMax;
        }
        
        public int _lodLevel = -1;
        public Lod[] _lods;
        
        public int _meshId { get; private set; }
        public Bounds _bounds { get; private set; }

        public MeshData(int id, Bounds bound)
        {
            _meshId = id;
            _bounds = bound;
        }

        public MeshData(int id, Bounds bound, int level)
        {
            _meshId = id;
            _bounds = bound;
            _lodLevel = level;
        }
    }
}
