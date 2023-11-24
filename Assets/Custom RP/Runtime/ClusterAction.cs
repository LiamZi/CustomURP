using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Cluster")]
    public sealed unsafe class ClusterAction : ScriptableObject
    {
        public static ClusterAction _cluster { get; private set; }
        public int _maxClusterCount = 100000;
        public int _maxMaterialCount = 1;
        public int _materialPoolSize = 500;
        
    }
}
