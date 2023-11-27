using UnityEngine;
using UnityEngine.Rendering;


namespace CustomURP
{
    [CreateAssetMenu(fileName = "ClusterShadingSettings", menuName = "Rendering/Custom Cluster Shading Settings", order = 0)]
    public class ClusterShadingSettings : ScriptableObject
    {
        [SerializeField]
        public ClusterAction _clusterAction;
    }
}
