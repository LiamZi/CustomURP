using UnityEngine;

namespace CustomURP
{
    public class LodDetail
    {
        public int _subdivision = 3;
        public float _slopeAngleError = 5f;
    }

    public interface ITerrainTreeScanner
    {
        void Run(Vector3 center, out Vector3 hitpos, out Vector3 hitnormal);
    }

    public class TerrainScanner
    {
        
    }
    
    public class MeshCreatorJob
    {
        public TerrainScanner[] _lods;
        public MeshCreatorJob(UnityEngine.Terrain terrain, Bounds bounds, int maxX, int maxZ, LodDetail[] settings)
        {
            
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

        public void End()
        {
            
        }
    }
}
