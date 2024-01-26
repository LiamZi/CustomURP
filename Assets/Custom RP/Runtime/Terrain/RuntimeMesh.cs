using UnityEngine;
namespace CustomURP
{
    public class RenderMesh
    {
        public Mesh _mesh;
        public Vector2 _uvMin;
        public Vector2 _uvMax;

        public void Clear()
        {
            MonoBehaviour.Destroy(_mesh);
            _mesh = null;
        }
    }
}
