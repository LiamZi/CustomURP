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
        
        struct VolumeTileAABB
        {
            float4 minPoint;
            float4 maxPoint;
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
        ComputeBuffer _screenToView;
        ComputeBuffer _clusterAABB;
        ComputeBuffer _lightIndexBuffer;
        ComputeBuffer _gridLightIndexBuffer;

        public const int CLUSTER_GRID_BUILD_NUMTHREADS_X = 8;
        public const int CLUSTER_GRID_BUILD_NUMTHREADS_Y = 4;
        public const int LIGHT_CLUSTER_Z_SLICE = 16;
        public const int CLUSTER_MAX_LIGHTS_COUNT = 255;

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
            _lightListArray = new ScreenToView[CLUSTER_MAX_LIGHTS_COUNT];

            _clusterAABB = new ComputeBuffer(total, sizeof(VolumeTileAABB));
            _screenToView = new ComputeBuffer(CLUSTER_MAX_LIGHTS_COUNT, sizeof(ScreenToView));
            _lightIndexBuffer = new ComputeBuffer(total * CLUSTER_MAX_LIGHTS_COUNT, sizeof(uint));
            _gridLightIndexBuffer = new ComputeBuffer(total, sizeof(uint));

        }
        
        public override void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            // _cmd = cmd;
            _camera = camera;

            _zFar = camera._camera.farClipPlane;
            _zNear = camera._camera.nearClipPlane;
            var scale = _clusterSize.z / Mathf.Log(_zFar / _zNear);
            var bias = -_clusterSize.z * Mathf.Log(_zNear) / Mathf.Log(_zFar / _zNear);
            _clusterData = new Vector4(_clusterSize.x, _clusterSize.y, scale, bias);

            BuildGrid(camera, cmd.Context);
        }

        private void BuildGrid(CustomRenderPipelineCamera camera, ScriptableRenderContext context)
        {
            _cmd.SetComputeVectorParam(_clusterShading, ShaderParams._clusterDataId, _clusterData);
            _cmd.SetComputeFloatParam(_clusterShading, ShaderParams._clusterZFarId, _zFar);
            _cmd.SetComputeFloatParam(_clusterShading, ShaderParams._clusterZNearId, _zNear);
            _cmd.SetComputeBufferParam(_clusterShading, _clusterGridBuildKernel, ShaderParams._clusterGridRWId, _clusterAABB);
            _cmd.DispatchCompute(_clusterShading, _clusterGridBuildKernel, 1, 1, 1);
            
            context.ExecuteCommandBuffer(_cmd.Cmd);
            _cmd.Cmd.Clear();
        }
        
        public override bool InspectProperty()
        {
            return true;
        }
        
        
    }
}
