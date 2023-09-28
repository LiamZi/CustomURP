using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Custom_RP.Runtime.Interfaces.Components
{
    public class ClusterCommandBuffer
    {
        public ComputeBuffer _clusterBuffer;
        public ComputeBuffer _instanceCountBuffer;
        public ComputeBuffer _dispatchBuffer;
        public ComputeBuffer _reCheckResult;
        public ComputeBuffer _resultBuffer;
        public ComputeBuffer _verticesBuffer;
        public ComputeBuffer _reCheckCount;
        
    }
}