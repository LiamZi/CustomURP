using System;
using Unity.Collections;
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
        
        struct AdditionalLightData
        {
            float3 PosWS;
            float AttenuationCoef;
            float3 Color;
            float3 SpotDir;
            float2 SpotAngle;
            float4 minPoint;
            float4 maxPoint; 
        };

        
        struct VolumeTileAABB
        {
            public float4 minPoint;
            public float4 maxPoint;
        };
        
        struct TestBox
        {
            public float3 p0, p1, p2, p3, p4, p5, p6, p7;
        };
        
        public static ClusterAction _cluster { get; private set; }
        public int _maxClusterCount = 100000;
        public int _maxMaterialCount = 1;
        public int _materialPoolSize = 500;
        public bool _isInited = false;
        
        int2 _gridXY;
        int _clusterGridBuildKernel;
        int _gridLightBuildKernel;
        AdditionalLightData[] _lightListArray;
        
        [NonSerialized]
        public ComputeShader _clusterShading;
        ComputeBuffer _lightListBuffer;
        ComputeBuffer _gridBuffer;
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
            _lightListArray = new AdditionalLightData[CLUSTER_MAX_LIGHTS_COUNT];

            _gridBuffer = new ComputeBuffer(total, sizeof(VolumeTileAABB));
            _lightListBuffer = new ComputeBuffer(CLUSTER_MAX_LIGHTS_COUNT, sizeof(AdditionalLightData));
            _lightIndexBuffer = new ComputeBuffer(total * CLUSTER_MAX_LIGHTS_COUNT, sizeof(uint));
            _gridLightIndexBuffer = new ComputeBuffer(total, sizeof(uint));

        }
        
        public override void BeginRendering(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            // _cmd = cmd;
            if (_isInited) return;
            _camera = camera;

            _zFar = camera._camera.farClipPlane;
            _zNear = camera._camera.nearClipPlane;
            var scale = _clusterSize.z / Mathf.Log(_zFar / _zNear);
            var bias = -_clusterSize.z * Mathf.Log(_zNear) / Mathf.Log(_zFar / _zNear);
            _clusterData = new Vector4(_clusterSize.x, _clusterSize.y, scale, bias);

            BuildGrid(camera, cmd.Context);

            _isInited = true;
            // DebugCluster(camera._camera);
        }

        private void BuildGrid(CustomRenderPipelineCamera camera, ScriptableRenderContext context)
        {
            _cmd.SetComputeVectorParam(_clusterShading, ShaderParams._clusterDataId, _clusterData);
            _cmd.SetComputeFloatParam(_clusterShading, ShaderParams._clusterZFarId, _zFar);
            _cmd.SetComputeFloatParam(_clusterShading, ShaderParams._clusterZNearId, _zNear);
            _cmd.SetComputeBufferParam(_clusterShading, _clusterGridBuildKernel, ShaderParams._clusterGridRWId, _gridBuffer);
            _cmd.DispatchCompute(_clusterShading, _clusterGridBuildKernel, 1, 1, 1);
            
            context.ExecuteCommandBuffer(_cmd.Cmd);
            _cmd.Cmd.Clear();
        }

        private void BuildLightList(NativeArray<VisibleLight> visibleLights, CustomRenderPipelineCamera camera, bool useLightsPerObject, int renderingLayerMask)
        {
            var w2v = camera._camera.worldToCameraMatrix;
            // int mainIndex = get
        }

        private void BuildGridLight(ScriptableRenderContext context)
        {
            _cmd.SetComputeFloatParam(_clusterShading, ShaderParams._clusterZFarId, _zFar);
            _cmd.SetComputeFloatParam(_clusterShading, ShaderParams._clusterZNearId, _zNear);
            _cmd.SetComputeBufferParam(_clusterShading, _gridLightBuildKernel, ShaderParams._clusterGridRWId, _gridBuffer);
            _cmd.SetComputeBufferParam(_clusterShading, _gridLightBuildKernel, ShaderParams._clusterLightListId, _lightListBuffer);
            _cmd.SetComputeBufferParam(_clusterShading, _gridLightBuildKernel, ShaderParams._clusterLightIndexRWId, _lightIndexBuffer);
            _cmd.SetComputeBufferParam(_clusterShading, _gridLightBuildKernel, ShaderParams._clusterGridRWId, _gridLightIndexBuffer);
            _cmd.Cmd.BeginSample("LightGridBuild");
            _cmd.DispatchCompute(_clusterShading, _clusterGridBuildKernel, 1, 1, 1);
            _cmd.Cmd.EndSample("LightGridBuild");
            context.ExecuteCommandBuffer(_cmd.Cmd);
            _cmd.Cmd.Clear();
        }

        public override bool InspectProperty()
        {
            return true;
        }
        
        public void DebugCluster(Camera camera)
        {
            var size = _clusterSize.x * _clusterSize.y * _clusterSize.z;
            VolumeTileAABB[] boxes = new VolumeTileAABB[size];
            _gridBuffer.GetData(boxes, 0, 0, size);
            
            
            Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            // Matrix4x4 projMatrix = _camera._camera.projectionMatrix;
            Matrix4x4 vpMatrix = projMatrix * viewMatrix;
            Matrix4x4 vpMatrixInv = vpMatrix.inverse;
            
            foreach (var i in boxes)
            {
                var xmin = i.minPoint.x;
                var ymin = i.minPoint.y;
                var zmin = i.minPoint.z;
                var wmin = i.minPoint.w;
                var xmax = i.maxPoint.x;
                var ymax = i.maxPoint.y;
                var zmax = i.maxPoint.z;
                
                float3 p0 = MatTransformProj(vpMatrixInv, new float3(xmin, ymin, 0));
                float3 p1 = MatTransformProj(vpMatrixInv, new float3(xmin, ymin, 1));
                float3 p2 = MatTransformProj(vpMatrixInv, new float3(xmin, ymax, 0));
                float3 p3 = MatTransformProj(vpMatrixInv, new float3(xmin, ymax, 1));
                float3 p4 = MatTransformProj(vpMatrixInv, new float3(xmax, ymin, 0));
                float3 p5 = MatTransformProj(vpMatrixInv, new float3(xmax, ymin, 1));
                float3 p6 = MatTransformProj(vpMatrixInv, new float3(xmax, ymax, 0));
                float3 p7 = MatTransformProj(vpMatrixInv, new float3(xmax, ymax, 1));
                
                TestBox box = new TestBox();
                
                box.p0 = p0 + zmin * (p1 - p0);
                box.p1 = p0 + zmax * (p1 - p0);
                box.p2 = p2 + zmin * (p3 - p2);
                box.p3 = p2 + zmax * (p3 - p2);
                box.p4 = p4 + zmin * (p5 - p4);
                box.p5 = p4 + zmax * (p5 - p4);
                box.p6 = p6 + zmin * (p7 - p6);
                box.p7 = p6 + zmax * (p7 - p6);
                
                DrawBox(box, Color.gray);
            }
               
        }
        
        float3 MatTransformProj(Matrix4x4 mat, float3 v3)
        {
            float4 v4 = new float4(v3, 1);
            // v4 = mul(mat, v4);
            v4 = mat * v4;
            v4 /= v4.w;
            return v4.xyz;
        }
        
        void DrawBox(TestBox box, Color color)
        {
            Debug.DrawLine(box.p0, box.p1, color);
            Debug.DrawLine(box.p0, box.p2, color);
            Debug.DrawLine(box.p0, box.p4, color);
        
            Debug.DrawLine(box.p6, box.p2, color);
            Debug.DrawLine(box.p6, box.p7, color);
            Debug.DrawLine(box.p6, box.p4, color);

            Debug.DrawLine(box.p5, box.p1, color);
            Debug.DrawLine(box.p5, box.p7, color);
            Debug.DrawLine(box.p5, box.p4, color);

            Debug.DrawLine(box.p3, box.p1, color);
            Debug.DrawLine(box.p3, box.p2, color);
            Debug.DrawLine(box.p3, box.p7, color);
        }
        
    }
}
