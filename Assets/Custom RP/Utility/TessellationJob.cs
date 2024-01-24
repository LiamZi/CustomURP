using UnityEngine;

namespace CustomURP
{
    public class TessellationJob : ITerrainTreeScanner
    {
        public MeshData[] _mesh;
        public TerrainScanner[] _scanners;
        public TessellationJob(TerrainScanner[] scanner, float minTriangleArea)
        {
            _scanners = scanner;
            MinTriArea = minTriangleArea;
        }
        
        public TessellationJob(Terrain terrain, Bounds volume, int sub, float angleError, int maxX, int maxZ, bool sbrd)
        {
            
        }
            
        public void Run(Vector3 center, out Vector3 hitpos, out Vector3 hitnormal)
        {
            hitpos = center;
            hitnormal = center;
        }

        public void Tick()
        {
            
        }

        public float Progress
        {
            get
            {
                return 0.0f;
            }
        }

        public bool Done
        {
            get
            {
                return true;
            }
        }
        
        public float MinTriArea { get; private set; }
    }
}
