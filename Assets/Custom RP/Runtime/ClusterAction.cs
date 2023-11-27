using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Cluster")]
    public sealed unsafe class ClusterAction : CoreAction
    {
        struct ScreenToView
        {
            float4x4 inverseProjection;
            uint tileSizeX;
            uint tileSizeY;
            uint tileSizeZ;
            uint padding1;
            float2 tileSizePx;
            float2 viewPxSize;
            float scale;
            float bias;
            uint padding2;
            uint padding3;
        };
        
        public static ClusterAction _cluster { get; private set; }
        public int _maxClusterCount = 100000;
        public int _maxMaterialCount = 1;
        public int _materialPoolSize = 500;
        
        int2 _gridXY;
        int _clusterGridBuildKernel;
        int _gridLightBuildKernel;
        ScreenToView[] _lightListArray;
        
        [NonSerialized]
        public ComputeShader _clusterShading;
        ComputeBuffer _clusterAABB;
        ComputeBuffer _screenToView;
        ComputeBuffer _lightIndexBuffer;
        ComputeBuffer _gridLightIndexBuffer;

        public const int CLUSTER_GRID_BUILD_NUMTHREADS_X = 8;
        public const int CLUSTER_GRID_BUILD_NUMTHREADS_Y = 4;
        public const int LIGHT_CLUSTER_Z_SLICE = 16;

        int3 _clusterSize = new int3(CLUSTER_GRID_BUILD_NUMTHREADS_X, CLUSTER_GRID_BUILD_NUMTHREADS_Y, LIGHT_CLUSTER_Z_SLICE);
        float _zFar;
        float _zNear;
        Vector4 _clusterData;
        
        protected internal override void Initialization(CustomRenderPipelineAsset asset)
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                throw new PlatformNotSupportedException("Compute Shader NOt Supported.");
            }
            
            _clusterShading = asset._pipelineShaders._ClusterRenderShader;
            _cmd = new Command("ClusterAction");
            _clusterGridBuildKernel = _clusterShading.FindKernel("ClusterGridBuild");
            _gridLightBuildKernel = _clusterShading.FindKernel("GridLightBuild");

            int total = _clusterSize.x * _clusterSize.y * _clusterSize.z;
            

        }
        
        public override void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            _cmd = cmd;
            _camera = camera;
            
            
        }

        public override bool InspectProperty()
        {
            return true;
        }
    }
}
